# Serienbrief G4 – Vorlageneditor

Der Editor liegt getrennt unter `KGV.Web/features/serienbrief/` und besteht aus
`SerienbriefEditor`, `SerienbriefBetreff`, `SerienbriefAnrede` und
`SerienbriefTextEditor`. Es wurde keine npm-Abhängigkeit ergänzt.

Der lokale, typisierte Zustand enthält Vorlagenname, Betreff, den festen
Anredemodus `briefanrede` und HTML-Inhalt. Der Editor nutzt ein begrenztes
`contentEditable`-Grundgerüst und erzeugt HTML mit Absatz-/Zeilenumbruch sowie
Fett, Kursiv und Aufzählungen. Eine Persistenz wird nicht aufgerufen.

Briefkopf, Absender, Empfängeranschrift, Fußzeile und Seitenlayout bleiben im
Grundlayout `briefbogen-allgemein/v2`. G5 ergänzt später erst den Feldkatalog
und das Einfügen von Exportfeldern.
