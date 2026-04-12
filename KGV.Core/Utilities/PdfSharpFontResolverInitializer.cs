using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using PdfSharpCore.Fonts;

namespace KGV.Core.Utilities
{
    internal static class PdfSharpFontResolverInitializer
    {
        private static int _initialized;
        private static readonly object SyncRoot = new();
        private static readonly IFontResolver Resolver = new PlatformFontResolver();

        [ModuleInitializer]
        internal static void InitializeModule()
            => EnsureInitialized();

        public static void EnsureInitialized()
        {
            if (Volatile.Read(ref _initialized) == 1)
                return;

            lock (SyncRoot)
            {
                if (_initialized == 1)
                    return;

                GlobalFontSettings.FontResolver = Resolver;
                _initialized = 1;
            }
        }

        private sealed class PlatformFontResolver : IFontResolver
        {
            private const string RegularFaceName = "kgv-platform-regular";
            private const string BoldFaceName = "kgv-platform-bold";
            private static readonly Lazy<IReadOnlyDictionary<string, byte[]>> FontBytes = new(LoadFontBytes);

            public string DefaultFontName => "Arial";

            public byte[] GetFont(string faceName)
            {
                if (FontBytes.Value.TryGetValue(faceName, out var bytes))
                    return bytes;

                throw new InvalidOperationException($"PdfSharpCore konnte die angeforderte Schrift '{faceName}' nicht auflösen.");
            }

            public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
            {
                var faceName = isBold ? BoldFaceName : RegularFaceName;
                return new FontResolverInfo(faceName);
            }

            private static IReadOnlyDictionary<string, byte[]> LoadFontBytes()
            {
                var regular = LoadFontFile(GetRegularCandidates(), "regular");
                var bold = TryLoadFontFile(GetBoldCandidates()) ?? regular;

                return new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
                {
                    [RegularFaceName] = regular,
                    [BoldFaceName] = bold
                };
            }

            private static byte[] LoadFontFile(IEnumerable<string> candidates, string role)
            {
                var bytes = TryLoadFontFile(candidates);
                if (bytes != null)
                    return bytes;

                throw new InvalidOperationException($"PdfSharpCore konnte keine gültige Schriftdatei für '{role}' finden. Geprüfte Kandidaten: {string.Join(", ", candidates)}");
            }

            private static byte[]? TryLoadFontFile(IEnumerable<string> candidates)
            {
                foreach (var candidate in candidates.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!File.Exists(candidate))
                        continue;

                    var bytes = File.ReadAllBytes(candidate);
                    if (bytes.Length > 0)
                        return bytes;
                }

                return null;
            }

            private static IEnumerable<string> GetRegularCandidates()
            {
                if (OperatingSystem.IsAndroid())
                {
                    yield return "/system/fonts/Roboto-Regular.ttf";
                    yield return "/system/fonts/NotoSans-Regular.ttf";
                    yield return "/system/fonts/DroidSans.ttf";
                    yield break;
                }

                if (OperatingSystem.IsWindows())
                {
                    var fontsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
                    yield return Path.Combine(fontsDirectory, "arial.ttf");
                    yield return Path.Combine(fontsDirectory, "segoeui.ttf");
                    yield break;
                }

                yield return "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf";
                yield return "/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf";
                yield return "/System/Library/Fonts/Supplemental/Arial Unicode.ttf";
            }

            private static IEnumerable<string> GetBoldCandidates()
            {
                if (OperatingSystem.IsAndroid())
                {
                    yield return "/system/fonts/Roboto-Bold.ttf";
                    yield return "/system/fonts/NotoSans-Bold.ttf";
                    yield return "/system/fonts/DroidSans-Bold.ttf";
                    yield break;
                }

                if (OperatingSystem.IsWindows())
                {
                    var fontsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
                    yield return Path.Combine(fontsDirectory, "arialbd.ttf");
                    yield return Path.Combine(fontsDirectory, "segoeuib.ttf");
                    yield break;
                }

                yield return "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf";
                yield return "/usr/share/fonts/truetype/liberation2/LiberationSans-Bold.ttf";
                yield return "/System/Library/Fonts/Supplemental/Arial Bold.ttf";
            }
        }
    }
}
