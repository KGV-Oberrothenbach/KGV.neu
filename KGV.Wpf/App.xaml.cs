using KGV;
using KGV.Core.Diagnostics;
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
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using KGV.Wpf.State;

namespace KGV.Wpf
{
    public partial class App : Application
    {
        private const string StartupLogCategory = "WpfStartup";
        private const string VersionMetadataUrl = "https://kgv-oberrothenbach.github.io/KGV-WPF/version.json";

        private static readonly HttpClient UpdateHttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppLocalFileLog.Initialize();
            AppLocalFileLog.Info(StartupLogCategory, $"WPF startup initialized. LogFile={AppLocalFileLog.LogFilePath}");

            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException -= App_DispatcherUnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

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
            LogSupabaseConfigurationDiagnostics(
                appSettingsInCurrentDir,
                appSettingsInOutput,
                File.Exists(appSettingsInCurrentDir),
                File.Exists(appSettingsInOutput),
                supabaseUrl,
                supabasePublishableKey);

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

            var continueStartup = await CheckForApplicationUpdateAsync();
            if (!continueStartup)
            {
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

        private async Task<bool> CheckForApplicationUpdateAsync()
        {
            try
            {
                AppLocalFileLog.Info(StartupLogCategory, $"Updateprüfung gestartet. MetadataUrl={VersionMetadataUrl}");

                var currentVersionText = GetCurrentApplicationVersion();
                if (!TryParseComparableVersion(currentVersionText, out var currentVersion))
                {
                    AppLocalFileLog.Warning(StartupLogCategory, $"Updateprüfung übersprungen, lokale Version nicht parsebar: {currentVersionText}");
                    return true;
                }

                var remoteInfo = await TryLoadRemoteVersionInfoAsync();
                if (remoteInfo == null)
                {
                    AppLocalFileLog.Info(StartupLogCategory, "Updateprüfung: keine Remote-Metadaten geladen.");
                    return true;
                }

                if (!TryParseComparableVersion(remoteInfo.Version, out var remoteVersion))
                {
                    AppLocalFileLog.Warning(StartupLogCategory, $"Updateprüfung: Remote-Version nicht parsebar: {remoteInfo.Version}");
                    return true;
                }

                AppLocalFileLog.Info(
                    StartupLogCategory,
                    $"Updateprüfung: lokal={currentVersionText}, remote={remoteInfo.Version}, mandatory={remoteInfo.Mandatory}");

                if (remoteVersion <= currentVersion)
                {
                    AppLocalFileLog.Info(StartupLogCategory, "Updateprüfung: keine neuere Version verfügbar.");
                    return true;
                }

                var downloadUrl = FirstNonEmpty(remoteInfo.VersionedSetupUrl, remoteInfo.SetupUrl);
                if (string.IsNullOrWhiteSpace(downloadUrl))
                {
                    AppLocalFileLog.Warning(StartupLogCategory, "Updateprüfung: neuere Version erkannt, aber keine Download-URL vorhanden.");
                    return true;
                }

                var wantsUpdateNow = ShowUpdateDialog(remoteInfo, currentVersionText);

                if (wantsUpdateNow)
                {
                    Process.Start(new ProcessStartInfo(downloadUrl) { UseShellExecute = true });
                    AppLocalFileLog.Info(StartupLogCategory, $"Updateprüfung: Download geöffnet. Url={downloadUrl}");
                    return false;
                }

                if (remoteInfo.Mandatory)
                {
                    AppLocalFileLog.Warning(StartupLogCategory, "Updateprüfung: Pflichtupdate abgebrochen, App wird beendet.");
                    return false;
                }

                AppLocalFileLog.Info(StartupLogCategory, "Updateprüfung: optionales Update zurückgestellt.");
                return true;
            }
            catch (Exception ex)
            {
                AppLocalFileLog.Error(StartupLogCategory, "Updateprüfung fehlgeschlagen, Startup läuft ohne Updateabfrage weiter.", ex);
                return true;
            }
        }

        private static async Task<VersionMetadata?> TryLoadRemoteVersionInfoAsync()
        {
            var rawJson = await UpdateHttpClient.GetStringAsync(VersionMetadataUrl);
            if (string.IsNullOrWhiteSpace(rawJson))
                return null;

            return JsonSerializer.Deserialize<VersionMetadata>(
                rawJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }

        private bool ShowUpdateDialog(VersionMetadata remoteInfo, string currentVersionText)
        {
            var decision = false;
            var isMandatory = remoteInfo.Mandatory;
            var title = isMandatory ? "Update erforderlich" : "Update verfügbar";
            var publishedAtText = TryFormatPublishedAt(remoteInfo.PublishedAt);
            var notes = string.IsNullOrWhiteSpace(remoteInfo.Notes)
                ? null
                : remoteInfo.Notes.Trim();

            var heading = new TextBlock
            {
                Text = isMandatory
                    ? $"Version {remoteInfo.Version} muss installiert werden"
                    : $"Version {remoteInfo.Version} ist verfügbar",
                FontSize = 22,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.Black,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var description = new TextBlock
            {
                Text = isMandatory
                    ? $"Installiert ist aktuell Version {currentVersionText}. Vor der weiteren Nutzung muss die neue Version installiert werden."
                    : $"Installiert ist aktuell Version {currentVersionText}. Möchtest du die neue Version jetzt herunterladen?",
                FontSize = 14,
                Foreground = Brushes.DimGray,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 18)
            };

            var versionCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(243, 246, 250)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 18),
                Child = new StackPanel
                {
                    Children =
                    {
                        CreateInfoLine("Installiert", currentVersionText),
                        CreateInfoLine("Verfügbar", remoteInfo.Version ?? "-"),
                        CreateInfoLine("Veröffentlicht", publishedAtText),
                        CreateInfoLine("Pflichtupdate", isMandatory ? "Ja" : "Nein")
                    }
                }
            };

            UIElement? notesElement = null;
            if (!string.IsNullOrWhiteSpace(notes))
            {
                notesElement = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(220, 225, 232)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(16),
                    Margin = new Thickness(0, 0, 0, 18),
                    Child = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "Hinweise",
                                FontSize = 16,
                                FontWeight = FontWeights.SemiBold,
                                Margin = new Thickness(0, 0, 0, 8)
                            },
                            new TextBlock
                            {
                                Text = notes,
                                TextWrapping = TextWrapping.Wrap,
                                Foreground = Brushes.Black
                            }
                        }
                    }
                };
            }

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var updateButton = new Button
            {
                Content = "Jetzt aktualisieren",
                MinWidth = 150,
                Height = 36,
                Margin = new Thickness(0, 0, 12, 0),
                Padding = new Thickness(16, 0, 16, 0)
            };

            var laterOrExitButton = new Button
            {
                Content = isMandatory ? "Beenden" : "Später",
                MinWidth = 110,
                Height = 36,
                Padding = new Thickness(16, 0, 16, 0)
            };

            var contentStack = new StackPanel();
            contentStack.Children.Add(heading);
            contentStack.Children.Add(description);
            contentStack.Children.Add(versionCard);
            if (notesElement != null)
                contentStack.Children.Add(notesElement);
            contentStack.Children.Add(buttonPanel);

            var dialog = new Window
            {
                Title = title,
                Width = 560,
                SizeToContent = SizeToContent.Height,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = Brushes.White,
                Content = new Border
                {
                    Padding = new Thickness(24),
                    Child = contentStack
                }
            };

            if (Current?.MainWindow != null && Current.MainWindow.IsVisible)
            {
                dialog.Owner = Current.MainWindow;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }

            updateButton.Click += (_, _) =>
            {
                decision = true;
                dialog.DialogResult = true;
                dialog.Close();
            };

            laterOrExitButton.Click += (_, _) =>
            {
                decision = false;
                dialog.DialogResult = false;
                dialog.Close();
            };

            buttonPanel.Children.Add(updateButton);
            buttonPanel.Children.Add(laterOrExitButton);

            dialog.ShowDialog();
            return decision;
        }

        private static FrameworkElement CreateInfoLine(string label, string value)
        {
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var labelBlock = new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.DimGray
            };

            var valueBlock = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(value) ? "-" : value,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.Black
            };

            Grid.SetColumn(labelBlock, 0);
            Grid.SetColumn(valueBlock, 1);

            grid.Children.Add(labelBlock);
            grid.Children.Add(valueBlock);
            return grid;
        }

        private static string GetCurrentApplicationVersion()
        {
            var informationalVersion = Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informationalVersion))
            {
                var cleaned = informationalVersion.Split('+', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                if (!string.IsNullOrWhiteSpace(cleaned))
                    return cleaned;
            }

            var assemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version;
            if (assemblyVersion != null)
            {
                if (assemblyVersion.Build >= 0)
                    return $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";

                return $"{assemblyVersion.Major}.{assemblyVersion.Minor}";
            }

            return "0.0.0";
        }

        private static bool TryParseComparableVersion(string? value, out Version version)
        {
            version = new Version(0, 0, 0, 0);

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var cleaned = value.Trim();

            var plusIndex = cleaned.IndexOf('+');
            if (plusIndex >= 0)
                cleaned = cleaned[..plusIndex];

            var dashIndex = cleaned.IndexOf('-');
            if (dashIndex >= 0)
                cleaned = cleaned[..dashIndex];

            var parts = cleaned
                .Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            if (parts.Count < 2 || parts.Count > 4)
                return false;

            if (parts.Any(x => !int.TryParse(x, out _)))
                return false;

            while (parts.Count < 4)
                parts.Add("0");

            version = new Version(
                int.Parse(parts[0]),
                int.Parse(parts[1]),
                int.Parse(parts[2]),
                int.Parse(parts[3]));

            return true;
        }

        private static string TryFormatPublishedAt(string? publishedAt)
        {
            if (string.IsNullOrWhiteSpace(publishedAt))
                return "-";

            return DateTimeOffset.TryParse(publishedAt, out var parsed)
                ? parsed.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
                : publishedAt.Trim();
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return null;
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

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                AppLocalFileLog.Error(StartupLogCategory, $"AppDomain.CurrentDomain.UnhandledException IsTerminating={e.IsTerminating}", exception);
                return;
            }

            AppLocalFileLog.Error(StartupLogCategory, $"AppDomain.CurrentDomain.UnhandledException without ExceptionObject. IsTerminating={e.IsTerminating}");
        }

        private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            AppLocalFileLog.Error(StartupLogCategory, "DispatcherUnhandledException", e.Exception);
        }

        private sealed class VersionMetadata
        {
            [JsonPropertyName("version")]
            public string? Version { get; set; }

            [JsonPropertyName("setupUrl")]
            public string? SetupUrl { get; set; }

            [JsonPropertyName("versionedSetupUrl")]
            public string? VersionedSetupUrl { get; set; }

            [JsonPropertyName("publishedAt")]
            public string? PublishedAt { get; set; }

            [JsonPropertyName("mandatory")]
            public bool Mandatory { get; set; }

            [JsonPropertyName("notes")]
            public string? Notes { get; set; }
        }
    }
}