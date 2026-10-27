import assert from "node:assert/strict";
import test from "node:test";
import { buildIcsCalendar, icsFileName } from "./ics-calendar-service.ts";

test("ICS-Dateiname verwendet ausschließlich Terminbezeichnung und Datum", () => {
  const fileName = icsFileName({ title: "Vorstandssitzung", date: "2026-11-12" });

  assert.equal(fileName, "Vorstandssitzung_2026-11-12.ics");
  assert.doesNotMatch(fileName, /termin-123|appointment\.id|termin\.id/i);
});

test("ICS-Dateiname sanitiziert unzulässige Zeichen und Zeilenumbrüche", () => {
  const fileName = icsFileName({ title: "  Vorstand/\r\nSitzung:*?  ", date: "2026-11-12" });

  assert.equal(fileName, "Vorstand_Sitzung_2026-11-12.ics");
  assert.doesNotMatch(fileName, /[\\/:*?"<>|\r\n]/);
});

test("ICS-Dateiname verwendet für leere Titel den neutralen Fallback", () => {
  assert.equal(icsFileName({ title: " \r\n ", date: "2026-11-12" }), "Termin_2026-11-12.ics");
});

test("technische ICS-UID bleibt im Kalenderinhalt erhalten", () => {
  const content = buildIcsCalendar({ uid: "termin-123@kgv-oberrothenbach", title: "Vorstandssitzung", date: "2026-11-12" }, new Date("2026-11-01T00:00:00Z"));

  assert.match(content, /UID:termin-123@kgv-oberrothenbach/);
});
