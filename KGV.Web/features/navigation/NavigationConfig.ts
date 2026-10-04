export const navigationConfig = {
  groups: {
    home: {
      id: "home",
      label: "Startseite",
      standalone: true,
      target: { id: "start", label: "Startseite", detail: "Deine Vereinsübersicht für die Saison" },
    },
    imprint: {
      id: "imprint",
      label: "Impressum",
      standalone: true,
      target: { id: "impressum", label: "Impressum", detail: "Vereinsangaben und Kontakt" },
    },
    meters: {
      id: "meters",
      label: "Ablesen",
      target: { id: "ablesen", label: "Ablesen", detail: "Zählerstände und Freigaben" },
      items: {
        photoUploads: { id: "foto-uploads", label: "Foto-Uploads", detail: "Lokal vorgemerkte Ablesefotos prüfen", depth: 1 },
        meterChanges: { id: "zaehlerwechsel", label: "Zählerwechsel", detail: "Ausbau, Einbau und Historie", depth: 1 },
      },
    },
    parcels: {
      id: "parcels",
      label: "Parzellenverwaltung",
      standalone: true,
      target: { id: "parzellen", label: "Parzellenverwaltung", detail: "Parzellen, Belegungen und Zuordnungen" },
    },
    maintenance: {
      id: "maintenance",
      label: "Wartungsverträge",
      standalone: true,
      target: { id: "wartung", label: "Wartungsverträge", detail: "Verträge verwalten und zuordnen" },
    },
    workhours: {
      id: "workhours",
      label: "Arbeitsstunden freigeben",
      standalone: true,
      target: { id: "arbeitsstunden-pruefen", label: "Arbeitsstunden freigeben", detail: "Offene Arbeitsstunden prüfen" },
    },
    export: {
      id: "export",
      label: "Export",
      standalone: true,
      target: { id: "export", label: "Export", detail: "Listen und Auswertungen exportieren" },
    },
    administration: {
      id: "administration",
      label: "Verwaltung",
      items: {
        seasons: { id: "saisons", label: "Saisonverwaltung", detail: "Saisons anlegen und verwalten", depth: 1 },
        annualClosing: { id: "jahresabschluss", label: "Jahresabschluss", detail: "Jahresabschluss vorbereiten und bearbeiten", depth: 1 },
        club: { id: "verein", label: "Vereinskonfiguration", detail: "Vereinsdaten und Grundeinstellungen", depth: 1 },
      },
    },
    members: {
      id: "members",
      label: "Mitglieder suchen",
      target: { id: "mitglieder", label: "Mitglieder suchen", detail: "Mitglieder finden und bearbeiten" },
    },
  },
  memberItems: {
    newMasterData: { id: "mitglied-stammdaten", label: "Stammdaten", detail: "Neues Mitglied anlegen", depth: 2 },
    masterData: { id: "mitglied-stammdaten", label: "Stammdaten", detail: "Mitgliedsdaten bearbeiten", depth: 2 },
    documents: { id: "mitglied-dokumente", label: "Dokumente", detail: "Dokumente dieses Mitglieds", depth: 2 },
    protocols: { id: "mitglied-protokolle", label: "Protokolle", detail: "Protokolle dieses Mitglieds", depth: 2 },
    maintenance: { id: "mitglied-wartung", label: "Wartungsverträge", detail: "Zugeordnete Verträge", depth: 2 },
    secondaryMember: { id: "mitglied-nebenmitglied", label: "Nebenmitglied", detail: "Nebenmitglied verwalten", depth: 2 },
    gardens: { id: "mitglied-gaerten", label: "Gärten des Mitglieds", detail: "Zugeordnete Gärten", depth: 2 },
    administration: { id: "mitglied-admin", label: "Admin-Menü", detail: "Rollen und Rechte", depth: 2 },
    workHours: { id: "mitglied-arbeitsstunden", label: "Arbeitsstunden", detail: "Arbeitsstunden dieses Mitglieds", depth: 2 },
  },
} as const;
