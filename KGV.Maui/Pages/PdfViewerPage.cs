using System;
using System.IO;
using System.Threading.Tasks;
using KGV.Core.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Storage;

namespace KGV.Maui.Pages
{
    // Simple POC in-app PDF viewer that embeds the PDF as a base64 data URI in an HTML page shown via WebView.
    // This is intentionally minimal and meant as a proof-of-concept before integrating PDF.js or a native viewer.
    public sealed class PdfViewerPage : ContentPage
    {
        private readonly Button _deleteButton;
        private readonly string _filePath;
        private readonly WebView _webView;
        private readonly KGV.Core.Interfaces.ISupabaseService _supabaseService;
        private readonly int? _mitgliedId;

        public PdfViewerPage(string filePath, KGV.Core.Interfaces.ISupabaseService supabaseService, int? mitgliedId = null)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mitgliedId = mitgliedId;
            Title = Path.GetFileName(filePath);
            BackgroundColor = Colors.White;

            _webView = new WebView
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            var signButton = new Button { Text = "Unterschreiben" };
            signButton.Clicked += async (_, _) => await OnSignClicked();

            var uploadButton = new Button { Text = "Speichern & Hochladen" };
            uploadButton.Clicked += async (_, _) => await OnUploadClicked();

            _deleteButton = new Button { Text = "Antrag löschen", IsVisible = false, BackgroundColor = Colors.LightCoral, TextColor = Colors.White };
            _deleteButton.Clicked += async (_, _) => await OnDeleteClicked();

            Content = new VerticalStackLayout
            {
                Padding = 0,
                Spacing = 8,
                Children =
                {
                    new HorizontalStackLayout
                    {
                        Padding = 8,
                        Spacing = 8,
                        Children = { signButton, _deleteButton, uploadButton }
                    },
                    new Border
                    {
                        Stroke = Colors.LightGray,
                        Content = _webView,
                        VerticalOptions = LayoutOptions.FillAndExpand,
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    }
                }
            };
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = LoadPdfIntoWebViewAsync();
        }

        private async Task UpdateDeleteButtonVisibilityAsync(byte[] fileBytes)
        {
            try
            {
                var fileName = Path.GetFileName(_filePath) ?? string.Empty;
                // Only show for Mitgliedsantrag documents
                if (!fileName.Contains("Mitgliedsantrag", StringComparison.OrdinalIgnoreCase))
                {
                    _deleteButton.IsVisible = false;
                    return;
                }

                // Do not show delete for uploaded documents; signed detection via metadata is not available here.

                // Also hide if already uploaded / not purely local
                var status = KGV.Maui.Services.Documents.LocalDocumentService.GetStatus(new KGV.Core.Models.DocumentInfo { Dateiname = fileName, Name = fileName });
                if (status.IsUploaded)
                {
                    _deleteButton.IsVisible = false;
                    return;
                }

                _deleteButton.IsVisible = true;
            }
            catch
            {
                _deleteButton.IsVisible = false;
            }
        }

        private async Task LoadPdfIntoWebViewAsync()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    await DisplayAlert("Fehler", "Die Datei wurde nicht gefunden.", "OK");
                    return;
                }

#if ANDROID
                // On Android prefer the native PdfRenderer-based activity to avoid WebView/pdf.js instability.
                try
                {
                    var context = global::Android.App.Application.Context;
                    var file = new Java.IO.File(_filePath);
                    if (file.Exists())
                    {
                        var authority = "de.kgv.oberrothenbach.fileProvider";
                        var uri = global::AndroidX.Core.Content.FileProvider.GetUriForFile(context, authority, file);
                        var intent = new global::Android.Content.Intent(context, typeof(KGV.Maui.Platforms.Android.NativePdfViewerActivity));
                        intent.SetData(uri);
                        intent.AddFlags(global::Android.Content.ActivityFlags.GrantReadUriPermission | global::Android.Content.ActivityFlags.NewTask);
                        context.StartActivity(intent);

                        // Close this page (it was likely pushed modally) since native viewer handles preview.
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            try { await Navigation.PopModalAsync(); } catch { }
                        });

                        return;
                    }
                }
                catch
                {
                    // fall back to embedded WebView if native activity cannot be started
                }
#endif

                var bytes = await File.ReadAllBytesAsync(_filePath);
                var base64 = Convert.ToBase64String(bytes);

                // PDF.js via CDN - minimal viewer HTML
                var template = @"<!doctype html>
<html>
<head>
  <meta charset='utf-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1.0'>
  <title>PDF Viewer (POC)</title>
  <style>body,html{{height:100%;margin:0}}#toolbar{{background:#f3f3f3;padding:6px;display:flex;gap:8px;align-items:center}}#viewerContainer{{height:calc(100% - 44px);overflow:auto;background:#666}}</style>
      <script>
        // Capture console.error and window.onerror for diagnostics when running inside WebView
        window._kgv_consoleErrors = [];
        (function(){
          var origConsoleError = console.error.bind(console);
          console.error = function(){ window._kgv_consoleErrors.push(Array.from(arguments).join(' ')); origConsoleError.apply(console, arguments); };
          window.onerror = function(msg, src, line, col, err){ try{ window._kgv_consoleErrors.push(msg + ' @' + src + ':' + line + ':' + col); }catch(e){} };
        })();
      </script>
      <script src='https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.10.110/pdf.min.js'></script>
</head>
<body>
  <div id='toolbar'>
    <button id='prev'>Prev</button>
    <button id='next'>Next</button>
    <span>Page: <span id='page_num'>1</span> / <span id='page_count'>--</span></span>
    <label>Zoom: <select id='zoom'><option value='0.5'>50%</option><option value='1' selected>100%</option><option value='1.5'>150%</option><option value='2'>200%</option></select></label>
  </div>
  <div id='viewerContainer'><canvas id='pdf-canvas' style='display:block;margin:0 auto;'></canvas></div>

  <script>
    (function(){{
      const base64 = '__BASE64__';
      const pdfData = atob(base64);
      const len = pdfData.length;
      const uint8 = new Uint8Array(len);
      for (let i = 0; i < len; i++) uint8[i] = pdfData.charCodeAt(i);

      pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.10.110/pdf.worker.min.js';
      let pdfDoc = null, pageNum = 1, scale = 1.0; 
      const canvas = document.getElementById('pdf-canvas');
      const ctx = canvas.getContext('2d');

      function renderPage(num){{
        pdfDoc.getPage(num).then(function(page){{
          const viewport = page.getViewport({{ scale: scale }});
          canvas.height = viewport.height;
          canvas.width = viewport.width;
          const renderContext = {{ canvasContext: ctx, viewport: viewport }};
          page.render(renderContext);
          document.getElementById('page_num').textContent = pageNum;
        }});
      }}

      pdfjsLib.getDocument({{data: uint8}}).promise.then(function(pdf){{
        pdfDoc = pdf;
        document.getElementById('page_count').textContent = pdf.numPages;
        renderPage(pageNum);
      }}).catch(function(err){{
        document.body.innerHTML = '<p style=\'color:red;padding:12px\'>' + err.message + '</p>';
      }});

      document.getElementById('prev').addEventListener('click', function(){{ if(pageNum <=1) return; pageNum--; renderPage(pageNum);}});
      document.getElementById('next').addEventListener('click', function(){{ if(pageNum >= pdfDoc.numPages) return; pageNum++; renderPage(pageNum);}});
      document.getElementById('zoom').addEventListener('change', function(e){{ scale = parseFloat(e.target.value); renderPage(pageNum); }});
    }})();
  </script>
</body>
</html>";

                var html = template.Replace("__BASE64__", base64);
                _webView.Source = new HtmlWebViewSource { Html = html };

                // Determine delete button visibility based on file content and status
                _ = UpdateDeleteButtonVisibilityAsync(bytes);

                // Kurze Prüfung, ob der eingebettete pdf.js-Viewer erfolgreich geladen hat.
                // Manche Android-WebViews blockieren das Laden des pdf.worker oder CDN-Assets;
                // dann bleibt die Anzeige weiß. Wir prüfen nach kurzer Wartezeit und bieten
                // als Fallback an, das PDF extern mit dem System-Viewer zu öffnen.
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // initial wait longer to allow pdf.js and worker to load on slow devices/net
                        await Task.Delay(3000);
                        string? pageCount = null;
                        try
                        {
                            pageCount = await _webView.EvaluateJavaScriptAsync("(function(){var el=document.getElementById('page_count'); return el ? el.textContent : ''; })();");
                        }
                        catch { pageCount = null; }

                        // Retry once after extra wait if initial check failed
                        if (string.IsNullOrWhiteSpace(pageCount) || pageCount.Contains("--") || pageCount.Contains("undefined"))
                        {
                            await Task.Delay(1500);
                            try { pageCount = await _webView.EvaluateJavaScriptAsync("(function(){var el=document.getElementById('page_count'); return el ? el.textContent : ''; })();"); } catch { pageCount = null; }
                        }

                        if (string.IsNullOrWhiteSpace(pageCount) || pageCount.Contains("--") || pageCount.Contains("undefined"))
                        {
                            // Collect console errors from the WebView for diagnostics
                            string? consoleErrors = null;
                            try { consoleErrors = await _webView.EvaluateJavaScriptAsync("(function(){ return window._kgv_consoleErrors ? window._kgv_consoleErrors.join('\n') : ''; })();"); } catch { consoleErrors = null; }

                            // Log diagnostics
                            try { System.Diagnostics.Debug.WriteLine($"PdfViewer: embedded viewer failed to initialize. pageCount=<{pageCount}>. consoleErrors=<{consoleErrors}>"); } catch { }

                            // On Android prefer the native PdfRenderer activity as fallback without showing the generic dialog.
                            if (DeviceInfo.Platform == DevicePlatform.Android)
                            {
                                await MainThread.InvokeOnMainThreadAsync(async () =>
                                {
                                    try
                                    {
#if ANDROID
                                        try
                                        {
                                            var context = global::Android.App.Application.Context;
                                            var file = new Java.IO.File(_filePath);
                                            if (file.Exists())
                                            {
                                                var authority = "de.kgv.oberrothenbach.fileProvider";
                                                var uri = global::AndroidX.Core.Content.FileProvider.GetUriForFile(context, authority, file);
                                                var intent = new global::Android.Content.Intent(context, typeof(KGV.Maui.Platforms.Android.NativePdfViewerActivity));
                                                intent.SetData(uri);
                                                intent.AddFlags(global::Android.Content.ActivityFlags.GrantReadUriPermission | global::Android.Content.ActivityFlags.NewTask);
                                                context.StartActivity(intent);
                                                try { await Navigation.PopModalAsync(); } catch { }
                                                return;
                                            }
                                        }
                                        catch { }
#endif
                                        // Fallback to external system viewer if native activity cannot be started
                                        try
                                        {
                                            await Launcher.Default.OpenAsync(new OpenFileRequest(Title ?? "Dokument", new ReadOnlyFile(_filePath)));
                                        }
                                        catch (Exception ex)
                                        {
                                            try { await DisplayAlert("Fehler beim Öffnen", ex.Message, "OK"); } catch { }
                                        }
                                    }
                                    catch { }
                                });
                            }
                            else
                            {
                                // Non-Android: ask the user whether to open externally
                                await MainThread.InvokeOnMainThreadAsync(async () =>
                                {
                                    try
                                    {
                                        var open = await DisplayAlert("Vorschau nicht verfügbar", "Die integrierte PDF-Vorschau kann dieses Dokument nicht darstellen. Soll das Dokument extern geöffnet werden?", "Ja", "Nein");
                                        if (open)
                                        {
                                            try
                                            {
                                                await Launcher.Default.OpenAsync(new OpenFileRequest(Title ?? "Dokument", new ReadOnlyFile(_filePath)));
                                            }
                                            catch (Exception ex)
                                            {
                                                await DisplayAlert("Fehler beim Öffnen", ex.Message, "OK");
                                            }
                                        }
                                    }
                                    catch { }
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        try { System.Diagnostics.Debug.WriteLine($"PdfViewer: diagnostic task failed: {ex.Message}"); } catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Fehler beim Laden", ex.Message, "OK");
            }
        }

        private async Task OnSignClicked()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    await DisplayAlert("Fehler", "Die Datei wurde nicht gefunden.", "OK");
                    return;
                }

                var fileName = Path.GetFileName(_filePath) ?? string.Empty;
                // Bestimme Platzhalter je nach Dokumenttyp (vereinfachte Heuristik)
                IReadOnlyList<KGV.Core.Models.SignaturePlaceholder> placeholders = Array.Empty<KGV.Core.Models.SignaturePlaceholder>();
                if (fileName.Contains("Mitgliedsantrag", StringComparison.OrdinalIgnoreCase))
                {
                    placeholders = KGV.Core.Utilities.MitgliedsantragDokumentFactory.GetSignaturePlaceholders();
                }
                else if (fileName.Contains("Pachtvertrag", StringComparison.OrdinalIgnoreCase))
                {
                    placeholders = KGV.Core.Utilities.PachtvertragDokumentFactory.GetSignaturePlaceholders();
                }

                if (placeholders == null || placeholders.Count == 0)
                {
                    await DisplayAlert("Signatur", "Keine Signaturplätze für dieses Dokument erkannt (POC).", "OK");
                    return;
                }

                var originalBytes = await File.ReadAllBytesAsync(_filePath);
                var captures = new List<(KGV.Core.Models.SignaturePlaceholder placeholder, KGV.Core.Models.DigitalSignatureCapture capture)>();

                foreach (var placeholder in placeholders)
                {
                    var docInfo = new KGV.Core.Models.DocumentInfo
                    {
                        Title = Title ?? string.Empty,
                        Dateiname = fileName,
                        Name = fileName,
                        MimeType = "application/pdf",
                        StoragePath = _filePath
                    };

                    var signPage = new VertragsSignaturPage(docInfo, placeholder.Name);
                    await Navigation.PushModalAsync(new NavigationPage(signPage));
                    var capture = await signPage.WaitForResultAsync();
                    if (capture == null)
                    {
                        await DisplayAlert("Signatur", "Signaturvorgang abgebrochen.", "OK");
                        return;
                    }

                    captures.Add((placeholder, capture));
                }

                // Insert signatures into PDF
                var updated = KGV.Core.Utilities.SignedVertragsdokumentPdfBuilder.InsertSignaturesIntoPdf(originalBytes, captures);

                // Save updated PDF locally (overwrite)
                try
                {
                    await KGV.Maui.Services.Documents.LocalDocumentService.SavePersistentCopyAsync(updated, fileName);
                }
                catch
                {
                    // ignore write failures
                }

                await DisplayAlert("Signatur", "Signaturen in PDF übernommen und lokal gespeichert.", "OK");

                // Reload viewer
                await LoadPdfIntoWebViewAsync();
                try
                {
                    var bytes = await File.ReadAllBytesAsync(_filePath);
                    _deleteButton.IsVisible = false; // signed -> do not allow delete
                    _ = UpdateDeleteButtonVisibilityAsync(bytes);
                }
                catch { }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Fehler", ex.Message, "OK");
            }
        }

        private async Task OnUploadClicked()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    await DisplayAlert("Fehler", "Die Datei wurde nicht gefunden.", "OK");
                    return;
                }

                var fileName = Path.GetFileName(_filePath) ?? string.Empty;
                var bytes = await File.ReadAllBytesAsync(_filePath);

                // Simple heuristic: Mitgliedsantrag -> upload as member document
                if (fileName.Contains("Mitgliedsantrag", StringComparison.OrdinalIgnoreCase))
                {
                    if (!_mitgliedId.HasValue || _mitgliedId.Value <= 0)
                    {
                        await DisplayAlert("Upload", "Mitglieds-ID unbekannt. Bitte Dokument aus der Mitgliedsansicht hochladen.", "OK");
                        return;
                    }

                    var request = new KGV.Core.Models.DokumentUploadRequest
                    {
                        MitgliedId = _mitgliedId.Value,
                        Titel = "Mitgliedsantrag (signiert)",
                        FileName = fileName,
                        MimeType = "application/pdf",
                        FileContent = bytes
                    };

                    var result = await _supabaseService.CreateDokumentAsync(request);
                    if (!result.Success)
                    {
                        await DisplayAlert("Upload fehlgeschlagen", result.Message, "OK");
                        return;
                    }

                    await DisplayAlert("Upload", "Dokument erfolgreich hochgeladen.", "OK");

                    if (result.Document != null && result.Document.CanOpen)
                    {
                        var url = await _supabaseService.ResolveDokumentOpenUrlAsync(result.Document, 3600);
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            try { await Launcher.Default.OpenAsync(url); } catch { /* ignore */ }
                        }
                    }

                    return;
                }

                // Fallback: upload generic document if MitgliedId available
                if (_mitgliedId.HasValue && _mitgliedId.Value > 0)
                {
                    var request = new KGV.Core.Models.DokumentUploadRequest
                    {
                        MitgliedId = _mitgliedId.Value,
                        Titel = fileName,
                        FileName = fileName,
                        MimeType = "application/pdf",
                        FileContent = bytes
                    };

                    var result = await _supabaseService.CreateDokumentAsync(request);
                    if (!result.Success)
                    {
                        await DisplayAlert("Upload fehlgeschlagen", result.Message, "OK");
                        return;
                    }

                    await DisplayAlert("Upload", "Dokument erfolgreich hochgeladen.", "OK");
                    return;
                }

                await DisplayAlert("Upload", "Dieses Dokument kann nicht automatisch hochgeladen werden. Bitte verwenden Sie die Dokumente-Ansicht.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Fehler beim Upload", ex.Message, "OK");
            }
        }

        // Note: removed Pdf metadata inspection to avoid additional package dependency in MAUI project.

        private async Task OnDeleteClicked()
        {
            try
            {
                var fileName = Path.GetFileName(_filePath) ?? string.Empty;
                var confirm = await DisplayAlert("Antrag löschen?", "Soll der lokale Antrag unwiderruflich gelöscht werden? Nur löschen, wenn er noch nicht signiert oder hochgeladen wurde.", "Löschen", "Abbrechen");
                if (!confirm) return;

                var status = KGV.Maui.Services.Documents.LocalDocumentService.GetStatus(new KGV.Core.Models.DocumentInfo { Dateiname = fileName, Name = fileName });
                if (status.IsUploaded)
                {
                    await DisplayAlert("Löschen nicht möglich", "Dieses Dokument wurde bereits hochgeladen und kann nicht lokal gelöscht werden.", "OK");
                    return;
                }

                if (status.Exists)
                {
                    try { File.Delete(status.LocalPath); } catch (Exception ex) { await DisplayAlert("Fehler", ex.Message, "OK"); return; }
                }

                await DisplayAlert("Antrag gelöscht", "Der lokale Antrag wurde entfernt.", "OK");
                // Close viewer
                try { await Navigation.PopAsync(); } catch { try { await Navigation.PopModalAsync(); } catch { } }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Fehler beim Löschen", ex.Message, "OK");
            }
        }
    }
}
