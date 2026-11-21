// AP-02.1A: Typisierte Fachrechts-Codes; noch nicht mit Legacy-Permissions verschaltet.
// Automatisch aus 05_Fachrechtekatalog_2_0.csv; fachliche Quelle bleibt der AP-01-Katalog.
namespace KGV.Core.Security;

public static class FachrechteV2
{
    /// <summary>Mitglieder lesen</summary>
    public const string MitgliedLesen = "mitglied.lesen";
    /// <summary>Mitglieder bearbeiten</summary>
    public const string MitgliedBearbeiten = "mitglied.bearbeiten";
    /// <summary>Mitglieder anlegen</summary>
    public const string MitgliedAnlegen = "mitglied.anlegen";
    /// <summary>Mitgliedschaft verwalten</summary>
    public const string MitgliedschaftVerwalten = "mitgliedschaft.verwalten";
    /// <summary>Aufnahmeentscheidung treffen</summary>
    public const string MitgliedschaftEntscheiden = "mitgliedschaft.entscheiden";
    /// <summary>Gesetzliche Vertreter verwalten</summary>
    public const string GesetzlicheVertreterVerwalten = "gesetzliche_vertreter.verwalten";
    /// <summary>Parzellen lesen</summary>
    public const string ParzelleLesen = "parzelle.lesen";
    /// <summary>Parzellen bearbeiten</summary>
    public const string ParzelleBearbeiten = "parzelle.bearbeiten";
    /// <summary>Belegungen verwalten</summary>
    public const string BelegungVerwalten = "belegung.verwalten";
    /// <summary>Pachtverträge verwalten</summary>
    public const string PachtvertragVerwalten = "pachtvertrag.verwalten";
    /// <summary>Historische Verträge erfassen</summary>
    public const string HistorischeVertraegeErfassen = "historische_vertraege.erfassen";
    /// <summary>Begehungen und Protokolle verwalten</summary>
    public const string BegehungVerwalten = "begehung.verwalten";
    /// <summary>Wertermittlungen bearbeiten</summary>
    public const string WertermittlungBearbeiten = "wertermittlung.bearbeiten";
    /// <summary>Zählerdaten lesen</summary>
    public const string ZaehlerLesen = "zaehler.lesen";
    /// <summary>Ablesungen erfassen</summary>
    public const string AblesungErfassen = "ablesung.erfassen";
    /// <summary>Eingereichte Ablesungen freigeben</summary>
    public const string AblesungFreigeben = "ablesung.freigeben";
    /// <summary>Zählerwechsel verwalten</summary>
    public const string ZaehlerwechselVerwalten = "zaehlerwechsel.verwalten";
    /// <summary>RFID-Zuordnungen verwalten</summary>
    public const string RfidVerwalten = "rfid.verwalten";
    /// <summary>Ablesungseinstellungen verwalten</summary>
    public const string AblesungEinstellungen = "ablesung.einstellungen";
    /// <summary>Finanzdaten lesen</summary>
    public const string FinanzenLesen = "finanzen.lesen";
    /// <summary>Rechnungen und Vorauszahlungen bearbeiten</summary>
    public const string FinanzenRechnungen = "finanzen.rechnungen";
    /// <summary>Kosten- und Umlagearten verwalten</summary>
    public const string FinanzenKostenarten = "finanzen.kostenarten";
    /// <summary>Jahresabschluss vorbereiten</summary>
    public const string JahresabschlussVorbereiten = "jahresabschluss.vorbereiten";
    /// <summary>Jahresabschluss endgültig abschließen</summary>
    public const string JahresabschlussFinalisieren = "jahresabschluss.finalisieren";
    /// <summary>Korrekturabschluss durchführen</summary>
    public const string JahresabschlussKorrigieren = "jahresabschluss.korrigieren";
    /// <summary>Arbeitsstunden lesen</summary>
    public const string ArbeitsstundenLesen = "arbeitsstunden.lesen";
    /// <summary>Arbeitsstunden erfassen und bearbeiten</summary>
    public const string ArbeitsstundenBearbeiten = "arbeitsstunden.bearbeiten";
    /// <summary>Arbeitsstunden freigeben</summary>
    public const string ArbeitsstundenFreigeben = "arbeitsstunden.freigeben";
    /// <summary>Arbeitseinsätze verwalten</summary>
    public const string ArbeitseinsatzVerwalten = "arbeitseinsatz.verwalten";
    /// <summary>Dokumente lesen</summary>
    public const string DokumentLesen = "dokument.lesen";
    /// <summary>Dokumente verwalten</summary>
    public const string DokumentVerwalten = "dokument.verwalten";
    /// <summary>Dokumente archivieren</summary>
    public const string DokumentArchivieren = "dokument.archivieren";
    /// <summary>Dokumenttypen verwalten</summary>
    public const string DokumenttypVerwalten = "dokumenttyp.verwalten";
    /// <summary>Dokumentwirkungen verwalten</summary>
    public const string DokumentwirkungVerwalten = "dokumentwirkung.verwalten";
    /// <summary>PDF- und Serienbriefvorlagen verwalten</summary>
    public const string VorlageVerwalten = "vorlage.verwalten";
    /// <summary>Vereinsunterlagen verwalten</summary>
    public const string VereinsunterlagenVerwalten = "vereinsunterlagen.verwalten";
    /// <summary>Vorgänge lesen</summary>
    public const string VorgangLesen = "vorgang.lesen";
    /// <summary>Vorgänge bearbeiten</summary>
    public const string VorgangBearbeiten = "vorgang.bearbeiten";
    /// <summary>Vertragsunterlagen vorbereiten</summary>
    public const string VertragVorbereiten = "vertrag.vorbereiten";
    /// <summary>Vertragsversand durchführen</summary>
    public const string VertragVersenden = "vertrag.versenden";
    /// <summary>Druckausgabe verwalten</summary>
    public const string DruckVerwalten = "druck.verwalten";
    /// <summary>Serienbriefe erstellen</summary>
    public const string SerienbriefErstellen = "serienbrief.erstellen";
    /// <summary>Vorstandsvorgänge bearbeiten</summary>
    public const string VorstandsvorgangBearbeiten = "vorstandsvorgang.bearbeiten";
    /// <summary>Termine verwalten</summary>
    public const string TerminVerwalten = "termin.verwalten";
    /// <summary>Bekanntmachungen verwalten</summary>
    public const string BekanntmachungVerwalten = "bekanntmachung.verwalten";
    /// <summary>Vereinsfunktionen verwalten</summary>
    public const string VereinsfunktionVerwalten = "vereinsfunktion.verwalten";
    /// <summary>Vereinskonfiguration verwalten</summary>
    public const string VereinKonfiguration = "verein.konfiguration";
    /// <summary>Saisonverwaltung</summary>
    public const string SaisonVerwalten = "saison.verwalten";
    /// <summary>Auswertungen lesen</summary>
    public const string AuswertungLesen = "auswertung.lesen";
    /// <summary>Daten exportieren</summary>
    public const string ExportAusfuehren = "export.ausfuehren";
    /// <summary>Benutzerkonten lesen</summary>
    public const string BenutzerLesen = "benutzer.lesen";
    /// <summary>Benutzerkonten verwalten</summary>
    public const string BenutzerVerwalten = "benutzer.verwalten";
    /// <summary>Rollen und Berechtigungen lesen</summary>
    public const string BerechtigungenLesen = "berechtigungen.lesen";
    /// <summary>Rollen und Berechtigungen verwalten</summary>
    public const string BerechtigungenVerwalten = "berechtigungen.verwalten";
    /// <summary>Alle Vereinsdaten lesen</summary>
    public const string VereinsdatenAlleLesen = "vereinsdaten.alle_lesen";
}
