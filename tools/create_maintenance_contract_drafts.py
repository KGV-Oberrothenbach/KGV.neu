from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import mm
from reportlab.pdfgen.canvas import Canvas
from reportlab.platypus import Paragraph

ROOT = Path('KGV.Core/Templates')
CONTRACT = ROOT / 'wartungsvertrag/v1/template.pdf'
RENEWAL = ROOT / 'wartungsvertrag-verlaengerung/v1/template.pdf'
W, H = A4
GREEN = colors.HexColor('#1f6840')
PALE = colors.HexColor('#e7f0ea')
INK = colors.HexColor('#1d2733')
LINE = colors.HexColor('#8ea395')


def header(c, page, pages, document_type):
    c.setFillColor(GREEN); c.rect(0, H - 29 * mm, W, 29 * mm, fill=1, stroke=0)
    logo = Path('Logo.png')
    if logo.exists():
        c.drawImage(str(logo), 16 * mm, H - 22 * mm, 12 * mm, 12 * mm, preserveAspectRatio=True, mask='auto')
    c.setFillColor(colors.white); c.setFont('Helvetica-Bold', 13); c.drawString(34 * mm, H - 12 * mm, 'KGV OBERROTHENBACH e.V.')
    c.setFont('Helvetica', 7); c.drawString(34 * mm, H - 20 * mm, document_type)
    c.drawRightString(194 * mm, H - 12 * mm, f'Seite {page} von {pages}')
    c.setFillColor(colors.HexColor('#7a817c')); c.setFont('Helvetica', 7)
    c.drawString(14 * mm, 10 * mm, 'KGV Oberrothenbach e.V.')
    c.drawRightString(196 * mm, 10 * mm, f'Seite {page}')


def title(c, value, subtitle=None):
    c.setFillColor(GREEN); c.setFont('Helvetica-Bold', 18); c.drawString(14 * mm, H - 47 * mm, value)
    if subtitle:
        c.setFillColor(INK); c.setFont('Helvetica', 8.5); c.drawString(14 * mm, H - 53 * mm, subtitle)


def section(c, number, value, y):
    c.setFillColor(PALE); c.roundRect(14 * mm, y - 8 * mm, 182 * mm, 8 * mm, 2.5 * mm, fill=1, stroke=0)
    c.setFillColor(GREEN); c.setFont('Helvetica-Bold', 8); c.drawString(17 * mm, y - 5.4 * mm, number)
    c.setFont('Helvetica-Bold', 11); c.drawString(27 * mm, y - 5.6 * mm, value)
    return y - 12 * mm


def para(c, value, x, y, width=182 * mm, size=8.5, leading=10.3):
    style = ParagraphStyle('body', fontName='Helvetica', fontSize=size, leading=leading, textColor=INK)
    paragraph = Paragraph(value, style); _, height = paragraph.wrap(width, 500); paragraph.drawOn(c, x, y - height)
    return height


def field(c, x, y, label, width):
    c.setFillColor(INK); c.setFont('Helvetica', 8.2); c.drawString(x, y, label)
    label_width = c.stringWidth(label, 'Helvetica', 8.2)
    c.setStrokeColor(LINE); c.setLineWidth(.45); c.line(x + label_width + 2 * mm, y - 2, x + width, y - 2)


def first_contract_page1(c):
    header(c, 1, 2, 'Wartungsvertrag')
    title(c, 'WARTUNGSVERTRAG', 'für die Saison {{saisonjahr}}')
    x, y = 14 * mm, H - 65 * mm
    y = section(c, '01', 'VERTRAGSPARTEIEN', y)
    para(c, 'Kleingartenverein Oberrothenbach e. V., Scheringer Str. 12, 08056 Zwickau, vertreten durch zwei gemeinsam vertretungsberechtigte Vorstandsmitglieder, nachfolgend „Verein“ genannt,', x, y, 182 * mm, 8.3, 10)
    y -= 12 * mm
    field(c, x, y, 'Mitglied / Vertragspartner: {{mitglied_name}}', 120 * mm)
    field(c, x, y - 9 * mm, 'Mitgliedsnummer: {{mitgliedsnummer}}', 68 * mm)
    field(c, x + 74 * mm, y - 9 * mm, 'Parzelle (soweit vorhanden): {{parzelle_nummer}}', 180 * mm)
    y -= 20 * mm
    y = section(c, '02', 'GEGENSTAND UND SAISON', y)
    field(c, x, y, 'Wartungsvertragsart: {{wartungsvertrag_titel}}', 182 * mm)
    field(c, x, y - 9 * mm, 'Bereich: {{wartungsvertrag_bereich}}', 90 * mm)
    field(c, x + 96 * mm, y - 9 * mm, 'Saison: {{saisonjahr}}', 182 * mm)
    y -= 20 * mm
    y = section(c, '03', 'VEREINBARTE WARTUNGSLEISTUNGEN', y)
    para(c, 'Die vereinbarten Wartungsleistungen ergeben sich vollständig aus der nachfolgenden, zum Wartungsvertrag gespeicherten Leistungsbeschreibung:', x, y, 182 * mm, 8.3, 10)
    y -= 13 * mm
    c.setFillColor(colors.HexColor('#f6f9f7')); c.roundRect(x, y - 44 * mm, 182 * mm, 44 * mm, 1.5 * mm, fill=1, stroke=0)
    para(c, '{{wartungsvertrag_beschreibung}}', x + 4 * mm, y - 4 * mm, 174 * mm, 8.2, 10)
    y -= 50 * mm
    y = section(c, '04', 'WEITERE ANGABEN', y)
    field(c, x, y, 'Arbeitsstundenwirkung: {{arbeitsstundenwirkung}}', 182 * mm)
    field(c, x, y - 10 * mm, 'Bemerkung: {{wartungsvertrag_bemerkung}}', 182 * mm)


def first_contract_page2(c):
    c.saveState(); header(c, 2, 2, 'Wartungsvertrag'); c.restoreState()
    title(c, 'WARTUNGSVERTRAG')
    x, y = 14 * mm, H - 60 * mm
    y = section(c, '05', 'LAUFZEIT UND VERLÄNGERUNG', y)
    y -= para(c, 'Dieser Wartungsvertrag gilt ausschließlich für die oben angegebene Saison {{saisonjahr}} und endet mit deren Ablauf. Der Vorstand kann den Wartungsvertrag jeweils durch Entscheidung um eine weitere Saison verlängern.<br/><br/>Wünscht das Mitglied keine Verlängerung für die folgende Saison, genügt eine kurze formlose Mitteilung an den Verein, sofern diese vor Beginn der neuen Saison eingeht. Eine besondere Kündigungsform oder ein gesondertes Formular ist hierfür nicht erforderlich.', x, y, 182 * mm, 8.5, 10.5) + 8
    y = section(c, '06', 'UNTERSCHRIFTEN', y)
    c.setFillColor(INK); c.setFont('Helvetica', 8.5); c.drawString(x, y, 'Ort / Datum:'); c.setStrokeColor(LINE); c.line(x + 27 * mm, y - 2, x + 120 * mm, y - 2)
    c.drawString(x, y - 18 * mm, 'Mitglied / Vertragspartner:'); c.line(x + 51 * mm, y - 20 * mm, x + 122 * mm, y - 20 * mm)
    c.setFont('Helvetica', 7); c.setFillColor(colors.HexColor('#59645d')); c.drawString(x + 51 * mm, y - 25 * mm, 'Unterschrift')
    c.setFillColor(INK); c.setFont('Helvetica', 8.5); c.drawString(x, y - 43 * mm, 'Für den Verein (zwei gemeinsam vertretungsberechtigte Vorstandsmitglieder):')
    c.drawString(x, y - 61 * mm, '1. Name / Funktion'); c.line(x + 40 * mm, y - 63 * mm, x + 86 * mm, y - 63 * mm); c.drawString(x + 89 * mm, y - 61 * mm, 'Unterschrift'); c.line(x + 112 * mm, y - 63 * mm, x + 152 * mm, y - 63 * mm)
    c.drawString(x, y - 79 * mm, '2. Name / Funktion'); c.line(x + 40 * mm, y - 81 * mm, x + 86 * mm, y - 81 * mm); c.drawString(x + 89 * mm, y - 79 * mm, 'Unterschrift'); c.line(x + 112 * mm, y - 81 * mm, x + 152 * mm, y - 81 * mm)


def renewal_notice(c):
    header(c, 1, 1, 'Verlängerungsmitteilung Wartungsvertrag')
    title(c, 'VERLÄNGERUNGSMITTEILUNG', 'Wartungsvertrag für die Saison {{saisonjahr}}')
    x, y = 14 * mm, H - 65 * mm
    y = section(c, '01', 'MITTEILUNG DES VORSTANDS', y)
    y -= para(c, 'Sehr geehrte/r {{mitglied_name}},<br/><br/>Ihr bestehender Wartungsvertrag „{{wartungsvertrag_titel}}“ wurde durch den Vorstand für die Saison {{saisonjahr}} verlängert. Diese Mitteilung dokumentiert den vollständigen, für diese Saison geltenden Vertragsumfang. Eine erneute Unterschrift des Mitglieds ist nicht erforderlich.', x, y, 182 * mm, 8.8, 10.8) + 8
    y = section(c, '02', 'ZUORDNUNG UND SAISON', y)
    field(c, x, y, 'Mitglied: {{mitglied_name}} · Mitgliedsnummer: {{mitgliedsnummer}}', 112 * mm)
    field(c, x, y - 9 * mm, 'Parzelle (soweit vorhanden): {{parzelle_nummer}}', 182 * mm)
    field(c, x, y - 18 * mm, 'Wartungsvertragsart: {{wartungsvertrag_titel}}', 182 * mm)
    field(c, x, y - 27 * mm, 'Saison: {{saisonjahr}}', 182 * mm)
    y -= 38 * mm
    y = section(c, '03', 'WARTUNGSLEISTUNGEN', y)
    para(c, 'Vollständige aktuelle Leistungsbeschreibung:', x, y, 182 * mm, 8.3, 10)
    y -= 8 * mm
    c.setFillColor(colors.HexColor('#f6f9f7')); c.roundRect(x, y - 37 * mm, 182 * mm, 37 * mm, 1.5 * mm, fill=1, stroke=0)
    para(c, '{{wartungsvertrag_beschreibung}}', x + 4 * mm, y - 4 * mm, 174 * mm, 8.2, 10)
    y -= 43 * mm
    field(c, x, y, 'Arbeitsstundenwirkung: {{arbeitsstundenwirkung}}', 182 * mm)
    field(c, x, y - 10 * mm, 'Bemerkung: {{wartungsvertrag_bemerkung}}', 182 * mm)
    y -= 21 * mm
    y = section(c, '04', 'DOKUMENTATION DER VERLÄNGERUNG', y)
    field(c, x, y, 'Beschlussdatum des Vorstands: {{verlaengerungsentscheidung_datum}}', 110 * mm)
    field(c, x, y - 10 * mm, 'Erstellt am: {{verlaengerungsmitteilung_erstellt_am}}', 100 * mm)
    c.setFillColor(INK); c.setFont('Helvetica', 8.2); c.drawString(x, y - 23 * mm, 'Der ursprüngliche Wartungsvertrag bleibt als Ursprungsvertrag erhalten.')


def main():
    CONTRACT.parent.mkdir(parents=True, exist_ok=True); RENEWAL.parent.mkdir(parents=True, exist_ok=True)
    c = Canvas(str(CONTRACT), pagesize=A4, pageCompression=1); c.setTitle('Wartungsvertrag - KGV Oberrothenbach')
    first_contract_page1(c); c.showPage(); first_contract_page2(c); c.save()
    c = Canvas(str(RENEWAL), pagesize=A4, pageCompression=1); c.setTitle('Verlängerungsmitteilung Wartungsvertrag - KGV Oberrothenbach')
    renewal_notice(c); c.save()


if __name__ == '__main__':
    main()
