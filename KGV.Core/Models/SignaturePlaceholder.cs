using System;

namespace KGV.Core.Models
{
    public sealed class SignaturePlaceholder
    {
        public string Name { get; init; } = string.Empty;
        public int Page { get; init; } = 1; // 1-based page index
        public double X { get; init; }
        public double Y { get; init; }
        public double Width { get; init; }
        public double Height { get; init; }
    }
}
