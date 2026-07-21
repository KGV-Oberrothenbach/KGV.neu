using System;
using System.Diagnostics;
using System.Threading.Tasks;
using KGV.Core.Models;
using Microsoft.Maui.Controls;

namespace KGV.Maui.Pages
{
    internal static class SignatureFlowHelper
    {
        public static async Task<DigitalSignatureCapture?> CaptureSignatureAsync(INavigation navigation, DocumentInfo sourceDocument, string? unterschriftTitel, bool isLastSignature = true, bool forceLandscape = true)
        {
            if (navigation == null)
                throw new ArgumentNullException(nameof(navigation));

            var signaturPage = new VertragsSignaturPage(sourceDocument, unterschriftTitel, isLastSignature: isLastSignature, forceLandscape: forceLandscape);
            try { Debug.WriteLine($"[SignatureFlow] Push signatur modal. navigationHash={navigation.GetHashCode()} page={signaturPage.GetType().Name}"); } catch { }
            await navigation.PushModalAsync(new NavigationPage(signaturPage));
            try { Debug.WriteLine($"[SignatureFlow] Awaiting signatur WaitForResultAsync(). navigationHash={navigation.GetHashCode()}"); } catch { }
            var result = await signaturPage.WaitForResultAsync();
            return result;
        }
    }
}
