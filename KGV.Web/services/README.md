# Fachservices

Fachservices koordinieren Regeln, Berechtigungen und Abläufe einer Fachgruppe.
Sie bilden die Grenze zwischen UI und Datenzugriff: React-Komponenten rufen
Services auf; Services verwenden Repositories oder gemeinsame technische
Services. Sie verändern keinen React-State und erzeugen keine UI-Texte.

Gemeinsame Querschnittsgrenzen bleiben verbindlich: Auth/Session gehört zu G1,
Rollen und Permissions zu G13, Dokumentablage zu G10, Parzellenbeziehungen zu
G5 sowie der technische Browserzugriff zu G15.
