# G13 – Ausgaberouting

G13 verwendet die bestehende Freigaberegel aus `public.vertragsversand_mailtext_snapshot()` in `20261023140000_vd3_contract_dispatch.sql`: gültige E-Mail plus Info- oder Rechnungseinwilligung führt zum Mailweg, sonst zum Druckweg. Der Standard-Mailtext einschließlich Datenschutzhinweis stammt aus derselben Funktion und wird nicht neu formuliert.

Das Routing dedupliziert im transienten Vorgang über Row-Key und Dokument-ID. Es erzeugt weder PDFs noch Versand oder Druck; G14 kann die Ergebnisse in der UI darstellen.
