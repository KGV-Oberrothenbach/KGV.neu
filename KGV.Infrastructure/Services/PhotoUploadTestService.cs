using KGV.Core.Interfaces;
using KGV.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace KGV.Infrastructure.Services
{
    public sealed class PhotoUploadTestService : IPhotoUploadTestService
    {
        private const string FunctionName = "kgv-upload-photo";
        private readonly IAuthService _authService;
        private readonly string _supabaseUrl;
        private readonly string _publishableKey;
        private readonly ILogger<PhotoUploadTestService>? _logger;
        private readonly HttpClient _httpClient;

        public PhotoUploadTestService(IAuthService authService, IConfiguration configuration, ILogger<PhotoUploadTestService>? logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger;
            _supabaseUrl = (configuration["Supabase:Url"] ?? string.Empty).Trim();
            _publishableKey = (configuration["Supabase:PublishableKey"] ?? configuration["Supabase:Key"] ?? string.Empty).Trim();
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(3)
            };
        }

        public async Task<PhotoUploadTestResult> UploadAsync(PhotoUploadTestRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var token = await _authService.GetAccessTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger?.LogWarning(
                    "Photo upload test cannot start. diagnosticCode={DiagnosticCode} stage={Stage} function={FunctionName}",
                    "UPLOAD_AUTH_TOKEN_MISSING",
                    "before_request",
                    FunctionName);

                return new PhotoUploadTestResult
                {
                    Success = false,
                    DiagnosticCode = "UPLOAD_AUTH_TOKEN_MISSING",
                    FailureStage = "before_request",
                    ExceptionMessage = "Kein Access-Token in der laufenden Session verfügbar."
                };
            }

            if (string.IsNullOrWhiteSpace(_supabaseUrl) || string.IsNullOrWhiteSpace(_publishableKey))
            {
                _logger?.LogWarning(
                    "Photo upload test cannot start. diagnosticCode={DiagnosticCode} stage={Stage} function={FunctionName} hasSupabaseUrl={HasSupabaseUrl} hasPublishableKey={HasPublishableKey}",
                    "UPLOAD_CONFIG_MISSING",
                    "before_request",
                    FunctionName,
                    !string.IsNullOrWhiteSpace(_supabaseUrl),
                    !string.IsNullOrWhiteSpace(_publishableKey));

                return new PhotoUploadTestResult
                {
                    Success = false,
                    DiagnosticCode = "UPLOAD_CONFIG_MISSING",
                    FailureStage = "before_request",
                    ExceptionMessage = "Supabase-URL oder Publishable Key ist nicht konfiguriert."
                };
            }

            var endpoint = new Uri(new Uri(_supabaseUrl.TrimEnd('/') + "/"), $"functions/v1/{FunctionName}");
            var fileBytes = request.FileContent ?? Array.Empty<byte>();
            var fileName = string.IsNullOrWhiteSpace(request.FileName) ? "upload.bin" : request.FileName;
            var contentType = string.IsNullOrWhiteSpace(request.ContentType) ? "application/octet-stream" : request.ContentType;
            var fileContentLength = (long)fileBytes.LongLength;

            for (var attempt = 1; attempt <= 2; attempt++)
            {
                var stage = "before_request";
                long? multipartContentLength = null;
                try
                {
                    using var multipart = CreateMultipartContent(request, fileBytes, fileName, contentType);

                    using var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
                    {
                        Content = multipart
                    };
                    multipartContentLength = message.Content?.Headers.ContentLength;
                    message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    message.Headers.Add("apikey", _publishableKey);

                    _logger?.LogInformation(
                        "Photo upload test request prepared. function={FunctionName} endpoint={Endpoint} fileName={FileName} fileContentLength={FileContentLength} multipartContentLength={MultipartContentLength} attempt={Attempt}",
                        FunctionName,
                        endpoint,
                        fileName,
                        fileContentLength,
                        multipartContentLength,
                        attempt);

                    stage = "send";
                    using var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);

                    var result = new PhotoUploadTestResult
                    {
                        Success = response.IsSuccessStatusCode,
                        HttpStatusCode = (int)response.StatusCode,
                        HttpStatusText = response.ReasonPhrase ?? string.Empty,
                        FailureStage = "response_headers"
                    };

                    stage = "response_read";
                    try
                    {
                        result.RawResponseBody = await response.Content.ReadAsStringAsync();
                    }
                    catch (Exception ex)
                    {
                        LogFailure(
                            diagnosticCode: "UPLOAD_RESPONSE_READ_FAIL",
                            stage: stage,
                            endpoint: endpoint,
                            fileName: fileName,
                            fileContentLength: fileContentLength,
                            multipartContentLength: multipartContentLength,
                            exception: ex,
                            attempt: attempt,
                            responseStatusCode: result.HttpStatusCode);

                        result.Success = false;
                        result.DiagnosticCode = "UPLOAD_RESPONSE_READ_FAIL";
                        result.FailureStage = stage;
                        result.ExceptionMessage = BuildUserMessage("UPLOAD_RESPONSE_READ_FAIL");
                        return result;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        result.DiagnosticCode = "UPLOAD_HTTP_ERROR";
                        result.FailureStage = stage;
                        result.ExceptionMessage = "Upload wurde vom Server abgelehnt.";
                    }
                    else
                    {
                        result.FailureStage = string.Empty;
                    }

                    TryPopulateResponseFields(result, result.RawResponseBody);

                    _logger?.LogInformation(
                        "Photo upload test completed. function={FunctionName} endpoint={Endpoint} statusCode={StatusCode} fileName={FileName} fileId={FileId} relativePath={RelativePath}",
                        FunctionName,
                        endpoint,
                        result.HttpStatusCode,
                        fileName,
                        result.FileId,
                        result.RelativePath);

                    return result;
                }
                catch (Exception ex) when (attempt == 1 && IsRetryableTransportException(ex))
                {
                    var diagnosticCode = DetermineDiagnosticCode(ex, stage);
                    LogFailure(
                        diagnosticCode,
                        stage,
                        endpoint,
                        fileName,
                        fileContentLength,
                        multipartContentLength,
                        ex,
                        attempt,
                        willRetry: true);

                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                }
                catch (Exception ex)
                {
                    var diagnosticCode = DetermineDiagnosticCode(ex, stage);
                    LogFailure(
                        diagnosticCode,
                        stage,
                        endpoint,
                        fileName,
                        fileContentLength,
                        multipartContentLength,
                        ex,
                        attempt);

                    return new PhotoUploadTestResult
                    {
                        Success = false,
                        DiagnosticCode = diagnosticCode,
                        FailureStage = stage,
                        ExceptionMessage = BuildUserMessage(diagnosticCode)
                    };
                }
            }

            return new PhotoUploadTestResult
            {
                Success = false,
                DiagnosticCode = "UPLOAD_SEND_FAIL",
                FailureStage = "send",
                ExceptionMessage = BuildUserMessage("UPLOAD_SEND_FAIL")
            };
        }

        private static MultipartFormDataContent CreateMultipartContent(PhotoUploadTestRequest request, byte[] fileBytes, string fileName, string contentType)
        {
            var multipart = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            multipart.Add(fileContent, "file", fileName);
            multipart.Add(new StringContent(request.Kind?.Trim() ?? string.Empty), "kind");
            multipart.Add(new StringContent(request.Medium?.Trim() ?? string.Empty), "medium");
            multipart.Add(new StringContent(request.Anlage?.Trim() ?? string.Empty), "anlage");
            multipart.Add(new StringContent(request.Garten?.Trim() ?? string.Empty), "garten");
            multipart.Add(new StringContent(request.Datum.ToString("yyyy-MM-dd")), "datum");
            if (!string.IsNullOrWhiteSpace(request.Zaehlernummer))
                multipart.Add(new StringContent(request.Zaehlernummer.Trim()), "zaehlernummer");

            return multipart;
        }

        private void LogFailure(
            string diagnosticCode,
            string stage,
            Uri endpoint,
            string fileName,
            long fileContentLength,
            long? multipartContentLength,
            Exception exception,
            int attempt,
            bool willRetry = false,
            int? responseStatusCode = null)
        {
            _logger?.LogWarning(
                exception,
                "Photo upload test failed. diagnosticCode={DiagnosticCode} stage={Stage} function={FunctionName} endpoint={Endpoint} fileName={FileName} fileContentLength={FileContentLength} multipartContentLength={MultipartContentLength} responseStatusCode={ResponseStatusCode} exceptionType={ExceptionType} exceptionMessage={ExceptionMessage} innerExceptionType={InnerExceptionType} innerExceptionMessage={InnerExceptionMessage} attempt={Attempt} willRetry={WillRetry}",
                diagnosticCode,
                stage,
                FunctionName,
                endpoint,
                fileName,
                fileContentLength,
                multipartContentLength,
                responseStatusCode,
                exception.GetType().FullName,
                exception.Message,
                exception.InnerException?.GetType().FullName,
                exception.InnerException?.Message,
                attempt,
                willRetry);
        }

        private static bool IsRetryableTransportException(Exception exception)
            => DetermineDiagnosticCode(exception, "send") is "UPLOAD_SOCKET_CLOSED" or "UPLOAD_SEND_FAIL";

        private static string DetermineDiagnosticCode(Exception exception, string stage)
        {
            if (exception is OperationCanceledException)
                return "UPLOAD_TIMEOUT";

            if (ContainsSocketClosed(exception))
                return "UPLOAD_SOCKET_CLOSED";

            return stage == "response_read"
                ? "UPLOAD_RESPONSE_READ_FAIL"
                : "UPLOAD_SEND_FAIL";
        }

        private static bool ContainsSocketClosed(Exception exception)
        {
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is SocketException)
                    return true;

                if (current is IOException ioException && ioException.Message.Contains("Socket closed", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (!string.IsNullOrWhiteSpace(current.Message) && current.Message.Contains("Socket closed", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string BuildUserMessage(string diagnosticCode)
            => diagnosticCode switch
            {
                "UPLOAD_TIMEOUT" => "Upload hat nicht rechtzeitig geantwortet.",
                "UPLOAD_SOCKET_CLOSED" => "Uploadverbindung wurde unerwartet geschlossen.",
                "UPLOAD_RESPONSE_READ_FAIL" => "Uploadantwort konnte nicht gelesen werden.",
                _ => "Upload technisch fehlgeschlagen."
            };

        private static void TryPopulateResponseFields(PhotoUploadTestResult result, string rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
                return;

            try
            {
                using var document = JsonDocument.Parse(rawResponse);

                var serverErrorCode = FindString(document.RootElement, "error_code");
                if (!string.IsNullOrWhiteSpace(serverErrorCode))
                {
                    result.DiagnosticCode = NormalizeServerErrorCode(serverErrorCode, result.DiagnosticCode);
                    if (!result.Success)
                    {
                        var serverMessage = FindString(document.RootElement, "message");
                        if (!string.IsNullOrWhiteSpace(serverMessage))
                            result.ExceptionMessage = serverMessage.Trim();
                    }
                }

                result.FileId = FindString(document.RootElement, "file_id");
                result.FileName = FindString(document.RootElement, "file_name");
                result.RelativePath = FindString(document.RootElement, "relative_path");
            }
            catch
            {
            }
        }

        private static string NormalizeServerErrorCode(string serverErrorCode, string fallback)
        {
            var code = serverErrorCode.Trim().ToUpperInvariant();
            return code switch
            {
                "GOOGLE_AUTH_ERROR" => "UPLOAD_GOOGLE_AUTH_ERROR",
                "CONFIG_MISSING" => "UPLOAD_CONFIG_MISSING",
                "GOOGLE_DRIVE_ERROR" => "UPLOAD_GOOGLE_DRIVE_ERROR",
                _ => string.IsNullOrWhiteSpace(fallback) ? code : fallback
            };
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
