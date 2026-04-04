using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace KGV.Infrastructure.Models
{
    [Table("app_user")]
    public sealed class AppUserInsertRecord : BaseModel
    {
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("mitglied_id")]
        public long? MitgliedId { get; set; }

        [Column("role")]
        public string? Role { get; set; }

        [Column("is_demo_account")]
        public bool IsDemoAccount { get; set; }

        [Column("permission_grants")]
        public long PermissionGrants { get; set; }

        [Column("permission_revocations")]
        public long PermissionRevocations { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
