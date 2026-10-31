import assert from "node:assert/strict";
import test from "node:test";
import { briefanrede, resolveSerienbriefText } from "../../features/serienbrief/serienbrief-vorschau.ts";
import { bereiteSerienbriefeVor } from "./serienbrief-render-service.ts";
import { routeSerienbriefe, serienbriefAusgabeweg } from "./serienbrief-routing-service.ts";
import { zusammenfassung, type SerienbriefStatusItem } from "./serienbrief-status-service.ts";

const vorlage: any = {
  vorlagenName: "Eichfälligkeit",
  betreff: "Erforderlicher Austausch Ihres {{zaehlerart}}",
  htmlInhalt: "<p>Ihr {{zaehlerart}} mit der Nummer {{zaehlernummer}} ist bis {{eichfaelligkeit}} zu tauschen.</p><p>Frist: {{frist}}</p>",
  freieFelder: [{ schluessel: "frist", anzeigename: "Frist", typ: "date", pflicht: true, sortierung: 1 }],
};
const rows: any[] = [
  { mitgliedId: 7, parzelleId: 11, empfaengerStatus: "hauptpaechter", werte: { parzelle: "12", zaehlerart: "Wasserzähler", zaehlernummer: "A-1", eichfaelligkeit: "31.12.2027" } },
  { mitgliedId: 7, parzelleId: 12, empfaengerStatus: "hauptpaechter", werte: { parzelle: "13", zaehlerart: "Wasserzähler", zaehlernummer: "A-2", eichfaelligkeit: "31.12.2027" } },
  { mitgliedId: null, parzelleId: 14, empfaengerStatus: "mehrdeutig", werte: { parzelle: "14", zaehlerart: "Wasserzähler", zaehlernummer: "A-3", eichfaelligkeit: "31.12.2027" } },
];
const members: any = new Map([[7, { id: 7, vorname: "Erika", name: "Muster", adresse: "Gartenweg 7", plz: "08056", ort: "Zwickau", anrede: "frau" }]]);

test("keeps the reference meter letter identical from preview through render job and routes both letters independently", () => {
  const freeValues = { frist: "2027-10-01" };
  const previewSubject = resolveSerienbriefText(vorlage.betreff, rows[0], vorlage, freeValues);
  const previewBody = resolveSerienbriefText(vorlage.htmlInhalt, rows[0], vorlage, freeValues, true);
  const result = bereiteSerienbriefeVor(rows.slice(0, 2), vorlage, freeValues, members, "2027-01-15");

  assert.equal(result.fehler.length, 0);
  assert.equal(result.erfolgreich.length, 2);
  assert.equal(result.erfolgreich[0].betreff, previewSubject.text);
  assert.equal(result.erfolgreich[0].htmlInhalt, previewBody.text);
  assert.equal(result.erfolgreich[0].briefanrede, briefanrede(members.get(7)));
  assert.match(result.erfolgreich[0].htmlInhalt, /01\.10\.2027/);
  assert.notEqual(result.erfolgreich[0].rowKey, result.erfolgreich[1].rowKey);

  const routed = routeSerienbriefe(result.erfolgreich.map((job, index) => ({ rowKey: job.rowKey, mitgliedId: job.mitgliedId, dokumentId: index + 1 })), new Map([[7, { email: "erika@example.org", emailInfo: true, emailRechnung: false }]]));
  assert.equal(routed.length, 2);
  assert.deepEqual(routed.map(item => item.ausgabeweg), ["mail", "mail"]);
  const status: SerienbriefStatusItem[] = routed.map(item => ({ rowKey: item.rowKey, mitgliedId: item.mitgliedId, status: "mailausgang", letzterErfolg: "mailausgang", ausgabeweg: "mail" }));
  assert.deepEqual(zusammenfassung(status), { ausgewaehlt: 2, pdf: 2, archiviert: 2, mail: 2, druck: 0, fehler: 0 });
});

test("preserves row failures and only falls back to print for address or consent decisions", () => {
  const result = bereiteSerienbriefeVor(rows, vorlage, { frist: "2027-10-01" }, members, "2027-01-15");
  assert.equal(result.erfolgreich.length, 2);
  assert.deepEqual(result.fehler.map(item => item.meldung), ["Mitglied nicht gefunden."]);
  assert.equal(serienbriefAusgabeweg("mail@example.org", false, false), "druck");
  assert.equal(serienbriefAusgabeweg(null, true, false), "druck");
  assert.equal(serienbriefAusgabeweg("ungueltig", true, false), "druck");
});
