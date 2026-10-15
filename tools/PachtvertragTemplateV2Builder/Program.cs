using System;
using System.IO;
using System.Linq;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.Annotations;
using PdfSharpCore.Pdf.IO;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var source = Path.Combine(root, "KGV.Core", "Templates", "pachtvertrag", "v1", "template.pdf");
var target = Path.Combine(root, "KGV.Core", "Templates", "pachtvertrag", "v2", "template.pdf");
using var input = File.OpenRead(source);
using var pdf = PdfReader.Open(input, PdfDocumentOpenMode.Modify);
void Erase(int page, double x, double y, double w, double h)
{
    using var gfx = XGraphics.FromPdfPage(pdf.Pages[page - 1], XGraphicsPdfPageOptions.Append);
    gfx.DrawRectangle(XBrushes.White, x, y, w, h);
}
// The former generic co-tenant block is rendered dynamically.  With no second
// party it is entirely blank, rather than leaving a misleading placeholder.
Erase(1, 38, 282, 520, 82);
// Cost reimbursements have no contractual meaning and are therefore absent.
Erase(1, 207, 558, 180, 48);
// Community services belong to membership rules, not a lease contract.
Erase(2, 38, 354, 520, 104);
// The second signature line and label are dynamic in the renderer.
Erase(3, 38, 548, 210, 74);
foreach (var page in pdf.Pages)
{
    if (page.Annotations == null) continue;
    var annotations = page.Annotations.Cast<PdfAnnotation>().ToArray();
    foreach (var annotation in annotations) page.Annotations.Remove(annotation);
}
pdf.Internals.Catalog.Elements.Remove("/AcroForm");
using var output = File.Create(target);
pdf.Save(output);
