using KGV;
using KGV.Core.Interfaces;
using KGV.Core.Security;
using KGV.Infrastructure.Authentication;
using KGV.Infrastructure.Services;
using KGV.Infrastructure.Supabase;
using KGV.ViewModels;
using KGV.Views;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using KGV.Wpf.State;

namespace KGV.Wpf
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Während des Login-Dialogs soll die App NICHT automatisch beenden,
            // nur weil das erste Window (Login) geschlossen wird.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Lade benutzerspezifische AppSettings
            AppSettings.Load();

            // Konfiguration laden
            // Hinweis: In WPF/Debug ist CurrentDirectory nicht deterministisch.
            // Wir laden daher explizit aus zwei Orten (Projekt-CurrentDir + Exe-OutputDir).
            var baseDir = AppContext.BaseDirectory;
            var appSettingsInOutput = Path.Combine(baseDir, "appsettings.json");
            var appSettingsInCurrentDir = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

            var builder = new ConfigurationBuilder()
                // Erst CurrentDir, dann OutputDir – OutputDir soll am Ende "gewinnen".
                .AddJsonFile(appSettingsInCurrentDir, optional: true, reloadOnChange: true)
                .AddJsonFile(appSettingsInOutput, optional: true, reloadOnChange: true);

            // Optional: UserSecrets einbinden
            try
            {
                var userSecretsAsm = Assembly.Load("Microsoft.Extensions.Configuration.UserSecrets");
                if (userSecretsAsm != null)
                {
                    var extensionsType = userSecretsAsm.GetType("Microsoft.Extensions.Configuration.UserSecrets.UserSecretsConfigurationExtensions");
                    if (extensionsType != null)
                    {
                        var methods = extensionsType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                        foreach (var m in methods)
                        {
                            if (m.Name == "AddUserSecrets" && m.IsGenericMethodDefinition)
                            {
                                var gen = m.MakeGenericMethod(typeof(App));
                                gen.Invoke(null, new object[] { builder, true });
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignorieren, falls UserSecrets-Paket nicht vorhanden
            }

            var config = builder.Build();

            var supabaseUrl = config["Supabase:Url"];
            var supabasePublishableKey = config["Supabase:PublishableKey"] ?? config["Supabase:Key"];
            LogSupabaseConfigurationDiagnostics(appSettingsInCurrentDir, appSettingsInOutput, File.Exists(appSettingsInCurrentDir), File.Exists(appSettingsInOutput), supabaseUrl, supabasePublishableKey);

            // Fail-fast mit brauchbarer Diagnose, bevor wir tief im Startup eine Exception bekommen.
            if (string.IsNullOrWhiteSpace(supabaseUrl) || string.IsNullOrWhiteSpace(supabasePublishableKey))
            {
                var msg =
                    "Supabase-Konfiguration fehlt.\n\n" +
                    $"Gesucht in:\n- {appSettingsInCurrentDir} (exists: {File.Exists(appSettingsInCurrentDir)})\n" +
                    $"- {appSettingsInOutput} (exists: {File.Exists(appSettingsInOutput)})\n\n" +
                    "Erwartete JSON-Struktur:\n{\n  \"Supabase\": {\n    \"Url\": \"...\",\n    \"PublishableKey\": \"sb_publishable_...\"\n  }\n}";

                MessageBox.Show(msg, "Konfiguration fehlt", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            // ⚡ SupabaseClientFactory erstellen (für AuthService & SupabaseService)
            var clientFactory = new SupabaseClientFactory(config);

            var permissionService = new PermissionService();
            var userContextService = new UserContextService(clientFactory, permissionService, null);

            // Services initialisieren
            var authService = new AuthService(clientFactory, null); // Logger optional
            var supabaseService = new SupabaseService(
                clientFactory,
                null,
                () => AppState.CurrentUserContext,
                authService,
                supabaseUrl,
                supabasePublishableKey);
            var photoUploadTestService = new PhotoUploadTestService(authService, config, null);

            // NavigationService braucht SupabaseService für VM-Erzeugung
            var navigationService = new NavigationService(supabaseService, authService, photoUploadTestService);

            // Letzte Email laden
            string lastEmail = AppSettings.LastEmail ?? string.Empty;

            // LoginViewModel erstellen
            var loginViewModel = new LoginViewModel(authService)
            {
                Email = lastEmail
            };

            var loginWindow = new LoginWindow
            {
                DataContext = loginViewModel
            };

            // Event bei erfolgreichem Login (Dialog schließen)
            loginViewModel.LoginSucceeded += () =>
            {
                // Setting DialogResult schließt das Window automatisch (bei ShowDialog)
                loginWindow.DialogResult = true;
            };

            var loginOk = loginWindow.ShowDialog();
            if (loginOk != true)
            {
                Shutdown();
                return;
            }

            if (string.IsNullOrWhiteSpace(authService.CurrentUserId) || !Guid.TryParse(authService.CurrentUserId, out var userId))
            {
                MessageBox.Show("Login ok, aber UserId ist ungültig.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            var userContext = await userContextService.GetUserContextAsync(userId);
            AppState.CurrentUserContext = userContext;

            if (userContext.Role == UserRole.User && userContext.MitgliedId == null)
            {
                MessageBox.Show(
                    "Dein Account ist keinem Mitglied zugeordnet.\nBitte wende dich an den Vorstand.",
                    "Zugriff eingeschränkt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Shutdown();
                return;
            }

            // SupabaseService initialisieren
            await supabaseService.InitializeAsync();

            // MainWindowViewModel mit allen Services erstellen
            var mainWindowViewModel = new MainWindowViewModel(
                authService,
                navigationService,
                supabaseService,
                userContext
            );

            // MainWindow erstellen und anzeigen
            var mainWindow = new MainWindow(mainWindowViewModel);

            Current.MainWindow = mainWindow;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();
        }

        private static void LogSupabaseConfigurationDiagnostics(string appSettingsInCurrentDir, string appSettingsInOutput, bool currentExists, bool outputExists, string? supabaseUrl, string? supabasePublishableKey)
        {
            var host = Uri.TryCreate(supabaseUrl, UriKind.Absolute, out var uri)
                ? uri.Host
                : "invalid";
            var trimmedKey = (supabasePublishableKey ?? string.Empty).Trim();
            var keySuffix = trimmedKey.Length >= 6 ? trimmedKey[^6..] : trimmedKey;
            var keyHash = trimmedKey.Length == 0
                ? "empty"
                : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(trimmedKey)));

            Debug.WriteLine($"[KGV.Wpf] CONFIG current={appSettingsInCurrentDir} exists={currentExists}");
            Debug.WriteLine($"[KGV.Wpf] CONFIG output={appSettingsInOutput} exists={outputExists}");
            Debug.WriteLine($"[KGV.Wpf] SUPABASE host={host} keyLength={trimmedKey.Length} keySuffix={keySuffix} keySha256={keyHash}");
            Trace.WriteLine($"[KGV.Wpf] SUPABASE host={host} keyLength={trimmedKey.Length} keySuffix={keySuffix} keySha256={keyHash}");
        }
    }
}
