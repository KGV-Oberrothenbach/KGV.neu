using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Supabase;
using KGV.Core.Interfaces;

// Alias um Konflikt mit Supabase.Postgrest.Client zu vermeiden
using SupabaseClient = Supabase.Client;

namespace KGV.Infrastructure.Supabase
{
    public class SupabaseClientFactory : ISupabaseClientFactory
    {
        private readonly IConfiguration _config;
        private readonly IVereinskontext _vereinskontext;
        private SupabaseClient? _client;

        public string Url { get; }
        public string Key { get; }

        public SupabaseClientFactory(IConfiguration config, IVereinskontext vereinskontext)
        {
            _config = config;
            _vereinskontext = vereinskontext;

            Url = _vereinskontext.Aktuell?.SupabaseUrl
                  ?? _config["Supabase:Url"]
                  ?? throw new InvalidOperationException("Supabase URL fehlt in appsettings.json");
            Key = _vereinskontext.Aktuell?.SupabasePublishableKey
                  ?? _config["Supabase:PublishableKey"]
                  ?? _config["Supabase:Key"]
                  ?? throw new InvalidOperationException("Supabase Publishable Key fehlt in appsettings.json");
        }

        public async Task<SupabaseClient> CreateAsync()
        {
            if (_client != null) return _client;

            _client = new SupabaseClient(Url, Key);
            await _client.InitializeAsync();

            return _client;
        }
    }
}
