using KGV.Core.Diagnostics;
using KGV.Wpf.Models;
using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KGV.Wpf.Services
{
    public sealed class UpdateCheckService
    {
        private const string LogCategory = "WpfUpdateCheck";

        private readonly string _versionJsonUrl;
        private readonly HttpClient _httpClient;

        public UpdateCheckService(string versionJsonUrl)
        {
            if (string.IsNullOrWhiteSpace(versionJsonUrl))
                throw new ArgumentException("Die URL zur version.json darf nicht leer sein.", nameof(versionJsonUrl));

            _versionJsonUrl = versionJsonUrl.Trim();
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                AppLocalFileLog.Info(LogCategory, $"Updateprüfung gestartet. Url={_versionJsonUrl}");

                using var response = await _httpClient.GetAsync(_versionJsonUrl, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var message = $"version.json konnte nicht geladen werden. HTTP={(int)response.StatusCode} {response.ReasonPhrase}";
                    AppLocalFileLog.Warning(LogCategory, message);

                    return UpdateCheckResult.Failure(message);
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(json))
                {
                    const string message = "version.json war leer.";
                    AppLocalFileLog.Warning(LogCategory, message);

                    return UpdateCheckResult.Failure(message);
                }

                var updateInfo = JsonSerializer.Deserialize<AppUpdateInfo>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (updateInfo == null)
                {
                    const string message = "version.json konnte nicht deserialisiert werden.";
                    AppLocalFileLog.Warning(LogCategory, message);

                    return UpdateCheckResult.Failure(message);
                }

                var localVersionText = GetCurrentAppVersionText();
                var localVersion = AppUpdateInfo.TryParseVersion(localVersionText, out var parsedLocalVersion) ? parsedLocalVersion : null;
                var remoteVersion = updateInfo.GetParsedVersion();

                if (localVersion == null)
                {
                    var message = $"Lokale Version konnte nicht geparst werden. Lokal='{localVersionText}'";
                    AppLocalFileLog.Warning(LogCategory, message);

                    return UpdateCheckResult.Failure(message);
                }

                if (remoteVersion == null)
                {
                    var message = $"Remote-Version konnte nicht geparst werden. Remote='{updateInfo.Version}'";
                    AppLocalFileLog.Warning(LogCategory, message);

                    return UpdateCheckResult.Failure(message);
                }

                var isUpdateAvailable = remoteVersion > localVersion;

                AppLocalFileLog.Info(
                    LogCategory,
                    $"Updateprüfung abgeschlossen. Lokal={localVersion}, Remote={remoteVersion}, UpdateAvailable={isUpdateAvailable}, DownloadUrl={updateInfo.SetupUrl}");

                return UpdateCheckResult.Success(
                    localVersionText,
                    updateInfo,
                    isUpdateAvailable);
            }
            catch (OperationCanceledException)
            {
                const string message = "Updateprüfung wurde abgebrochen.";
                AppLocalFileLog.Warning(LogCategory, message);
                return UpdateCheckResult.Failure(message);
            }
            catch (Exception ex)
            {
                AppLocalFileLog.Error(LogCategory, "Updateprüfung fehlgeschlagen.", ex);
                return UpdateCheckResult.Failure("Updateprüfung fehlgeschlagen.");
            }
        }

        private static string GetCurrentAppVersionText()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

            var informationalVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informationalVersion))
            {
                var plusIndex = informationalVersion.IndexOf('+');
                return plusIndex >= 0
                    ? informationalVersion[..plusIndex]
                    : informationalVersion;
            }

            return assembly.GetName().Version?.ToString() ?? "0.0.0";
        }
    }

    public sealed class UpdateCheckResult
    {
        private UpdateCheckResult()
        {
        }

        public bool IsSuccess { get; private set; }
        public bool IsUpdateAvailable { get; private set; }
        public string LocalVersion { get; private set; } = string.Empty;
        public AppUpdateInfo? RemoteInfo { get; private set; }
        public string Message { get; private set; } = string.Empty;

        public static UpdateCheckResult Success(string localVersion, AppUpdateInfo remoteInfo, bool isUpdateAvailable)
        {
            return new UpdateCheckResult
            {
                IsSuccess = true,
                IsUpdateAvailable = isUpdateAvailable,
                LocalVersion = localVersion ?? string.Empty,
                RemoteInfo = remoteInfo,
                Message = isUpdateAvailable
                    ? "Update verfügbar."
                    : "Kein Update verfügbar."
            };
        }

        public static UpdateCheckResult Failure(string message)
        {
            return new UpdateCheckResult
            {
                IsSuccess = false,
                IsUpdateAvailable = false,
                Message = message ?? string.Empty
            };
        }
    }
}