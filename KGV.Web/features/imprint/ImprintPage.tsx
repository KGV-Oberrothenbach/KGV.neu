"use client";

import { useEffect, useState } from "react";
import { type BrowserSession } from "../../lib/supabase-auth";
import { loadImprintContacts, type ImprintContact } from "../../services/imprint/imprint-service";

function ContactSection({ title, items, fallback }: { title: string; items: ImprintContact[]; fallback: string }) {
  return <section className="imprint-contact-section"><h2>{title}</h2>{items.length ? <div className="imprint-contact-list">{items.map((item) => <article key={item.id}><strong>{item.functionName}</strong><span>{item.name}</span>{item.mobile ? <a href={`tel:${item.mobile.replace(/\s/g, "")}`}>{item.mobile}</a> : <small>Handynummer aktuell nicht hinterlegt.</small>}{item.address && <small>{item.address}</small>}</article>)}</div> : <p>{fallback}</p>}</section>;
}

export default function ImprintPage({ session }: { session: BrowserSession }) {
  const [contacts, setContacts] = useState<ImprintContact[]>([]);
  const [message, setMessage] = useState("");
  const [privacyOpen, setPrivacyOpen] = useState(false);

  useEffect(() => {
    let active = true;
    setMessage("");
    loadImprintContacts(session).then((next) => {
      if (active) setContacts(next);
    }).catch((cause) => {
      if (active) { setContacts([]); setMessage(cause instanceof Error ? cause.message : "Weitere Impressumskontakte konnten nicht geladen werden."); }
    });
    return () => { active = false; };
  }, [session]);

  useEffect(() => { if (!privacyOpen) return; const close = (event: KeyboardEvent) => { if (event.key === "Escape") setPrivacyOpen(false); }; window.addEventListener("keydown", close); return () => window.removeEventListener("keydown", close); }, [privacyOpen]);
  const board = contacts.filter((item) => !item.isConstruction);
  const construction = contacts.filter((item) => item.isConstruction);
  const privacyUrl = "https://kgv-oberrothenbach.github.io/KGV.neu/datenschutz.html";

  return <section className="data-workspace imprint-page">
    {message && <p className="notice" role="status">{message}</p>}
    <div className="imprint-layout"><section className="imprint-club-card"><span className="eyebrow">Impressum</span><h2>Kleingartenverein Oberrothenbach e.V.</h2><p>Amtsgericht Chemnitz VR 70502</p><div className="imprint-responsible"><strong>Verantwortlich</strong><span>Mary Krüger-Rau</span><span>Scheringer Str. 12</span><span>08056 Zwickau</span></div><a href="mailto:kgvoberrothenbach@gmx.de">kgvoberrothenbach@gmx.de</a></section><div className="imprint-contacts"><ContactSection title="Weitere Vorstandsmitglieder" items={board} fallback="Aktuell keine weiteren Vorstandsangaben hinterlegt." /><ContactSection title="Bauausschuss" items={construction} fallback="Aktuell keine Angaben zum Bauausschuss hinterlegt." /></div></div>
    <section className="imprint-privacy"><div><h2>Datenschutz</h2><p>Die Datenschutzerklärung zur App ist online abrufbar.</p></div><button onClick={() => setPrivacyOpen(true)}>Datenschutzerklärung anzeigen</button></section>
    {privacyOpen && <div className="modal-backdrop" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) setPrivacyOpen(false); }}><section className="home-detail-modal privacy-modal" role="dialog" aria-modal="true" aria-labelledby="privacy-title"><div className="home-detail-header"><div><span className="eyebrow">Rechtliche Informationen</span><h2 id="privacy-title">Datenschutzerklärung</h2></div><button className="modal-close" aria-label="Datenschutzerklärung schließen" onClick={() => setPrivacyOpen(false)}>×</button></div><p>Die aktuelle Datenschutzerklärung wird auf der offiziellen Vereinsseite bereitgestellt. Sie öffnet sich in einem neuen Browser-Tab.</p><a className="button-link" href={privacyUrl} target="_blank" rel="noopener noreferrer">Datenschutzerklärung öffnen</a></section></div>}
  </section>;
}