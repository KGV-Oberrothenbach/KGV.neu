using KGV.Core.Security;
using KGV.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Supabase;
using System;
using System.Linq;
using System.Threading.Tasks;
using KGV.Core.Interfaces;

namespace KGV.Infrastructure.Services
{
    public sealed class KgvAccessDeniedException : Exception
    {
        public KgvAccessDeniedException() : base("Für dieses Konto ist kein gültiger KGV-Zugang hinterlegt. Bitte wende dich an den Vorstand.") { }
    }

    public interface IUserContextService
    {
        Task<UserContext> GetUserContextAsync(Guid userId);
    }

    public sealed class UserContextService : IUserContextService
    {
        private sealed class CurrentAppUserContextRecord
        {
            [JsonProperty("user_id")]
            public Guid UserId { get; set; }

            [JsonProperty("mitglied_id")]
            public long? MitgliedId { get; set; }

            [JsonProperty("role")]
            public string? Role { get; set; }

            [JsonProperty("permission_grants")]
            public long? PermissionGrants { get; set; }

            [JsonProperty("permission_revocations")]
            public long? PermissionRevocations { get; set; }

            [JsonProperty("is_protected")]
            public bool IsProtected { get; set; }

            [JsonProperty("is_demo_account")]
            public bool IsDemoAccount { get; set; }
        }

        private readonly ISupabaseClientFactory _clientFactory;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<UserContextService>? _logger;

        public UserContextService(
            ISupabaseClientFactory clientFactory,
            IPermissionService permissionService,
            ILogger<UserContextService>? logger = null)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _logger = logger;
        }

        public async Task<UserContext> GetUserContextAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("userId must not be empty", nameof(userId));

            try
            {
                Client client = await _clientFactory.CreateAsync();

                var records = await client.Rpc<CurrentAppUserContextRecord[]>("current_app_user_context", new { })
                    ?? Array.Empty<CurrentAppUserContextRecord>();

                if (records.Length == 0)
                {
                    _logger?.LogWarning("No valid app_user context found for user {UserId}.", userId);
                    throw new KgvAccessDeniedException();
                }

                if (records.Length > 1)
                {
                    _logger?.LogWarning(
                        "Rejected app_user context for user {UserId}: RPC returned {RecordCount} records.",
                        userId,
                        records.Length);
                    throw new KgvAccessDeniedException();
                }

                var record = records[0];

                if (record.UserId != userId || !record.MitgliedId.HasValue || record.MitgliedId <= 0 ||
                    !string.Equals(record.Role, UserRoles.Admin, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(record.Role, UserRoles.Vorstand, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(record.Role, UserRoles.User, StringComparison.OrdinalIgnoreCase))
                    throw new KgvAccessDeniedException();

                return _permissionService.CreateContext(userId, record.Role, record.MitgliedId, record.PermissionGrants, record.PermissionRevocations, record.IsDemoAccount);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "GetUserContextAsync failed for user {UserId}", userId);
                throw;
            }
        }
    }
}
