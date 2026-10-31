# G14 – Status und Fehler

G14 führt die G10–G13-Ergebnisse transient per `rowKey` zusammen. Statuswerte sind vorbereitet, PDF erstellt, archiviert, Mailausgang, Druckausgabe und Fehler. Bei Fehlern bleiben Phase und letzter erfolgreicher Zustand erhalten.

Die Abschlusszählung trennt Endwege Mail und Druck und zählt Fehler nicht doppelt. Eine neue Laufpersistenz ist nicht nötig: Dokument-ID und bestehender Kommunikationsvorgang liefern die dauerhafte Nachvollziehbarkeit. G15 bleibt außerhalb dieses Schritts.
