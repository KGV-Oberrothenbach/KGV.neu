# Jahresabschluss – Phase 5 Freigabe

**Stand:** 28. September 2026  
**Automatischer Build:** Android-MAUI fehlerfrei  
**Bestehender Testbestand:** erfolgreich ausgeführt

## Vor der ersten produktiven Berechnung

- Test-Saison verwenden, nie die produktive Saison.
- Eine vollständig verteilte Rechnung je Umlageart erfassen.
- Fehlende Ablesung, fehlendes Foto und nicht freigegebene Ablesung einzeln prüfen: Der Prüfbericht muss jeweils blockieren.
- Rückläufigen Zählerstand prüfen: Der Prüfbericht muss blockieren.
- Auffälligen Verbrauch prüfen: Der Prüfbericht muss warnen, aber nicht stillschweigend verändern.
- Pächterwechsel mit den tatsächlichen Wechselablesungen prüfen.
- U2 mit verpachteter Fläche, Leerstand und Vereinsgarten gegen einen Handrechenweg vergleichen.
- Prüfen, dass eine unvollständig verteilte Rechnung die Berechnung blockiert.
- Berechnung zweimal ausführen: Nur der Entwurf darf ersetzt werden; die Summe muss identisch bleiben.
- Abschlussdialog mit Testdaten durchführen und anschließend direkt in Supabase prüfen: Abschluss und Ergebnispositionen dürfen nicht mehr änderbar sein.

## Noch kein Produktivabschluss

Der erste produktive Abschluss erfolgt erst nach diesem Test mit anonymisierten Echtdaten und der Gegenprüfung durch den Vorstand. Verbrauchsabrechnungen mit Zähler-/Pächterwechseln werden dabei besonders geprüft, weil sie die strengsten Nachweisanforderungen haben.
