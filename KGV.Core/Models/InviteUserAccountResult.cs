using System;

namespace KGV.Core.Models
{
    public sealed class InviteUserAccountResult
    {
        public bool Success { get; set; }
        public Guid? AuthUserId { get; set; }
        public string? Email { get; set; }
        public string? InviteLink { get; set; }
        public string? Message { get; set; }
    }
}
