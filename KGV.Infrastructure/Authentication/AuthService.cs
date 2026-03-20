using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Infrastructure.Models;
using KGV.Infrastructure.Supabase;
using Supabase;
using Supabase.Gotrue.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GotrueUserAttributes = Supabase.Gotrue.UserAttributes;

namespace KGV.Infrastructure.Authentication
{
    public class AuthService : IAuthService
    {
        private readonly ISupabaseClientFactory _clientFactory;
        private readonly ILogger<AuthService>? _logger;
        private global::Supabase.Client? _client;

        public AuthService(ISupabaseClientFactory clientFactory, ILogger<AuthService>? logger = null)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _logger = logger;
        }

        public bool IsVorstand { get; private set; } = false;
        public bool IsAdmin { get; private set; } = false;
        public string? CurrentUserId { get; private set; }

        /// <summary>
        /// Supabase Client initialisieren oder zurückgeben
        /// </summary>
        public async Task<global::Supabase.Client> GetClientAsync()
        {
            if (_client == null)
            {
                _client = await _clientFactory.CreateAsync();
            }
            return _client;
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _logger?.LogWarning("Login attempt rejected: missing email or password.");
                return false;
            }

            email = email.Trim();

            try
            {
                _logger?.LogInformation("SignIn attempt for {EmailMasked}", MaskEmail(email));

                var client = await GetClientAsync();
                if (client == null)
                {
                    _logger?.LogError("Supabase client is null in LoginAsync.");
                    return false;
                }

                var session = await client.Auth.SignIn(email: email, password: password);
                if (session == null)
                {
                    _logger?.LogWarning("SignIn returned null session for {EmailMasked}", MaskEmail(email));
                    return false;
                }

                var user = session.User;
                if (user == null || string.IsNullOrEmpty(user.Id))
                {
                    _logger?.LogWarning("SignIn succeeded but session.User is null or has no Id for {EmailMasked}", MaskEmail(email));
                    return false;
                }

                _logger?.LogInformation("SignIn successful for {EmailMasked}", MaskEmail(email));

                CurrentUserId = user.Id;

                // Rollen setzen – MitgliedRecord anhand AuthUserId (uuid) abrufen
                MitgliedRecord? userRecord = null;

                // user.Id ist string -> in Guid umwandeln, weil MitgliedRecord.AuthUserId = Guid?
                if (!Guid.TryParse(user.Id, out var userGuid))
                {
                    _logger?.LogWarning("User.Id is not a valid Guid: {UserId}", user.Id);
                    IsVorstand = false;
                    IsAdmin = false;
                    return true; // Login ist trotzdem ok, nur keine Rollen
                }

                try
                {
                    userRecord = await client
                        .From<MitgliedRecord>()
                        .Where(m => m.AuthUserId == userGuid)
                        .Single();
                }
                catch (Exception ex)
                {
                    // Query kann fehlschlagen, z.B. wenn kein Datensatz existiert
                    _logger?.LogInformation(ex, "No MitgliedRecord found or error while querying for user {UserId}", user.Id);
                }

                if (userRecord != null)
                {
                    var role = (userRecord.Role ?? string.Empty).Trim();

                    // akzeptiere alte/uneinheitliche Rollenwerte aus der DB
                    // (z.B. "Admin"/"Vorstand" vs. "admin"/"vorstand")
                    IsVorstand = string.Equals(role, "vorstand", StringComparison.OrdinalIgnoreCase);
                    IsAdmin = string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    IsVorstand = false;
                    IsAdmin = false;
                }

                return true;
            }
            catch (GotrueException ex)
            {
                _logger?.LogError(ex, "GotrueException during SignIn for {EmailMasked}: {Message}", MaskEmail(email), ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error during SignIn for {EmailMasked}", MaskEmail(email));
                return false;
            }
        }

        public async Task<List<AppUserDTO>> GetAppUsersAsync()
        {
            try
            {
                var client = await GetClientAsync();

                var appUsersResponse = await client.From<AppUserRecord>().Get();
                var membersResponse = await client.From<MitgliedRecord>().Get();

                var appUsers = appUsersResponse?.Models?.ToList() ?? new List<AppUserRecord>();
                var members = membersResponse?.Models?.ToList() ?? new List<MitgliedRecord>();

                var result = new Dictionary<Guid, AppUserDTO>();

                foreach (var appUser in appUsers)
                {
                    var member = members.FirstOrDefault(x => x.AuthUserId == appUser.UserId);
                    result[appUser.UserId] = CreateAppUserDto(appUser, member);
                }

                foreach (var member in members.Where(x => x.AuthUserId.HasValue))
                {
                    var authUserId = member.AuthUserId!.Value;
                    if (result.ContainsKey(authUserId))
                        continue;

                    result[authUserId] = CreateAppUserDto(appUser: null, member);
                }

                return result.Values
                    .OrderBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => x.Email, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "GetAppUsersAsync failed.");
                return new List<AppUserDTO>();
            }
        }

        public async Task<bool> ChangeEmailAsync(string newEmail)
        {
            if (string.IsNullOrWhiteSpace(newEmail))
                return false;

            try
            {
                var client = await GetClientAsync();
                await client.Auth.Update(new GotrueUserAttributes
                {
                    Email = newEmail.Trim()
                });

                return true;
            }
            catch (GotrueException ex)
            {
                _logger?.LogError(ex, "ChangeEmailAsync failed for {EmailMasked}: {Message}", MaskEmail(newEmail), ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ChangeEmailAsync failed for {EmailMasked}", MaskEmail(newEmail));
                return false;
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var client = await GetClientAsync();
                await client.Auth.ResetPasswordForEmail(email.Trim());
                return true;
            }
            catch (GotrueException ex)
            {
                _logger?.LogError(ex, "SendPasswordResetEmailAsync failed for {EmailMasked}: {Message}", MaskEmail(email), ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "SendPasswordResetEmailAsync failed for {EmailMasked}", MaskEmail(email));
                return false;
            }
        }

        private static AppUserDTO CreateAppUserDto(AppUserRecord? appUser, MitgliedRecord? member)
        {
            var authUserId = appUser?.UserId ?? member?.AuthUserId;
            var memberId = member?.Id;

            if (!memberId.HasValue && appUser?.MitgliedId is >= int.MinValue and <= int.MaxValue)
                memberId = (int)appUser.MitgliedId.Value;

            return new AppUserDTO
            {
                AuthUserId = authUserId,
                MitgliedId = memberId,
                Email = member?.Email ?? string.Empty,
                DisplayName = FormatDisplayName(member),
                Role = FirstNonEmpty(appUser?.Role, member?.Role),
                Aktiv = member?.Aktiv ?? true,
                EmailBestaetigt = false,
                CreatedAt = appUser?.CreatedAt
            };
        }

        private static string FormatDisplayName(MitgliedRecord? member)
        {
            if (member == null)
                return string.Empty;

            var displayName = $"{member.Vorname} {member.Name}".Trim();
            return string.IsNullOrWhiteSpace(displayName) ? (member.Email ?? string.Empty) : displayName;
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return string.Empty;
        }

        private static string MaskEmail(string? email)
        {
            if (string.IsNullOrEmpty(email))
                return "<empty>";

            var atIndex = email.IndexOf('@');
            if (atIndex > 1)
            {
                var domain = email.Substring(atIndex + 1);
                return $"{email[0]}***@{domain}";
            }

            if (email.Length > 3)
                return $"{email.Substring(0, 1)}***{email.Substring(email.Length - 1)}";

            return "***";
        }
    }
}