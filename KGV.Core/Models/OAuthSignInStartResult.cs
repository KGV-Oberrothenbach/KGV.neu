namespace KGV.Core.Models
{
    public sealed class OAuthSignInStartResult
    {
        public bool Success { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? AuthorizationUrl { get; set; }
        public string? State { get; set; }
        public string? Message { get; set; }
    }
}
