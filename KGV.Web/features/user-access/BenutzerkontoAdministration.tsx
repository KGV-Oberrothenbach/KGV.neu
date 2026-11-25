"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import type { BrowserSession } from "../../lib/supabase-auth";
import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import { useSelectedClub } from "../auth/AuthProvider";
import { deleteAppUser, inviteAppUser, sendPasswordReset } from "../../services/users/app-user-service";

type UserAccount = {
  user_id: string | null;
  mitglied_id: number;
  email: string | null;
  legacy_role: string | null;
  ist_geschuetzt: boolean;
  hat_admin_zusatzrolle: boolean;
};

export function BenutzerkontoAdministration({ session, memberId, canManage }: {
  session: BrowserSession;
  memberId: number;
  canManage: boolean;
}) {
  const club = useSelectedClub();
  const [account, setAccount] = useState<UserAccount | null>(null);
  const [pending, setPending] = useState(false);
  const [loading, setLoading] = useState(true);
  const [notice, setNotice] = useState("");
  const [error, setError] = useState("");

  const requestGeneration = useRef(0);
  const refresh = useCallback(async () => {
    const generation = ++requestGeneration.current;
    const rows = await callSupabaseRpc<UserAccount[]>(
      { club, session }, "fr212_user_account", { p_mitglied_id: memberId },
    );
    if (!Array.isArray(rows) || rows.length !== 1 || rows[0].mitglied_id !== memberId) {
      throw new Error("Der Benutzerstatus konnte nicht eindeutig gelesen werden.");
    }
    if (generation !== requestGeneration.current) return false;
    setAccount(rows[0]);
    return true;
  }, [club, session, memberId]);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setAccount(null);
    setError("");
    refresh().catch((cause: unknown) => {
      if (active) setError(cause instanceof Error ? cause.message : "Benutzerstatus nicht verfügbar.");
    }).finally(() => { if (active) setLoading(false); });
    return () => { active = false; ++requestGeneration.current; };
  }, [refresh]);

  async function perform(task: () => Promise<string | void>) {
    if (!canManage || pending || loading || !account) return;
    setPending(true);
    setNotice("");
    setError("");
    try {
      const message = await task();
      if (!await refresh()) return;
      setNotice(message || "Vorgang abgeschlossen; Kontostatus erneut vom Server gelesen.");
    } catch (cause) {
      setAccount(null);
      setError(cause instanceof Error ? cause.message : "Benutzervorgang fehlgeschlagen.");
    } finally {
      setPending(false);
    }
  }

  function invite() {
    if (!account || account.user_id || !account.email) return;
    void perform(async () => {
      const result = await inviteAppUser(club, session, memberId, "user");
      if (result.success !== true && result.outcome !== "prepared_mail_failed") throw new Error(result.message ?? "Einladung wurde abgelehnt.");
      if (result.mailSent === false || result.outcome === "prepared_mail_failed") return "Konto vorbereitet, Willkommensmail jedoch nicht versendet.";
    });
  }

  function resetPassword() {
    if (!account?.user_id || !account.email) return;
    void perform(async () => {
      const result = await sendPasswordReset(club, session, account.email!);
      if (result.success !== true) throw new Error(result.message ?? "Reset-Anforderung fehlgeschlagen.");
      return result.message ?? "Reset-Anfrage angenommen; ein Versand ist damit nicht bestätigt.";
    });
  }

  function remove() {
    if (!account?.user_id || account.ist_geschuetzt || account.hat_admin_zusatzrolle) return;
    if (!window.confirm("Das Auth-Benutzerkonto dauerhaft löschen und seine Mitgliedszuordnung entfernen?")) return;
    void perform(async () => { await deleteAppUser(club, session, memberId, account.user_id!); });
  }

  return <section className="data-workspace" aria-label="Benutzerkonto">
    <h2>Benutzerkonto</h2>
    <p className="context-note">Benutzerkonten und Fachrechte werden getrennt verwaltet. Neue Konten erhalten keine privilegierte Rolle durch die Einladung.</p>
    {loading && <p role="status">Lade Kontostatus …</p>}
    {error && <p className="notice" role="alert">{error}</p>}
    {notice && <p role="status">{notice}</p>}
    {!loading && account && <div className="detail-panel">
      <p>App-Konto: <strong>{account.user_id ? "verknüpft" : "nicht vorhanden"}</strong>{account.ist_geschuetzt || account.hat_admin_zusatzrolle ? " · geschützt/privilegiert" : ""}</p>
      <p>Kontakt: {account.email || "keine E-Mail hinterlegt"}</p>
      {!canManage && <p className="context-note">Nur Lesezugriff auf Benutzerkonten.</p>}
      {canManage && <div className="editor-actions">
        {!account.user_id && <button disabled={pending || !account.email} onClick={invite}>Nutzer einladen</button>}
        {account.user_id && <button className="secondary-action" disabled={pending || !account.email} onClick={resetPassword}>Passwort-Reset anfordern</button>}
        {account.user_id && !account.ist_geschuetzt && !account.hat_admin_zusatzrolle &&
          <button className="danger-action" disabled={pending} onClick={remove}>Benutzerkonto entfernen</button>}
      </div>}
    </div>}
  </section>;
}
