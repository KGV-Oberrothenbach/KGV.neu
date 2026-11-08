"use client";
import { useEffect, useState } from "react";
import type { BrowserSession } from "../../lib/supabase-auth";
import { useSelectedClub } from "../auth/AuthProvider";
import { getMainSubMeterDifferences } from "../../services/seasons/annual-consumption-service";
import type { MainSubMeterDifference } from "../../repositories/readings/annual-consumption-repository";

const display = (value: number | null) => value === null ? "Nicht berechenbar" : value.toLocaleString("de-DE", { maximumFractionDigits: 2 });
export default function MainSubMeterDifferenceView({ session, year }: { session: BrowserSession; year: number }) {
  const club = useSelectedClub(); const [rows, setRows] = useState<MainSubMeterDifference[]>([]); const [error, setError] = useState(""); const [loading, setLoading] = useState(true);
  useEffect(() => { let active=true; setLoading(true); getMainSubMeterDifferences(club,session,year).then(x=>{if(active){setRows(x);setError("");}}).catch(e=>active&&setError(e instanceof Error?e.message:"Auswertung konnte nicht geladen werden.")).finally(()=>active&&setLoading(false)); return()=>{active=false;}; },[club,session,year]);
  return <section className="detail-panel reading-history" aria-label="Haupt- und Unterzählerauswertung"><h2>Haupt-/Unterzähler {year}</h2>{loading?<p>Lädt …</p>:error?<p className="notice" role="alert">{error}</p>:<div className="data-table-wrap"><table><thead><tr><th>Bereich</th><th>Hauptzähler</th><th>Unterzähler</th><th>Differenz</th><th>Einheit</th><th>Status</th></tr></thead><tbody>{rows.map(row=>{const status=!row.berechenbar?`Nicht berechenbar${row.fehlertext?`: ${row.fehlertext}`:""}`:row.pruefbeduerftig?"Prüfbedürftig":"Berechenbar";return <tr key={`${row.medium}-${row.anlagenbereich}`} className={row.pruefbeduerftig?"selected-row":""}><td>{row.medium} {row.anlagenbereich}</td><td>{display(row.hauptzaehler_verbrauch)}</td><td>{display(row.unterzaehler_verbrauch)}</td><td>{display(row.differenz)}</td><td>{row.einheit}</td><td>{status}</td></tr>;})}</tbody></table></div>}</section>;
}
