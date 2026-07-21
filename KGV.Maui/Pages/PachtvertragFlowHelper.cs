using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.IO;

namespace KGV.Maui.Pages;

internal static class PachtvertragFlowHelper
{
    public static async Task RunAsync(INavigation navigation, ISupabaseService supabaseService, int mitgliedId, int parzelleId, DateTime vertragsbeginn)
    {
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(supabaseService);

        PachtvertragDokumentRequest? initialRequest = null;
        while (true)
        {
            var request = await PromptPachtvertragRequestAsync(navigation, supabaseService, mitgliedId, parzelleId, vertragsbeginn, initialRequest);
            if (request == null)
                return;

            DokumentUploadRequest? previewUploadRequest;
            try
            {
                previewUploadRequest = await supabaseService.BuildPachtvertragPreviewAsync(request);
            }
            catch (Exception ex)
            {
                // provide more context for UI error messages
                throw new InvalidOperationException($"Pachtvertrag-Vorschau konnte nicht erzeugt werden: {ex.Message}", ex);
            }

            if (previewUploadRequest == null || (previewUploadRequest.FileContent?.Length ?? 0) <= 0)
                throw new InvalidOperationException("Pachtvertrag-Vorschau konnte nicht erzeugt werden.");

            var previewPage = new PachtvertragPreviewPage(previewUploadRequest);
            await navigation.PushModalAsync(new NavigationPage(previewPage));
            var previewDecision = await previewPage.WaitForResultAsync();
            if (previewDecision == PachtvertragPreviewDecision.BackToEditor)
            {
                initialRequest = request;
                continue;
            }

            if (previewDecision != PachtvertragPreviewDecision.ContinueToSignature)
                return;

            var sourceDocument = new DocumentInfo
            {
                Title = previewUploadRequest.Titel,
                Dateiname = previewUploadRequest.FileName,
                Name = previewUploadRequest.FileName,
                MimeType = previewUploadRequest.MimeType,
                // provide persistent local preview path for signing
                StoragePath = KGV.Maui.Services.Documents.DocumentStorage.GetPersistentFilePath(previewUploadRequest.FileName)
            };

            // Ensure the persistent preview file exists (Mitgliedsantrag flow schreibt eine persistente Kopie in der Preview-Seite).
            // Wenn das Schreiben beim Öffnen der Vorschau fehlgeschlagen ist, versuchen wir es hier erneut, damit die Signaturseite die Datei öffnen/verwenden kann.
            try
            {
                var persistentPath = sourceDocument.StoragePath;
                if (!string.IsNullOrWhiteSpace(persistentPath) && (previewUploadRequest.FileContent?.Length ?? 0) > 0)
                {
                    var dir = Path.GetDirectoryName(persistentPath)!;
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    if (!File.Exists(persistentPath))
                        File.WriteAllBytes(persistentPath, previewUploadRequest.FileContent);
                }
            }
            catch
            {
                // Ignoriere Schreibfehler hier; Preview funktioniert weiterhin. Fehler werden ggf. beim finalen Upload sichtbar.
            }

            var signatureCapture = await SignatureFlowHelper.CaptureSignatureAsync(navigation, sourceDocument, "Unterschrift Pächter/in", isLastSignature: !request.IstMinderjaehrig, forceLandscape: false);
            if (signatureCapture == null)
                return;

            DigitalSignatureCapture? gesetzlicherVertreterSignatureCapture = null;
            if (request.IstMinderjaehrig)
            {
                gesetzlicherVertreterSignatureCapture = await SignatureFlowHelper.CaptureSignatureAsync(navigation, sourceDocument, "Unterschrift gesetzliche/r Vertreter/in", isLastSignature: true, forceLandscape: false);
                if (gesetzlicherVertreterSignatureCapture == null)
                    return;
            }

            DokumentUploadResult? result = null;
            try
            {
                try { System.Diagnostics.Debug.WriteLine($"[PachtvertragFlow] signatureCapture present={signatureCapture != null}, hasContent={signatureCapture?.HasContent}, strokes={(signatureCapture?.Strokes?.Count ?? 0)}"); } catch { }
                try { System.Diagnostics.Debug.WriteLine($"[PachtvertragFlow] gesetzlicherVertreter present={gesetzlicherVertreterSignatureCapture != null}, hasContent={gesetzlicherVertreterSignatureCapture?.HasContent}, strokes={(gesetzlicherVertreterSignatureCapture?.Strokes?.Count ?? 0)}"); } catch { }

                result = await supabaseService.CreateSignedPachtvertragDokumentAsync(request, signatureCapture, gesetzlicherVertreterSignatureCapture);
                try { System.Diagnostics.Debug.WriteLine($"[PachtvertragFlow] CreateSignedPachtvertragDokumentAsync result: Success={result?.Success}, Message={result?.Message}"); } catch { }

                if (result == null || !result.Success)
                    throw new InvalidOperationException(result?.Message ?? "Unbekannter Fehler beim Speichern des signierten Pachtvertrags.");
            }
            catch (Exception ex)
            {
                try { System.Diagnostics.Debug.WriteLine($"[PachtvertragFlow] Create signed failed: {ex}"); } catch { }
                throw;
            }

            var document = result.Document;
            if (document?.CanOpen != true)
                return;

            var url = await supabaseService.ResolveDokumentOpenUrlAsync(document, 3600);
            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException("Pachtvertrag wurde gespeichert, konnte aber nicht direkt geöffnet werden.");

            await Launcher.Default.OpenAsync(url);
            return;
        }
    }

    private static async Task<PachtvertragDokumentRequest?> PromptPachtvertragRequestAsync(INavigation navigation, ISupabaseService supabaseService, int mitgliedId, int parzelleId, DateTime vertragsbeginn, PachtvertragDokumentRequest? initialRequest)
    {
        var member = await supabaseService.GetMitgliedByIdAsync(mitgliedId);
        if (member == null)
            throw new InvalidOperationException("Mitglied konnte nicht geladen werden.");

        var parzelle = (await supabaseService.GetAllParzellenAsync()).FirstOrDefault(x => x.Id == parzelleId);
        if (parzelle == null)
            throw new InvalidOperationException("Parzelle konnte nicht geladen werden.");

        var gesetzlicherVertreterAufloesung = await supabaseService.ResolveGesetzlicherVertreterAsync(mitgliedId, vertragsbeginn);
        IReadOnlyCollection<MitgliedRecord> vertreterMitglieder = gesetzlicherVertreterAufloesung.IstMinderjaehrig
            ? (await supabaseService.GetMitgliederAsync()).ToList()
            : Array.Empty<MitgliedRecord>();

        var dialogPage = new PachtvertragDialogPage(member, parzelle, vertragsbeginn, gesetzlicherVertreterAufloesung, vertreterMitglieder, initialRequest);
        await navigation.PushModalAsync(new NavigationPage(dialogPage));
        return await dialogPage.WaitForResultAsync();
    }
}
