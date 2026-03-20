using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using Microsoft.Extensions.Logging;
using Supabase;

namespace KGV.Infrastructure.Services
{
    public class SupabaseService : ISupabaseService
    {
        private readonly ISupabaseClientFactory _clientFactory;
        private readonly ILogger<SupabaseService>? _logger;
        private readonly Func<UserContext?>? _currentUserContextAccessor;
        private Client? _client;

        public SupabaseService(
            ISupabaseClientFactory clientFactory,
            ILogger<SupabaseService>? logger,
            Func<UserContext?>? currentUserContextAccessor)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _logger = logger;
            _currentUserContextAccessor = currentUserContextAccessor;
        }

        public Client Client => _client ?? throw CreateUnavailableException();

        public async Task InitializeAsync()
        {
            _client = await _clientFactory.CreateAsync();
        }

        public async Task<List<string>> GetSeasonsAsync()
        {
            var saisons = await GetSaisonRecordsAsync();
            return saisons
                .OrderByDescending(x => x.Jahr)
                .Select(x => x.Jahr.ToString())
                .ToList();
        }

        public Task<List<MitgliedRecord>> GetMitgliederAsync() => ExecuteAsync(
            "GetMitgliederAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client.From<MitgliedRecord>().Get();
                return response?.Models?
                    .OrderBy(x => x.Name ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => x.Vorname ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => x.Email ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                    .ToList()
                    ?? new List<MitgliedRecord>();
            },
            new List<MitgliedRecord>());

        public Task<MitgliedRecord?> GetMitgliedByIdAsync(int mitgliedId) => ExecuteAsync<MitgliedRecord?>(
            "GetMitgliedByIdAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client
                    .From<MitgliedRecord>()
                    .Where(x => x.Id == mitgliedId)
                    .Get();

                return response?.Models?.FirstOrDefault();
            },
            null);

        public Task<bool> UpdateMitgliedAsync(MemberDTO dto, string userId) => ExecuteAsync(
            "UpdateMitgliedAsync",
            async () =>
            {
                if (dto == null || dto.Id <= 0)
                    return false;

                if (!Guid.TryParse(userId, out var userGuid))
                    return false;

                var existing = await GetMitgliedByIdAsync(dto.Id);
                if (existing == null || existing.LockedByUserId != userGuid)
                    return false;

                var client = await EnsureClientAsync();
                await client
                    .From<MitgliedRecord>()
                    .Where(x => x.Id == dto.Id)
                    .Set(x => x.Vorname, CleanRequiredText(dto.Vorname))
                    .Set(x => x.Name, CleanRequiredText(dto.Nachname))
                    .Set(x => x.Email, CleanOptionalText(dto.Email))
                    .Set(x => x.Role, string.IsNullOrWhiteSpace(dto.Role) ? existing.Role : dto.Role.Trim())
                    .Set(x => x.Geburtsdatum, NormalizeDate(dto.Geburtsdatum))
                    .Set(x => x.Adresse, CleanOptionalText(dto.Strasse))
                    .Set(x => x.Plz, CleanOptionalText(dto.PLZ))
                    .Set(x => x.Ort, CleanOptionalText(dto.Ort))
                    .Set(x => x.Telefon, CleanOptionalText(dto.Telefon))
                    .Set(x => x.Handy, CleanOptionalText(dto.Mobilnummer))
                    .Set(x => x.Bemerkung, CleanOptionalText(dto.Bemerkungen))
                    .Set(x => x.WhatsappEinwilligung, dto.WhatsappEinwilligung)
                    .Set(x => x.MitgliedSeit, NormalizeDate(dto.MitgliedSeit))
                    .Set(x => x.MitgliedEnde, NormalizeDate(dto.MitgliedEnde))
                    .Set(x => x.Aktiv, dto.MitgliedEnde == null)
                    .Update();

                return true;
            },
            false);
        public Task<ParzelleRecord?> GetParzelleByNumberAsync(string gartenNr) => ExecuteAsync<ParzelleRecord?>(
            "GetParzelleByNumberAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client
                    .From<ParzelleRecord>()
                    .Where(x => x.GartenNr == gartenNr)
                    .Get();

                return response?.Models?.FirstOrDefault();
            },
            null);

        public Task<List<ParzelleRecord>> GetAllParzellenAsync() => ExecuteAsync(
            "GetAllParzellenAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client.From<ParzelleRecord>().Get();

                return response?.Models?
                    .OrderBy(x => GetGartenNrSortKey(x.GartenNr))
                    .ThenBy(x => x.GartenNr, StringComparer.CurrentCultureIgnoreCase)
                    .ToList()
                    ?? new List<ParzelleRecord>();
            },
            new List<ParzelleRecord>());

        public Task<ParzellenBelegungRecord?> GetCurrentBelegungForParzelleAsync(int parzelleId) => ExecuteAsync<ParzellenBelegungRecord?>(
            "GetCurrentBelegungForParzelleAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client
                    .From<ParzellenBelegungRecord>()
                    .Where(x => x.ParzelleId == parzelleId)
                    .Get();

                return response?.Models?
                    .Where(x => IsBelegungActiveOn(x, DateTime.Today))
                    .OrderByDescending(x => x.VonDatum ?? DateTime.MinValue)
                    .FirstOrDefault();
            },
            null);

        public Task<List<ParzellenBelegungRecord>> GetBelegungenForMitgliedAsync(int mitgliedId) => ExecuteAsync(
            "GetBelegungenForMitgliedAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client
                    .From<ParzellenBelegungRecord>()
                    .Where(x => x.MitgliedId == mitgliedId)
                    .Get();

                return response?.Models?
                    .OrderByDescending(x => x.BisDatum == null)
                    .ThenByDescending(x => x.VonDatum ?? DateTime.MinValue)
                    .ToList()
                    ?? new List<ParzellenBelegungRecord>();
            },
            new List<ParzellenBelegungRecord>());

        public Task<List<ParzellenBelegungRecord>> GetAllParzellenBelegungenAsync() => ExecuteAsync(
            "GetAllParzellenBelegungenAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client.From<ParzellenBelegungRecord>().Get();

                return response?.Models?
                    .OrderBy(x => x.ParzelleId)
                    .ThenByDescending(x => x.VonDatum ?? DateTime.MinValue)
                    .ToList()
                    ?? new List<ParzellenBelegungRecord>();
            },
            new List<ParzellenBelegungRecord>());
        public Task<bool> AssignParzelleToMitgliedAsync(int mitgliedId, int parzelleId, DateTime startDatum) => Unavailable<bool>();
        public Task<bool> EndParzellenBelegungAsync(int belegungId, DateTime bisDatum) => Unavailable<bool>();
        public Task<List<ZaehlerAblesungDTO>> GetStromAblesungenAsync(int parzelleId) => Unavailable<List<ZaehlerAblesungDTO>>();
        public Task<List<ZaehlerAblesungDTO>> GetWasserAblesungenAsync(int parzelleId) => Unavailable<List<ZaehlerAblesungDTO>>();
        public Task<StromzaehlerRecord?> GetActiveStromzaehlerAsync(int parzelleId, DateTime onDate) => Unavailable<StromzaehlerRecord?>();
        public Task<WasserzaehlerRecord?> GetActiveWasserzaehlerAsync(int parzelleId, DateTime onDate) => Unavailable<WasserzaehlerRecord?>();
        public Task<bool> AddStromzaehlerAsync(int parzelleId, string zaehlernummer, DateTime eichdatum, DateTime eingebautAm) => Unavailable<bool>();
        public Task<bool> AddWasserzaehlerAsync(int parzelleId, string zaehlernummer, DateTime eichdatum, DateTime eingebautAm) => Unavailable<bool>();
        public Task<bool> SetStromzaehlerAusgebautAmAsync(long stromzaehlerId, DateTime ausgebautAm) => Unavailable<bool>();
        public Task<bool> SetWasserzaehlerAusgebautAmAsync(long wasserzaehlerId, DateTime ausgebautAm) => Unavailable<bool>();
        public Task<bool> AddAblesungAsync(short zaehlerTyp, long zaehlerId, DateTime ablesedatum, decimal stand, string? fotoPfad) => Unavailable<bool>();
        public Task<bool> UpdateAblesungAsync(long ablesungId, DateTime ablesedatum, decimal stand, string? fotoPfad) => Unavailable<bool>();
        public Task<MitgliedRecord?> GetNebenmitgliedByHauptmitgliedIdAsync(int hauptmitgliedId) => ExecuteAsync<MitgliedRecord?>(
            "GetNebenmitgliedByHauptmitgliedIdAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client
                    .From<MitgliedRecord>()
                    .Where(x => x.HauptmitgliedId == hauptmitgliedId)
                    .Get();

                return response?.Models?
                    .OrderBy(x => x.Id)
                    .FirstOrDefault();
            },
            null);
        public Task<MitgliedRecord?> CreateNebenmitgliedAsync(int hauptmitgliedId, string vorname, string nachname, bool adresseUebernehmen) => Unavailable<MitgliedRecord?>();
        public Task<List<SaisonRecord>> GetSaisonRecordsAsync() => ExecuteAsync(
            "GetSaisonRecordsAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client.From<SaisonRecord>().Get();

                return response?.Models?
                    .OrderByDescending(x => x.Jahr)
                    .ToList()
                    ?? new List<SaisonRecord>();
            },
            new List<SaisonRecord>());

        public Task<MitgliedRecord?> GetMitgliedByAuthUserIdAsync(Guid authUserId) => ExecuteAsync<MitgliedRecord?>(
            "GetMitgliedByAuthUserIdAsync(Guid)",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client
                    .From<MitgliedRecord>()
                    .Where(x => x.AuthUserId == authUserId)
                    .Get();

                return response?.Models?.FirstOrDefault();
            },
            null);

        public async Task<MitgliedRecord?> GetMitgliedByAuthUserIdAsync(string authUserId)
        {
            if (!Guid.TryParse(authUserId, out var parsed))
                return null;

            return await GetMitgliedByAuthUserIdAsync(parsed);
        }
        public Task<bool> UpdateOwnContactAsync(int mitgliedId, string? telefon, string? handy, string? adresse, string? plz, string? ort) => Unavailable<bool>();
        public Task<List<ArbeitsstundeDTO>> GetArbeitsstundenAsync(params int[] mitgliedIds) => ExecuteAsync(
            "GetArbeitsstundenAsync",
            async () =>
            {
                var idSet = new HashSet<int>((mitgliedIds ?? Array.Empty<int>()).Where(x => x > 0));
                if (idSet.Count == 0)
                    return new List<ArbeitsstundeDTO>();

                var client = await EnsureClientAsync();
                var response = await client.From<ArbeitsstundeRecord>().Get();
                var records = response?.Models?
                    .Where(x => idSet.Contains(x.MitgliedId))
                    .OrderByDescending(x => x.Datum)
                    .ThenByDescending(x => x.Id)
                    .ToList()
                    ?? new List<ArbeitsstundeRecord>();

                if (records.Count == 0)
                    return new List<ArbeitsstundeDTO>();

                var mitglieder = await GetMitgliederAsync();
                var mitgliedById = mitglieder.ToDictionary(x => x.Id, x => x);
                var saisonById = (await GetSaisonRecordsAsync()).ToDictionary(x => x.Id, x => x);

                return records.Select(record =>
                {
                    mitgliedById.TryGetValue(record.MitgliedId, out var mitglied);
                    MitgliedRecord? approver = null;
                    if (record.GenehmigtVon.HasValue)
                        mitgliedById.TryGetValue(record.GenehmigtVon.Value, out approver);

                    return new ArbeitsstundeDTO
                    {
                        Id = record.Id,
                        MitgliedId = record.MitgliedId,
                        Vorname = mitglied?.Vorname ?? string.Empty,
                        Nachname = mitglied?.Name ?? string.Empty,
                        Datum = record.Datum,
                        SaisonId = record.SaisonId,
                        SaisonJahr = saisonById.TryGetValue(record.SaisonId, out var saison) ? saison.Jahr : 0,
                        Stunden = record.Stunden,
                        Beschreibung = record.ArtDerArbeit ?? string.Empty,
                        Status = record.Status,
                        Freigegeben = record.Freigegeben,
                        FreigegebenAm = record.GenehmigtAm,
                        FreigegebenVonId = record.GenehmigtVon,
                        FreigegebenVonName = FormatMemberName(approver)
                    };
                }).ToList();
            },
            new List<ArbeitsstundeDTO>());
        public Task<bool> AddArbeitsstundeAsync(ArbeitsstundeRecord record) => Unavailable<bool>();
        public Task<bool> UpdateArbeitsstundeAsync(ArbeitsstundeRecord record) => ExecuteAsync(
            "UpdateArbeitsstundeAsync",
            async () =>
            {
                if (record == null || record.Id <= 0)
                    return false;

                var client = await EnsureClientAsync();
                await client
                    .From<ArbeitsstundeRecord>()
                    .Where(x => x.Id == record.Id)
                    .Set(x => x.MitgliedId, record.MitgliedId)
                    .Set(x => x.SaisonId, record.SaisonId)
                    .Set(x => x.Datum, record.Datum.Date)
                    .Set(x => x.Stunden, record.Stunden)
                    .Set(x => x.ArtDerArbeit, record.ArtDerArbeit ?? string.Empty)
                    .Set(x => x.Status, string.IsNullOrWhiteSpace(record.Status) ? "offen" : record.Status)
                    .Set(x => x.Freigegeben, record.Freigegeben)
                    .Set(x => x.GenehmigtAm, record.GenehmigtAm)
                    .Set(x => x.GenehmigtVon, record.GenehmigtVon)
                    .Update();

                return true;
            },
            false);
        public Task<bool> DeleteArbeitsstundeAsync(int arbeitsstundeId) => Unavailable<bool>();
        public Task<List<(int MitgliedId, string Vorname, string Nachname, int Count)>> GetUnapprovedArbeitsstundenByMitgliedAsync() => Unavailable<List<(int MitgliedId, string Vorname, string Nachname, int Count)>>();
        public Task<bool> TryLockMitgliedAsync(int mitgliedId, string userId, int timeoutMinutes = 10) => ExecuteAsync(
            "TryLockMitgliedAsync",
            async () =>
            {
                if (!Guid.TryParse(userId, out var userGuid))
                    return false;

                var existing = await GetMitgliedByIdAsync(mitgliedId);
                if (existing == null)
                    return false;

                var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                var hasActiveForeignLock = existing.LockedByUserId.HasValue
                    && existing.LockedByUserId.Value != userGuid
                    && (!existing.LockedAt.HasValue || existing.LockedAt.Value.AddMinutes(timeoutMinutes) > now);

                if (hasActiveForeignLock)
                    return false;

                var client = await EnsureClientAsync();
                await client
                    .From<MitgliedRecord>()
                    .Where(x => x.Id == mitgliedId)
                    .Set(x => x.LockedByUserId, userGuid)
                    .Set(x => x.LockedAt, now)
                    .Update();

                return true;
            },
            false);

        public Task<bool> ReleaseLockMitgliedAsync(int mitgliedId, string userId, bool force = false) => ExecuteAsync(
            "ReleaseLockMitgliedAsync",
            async () =>
            {
                var existing = await GetMitgliedByIdAsync(mitgliedId);
                if (existing == null)
                    return false;

                if (!existing.LockedByUserId.HasValue)
                    return true;

                if (!force)
                {
                    if (!Guid.TryParse(userId, out var userGuid))
                        return false;

                    if (existing.LockedByUserId.Value != userGuid)
                        return false;
                }

                var client = await EnsureClientAsync();
                await client
                    .From<MitgliedRecord>()
                    .Where(x => x.Id == mitgliedId)
                    .Set(x => x.LockedByUserId, (Guid?)null)
                    .Set(x => x.LockedAt, (DateTime?)null)
                    .Update();

                return true;
            },
            false);
        public Task<bool> TryLockArbeitsstundeAsync(int arbeitsstundeId, string userId, int timeoutMinutes = 10) => Unavailable<bool>();
        public Task<bool> ReleaseLockArbeitsstundeAsync(int arbeitsstundeId, string userId, bool force = false) => Unavailable<bool>();
        public Task<List<DocumentInfo>> GetMitgliedDokumenteAsync(int mitgliedId) => ExecuteAsync(
            "GetMitgliedDokumenteAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client
                    .From<DokumentRecord>()
                    .Where(x => x.MitgliedId == mitgliedId)
                    .Get();

                return response?.Models?
                    .OrderByDescending(x => x.UpdatedAt)
                    .ThenBy(x => x.Dateiname ?? x.Titel ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                    .Select(MapDocumentInfo)
                    .ToList()
                    ?? new List<DocumentInfo>();
            },
            new List<DocumentInfo>());

        public Task<List<DocumentInfo>> GetParzelleDokumenteAsync(int parzelleId) => ExecuteAsync(
            "GetParzelleDokumenteAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client
                    .From<DokumentRecord>()
                    .Where(x => x.ParzelleId == parzelleId)
                    .Get();

                return response?.Models?
                    .OrderByDescending(x => x.UpdatedAt)
                    .ThenBy(x => x.Dateiname ?? x.Titel ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                    .Select(MapDocumentInfo)
                    .ToList()
                    ?? new List<DocumentInfo>();
            },
            new List<DocumentInfo>());

        public Task<string?> CreateDokumentSignedUrlAsync(string storagePath, int expiresInSeconds = 3600) => ExecuteAsync<string?>(
            "CreateDokumentSignedUrlAsync",
            async () =>
            {
                if (!TryParseStorageReference(storagePath, out var bucket, out var path))
                    return null;

                var client = await EnsureClientAsync();
                return await client.Storage.From(bucket).CreateSignedUrl(path, expiresInSeconds);
            },
            null);

        private Task<T> Unavailable<T>()
        {
            _logger?.LogWarning("Recovered placeholder SupabaseService invoked without reconstructed implementation. User context available: {HasUserContext}", _currentUserContextAccessor?.Invoke() != null);
            return Task.FromException<T>(CreateUnavailableException());
        }

        private async Task<Client> EnsureClientAsync()
        {
            if (_client == null)
                _client = await _clientFactory.CreateAsync();

            return _client;
        }

        private async Task<T> ExecuteAsync<T>(string operation, Func<Task<T>> action, T fallback)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "{Operation} failed.", operation);
                return fallback;
            }
        }

        private static DateTime? NormalizeDate(DateTime? value)
        {
            if (!value.HasValue)
                return null;

            var normalized = value.Value.Date.AddHours(12);
            return DateTime.SpecifyKind(normalized, DateTimeKind.Unspecified);
        }

        private static string CleanRequiredText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string? CleanOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static bool IsBelegungActiveOn(ParzellenBelegungRecord belegung, DateTime date)
        {
            var onDate = date.Date;
            var von = (belegung.VonDatum ?? DateTime.MinValue).Date;
            var bis = belegung.BisDatum?.Date;
            return von <= onDate && (bis == null || bis.Value >= onDate);
        }

        private static int GetGartenNrSortKey(string? gartenNr)
        {
            if (string.IsNullOrWhiteSpace(gartenNr))
                return int.MaxValue;

            var digits = new string(gartenNr.TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digits, out var value) ? value : int.MaxValue;
        }

        private static string? FormatMemberName(MitgliedRecord? member)
        {
            if (member == null)
                return null;

            var fullName = $"{member.Vorname} {member.Name}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? member.Email : fullName;
        }

        private static DocumentInfo MapDocumentInfo(DokumentRecord record)
        {
            var storagePath = ComposeStorageReference(record);
            var fallbackName = string.IsNullOrWhiteSpace(record.StoragePath)
                ? string.Empty
                : Path.GetFileName(record.StoragePath.Replace('\\', '/'));

            return new DocumentInfo
            {
                Name = FirstNonEmpty(record.Titel, record.Dateiname, fallbackName),
                StoragePath = storagePath,
                Size = record.SizeBytes,
                UpdatedAt = record.UpdatedAt
            };
        }

        private static string ComposeStorageReference(DokumentRecord record)
        {
            var path = (record.StoragePath ?? string.Empty).Trim().TrimStart('/');
            var bucket = (record.Bucket ?? string.Empty).Trim().Trim('/');

            if (string.IsNullOrWhiteSpace(bucket))
                return path;

            if (path.StartsWith(bucket + "/", StringComparison.OrdinalIgnoreCase))
                return path;

            return string.IsNullOrWhiteSpace(path) ? bucket : $"{bucket}/{path}";
        }

        private static bool TryParseStorageReference(string? storageReference, out string bucket, out string path)
        {
            bucket = string.Empty;
            path = string.Empty;

            if (string.IsNullOrWhiteSpace(storageReference))
                return false;

            var normalized = storageReference.Trim().Replace('\\', '/').TrimStart('/');
            var separatorIndex = normalized.IndexOf('/');
            if (separatorIndex <= 0 || separatorIndex == normalized.Length - 1)
                return false;

            bucket = normalized[..separatorIndex];
            path = normalized[(separatorIndex + 1)..];
            return !string.IsNullOrWhiteSpace(bucket) && !string.IsNullOrWhiteSpace(path);
        }

        private static string FirstNonEmpty(params string?[] candidates)
        {
            foreach (var candidate in candidates)
            {
                if (!string.IsNullOrWhiteSpace(candidate))
                    return candidate.Trim();
            }

            return string.Empty;
        }

        private static NotSupportedException CreateUnavailableException()
        {
            return new NotSupportedException("SupabaseService wurde im Wiederaufbau nur minimal als Platzhalter wiederhergestellt und ist fachlich noch nicht rekonstruiert.");
        }
    }
}
