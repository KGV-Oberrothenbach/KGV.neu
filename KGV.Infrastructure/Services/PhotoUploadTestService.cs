using KGV.Core.Interfaces;
using KGV.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace KGV.Infrastructure.Services
{
    public sealed class PhotoUploadTestService : IPhotoUploadTestService
    {
        private readonly IAuthService _authService;
        private readonly string _supabaseUrl;
        private readonly string _publishableKey;
        private readonly ILogger<PhotoUploadTestService>? _logger;

        public PhotoUploadTestService(IAuthService authService, IConfiguration configuration, ILogger<PhotoUploadTestService>? logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger;
            _supabaseUrl = (configuration["Supabase:Url"] ?? string.Empty).Trim();
            _publishableKey = (configuration["Supabase:PublishableKey"] ?? configuration["Supabase:Key"] ?? string.Empty).Trim();
        }

        public async Task<PhotoUploadTestResult> UploadAsync(PhotoUploadTestRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var token = await _authService.GetAccessTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                return new PhotoUploadTestResult
                {
                    Success = false,
                    ExceptionMessage = "Kein Access-Token in der laufenden Session verfügbar."
                };
            }

            if (string.IsNullOrWhiteSpace(_supabaseUrl) || string.IsNullOrWhiteSpace(_publishableKey))
            {
                return new PhotoUploadTestResult
                {
                    Success = false,
                    ExceptionMessage = "Supabase-URL oder Publishable Key ist nicht konfiguriert."
                };
            }

            var endpoint = new Uri(new Uri(_supabaseUrl.TrimEnd('/') + "/"), "functions/v1/kgv-upload-photo");

            try
            {
                using var httpClient = new HttpClient();
                using var multipart = new MultipartFormDataContent();
                using var fileContent = new ByteArrayContent(request.FileContent ?? Array.Empty<byte>());
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(string.IsNullOrWhiteSpace(request.ContentType) ? "application/octet-stream" : request.ContentType);
                multipart.Add(fileContent, "file", string.IsNullOrWhiteSpace(request.FileName) ? "upload.bin" : request.FileName);
                multipart.Add(new StringContent(request.Kind?.Trim() ?? string.Empty), "kind");
                multipart.Add(new StringContent(request.Medium?.Trim() ?? string.Empty), "medium");
                multipart.Add(new StringContent(request.Anlage?.Trim() ?? string.Empty), "anlage");
                multipart.Add(new StringContent(request.Garten?.Trim() ?? string.Empty), "garten");
                multipart.Add(new StringContent(request.Datum.ToString("yyyy-MM-dd")), "datum");
                if (!string.IsNullOrWhiteSpace(request.Zaehlernummer))
                    multipart.Add(new StringContent(request.Zaehlernummer.Trim()), "zaehlernummer");

                using var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = multipart
                };
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                message.Headers.Add("apikey", _publishableKey);

                using var response = await httpClient.SendAsync(message);
                var rawResponse = await response.Content.ReadAsStringAsync();
                var result = new PhotoUploadTestResult
                {
                    Success = response.IsSuccessStatusCode,
                    HttpStatusCode = (int)response.StatusCode,
                    HttpStatusText = response.ReasonPhrase ?? string.Empty,
                    RawResponseBody = rawResponse
                };

                TryPopulateResponseFields(result, rawResponse);
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Photo upload test against kgv-upload-photo failed.");
                return new PhotoUploadTestResult
                {
                    Success = false,
                    ExceptionMessage = ex.Message
                };
            }
        }

        private static void TryPopulateResponseFields(PhotoUploadTestResult result, string rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
                return;

            try
            {
                using var document = JsonDocument.Parse(rawResponse);
                result.FileId = FindString(document.RootElement, "file_id");
                result.FileName = FindString(document.RootElement, "file_name");
                result.RelativePath = FindString(document.RootElement, "relative_path");
            }
            catch
            {
            }
        }

        private static string FindString(JsonElement element, string propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                        return property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() ?? string.Empty : property.Value.ToString();

                    var nested = FindString(property.Value, propertyName);
                    if (!string.IsNullOrWhiteSpace(nested))
                        return nested;
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var nested = FindString(item, propertyName);
                    if (!string.IsNullOrWhiteSpace(nested))
                        return nested;
                }
            }

            return string.Empty;
        }
    }
}
