using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KGV.Core.Utilities
{
    public static class SimplePdfDocumentBuilder
    {
        public static byte[] BuildDocument(string title, IReadOnlyCollection<string> lines)
        {
            var normalizedTitle = NormalizeText(title);
            var normalizedLines = (lines ?? Array.Empty<string>())
                .Select(NormalizeText)
                .ToList();

            var contentBuilder = new StringBuilder();
            contentBuilder.AppendLine("BT");
            contentBuilder.AppendLine("/F1 18 Tf");
            contentBuilder.AppendLine("50 800 Td");
            contentBuilder.AppendLine($"({EscapePdfLiteral(normalizedTitle)}) Tj");
            contentBuilder.AppendLine("/F1 11 Tf");
            contentBuilder.AppendLine("0 -26 Td");

            foreach (var line in normalizedLines)
            {
                contentBuilder.AppendLine($"({EscapePdfLiteral(line)}) Tj");
                contentBuilder.AppendLine("0 -16 Td");
            }

            contentBuilder.AppendLine("ET");

            var contentBytes = Encoding.ASCII.GetBytes(contentBuilder.ToString());
            var objects = new[]
            {
                "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
                "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n",
                "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>\nendobj\n",
                "4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n"
            };

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
            var offsets = new List<long> { 0 };

            writer.Write(Encoding.ASCII.GetBytes("%PDF-1.4\n"));

            foreach (var obj in objects)
            {
                offsets.Add(stream.Position);
                writer.Write(Encoding.ASCII.GetBytes(obj));
            }

            offsets.Add(stream.Position);
            writer.Write(Encoding.ASCII.GetBytes($"5 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n"));
            writer.Write(contentBytes);
            writer.Write(Encoding.ASCII.GetBytes("endstream\nendobj\n"));

            var xrefStart = stream.Position;
            writer.Write(Encoding.ASCII.GetBytes($"xref\n0 {offsets.Count}\n"));
            writer.Write(Encoding.ASCII.GetBytes("0000000000 65535 f \n"));

            foreach (var offset in offsets.Skip(1))
                writer.Write(Encoding.ASCII.GetBytes($"{offset:0000000000} 00000 n \n"));

            writer.Write(Encoding.ASCII.GetBytes($"trailer\n<< /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xrefStart}\n%%EOF"));
            writer.Flush();

            return stream.ToArray();
        }

        private static string NormalizeText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Replace("Ä", "Ae", StringComparison.Ordinal)
                .Replace("Ö", "Oe", StringComparison.Ordinal)
                .Replace("Ü", "Ue", StringComparison.Ordinal)
                .Replace("ä", "ae", StringComparison.Ordinal)
                .Replace("ö", "oe", StringComparison.Ordinal)
                .Replace("ü", "ue", StringComparison.Ordinal)
                .Replace("ß", "ss", StringComparison.Ordinal)
                .Trim();
        }

        private static string EscapePdfLiteral(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);
        }
    }
}
