# NB-2A-D1 – Audit Mitgliedsnummer in PDF- und Dokumentgeneratoren

Stand: 26.10.2026 · Audit-only. Dieser Block ändert keine Generatoren, Vorlagen, Edge Functions, Storage-Objekte, Migrationen oder Produktionsdaten.

## 1. Scope und verbindliche Regeln

Geprüft wurden die produktiven C#-Generatoren und -Signaturpfade, `SupabaseService`-Aufrufer, der zentrale Katalog samt lokaler Field-Specs, `kgv-generate-contract`, die hierfür einschlägigen Wartungsvertrags-Migrationen sowie die Dokumentationsquellen `PDF_FIELD_SPEC.md`, `PDF_TEMPLATE_CATALOG.md`, `PDF_LEGACY_RETENTION.md` und `PREPARED_CONTRACT_SIGNATURE_ARCHITECTURE.md`.

Die einzige kanonische Mitgliedsnummer ist die gespeicherte Spalte `mitglied.mitgliedsnummer`. Eine technische `mitglied.id`, eine Antrags-/Vertragsnummer oder eine Dateinamen-ID ist kein Ersatz. Für Vertragsdokumente wird das UI-Labelformat nicht pauschal übernommen: Jede Nummer muss als zum Dokumentzweck passende eigene Angabe erscheinen.

Klassifikation: **A** korrekt sichtbar genutzt, **B** im Pfad vorhanden aber nicht sichtbar, **C** im Datenmodell/Request nicht vorhanden, **D** technische ID sichtbar oder als Mitgliedsnummer ausgegeben, **E** für den Dokumentzweck nicht erforderlich, **F** fachliche Entscheidung nötig.

## 2. Gefundene Generatoren und Pfade

Es gibt sieben fachliche Dokumenttypen bzw. erzeugende Pfade sowie zwei ergänzende Signaturpfade:

| Dokumenttyp / Pfad | Erzeuger und Aufruf | Vorlage / Datenquelle | Sichtbare Personendaten und Nummern | Befund |
| --- | --- | --- | --- | --- |
| Mitgliedsantrag | `MitgliedsantragDokumentFactory`, aufgerufen über `SupabaseService`; alternativ `kgv-generate-contract` | Aktiver Katalogschlüssel `mitgliedsantrag`, lokale Referenzen v1–v4 | Antragsteller: Name, Geburtsdatum, Anschrift, Kontakt, Beiträge; zweite Partei/Vertretung gleichartig | **F/C:** Field-Spec v4 hat kein Mitgliedsnummer-Feld. Der Antrag verwendet keine technische ID sichtbar. Ob eine Nummer im Antragsdokument fachlich gewollt ist, ist offen; die Antragsnummer darf nicht damit verwechselt werden. |
| Mitgliedsvertrag | `MitgliedsvertragDokumentFactory` → `VereinsdokumentPdfBuilder`; Edge-Fallback `buildSimpleContract` | keine Katalogvorlage, PdfSharp/pdf-lib | Name, Geburtsdatum, Anschrift, Kontakt, Eintritt; aktuell `Mitglied-ID: <id>` | **D:** sichtbare technische ID in beiden Erzeugern. Kanonische Nummer ist im `MitgliedRecord` bzw. Edge-Member-Datensatz verfügbar. |
| Pachtvertrag | `PachtvertragDokumentFactory` → `PachtvertragTemplateFactory`; alternativ `kgv-generate-contract` | Katalogschlüssel `pachtvertrag`, lokale Referenzen v1–v4 | Hauptpächter und ggf. zweite Partei: Name, Anschrift, Kontakt, Geburtsdatum; v1–v3 enthalten Mitgliedsnummer-Felder, v4 nicht mehr | **D:** `PachtvertragTemplateFactory` und der Edge-v3-Pfad befüllen die Mitgliedsnummer-Felder mit `Id`, nicht `Mitgliedsnummer`. Im aktiven v4-Layout gibt es kein Mitgliedsnummer-Feld; daher ist eine Layout-/Vertragsentscheidung erforderlich, bevor v4 erweitert wird. |
| Wartungsvertrag | `kgv-generate-contract`, Prepared-Contract-Signaturen | Katalogschlüssel `wartungsvertrag`, Field-Spec v1 | Name, Mitgliedsnummer, Parzelle, Vertragsinhalt, Saison, Signaturen; Vertragspartei ist das Hauptmitglied der Zuordnung | **D:** das sichtbare Feld `mitgliedsnummer` erhält `String(member.id)`. Die kanonische Spalte ist im geladenen Member-Datensatz verfügbar. `wartungsvertrag_id` bleibt davon getrennte Vertragsnummer. |
| Wartungsvertragsverlängerungsmitteilung | `kgv-generate-contract` aus `kommunikationsvorgang.entwurf_daten` | Katalogschlüssel `wartungsvertrag-verlaengerung`, Field-Spec v1 | Name, Mitgliedsnummer, Parzelle, Vertragsinhalt und Verlängerungsentscheidung | **D:** die Edge Function gibt den Snapshot weiter, aber die zugrunde liegenden Wartungsvertrags-Migrationen schreiben aktuell `v_member.id` in `mitgliedsnummer`. Eine echte Verlängerung behält ihre `wartungsvertrag_id`; diese Nummer ersetzt die Mitgliedsnummer nicht. |
| Parzellenprotokoll | `ParzellenProtokollPdfBuilder`, aus `ParzellenProtokollePage`; ebenfalls im Edge-Generatorpfad vorhanden | programmatisch erzeugtes PdfSharp-/pdf-lib-Dokument | Mitglied/Pächter, Vorstände, Begleitperson, Garten, Zählerstände, Signaturen | **F:** Name-only-Darstellungen. Das Dokument ist Protokoll und Unterzeichnungsnachweis; ob für Pächter, Begleitperson oder Funktionspersonen eine Mitgliedsnummer erforderlich ist, braucht eine eigene fachliche Entscheidung. |
| Vereinsdokumente / bereitgestellte Vereinsunterlagen | `VereinsdokumentPdfBuilder`, Dokument-/Storage-Services, Versandfunktionen | hochgeladene, versionierte Vereins-PDFs; kein personenbezogener Generator | Dokumenttyp, Fassung und Bereitstellungsnachweis; keine Mitgliedsdaten in den Vorlagen | **E:** kein Dokumentgenerator für eine konkrete Mitgliedsdarstellung. Die technische `mitglied_id` dient nur Zuordnung, Versand und Nachweis. |
| Historische Dokumente | `HistoricalDocumentImportPage`, Registrierungs-/Archivservices | bestehende Uploads, Dokumenttyp-Katalog | vorhandene Dateien werden registriert/archiviert, nicht fachlich neu erzeugt | **E:** kein Generator; Altbestand gemäß `PDF_LEGACY_RETENTION.md` nicht nachträglich umschreiben. |
| Signaturergänzung bestehender PDFs | `SignedVertragsdokumentPdfBuilder`, `PdfSignatureRenderer`, `DocumentGenerationPipeline` | bereits vorhandene PDF, Signaturdaten | Metaseite mit Dokument, Status, Mitglied, Quelle, Signaturdatum | **D:** Fallback `Mitglied #<id>` ist sichtbar. Bei vorhandenem Namen wird nur der Name ausgegeben; die Frage, ob die Metaseite generell die Mitgliedsnummer tragen soll, ist fachlich zu entscheiden. |

`SimplePdfDocumentBuilder`, `PdfTemplateRenderer`, `PdfTemplateAssetCache`, `SupabasePdfTemplateProvider` und `DocumentGenerationPipeline` sind technische Renderer, Loader oder Signatur-Orchestrierung. Sie erfinden keine Personendarstellung; fachliche Befunde liegen in ihren jeweiligen Factories/Aufrufern.

## 3. Vorlagen, Katalog und Nummernarten

- Der zentrale Katalog trennt Vorlagen (`pdf_template_catalog`, privater Bucket `pdf-templates`) von fertigen Dokumenten. `PDF_FIELD_SPEC.md` beschreibt ausschließlich Feldtechnik, keine fachliche Entscheidung über Nummern.
- `mitgliedsantrag` v4 enthält kein Mitgliedsnummer-Feld. Die Anwendung erzeugt den Antrag aus Antragsteller-/Vertreterdaten; eine mögliche Antragsnummer ist keine Mitgliedsnummer.
- `pachtvertrag` v1–v3 enthalten `mitgliedsnummer` bzw. ein Feld der zweiten Partei; v4 enthält derzeit kein solches Feld. Die in einem Datenobjekt bereitgestellten, aber von v4 nicht platzierten Werte sind nicht sichtbar.
- `wartungsvertrag` v1 und `wartungsvertrag-verlaengerung` v1 besitzen beide ein sichtbares Feld `mitgliedsnummer`.
- Antrags-/Vertragsnummernbereiche (Mitgliedsantrag 10000–19999, Pachtvertrag 20000–29999, historische Kombiverträge 80000–89999, Wartungsvertrag 90000–99999) sind Vorgangsnummern. Sie wurden in keinem Befund als Mitgliedsnummer bewertet.

## 4. Sichtbare technische IDs und Ableitungen

| Fundstelle | Art | Bewertung |
| --- | --- | --- |
| `MitgliedsvertragDokumentFactory` | `Mitglied-ID: {member.Id}` | **D**, unmittelbar sichtbar. |
| `kgv-generate-contract` / `buildSimpleContract` | `Mitglied-ID: ${member.id}` | **D**, unmittelbar sichtbar. |
| `SignedVertragsdokumentPdfBuilder` | Fallback `Mitglied #{member.Id}` | **D**, sichtbar bei fehlendem Namen. |
| `kgv-generate-contract` / Pachtvertrag | `mitgliedsnummer: String(member.id)` und zweite Partei analog | **D**, falsche sichtbare Fachnummer, sobald ein Feld der v1–v3-Layouts gerendert wird. |
| `PachtvertragTemplateFactory` | `FormatMitgliedsnummer` gibt `member.Id` zurück | **D**, gleicher Fehler im lokalen Pfad. |
| Wartungsvertrag und Verlängerungs-Snapshots | `mitgliedsnummer` wird aus `member.id` bzw. `v_member.id` gesetzt | **D**, der aktuelle WV-/Verlängerungs-Templatepfad zeigt die falsche fachliche Nummer. |
| Dateinamen, Owner, `mitglied_id`, Signatur- und Storage-Referenzen | technische IDs | zulässig; nicht als normale Dokumentanzeige bewerten. |

Eine aktive Berechnung `Id + 1000`, `1000 + Id` oder Nutzung von `Fachnummer.Mitglied` wurde in den Dokumentgeneratorpfaden nicht gefunden. Der alte Helper `KGV.Core/Models/Fachnummer.cs` existiert, ist aber in keinem geprüften Erzeuger referenziert.

## 5. Fachliche Sonderfälle und offene Entscheidungen

1. **Mitgliedsantrag/Aufnahmebestätigung:** Es wurde kein eigenständiger Generator für eine Aufnahmebestätigung gefunden. Der Mitgliedsantrag hat kein Nummernfeld. Vor einer Erweiterung ist zu entscheiden, ob eine bei Antragserstellung bereits vorhandene Mitgliedsnummer rechtlich/fachlich auf den Antrag gehört; eine Antragsnummer ist davon strikt getrennt.
2. **Pachtvertrag v4:** Vor einer erneuten Nummernaufnahme muss entschieden werden, ob Hauptpächter und welche zweite Partei im Vertragslayout eine Mitgliedsnummer erhalten. Gesetzliche Vertreter sind nicht automatisch als Mitgliedsreferenz zu behandeln.
3. **Parzellenprotokoll:** Pächter, Vorstandsmitglieder und Begleitpersonen haben unterschiedliche Rollen. Keine pauschale Übernahme des UI-Labels oder der Mitgliedsnummer ohne Protokoll-/Unterschriftenregel.
4. **Signaturmetaseite:** Die technische Fallback-ID ist zu entfernen. Ob die Metaseite zusätzlich zur Namensangabe stets eine Mitgliedsnummer zeigen soll, ist gesondert zu entscheiden.
5. **Vereinsunterzeichner und Vertretungen:** Funktions- und Vertretungssnapshots sind rechtliche Rollenangaben; ihre technische `mitglied_id` bleibt intern. Eine Mitgliedsnummer ist dort nicht automatisch erforderlich.

## 6. Abgrenzung: Exporte

`ExportPdfBuilder`, `ExportPdfDocumentDefinition`, `CentralExportRequest`, `KGV.Web/services/export/*`, `supabase/functions/kgv-generate-export-pdf`, exportbezogene Template-Katalogeinträge und Jahresabschluss-/CSV-/XLSX-Ausgaben gehören ausdrücklich in den separaten **NB-2A-Exportblock**. Sie wurden hier weder fachlich analysiert noch geändert.

## 7. Empfohlene kleine Folgeblöcke

1. **NB-2A-D2 – Mitgliedsvertrag und Signaturmetaseite** (3–5 Dateien): sichtbare technische IDs durch kanonische Nummern ersetzen; Entscheidung zur regulären Nummernausgabe auf der Metaseite vorbereiten bzw. umsetzen.
2. **NB-2A-D3 – Wartungsvertrag und Verlängerung** (3–6 Dateien): Edge-Werte und Snapshot-Erzeugung auf `mitgliedsnummer` umstellen; Field-Specs bleiben unverändert, weil die Felder bereits bestehen. Keine Änderung der `wartungsvertrag_id`-Semantik.
3. **NB-2A-D4 – Pachtvertrag** (nach fachlicher Entscheidung): erst Nummernsemantik der Vertragsparteien und das v4-Layout festlegen, dann lokale und Edge-Datenaufbereitung sowie gegebenenfalls eine neue veröffentlichte Templateversion gemeinsam ändern.
4. **NB-2A-D5 – Antrag, Aufnahmebestätigung und Parzellenprotokoll** (nach fachlicher Entscheidung): klären, ob diese Rollen tatsächlich eine Mitgliedsnummer im Dokument benötigen. Kein automatischer UI-Label-Transfer.

Die D2- und D3-Befunde sind technische Fehler mit klarer kanonischer Datenquelle. D4 und D5 benötigen vor Umsetzung eine fachliche Entscheidung über die sichtbare Dokumentsemantik.

## 8. NB-2A-D2 – umgesetzt am 26.10.2026

Der lokale `MitgliedsvertragDokumentFactory` und der Edge-Fallback `buildSimpleContract` zeigen jetzt jeweils `Mitgliedsnummer: <mitgliedsnummer>` aus der gespeicherten kanonischen Spalte. Die sichtbare technische `mitglied.id` wurde aus beiden Mitgliedsvertragsgeneratoren entfernt; es gibt weder eine Berechnung noch einen ID-basierten Ersatz. Fehlt die gespeicherte Nummer, zeigen beide Pfade verständlich `nicht verfügbar`.

Auf der lokalen Signaturmetaseite bleibt ein vorhandener Vor- und Nachname unverändert die alleinige normale Mitgliedsangabe. Nur falls kein Name vorliegt, wird `Mgl.-Nr. <mitgliedsnummer>` verwendet; ohne gespeicherte Nummer lautet die neutrale Angabe `Mitglied`. Der frühere sichtbare Fallback `Mitglied #<id>` ist entfernt. Die Edge-Abfrage lädt den Member-Datensatz weiterhin vollständig, daher steht `mitgliedsnummer` ohne zusätzliche Abfrage und ohne N+1-Aufrufe bereit.

NB-2A-D3 bis D5 bleiben offen und sind durch diesen Block nicht verändert worden.

## 9. NB-2A-D3 – umgesetzt am 26.10.2026

Der Edge-Pfad für den Wartungsvertrag befüllt das vorhandene sichtbare Feld `mitgliedsnummer` jetzt aus dem bereits vollständig geladenen `mitglied`-Datensatz (`member.mitgliedsnummer`). `member.id` bleibt ausschließlich die interne Zuordnungs-ID; `wartungsvertrag_id` bleibt unverändert die eigenständige Vertragsidentität.

Für die Verlängerung ersetzt die neue, migrationssichere Quelldatei `20261026090000_nb2a_d3_wartungsvertrag_mitgliedsnummer.sql` die WV-6-Funktionsdefinition für künftig erzeugte Snapshots. Sie speichert `v_member.mitgliedsnummer` im Entwurf und verweigert die Verlängerung mit einer fachlichen Meldung, wenn keine gültige Mitgliedsnummer vorliegt. Die Migration ist **nur angelegt und nicht ausgeführt**; die historische WV-6-Migration wurde nicht verändert. Die Regel einer echten Verlängerung mit derselben `wartungsvertrag_id` bleibt unverändert erhalten.

Bereits gespeicherte Verlängerungsentwürfe werden nicht geändert und können weiterhin die frühere technische ID im gespeicherten JSON enthalten. Beim Rendern wird deren sichtbares Nummernfeld jedoch sicher aus dem bereits zum Vorgang geladenen kanonischen Mitgliedsdatensatz aufgelöst. Dies ist keine Heuristik und keine zusätzliche Abfrage; der Vorgang validiert bereits dieselbe `mitglied_id`. Weder der Wartungsvertrag noch die Verlängerung berechnen eine Mitgliedsnummer aus einer technischen ID.

NB-2A-D4 und D5 bleiben offen und sind durch diesen Block nicht verändert worden.

## 10. NB-2A-D4 – umgesetzt am 26.10.2026

Für den Pachtvertrag wurde die neue, separate Vorlage `pachtvertrag` v5 vorbereitet. Sie ergänzt die sichtbaren Vertragsdaten `Mgl.-Nr.` für Pächter/in 1 (`mitgliedsnummer`) und die zweite Vertragspartei (`zweite_partei_mitgliedsnummer`). Die v4-PDF und ihre Field-Spec bleiben unverändert. Die v5-Felder sind kein UI-Label und keine Signaturidentitäten.

Im lokalen Pfad `PachtvertragTemplateFactory` und im Edge-Pfad `kgv-generate-contract` stammen die Nummern ausschließlich aus der gespeicherten kanonischen Spalte `mitglied.mitgliedsnummer`. Die bisherigen ID-Befüllungen für die alten v1–v3-Felder wurden damit im weiter verwendeten Datenaufbereiter korrigiert; der Edge-Pfad unterstützt v3 weiterhin, ohne `member.id` oder `party2.id` sichtbar als Mitgliedsnummer zu verwenden. Pächter/in 1 und eine vorhandene zweite Vertragspartei brechen mit einer fachlichen Meldung ab, wenn die gespeicherte Nummer fehlt.

Ein gesetzlicher Vertreter erhält nur dann den optionalen Nummernwert der zweiten Vertragspartei, wenn er als Mitgliedsdatensatz vorhanden ist. Ohne verknüpften Mitgliedsdatensatz bleibt das Feld leer; es gibt keine Nummernheuristik und keinen technischen Ersatz. Vorstands- und Funktionsunterzeichner bleiben unverändert ohne Mitgliedsnummer.

Die neue Katalogmigration `20261026100000_nb2a_d4_publish_pachtvertrag_v5.sql` ist **nur angelegt und nicht ausgeführt**. Sie schützt die neuen Storage-Pfade und würde v5 erst nach manuellem Upload registrieren; weder Storage noch Katalog oder Produktionsdaten wurden geändert. NB-2A-D5 bleibt offen.
