# G9 – Vorlagenverwaltung

Die transaktionale RPC `speichere_serienbrief_vorlage` speichert Vorlage, verwendete Exportfeldschlüssel und Freifelder atomar. Freifeldwerte und Serienbriefläufe werden nicht persistiert. Die Servicevalidierung erlaubt nur bekannte Export- oder definierte Freifeldplatzhalter und ermittelt ausschließlich tatsächlich verwendete Exportfelder. Kompatibilität verwendet nur stabile Feldschlüssel; G10 bleibt unberührt.
