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

        }

        private sealed class EffectiveFachrechtRow
        {
            [JsonProperty("code")]
            public string? Code { get; set; }
            [JsonProperty("erlaubt")]
            public bool Erlaubt { get; set; }
            [JsonProperty("individuell_entzogen")]
            public bool IndividuellEntzogen { get; set; }
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

                                var rows = await client.Rpc<EffectiveFachrechtRow[]>("ap02_preview_effective_rights", new { });
                var known = typeof(FachrechteV2).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                    .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                    .Select(field => (string)field.GetRawConstantValue()!)
                    .ToHashSet(StringComparer.Ordinal);
                if (rows == null || rows.Length != 55 || known.Count != 55
                    || rows.Any(row => row == null || string.IsNullOrWhiteSpace(row.Code) || !known.Contains(row.Code!))
                    || rows.Select(row => row.Code).Distinct(StringComparer.Ordinal).Count() != 55)
                    throw new KgvAccessDeniedException();

                // ap02_can_read_mitglied sperrt auch Eigenzugriff bei individuellem Leseentzug.
                var ownReadDenied = rows.Any(row => row.Code == "mitglied.lesen" && row.IndividuellEntzogen);
                var mask = FachrechteV2EffectiveMask.FromNamedRights(
                    rows.Where(row => row.Erlaubt).Select(row => row.Code!),
                    ownReadDenied ? null : record.MitgliedId);
                return new UserContext(userId, UserRoles.Parse(record.Role), record.MitgliedId,
                    mask, PermissionFlags.None, PermissionFlags.None, PermissionFlags.None);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "GetUserContextAsync failed for user {UserId}", userId);
                throw;
            }
        }
    }
}
