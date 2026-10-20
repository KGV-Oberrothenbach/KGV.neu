"use client";
import { useSelectedClub } from "../auth/AuthProvider";
import { useEffect, useState } from "react";
import { type BrowserSession } from "../../lib/supabase-auth";
import { loadImprintData, type ImprintContact, type ImprintData } from "../../services/imprint/imprint-service";

const emptyImprint: ImprintData = { club: { name: "Aktuell nicht hinterlegt", register: "Aktuell nicht hinterlegt", email: "" }, responsible: null, board: [], construction: [] };
function ContactSection({ title, items, fallback }: { title: string; items: ImprintContact[]; fallback: string }) { return <section className="imprint-contact-section"><h2>{title}</h2>{items.length ? <div className="imprint-contact-list">{items.map((item) => <article key={item.id}><strong>{item.functionName}</strong><span>{item.name}</span>{item.mobile ? <a href={`tel:${item.mobile.replace(/\s/g, "")}`}>{item.mobile}</a> : <small>Handynummer aktuell nicht hinterlegt.</small>}</article>)}</div> : <p>{fallback}</p>}</section>; }

export default function ImprintPage({ session }: { session: BrowserSession }) {
  const club = useSelectedClub(); const [imprint, setImprint] = useState<ImprintData>(emptyImprint); const [message, setMessage] = useState(""); const [privacyOpen, setPrivacyOpen] = useState(false);
  useEffect(() => { let active = true; loadImprintData(club, session).then((next) => { if (active) { setImprint(next); setMessage(""); } }).catch((cause) => { if (active) { setImprint(emptyImprint); setMessage(cause instanceof Error ? cause.message : "Impressumsdaten konnten nicht geladen werden."); } }); return () => { active = false; }; }, [session, club]);
  useEffect(() => { if (!privacyOpen) return; const close = (event: KeyboardEvent) => { if (event.key === "Escape") setPrivacyOpen(false); }; window.addEventListener("keydown", close); return () => window.removeEventListener("keydown", close); }, [privacyOpen]);
  const privacyUrl = "https://kgv-oberrothenbach.github.io/KGV.neu/datenschutz.html";
  const responsible = imprint.responsible;
  return <section className="data-workspace imprint-page">
    {message && <p className="notice" role="status">{message}</p>}
    <div className="imprint-layout"><section className="imprint-club-card"><span className="eyebrow">Impressum</span><h2>{imprint.club.name}</h2><p>{imprint.club.register}</p><div className="imprint-responsible"><strong>Verantwortlich</strong><span>{responsible?.name ?? "Aktuell nicht hinterlegt."}</span>{responsible?.address && <span>{responsible.address}</span>}</div>{imprint.club.email ? <a href={`mailto:${imprint.club.email}`}>{imprint.club.email}</a> : <span>Aktuell nicht hinterlegt.</span>}</section><div className="imprint-contacts"><ContactSection title="Weitere Vorstandsmitglieder" items={imprint.board} fallback="Aktuell keine weiteren Vorstandsangaben hinterlegt." /><ContactSection title="Bauausschuss" items={imprint.construction} fallback="Aktuell keine Angaben zum Bauausschuss hinterlegt." /></div></div>
    <section className="imprint-privacy"><div><h2>Datenschutz</h2><p>Die Datenschutzerklärung zur App ist online abrufbar.</p></div><button onClick={() => setPrivacyOpen(true)}>Datenschutzerklärung anzeigen</button></section>
    {privacyOpen && <div className="modal-backdrop" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) setPrivacyOpen(false); }}><section className="home-detail-modal privacy-modal" role="dialog" aria-modal="true" aria-labelledby="privacy-title"><div className="home-detail-header"><div><span className="eyebrow">Rechtliche Informationen</span><h2 id="privacy-title">Datenschutzerklärung</h2></div><button className="modal-close" aria-label="Datenschutzerklärung schließen" onClick={() => setPrivacyOpen(false)}>×</button></div><p>Die aktuelle Datenschutzerklärung wird auf der offiziellen Vereinsseite bereitgestellt. Sie öffnet sich in einem neuen Browser-Tab.</p><a className="button-link" href={privacyUrl} target="_blank" rel="noopener noreferrer">Datenschutzerklärung öffnen</a></section></div>}
  </section>;
}
