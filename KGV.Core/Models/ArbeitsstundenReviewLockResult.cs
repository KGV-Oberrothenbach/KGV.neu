using System;

namespace KGV.Core.Models
{
    public sealed class ArbeitsstundenReviewLockResult
    {
        public bool Acquired { get; set; }
        public string? LockedByUserId { get; set; }
        public string? LockedByDisplayName { get; set; }
        public DateTime? LockedAt { get; set; }
    }
}
