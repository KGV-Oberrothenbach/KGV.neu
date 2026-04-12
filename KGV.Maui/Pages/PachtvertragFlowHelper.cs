using System;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace KGV.Maui.Pages;

internal static class PachtvertragFlowHelper
{
    public static async Task RunAsync(INavigation navigation, ISupabaseService supabaseService, int mitgliedId, int parzelleId, DateTime vertragsbeginn)
    {
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(supabaseService);

        var previewUploadRequest = await supabaseService.BuildPachtvertragPreviewAsync(mitgliedId, parzelleId, vertragsbeginn);
        if (previewUploadRequest == null || (previewUploadRequest.FileContent?.Length ?? 0) <= 0)
            throw new InvalidOperationException("Pachtvertrag-Vorschau konnte nicht erzeugt werden.");

        while (true)
        {
            var previewPage = new PachtvertragPreviewPage(previewUploadRequest);
            await navigation.PushModalAsync(new NavigationPage(previewPage));
            var previewDecision = await previewPage.WaitForResultAsync();
            if (previewDecision == PachtvertragPreviewDecision.BackToContext)
                return;

            if (previewDecision != PachtvertragPreviewDecision.ContinueToSignature)
                return;

            var sourceDocument = new DocumentInfo
            {
                Title = previewUploadRequest.Titel,
                Dateiname = previewUploadRequest.FileName,
                Name = previewUploadRequest.FileName,
                MimeType = previewUploadRequest.MimeType,
                StoragePath = previewUploadRequest.FileName
            };

            var signaturPage = new VertragsSignaturPage(sourceDocument);
            await navigation.PushModalAsync(new NavigationPage(signaturPage));
            var signatureCapture = await signaturPage.WaitForResultAsync();
            if (signatureCapture == null)
                return;

            var result = await supabaseService.CreateSignedPachtvertragDokumentAsync(mitgliedId, parzelleId, vertragsbeginn, signatureCapture);
            if (!result.Success)
                throw new InvalidOperationException(result.Message);

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
}
