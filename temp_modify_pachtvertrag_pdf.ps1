$dll='C:\Users\Andreas\.nuget\packages\pdfsharpcore\1.3.67\lib\net5.0\PdfSharpCore.dll'
$zipDll='C:\Users\Andreas\.nuget\packages\sharpziplib\1.4.2\lib\net6.0\ICSharpCode.SharpZipLib.dll'
Add-Type -Path $zipDll
Add-Type -Path $dll
$pdfPath='C:\Programmieren\KGV\KGV.neu\Formulare\Pachtvertrag_KGV_bereinigt_mit_Feldern.pdf'
$pdf=[PdfSharpCore.Pdf.IO.PdfReader]::Open($pdfPath,[PdfSharpCore.Pdf.IO.PdfDocumentOpenMode]::Modify)
$page=$pdf.Pages[3]
$graphics=[PdfSharpCore.Drawing.XGraphics]::FromPdfPage($page,[PdfSharpCore.Drawing.XGraphicsPdfPageOptions]::Append)
$yTop=$page.Height.Point - 448
$rect=New-Object PdfSharpCore.Drawing.XRect(235,$yTop,335,28)
$graphics.DrawRectangle([PdfSharpCore.Drawing.XBrushes]::White,$rect)
$graphics.Dispose()
$pdf.Save($pdfPath)
$pdf.Close()