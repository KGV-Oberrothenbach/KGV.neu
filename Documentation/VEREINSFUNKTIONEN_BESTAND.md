# Vereinsfunktionen und Besetzung – Bestand

Stand: 21.10.2026. Reine Bestandsaufnahme; keine Daten, Migrationen oder Produktivobjekte wurden geändert.

## Ergebnis

Eine datengetriebene Personenbesetzung gibt es bereits, aber nur für das Impressum: impressum_funktion_slot verknüpft einen freien Funktions-/Slottext über mitglied_id mit einem Mitglied. Es existiert noch kein allgemeines Modell für Vereinsfunktionen, Amtszeiten, Vertretungen oder Zeichnungsberechtigungen.

Die Werte admin, vorstand und user in app_user.role sind technische Berechtigungsrollen. Sie steuern RLS, Navigation und administrative Funktionen. Sie dürfen nicht als Vorsitz, Kassierer oder sonstige Vereinsfunktion interpretiert werden.

## Vorhandene Datenquellen

| Quelle | Bestand |
| --- | --- |
| impressum_funktion_slot | id, eindeutiger slot_key, freier Text funktion, sort_order, optionaler FK mitglied_id sowie Zeitstempel |
| mitglied | Name und Kontaktstammdaten zur Auflösung einer Slot-Besetzung |
| app_user | Konto, Mitgliedsbezug und technische Rolle admin/vorstand/user |
| vereinskonfiguration | Vereinsstammdaten, jedoch keine Personen- oder Funktionsbesetzung |
| Vertrags-Requests/Drafts | Unterzeichnertext und konkrete Signatur-Captures als Momentaufnahme |

Das Basisschema enthält für impressum_funktion_slot FK, eindeutigen Slot und Reihenfolge, Update-Trigger sowie RLS. Eine allgemeine Funktionsdefinition oder Besetzungshistorie fehlt.

## Bereits datengetrieben: Impressum

SupabaseService.GetImpressumInfoAsync liest die Funktionsslots und Mitglieder und baut die Impressumsanzeige auf. Web liest Slots über das Imprint-Repository; MAUI zeigt die resultierenden Bereiche in ImpressumPage.

Die Einordnung ist noch textbasiert:

- Vorsitz wird aus slot_key/funktion mit Begriffen wie vorsitz erkannt; Stellvertretungen werden ausgeschlossen.
- Bauausschuss wird aus Varianten von Bauausschuss/Bau/Ausschuss erkannt.
- Weitere Slots erscheinen als Vorstand.

Das ist eine Anzeige-Heuristik, keine fachliche Autorisierung. Eine Slot-Besetzung vergibt weder App-Rechte noch eine Zeichnungsberechtigung.

## Feste oder fachlich relevante Stellen

| Stelle | Zustand | Einordnung |
| --- | --- | --- |
| IsVorstandsvorsitzSlot | Textsuche in Slot/Funktionsbezeichnung | Anzeige-Heuristik |
| IsBauausschussSlot | Textsuche in Slot/Funktionsbezeichnung | Anzeige-Heuristik |
| ImpressumKontaktItem.ShowAdresse | Adresse nur bei erkanntem Vorsitz | reine Anzeige |
| app_user.role und is_admin_or_vorstand | admin/vorstand als Zugriffskriterium | technische Rolle, kein Amt |
| PachtvertragDialogPage | freies Feld Vereinsunterzeichner | fachlich besetzungsbedürftig, nicht referenziert |
| Mitgliedsantrag/Pachtvertrag-Signatur | übergebene Vereins-/Vorstandssignatur | konkrete Aktion, keine Funktionsauflösung |

ImpressumInfo enthält zusätzlich Legacy-Fallback-Konstanten für Name, Register, E-Mail und Datenschutz-URL. Das sind Vereinsstammdaten, keine Funktionen.

## Signatur- und Dokumentpfade

Der Edge Renderer für Mitgliedsanträge kennt die Felder vereins_unterzeichner und unterschrift_verein. Die Infrastruktur validiert und speichert die vom Vorgang übergebene digitale Signatur.

Beim Pachtvertrag ist VereinsUnterzeichner beziehungsweise ClubSignatory ein frei bearbeitbarer Text in Request bzw. Entwurf. PachtvertragDialogPage erfasst ihn manuell; unterschrift_vorstand wird als Capture übergeben. Es gibt keine Zuordnung zu einem Impressumsslot, keine Prüfung einer Amtszeit und keine Regel für zulässige Unterzeichner.

Wartungsverträge sind ausschließlich Teil der Arbeitsstunden-/Befreiungslogik. Sie bilden keine Vereinsfunktion oder Vertretung ab.

## Komponenten

- Core: ImpressumFunktionSlotRecord, ImpressumInfo und ImpressumKontaktItem modellieren den Anzeigezustand. PachtvertragDokumentRequest und PachtvertragTemplateData transportieren freien Unterzeichnertext.
- Interfaces/Infrastructure: ISupabaseService und SupabaseService sind der zentrale Leseweg für das Impressum. Signaturmethoden erwarten Capture-Werte, keine Funktionsbesetzung.
- MAUI: ImpressumPage zeigt Vorstand/Bauausschuss dynamisch; PachtvertragDialogPage hat ein manuelles Unterzeichnerfeld.
- Web: Imprint-Repository/-Service lesen die Slots; app_user.role wird als Rollenbasis verwaltet.

## Bereits generisch / Umbaubedarf

Bereits generisch sind Slot-zu-Mitglied-FK, datengetriebene Reihenfolge und Auflösung von Personendaten. Umbau nötig wären die textbasierte Kategorisierung, die ausschließlich aufs Impressum zugeschnittene Modellierung sowie die fehlende Referenzierung einer zulässigen Unterzeichner-Besetzung.

## Schlankes Zielmodell

Ohne neue konkrete Funktionen zu erfinden, empfiehlt sich:

    vereinsfunktion
      fachlicher Schlüssel, Bezeichnung, Kategorie/Verwendungszweck
    vereinsfunktion_besetzung
      vereinsfunktion_id, mitglied_id, gueltig_ab, gueltig_bis, Reihenfolge
    vereinsfunktion_verwendung
      Zuordnung für bestehende Zwecke wie Impressum oder Vertragsunterzeichnung

Die technische Rolle app_user.role bleibt getrennt. Eine Besetzung kann im Impressum erscheinen, für eine Unterzeichnung zulässig sein oder beides, aber keine Wirkung soll aus einer anderen implizit folgen.

## Betroffene Dateien

- supabase/migrations/20260323093513_remote_schema.sql
- KGV.Core/Models/ImpressumFunktionSlotRecord.cs
- KGV.Core/Models/ImpressumInfo.cs
- KGV.Core/Models/ImpressumKontaktItem.cs
- KGV.Core/Models/PachtvertragDokumentRequest.cs
- KGV.Core/Interfaces/ISupabaseService.cs
- KGV.Infrastructure/Services/SupabaseService.cs
- KGV.Maui/Pages/ImpressumPage.cs
- KGV.Maui/Pages/PachtvertragDialogPage.cs
- KGV.Web/repositories/imprint/imprint-repository.ts
- KGV.Web/services/imprint/imprint-service.ts
- supabase/functions/kgv-generate-contract/index.ts
- KGV.Core/Utilities/MitgliedsantragDokumentFactory.cs
- KGV.Core/Utilities/PachtvertragDokumentFactory.cs

## Offene fachliche Entscheidungen

1. Welche bereits verwendeten Funktionsbezeichnungen erhalten fachliche Schlüssel?
2. Welche Funktionen sind reine Impressumsanzeige, welche zeichnungsberechtigt?
3. Sind parallele Besetzungen und Vertretungen zulässig?
4. Werden Amtszeiten historisch geführt?
5. Speichert ein Vertrag unterzeichnende Person, Funktion oder beide als Snapshot?
6. Wer pflegt Besetzungen, unabhängig von der technischen Rolle admin/vorstand?

## V2 – Zielmodell

V2 ergänzt das allgemeine Fundament, ohne einen bestehenden Nutzerpfad umzuschalten:

- vereinsfunktion: stabiler Schlüssel, veränderbare Bezeichnung und Aktivstatus.
- vereinsfunktion_besetzung: Mitgliedsbezug, Gültigkeitszeitraum und Reihenfolge; parallele Besetzungen bleiben technisch möglich.
- vereinsfunktion_verwendung: fachlicher Verwendungszweck einer Funktion. V2 übernimmt ausschließlich den bereits bestehenden Zweck impressum.

Die Migration übernimmt jeden vorhandenen Impressum-Slot über seinen slot_key als Funktion und dessen funktion als Bezeichnung. Bestehende Mitgliedsbezüge werden als zum Migrationszeitpunkt aktuelle Besetzung übernommen; das gewählte gueltig_ab ist ausdrücklich nur der Übernahmezeitpunkt, keine behauptete historische Amtszeit. Die bisherige Textheuristik für Vorsitz und Bauausschuss wird nicht in ein neues Fachgesetz überführt. Der produktive Impressumspfad bleibt unverändert impressum_funktion_slot.

Technische Rollen bleiben getrennt: app_user.role wird weder aus einer Besetzung erzeugt noch durch diese geändert. Ebenso entstehen keine Berechtigungen für Arbeitsstunden oder Unterzeichnungen.

Bewusst noch nicht umgestellt sind Impressum, Web, MAUI, Mitgliedsantrag, Pachtvertrag und sämtliche Signaturpfade. Für V3 offen bleiben insbesondere zulässige Funktionen/Bezeichnungen, Mehrfach- und Vertretungsregeln, belastbare Amtsbeginn-Daten sowie die fachlichen Regeln für eine Vertragsunterzeichnung.

RLS in V2 lässt Schreibzugriffe weiterhin nur für Admin/Vorstand zu. Normale angemeldete Nutzer dürfen ausschließlich aktive Funktionen, deren Verwendung impressum und aktuelle Besetzungen mit diesem Zweck lesen. Historische Besetzungen und künftige andere Verwendungszwecke bleiben nicht allgemein lesbar. Die Auflösung von Namen und Kontaktdaten bleibt in V2 beim bestehenden, separat gesicherten Mitglieds-/Impressumspfad; der spätere Impressumsleseweg muss diese Daten gezielt und sicher zusammenführen.
