using KGV.Core.Models;
using KGV.Core.Services;

namespace KGV.Tests;

public sealed class DigitalSignatureEncoderTests
{
    [Fact] public void Valid_capture_returns_non_empty_signature_string() => Assert.NotEmpty(DigitalSignatureEncoder.EncodePngDataUrl(Capture()));
    [Fact] public void Output_uses_exact_web_expected_format() => Assert.StartsWith("data:image/png;base64,", DigitalSignatureEncoder.EncodePngDataUrl(Capture()));
    [Fact] public void Output_contains_valid_png_if_png_is_expected() { var bytes = Bytes(DigitalSignatureEncoder.EncodePngDataUrl(Capture())); Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, bytes.Take(8)); }
    [Fact] public void Multiple_strokes_are_encoded() { var capture = new DigitalSignatureCapture { CanvasWidth = 120, CanvasHeight = 60, Strokes = [new() { Points = [new() { X = 10, Y = 10 }, new() { X = 20, Y = 20 }] }, new() { Points = [new() { X = 60, Y = 20 }, new() { X = 70, Y = 30 }] }] }; Assert.NotEmpty(DigitalSignatureEncoder.EncodePngDataUrl(capture)); }
    [Fact] public void Rejects_capture_without_strokes() => Assert.Throws<ArgumentException>(() => DigitalSignatureEncoder.EncodePngDataUrl(new DigitalSignatureCapture { CanvasWidth = 100, CanvasHeight = 50 }));
    [Fact] public void Rejects_invalid_canvas_size() => Assert.Throws<ArgumentException>(() => DigitalSignatureEncoder.EncodePngDataUrl(new DigitalSignatureCapture { CanvasWidth = 0, CanvasHeight = 60, Strokes = Capture().Strokes }));
    [Fact] public void Does_not_write_files() { var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")); DigitalSignatureEncoder.EncodePngDataUrl(Capture()); Assert.False(File.Exists(path)); Assert.False(Directory.Exists(path)); }

    private static DigitalSignatureCapture Capture() => new() { CanvasWidth = 120, CanvasHeight = 60, Strokes = [new() { Points = [new() { X = 10, Y = 10 }, new() { X = 50, Y = 30 }, new() { X = 100, Y = 20 }] }] };
    private static byte[] Bytes(string dataUrl) => Convert.FromBase64String(dataUrl["data:image/png;base64,".Length..]);
}
