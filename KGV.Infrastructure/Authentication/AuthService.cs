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
using System.Reflection;
using System.Threading.Tasks;
using GotrueUserAttributes = Supabase.Gotrue.UserAttributes;

namespace KGV.Infrastructure.Authentication
{
    public class AuthService : IAuthService
    {
        private readonly ISupabaseClientFactory _clientFactory;
        private readonly ILogger<AuthService>? _logger;
        private global::Supabase.Client? _client;
        private string? _verifiedOtpEmail;
        private string? _pendingEmailChangeTarget;

        public AuthService(ISupabaseClientFactory clientFactory, ILogger<AuthService>? logger = null)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _logger = logger;
        }

        public async Task<bool> RequestOtpAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                return await RequestRecoveryOtpAsync(email.Trim(), "first-login");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "RequestOtpAsync failed for {EmailMasked}", MaskEmail(email));
                return false;
            }
        }

        public async Task<bool> VerifyOtpAsync(string email, string code)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                _logger?.LogInformation("VerifyOtpAsync: missing email or code");
                return false;
            }

            try
            {
                var ok = await VerifyOtpInternalAsync(email.Trim(), code.Trim(), "Recovery");
                if (!ok)
                    return false;

                _verifiedOtpEmail = email.Trim();
                IsVorstand = false;
                IsAdmin = false;
                return true;
            }
            catch (GotrueException ex)
            {
                _logger?.LogError(ex, "VerifyOtpAsync failed for {EmailMasked}: {Message}", MaskEmail(email), ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "VerifyOtpAsync failed for {EmailMasked}", MaskEmail(email));
                return false;
            }
        }

        public async Task<bool> SetPasswordWithOtpAsync(string email, string code, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(newPassword))
                return false;

            if (newPassword.Length < 8)
                return false;

            var emailTrim = email.Trim();
            if (!string.Equals(_verifiedOtpEmail, emailTrim, StringComparison.OrdinalIgnoreCase) && !await VerifyOtpAsync(emailTrim, code))
                return false;

            try
            {
                var client = await GetClientAsync();
                await client.Auth.Update(new GotrueUserAttributes
                {
                    Password = newPassword
                });

                await TrySignOutAsync(client);
                ResetAuthState();
                _logger?.LogInformation("SetPasswordWithOtpAsync succeeded for {EmailMasked}", MaskEmail(email));
                return true;
            }
            catch (GotrueException ex)
            {
                _logger?.LogError(ex, "SetPasswordWithOtpAsync failed for {EmailMasked}: {Message}", MaskEmail(email), ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "SetPasswordWithOtpAsync failed for {EmailMasked}", MaskEmail(email));
                return false;
            }
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

                _verifiedOtpEmail = null;
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
                var orphanMembers = new List<AppUserDTO>();

                foreach (var appUser in appUsers)
                {
                    var member = members.FirstOrDefault(x => x.AuthUserId == appUser.UserId);
                    result[appUser.UserId] = CreateAppUserDto(appUser, member);
                }

                foreach (var member in members)
                {
                    if (member.AuthUserId.HasValue)
                    {
                        var authUserId = member.AuthUserId.Value;
                        if (result.ContainsKey(authUserId))
                            continue;

                        result[authUserId] = CreateAppUserDto(appUser: null, member);
                        continue;
                    }

                    orphanMembers.Add(CreateAppUserDto(appUser: null, member));
                }

                return result.Values
                    .Concat(orphanMembers)
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

        public async Task<InviteUserAccountResult> InviteUserAsync(AppUserDTO user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var email = user.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                return new InviteUserAccountResult
                {
                    Success = false,
                    Message = "Für die Einladung fehlt eine E-Mail-Adresse."
                };
            }

            try
            {
                var authUserId = user.AuthUserId ?? await EnsureAuthUserForInviteAsync(email);
                if (!authUserId.HasValue)
                {
                    return new InviteUserAccountResult
                    {
                        Success = false,
                        Email = email,
                        Message = "Auth-Konto konnte für die Einladung nicht vorbereitet werden."
                    };
                }

                await EnsureMemberInviteMappingAsync(authUserId.Value, user.MitgliedId, email);
                await EnsureAppUserRecordAsync(authUserId.Value, user.MitgliedId, NormalizeRole(user.Role));

                var requested = await RequestRecoveryOtpAsync(email, "invite");
                return new InviteUserAccountResult
                {
                    Success = requested,
                    AuthUserId = authUserId,
                    Email = email,
                    Message = requested
                        ? "Einladungs-/Erstlogin-Code wurde versendet. Der Einstieg erfolgt jetzt über E-Mail + OTP + neues Passwort."
                        : "Einladungs-/Erstlogin-Code konnte nicht versendet werden."
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "InviteUserAsync failed for {EmailMasked}", MaskEmail(email));
                return new InviteUserAccountResult
                {
                    Success = false,
                    Email = email,
                    Message = "Einladung fehlgeschlagen. Details stehen im Log."
                };
            }
        }

        public async Task<bool> RequestEmailChangeAsync(string newEmail)
        {
            if (string.IsNullOrWhiteSpace(newEmail))
                return false;

            try
            {
                var emailTrim = newEmail.Trim();
                var client = await GetClientAsync();
                await client.Auth.Update(new GotrueUserAttributes
                {
                    Email = emailTrim
                });

                _pendingEmailChangeTarget = emailTrim;
                _logger?.LogInformation("Email change OTP requested for {EmailMasked}", MaskEmail(emailTrim));
                return true;
            }
            catch (GotrueException ex)
            {
                _logger?.LogError(ex, "RequestEmailChangeAsync failed for {EmailMasked}: {Message}", MaskEmail(newEmail), ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "RequestEmailChangeAsync failed for {EmailMasked}", MaskEmail(newEmail));
                return false;
            }
        }

        public async Task<bool> VerifyEmailChangeOtpAsync(string newEmail, string code)
        {
            if (string.IsNullOrWhiteSpace(newEmail) || string.IsNullOrWhiteSpace(code))
                return false;

            var emailTrim = newEmail.Trim();
            if (!string.Equals(_pendingEmailChangeTarget, emailTrim, StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                var ok = await VerifyOtpInternalAsync(emailTrim, code.Trim(), "EmailChange");
                if (!ok)
                    return false;

                _pendingEmailChangeTarget = null;
                return true;
            }
            catch (GotrueException ex)
            {
                _logger?.LogError(ex, "VerifyEmailChangeOtpAsync failed for {EmailMasked}: {Message}", MaskEmail(newEmail), ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "VerifyEmailChangeOtpAsync failed for {EmailMasked}", MaskEmail(newEmail));
                return false;
            }
        }

        public async Task<bool> ChangeEmailAsync(string newEmail)
        {
            return await RequestEmailChangeAsync(newEmail);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                return await RequestRecoveryOtpAsync(email.Trim(), "password-reset");
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

        private async Task<bool> VerifyOtpInternalAsync(string email, string code, string otpTypeName)
        {
            var client = await GetClientAsync();
            var authClient = client.Auth;
            var emailOtpType = authClient.GetType().Assembly
                .GetTypes()
                .FirstOrDefault(t => t.Name == "EmailOtpType" && t.IsEnum);

            if (emailOtpType == null)
            {
                _logger?.LogError("VerifyOtpInternalAsync failed: EmailOtpType enum not found.");
                return false;
            }

            var otpType = Enum.Parse(emailOtpType, otpTypeName, ignoreCase: true);
            var verifyOtpMethod = authClient.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m =>
                {
                    var parameters = m.GetParameters();
                    return m.Name == "VerifyOTP"
                           && parameters.Length == 3
                           && parameters[0].ParameterType == typeof(string)
                           && parameters[1].ParameterType == typeof(string)
                           && parameters[2].ParameterType == emailOtpType;
                });

            if (verifyOtpMethod == null)
            {
                _logger?.LogError("VerifyOtpInternalAsync failed: VerifyOTP(email, code, EmailOtpType) not found.");
                return false;
            }

            var result = verifyOtpMethod.Invoke(authClient, new[] { email, code, otpType });
            var session = await AwaitMethodResultAsync(result);
            var currentUserId = ExtractUserId(session) ?? authClient.CurrentUser?.Id ?? CurrentUserId;

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                _logger?.LogWarning("VerifyOtpInternalAsync returned no authenticated user for {EmailMasked}", MaskEmail(email));
                return false;
            }

            CurrentUserId = currentUserId;
            return true;
        }

        private async Task<bool> RequestRecoveryOtpAsync(string email, string flowKind)
        {
            ResetAuthState();

            var client = await GetClientAsync();
            await client.Auth.ResetPasswordForEmail(email);
            _logger?.LogInformation("Recovery OTP requested for {FlowKind} and {EmailMasked}", flowKind, MaskEmail(email));
            return true;
        }

        private async Task<Guid?> EnsureAuthUserForInviteAsync(string email)
        {
            var isolatedClient = new global::Supabase.Client(_clientFactory.Url, _clientFactory.Key);
            await isolatedClient.InitializeAsync();

            try
            {
                var signUpMethod = isolatedClient.Auth.GetType()
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "SignUp")
                            return false;

                        var parameters = m.GetParameters();
                        return parameters.Length >= 2
                               && parameters[0].ParameterType == typeof(string)
                               && parameters[1].ParameterType == typeof(string);
                    });

                if (signUpMethod == null)
                {
                    _logger?.LogError("EnsureAuthUserForInviteAsync failed: SignUp(email, password, ...) not found.");
                    return null;
                }

                var args = new object?[signUpMethod.GetParameters().Length];
                args[0] = email;
                args[1] = GenerateTemporaryPassword();
                for (var i = 2; i < args.Length; i++)
                    args[i] = null;

                var signUpResult = signUpMethod.Invoke(isolatedClient.Auth, args);
                var result = await AwaitMethodResultAsync(signUpResult);
                var userId = ExtractUserId(result) ?? isolatedClient.Auth.CurrentUser?.Id;
                if (!Guid.TryParse(userId, out var authUserId))
                    return null;

                return authUserId;
            }
            catch (GotrueException ex) when (ex.Message.Contains("already", StringComparison.OrdinalIgnoreCase))
            {
                _logger?.LogWarning(ex, "EnsureAuthUserForInviteAsync detected an existing auth user for {EmailMasked} without recoverable user id.", MaskEmail(email));
                return null;
            }
            finally
            {
                await TrySignOutAsync(isolatedClient);
            }
        }

        private async Task EnsureMemberInviteMappingAsync(Guid authUserId, int? mitgliedId, string email)
        {
            if (!mitgliedId.HasValue)
                return;

            var client = await GetClientAsync();
            await client
                .From<MitgliedRecord>()
                .Where(x => x.Id == mitgliedId.Value)
                .Set(x => x.AuthUserId, authUserId)
                .Set(x => x.Email, email)
                .Update();
        }

        private async Task EnsureAppUserRecordAsync(Guid authUserId, int? mitgliedId, string role)
        {
            var client = await GetClientAsync();
            var existing = await client
                .From<AppUserRecord>()
                .Where(x => x.UserId == authUserId)
                .Get();

            var record = existing?.Models?.FirstOrDefault();
            if (record != null)
            {
                await client
                    .From<AppUserRecord>()
                    .Where(x => x.UserId == authUserId)
                    .Set(x => x.MitgliedId, mitgliedId.HasValue ? (long?)mitgliedId.Value : null)
                    .Set(x => x.Role, role)
                    .Update();
                return;
            }

            await client.From<AppUserRecord>().Insert(new AppUserRecord
            {
                UserId = authUserId,
                MitgliedId = mitgliedId,
                Role = role,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        private static string GenerateTemporaryPassword()
        {
            return $"Tmp!{Guid.NewGuid():N}aA1";
        }

        private static string NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return "user";

            return role.Trim().ToLowerInvariant();
        }

        private void ResetAuthState()
        {
            _verifiedOtpEmail = null;
            _pendingEmailChangeTarget = null;
            CurrentUserId = null;
            IsVorstand = false;
            IsAdmin = false;
        }

        private async Task TrySignOutAsync(global::Supabase.Client client)
        {
            try
            {
                await client.Auth.SignOut();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "SignOut after password update failed.");
            }
        }

        private static async Task<object?> AwaitMethodResultAsync(object? invocationResult)
        {
            if (invocationResult is not Task task)
                return invocationResult;

            await task.ConfigureAwait(false);

            var taskType = task.GetType();
            return taskType.IsGenericType
                ? taskType.GetProperty("Result")?.GetValue(task)
                : null;
        }

        private static string? ExtractUserId(object? session)
        {
            if (session == null)
                return null;

            var sessionType = session.GetType();
            var user = sessionType.GetProperty("User")?.GetValue(session);
            return user?.GetType().GetProperty("Id")?.GetValue(user) as string;
        }
    }
}