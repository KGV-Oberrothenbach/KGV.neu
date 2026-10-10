import { type WorkHoursSummary } from "./work-hours-types";

const formatHours = (value: number | null | undefined) => value === null || value === undefined
  ? "–"
  : `${Number(value).toLocaleString("de-DE", { maximumFractionDigits: 2 })} h`;

const formatEuro = (value: number | null | undefined) => value === null || value === undefined
  ? "–"
  : new Intl.NumberFormat("de-DE", { style: "currency", currency: "EUR" }).format(Number(value));

function statusNotes(summary: WorkHoursSummary): string[] {
  const notes: string[] = [];
  if (summary.ist_befreit) notes.push(summary.regelgrund ? `Von Pflichtstunden befreit: ${summary.regelgrund}` : "Von Pflichtstunden befreit.");
  else if (summary.regelgrund) notes.push(`Regelgrund: ${summary.regelgrund}`);
  if (summary.hat_wartungsvertrag) notes.push("Ein Wartungsvertrag ist berücksichtigt.");
  if (!summary.ist_befreit && summary.wartungsvertrag_gutschrift_stunden > 0) notes.push(`WV-Gutschrift: ${formatHours(summary.wartungsvertrag_gutschrift_stunden)}`);
  if (summary.altersbefreit) notes.push("Altersbefreiung ist berücksichtigt.");
  if (summary.eintritt_im_saisonjahr) notes.push("Eintritt im Saisonjahr ist berücksichtigt.");
  if (summary.eintritt_zweites_halbjahr) notes.push("Eintritt im zweiten Halbjahr ist berücksichtigt.");
  return notes;
}

export function RequiredHoursSummary({ summary }: { summary: WorkHoursSummary | null }) {
  const notes = summary ? statusNotes(summary) : [];
  return <>
    <div className="meter-summary work-hours-summary">
      <div><span>Sollstunden</span><strong>{formatHours(summary?.pflichtstunden_soll)}</strong></div>
      <div><span>Freigegeben</span><strong>{formatHours(summary?.geleistete_stunden)}</strong></div>
      <div><span>Noch offen</span><strong>{formatHours(summary?.offene_stunden)}</strong></div>
      <div><span>Euro je Fehlstunde</span><strong>{formatEuro(summary?.euro_pro_fehlstunde)}</strong></div>
      <div><span>Fehlbetrag</span><strong>{formatEuro(summary?.fehlbetrag)}</strong></div>
    </div>
    {notes.length > 0 && <section className="work-hours-notes" aria-label="Pflichtstundenstatus"><strong>Pflichtstundenstatus</strong><ul>{notes.map((note) => <li key={note}>{note}</li>)}</ul></section>}
  </>;
}
