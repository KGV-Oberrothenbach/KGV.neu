"""Build MA v4 from the unchanged v3 content plus persistent-signature zones."""
from __future__ import annotations
import json
import runpy
from io import BytesIO
from pathlib import Path

from pypdf import PdfReader, PdfWriter
from reportlab.lib.colors import HexColor
from reportlab.lib.pagesizes import A4
from reportlab.pdfgen.canvas import Canvas

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "KGV.Core/Templates/mitgliedsantrag/v4/template.pdf"
SPEC = ROOT / "KGV.Core/Templates/mitgliedsantrag/v4/field-spec.json"

def overlay() -> BytesIO:
    stream = BytesIO(); canvas = Canvas(stream, pagesize=A4); width, height = A4
    canvas.setStrokeColor(HexColor("#172233")); canvas.setFillColor(HexColor("#172233")); canvas.setFont("Helvetica-Bold", 8)
    canvas.showPage()
    canvas.setStrokeColor(HexColor("#172233")); canvas.setFillColor(HexColor("#172233")); canvas.setFont("Helvetica-Bold", 8)
    canvas.drawString(40, 250, "Für den Verein")
    for index, y in enumerate((220, 160), 1):
        canvas.setFont("Helvetica", 8); canvas.drawString(40, y + 12, f"Vereinsunterzeichner {index}")
        canvas.line(40, y, 280, y); canvas.line(315, y, 555, y)
        canvas.setFont("Helvetica", 6.5); canvas.setFillColor(HexColor("#5b6573"))
        canvas.drawString(40, y - 10, "Unterschrift")
        canvas.drawString(315, y - 10, "Name / Funktion / gegebenenfalls Vertretung")
        canvas.setFillColor(HexColor("#172233"))
    canvas.save(); stream.seek(0); return stream

def build_spec() -> None:
    value = json.loads((ROOT / "KGV.Core/Templates/mitgliedsantrag/v2/field-spec.json").read_text(encoding="utf-8"))
    value["field_spec_version"] = "mitgliedsantrag-field-spec/v4"
    fields = value["fields"]
    rename = {"unterschrift_antragsteller": "unterschrift_antragsteller_antrag", "unterschrift_zweite_partei": "unterschrift_zweite_partei_antrag", "mitglied_datenschutz_unterschrift": "unterschrift_antragsteller_datenschutz", "zweite_partei_datenschutz_unterschrift": "unterschrift_zweite_partei_datenschutz"}
    for field in fields:
        if field["name"] in rename: field["name"] = rename[field["name"]]
    fields[:] = [field for field in fields if field["name"] not in {"unterschrift_verein", "vereins_unterzeichner"}]
    fields.extend([
        {"name":"unterschrift_vorstand_1","type":"signature","page":2,"rect":{"x":40,"y":592,"w":240,"h":30},"flags":{}},
        {"name":"vereinsunterzeichner_1","type":"multiline","page":2,"rect":{"x":315,"y":582,"w":240,"h":42},"flags":{"multiline":True}},
        {"name":"unterschrift_vorstand_2","type":"signature","page":2,"rect":{"x":40,"y":652,"w":240,"h":30},"flags":{}},
        {"name":"vereinsunterzeichner_2","type":"multiline","page":2,"rect":{"x":315,"y":642,"w":240,"h":42},"flags":{"multiline":True}},
    ])
    SPEC.parent.mkdir(parents=True, exist_ok=True); SPEC.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

def main() -> None:
    v3 = runpy.run_path(str(ROOT / "tools/create_membership_application_v3.py")); v3["main"].__globals__["OUT"] = OUT; OUT.parent.mkdir(parents=True, exist_ok=True); v3["main"]()
    source = PdfReader(str(OUT)); top = PdfReader(overlay()); writer = PdfWriter()
    for number, page in enumerate(source.pages):
        if number == 1: page.merge_page(top.pages[1])
        writer.add_page(page)
    with OUT.open("wb") as file: writer.write(file)
    build_spec()

if __name__ == "__main__": main()
