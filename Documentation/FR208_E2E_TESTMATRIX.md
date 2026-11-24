# FR2-08 – gemeinsame authentifizierte HTTP-E2E-Phase (ZURÜCKGESTELLT)

Die folgenden Fälle werden erst nach Abschluss aller FR2-Arbeitspakete ausgeführt.
Für jede Variante wird ein synthetischer Benutzer in der DemoDB, sein effektiver Rechtebestand und der exakte Objektbezug vor jedem Schreibaufruf dokumentiert. Echtdaten nicht mutieren.

| Nr. | Akteur / Variante | Aktion | Erwartung / Persistenznachweis |
|---|---|---|---|
| 01 | Kein JWT / ungültiger JWT | Dokument-Download und Upload | HTTP 401, kein Metadaten-/Storage-Write |
| 02 | Nur `dokument.lesen` | Eigenes bzw. erlaubtes Dokument öffnen | HTTP 200, bytegleicher Hash, keine Metadatenmutation |
| 03 | Nur `dokument.lesen` | Fremdes Dokument öffnen | HTTP 403 oder 404, keine Dateiausgabe |
| 04 | Nur `dokument.verwalten` | Dokument einer erlaubten Mitglieds-ID hochladen | Erfolgreich, genau ein Datensatz und eine Datei, owner servergeprüft |
| 05 | Nur `dokument.verwalten` | Fremde Mitglieds-ID / Parzelle hochladen | HTTP 403, keine verwaiste Datei |
| 06 | Nur `dokument.verwalten` | Dokumentliste / Download ohne Leserecht | Keine implizite Freigabe |
| 07 | Nur `dokument.archivieren` | Unverknüpftes Dokument archivieren, korrektes Passwort | Erfolg nur bei erlaubtem Objektbezug; DB-Archivstatus inkl. Begründung |
| 08 | Nur `dokument.archivieren` | Verknüpftes Vertragsdokument archivieren | Verweigert, Hash/Version/Vertragsrelation unverändert |
| 09 | Archiv-Recht entzogen | Archivieren trotz bekanntem Passwort | HTTP 403, Status unverändert |
| 10 | Leserecht nach erster Ansicht entzogen | Erneuter Download / private Objekt-URL | Kein neuer Dateizugriff (bisherige bereits ausgegebene Bytes können nicht zurückgezogen werden) |
| 11 | Gültige Admin-Leseberechtigung `vereinsdaten.alle_lesen` | Mitglieder-/Vereinsunterlagen lesen, ohne Schreibrecht | Lesen erlaubt, Upload/Typ-Wirkungs-Änderung untersagt |
| 12 | Nur `dokumenttyp.verwalten` | Typ-Metadaten ändern ohne Wirkungen | Erfolg, Wirkungsliste identisch |
| 13 | Nur `dokumenttyp.verwalten` | Typ-Wirkungen ändern | HTTP 403, bisherige Nachweise identisch |
| 14 | Nur `dokumentwirkung.verwalten` | Wirkung von unbenutztem Typ ändern | Erfolg ohne Änderung von Titel/Aktiv/Sortierung |
| 15 | Wirkungsrecht, aber Typ historisch genutzt | Wirkung ändern | Verweigert; historische Vertragsnachweise unverändert |
| 16 | Nur `vorlage.verwalten` | Neue PDF-Vorlage als neue Datei anlegen | Erfolgreich ohne Überschreiben historischer Vorlage |
| 17 | Ohne `vorlage.verwalten` | Private PDF-/Serienbriefvorlage manipulieren | Verweigert |
| 18 | Ohne `vereinsunterlagen.verwalten` | Vereinsregelwerk-Upload und unregistrierter Cleanup | Verweigert |
| 19 | Mit `vereinsunterlagen.verwalten` | Neue Vereinsfassung registrieren | Einmalige neue Version, altes PDF unverändert, SHA stimmt |
| 20 | Zwei parallele Nutzer | gleiche Typ-/Wirkungs-Änderung | keine Lost Updates, deterministischer Versionsstand |
| 21 | Doppelte identische Altvertrag-Operation | MA+PV / PV Upload | Idempotenz, genau ein fachlich zugehöriger Nachweis |
| 22 | Unzulässige abweichende Retry-Operation | Altvertrag | Abweisung, kein zweites PDF/keine neue 80xxx-Nummer |
| 23 | Signaturworkflow | Prepared Vertrag/Anlage/Signatur und Snapshot | Erfolgreich, Bytes, Hash, Vertragsstatus und Signatur-Freigaben unverändert |
| 24 | Storage-Metadaten | direkter SELECT, Bucket-Pfad, signed URL, direkter Download | RLS unterscheidet Lesen/Schreiben; private URL-Bypass ausgeschlossen |

**Prüfprotokoll je Fall:** UTC-Zeit, Supabase-Projekt, Git-SHA, Migrationen, Testidentität (keine Tokens im Log), Fachrechte inkl. Entzüge, Zielobjekt, HTTP-Code, Response-Header, Dateihash, Vorher/Nachher-Datenstand, unabhängiges SQL-Resultat und Cleanup.
