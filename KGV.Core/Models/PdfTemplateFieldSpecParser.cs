using System;
using System.Collections.Generic;
using System.Text.Json;

namespace KGV.Core.Models;

/// <summary>Parser and validator for canonical and legacy PDF field specifications.</summary>
public static class PdfTemplateFieldSpecParser
{
    public const string CanonicalSchemaVersion = "kgv-pdf-field-spec/v1";

    public static IReadOnlyList<PdfTemplateFieldSpec> Parse(byte[] json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.TryGetProperty("schema_version", out var schemaVersion)
            && !string.Equals(schemaVersion.GetString(), CanonicalSchemaVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Die Field-Spec verwendet eine nicht unterstützte schema_version.");
        if (root.TryGetProperty("coordinate_system", out var coordinateSystem)
            && !string.Equals(coordinateSystem.GetString(), "top-left-points", StringComparison.Ordinal))
            throw new InvalidOperationException("Die Field-Spec verwendet kein unterstütztes Koordinatensystem.");
        if (!root.TryGetProperty("settings", out var settings)
            || !settings.TryGetProperty("coordinateOrigin", out var coordinateOrigin)
            || !string.Equals(coordinateOrigin.GetString(), "topLeft", StringComparison.Ordinal))
            throw new InvalidOperationException("Die Field-Spec verwendet keinen unterstützten Koordinatenursprung.");
        if (!root.TryGetProperty("fields", out var fields) || fields.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Die Field-Spec enthält kein Feld-Array.");

        var names = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<PdfTemplateFieldSpec>();
        foreach (var field in fields.EnumerateArray())
        {
            if (field.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Die Field-Spec enthält ein ungültiges Feld.");
            var name = field.TryGetProperty("name", out var nameElement) ? nameElement.GetString()?.Trim() : null;
            if (string.IsNullOrEmpty(name) || !names.Add(name)) throw new InvalidOperationException("Jeder Field-Spec-Feldname muss vorhanden und eindeutig sein.");
            var type = field.TryGetProperty("type", out var typeElement) ? typeElement.GetString()?.Trim().ToLowerInvariant() : null;
            var multiline = type == "text" && field.TryGetProperty("flags", out var flags) && flags.TryGetProperty("multiline", out var legacyMultiline) && legacyMultiline.ValueKind == JsonValueKind.True;
            type = multiline ? "multiline" : type;
            if (type is not ("text" or "multiline" or "checkbox" or "signature")) throw new InvalidOperationException("Die Field-Spec enthält einen nicht unterstützten Feldtyp.");
            if (!field.TryGetProperty("page", out var pageElement) || !pageElement.TryGetInt32(out var page) || page < 1) throw new InvalidOperationException("Die Field-Spec-Seite muss eine positive Ganzzahl sein.");
            if (!field.TryGetProperty("rect", out var rect) || rect.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Die Field-Spec enthält ein Feld ohne Rechteck.");
            var x = ReadCoordinate(rect, "x", false); var y = ReadCoordinate(rect, "y", false);
            var width = ReadCoordinate(rect, "w", true); var height = ReadCoordinate(rect, "h", true);
            result.Add(new PdfTemplateFieldSpec(name, type, page, x, y, width, height));
        }
        if (result.Count == 0) throw new InvalidOperationException("Die Field-Spec enthält keine Felder.");
        return result;
    }

    private static double ReadCoordinate(JsonElement rect, string name, bool strictlyPositive)
    {
        if (!rect.TryGetProperty(name, out var value) || !value.TryGetDouble(out var number) || !double.IsFinite(number) || (strictlyPositive ? number <= 0 : number < 0))
            throw new InvalidOperationException("Die Field-Spec enthält ein ungültiges Rechteck.");
        return number;
    }
}
