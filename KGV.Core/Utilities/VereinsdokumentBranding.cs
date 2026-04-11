using System;
using System.IO;
using System.Linq;
using KGV.Core.Models;

namespace KGV.Core.Utilities
{
    public static class VereinsdokumentBranding
    {
        private static readonly Lazy<byte[]> LogoBytes = new(LoadLogoBytes);

        public static string VereinsName => ImpressumInfo.VereinsName;
        public static string VereinsRegister => ImpressumInfo.VereinsRegister;
        public static string VereinsEmail => ImpressumInfo.VereinsEmail;

        public static byte[] GetLogoBytes()
            => LogoBytes.Value;

        private static byte[] LoadLogoBytes()
        {
            var assembly = typeof(VereinsdokumentBranding).Assembly;
            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(x => x.EndsWith("kgv_logo.png", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(resourceName))
                throw new InvalidOperationException("Das Vereinslogo konnte nicht als eingebettete Ressource gefunden werden.");

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException("Das eingebettete Vereinslogo konnte nicht geöffnet werden.");
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
