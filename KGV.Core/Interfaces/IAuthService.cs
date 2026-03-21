using Supabase;
using System.Collections.Generic;
using System.Threading.Tasks;
using KGV.Core.Models;

namespace KGV.Core.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Login mit Email + Passwort
        /// </summary>
        Task<bool> LoginAsync(string email, string password);

        Task<List<AppUserDTO>> GetAppUsersAsync();
        Task<bool> ChangeEmailAsync(string newEmail);
        Task<bool> SendPasswordResetEmailAsync(string email);

        /// <summary>
        /// Request an OTP (or similar first-login code) for the given email.
        /// Minimal contract used by the UI; implementation may reuse existing reset flows.
        /// </summary>
        Task<bool> RequestOtpAsync(string email);

        /// <summary>
        /// Verify a previously requested OTP/code for the given email.
        /// </summary>
        Task<bool> VerifyOtpAsync(string email, string code);

        /// <summary>
        /// Set a new password using an OTP/code (first-time password set or recovery).
        /// </summary>
        Task<bool> SetPasswordWithOtpAsync(string email, string code, string newPassword);

        /// <summary>
        /// Supabase-Client, um weitere Abfragen zu machen
        /// </summary>
        Task<Client> GetClientAsync();

        /// <summary>
        /// Rollen des eingeloggten Users
        /// </summary>
        bool IsVorstand { get; }
        bool IsAdmin { get; }
        /// <summary>
        /// Current authenticated user's id (supabase auth user id)
        /// </summary>
        string? CurrentUserId { get; }
    }
}
