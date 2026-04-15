$dll='C:\Users\Andreas\.nuget\packages\pdfsharpcore\1.3.67\lib\net5.0\PdfSharpCore.dll'
Add-Type -Path $dll
$pdf=[PdfSharpCore.Pdf.IO.PdfReader]::Open('C:\Programmieren\KGV\KGV.neu\Formulare\Pachtvertrag_KGV_bereinigt_mit_Feldern.pdf',[PdfSharpCore.Pdf.IO.PdfDocumentOpenMode]::Modify)
Write-Output ("Pages=" + $pdf.Pages.Count)
for($i=0; $i -lt $pdf.Pages.Count; $i++){
  $page=$pdf.Pages[$i]
  Write-Output ("PAGE " + ($i + 1))
  $annots=$page.Elements['/Annots']
  if($null -eq $annots){ Write-Output '  no annots'; continue }
  Write-Output ("  annotsType=" + $annots.GetType().FullName)
  if($annots -is [PdfSharpCore.Pdf.Advanced.PdfReference]){ $annots=$annots.Value }
  if($annots -is [PdfSharpCore.Pdf.PdfArray]){
    foreach($item in $annots.Elements){
      $obj=$item
      if($obj -is [PdfSharpCore.Pdf.Advanced.PdfReference]){ $obj=$obj.Value }
      $name=$obj.Elements['/T']
      $rect=$obj.Elements['/Rect']
      $subtype=$obj.Elements['/Subtype']
      Write-Output ("  field=" + $name + " subtype=" + $subtype + " rect=" + $rect)
      if(([string]$name) -like '*member_fee_display*' -or ([string]$name) -like '*rent_display*' -or ([string]$name) -like '*total_display*'){
        Write-Output '  --- element keys ---'
        foreach($key in $obj.Elements.KeyNames){ Write-Output ('    ' + $key + '=' + $obj.Elements[$key]) }
        Write-Output ('  rectType=' + $obj.Elements['/Rect'].GetType().FullName)
        Write-Output ('  rectValues=' + (($obj.Elements['/Rect'].Elements | ForEach-Object { $_.ToString() }) -join ','))
      }
    }
  }
}
