# G12 – Archivierung

G12 verwendet `ISupabaseService.CreateDokumentAsync`, den bestehenden Uploadvertrag. Das finale G11-PDF wird byteidentisch als `application/pdf` mit unverändertem Dateinamen übergeben. Mitglied ist zwingend; eine eindeutige Parzelle wird optional mitgegeben.

Der Batch dedupliziert über `rowKey` und liefert pro Zeile Dokument-ID oder Fehler. G13 kann die `dokumentId` nutzen; Versand und Kommunikation bleiben unberührt.
