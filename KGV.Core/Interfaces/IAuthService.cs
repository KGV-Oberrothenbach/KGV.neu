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
