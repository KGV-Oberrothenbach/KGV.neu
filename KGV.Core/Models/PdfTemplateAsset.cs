using System;
using System.Collections.Generic;

namespace KGV.Core.Models;

public sealed class PdfTemplateAsset
{
    public string TemplateKey { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string FieldSpecVersion { get; init; } = string.Empty;
    public string RendererVersion { get; init; } = string.Empty;
    public byte[] PdfBytes { get; init; } = Array.Empty<byte>();
    public IReadOnlyList<PdfTemplateFieldSpec> Fields { get; init; } = Array.Empty<PdfTemplateFieldSpec>();
}

public sealed record PdfTemplateFieldSpec(string Name, string Kind, int Page, double X, double Y, double Width, double Height);
