from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import mm
from reportlab.pdfgen.canvas import Canvas
from reportlab.platypus import Paragraph

OUT = Path('KGV.Core/Templates/pachtvertrag/v4/template.pdf')
W, H = A4
GREEN = colors.HexColor('#1f6840')
PALE = colors.HexColor('#e7f0ea')
INK = colors.HexColor('#1d2733')
LINE = colors.HexColor('#8ea395')

def header(c, page):
    c.setFillColor(GREEN); c.rect(0, H-29*mm, W, 29*mm, fill=1, stroke=0)
    logo = Path('Logo.png')
    if logo.exists(): c.drawImage(str(logo), 16*mm, H-22*mm, 12*mm, 12*mm, preserveAspectRatio=True, mask='auto')
    c.setFillColor(colors.white); c.setFont('Helvetica-Bold', 13); c.drawString(34*mm, H-12*mm, 'KGV OBERROTHENBACH e.V.')
    c.setFont('Helvetica', 7); c.drawString(34*mm, H-20*mm, 'Kleingartenpachtvertrag')
    c.drawRightString(194*mm, H-12*mm, f'Seite {page} von 3')
    c.setFillColor(colors.HexColor('#7a817c')); c.setFont('Helvetica', 7)
    c.drawString(14*mm, 10*mm, 'KGV Oberrothenbach e.V. · Vertragsentwurf 21.10.2026')
    c.drawRightString(196*mm, 10*mm, f'Seite {page}')

def title(c, text, page):
    if page == 1:
        c.setFillColor(GREEN); c.setFont('Helvetica-Bold', 19); c.drawString(14*mm, H-47*mm, text)
        c.setFillColor(INK); c.setFont('Helvetica', 8.5); c.drawString(14*mm, H-53*mm, 'zwischen dem KGV Oberrothenbach e.V. und den Pachtparteien')
    else:
        c.setFillColor(GREEN); c.setFont('Helvetica-Bold', 18); c.drawString(14*mm, H-47*mm, text)

def section(c, number, text, y):
    c.setFillColor(PALE); c.roundRect(14*mm, y-8*mm, 182*mm, 8*mm, 2.5*mm, fill=1, stroke=0)
    c.setFillColor(GREEN); c.setFont('Helvetica-Bold', 8); c.drawString(17*mm, y-5.4*mm, number)
    c.setFont('Helvetica-Bold', 11); c.drawString(27*mm, y-5.6*mm, text)
    return y-12*mm

def para(c, value, x, y, width=182*mm, size=8.8, leading=11):
    style = ParagraphStyle('p', fontName='Helvetica', fontSize=size, leading=leading, textColor=INK)
    p = Paragraph(value, style); _, h = p.wrap(width, 500); p.drawOn(c, x, y-h); return h

def rule(c, x, y, length):
    c.setStrokeColor(LINE); c.setLineWidth(.45); c.line(x, y, x+length, y)

def check(c, x, y, label, size=8):
    c.setStrokeColor(LINE); c.rect(x, y-2, 6, 6, fill=0, stroke=1); c.setFillColor(INK); c.setFont('Helvetica', size); c.drawString(x+10, y, label)

def page1(c):
    header(c, 1); title(c, 'KLEINGARTENPACHTVERTRAG', 1); x, y = 14*mm, H-65*mm
    y=section(c,'01','VERTRAGSPARTEIEN',y)
    c.setFillColor(INK)
    y-=para(c,'Verpächter: Kleingartenverein Oberrothenbach e. V., Scheringer Str. 12, 08056 Zwickau, vertreten durch zwei gemeinsam vertretungsberechtigte Vorstandsmitglieder.',x,y,182*mm,8,9)+10
    for yy, text, start in [(y,'Pächter/in 1: Name',38), (y-9*mm,'Geburtsdatum',30), (y-18*mm,'Anschrift',25), (y-27*mm,'E-Mail / Telefon (freiwillig)',48)]:
        c.drawString(x,yy,text); rule(c,x+start*mm,yy-2, 105*mm if start<40 else 82*mm)
    y-=35*mm
    check(c,x,y,'Kein weiterer Mitpächter'); check(c,x+50*mm,y,'Mitpächter/in 2: Name'); rule(c,x+105*mm,y-2,62*mm)
    c.drawString(x,y-9*mm,'Geburtsdatum'); rule(c,x+30*mm,y-11*mm,28*mm); c.drawString(x+60*mm,y-9*mm,'Anschrift'); rule(c,x+84*mm,y-11*mm,60*mm)
    c.drawString(x,y-18*mm,'E-Mail / Telefon (freiwillig)'); rule(c,x+48*mm,y-20*mm,75*mm)
    y-=29*mm
    h=para(c,'Eine bloß als Kontaktperson oder gesetzliche Vertretung benannte Person wird nicht dadurch Mitpächter. Bei mehreren Pächtern bestehen vertragliche Zahlungspflichten als Gesamtschuldner; Erklärungen zur Beendigung oder Änderung des Pachtvertrags sind grundsätzlich gegenüber beziehungsweise von allen Vertragsparteien abzugeben.',x,y,182*mm,8.2,10); y-=h+7
    y=section(c,'02','PACHTGEGENSTAND UND LAUFZEIT',y)
    c.setFont('Helvetica',8.2); c.setFillColor(INK)
    c.drawString(x,y,'Kleingartenanlage'); rule(c,x+35*mm,y-2,37*mm); c.drawString(x+76*mm,y,'Parzelle Nr.'); rule(c,x+101*mm,y-2,22*mm); c.drawString(x+127*mm,y,'Lage'); rule(c,x+141*mm,y-2,38*mm)
    c.drawString(x,y-9*mm,'Fläche ca.'); rule(c,x+19*mm,y-11*mm,16*mm); c.drawString(x+37*mm,y-9*mm,'m²; verbindliche Flächenbasis laut Anlage / Lageplan:'); rule(c,x+118*mm,y-11*mm,62*mm)
    c.drawString(x,y-18*mm,'Beginn des Pachtverhältnisses:'); rule(c,x+54*mm,y-20*mm,30*mm); c.drawString(x+87*mm,y-18*mm,'Übergabe am:'); rule(c,x+112*mm,y-20*mm,30*mm)
    y-=28*mm
    h=para(c,'Die Parzelle wird zur nichterwerbsmäßigen kleingärtnerischen Nutzung nach dem Bundeskleingartengesetz (BKleingG) auf unbestimmte Zeit verpachtet. Eine Probezeit oder ein besonderes probezeitbedingtes Kündigungsrecht wird nicht vereinbart. Der Übergabezustand, vorhandene Anlagen und Zählerstände werden in einem gesonderten Übergabeprotokoll festgehalten.',x,y,182*mm,8.3,10); y-=h+7
    y=section(c,'03','PACHTZINS UND WEITERE KOSTEN',y)
    c.setFont('Helvetica',8.2); c.drawString(x,y,'Pachtzins:'); rule(c,x+18*mm,y-2,16*mm); c.drawString(x+36*mm,y,'EUR je m² und Jahr; auf Grundlage von'); rule(c,x+104*mm,y-2,16*mm); c.drawString(x+122*mm,y,'m² insgesamt'); rule(c,x+148*mm,y-2,17*mm); c.drawString(x+167*mm,y,'EUR jährlich.')
    c.drawString(x,y-9*mm,'Fälligkeit:'); rule(c,x+23*mm,y-11*mm,28*mm); c.drawString(x+55*mm,y-9*mm,'Der Pachtzins darf die gesetzlich zulässige Höhe nicht überschreiten.')
    c.drawString(x,y-18*mm,'Änderungen erfolgen nur auf wirksamer gesetzlicher oder vertraglicher Grundlage.')
    y-=27*mm
    para(c,'Gesondert geschuldete, rechtlich zulässige und nachvollziehbar abgerechnete Neben- und Verbrauchskosten, insbesondere Wasser, Strom und öffentliche Lasten, trägt der Pächter nach Maßgabe dieses Pachtvertrags.',x,y,182*mm,8.2,10)
    # Die weiteren Regelungen zu den Kosten stehen am Beginn der Folgeseite,
    # damit die bestehende Seitenaufteilung nicht unnötig verändert wird.

def page2(c):
    header(c,2); title(c,'KLEINGARTENPACHTVERTRAG',2); x,y=14*mm,H-60*mm
    y-=para(c,'Die Einzelheiten insbesondere zu Vorauszahlungen, Abrechnungszeiträumen, Verbrauchsermittlung, Messeinrichtungen, Verteilungsmaßstäben und Abrechnung richten sich nach der Kleingartenordnung in ihrer jeweils wirksamen Fassung.<br/><br/>Durch Änderungen der Kleingartenordnung können keine neuen Arten von Zahlungspflichten begründet werden, soweit hierfür eine gesonderte gesetzliche oder vertragliche Grundlage erforderlich ist.',x,y,182*mm,7.8,9.5)+7
    y=section(c,'04','NUTZUNG UND GELTENDE KLEINGARTENORDNUNG',y)
    text='(1) Die bei Unterzeichnung übergebene Kleingartenordnung (Fassung vom ____________) ist Bestandteil dieses Pachtvertrags. Die Parteien vereinbaren zusätzlich die nachfolgend begrenzte Möglichkeit, spätere wirksame Fassungen in das Pachtverhältnis einzubeziehen.<br/><br/>(2) Änderungen der Kleingartenordnung durch die nach Satzung zuständige Mitgliederversammlung können für dieses Pachtverhältnis verbindlich werden, soweit sie sich auf die ordnungsgemäße kleingärtnerische Bewirtschaftung, den Schutz von Natur und Umwelt, die Sicherheit, die Nutzung gemeinschaftlicher Anlagen oder die Umsetzung gesetzlicher Anforderungen beziehen, sachlich erforderlich, hinreichend bestimmt und für die Pächter zumutbar sind.<br/><br/>(3) Änderungen dürfen keine neuen Pachtzins-, Gebühren- oder sonstigen Zahlungspflichten begründen, keine wesentlichen vertraglichen Rechte entziehen und keine zwingenden gesetzlichen Regelungen umgehen. Berechtigte Bestandsinteressen und erforderliche Übergangsfristen sind zu berücksichtigen.<br/><br/>(4) Der Verein teilt den Pächtern die beschlossenen Änderungen, den vollständigen geänderten Text, die Gründe und den vorgesehenen Zeitpunkt des Inkrafttretens in Textform mit. Regelmäßig ist eine Vorlaufzeit von mindestens vier Wochen vorzusehen; bei zwingenden gesetzlichen Vorgaben oder akuter Gefahr kann eine kürzere Frist sachlich erforderlich sein.<br/><br/>(5) Änderungen gelten nur, soweit diese Änderungsvereinbarung und die konkrete Änderung nach den gesetzlichen Vorschriften, insbesondere dem AGB-Recht, wirksam sind. Wesentliche Vertragsänderungen bedürfen einer gesonderten Vereinbarung. Ein Vereinsbeschluss allein ersetzt diese nicht.'
    y-=para(c,text,x,y,182*mm,8.25,10.2)+8
    y=section(c,'05','VERHÄLTNIS ZUR VEREINSMITGLIEDSCHAFT',y)
    y-=para(c,'Voraussetzung für die Pächterstellung ist die Mitgliedschaft im Kleingartenverein Oberrothenbach e. V. Bei mehreren Pächtern muss jede Vertragspartei Mitglied des Vereins sein.<br/><br/>Der jeweilige Mitgliedsstatus richtet sich nach der Satzung. Ein Mitpächter kann insbesondere Nebenmitglied sein, ohne dadurch den Status eines aktiven Mitglieds zu erlangen.<br/><br/>Mitgliedschaft und Pachtverhältnis sind rechtlich eigenständige Rechtsverhältnisse. Mitgliedsbeiträge und mitgliedschaftliche Pflichtarbeitsstunden richten sich nach den wirksamen Vereinsregelungen, nicht nach diesem Pachtvertrag. Dieselbe Arbeitsleistung wird nicht ohne sachlichen Grund doppelt gefordert.',x,y,182*mm,8.25,10.2)+8
    y=section(c,'06','SORGFALT, ANLAGEN UND ZUGANG',y)
    y-=para(c,'Die Pächter haben die Parzelle ordnungsgemäß zu bewirtschaften, gemeinschaftliche Anlagen zu schonen, Schäden unverzüglich mitzuteilen und notwendige gesetzlich zulässige Kontrollen sowie Wartungsarbeiten nach angemessener Vorankündigung zu ermöglichen; bei Gefahr im Verzug gelten die gesetzlichen Regeln. Bauliche Maßnahmen bedürfen der nach Kleingartenordnung und Gesetz erforderlichen Zustimmungen.<br/><br/>Die Einzelheiten zur Nutzung gemeinschaftlicher Versorgungseinrichtungen sowie zu parzellenbezogenen Strom- und Wassermesseinrichtungen richten sich nach der Kleingartenordnung in ihrer jeweils wirksamen Fassung.',x,y,182*mm,8.25,10.2)+8
    y=section(c,'07','KÜNDIGUNG, RÜCKGABE UND BESTEHENDE RECHTE',y)
    para(c,'Kündigung und Beendigung richten sich nach den zwingenden Vorschriften des BKleingG, insbesondere §§ 7 bis 9, und den ergänzend anwendbaren gesetzlichen Bestimmungen. Die Beendigung einer Vereinsmitgliedschaft führt nicht automatisch zur Beendigung dieses Pachtvertrags. Bei Rückgabe wird ein Protokoll erstellt; Eigentums-, Entschädigungs-, Beseitigungs- und sonstige Ansprüche richten sich nach der wirksamen vertraglichen und gesetzlichen Rechtslage.',x,y,182*mm,8.25,10.2)

def page3(c):
    header(c,3); title(c,'KLEINGARTENPACHTVERTRAG',3); x,y=14*mm,H-60*mm
    y=section(c,'08','NACHSORGE BEI FEHLENDEM NACHPÄCHTER',y)
    text='(1) Endet das Pachtverhältnis aufgrund einer Kündigung durch den Pächter und ist bei der Rückgabe noch kein Nachpächter vorhanden, verpflichtet sich der bisherige Pächter, soweit diese Vereinbarung rechtlich wirksam ist, für längstens zwei Jahre ab dem rechtlichen Ende des Pachtverhältnisses notwendige Maßnahmen zur Erhaltung eines ordnungsgemäßen Zustands der Parzelle zu übernehmen. Die Pflicht endet vorzeitig mit der dokumentierten Übergabe an einen Nachpächter oder mit schriftlicher Entlassung aus der Nachsorge durch den Verpächter.<br/><br/>(2) Zur Nachsorge zählen erforderliche, zumutbare Pflege- und Sicherungsarbeiten, insbesondere Rasenpflege, zulässiger Rückschnitt von Hecken und Bewuchs, Beseitigung von Unkraut und Abfällen sowie Sicherung vorhandener Anlagen. Eine fortgesetzte kleingärtnerische Nutzung oder die Wiederaufnahme des beendeten Pachtverhältnisses wird nicht geschuldet. Gesetzliche Schutzfristen, insbesondere für Gehölze und brütende Tiere, sind zu beachten.<br/><br/>(3) Der Verpächter ermöglicht den für die Nachsorge erforderlichen Zugang nach vorheriger Abstimmung. Umfang, Zustand und etwaige erforderliche Arbeiten sollen im Rückgabeprotokoll dokumentiert werden. Der Verpächter berücksichtigt nachgewiesene erhebliche Härten und kann auf Antrag die Nachsorge ganz oder teilweise erlassen oder eine zumutbare Ersatzregelung vereinbaren.<br/><br/>(4) Werden wirksam geschuldete Nachsorgearbeiten nicht erbracht, benennt der Verpächter die Versäumnisse in Textform und setzt eine angemessene Frist zur Abhilfe. Nach erfolglosem Fristablauf kann er unter den gesetzlichen Voraussetzungen notwendige Arbeiten ausführen lassen und Ersatz der erforderlichen, nachgewiesenen und angemessenen Kosten verlangen. Gesetzliche Ausnahmen von einer Fristsetzung bleiben unberührt. Eine pauschale Vertragsstrafe wird nicht vereinbart.<br/><br/>(5) Diese Klausel begründet keine pauschale Haftung für Schäden Dritter oder gewöhnliche Abnutzung. Zwingende gesetzliche Regelungen, insbesondere zum Kleingartenpachtrecht, zu allgemeinen Geschäftsbedingungen und zur Rückgabe der Pachtsache, bleiben unberührt.'
    y-=para(c,text,x,y,182*mm,7.4,9.2)+7
    y=section(c,'09','ANLAGEN UND INDIVIDUELLE VEREINBARUNGEN',y)
    c.setFillColor(INK)
    # Die Anlagenauswahl bleibt kompakt, damit die Unterschriftsbereiche erhalten bleiben.
    check(c,x,y,'Kleingartenordnung',6.7)
    check(c,x+52*mm,y,'Beitrags- und Gebührenordnung',6.7)
    check(c,x+124*mm,y,'Arbeitsordnung',6.7)
    check(c,x,y-10*mm,'Lageplan / Flächennachweis',6.7)
    check(c,x+77*mm,y-10*mm,'Übergabeprotokoll',6.7)
    check(c,x,y-20*mm,'Sonstige:',6.7); rule(c,x+23*mm,y-22*mm,99*mm)
    c.drawString(x,y-33*mm,'Individuelle Zusatzvereinbarungen (gegebenenfalls gesondertes, beidseitig unterzeichnetes Blatt):')
    rule(c,x,y-42*mm,182*mm); rule(c,x,y-51*mm,182*mm)
    y-=50*mm
    y=section(c,'10','UNTERSCHRIFTEN',y)
    c.setFont('Helvetica',8.5); c.drawString(x,y,'Ort / Datum:'); rule(c,x+27*mm,y-2,95*mm)
    c.drawString(x,y-14*mm,'Pächter/in 1:'); rule(c,x+30*mm,y-16*mm,40*mm); c.drawString(x+73*mm,y-14*mm,'Mitpächter/in 2:'); rule(c,x+112*mm,y-16*mm,40*mm)
    c.drawString(x,y-29*mm,'Für den Verein (zwei gemeinsam vertretungsberechtigte Vorstandsmitglieder):')
    c.drawString(x,y-45*mm,'1. Name / Funktion'); rule(c,x+40*mm,y-47*mm,46*mm); c.drawString(x+89*mm,y-45*mm,'Unterschrift'); rule(c,x+112*mm,y-47*mm,40*mm)
    c.drawString(x,y-60*mm,'2. Name / Funktion'); rule(c,x+40*mm,y-62*mm,46*mm); c.drawString(x+89*mm,y-60*mm,'Unterschrift'); rule(c,x+112*mm,y-62*mm,40*mm)

def main():
    OUT.parent.mkdir(parents=True,exist_ok=True)
    c=Canvas(str(OUT),pagesize=A4,pageCompression=1); c.setTitle('Kleingartenpachtvertrag - KGV Oberrothenbach')
    page1(c); c.showPage(); page2(c); c.showPage(); page3(c); c.save()

if __name__=='__main__': main()
