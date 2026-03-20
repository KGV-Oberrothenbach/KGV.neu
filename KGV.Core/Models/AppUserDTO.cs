using System;

namespace KGV.Core.Models
{
    public sealed class AppUserDTO
    {
        public Guid? AuthUserId { get; set; }
        public int? MitgliedId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool Aktiv { get; set; }
        public bool EmailBestaetigt { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
