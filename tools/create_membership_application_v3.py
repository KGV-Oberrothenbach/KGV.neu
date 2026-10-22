from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import mm
from reportlab.pdfbase.pdfmetrics import stringWidth
from reportlab.pdfgen.canvas import Canvas
from reportlab.platypus import Paragraph


OUT = Path("KGV.Core/Templates/mitgliedsantrag/v3/template.pdf")
W, H = A4
GREEN = colors.HexColor("#174f2c")
PALE_GREEN = colors.HexColor("#e7f0ea")
LABEL = colors.HexColor("#617086")
FIELD = colors.HexColor("#c8d4f6")


def section(c, no, title, y):
    c.setFillColor(PALE_GREEN)
    c.roundRect(14 * mm, y - 8 * mm, 182 * mm, 8 * mm, 3 * mm, fill=1, stroke=0)
    c.setFillColor(GREEN)
    c.setFont("Helvetica-Bold", 8)
    c.drawString(17 * mm, y - 5.3 * mm, no)
    c.setFont("Helvetica-Bold", 11)
    c.drawString(27 * mm, y - 5.6 * mm, title)
    return y - 11 * mm


def label(c, text, x, y):
    c.setFillColor(LABEL)
    c.setFont("Helvetica-Bold", 6.5)
    c.drawString(x, y, text.upper())


def line(c, x, y, width):
    c.setStrokeColor(colors.HexColor("#9caab8"))
    c.setLineWidth(.45)
    c.line(x, y, x + width, y)


def field(c, text, x, y, width, h=18):
    label(c, text, x, y + h - 7)
    c.setFillColor(FIELD)
    c.rect(x, y, width, h - 8, fill=1, stroke=0)


def text(c, value, x, y, width, font=8.6, leading=11):
    style = ParagraphStyle("body", fontName="Helvetica", fontSize=font, leading=leading, textColor=colors.HexColor("#172233"))
    p = Paragraph(value, style)
    _, ph = p.wrap(width, 500)
    p.drawOn(c, x, y - ph)
    return ph


def header(c, subtitle, page):
    logo = Path("Logo.png")
    if logo.exists():
        c.drawImage(str(logo), 157 * mm, H - 37 * mm, width=19 * mm, height=19 * mm, preserveAspectRatio=True, mask="auto")
    c.setFillColor(GREEN)
    c.setFont("Helvetica-Bold", 17)
    c.drawString(14 * mm, H - 18 * mm, "Kleingartenverein Oberrothenbach e.V.")
    c.setFont("Helvetica", 7)
    c.drawString(14 * mm, H - 23 * mm, "kgvoberrothenbach@gmx.de")
    c.setFont("Helvetica-Bold", 18)
    c.drawString(14 * mm, H - 33 * mm, "MITGLIEDSANTRAG" if page == 1 else "ERGÄNZUNGEN")
    c.setFillColor(colors.HexColor("#172233"))
    c.setFont("Helvetica", 9)
    c.drawString(14 * mm, H - 39 * mm, subtitle)
    c.setStrokeColor(GREEN)
    c.setLineWidth(1.2)
    c.line(14 * mm, H - 46 * mm, 196 * mm, H - 46 * mm)
    c.setStrokeColor(colors.HexColor("#9caab8"))
    c.setLineWidth(.35)
    c.line(14 * mm, 15 * mm, 196 * mm, 15 * mm)
    c.setFillColor(colors.HexColor("#65716b"))
    c.setFont("Helvetica", 6.4)
    c.drawString(14 * mm, 10 * mm, "Kleingartenverein Oberrothenbach e.V. · Amtsgericht Chemnitz VR 70502")
    c.drawRightString(196 * mm, 10 * mm, f"Seite {page} von 2")


def draw_page_one(c):
    header(c, "Für jede Person ist ein eigener Antrag einzureichen. Dieser Antrag begründet kein Pachtverhältnis.", 1)
    x = 14 * mm
    y = H - 58 * mm
    y = section(c, "01", "ANGABEN ZUR ANTRAGSTELLENDEN PERSON", y)
    field(c, "Name", x, y - 8, 44 * mm); field(c, "Vorname", x + 46 * mm, y - 8, 44 * mm)
    field(c, "Geburtsdatum", x + 92 * mm, y - 8, 44 * mm); field(c, "Telefon (freiwillig)", x + 138 * mm, y - 8, 44 * mm)
    field(c, "E-Mail (freiwillig)", x, y - 29, 88 * mm); field(c, "Anschrift", x + 90 * mm, y - 29, 92 * mm)
    y -= 38
    y = section(c, "02", "BEANTRAGTE MITGLIEDSCHAFT", y)
    c.setFillColor(colors.HexColor("#172233")); c.setFont("Helvetica", 8.5)
    c.drawString(x, y - 1, "Bitte genau eine Mitgliedschaftsart auswählen:")
    for yy, t in [(y - 11, "Aktiv (bei bestehendem oder gleichzeitig wirksam werdendem Pachtvertrag)"),
                  (y - 20, "Passiv (ohne eigenen Pachtvertrag)"),
                  (y - 29, "Nebenmitglied (z. B. Ehe- oder Lebenspartner eines aktiven Mitglieds)")]:
        c.rect(x + 1, yy - 2.5, 7, 7, fill=0, stroke=1); c.drawString(x + 12, yy, t)
    c.setFont("Helvetica", 7.7)
    c.drawString(x, y - 39, "Ehrenmitgliedschaft entsteht ausschließlich durch Ernennung durch die Mitgliederversammlung.")
    c.setFont("Helvetica", 8.3)
    c.drawString(x, y - 49, "Bei Nebenmitgliedschaft - aktives Bezugsmitglied:"); line(c, x + 91 * mm, y - 51, 82 * mm)
    c.drawString(x, y - 59, "Parzelle (falls bekannt):"); line(c, x + 43 * mm, y - 61, 58 * mm)
    c.setFont("Helvetica", 7.5)
    text(c, "Die Zuordnung als aktives oder passives Mitglied richtet sich nach der tatsächlichen Pächterstellung. Über die Aufnahme entscheidet der Vorstand.", x, y - 66, 182 * mm, 7.5, 9)
    y -= 82
    y = section(c, "03", "MITGLIEDSCHAFT UND PFLICHTEN", y)
    h = text(c, "Ich beantrage die Aufnahme in den Kleingartenverein Oberrothenbach e. V. und erkenne die Satzung sowie die für meine Mitgliedschaft wirksam geltenden Vereinsordnungen an. Die Mitgliedschaft beginnt mit der Aufnahmebestätigung des Vorstands, sofern darin kein späterer Zeitpunkt genannt wird.<br/><br/>Aktive, passive und Ehrenmitglieder sind ab Vollendung des 16. Lebensjahres stimmberechtigt. Nebenmitglieder haben kein eigenes Stimmrecht und schulden keine eigenen Pflichtarbeitsstunden. Beiträge, Gebühren und gegebenenfalls Pflichtarbeitsleistungen richten sich nach der Satzung und den wirksam beschlossenen Ordnungen. Die Mitgliedschaft allein begründet keinen Anspruch auf eine Parzelle.", x, y, 182 * mm, 8.6, 11)
    y -= h + 6
    y = section(c, "04", "DATENSCHUTZ UND KONTAKT", y)
    h = text(c, "Der Verein verarbeitet die für Prüfung und Verwaltung der Mitgliedschaft erforderlichen personenbezogenen Daten nach geltendem Datenschutzrecht. Die Datenschutzinformationen gemäß Art. 13 DSGVO sind vor oder bei Erhebung bereitzustellen. Freiwillige Kontaktdaten können für die Zukunft zurückgezogen werden, soweit keine andere Rechtsgrundlage besteht.", x, y, 182 * mm, 8.2, 10)
    y -= h + 5
    c.setFont("Helvetica", 8.3); c.setFillColor(colors.HexColor("#172233"))
    c.drawString(x, y, "Bevorzugter Kontaktweg (freiwillig):")
    for xx, lbl in [(x + 58 * mm, "Post"), (x + 78 * mm, "E-Mail"), (x + 101 * mm, "Telefon")]:
        c.rect(xx, y - 2.5, 7, 7, fill=0, stroke=1); c.drawString(xx + 11, y, lbl)
    y -= 10
    y = section(c, "05", "ERKLÄRUNGEN UND UNTERSCHRIFTEN", y)
    h = text(c, "Ich bestätige die Richtigkeit meiner Angaben und dass mir die Satzung und die einschlägigen Vereinsordnungen zugänglich gemacht wurden.", x, y, 182 * mm, 8.4, 10)
    y -= h + 5
    c.setFont("Helvetica", 8.2); c.drawString(x, y, "Ort, Datum:"); line(c, x + 24 * mm, y - 2, 70 * mm)
    c.drawString(x, y - 12, "Unterschrift:"); line(c, x + 24 * mm, y - 14, 70 * mm)
    c.setFont("Helvetica", 6.5); c.setFillColor(LABEL)
    c.drawString(x + 24 * mm, y - 20, "Antragsteller/in")


def draw_page_two(c):
    header(c, "Zustimmung bei Minderjährigen und Entscheidung des Vorstands", 2)
    x = 14 * mm
    y = H - 58 * mm
    y = section(c, "06", "BEI MINDERJÄHRIGEN - ZUSTIMMUNG DER GESETZLICHEN VERTRETUNG", y)
    c.setFillColor(colors.HexColor("#172233")); c.setFont("Helvetica", 9)
    c.drawString(x, y, "Name(n):"); line(c, x + 22 * mm, y - 2, 88 * mm)
    c.drawString(x, y - 14, "Ort, Datum:"); line(c, x + 25 * mm, y - 16, 88 * mm)
    c.drawString(x, y - 28, "Unterschrift(en):"); line(c, x + 31 * mm, y - 30, 100 * mm)
    y -= 43
    y = section(c, "07", "ENTSCHEIDUNG DES VORSTANDS (VOM VEREIN AUSZUFÜLLEN)", y)
    c.setFont("Helvetica", 9); c.setFillColor(colors.HexColor("#172233"))
    c.rect(x + 1, y - 3, 7, 7, fill=0, stroke=1); c.drawString(x + 12, y, "Aufnahme bestätigt")
    c.rect(x + 59 * mm, y - 3, 7, 7, fill=0, stroke=1); c.drawString(x + 62 * mm, y, "Aufnahme abgelehnt")
    for offset, title, start in [(15, "Mitgliedschaft ab:", 40), (29, "Mitgliedsnummer:", 38), (43, "Mitgliedsart bei Aufnahme:", 56), (57, "Datum / dokumentierter Vorstandsbeschluss:", 78), (71, "Mitteilung an antragstellende Person am:", 75)]:
        c.drawString(x, y - offset, title)
        if offset == 43:
            xx = x + start * mm
            for lbl in ("Aktiv", "Passiv", "Nebenmitglied"):
                c.rect(xx, y - offset - 3, 7, 7, fill=0, stroke=1); c.drawString(xx + 11, y - offset, lbl); xx += stringWidth(lbl, "Helvetica", 9) + 23
        else:
            line(c, x + start * mm, y - offset - 2, 105 * mm)
    c.setFillColor(colors.HexColor("#172233"))
    text(c, "Hinweis: Für rechtsgeschäftliche Erklärungen des Vereins gilt die gemeinsame Vertretung durch zwei Vorstandsmitglieder gemäß Satzung.", x, y - 87, 182 * mm, 8.2, 10)
    c.setFillColor(LABEL); c.setFont("Helvetica", 7)
    c.drawString(x, y - 107, "Arbeitsfassung 21.10.2026 - vor Verwendung mit beschlossenen Ordnungen und Datenschutzinformationen abgleichen.")


def draw_membership_application_page(c):
    """Page 1: application, legal-representative consent, and board decision."""
    header(c, "Für jede Person ist ein eigener Antrag einzureichen. Dieser Antrag begründet kein Pachtverhältnis.", 1)
    x, y = 14 * mm, H - 58 * mm
    y = section(c, "01", "ANGABEN ZUR ANTRAGSTELLENDEN PERSON", y)
    field(c, "Name", x, y - 8, 44 * mm); field(c, "Vorname", x + 46 * mm, y - 8, 44 * mm)
    field(c, "Geburtsdatum", x + 92 * mm, y - 8, 44 * mm); field(c, "Telefon (freiwillig)", x + 138 * mm, y - 8, 44 * mm)
    field(c, "E-Mail (freiwillig)", x, y - 29, 88 * mm); field(c, "Anschrift", x + 90 * mm, y - 29, 92 * mm)
    y -= 38
    y = section(c, "02", "BEANTRAGTE MITGLIEDSCHAFT", y)
    c.setFillColor(colors.HexColor("#172233")); c.setFont("Helvetica", 8.2)
    c.drawString(x, y, "Bitte genau eine Mitgliedschaftsart auswählen:")
    for yy, item in [(y - 9, "Aktiv (bei bestehendem oder gleichzeitig wirksam werdendem Pachtvertrag)"), (y - 17, "Passiv (ohne eigenen Pachtvertrag)"), (y - 25, "Nebenmitglied (z. B. Ehe- oder Lebenspartner eines aktiven Mitglieds)")]:
        c.rect(x + 1, yy - 2.5, 6, 6, fill=0, stroke=1); c.drawString(x + 11, yy, item)
    c.setFont("Helvetica", 7.2); c.drawString(x, y - 33, "Ehrenmitgliedschaft entsteht ausschließlich durch Ernennung durch die Mitgliederversammlung.")
    c.setFont("Helvetica", 7.9); c.drawString(x, y - 42, "Bei Nebenmitgliedschaft - aktives Bezugsmitglied:"); line(c, x + 83 * mm, y - 44, 90 * mm)
    c.drawString(x, y - 51, "Parzelle (falls bekannt):"); line(c, x + 39 * mm, y - 53, 62 * mm)
    c.setFont("Helvetica", 7.1); c.drawString(x, y - 61, "Die Zuordnung als aktives oder passives Mitglied richtet sich nach der tatsächlichen Pächterstellung. Über die Aufnahme entscheidet der Vorstand.")
    y -= 69
    y = section(c, "03", "MITGLIEDSCHAFT UND PFLICHTEN", y)
    h = text(c, "Ich beantrage die Aufnahme in den Kleingartenverein Oberrothenbach e. V. und erkenne die Satzung sowie die für meine Mitgliedschaft wirksam geltenden Vereinsordnungen an. Die Mitgliedschaft beginnt mit der Aufnahmebestätigung des Vorstands, sofern darin kein späterer Zeitpunkt genannt wird.<br/><br/>Aktive, passive und Ehrenmitglieder sind ab Vollendung des 16. Lebensjahres stimmberechtigt. Nebenmitglieder haben kein eigenes Stimmrecht und schulden keine eigenen Pflichtarbeitsstunden. Beiträge, Gebühren und gegebenenfalls Pflichtarbeitsleistungen richten sich nach der Satzung und den wirksam beschlossenen Ordnungen. Die Mitgliedschaft allein begründet keinen Anspruch auf eine Parzelle.", x, y, 182 * mm, 7.4, 8.8)
    y -= h + 4
    y = section(c, "04", "DATENSCHUTZ UND KONTAKT", y)
    h = text(c, "Der Verein verarbeitet die für Prüfung und Verwaltung der Mitgliedschaft erforderlichen personenbezogenen Daten nach geltendem Datenschutzrecht. Die vollständige Datenschutzinformation nach Art. 13 DSGVO befindet sich auf Seite 2.", x, y, 182 * mm, 7.3, 8.6)
    y -= h + 8
    c.setFont("Helvetica", 7.8); c.drawString(x, y, "Bevorzugter Kontaktweg (freiwillig):")
    for xx, lbl in [(x + 58 * mm, "Post"), (x + 78 * mm, "E-Mail"), (x + 101 * mm, "Telefon")]:
        c.rect(xx, y - 2.5, 6, 6, fill=0, stroke=1); c.drawString(xx + 10, y, lbl)
    y -= 9
    y = section(c, "05", "ERKLÄRUNG UND UNTERSCHRIFT", y)
    h = text(c, "Ich bestätige die Richtigkeit meiner Angaben und dass mir die Satzung und die einschlägigen Vereinsordnungen zugänglich gemacht wurden.", x, y, 182 * mm, 7.5, 8.8)
    y -= h + 7
    c.setFont("Helvetica", 7.8); c.drawString(x, y, "Ort, Datum:"); line(c, x + 22 * mm, y - 2, 65 * mm)
    c.drawString(x, y - 10, "Unterschrift:"); line(c, x + 22 * mm, y - 12, 65 * mm)
    y -= 20
    y = section(c, "06", "BEI MINDERJÄHRIGEN - ZUSTIMMUNG DER GESETZLICHEN VERTRETUNG", y)
    c.setFont("Helvetica", 7.8); c.drawString(x, y, "Name(n):"); line(c, x + 18 * mm, y - 2, 65 * mm)
    c.drawString(x, y - 10, "Ort, Datum:"); line(c, x + 22 * mm, y - 12, 65 * mm)
    c.drawString(x, y - 20, "Unterschrift(en):"); line(c, x + 27 * mm, y - 22, 75 * mm)
    y -= 30
    y = section(c, "07", "ENTSCHEIDUNG DES VORSTANDS (VOM VEREIN AUSZUFÜLLEN)", y)
    c.setFont("Helvetica", 7.6)
    c.rect(x + 1, y - 3, 6, 6, fill=0, stroke=1); c.drawString(x + 10, y, "Aufnahme bestätigt")
    c.rect(x + 53 * mm, y - 3, 6, 6, fill=0, stroke=1); c.drawString(x + 56 * mm, y, "Aufnahme abgelehnt")
    c.drawString(x, y - 10, "Mitgliedschaft ab:"); line(c, x + 32 * mm, y - 12, 60 * mm)
    c.drawString(x + 98 * mm, y - 10, "Mitgliedsnummer:"); line(c, x + 131 * mm, y - 12, 51 * mm)
    c.drawString(x, y - 20, "Mitgliedsart bei Aufnahme:")
    for xx, lbl in [(x + 47 * mm, "Aktiv"), (x + 66 * mm, "Passiv"), (x + 87 * mm, "Nebenmitglied")]:
        c.rect(xx, y - 23, 6, 6, fill=0, stroke=1); c.drawString(xx + 9, y - 20, lbl)
    c.drawString(x, y - 30, "Datum / dokumentierter Vorstandsbeschluss:"); line(c, x + 74 * mm, y - 32, 108 * mm)
    c.drawString(x, y - 40, "Mitteilung an antragstellende Person am:"); line(c, x + 68 * mm, y - 42, 114 * mm)


def draw_privacy_page(c):
    """Page 2: the full Art. 13 DSGVO information and voluntary consents."""
    header(c, "Informationen nach Art. 13 DSGVO und Kommunikationseinwilligungen", 2)
    x, y = 14 * mm, H - 58 * mm
    y = section(c, "04", "DATENSCHUTZ UND KONTAKT", y)
    text(c, "Der Verein verarbeitet die für Prüfung und Verwaltung der Mitgliedschaft erforderlichen personenbezogenen Daten nach geltendem Datenschutzrecht. Die folgenden Informationen erfüllen die Hinweise nach Art. 13 DSGVO.", x, y, 182 * mm, 6.8, 8)
    y -= 21
    blocks = [
        ("VERANTWORTLICHER", "Kleingartenverein Oberrothenbach e.V., Scheringer Str. 12, 08056 Zwickau, E-Mail: kgvoberrothenbach@gmx.de, Vereinsregister: Amtsgericht Chemnitz VR 70502."),
        ("ZWECKE", "Entscheidung über den Aufnahmeantrag, Begründung und Durchführung der Mitgliedschaft, Mitgliederverwaltung, Beitragserhebung, Erfüllung satzungsgemäßer Aufgaben, Kommunikation und - soweit einschlägig - Verwaltung eines Pachtverhältnisses."),
        ("RECHTSGRUNDLAGEN", "Art. 6 Abs. 1 Buchst. b DSGVO (Antrag, Mitgliedschaft und Pachtverhältnis), Art. 6 Abs. 1 Buchst. c DSGVO (gesetzliche Pflichten) sowie Art. 6 Abs. 1 Buchst. f DSGVO (berechtigte Interessen an einer geordneten Vereinsverwaltung). Freiwillige Kommunikationswege beruhen auf Art. 6 Abs. 1 Buchst. a DSGVO."),
        ("EMPFÄNGER", "Vorstand und hierzu berechtigte Funktionsträger des Vereins; bei Bedarf weisungsgebundene Dienstleister, etwa für IT, Buchhaltung oder Postversand, sowie Stellen, denen Daten aufgrund gesetzlicher Pflichten offenzulegen sind."),
        ("SPEICHERDAUER", "Für die Dauer der Mitgliedschaft bzw. des jeweiligen Vertragsverhältnisses; anschließend entsprechend gesetzlicher Aufbewahrungsfristen und bis zum Ablauf möglicher zivilrechtlicher Verjährungsfristen. Daten aus widerrufenen Einwilligungen werden nicht mehr für den betroffenen Kommunikationsweg genutzt."),
        ("IHRE RECHTE", "Sie haben das Recht auf Auskunft, Berichtigung, Löschung, Einschränkung der Verarbeitung, Datenübertragbarkeit sowie - bei Verarbeitung auf Grundlage berechtigter Interessen - Widerspruch. Eine erteilte Einwilligung kann jederzeit mit Wirkung für die Zukunft widerrufen werden. Beschwerderecht: Sächsischer Datenschutzbeauftragter, Devrientstraße 5, 01067 Dresden, www.datenschutz.sachsen.de."),
        ("ERFORDERLICHKEIT", "Die mit dem Mitgliedsantrag abgefragten Pflichtangaben sind für die Entscheidung über die Aufnahme und die Verwaltung der Mitgliedschaft erforderlich. Ohne diese Daten kann der Antrag regelmäßig nicht bearbeitet werden. Eine automatisierte Entscheidungsfindung oder Profiling findet nicht statt.")]
    for title, body in blocks:
        label(c, title, x, y)
        h = text(c, body, x, y - 3, 182 * mm, 6.4, 7.6)
        y -= h + 11
    y -= 2
    y = section(c, "05", "FREIWILLIGE KOMMUNIKATIONSEINWILLIGUNGEN", y)
    h = text(c, "Die folgenden Angaben sind freiwillig. Eine Verweigerung oder ein späterer Widerruf hat keine Nachteile für die Mitgliedschaft; ausgenommen ist lediglich die gewählte Zustellung per E-Mail, soweit sie ausdrücklich gewünscht wird.", x, y, 182 * mm, 6.8, 8)
    y -= h + 4
    c.setFillColor(PALE_GREEN); c.roundRect(x, y - 9 * mm, 182 * mm, 9 * mm, 2 * mm, fill=1, stroke=0)
    c.setFillColor(GREEN); c.setFont("Helvetica-Bold", 8); c.drawString(x + 3 * mm, y - 5.7 * mm, "Antragstellende Person")
    y -= 13 * mm
    c.setFillColor(colors.HexColor("#172233")); c.setFont("Helvetica", 6.8); c.drawString(x, y, "Bitte ankreuzen, soweit die jeweilige Kommunikation gewünscht ist:")
    for n, line_text in enumerate(["E-Mail für Vereinsinformationen", "E-Mail für Rechnungen / Abrechnungen", "WhatsApp für Vereinsinformationen"]):
        yy = y - 10 - n * 11; c.setFillColor(FIELD); c.rect(x + 3, yy - 4, 8, 8, fill=1, stroke=0); c.setFillColor(colors.HexColor("#172233")); c.drawString(x + 15, yy - 1, line_text)
    y -= 47
    c.setFont("Helvetica", 6.8); c.drawString(x, y, "Einwilligung: Die angekreuzten Kommunikationswege dürfen bis zu meinem Widerruf genutzt werden.")
    line(c, x, y - 15, 80 * mm); c.setFillColor(LABEL); c.setFont("Helvetica", 6.2); c.drawString(x + 12 * mm, y - 23, "Unterschrift")


def draw_membership_application_page_compact(c):
    header(c, "Für jede Person ist ein eigener Antrag einzureichen. Dieser Antrag begründet kein Pachtverhältnis.", 1)
    x, y = 14 * mm, H - 58 * mm
    y = section(c, "01", "ANGABEN ZUR ANTRAGSTELLENDEN PERSON", y)
    field(c, "Name", x, y - 8, 44 * mm); field(c, "Vorname", x + 46 * mm, y - 8, 44 * mm)
    field(c, "Geburtsdatum", x + 92 * mm, y - 8, 44 * mm); field(c, "Telefon (freiwillig)", x + 138 * mm, y - 8, 44 * mm)
    field(c, "E-Mail (freiwillig)", x, y - 29, 88 * mm); field(c, "Anschrift", x + 90 * mm, y - 29, 92 * mm)
    y -= 38
    y = section(c, "02", "BEANTRAGTE MITGLIEDSCHAFT", y)
    c.setFillColor(colors.HexColor("#172233")); c.setFont("Helvetica", 7.8)
    c.drawString(x, y, "Bitte genau eine Mitgliedschaftsart auswählen:")
    for xx, label_text in [(x + 15 * mm, "Aktiv"), (x + 85 * mm, "Passiv"), (x + 145 * mm, "Nebenmitglied")]:
        c.rect(xx, y - 13, 6, 6, fill=0, stroke=1); c.drawString(xx + 9, y - 10, label_text)
    c.setFont("Helvetica", 7.1)
    c.drawString(x, y - 20, "Aktiv: bei bestehendem oder gleichzeitig wirksam werdendem Pachtvertrag · Passiv: ohne eigenen Pachtvertrag.")
    c.drawString(x, y - 29, "Nebenmitglied: z. B. Ehe- oder Lebenspartner eines aktiven Mitglieds · Ehrenmitgliedschaft nur durch Ernennung der Mitgliederversammlung.")
    c.setFont("Helvetica", 7.6)
    c.drawString(x, y - 40, "Bei Nebenmitgliedschaft - aktives Bezugsmitglied:"); line(c, x + 83 * mm, y - 42, 46 * mm)
    c.drawString(x + 133 * mm, y - 40, "Parzelle:"); line(c, x + 151 * mm, y - 42, 31 * mm)
    c.setFont("Helvetica", 7.0); c.drawString(x, y - 50, "Die Zuordnung als aktives oder passives Mitglied richtet sich nach der tatsächlichen Pächterstellung. Über die Aufnahme entscheidet der Vorstand.")
    y -= 59
    y = section(c, "03", "MITGLIEDSCHAFT UND PFLICHTEN", y)
    h = text(c, "Ich beantrage die Aufnahme in den Kleingartenverein Oberrothenbach e. V. und erkenne die Satzung sowie die für meine Mitgliedschaft wirksam geltenden Vereinsordnungen an. Die Mitgliedschaft beginnt mit der Aufnahmebestätigung des Vorstands, sofern darin kein späterer Zeitpunkt genannt wird.<br/><br/>Aktive, passive und Ehrenmitglieder sind ab Vollendung des 16. Lebensjahres stimmberechtigt. Nebenmitglieder haben kein eigenes Stimmrecht und schulden keine eigenen Pflichtarbeitsstunden. Beiträge, Gebühren und gegebenenfalls Pflichtarbeitsleistungen richten sich nach der Satzung und den wirksam beschlossenen Ordnungen. Die Mitgliedschaft allein begründet keinen Anspruch auf eine Parzelle.", x, y, 182 * mm, 7.6, 9)
    y -= h + 6
    y = section(c, "05", "ERKLÄRUNG UND UNTERSCHRIFT", y)
    h = text(c, "Ich bestätige die Richtigkeit meiner Angaben und dass mir die Satzung und die einschlägigen Vereinsordnungen zugänglich gemacht wurden.", x, y, 182 * mm, 7.8, 9.5)
    y -= h + 7
    c.setFont("Helvetica", 8); c.drawString(x, y, "Ort, Datum:"); line(c, x + 22 * mm, y - 2, 75 * mm)
    c.drawString(x, y - 24, "Unterschrift:"); line(c, x + 22 * mm, y - 26, 105 * mm)
    c.setFillColor(LABEL); c.setFont("Helvetica", 6.2); c.drawString(x + 22 * mm, y - 33, "Antragsteller/in")
    y -= 45
    y = section(c, "06", "BEI MINDERJÄHRIGEN - ZUSTIMMUNG DER GESETZLICHEN VERTRETUNG", y)
    c.setFillColor(colors.HexColor("#172233")); c.setFont("Helvetica", 7.8)
    c.drawString(x, y, "Name(n):"); line(c, x + 18 * mm, y - 2, 65 * mm)
    c.drawString(x, y - 12, "Ort, Datum:"); line(c, x + 22 * mm, y - 14, 65 * mm)
    c.drawString(x, y - 30, "Unterschrift(en):"); line(c, x + 27 * mm, y - 32, 100 * mm)
    y -= 43
    y = section(c, "07", "ENTSCHEIDUNG DES VORSTANDS (VOM VEREIN AUSZUFÜLLEN)", y)
    c.setFont("Helvetica", 7.6)
    c.rect(x + 1, y - 3, 6, 6, fill=0, stroke=1); c.drawString(x + 10, y, "Aufnahme bestätigt")
    c.rect(x + 53 * mm, y - 3, 6, 6, fill=0, stroke=1); c.drawString(x + 56 * mm, y, "Aufnahme abgelehnt")
    c.drawString(x, y - 10, "Mitgliedschaft ab:"); line(c, x + 32 * mm, y - 12, 60 * mm)
    c.drawString(x + 98 * mm, y - 10, "Mitgliedsnummer:"); line(c, x + 131 * mm, y - 12, 51 * mm)
    c.drawString(x, y - 20, "Mitgliedsart bei Aufnahme:")
    for xx, label_text in [(x + 47 * mm, "Aktiv"), (x + 66 * mm, "Passiv"), (x + 87 * mm, "Nebenmitglied")]:
        c.rect(xx, y - 23, 6, 6, fill=0, stroke=1); c.drawString(xx + 9, y - 20, label_text)
    c.drawString(x, y - 30, "Datum / dokumentierter Vorstandsbeschluss:"); line(c, x + 74 * mm, y - 32, 108 * mm)
    c.drawString(x, y - 40, "Mitteilung an antragstellende Person am:"); line(c, x + 68 * mm, y - 42, 114 * mm)


def main():
    OUT.parent.mkdir(parents=True, exist_ok=True)
    c = Canvas(str(OUT), pagesize=A4, pageCompression=1)
    c.setTitle("Mitgliedsantrag - KGV Oberrothenbach")
    draw_membership_application_page_compact(c)
    c.showPage()
    draw_privacy_page(c)
    c.save()


if __name__ == "__main__":
    main()
