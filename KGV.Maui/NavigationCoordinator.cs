using System;
using System.Collections.Generic;
using KGV.Maui.Services.Diagnostics;

namespace KGV.Maui;

internal static class NavigationCoordinator
{
    public const string RootSwitchScope = "root-switch";
    public const string MemberSwitchScope = "member-switch";

    private static readonly object SyncRoot = new();
    private static readonly HashSet<string> ActiveScopes = new(StringComparer.OrdinalIgnoreCase);

    public static IDisposable? TryBegin(string scope, string detail, params string[] conflictingScopes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);

        var navigationLabel = string.IsNullOrWhiteSpace(detail)
            ? scope
            : $"{scope} ({detail})";

        AppFileLog.Info("KGV.Navigation", $"Navigation angefordert: {navigationLabel}.");

        lock (SyncRoot)
        {
            if (TryGetBlockingScope(scope, conflictingScopes, out var blockingScope))
            {
                AppFileLog.Warning("KGV.Navigation", $"Navigation unterdrückt: {navigationLabel}. Bereits aktiv: {blockingScope}.");
                return null;
            }

            ActiveScopes.Add(scope);
        }

        AppFileLog.Info("KGV.Navigation", $"Navigation gestartet: {navigationLabel}.");
        return new NavigationScopeLease(scope);
    }

    public static bool IsActive(params string[] scopes)
    {
        if (scopes == null || scopes.Length == 0)
            return false;

        lock (SyncRoot)
        {
            foreach (var scope in scopes)
            {
                if (!string.IsNullOrWhiteSpace(scope) && ActiveScopes.Contains(scope))
                    return true;
            }

            return false;
        }
    }

    private static bool TryGetBlockingScope(string scope, IEnumerable<string> conflictingScopes, out string blockingScope)
    {
        if (ActiveScopes.Contains(scope))
        {
            blockingScope = scope;
            return true;
        }

        foreach (var conflictingScope in conflictingScopes)
        {
            if (!string.IsNullOrWhiteSpace(conflictingScope) && ActiveScopes.Contains(conflictingScope))
            {
                blockingScope = conflictingScope;
                return true;
            }
        }

        blockingScope = string.Empty;
        return false;
    }

    private sealed class NavigationScopeLease : IDisposable
    {
        private readonly string _scope;
        private bool _disposed;

        public NavigationScopeLease(string scope)
        {
            _scope = scope;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            lock (SyncRoot)
            {
                ActiveScopes.Remove(_scope);
            }

            _disposed = true;
        }
    }
}
