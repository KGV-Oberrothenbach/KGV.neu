// Reparaturblock 03: an account may only be deleted after a durable, member-bound
// SQL reservation. Restoration is one SQL transaction and never trusts JSON roles.
const CAN_MANAGE_ROLES = 512n;
const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

function result(status, outcome, message) {
  return { httpStatus: status, outcome, message };
}

function bit(value) {
  if (typeof value === 'bigint') return value;
  if (typeof value === 'number' && Number.isSafeInteger(value)) return BigInt(value);
  if (typeof value === 'string' && /^\d+$/.test(value)) return BigInt(value);
  throw new Error('Invalid permission bit field');
}

async function restoreReservation(admin, userId, operationId) {
  try {
    const r = await admin.rpc('restore_auth_user_deletion', {
      p_user_id: userId, p_operation_id: operationId,
    });
    return !r?.error && r?.data === true;
  } catch { return false; }
}

async function reservationFor(admin, userId, operationId, memberId) {
  const response = await admin.from('auth_user_delete_lock')
    .select('user_id,operation_id,mitglied_id')
    .eq('user_id', userId)
    .maybeSingle();
  if (response.error) return { status: 'unknown' };
  if (!response.data) return { status: 'absent' };
  if (response.data.operation_id !== operationId || Number(response.data.mitglied_id) !== memberId) {
    return { status: 'other_operation' };
  }
  return { status: 'ours' };
}

/**
 * Caller JWT is verified by index.ts. The privileged client must stay server-only.
 * Auth deletion can fail because historical documents/contract parties reference
 * auth.users via RESTRICT/NO ACTION. Those failures restore the exact DB snapshot.
 * Unknown outcomes retain the durable reservation and require manual review.
 */
export async function performAccountDeletion({ callerClient, adminClient, callerId, memberId, expectedAuthUserId, dryRun = false }) {
  if (!Number.isSafeInteger(memberId) || memberId <= 0) {
    return result(400, 'bad_request', 'mitgliedId muss eine positive ganze Zahl sein.');
  }
  if (!UUID.test(callerId ?? '')) {
    return result(401, 'forbidden', 'Authentifizierter Benutzer fehlt.');
  }

  const permission = await callerClient.rpc('has_effective_permission', {
    p_permission: Number(CAN_MANAGE_ROLES),
  });
  if (permission.error || permission.data !== true) {
    return result(403, 'forbidden', 'CanManageRoles fehlt oder konnte nicht geprüft werden.');
  }

  const member = await adminClient.from('mitglied')
    .select('id,auth_user_id').eq('id', memberId).maybeSingle();
  if (member.error) return result(503, 'error', 'Mitglied konnte nicht sicher geladen werden.');
  if (!member.data) return result(404, 'not_found', 'Mitglied nicht gefunden.');
  const authUserId = member.data.auth_user_id;
  if (!authUserId) return result(200, 'no_user', 'Kein verknüpftes Benutzerkonto vorhanden.');
  if (authUserId !== expectedAuthUserId) {
    return result(409, 'blocked', 'Die ausgewählte Benutzerzuordnung hat sich geändert.');
  }
  if (authUserId === callerId) {
    return result(403, 'forbidden', 'Das eigene Benutzerkonto kann hier nicht gelöscht werden.');
  }

  const target = await adminClient.from('app_user')
    .select('user_id,mitglied_id,role,permission_grants,permission_revocations,is_protected')
    .eq('user_id', authUserId).maybeSingle();
  if (target.error || !target.data || Number(target.data.mitglied_id) !== memberId) {
    return result(409, 'blocked', 'Die Benutzerzuordnung ist unvollständig oder verändert.');
  }
  if (target.data.is_protected) {
    return result(403, 'forbidden', 'Geschützte Konten dürfen nicht gelöscht werden.');
  }

  const base = await adminClient.rpc('permission_role_base', { p_role: target.data.role });
  if (base.error || base.data == null) {
    return result(503, 'error', 'Zielberechtigung konnte nicht sicher geprüft werden.');
  }
  let targetPermissions;
  try {
    targetPermissions = (bit(base.data) | bit(target.data.permission_grants))
      & ~bit(target.data.permission_revocations);
  } catch {
    return result(503, 'error', 'Ungültige Berechtigungsdaten; Löschung gesperrt.');
  }
  if ((targetPermissions & CAN_MANAGE_ROLES) === CAN_MANAGE_ROLES) {
    return result(403, 'forbidden', 'Rollenverwalter benötigen ein gesondertes Löschverfahren.');
  }

  const existing = await adminClient.from('auth_user_delete_lock')
    .select('user_id').eq('user_id', authUserId).maybeSingle();
  if (existing.error) return result(503, 'error', 'Löschstatus konnte nicht überprüft werden.');
  if (existing.data) return result(409, 'blocked', 'Für dieses Konto läuft bereits eine Löschung.');
  if (dryRun === true) return result(200, 'eligible', 'Prüfung bestanden; keine Daten verändert.');

  // An opaque per-attempt ID prevents an ambiguous response from restoring
  // a different administrator's reservation.
  const operationId = crypto.randomUUID();
  let reserved;
  try {
    reserved = await callerClient.rpc('begin_auth_user_deletion', {
      p_user_id: authUserId,
      p_mitglied_id: memberId,
      p_operation_id: operationId,
    });
  } catch { reserved = { error: { message: 'Unknown SQL outcome' } }; }

  if (reserved?.error || reserved?.data !== true) {
    // A timeout may occur *after* COMMIT. Restore only this specific attempt.
    const state = await reservationFor(adminClient, authUserId, operationId, memberId);
    if (state.status === 'absent') {
      return result(409, 'blocked', 'Die Datenbank hat die sichere Kontotrennung verweigert.');
    }
    if (state.status === 'ours') {
      const auth = await adminClient.auth.admin.getUserById(authUserId).catch(() => null);
      if (!auth?.error && auth?.data?.user?.id === authUserId) {
        const recovered = await restoreReservation(adminClient, authUserId, operationId);
        return recovered
          ? result(502, 'unlink_outcome_restored', 'Unklaren SQL-Aufruf sicher zurückgesetzt.')
          : result(503, 'manual_recovery_required', 'Kontotrennung unklar; Wiederherstellung unvollständig.');
      }
    }
    return result(503, 'manual_review_required', 'Löschstatus unklar; keine weiteren Versuche starten.');
  }

  let deleted;
  try { deleted = await adminClient.auth.admin.deleteUser(authUserId); }
  catch { deleted = { error: { message: 'Auth API unavailable' } }; }

  if (!deleted?.error) {
    try {
      const finished = await adminClient.rpc('finish_auth_user_deletion', {
        p_user_id: authUserId, p_operation_id: operationId,
      });
      if (!finished?.error && finished?.data === true) {
        return result(200, 'deleted', 'Benutzerkonto und Mitgliedsverknüpfung gelöscht.');
      }
    } catch { /* fail closed */ }
    return result(503, 'manual_review_required',
      'Auth-Konto wurde gelöscht; der Abschlussdatensatz muss manuell geprüft werden.');
  }

  let current;
  try { current = await adminClient.auth.admin.getUserById(authUserId); }
  catch { current = null; }
  if (!current?.error && current?.data?.user?.id === authUserId) {
    const recovered = await restoreReservation(adminClient, authUserId, operationId);
    return recovered
      ? result(502, 'delete_failed_restored', 'Auth-Löschung fehlgeschlagen; Zuordnung und Rechte wiederhergestellt.')
      : result(503, 'manual_recovery_required', 'Auth-Löschung fehlgeschlagen; Wiederherstellung nicht vollständig.');
  }
  return result(503, 'manual_review_required',
    'Auth-Löschstatus nicht sicher bestimmbar. Löschsperre bleibt bestehen.');
}
