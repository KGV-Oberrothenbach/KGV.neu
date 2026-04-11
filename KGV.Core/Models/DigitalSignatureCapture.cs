using System;
using System.Collections.Generic;
using System.Linq;

namespace KGV.Core.Models
{
    public sealed class DigitalSignatureCapture
    {
        public double CanvasWidth { get; init; }
        public double CanvasHeight { get; init; }
        public DateTime SignedAt { get; init; } = DateTime.Now;
        public IReadOnlyCollection<DigitalSignatureStroke> Strokes { get; init; } = Array.Empty<DigitalSignatureStroke>();

        public bool HasContent
            => CanvasWidth > 0
                && CanvasHeight > 0
                && Strokes.Any(stroke => stroke?.Points?.Count > 0);
    }

    public sealed class DigitalSignatureStroke
    {
        public IReadOnlyCollection<DigitalSignaturePoint> Points { get; init; } = Array.Empty<DigitalSignaturePoint>();
    }

    public sealed class DigitalSignaturePoint
    {
        public double X { get; init; }
        public double Y { get; init; }
    }
}