import { useEffect, useState } from "react";
import { loadExpiringMaintenanceAssignments } from "../../services/maintenance/maintenance-continuation-service";
import { useSelectedClub } from "../auth/AuthProvider";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { ExpiringMaintenanceAssignment } from "../../models/maintenance/maintenance-continuation";

export function ExpiringMaintenanceContracts({ session }: { session: BrowserSession }) {
  const club = useSelectedClub(); const [items, setItems] = useState<ExpiringMaintenanceAssignment[]>([]); const [open, setOpen] = useState(false); const [error, setError] = useState("");
  useEffect(() => { if (!open) return; void loadExpiringMaintenanceAssignments(club, session).then((rows) => { setItems(rows); setError(""); }).catch(() => setError("Auslaufende Wartungsverträge konnten nicht geladen werden.")); }, [club, open, session]);
  return <section className="maintenance-expiring"><button className="secondary-action" onClick={() => setOpen((value) => !value)}>{open ? "Auslaufende Wartungsverträge ausblenden" : "Auslaufende Wartungsverträge"}</button>{open && <>{error && <p className="notice" role="alert">{error}</p>}<div className="data-table-wrap"><table><thead><tr><th>Vertragsende</th><th>Mitglied</th><th>Wartungsvertrag</th><th>Entscheidung</th></tr></thead><tbody>{items.map((item) => <tr key={item.zuordnung_id}><td>{item.gueltig_bis.split("-").reverse().join(".")}</td><td>{item.mitglied_name ?? `Mitglied #${item.hauptmitglied_id}`}</td><td>{item.wartungsvertrag_titel}</td><td>{item.entscheidung === "verlaengern" ? "Verlängert" : item.entscheidung === "nicht_verlaengern" ? (item.zurueckgenommen_am ? "Nichtverlängerung zurückgenommen" : "Nicht verlängern") : "Offen"}</td></tr>)}</tbody></table>{items.length === 0 && <p>Keine auslaufenden Wartungsverträge.</p>}</div></>}</section>;
}
