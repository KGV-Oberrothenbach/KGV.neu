# G11 – PDF-Rendering

G11 verwendet `IPdfTemplateProvider` und den bestehenden `PdfTemplateRenderer` mit `briefbogen-allgemein`. Der Adapter erwartet Empfänger-, Betreff-, Anrede- und Textfelder, löst keine Fachdaten neu auf und liefert pro Renderjob Bytes oder einen Fehler.

Der Editor-HTML-Subset wird als sicherer Textfluss gerendert; Absätze und Umbrüche bleiben erhalten, unbekannte Tags werden entfernt. Dateinamen verwenden den vorbereiteten Kontext; Dubletten erhalten einen numerischen Suffix. PDF-Ergebnisse werden nicht archiviert oder versendet; G12 übernimmt dies.
