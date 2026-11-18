// P11-FIX-01: isolated and unit-testable authorization + account deletion workflow.
// This module must never be exposed directly as an HTTP handler.
const CAN_MANAGE_ROLES = 512n;

function result(status, outcome, message, extra = {}) {
  return { httpStatus: status, outcome, message, ...extra };
}

function validPositiveInteger(value) {
  return Number.isSafeInteger(value) && value > 0;
}

function permissionBit(value) {
  if (typeof value === 'bigint') return value;
  if (typeof value === 'number' && Number.isSafeInteger(value)) return BigInt(value);
  if (typeof value === 'string' && /^-?\d+$/.test(value)) return BigInt(value);
  throw new Error('Invalid bigint permission value');
}

async function restoreFailedDeletion(admin, snapshot) {
  const { memberId, authUserId, appUser } = snapshot;
  // member.auth_user_id references auth.users ON DELETE CASCADE. Reattachment
  // is safe ONLY after verifying that the Auth account still exists.
  const linked = await admin.from('mitglied')
    .update({ auth_user_id: authUserId })
    .eq('id', memberId)
    .is('auth_user_id', null)
    .select('id')
    .maybeSingle();
  if (linked.error || !linked.data) return false;

  // The member trigger recreates app_user with default role. Restore the exact
  // previously effective permission inputs after the link has been restored.
  const restored = await admin.from('app_user')
    .update({
      role: appUser.role,
      permission_grants: appUser.permission_grants,
      permission_revocations: appUser.permission_revocations,
      is_protected: appUser.is_protected,
    })
    .eq('user_id', authUserId)
    .select('user_id')
    .maybeSingle();
  return !restored.error && !!restored.data;
}

/**
 * The callerClient MUST carry the verified user JWT. The adminClient MUST be
 * server-only. This workflow never accepts caller role/permissions from input.
 *
 * Managing/privileged target accounts deliberately require separate manual
 * administration: they are rejected even if not marked is_protected. This is
 * an intentional fail-closed rule until a transactional last-manager protocol
 * is implemented and independently exercised.
 */
export async function performAccountDeletion({ callerClient, adminClient, callerId, memberId, dryRun = false }) {
  if (!validPositiveInteger(memberId)) return result(400, 'bad_request', 'mitgliedId muss eine positive ganze Zahl sein.');
  if (typeof callerId !== 'string' || !/^[a-f\d-]{36}$/i.test(callerId)) {
    return result(401, 'forbidden', 'Authentifizierter Benutzer fehlt.');
  }

  // Guard 1: Do NOT authorize based on admin/vorstand text roles.
  const permission = await callerClient.rpc('has_effective_permission', { p_permission: Number(CAN_MANAGE_ROLES) });
  if (permission.error || permission.data !== true) {
    return result(403, 'forbidden', 'CanManageRoles fehlt oder konnte nicht geprüft werden.');
  }

  const member = await adminClient.from('mitglied')
    .select('id,auth_user_id')
    .eq('id', memberId)
    .maybeSingle();
  if (member.error) return result(503, 'error', 'Mitglied konnte nicht sicher geladen werden.');
  if (!member.data) return result(404, 'not_found', 'Mitglied nicht gefunden.');
  const authUserId = member.data.auth_user_id;
  if (!authUserId) return result(200, 'no_user', 'Kein verknüpftes Benutzerkonto vorhanden.');
  if (authUserId === callerId) return result(403, 'forbidden', 'Das eigene Benutzerkonto kann hier nicht gelöscht werden.');

  const target = await adminClient.from('app_user')
    .select('user_id,mitglied_id,role,permission_grants,permission_revocations,is_protected')
    .eq('user_id', authUserId)
    .maybeSingle();
  if (target.error || !target.data) {
    return result(409, 'blocked', 'Die Benutzerzuordnung ist unvollständig; Löschung gesperrt.');
  }
  if (Number(target.data.mitglied_id) !== memberId) {
    return result(409, 'blocked', 'Die Benutzerzuordnung stimmt nicht überein.');
  }
  if (target.data.is_protected === true) {
    return result(403, 'forbidden', 'Geschützte Benutzerkonten dürfen nicht gelöscht werden.');
  }

  // Guard 2: privileged TARGET accounts are not deletable by this endpoint.
  // This is stronger than a non-atomic count of remaining administrators.
  const base = await adminClient.rpc('permission_role_base', { p_role: target.data.role });
  if (base.error || base.data === null || base.data === undefined) {
    return result(503, 'error', 'Zielberechtigung konnte nicht sicher geprüft werden.');
  }
  let managesRoles;
  try {
    const effective = (permissionBit(base.data) | permissionBit(target.data.permission_grants))
      & ~permissionBit(target.data.permission_revocations);
    managesRoles = (effective & CAN_MANAGE_ROLES) === CAN_MANAGE_ROLES;
  } catch (_error) {
    return result(503, 'error', 'Ungültiges Berechtigungsformat; Löschung gesperrt.');
  }
  if (managesRoles) {
    return result(403, 'forbidden', 'Konten mit Rollenverwaltungsrechten benötigen ein gesondertes Verfahren.');
  }

  if (dryRun === true) return result(200, 'eligible', 'Prüfung bestanden; keine Daten verändert.');

  // Guard 3: the caller-bound DB RPC rechecks CanManageRoles, locks the target,
  // rejects is_protected and unlinks member/app_user in one DB transaction.
  let unlinked;
  try { unlinked = await callerClient.rpc('unlink_app_user', { p_user_id: authUserId }); }
  catch (_error) { unlinked = { error: { message: 'RPC outcome unavailable' } }; }
  if (unlinked?.error) {
    // An API timeout is ambiguous: the SQL transaction may already have
    // committed. Verify the resulting relationship before claiming failure.
    try {
      const [postMember, postApp] = await Promise.all([
        adminClient.from('mitglied').select('id,auth_user_id').eq('id', memberId).maybeSingle(),
        adminClient.from('app_user').select('user_id').eq('user_id', authUserId).maybeSingle(),
      ]);
      if (!postMember.error && !postApp.error &&
          postMember.data?.auth_user_id === authUserId && postApp.data) {
        return result(409, 'blocked', 'Datenbank hat die Kontotrennung verweigert.');
      }
      if (!postMember.error && !postApp.error &&
          postMember.data?.auth_user_id === null && !postApp.data) {
        const auth = await adminClient.auth.admin.getUserById(authUserId);
        if (!auth.error && auth.data?.user?.id === authUserId) {
          const recovered = await restoreFailedDeletion(adminClient,
            { memberId, authUserId, appUser: target.data });
          return recovered
            ? result(502, 'unlink_outcome_restored', 'Unklaren Datenbankaufruf sicher zurückgesetzt.')
            : result(503, 'manual_recovery_required', 'Unklarer Datenbankaufruf; Wiederherstellung unvollständig.');
        }
      }
    } catch (_error) { /* fall through to fail-closed manual review */ }
    return result(503, 'manual_review_required', 'Ergebnis der Kontotrennung unklar; manuelle Prüfung notwendig.');
  }

  // Auth deletion and database unlink cannot be part of one SQL transaction.
  // A failure is handled by checking account existence and restoring snapshot.
  let authDeletion;
  try {
    authDeletion = await adminClient.auth.admin.deleteUser(authUserId);
  } catch (_error) {
    authDeletion = { error: { message: 'Auth deletion request failed' } };
  }
  if (!authDeletion?.error) return result(200, 'deleted', 'Benutzerkonto gelöscht.');

  let current;
  try { current = await adminClient.auth.admin.getUserById(authUserId); }
  catch (_error) { current = { error: { message: 'Unable to verify Auth state' } }; }

  if (current && !current.error && current.data?.user?.id === authUserId) {
    try {
      const recovered = await restoreFailedDeletion(adminClient, {
        memberId, authUserId, appUser: target.data,
      });
      return recovered
        ? result(502, 'delete_failed_restored', 'Auth-Löschung fehlgeschlagen; Mitglied und Rechte wiederhergestellt.')
        : result(503, 'manual_recovery_required', 'Auth-Löschung fehlgeschlagen; Wiederherstellung nicht vollständig.');
    } catch (_error) {
      return result(503, 'manual_recovery_required', 'Auth-Löschung fehlgeschlagen; Wiederherstellung nicht vollständig.');
    }
  }
  return result(503, 'manual_review_required', 'Auth-Löschstatus ist unklar; bitte vor weiteren Schritten manuell prüfen.');
}
