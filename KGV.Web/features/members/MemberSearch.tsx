"use client";

import { useEffect, useMemo, useState } from "react";
import { type BrowserSession } from "../../lib/supabase-auth";
import { type MemberSearchResult } from "../../models/members/member";
import { filterMemberSearchResults, loadMemberSearchResults } from "../../services/members/member-service";

export function MemberSearch({ session, selectedMemberId, onSelect, canCreate, onCreate }: { session: BrowserSession; selectedMemberId: number | null; onSelect: (mitgliedId: number) => void; canCreate: boolean; onCreate: () => void }) {
  const [members, setMembers] = useState<MemberSearchResult[]>([]);
  const [query, setQuery] = useState("");
  const [showInactiveMembers, setShowInactiveMembers] = useState(false);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    loadMemberSearchResults(session)
      .then((items) => { if (active) setMembers(items); })
      .catch((cause: Error) => { if (active) setError(cause.message); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [session]);

  const results = useMemo(() => filterMemberSearchResults(members, { query, includeInactive: showInactiveMembers }), [members, query, showInactiveMembers]);
  const selected = members.find((item) => item.id === selectedMemberId) ?? null;
  return <section className="data-workspace" aria-label="Mitglieder suchen">
    <div className="data-toolbar"><label>Suche<input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Name, E-Mail, Mitgliedsnummer oder Gartennummer" /></label><label className="filter-check"><input type="checkbox" checked={showInactiveMembers} onChange={(event) => setShowInactiveMembers(event.target.checked)} /> Inaktive Mitglieder anzeigen</label><span>{loading ? "Lädt …" : `${results.length} Mitglieder`}</span>{canCreate && <button onClick={onCreate}>Mitglied anlegen</button>}</div>
    {error ? <p className="notice" role="alert">{error}</p> : <><div className="data-table-wrap"><table><thead><tr><th>Name</th><th>Garten</th><th>E-Mail</th><th>Status</th></tr></thead><tbody>{results.map((member) => <tr key={member.id} className={selected?.id === member.id ? "selected-row" : ""} onClick={() => onSelect(member.id)}><td><strong>{member.name ?? "–"}</strong>, {member.vorname ?? ""}</td><td>{member.gartenNummern.join(", ") || "–"}</td><td>{member.email ?? "–"}</td><td>{member.aktiv ? "aktiv" : "inaktiv"}</td></tr>)}</tbody></table></div>{selected && <p className="context-note">{[selected.vorname, selected.name].filter(Boolean).join(" ")} ist ausgewählt. Die zugehörigen Bereiche – einschließlich Stammdaten – stehen nun eingerückt im Menü.</p>}</>}
  </section>;
}
