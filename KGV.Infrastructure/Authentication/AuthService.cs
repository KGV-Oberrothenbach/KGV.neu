using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Infrastructure.Models;
using KGV.Infrastructure.Supabase;
using Supabase;
using Supabase.Gotrue.Exceptions;
using Supabase.Postgrest.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
                var emailTrim = email.Trim();
                var preparation = await EnsureOtpPreparationAsync(emailTrim, "first-login");
                if (!preparation.Success)
                {
                    _logger?.LogWarning("RequestOtpAsync blocked for {EmailMasked}: {Reason}", MaskEmail(emailTrim), preparation.Message);
                    return false;
                }

                return await RequestRecoveryOtpAsync(emailTrim, "first-login");
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
                var emailTrim = email.Trim();
                var ok = await VerifyOtpInternalAsync(emailTrim, code.Trim(), "Recovery");
                if (!ok)
                    return false;

                var repairResult = await RepairOtpContextAfterVerificationAsync(emailTrim);
                if (!repairResult.Success)
                {
                    _logger?.LogWarning("VerifyOtpAsync rejected verified OTP for {EmailMasked}: {Reason}", MaskEmail(emailTrim), repairResult.Message);
                    var client = await GetClientAsync();
                    await TrySignOutAsync(client);
                    ResetAuthState();
                    return false;
                }

                _verifiedOtpEmail = emailTrim;
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

        public async Task<string?> GetAccessTokenAsync()
        {
            var client = await GetClientAsync();
            if (client == null)
                return null;

            var auth = client.Auth;
            if (auth == null)
                return null;

            var authType = auth.GetType();
            var currentSession = authType.GetProperty("CurrentSession")?.GetValue(auth);
            if (currentSession == null)
            {
                var retrieveSessionMethod = authType.GetMethod("RetrieveSessionAsync");
                if (retrieveSessionMethod != null)
                    currentSession = await AwaitMethodResultAsync(retrieveSessionMethod.Invoke(auth, null));
            }

            return ExtractAccessToken(currentSession);
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
                var endpointContext = GetSupabaseEndpointContext();
                _logger?.LogInformation("SignIn attempt for {EmailMasked}", MaskEmail(email));
                _logger?.LogInformation("LOGIN_AUTH_TARGET {EndpointContext} for {EmailMasked}", endpointContext, MaskEmail(email));
                _logger?.LogInformation("LOGIN_AUTH_STAGE:GET_CLIENT for {EmailMasked}", MaskEmail(email));

                var client = await GetClientAsync();
                if (client == null)
                {
                    _logger?.LogWarning("LOGIN_FAIL_REASON:SUPABASE_CLIENT_NULL for {EmailMasked}", MaskEmail(email));
                    return false;
                }

                _logger?.LogInformation("LOGIN_AUTH_STAGE:SIGN_IN for {EmailMasked}", MaskEmail(email));
                var session = await client.Auth.SignIn(email: email, password: password);
                if (session == null)
                {
                    _logger?.LogWarning("LOGIN_FAIL_REASON:NULL_SESSION for {EmailMasked}", MaskEmail(email));
                    return false;
                }

                var user = session.User;
                if (user == null || string.IsNullOrEmpty(user.Id))
                {
                    _logger?.LogWarning("LOGIN_FAIL_REASON:MISSING_USER_ID for {EmailMasked}", MaskEmail(email));
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
                    _logger?.LogWarning("LOGIN_FAIL_REASON:INVALID_USER_GUID for {EmailMasked}", MaskEmail(email));
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
                    _logger?.LogWarning(ex, "LOGIN_ROLE_LOOKUP_WARN for {EmailMasked}: {MessageMasked}", MaskEmail(email), MaskDiagnosticMessage(ex.Message));
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
                _logger?.LogError(
                    ex,
                    "LOGIN_EXCEPTION:AUTH_SIGNIN_GOTRUE {EndpointContext} exceptionType={ExceptionType} innerType={InnerExceptionType} innerMessage={InnerMessageMasked} message={MessageMasked} for {EmailMasked}",
                    GetSupabaseEndpointContext(),
                    ex.GetType().FullName,
                    ex.InnerException?.GetType().FullName ?? "none",
                    MaskDiagnosticMessage(ex.InnerException?.Message),
                    MaskDiagnosticMessage(ex.Message),
                    MaskEmail(email));
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "LOGIN_EXCEPTION:AUTH_SIGNIN_UNEXPECTED {EndpointContext} exceptionType={ExceptionType} innerType={InnerExceptionType} innerMessage={InnerMessageMasked} message={MessageMasked} for {EmailMasked}",
                    GetSupabaseEndpointContext(),
                    ex.GetType().FullName,
                    ex.InnerException?.GetType().FullName ?? "none",
                    MaskDiagnosticMessage(ex.InnerException?.Message),
                    MaskDiagnosticMessage(ex.Message),
                    MaskEmail(email));
                return false;
            }
        }

        private string GetSupabaseEndpointContext()
        {
            if (!Uri.TryCreate(_clientFactory.Url, UriKind.Absolute, out var uri))
            {
                return "scheme=invalid host=invalid";
            }

            return $"scheme={uri.Scheme} host={uri.Host}";
        }

        private static string MaskDiagnosticMessage(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "unknown";
            }

            var sanitized = message.Replace("\r", " ").Replace("\n", " ").Trim();
            sanitized = Regex.Replace(sanitized, @"sb_publishable_[A-Za-z0-9_\-\.]+", "sb_publishable_***", RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"(access_token=)[^\s&]+", "$1***", RegexOptions.IgnoreCase);

            if (sanitized.Length > 160)
            {
                sanitized = sanitized[..160];
            }

            return sanitized;
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
                    var dto = CreateAppUserDto(appUser, member);
                    if (!OperationalDataFilter.IsOperationalAppUser(member, dto.DisplayName, dto.Email))
                        continue;

                    result[appUser.UserId] = dto;
                }

                foreach (var member in members)
                {
                    if (!OperationalDataFilter.IsOperationalMember(member))
                        continue;

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
            var mitgliedId = user.MitgliedId;
            var authUserId = user.AuthUserId;
            var inviteStep = "validate";
            var authUserVerified = false;
            var mappingAttempts = 0;
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
                _logger?.LogInformation(
                    "InviteUserAsync started for mitgliedId={MitgliedId} email={EmailMasked} authUserId={AuthUserId}",
                    mitgliedId,
                    MaskEmail(email),
                    authUserId);

                inviteStep = "prepare-invite-context";
                var preparation = await EnsureInvitePreparationAsync(user, email, "invite", allowDeferredOtpRepair: false);
                if (!preparation.Success)
                {
                    _logger?.LogWarning(
                        "InviteUserAsync aborted at step {InviteStep} for mitgliedId={MitgliedId} email={EmailMasked}: {Reason}",
                        inviteStep,
                        mitgliedId,
                        MaskEmail(email),
                        preparation.Message);

                    return new InviteUserAccountResult
                    {
                        Success = false,
                        Email = email,
                        Message = preparation.Message
                    };
                }

                authUserId = preparation.AuthUserId;
                authUserVerified = authUserId.HasValue;
                mappingAttempts = preparation.MappingAttempts;

                _logger?.LogInformation(
                    "InviteUserAsync auth user resolved for mitgliedId={MitgliedId} email={EmailMasked} authUserId={AuthUserId}",
                    mitgliedId,
                    MaskEmail(email),
                    authUserId);

                inviteStep = "request-recovery-otp";
                var requested = await RequestRecoveryOtpAsync(email, "invite");

                _logger?.LogInformation(
                    "InviteUserAsync finished for mitgliedId={MitgliedId} email={EmailMasked} authUserId={AuthUserId} authUserVerified={AuthUserVerified} mappingAttempts={MappingAttempts} otpRequested={OtpRequested}",
                    mitgliedId,
                    MaskEmail(email),
                    authUserId,
                    authUserVerified,
                    mappingAttempts,
                    requested);

                return new InviteUserAccountResult
                {
                    Success = requested,
                    AuthUserId = authUserId,
                    Email = email,
                    Message = requested
                        ? "Einladungs-/Erstlogin-Code wurde versendet. Einstieg über KGV-App oder PC-Login mit E-Mail + OTP + neuem Passwort."
                        : "Einladungs-/Erstlogin-Code konnte nicht versendet werden."
                };
            }
            catch (PostgrestException ex) when (IsMemberAuthUserForeignKeyFailure(ex))
            {
                _logger?.LogError(
                    ex,
                    "InviteUserAsync failed at step {InviteStep} for mitgliedId={MitgliedId} email={EmailMasked} authUserId={AuthUserId} authUserVerified={AuthUserVerified} mappingAttempts={MappingAttempts}. {PostgrestDetail}",
                    inviteStep,
                    mitgliedId,
                    MaskEmail(email),
                    authUserId,
                    authUserVerified,
                    mappingAttempts,
                    ExtractPostgrestRelevantMessage(ex));

                return new InviteUserAccountResult
                {
                    Success = false,
                    Email = email,
                    Message = "Einladung fehlgeschlagen, weil der Auth-User beim Verknüpfen noch nicht belastbar verfügbar war. Bitte erneut versuchen."
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "InviteUserAsync failed at step {InviteStep} for mitgliedId={MitgliedId} email={EmailMasked} authUserId={AuthUserId} authUserVerified={AuthUserVerified} mappingAttempts={MappingAttempts}",
                    inviteStep,
                    mitgliedId,
                    MaskEmail(email),
                    authUserId,
                    authUserVerified,
                    mappingAttempts);
                return new InviteUserAccountResult
                {
                    Success = false,
                    Email = email,
                    Message = "Einladung fehlgeschlagen. Details stehen im Log."
                };
            }
        }

        public async Task<bool> RemoveUserAsync(AppUserDTO user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (!user.AuthUserId.HasValue && !user.MitgliedId.HasValue)
                return false;

            try
            {
                var client = await GetClientAsync();

                if (user.MitgliedId.HasValue)
                {
                    await client
                        .From<MitgliedRecord>()
                        .Where(x => x.Id == user.MitgliedId.Value)
                        .Set(x => x.AuthUserId, (Guid?)null)
                        .Update();
                }

                if (user.AuthUserId.HasValue)
                {
                    await client
                        .From<AppUserRecord>()
                        .Where(x => x.UserId == user.AuthUserId.Value)
                        .Delete();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "RemoveUserAsync failed for auth user {AuthUserId} and mitglied {MitgliedId}", user.AuthUserId, user.MitgliedId);
                return false;
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
                var emailTrim = email.Trim();
                var preparation = await EnsureOtpPreparationAsync(emailTrim, "password-reset");
                if (!preparation.Success)
                {
                    _logger?.LogWarning("SendPasswordResetEmailAsync blocked for {EmailMasked}: {Reason}", MaskEmail(emailTrim), preparation.Message);
                    return false;
                }

                return await RequestRecoveryOtpAsync(emailTrim, "password-reset");
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
                Role = FirstNonEmpty(member?.Role, appUser?.Role),
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

        private async Task<AuthUserPreparationResult> EnsureAuthUserForInviteAsync(string email)
        {
            var resolvedExistingUserId = await ResolveExistingAuthUserIdByEmailAsync(email);
            if (resolvedExistingUserId.HasValue)
            {
                return AuthUserPreparationResult.Resolved(resolvedExistingUserId.Value);
            }

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
                    return AuthUserPreparationResult.Fail("Auth-Konto konnte nicht vorbereitet werden.");
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
                {
                    var fallbackAuthUserId = await ResolveExistingAuthUserIdByEmailAsync(email);
                    return fallbackAuthUserId.HasValue
                        ? AuthUserPreparationResult.Resolved(fallbackAuthUserId.Value)
                        : AuthUserPreparationResult.Fail("Auth-Konto konnte nicht vorbereitet werden.");
                }

                return AuthUserPreparationResult.Resolved(authUserId);
            }
            catch (GotrueException ex) when (ex.Message.Contains("already", StringComparison.OrdinalIgnoreCase))
            {
                _logger?.LogWarning(ex, "EnsureAuthUserForInviteAsync detected an existing auth user for {EmailMasked}. Trying RPC lookup.", MaskEmail(email));
                var existingAuthUserId = await ResolveExistingAuthUserIdByEmailAsync(email);
                return existingAuthUserId.HasValue
                    ? AuthUserPreparationResult.Resolved(existingAuthUserId.Value)
                    : AuthUserPreparationResult.Fail("Bestehendes Auth-Konto konnte nicht belastbar aufgelöst werden.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "EnsureAuthUserForInviteAsync failed for {EmailMasked}", MaskEmail(email));
                return AuthUserPreparationResult.Fail("Auth-Konto konnte nicht vorbereitet werden.");
            }
            finally
            {
                await TrySignOutAsync(isolatedClient);
            }
        }

        private async Task<OtpPreparationResult> EnsureOtpPreparationAsync(string email, string flowKind)
        {
            var memberResolution = await TryResolveOperationalMemberAsync(email, null);
            if (!memberResolution.Success || memberResolution.Member == null)
            {
                return OtpPreparationResult.Fail(memberResolution.Message);
            }

            var member = memberResolution.Member;
            var authUserId = member.AuthUserId ?? await ResolveAuthUserIdFromExistingMappingsAsync(member.Id, email);
            if (!authUserId.HasValue)
            {
                return OtpPreparationResult.Fail("Für diese E-Mail-Adresse ist aktuell kein vorbereiteter App-Zugang vorhanden.");
            }

            var mappingAttempts = await EnsureMemberInviteMappingAsync(authUserId.Value, member.Id, email);
            await EnsureAppUserRecordAsync(authUserId.Value, member.Id, NormalizeRole(member.Role));

            _logger?.LogInformation(
                "EnsureOtpPreparationAsync verified prepared OTP context for flowKind={FlowKind} email={EmailMasked} mitgliedId={MitgliedId} authUserId={AuthUserId} mappingAttempts={MappingAttempts}",
                flowKind,
                MaskEmail(email),
                member.Id,
                authUserId.Value,
                mappingAttempts);

            return OtpPreparationResult.Ok();
        }

        private async Task<InvitePreparationResult> EnsureInvitePreparationAsync(AppUserDTO user, string email, string flowKind, bool allowDeferredOtpRepair)
        {
            var memberResolution = await TryResolveOperationalMemberAsync(email, user.MitgliedId);
            if (!memberResolution.Success || memberResolution.Member == null)
            {
                return InvitePreparationResult.Fail(memberResolution.Message);
            }

            var member = memberResolution.Member;
            var normalizedRole = NormalizeRole(FirstNonEmpty(user.Role, member.Role));
            var authUserId = user.AuthUserId ?? member.AuthUserId ?? await ResolveAuthUserIdFromExistingMappingsAsync(member.Id, email);
            var mappingAttempts = 0;

            if (authUserId.HasValue)
            {
                mappingAttempts = await EnsureMemberInviteMappingAsync(authUserId.Value, member.Id, email);
                await EnsureAppUserRecordAsync(authUserId.Value, member.Id, normalizedRole);
                return InvitePreparationResult.Ok(authUserId.Value, member.Id, normalizedRole, mappingAttempts);
            }

            var authPreparation = await EnsureAuthUserForInviteAsync(email);
            if (authPreparation.AuthUserId.HasValue)
            {
                authUserId = authPreparation.AuthUserId.Value;
                mappingAttempts = await EnsureMemberInviteMappingAsync(authUserId.Value, member.Id, email);
                await EnsureAppUserRecordAsync(authUserId.Value, member.Id, normalizedRole);
                return InvitePreparationResult.Ok(authUserId.Value, member.Id, normalizedRole, mappingAttempts);
            }

            if (allowDeferredOtpRepair && authPreparation.DeferredOtpRepairRequired)
            {
                return InvitePreparationResult.OkDeferred(member.Id, normalizedRole, "Bestehendes Auth-Konto wird bei der OTP-Bestätigung vervollständigt.");
            }

            return InvitePreparationResult.Fail(authPreparation.Message);
        }

        private async Task<OtpVerificationRepairResult> RepairOtpContextAfterVerificationAsync(string email)
        {
            var memberResolution = await TryResolveOperationalMemberAsync(email, null);
            if (!memberResolution.Success || memberResolution.Member == null)
            {
                return OtpVerificationRepairResult.Fail(memberResolution.Message);
            }

            if (!Guid.TryParse(CurrentUserId, out var authUserId))
            {
                return OtpVerificationRepairResult.Fail("Auth-Kontext konnte nach der OTP-Bestätigung nicht gelesen werden.");
            }

            var member = memberResolution.Member;
            var existingMappedAuthUserId = member.AuthUserId ?? await ResolveAuthUserIdFromExistingMappingsAsync(member.Id, email);
            if (existingMappedAuthUserId.HasValue && existingMappedAuthUserId.Value != authUserId)
            {
                return OtpVerificationRepairResult.Fail("Für diese E-Mail-Adresse besteht bereits eine andere Auth-Zuordnung.");
            }

            await EnsureMemberInviteMappingAsync(authUserId, member.Id, email);
            await EnsureAppUserRecordAsync(authUserId, member.Id, NormalizeRole(member.Role));
            return OtpVerificationRepairResult.Ok();
        }

        private async Task<(bool Success, MitgliedRecord? Member, string Message)> TryResolveOperationalMemberAsync(string email, int? mitgliedId)
        {
            var client = await GetClientAsync();

            if (mitgliedId.HasValue)
            {
                var response = await client
                    .From<MitgliedRecord>()
                    .Where(x => x.Id == mitgliedId.Value)
                    .Get();

                var member = response?.Models?.FirstOrDefault();
                if (!OperationalDataFilter.IsOperationalMember(member))
                {
                    return (false, null, "Für die angegebene E-Mail-Adresse konnte kein gültiges Mitglied vorbereitet werden.");
                }

                return (true, member, string.Empty);
            }

            var membersResponse = await client.From<MitgliedRecord>().Get();
            var normalizedEmail = email.Trim();
            var matches = membersResponse?.Models?
                .Where(OperationalDataFilter.IsOperationalMember)
                .Where(x => string.Equals((x.Email ?? string.Empty).Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase))
                .ToList()
                ?? new List<MitgliedRecord>();

            if (matches.Count == 1)
            {
                return (true, matches[0], string.Empty);
            }

            return matches.Count > 1
                ? (false, null, "Die E-Mail-Adresse ist keinem eindeutig vorbereitbaren Mitglied zugeordnet.")
                : (false, null, "Für diese E-Mail-Adresse ist aktuell kein vorbereiteter App-Zugang vorhanden.");
        }

        private async Task<Guid?> ResolveAuthUserIdFromExistingMappingsAsync(int mitgliedId, string email)
        {
            var client = await GetClientAsync();
            var appUsersResponse = await client
                .From<AppUserRecord>()
                .Where(x => x.MitgliedId == (long)mitgliedId)
                .Get();

            var candidates = appUsersResponse?.Models?
                .Where(x => x.UserId != Guid.Empty)
                .Select(x => x.UserId)
                .Distinct()
                .ToList()
                ?? new List<Guid>();

            if (candidates.Count == 1)
            {
                _logger?.LogInformation(
                    "ResolveAuthUserIdFromExistingMappingsAsync resolved auth user via app_user for mitgliedId={MitgliedId} email={EmailMasked} authUserId={AuthUserId}",
                    mitgliedId,
                    MaskEmail(email),
                    candidates[0]);
                return candidates[0];
            }

            if (candidates.Count > 1)
            {
                _logger?.LogWarning(
                    "ResolveAuthUserIdFromExistingMappingsAsync found multiple app_user candidates for mitgliedId={MitgliedId} email={EmailMasked}",
                    mitgliedId,
                    MaskEmail(email));
            }

            return null;
        }

        private async Task<Guid?> ResolveExistingAuthUserIdByEmailAsync(string email)
        {
            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _logger?.LogInformation("ResolveExistingAuthUserIdByEmailAsync skipped for {EmailMasked}: no access token available.", MaskEmail(email));
                return null;
            }

            try
            {
                using var httpClient = new HttpClient();
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{_clientFactory.Url.TrimEnd('/')}/rest/v1/rpc/find_auth_user_id_by_email");

                request.Headers.Add("apikey", _clientFactory.Key);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(
                    JsonSerializer.Serialize(new { p_email = email }),
                    Encoding.UTF8,
                    "application/json");

                using var response = await httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogWarning(
                        "ResolveExistingAuthUserIdByEmailAsync failed for {EmailMasked}: status={StatusCode} content={ContentMasked}",
                        MaskEmail(email),
                        (int)response.StatusCode,
                        MaskDiagnosticMessage(content));
                    return null;
                }

                var resolvedAuthUserId = TryParseAuthUserIdFromLookupResponse(content);
                if (resolvedAuthUserId.HasValue)
                {
                    _logger?.LogInformation(
                        "ResolveExistingAuthUserIdByEmailAsync resolved auth user for {EmailMasked} authUserId={AuthUserId}",
                        MaskEmail(email),
                        resolvedAuthUserId.Value);
                }

                return resolvedAuthUserId;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "ResolveExistingAuthUserIdByEmailAsync failed for {EmailMasked}", MaskEmail(email));
                return null;
            }
        }

        private static Guid? TryParseAuthUserIdFromLookupResponse(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            try
            {
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in root.EnumerateArray())
                    {
                        if (TryReadAuthUserIdElement(element, out var authUserId))
                            return authUserId;
                    }

                    return null;
                }

                if (TryReadAuthUserIdElement(root, out var directAuthUserId))
                    return directAuthUserId;
            }
            catch
            {
            }

            return null;
        }

        private static bool TryReadAuthUserIdElement(JsonElement element, out Guid authUserId)
        {
            authUserId = Guid.Empty;

            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("auth_user_id", out var authUserIdProperty)
                    && Guid.TryParse(authUserIdProperty.GetString(), out authUserId))
                {
                    return true;
                }

                return false;
            }

            return element.ValueKind == JsonValueKind.String
                && Guid.TryParse(element.GetString(), out authUserId);
        }

        private async Task<int> EnsureMemberInviteMappingAsync(Guid authUserId, int? mitgliedId, string email)
        {
            if (!mitgliedId.HasValue)
            {
                _logger?.LogInformation(
                    "EnsureMemberInviteMappingAsync skipped: no mitgliedId for email={EmailMasked} authUserId={AuthUserId}",
                    MaskEmail(email),
                    authUserId);
                return 0;
            }

            var client = await GetClientAsync();
            const int maxAttempts = 5;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    _logger?.LogInformation(
                        "EnsureMemberInviteMappingAsync attempt {Attempt}/{MaxAttempts} for mitgliedId={MitgliedId} email={EmailMasked} authUserId={AuthUserId}",
                        attempt,
                        maxAttempts,
                        mitgliedId.Value,
                        MaskEmail(email),
                        authUserId);

                    await client
                        .From<MitgliedRecord>()
                        .Where(x => x.Id == mitgliedId.Value)
                        .Set(x => x.AuthUserId, authUserId)
                        .Set(x => x.Email, email)
                        .Update();

                    _logger?.LogInformation(
                        "EnsureMemberInviteMappingAsync verified auth user mapping for mitgliedId={MitgliedId} email={EmailMasked} authUserId={AuthUserId} on attempt {Attempt}/{MaxAttempts}",
                        mitgliedId.Value,
                        MaskEmail(email),
                        authUserId,
                        attempt,
                        maxAttempts);

                    return attempt;
                }
                catch (PostgrestException ex) when (IsMemberAuthUserForeignKeyFailure(ex) && attempt < maxAttempts)
                {
                    _logger?.LogWarning(
                        ex,
                        "EnsureMemberInviteMappingAsync retry {Attempt}/{MaxAttempts} required for mitgliedId={MitgliedId} email={EmailMasked} authUserId={AuthUserId}. {PostgrestDetail}",
                        attempt,
                        maxAttempts,
                        mitgliedId.Value,
                        MaskEmail(email),
                        authUserId,
                        ExtractPostgrestRelevantMessage(ex));

                    await Task.Delay(GetInviteMappingRetryDelay(attempt));
                }
            }

            return 0;
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

            await client.From<AppUserInsertRecord>().Insert(new AppUserInsertRecord
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

        private static TimeSpan GetInviteMappingRetryDelay(int attempt)
            => TimeSpan.FromMilliseconds(200 * attempt);

        private static string NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return "user";

            return role.Trim().ToLowerInvariant();
        }

        private static bool IsMemberAuthUserForeignKeyFailure(PostgrestException ex)
        {
            var message = ExtractPostgrestRelevantMessage(ex);
            var code = ExtractPostgrestCode(ex);

            return string.Equals(code, "23503", StringComparison.OrdinalIgnoreCase)
                && (message.Contains("mitglied_auth_user_id_fkey", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("table \"users\"", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("auth_user_id", StringComparison.OrdinalIgnoreCase));
        }

        private static string ExtractPostgrestCode(PostgrestException ex)
        {
            if (string.IsNullOrWhiteSpace(ex.Content))
                return string.Empty;

            try
            {
                using var document = JsonDocument.Parse(ex.Content);
                return TryGetJsonString(document.RootElement, "code") ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ExtractPostgrestRelevantMessage(PostgrestException ex)
        {
            if (string.IsNullOrWhiteSpace(ex.Content))
                return MaskDiagnosticMessage(ex.Message);

            try
            {
                using var document = JsonDocument.Parse(ex.Content);
                var values = new[]
                {
                    TryGetJsonString(document.RootElement, "message"),
                    TryGetJsonString(document.RootElement, "details"),
                    TryGetJsonString(document.RootElement, "hint"),
                    TryGetJsonString(document.RootElement, "code")
                };

                var joined = string.Join(" | ", values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
                return string.IsNullOrWhiteSpace(joined)
                    ? MaskDiagnosticMessage(ex.Message)
                    : MaskDiagnosticMessage(joined);
            }
            catch
            {
                return MaskDiagnosticMessage(ex.Content);
            }
        }

        private static string? TryGetJsonString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
                return null;

            return property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : property.ToString();
        }

        private sealed class AuthUserPreparationResult
        {
            public Guid? AuthUserId { get; init; }
            public bool DeferredOtpRepairRequired { get; init; }
            public string Message { get; init; } = "Auth-Konto konnte nicht vorbereitet werden.";

            public static AuthUserPreparationResult Resolved(Guid authUserId)
                => new() { AuthUserId = authUserId, Message = string.Empty };

            public static AuthUserPreparationResult RequiresDeferredRepair()
                => new() { DeferredOtpRepairRequired = true, Message = string.Empty };

            public static AuthUserPreparationResult Fail(string message)
                => new() { Message = message };
        }

        private sealed class InvitePreparationResult
        {
            public bool Success { get; init; }
            public bool DeferredOtpRepairRequired { get; init; }
            public Guid? AuthUserId { get; init; }
            public int MitgliedId { get; init; }
            public string Role { get; init; } = string.Empty;
            public int MappingAttempts { get; init; }
            public string Message { get; init; } = string.Empty;

            public static InvitePreparationResult Ok(Guid authUserId, int mitgliedId, string role, int mappingAttempts)
                => new()
                {
                    Success = true,
                    AuthUserId = authUserId,
                    MitgliedId = mitgliedId,
                    Role = role,
                    MappingAttempts = mappingAttempts
                };

            public static InvitePreparationResult OkDeferred(int mitgliedId, string role, string message)
                => new()
                {
                    Success = true,
                    DeferredOtpRepairRequired = true,
                    MitgliedId = mitgliedId,
                    Role = role,
                    Message = message
                };

            public static InvitePreparationResult Fail(string message)
                => new() { Message = message };
        }

        private sealed class OtpPreparationResult
        {
            public bool Success { get; init; }
            public string Message { get; init; } = string.Empty;

            public static OtpPreparationResult Ok()
                => new() { Success = true };

            public static OtpPreparationResult Fail(string message)
                => new() { Message = message };
        }

        private sealed class OtpVerificationRepairResult
        {
            public bool Success { get; init; }
            public string Message { get; init; } = string.Empty;

            public static OtpVerificationRepairResult Ok()
                => new() { Success = true };

            public static OtpVerificationRepairResult Fail(string message)
                => new() { Message = message };
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

        private static string? ExtractAccessToken(object? session)
        {
            if (session == null)
                return null;

            var sessionType = session.GetType();
            return sessionType.GetProperty("AccessToken")?.GetValue(session) as string
                ?? sessionType.GetProperty("access_token")?.GetValue(session) as string;
        }
    }
}