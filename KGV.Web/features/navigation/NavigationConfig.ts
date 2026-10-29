const item = (id: string, label: string, detail: string, depth = 1) => ({ id, label, detail, depth });

export const navigationConfig = {
  groups: {
    home: { id: "home", label: "Startseite", kind: "item", target: item("start", "Startseite", "Deine Vereinsübersicht für die Saison", 0) },
    imprint: { id: "imprint", label: "Impressum", kind: "item", target: item("impressum", "Impressum", "Vereinsangaben und Kontakt", 0) },
    processes: {
      id: "processes", label: "Vorgänge", kind: "group",
      items: {
        signatures: item("signaturen", "Signaturen", "Signaturen und Freigaben verwalten"),
        documents: item("dokumente", "Dokumente", "Dokumente und Vorgänge verwalten"),
        board: item("vorstand", "Vorstand", "Vorgänge des Vorstands"),
        mailOutbox: item("mailausgang", "Mailausgang", "Ausgehende Nachrichten"),
        sent: item("versendet", "Versendet", "Versendete Vorgänge"),
        printOutput: item("druckausgabe", "Druckausgabe", "Druckausgabe vorbereiten"),
        history: item("verlauf", "Verlauf", "Vorgangsverlauf"),
      },
    },
    meters: {
      id: "meters", label: "Ablesen", kind: "group",
      items: {
        capture: item("ablesen-erfassen", "Ablesung erfassen", "Zählerstände erfassen"),
        annualCapture: item("jae-erfassen", "JAE erfassen", "Jahresendablesung erfassen"),
        meterChanges: item("zaehlerwechsel", "Zählerwechsel", "Ausbau, Einbau und Historie"),
        rfidSetup: item("rfid-einrichten", "RFID einrichten", "RFID für Parzellen einrichten"),
        approve: item("ablesungen-freigeben", "Ablesungen freigeben", "Eingereichte Ablesungen prüfen"),
      },
    },
    administration: {
      id: "administration", label: "Verwaltung", kind: "group",
      items: {
        parcels: item("parzellen", "Parzellenverwaltung", "Parzellen, Belegungen und Zuordnungen"),
        maintenance: item("wartung", "Wartungsverträge", "Verträge verwalten und zuordnen"),
        seasons: item("saisons", "Saisonverwaltung", "Saisons anlegen und verwalten"),
        annualClosing: item("jahresabschluss", "Jahresabschluss", "Jahresabschluss vorbereiten und bearbeiten"),
        club: item("verein", "Vereinskonfiguration", "Vereinsdaten und Grundeinstellungen"),
        clubDocuments: item("vereinsunterlagen", "Vereinsunterlagen", "Vereinsunterlagen verwalten"),
        clubFunctions: item("vereinsfunktionen", "Vereinsfunktionen", "Vereinsfunktionen verwalten"),
        documentTypes: item("dokumenttypen", "Dokumenttypen", "Dokumenttypen verwalten"),
        backfillContracts: item("vertraege-nacherfassen", "Verträge nacherfassen", "Bestehende Verträge nacherfassen"),
      },
    },
    workhours: { id: "workhours", label: "Arbeitsstunden freigeben", kind: "item", target: item("arbeitsstunden-pruefen", "Arbeitsstunden freigeben", "Offene Arbeitsstunden prüfen", 0) },
    export: { id: "export", label: "Export", kind: "item", target: item("export", "Export", "Listen und Auswertungen exportieren", 0) },
    members: { id: "members", label: "Mitgliedersuche", kind: "item", target: item("mitglieder", "Mitgliedersuche", "Mitglieder finden und bearbeiten", 0) },
  },
  memberItems: {
    newMasterData: item("mitglied-stammdaten", "Stammdaten", "Neues Mitglied anlegen", 2),
    masterData: item("mitglied-stammdaten", "Stammdaten", "Mitgliedsdaten bearbeiten", 2),
    documents: item("mitglied-dokumente", "Dokumente", "Dokumente dieses Mitglieds", 2),
    protocols: item("mitglied-protokolle", "Protokolle", "Protokolle dieses Mitglieds", 2),
    maintenance: item("mitglied-wartung", "Wartungsverträge", "Zugeordnete Verträge", 2),
    secondaryMember: item("mitglied-nebenmitglied", "Nebenmitglied", "Nebenmitglied verwalten", 2),
    gardens: item("mitglied-gaerten", "Gärten des Mitglieds", "Zugeordnete Gärten", 2),
    administration: item("mitglied-admin", "Admin-Menü", "Rollen und Rechte", 2),
    workHours: item("mitglied-arbeitsstunden", "Arbeitsstunden", "Arbeitsstunden dieses Mitglieds", 2),
  },
} as const;
