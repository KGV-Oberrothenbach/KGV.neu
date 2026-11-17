using System;
using System.Threading;
using System.Threading.Tasks;
using Supabase;
using KGV.Core.Interfaces;

// Alias um Konflikt mit Supabase.Postgrest.Client zu vermeiden
using SupabaseClient = Supabase.Client;

namespace KGV.Infrastructure.Supabase
{
    public class SupabaseClientFactory : ISupabaseClientFactory
    {
        private readonly IVereinskontext _vereinskontext;
        private readonly SemaphoreSlim _initializationLock = new(1, 1);
        private SupabaseClient? _client;
        private string? _clientUrl;
        private string? _clientKey;

        public string Url => GetRequiredCredentials().Url;
        public string Key => GetRequiredCredentials().Key;

        public SupabaseClientFactory(IVereinskontext vereinskontext)
        {
            _vereinskontext = vereinskontext ?? throw new ArgumentNullException(nameof(vereinskontext));
        }

        public async Task<SupabaseClient> CreateAsync()
        {
            var credentials = GetRequiredCredentials();
            if (MatchesCachedClient(credentials))
                return _client!;

            await _initializationLock.WaitAsync();
            try
            {
                credentials = GetRequiredCredentials();
                if (MatchesCachedClient(credentials))
                    return _client!;

                var client = new SupabaseClient(credentials.Url, credentials.Key);
                await client.InitializeAsync();

                _client = client;
                _clientUrl = credentials.Url;
                _clientKey = credentials.Key;
                return client;
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        private bool MatchesCachedClient((string Url, string Key) credentials)
            => _client is not null
               && string.Equals(_clientUrl, credentials.Url, StringComparison.Ordinal)
               && string.Equals(_clientKey, credentials.Key, StringComparison.Ordinal);

        private (string Url, string Key) GetRequiredCredentials()
        {
            var context = _vereinskontext.Aktuell
                ?? throw new InvalidOperationException(
                    "Supabase-Zugriff ist ohne ausgewählten Vereinskontext nicht zulässig.");

            var url = SupabaseHeaderValue.NormalizeRequired(context.SupabaseUrl, "Supabase URL des Vereins");
            var key = SupabaseHeaderValue.NormalizeRequired(
                context.SupabasePublishableKey,
                "Supabase Publishable Key des Vereins");

            return (url, key);
        }
    }
}
