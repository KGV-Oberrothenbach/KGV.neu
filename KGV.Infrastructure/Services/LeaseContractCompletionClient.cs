using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using KGV.Core.Interfaces;
using KGV.Core.Models;

namespace KGV.Infrastructure.Services;

public sealed class LeaseContractCompletionClient : ILeaseContractCompletionClient
{
    private readonly ISupabaseClientFactory _clientFactory;
    private readonly IAuthService _auth;
    private readonly Func<HttpClient> _httpClientFactory;
    public LeaseContractCompletionClient(ISupabaseClientFactory clientFactory, IAuthService auth) : this(clientFactory, auth, () => new HttpClient()) { }
    public LeaseContractCompletionClient(ISupabaseClientFactory clientFactory, IAuthService auth, Func<HttpClient> httpClientFactory) { _clientFactory = clientFactory; _auth = auth; _httpClientFactory = httpClientFactory; }
    public async Task<LeaseContractCompletionResult> CompleteAsync(long kommunikationsvorgangId, int expectedRevision, CancellationToken cancellationToken = default)
    {
        if (kommunikationsvorgangId <= 0 || expectedRevision < 1) throw new ArgumentException("Kommunikationsvorgang und Revision sind erforderlich.");
        var token = await _auth.GetAccessTokenAsync(); if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("Die Anmeldung ist abgelaufen.");
        using var client = _httpClientFactory(); using var request = new HttpRequestMessage(HttpMethod.Post, $"{_clientFactory.Url.TrimEnd('/')}/functions/v1/kgv-complete-prepared-lease-contract");
        request.Headers.Add("apikey", _clientFactory.Key); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(new { kommunikationsvorgang_id = kommunikationsvorgangId, expected_revision = expectedRevision }), Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request, cancellationToken); var text = await response.Content.ReadAsStringAsync(cancellationToken);
        using var json = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text); var root = json.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Der Pachtvertragsabschluss lieferte eine ungültige Antwort.");
        var message = root.TryGetProperty("message", out var m) ? m.GetString() : null;
        if (!response.IsSuccessStatusCode || !root.TryGetProperty("success", out var success) || !success.GetBoolean()) throw new InvalidOperationException(message ?? (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ? "Die Anmeldung ist abgelaufen." : response.StatusCode == System.Net.HttpStatusCode.Forbidden ? "Keine Berechtigung für den Pachtvertragsabschluss." : response.StatusCode == System.Net.HttpStatusCode.Conflict ? "Der Pachtvertragsabschluss steht in Konflikt zum aktuellen Stand." : "Der Pachtvertragsabschluss konnte nicht bestätigt werden."));
        long? id(string name) => root.TryGetProperty(name, out var value) && value.TryGetInt64(out var number) ? number : null;
        return new LeaseContractCompletionResult(id("pachtverhaeltnis_id"), id("pachtvertragsfassung_id"), root.TryGetProperty("bereits_abgeschlossen", out var done) && done.GetBoolean());
    }
}
