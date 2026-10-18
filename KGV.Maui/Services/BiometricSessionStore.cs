using KGV.Core.Models;
using Microsoft.Maui.Storage;

namespace KGV.Maui.Services;

public sealed class BiometricSessionStore
{
    private const string AccessKey = "kgv.biometric.access";
    private const string RefreshKey = "kgv.biometric.refresh";
    private const string ClubKey = "kgv.biometric.club";
    private const string EnabledKey = "kgv.biometric.enabled";
    private const string DecisionKey = "kgv.biometric.decision";
    public async Task SaveAsync(string clubId, BiometricSessionTokens tokens)
    {
        await SecureStorage.Default.SetAsync(AccessKey, tokens.AccessToken);
        await SecureStorage.Default.SetAsync(RefreshKey, tokens.RefreshToken);
        await SecureStorage.Default.SetAsync(ClubKey, clubId);
        await SecureStorage.Default.SetAsync(EnabledKey, bool.TrueString);
        await SecureStorage.Default.SetAsync(DecisionKey, bool.TrueString);
    }
    public async Task<BiometricSessionTokens?> GetAsync(string? clubId)
    {
        if (string.IsNullOrWhiteSpace(clubId)
            || !string.Equals(await SecureStorage.Default.GetAsync(EnabledKey), bool.TrueString, StringComparison.Ordinal)
            || !string.Equals(await SecureStorage.Default.GetAsync(ClubKey), clubId, StringComparison.Ordinal)) return null;
        var access = await SecureStorage.Default.GetAsync(AccessKey); var refresh = await SecureStorage.Default.GetAsync(RefreshKey);
        return string.IsNullOrWhiteSpace(access) || string.IsNullOrWhiteSpace(refresh) ? null : new BiometricSessionTokens(access, refresh);
    }
    public async Task<bool> HasDecisionAsync(string? clubScope)
        => !string.IsNullOrWhiteSpace(clubScope)
           && string.Equals(await SecureStorage.Default.GetAsync(ClubKey), clubScope, StringComparison.Ordinal)
           && string.Equals(await SecureStorage.Default.GetAsync(DecisionKey), bool.TrueString, StringComparison.Ordinal);
    public async Task MarkDeclinedAsync(string clubScope)
    {
        if (string.IsNullOrWhiteSpace(clubScope)) return;
        await SecureStorage.Default.SetAsync(ClubKey, clubScope);
        await SecureStorage.Default.SetAsync(DecisionKey, bool.TrueString);
    }
    public void Clear() { SecureStorage.Default.Remove(AccessKey); SecureStorage.Default.Remove(RefreshKey); SecureStorage.Default.Remove(ClubKey); SecureStorage.Default.Remove(EnabledKey); SecureStorage.Default.Remove(DecisionKey); }
}
