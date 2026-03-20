namespace KGV.Core.Models
{
    public sealed class PrepareAddUserResult
    {
        public bool Success { get; set; }
        public int? MitgliedId { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public bool RequiresInvite { get; set; }
        public string? Message { get; set; }
    }
}
