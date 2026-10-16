using KGV.Core.Utilities;

if (args.Length == 0 || args.Contains("--help", StringComparer.Ordinal))
{
    Console.WriteLine("""
PDF Template Publish Helper — local dry run only; it never contacts Supabase.

dotnet run --project tools/PdfTemplatePublishHelper -- \
  --template-key <key> --version <positive integer> --pdf <template.pdf> \
  --field-spec <field-spec.json> --field-spec-version <contract> \
  --renderer-version <contract> --expected-field-spec-version <contract> \
  --expected-renderer-version <contract> [--manifest <output.md>]
""");
    return;
}

try
{
    var values = ParseArguments(args);
    var request = new PdfTemplatePublishRequest(
        Required(values, "template-key"),
        int.TryParse(Required(values, "version"), out var version) ? version : 0,
        Required(values, "pdf"),
        Required(values, "field-spec"),
        Required(values, "field-spec-version"),
        Required(values, "renderer-version"),
        Required(values, "expected-field-spec-version"),
        Required(values, "expected-renderer-version"));
    var manifest = PdfTemplatePublishManifestBuilder.Build(request).ToMarkdown();
    if (values.TryGetValue("manifest", out var outputPath))
    {
        File.WriteAllText(outputPath, manifest);
        Console.WriteLine($"Lokales Publish-Manifest geschrieben: {Path.GetFullPath(outputPath)}");
    }
    else
    {
        Console.WriteLine(manifest);
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine("Publish-Vorbereitung abgebrochen: " + ex.Message);
    Environment.ExitCode = 1;
}

static Dictionary<string, string> ParseArguments(string[] arguments)
{
    var result = new Dictionary<string, string>(StringComparer.Ordinal);
    for (var index = 0; index < arguments.Length; index += 2)
    {
        if (!arguments[index].StartsWith("--", StringComparison.Ordinal) || index + 1 >= arguments.Length)
            throw new InvalidOperationException("Jede Option benötigt einen Wert.");
        var key = arguments[index][2..];
        if (!result.TryAdd(key, arguments[index + 1])) throw new InvalidOperationException($"Option --{key} wurde mehrfach angegeben.");
    }
    return result;
}

static string Required(IReadOnlyDictionary<string, string> values, string key)
    => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
        ? value
        : throw new InvalidOperationException($"Option --{key} fehlt.");
