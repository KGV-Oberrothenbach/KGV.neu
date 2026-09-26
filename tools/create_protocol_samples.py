from pathlib import Path
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import mm
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak
from reportlab.lib.utils import ImageReader

OUT = Path('output/pdf')
OUT.mkdir(parents=True, exist_ok=True)
LOGO = ImageReader('KGV.Maui/Resources/Images/kgv_logo.png')
styles = getSampleStyleSheet()
styles.add(ParagraphStyle(name='Small', parent=styles['Normal'], fontName='Helvetica', fontSize=8.5, leading=11))
styles.add(ParagraphStyle(name='Head', parent=styles['Heading1'], textColor=colors.HexColor('#174a2c'), fontSize=17, leading=21, spaceAfter=6))
styles.add(ParagraphStyle(name='Section', parent=styles['Heading2'], textColor=colors.HexColor('#174a2c'), fontSize=11, leading=14, spaceBefore=9, spaceAfter=4))

def header(canvas, doc):
    canvas.saveState()
    canvas.setFillColor(colors.HexColor('#174a2c')); canvas.rect(0, A4[1]-18*mm, A4[0], 18*mm, fill=1, stroke=0)
    canvas.drawImage(LOGO, 18*mm, A4[1]-15.5*mm, 10*mm, 10*mm, preserveAspectRatio=True, mask='auto')
    canvas.setFillColor(colors.white); canvas.setFont('Helvetica-Bold', 12)
    canvas.drawString(31*mm, A4[1]-11*mm, 'KLEINGARTENVEREIN OBERROTHENBACH e. V.')
    canvas.setFont('Helvetica', 7); canvas.drawRightString(A4[0]-18*mm, A4[1]-11*mm, 'KGV-App | Musterformular')
    canvas.setStrokeColor(colors.HexColor('#174a2c')); canvas.line(18*mm, 14*mm, A4[0]-18*mm, 14*mm)
    canvas.setFillColor(colors.HexColor('#555555')); canvas.setFont('Helvetica', 7)
    canvas.drawString(18*mm, 9*mm, 'Dokument wird dem Mitglied zugeordnet; die Parzelle ist der fachliche Bezug.')
    canvas.drawRightString(A4[0]-18*mm, 9*mm, f'Seite {doc.page}')
    canvas.restoreState()

def grid(rows, widths=(52*mm, 123*mm)):
    t=Table(rows, colWidths=widths, hAlign='LEFT')
    t.setStyle(TableStyle([('GRID',(0,0),(-1,-1),.35,colors.HexColor('#b7c3bb')),('BACKGROUND',(0,0),(0,-1),colors.HexColor('#edf4ee')),('FONTNAME',(0,0),(0,-1),'Helvetica-Bold'),('FONTNAME',(1,0),(-1,-1),'Helvetica'),('FONTSIZE',(0,0),(-1,-1),7.8),('LEADING',(0,0),(-1,-1),10),('VALIGN',(0,0),(-1,-1),'TOP'),('LEFTPADDING',(0,0),(-1,-1),5),('RIGHTPADDING',(0,0),(-1,-1),5),('TOPPADDING',(0,0),(-1,-1),5),('BOTTOMPADDING',(0,0),(-1,-1),5)]))
    return t

def section(story, title, rows):
    story.append(Paragraph(title, styles['Section'])); story.append(grid(rows)); story.append(Spacer(1,3*mm))

def protocol(kind, member_role, file):
    story=[Spacer(1,14*mm), Paragraph(f'{kind}PROTOKOLL PARZELLE', styles['Head']), Paragraph('Muster - digitale Erfassung mit Fotoanlagen und Unterschriften', styles['Small']), Spacer(1,4*mm)]
    section(story, '1. Beteiligte und Anlass', [('Parzelle','Nr. ________   Anlage: ____________________'),(member_role,'____________________________________________'),('Vorstand','____________________________________________'),('Datum / Uhrzeit','__.__.20__  |  ______ Uhr')])
    section(story, '2. Zählerstände', [('Wasser','Zählernr. __________  Stand ______ m³   [ ] neu abgelesen   [ ] letzte Ablesung übernommen'),('Strom','Zählernr. __________  Stand ______ kWh  [ ] neu abgelesen   [ ] letzte Ablesung übernommen'),('Hinweis','Neu: PA-/PE-Ablesung speichern. Übernahme: vorhandene Ablesung nur referenzieren.')])
    section(story, '3. Zustand und Vereinbarungen', [('Laube / Gebäude','____________________________________________________________'),('Garten / Einfriedung','____________________________________________________________'),('Wasser / Strom','____________________________________________________________'),('Bemerkungen','____________________________________________________________\n____________________________________________________________')])
    section(story, '4. Fotos und Anlagen', [('Fotos','Bis zu 10 Fotos; jedes Foto erhält Nummer, Datum und Beschreibung.'),('Foto 1-10','01 __________  02 __________  03 __________  04 __________  05 __________\n06 __________  07 __________  08 __________  09 __________  10 __________')])
    section(story, '5. Unterschriften', [(member_role,'Digitale Unterschrift: ____________________________________'),('Vorstand','Digitale Unterschrift: ____________________________________')])
    SimpleDocTemplate(str(OUT/file), pagesize=A4, leftMargin=18*mm,rightMargin=18*mm,topMargin=22*mm,bottomMargin=18*mm).build(story,onFirstPage=header,onLaterPages=header)

def inspection():
    story=[Spacer(1,14*mm), Paragraph('BEGEHUNGSPROTOKOLL PARZELLE', styles['Head']), Paragraph('Muster - Hinweis auf Feststellungen mit nachvollziehbarer Frist', styles['Small']), Spacer(1,4*mm)]
    section(story,'1. Beteiligte und Anlass',[('Parzelle','Nr. ________   Anlage: ____________________'),('Pächter','____________________________________________'),('Vorstand','____________________________________________'),('Datum / Anlass','__.__.20__  |  ☐ Routine  ☐ Anlassbezogen  ☐ Nachkontrolle')])
    section(story,'2. Feststellungen',[('Bereich','☐ Garten  ☐ Laube  ☐ Wege  ☐ Einfriedung  ☐ Wasser/Strom  ☐ Sonstiges'),('Feststellung','____________________________________________________________\n____________________________________________________________'),('Erforderliche Maßnahme','____________________________________________________________'),('Frist','__.__.20__')])
    section(story,'3. Fotos und Anlagen',[('Fotos','Bis zu 10 Fotos, nummeriert und mit Beschreibung.'),('Foto 1-10','01 __________  02 __________  03 __________  04 __________  05 __________\n06 __________  07 __________  08 __________  09 __________  10 __________')])
    section(story,'4. Kenntnisnahme',[('Pächter','Digitale Unterschrift / Kenntnisnahme: ______________________'),('Vorstand','Digitale Unterschrift: ____________________________________'),('Hinweis','Eine fehlende Unterschrift wird mit Grund im Protokoll vermerkt.')])
    SimpleDocTemplate(str(OUT/'Muster_Begehungsprotokoll.pdf'), pagesize=A4,leftMargin=18*mm,rightMargin=18*mm,topMargin=22*mm,bottomMargin=18*mm).build(story,onFirstPage=header,onLaterPages=header)

protocol('ÜBERNAHME', 'Neuer Pächter', 'Muster_Uebernahmeprotokoll.pdf')
protocol('RÜCKGABE', 'Bisheriger Pächter', 'Muster_Rueckgabeprotokoll.pdf')
inspection()
