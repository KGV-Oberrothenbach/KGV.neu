using System;
using System.Collections.Generic;
using System.Linq;
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

        PachtvertragDokumentRequest? initialRequest = null;
        while (true)
        {
            var request = await PromptPachtvertragRequestAsync(navigation, supabaseService, mitgliedId, parzelleId, vertragsbeginn, initialRequest);
            if (request == null)
                return;

            var previewUploadRequest = await supabaseService.BuildPachtvertragPreviewAsync(request);
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

            var signaturPage = new VertragsSignaturPage(sourceDocument, "Unterschrift Pächter/in");
            await navigation.PushModalAsync(new NavigationPage(signaturPage));
            var signatureCapture = await signaturPage.WaitForResultAsync();
            if (signatureCapture == null)
                return;

            DigitalSignatureCapture? gesetzlicherVertreterSignatureCapture = null;
            if (request.IstMinderjaehrig)
            {
                var vertreterSignaturPage = new VertragsSignaturPage(sourceDocument, "Unterschrift gesetzliche/r Vertreter/in");
                await navigation.PushModalAsync(new NavigationPage(vertreterSignaturPage));
                gesetzlicherVertreterSignatureCapture = await vertreterSignaturPage.WaitForResultAsync();
                if (gesetzlicherVertreterSignatureCapture == null)
                    return;
            }

            var result = await supabaseService.CreateSignedPachtvertragDokumentAsync(request, signatureCapture, gesetzlicherVertreterSignatureCapture);
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
