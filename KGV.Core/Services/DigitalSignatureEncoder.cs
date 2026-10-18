using System.IO.Compression;
using KGV.Core.Models;

namespace KGV.Core.Services;

public static class DigitalSignatureEncoder
{
    private const int MaxPixels = 16_777_216;
    private const double StrokeWidth = 2.2d;

    public static string EncodePngDataUrl(DigitalSignatureCapture? capture)
    {
        Validate(capture);
        var width = checked((int)Math.Ceiling(capture!.CanvasWidth));
        var height = checked((int)Math.Ceiling(capture.CanvasHeight));
        if ((long)width * height > MaxPixels)
            throw new ArgumentException("Die Signaturfläche ist zu groß.", nameof(capture));

        var pixels = new byte[checked(width * height * 4)];
        var scaleX = width / capture.CanvasWidth;
        var scaleY = height / capture.CanvasHeight;
        foreach (var stroke in capture.Strokes.Where(stroke => stroke?.Points?.Count > 0))
        {
            var points = stroke.Points.Select(point => (X: point.X * scaleX, Y: point.Y * scaleY)).ToArray();
            DrawDot(pixels, width, height, points[0].X, points[0].Y);
            for (var index = 1; index < points.Length; index++)
                DrawLine(pixels, width, height, points[index - 1], points[index]);
        }

        return "data:image/png;base64," + Convert.ToBase64String(CreatePng(width, height, pixels));
    }

    private static void Validate(DigitalSignatureCapture? capture)
    {
        if (capture is null) throw new ArgumentNullException(nameof(capture));
        if (!double.IsFinite(capture.CanvasWidth) || !double.IsFinite(capture.CanvasHeight) || capture.CanvasWidth <= 0 || capture.CanvasHeight <= 0)
            throw new ArgumentException("Die Signaturfläche hat ungültige Abmessungen.", nameof(capture));
        if (!capture.HasContent || !capture.Strokes.SelectMany(stroke => stroke?.Points ?? []).Any(point => double.IsFinite(point.X) && double.IsFinite(point.Y)))
            throw new ArgumentException("Die Signatur enthält keine verwertbaren Punkte.", nameof(capture));
    }

    private static void DrawLine(byte[] pixels, int width, int height, (double X, double Y) start, (double X, double Y) end)
    {
        var distance = Math.Max(Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y));
        var steps = Math.Max(1, (int)Math.Ceiling(distance));
        for (var step = 1; step <= steps; step++)
        {
            var ratio = step / (double)steps;
            DrawDot(pixels, width, height, start.X + (end.X - start.X) * ratio, start.Y + (end.Y - start.Y) * ratio);
        }
    }

    private static void DrawDot(byte[] pixels, int width, int height, double x, double y)
    {
        var radius = StrokeWidth / 2d;
        var minX = Math.Max(0, (int)Math.Floor(x - radius));
        var maxX = Math.Min(width - 1, (int)Math.Ceiling(x + radius));
        var minY = Math.Max(0, (int)Math.Floor(y - radius));
        var maxY = Math.Min(height - 1, (int)Math.Ceiling(y + radius));
        for (var pixelY = minY; pixelY <= maxY; pixelY++)
        for (var pixelX = minX; pixelX <= maxX; pixelX++)
        {
            var dx = pixelX + .5d - x;
            var dy = pixelY + .5d - y;
            if (dx * dx + dy * dy > radius * radius) continue;
            var offset = (pixelY * width + pixelX) * 4;
            pixels[offset] = 23; pixels[offset + 1] = 32; pixels[offset + 2] = 22; pixels[offset + 3] = 255;
        }
    }

    private static byte[] CreatePng(int width, int height, byte[] pixels)
    {
        using var stream = new MemoryStream();
        stream.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        using (var header = new MemoryStream()) { WriteInt32(header, width); WriteInt32(header, height); header.Write([8, 6, 0, 0, 0]); WriteChunk(stream, "IHDR", header.ToArray()); }
        using var raw = new MemoryStream();
        for (var row = 0; row < height; row++) { raw.WriteByte(0); raw.Write(pixels, row * width * 4, width * 4); }
        using var compressed = new MemoryStream();
        raw.Position = 0;
        using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
            raw.CopyTo(zlib);
        WriteChunk(stream, "IDAT", compressed.ToArray()); WriteChunk(stream, "IEND", []);
        return stream.ToArray();
    }

    private static void WriteChunk(Stream target, string type, byte[] data)
    {
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type); WriteInt32(target, data.Length); target.Write(typeBytes); target.Write(data);
        var crc = Crc32(typeBytes, data); WriteInt32(target, unchecked((int)crc));
    }

    private static void WriteInt32(Stream target, int value) => WriteInt32(target, unchecked((uint)value));
    private static void WriteInt32(Stream target, uint value) { target.WriteByte((byte)(value >> 24)); target.WriteByte((byte)(value >> 16)); target.WriteByte((byte)(value >> 8)); target.WriteByte((byte)value); }
    private static uint Crc32(byte[] type, byte[] data) { uint crc = 0xffffffff; foreach (var value in type.Concat(data)) { crc ^= value; for (var bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1) == 1 ? 0xedb88320 : 0); } return ~crc; }
}
