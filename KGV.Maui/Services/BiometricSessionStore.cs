using KGV.Core.Models;
using Microsoft.Maui.Storage;

namespace KGV.Maui.Services;

public sealed class BiometricSessionStore
{
    private const string AccessKey = "kgv.biometric.access";
    private const string RefreshKey = "kgv.biometric.refresh";
    private const string ClubKey = "kgv.biometric.club";
    public async Task SaveAsync(string clubId, BiometricSessionTokens tokens)
    {
        await SecureStorage.Default.SetAsync(AccessKey, tokens.AccessToken);
        await SecureStorage.Default.SetAsync(RefreshKey, tokens.RefreshToken);
        await SecureStorage.Default.SetAsync(ClubKey, clubId);
    }
    public async Task<BiometricSessionTokens?> GetAsync(string? clubId)
    {
        if (string.IsNullOrWhiteSpace(clubId) || !string.Equals(await SecureStorage.Default.GetAsync(ClubKey), clubId, StringComparison.Ordinal)) return null;
        var access = await SecureStorage.Default.GetAsync(AccessKey); var refresh = await SecureStorage.Default.GetAsync(RefreshKey);
        return string.IsNullOrWhiteSpace(access) || string.IsNullOrWhiteSpace(refresh) ? null : new BiometricSessionTokens(access, refresh);
    }
    public void Clear() { SecureStorage.Default.Remove(AccessKey); SecureStorage.Default.Remove(RefreshKey); SecureStorage.Default.Remove(ClubKey); }
}
