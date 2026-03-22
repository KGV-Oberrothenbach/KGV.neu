using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
                    .Set(x => x.Email, existing.Email)
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

        public Task<ParzelleDetailDTO?> GetParzelleDetailAsync(int parzelleId) => ExecuteAsync<ParzelleDetailDTO?>(
            "GetParzelleDetailAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client
                    .From<ParzelleRecord>()
                    .Where(x => x.Id == parzelleId)
                    .Get();

                var parzelle = response?.Models?.FirstOrDefault();
                if (parzelle == null)
                    return null;

                var belegung = await GetCurrentBelegungForParzelleAsync(parzelleId);
                MitgliedRecord? mitglied = null;
                if (belegung != null)
                    mitglied = await GetMitgliedByIdAsync(belegung.MitgliedId);

                var stromzaehler = await GetActiveStromzaehlerAsync(parzelleId, DateTime.Today);
                var wasserzaehler = await GetActiveWasserzaehlerAsync(parzelleId, DateTime.Today);
                var stromAblesungen = await GetStromAblesungenAsync(parzelleId);
                var wasserAblesungen = await GetWasserAblesungenAsync(parzelleId);
                var dokumente = await GetParzelleDokumenteAsync(parzelleId);

                return new ParzelleDetailDTO
                {
                    ParzelleId = parzelle.Id,
                    BelegungId = belegung?.Id,
                    GartenNr = parzelle.GartenNr,
                    Anlage = parzelle.Anlage,
                    IstVergeben = belegung != null,
                    MitgliedId = belegung?.MitgliedId,
                    MitgliedName = FormatMemberName(mitglied),
                    MitgliedEmail = mitglied?.Email ?? string.Empty,
                    VonDatum = belegung?.VonDatum,
                    BisDatum = belegung?.BisDatum,
                    AktiverStromzaehler = stromzaehler,
                    AktiverWasserzaehler = wasserzaehler,
                    StromAblesungenCount = stromAblesungen.Count,
                    WasserAblesungenCount = wasserAblesungen.Count,
                    Dokumente = dokumente
                };
            },
            null);

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
        public Task<bool> AssignParzelleToMitgliedAsync(int mitgliedId, int parzelleId, DateTime startDatum) => ExecuteAsync(
            "AssignParzelleToMitgliedAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var normalizedStart = NormalizeDate(startDatum) ?? startDatum.Date;

                var response = await client
                    .From<ParzellenBelegungRecord>()
                    .Where(x => x.ParzelleId == parzelleId)
                    .Get();

                var existing = response?.Models ?? new List<ParzellenBelegungRecord>();
                if (existing.Any(x => IsBelegungActiveOn(x, normalizedStart)))
                    return false;

                await client.From<ParzellenBelegungInsertRecord>().Insert(new ParzellenBelegungInsertRecord
                {
                    ParzelleId = parzelleId,
                    MitgliedId = mitgliedId,
                    VonDatum = normalizedStart,
                    BisDatum = null
                });

                return true;
            },
            false);

        public Task<bool> EndParzellenBelegungAsync(int belegungId, DateTime bisDatum) => ExecuteAsync(
            "EndParzellenBelegungAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client
                    .From<ParzellenBelegungRecord>()
                    .Where(x => x.Id == belegungId)
                    .Get();

                var existing = response?.Models?.FirstOrDefault();
                if (existing == null)
                    return false;

                var normalizedEnd = NormalizeDate(bisDatum) ?? bisDatum.Date;
                var normalizedStart = existing.VonDatum?.Date ?? DateTime.MinValue;
                if (normalizedEnd.Date < normalizedStart)
                    return false;

                await client
                    .From<ParzellenBelegungRecord>()
                    .Where(x => x.Id == belegungId)
                    .Set(x => x.BisDatum, normalizedEnd)
                    .Update();

                return true;
            },
            false);
        public Task<List<ZaehlerAblesungDTO>> GetStromAblesungenAsync(int parzelleId) => ExecuteAsync(
            "GetStromAblesungenAsync",
            async () =>
            {
                var meters = await GetStromzaehlerForParzelleAsync(parzelleId);
                return await GetAblesungenAsync(meters, zaehlerTyp: 1);
            },
            new List<ZaehlerAblesungDTO>());

        public Task<List<ZaehlerAblesungDTO>> GetWasserAblesungenAsync(int parzelleId) => ExecuteAsync(
            "GetWasserAblesungenAsync",
            async () =>
            {
                var meters = await GetWasserzaehlerForParzelleAsync(parzelleId);
                return await GetAblesungenAsync(meters, zaehlerTyp: 2);
            },
            new List<ZaehlerAblesungDTO>());

        public Task<StromzaehlerRecord?> GetActiveStromzaehlerAsync(int parzelleId, DateTime onDate) => ExecuteAsync<StromzaehlerRecord?>(
            "GetActiveStromzaehlerAsync",
            async () =>
            {
                var meters = await GetStromzaehlerForParzelleAsync(parzelleId);
                return meters
                    .Where(x => IsMeterActiveOn(x.EingebautAm, x.AusgebautAm, onDate))
                    .OrderByDescending(x => x.EingebautAm)
                    .FirstOrDefault();
            },
            null);

        public Task<WasserzaehlerRecord?> GetActiveWasserzaehlerAsync(int parzelleId, DateTime onDate) => ExecuteAsync<WasserzaehlerRecord?>(
            "GetActiveWasserzaehlerAsync",
            async () =>
            {
                var meters = await GetWasserzaehlerForParzelleAsync(parzelleId);
                return meters
                    .Where(x => IsMeterActiveOn(x.EingebautAm, x.AusgebautAm, onDate))
                    .OrderByDescending(x => x.EingebautAm)
                    .FirstOrDefault();
            },
            null);

        public Task<bool> AddStromzaehlerAsync(int parzelleId, string zaehlernummer, DateTime eichdatum, DateTime eingebautAm) => ExecuteAsync(
            "AddStromzaehlerAsync",
            async () =>
            {
                if (string.IsNullOrWhiteSpace(zaehlernummer))
                    return false;

                var client = await EnsureClientAsync();
                await client.From<StromzaehlerRecord>().Insert(new StromzaehlerRecord
                {
                    ParzelleId = parzelleId,
                    Zaehlernummer = zaehlernummer.Trim(),
                    Eichdatum = NormalizeDateTime(eichdatum),
                    EingebautAm = NormalizeDateTime(eingebautAm.Date)
                });

                return true;
            },
            false);

        public Task<bool> AddWasserzaehlerAsync(int parzelleId, string zaehlernummer, DateTime eichdatum, DateTime eingebautAm) => ExecuteAsync(
            "AddWasserzaehlerAsync",
            async () =>
            {
                if (string.IsNullOrWhiteSpace(zaehlernummer))
                    return false;

                var client = await EnsureClientAsync();
                await client.From<WasserzaehlerRecord>().Insert(new WasserzaehlerRecord
                {
                    ParzelleId = parzelleId,
                    Zaehlernummer = zaehlernummer.Trim(),
                    Eichdatum = NormalizeDateTime(eichdatum),
                    EingebautAm = NormalizeDateTime(eingebautAm.Date)
                });

                return true;
            },
            false);

        public Task<bool> SetStromzaehlerAusgebautAmAsync(long stromzaehlerId, DateTime ausgebautAm) => ExecuteAsync(
            "SetStromzaehlerAusgebautAmAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                await client
                    .From<StromzaehlerRecord>()
                    .Where(x => x.Id == stromzaehlerId)
                    .Set(x => x.AusgebautAm, NormalizeDateTime(ausgebautAm.Date))
                    .Update();

                return true;
            },
            false);

        public Task<bool> SetWasserzaehlerAusgebautAmAsync(long wasserzaehlerId, DateTime ausgebautAm) => ExecuteAsync(
            "SetWasserzaehlerAusgebautAmAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                await client
                    .From<WasserzaehlerRecord>()
                    .Where(x => x.Id == wasserzaehlerId)
                    .Set(x => x.AusgebautAm, NormalizeDateTime(ausgebautAm.Date))
                    .Update();

                return true;
            },
            false);

        public Task<bool> AddAblesungAsync(short zaehlerTyp, long zaehlerId, DateTime ablesedatum, decimal stand, string? fotoPfad) => ExecuteAsync(
            "AddAblesungAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                await client.From<AblesungRecord>().Insert(new AblesungRecord
                {
                    ZaehlerTyp = zaehlerTyp,
                    ZaehlerId = zaehlerId,
                    Ablesedatum = NormalizeDateTime(ablesedatum),
                    Stand = stand,
                    Freigegeben = true,
                    FotoPfad = CleanOptionalText(fotoPfad)
                });

                return true;
            },
            false);

        public Task<bool> UpdateAblesungAsync(long ablesungId, DateTime ablesedatum, decimal stand, string? fotoPfad) => ExecuteAsync(
            "UpdateAblesungAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                await client
                    .From<AblesungRecord>()
                    .Where(x => x.Id == ablesungId)
                    .Set(x => x.Ablesedatum, NormalizeDateTime(ablesedatum))
                    .Set(x => x.Stand, stand)
                    .Set(x => x.FotoPfad, CleanOptionalText(fotoPfad))
                    .Set(x => x.Freigegeben, true)
                    .Update();

                return true;
            },
            false);
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
        public Task<bool> AddArbeitsstundeAsync(ArbeitsstundeRecord record) => ExecuteAsync(
            "AddArbeitsstundeAsync",
            async () =>
            {
                if (record == null || record.MitgliedId <= 0 || record.SaisonId <= 0 || record.Stunden <= 0 || string.IsNullOrWhiteSpace(record.ArtDerArbeit))
                    return false;

                var client = await EnsureClientAsync();
                await client.From<ArbeitsstundeRecord>().Insert(new ArbeitsstundeRecord
                {
                    MitgliedId = record.MitgliedId,
                    SaisonId = record.SaisonId,
                    Datum = NormalizeDateTime(record.Datum.Date),
                    Stunden = record.Stunden,
                    ArtDerArbeit = record.ArtDerArbeit.Trim(),
                    Status = string.IsNullOrWhiteSpace(record.Status) ? "offen" : record.Status.Trim(),
                    Freigegeben = record.Freigegeben,
                    GenehmigtAm = record.GenehmigtAm,
                    GenehmigtVon = record.GenehmigtVon
                });

                return true;
            },
            false);
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
        public Task<bool> DeleteArbeitsstundeAsync(int arbeitsstundeId) => ExecuteAsync(
            "DeleteArbeitsstundeAsync",
            async () =>
            {
                if (arbeitsstundeId <= 0)
                    return false;

                var client = await EnsureClientAsync();
                await client
                    .From<ArbeitsstundeRecord>()
                    .Where(x => x.Id == arbeitsstundeId)
                    .Delete();

                return true;
            },
            false);
        public Task<List<(int MitgliedId, string Vorname, string Nachname, int Count)>> GetUnapprovedArbeitsstundenByMitgliedAsync() => ExecuteAsync(
            "GetUnapprovedArbeitsstundenByMitgliedAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client.From<ArbeitsstundeRecord>().Get();
                var offeneArbeitsstunden = response?.Models?
                    .Where(x => !x.Freigegeben && (string.IsNullOrWhiteSpace(x.Status) || x.Status.Equals("offen", StringComparison.OrdinalIgnoreCase)))
                    .ToList()
                    ?? new List<ArbeitsstundeRecord>();

                if (offeneArbeitsstunden.Count == 0)
                    return new List<(int MitgliedId, string Vorname, string Nachname, int Count)>();

                var mitglieder = await GetMitgliederAsync();
                var operativeMitglieder = mitglieder
                    .Where(OperationalDataFilter.IsOperationalMember)
                    .ToDictionary(x => x.Id, x => x);

                return offeneArbeitsstunden
                    .Where(x => operativeMitglieder.ContainsKey(x.MitgliedId))
                    .GroupBy(x => x.MitgliedId)
                    .Select(g =>
                    {
                        var mitglied = operativeMitglieder[g.Key];
                        return (
                            MitgliedId: g.Key,
                            Vorname: mitglied.Vorname ?? string.Empty,
                            Nachname: mitglied.Name ?? string.Empty,
                            Count: g.Count());
                    })
                    .OrderBy(x => x.Nachname, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => x.Vorname, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            },
            new List<(int MitgliedId, string Vorname, string Nachname, int Count)>());
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

        public Task<HomeOverviewDTO> GetHomeOverviewAsync(UserRole role, int? mitgliedId) => ExecuteAsync(
            "GetHomeOverviewAsync",
            async () =>
            {
                var overview = HomeOverviewFactory.Build(role);
                var operationalItems = new List<HomeOperationalItem>();
                HomeWorkHoursSummary? workHoursSummary = null;
                var workAssignmentsEmptyText = "Für Home sind aktuell keine Arbeitseinsätze in der Startseiten-View vorhanden.";
                var appointmentsEmptyText = "Für Home sind aktuell keine Termine in der Startseiten-View vorhanden.";
                var announcementEmptyText = "Für Home sind aktuell keine Bekanntmachungen in der Startseiten-View vorhanden.";

                if (mitgliedId is > 0)
                {
                    var homeMitgliedId = await ResolveHomeMitgliedIdAsync(mitgliedId.Value);
                    var (loadedSummary, summaryLoaded) = await TryLoadHomeSectionAsync(
                        "LoadPflichtstundenSummaryAsync",
                        () => LoadPflichtstundenSummaryAsync(homeMitgliedId, DateTime.Today.Year),
                        (HomeWorkHoursSummary?)null);

                    workHoursSummary = loadedSummary;
                    if (workHoursSummary != null)
                        operationalItems.Add(BuildWorkHoursItem(workHoursSummary));
                    else if (!summaryLoaded)
                        workHoursSummary = new HomeWorkHoursSummary { Year = DateTime.Today.Year, RuleReason = "Pflichtstunden konnten aktuell nicht geladen werden. Details stehen im Debug-/Anwendungslog." };
                }

                var (workAssignments, workAssignmentsLoaded) = await TryLoadHomeSectionAsync(
                    "LoadStartseiteArbeitseinsaetzeAsync",
                    LoadStartseiteArbeitseinsaetzeAsync,
                    new List<HomeWorkAssignmentItem>());

                var (appointments, appointmentsLoaded) = await TryLoadHomeSectionAsync(
                    "LoadStartseiteTermineAsync",
                    LoadStartseiteTermineAsync,
                    new List<HomeAppointmentItem>());

                var (announcements, announcementsLoaded) = await TryLoadHomeSectionAsync(
                    "LoadStartseiteBekanntmachungenAsync",
                    LoadStartseiteBekanntmachungenAsync,
                    new List<HomeAnnouncementItem>());

                if (!workAssignmentsLoaded)
                    workAssignmentsEmptyText = "Arbeitseinsätze konnten aktuell nicht geladen werden. Details stehen im Debug-/Anwendungslog.";

                if (!appointmentsLoaded)
                    appointmentsEmptyText = "Termine konnten aktuell nicht geladen werden. Details stehen im Debug-/Anwendungslog.";

                if (!announcementsLoaded)
                    announcementEmptyText = "Bekanntmachungen konnten aktuell nicht geladen werden. Details stehen im Debug-/Anwendungslog.";
                else if (announcements.Count == 0)
                    LogHomeLoadInfo("LoadStartseiteBekanntmachungenAsync", "Die Startseiten-View lieferte aktuell keine Bekanntmachungen.");

                return new HomeOverviewDTO
                {
                    Description = overview.Description,
                    QuickLinksTitle = overview.QuickLinksTitle,
                    QuickLinksEmptyText = overview.QuickLinksEmptyText,
                    OperationalTitle = overview.OperationalTitle,
                    OperationalEmptyText = overview.OperationalEmptyText,
                    AnnouncementTitle = overview.AnnouncementTitle,
                    AnnouncementHintText = overview.AnnouncementHintText,
                    AnnouncementEmptyText = announcementEmptyText,
                    WorkAssignmentsEmptyText = workAssignmentsEmptyText,
                    AppointmentsEmptyText = appointmentsEmptyText,
                    WorkHoursSummary = workHoursSummary,
                    WorkAssignments = workAssignments,
                    Appointments = appointments,
                    QuickLinks = overview.QuickLinks,
                    OperationalItems = operationalItems,
                    Announcements = announcements
                };
            },
            HomeOverviewFactory.Build(role));

        private async Task<(T Result, bool Success)> TryLoadHomeSectionAsync<T>(string sectionName, Func<Task<T>> loader, T fallback)
        {
            try
            {
                return (await loader(), true);
            }
            catch (Exception ex)
            {
                LogHomeLoadFailure(sectionName, ex);
                return (fallback, false);
            }
        }

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

        private static DateTime NormalizeDateTime(DateTime value)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
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

        private static bool IsMeterActiveOn(DateTime eingebautAm, DateTime? ausgebautAm, DateTime onDate)
        {
            var date = onDate.Date;
            return eingebautAm.Date <= date && (ausgebautAm == null || ausgebautAm.Value.Date >= date);
        }

        private async Task<List<StromzaehlerRecord>> GetStromzaehlerForParzelleAsync(int parzelleId)
        {
            var client = await EnsureClientAsync();
            var response = await client
                .From<StromzaehlerRecord>()
                .Where(x => x.ParzelleId == parzelleId)
                .Get();

            return response?.Models?
                .OrderByDescending(x => x.EingebautAm)
                .ToList()
                ?? new List<StromzaehlerRecord>();
        }

        private async Task<List<WasserzaehlerRecord>> GetWasserzaehlerForParzelleAsync(int parzelleId)
        {
            var client = await EnsureClientAsync();
            var response = await client
                .From<WasserzaehlerRecord>()
                .Where(x => x.ParzelleId == parzelleId)
                .Get();

            return response?.Models?
                .OrderByDescending(x => x.EingebautAm)
                .ToList()
                ?? new List<WasserzaehlerRecord>();
        }

        private async Task<List<ZaehlerAblesungDTO>> GetAblesungenAsync<TMeter>(IReadOnlyCollection<TMeter> meters, short zaehlerTyp)
            where TMeter : class
        {
            if (meters.Count == 0)
                return new List<ZaehlerAblesungDTO>();

            var client = await EnsureClientAsync();
            var response = await client.From<AblesungRecord>().Get();
            var ablesungen = response?.Models ?? new List<AblesungRecord>();

            if (typeof(TMeter) == typeof(StromzaehlerRecord))
            {
                var meterById = meters.Cast<StromzaehlerRecord>().ToDictionary(x => x.Id, x => x);
                return ablesungen
                    .Where(x => x.ZaehlerTyp == zaehlerTyp && meterById.ContainsKey(x.ZaehlerId))
                    .OrderByDescending(x => x.Ablesedatum)
                    .ThenByDescending(x => x.Id)
                    .Select(x => MapZaehlerAblesungDto(x, meterById[x.ZaehlerId].Zaehlernummer, meterById[x.ZaehlerId].Eichdatum))
                    .ToList();
            }

            var wasserById = meters.Cast<WasserzaehlerRecord>().ToDictionary(x => x.Id, x => x);
            return ablesungen
                .Where(x => x.ZaehlerTyp == zaehlerTyp && wasserById.ContainsKey(x.ZaehlerId))
                .OrderByDescending(x => x.Ablesedatum)
                .ThenByDescending(x => x.Id)
                .Select(x => MapZaehlerAblesungDto(x, wasserById[x.ZaehlerId].Zaehlernummer, wasserById[x.ZaehlerId].Eichdatum))
                .ToList();
        }

        private static ZaehlerAblesungDTO MapZaehlerAblesungDto(AblesungRecord record, string zaehlernummer, DateTime eichdatum)
        {
            return new ZaehlerAblesungDTO
            {
                AblesungId = record.Id,
                ZaehlerId = record.ZaehlerId,
                Ablesedatum = record.Ablesedatum,
                Stand = record.Stand,
                Zaehlernummer = zaehlernummer,
                Eichdatum = eichdatum,
                FotoPfad = record.FotoPfad
            };
        }

        private static string? FormatMemberName(MitgliedRecord? member)
        {
            if (member == null)
                return null;

            var fullName = $"{member.Vorname} {member.Name}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? member.Email : fullName;
        }

        private void LogHomeLoadFailure(string sectionName, Exception ex)
        {
            _logger?.LogError(ex, "Home load section {SectionName} failed.", sectionName);
            Debug.WriteLine($"[HomeLoad] {sectionName} failed: {ex}");
        }

        private void LogHomeLoadInfo(string sectionName, string message)
        {
            _logger?.LogInformation("Home load section {SectionName}: {Message}", sectionName, message);
            Debug.WriteLine($"[HomeLoad] {sectionName}: {message}");
        }

        private async Task<HomeWorkHoursSummary?> LoadPflichtstundenSummaryAsync(int mitgliedId, int year)
        {
            var client = await EnsureClientAsync();
            var currentSeason = (await GetSaisonRecordsAsync())
                .OrderByDescending(x => x.Jahr == year)
                .ThenByDescending(x => x.Jahr)
                .FirstOrDefault();

            var allResponse = await client
                .From<PflichtstundenUebersichtRecord>()
                .Get();

            var allRecords = allResponse?.Models?.ToList() ?? new List<PflichtstundenUebersichtRecord>();
            LogHomeLoadInfo("LoadPflichtstundenSummaryAsync", $"Pflichtstunden-View lieferte {allRecords.Count} Datensätze vor dem Home-Filter.");

            var matchingRecords = allRecords
                .Where(x => MatchesHomeMitglied(x, mitgliedId))
                .ToList();

            LogHomeLoadInfo("LoadPflichtstundenSummaryAsync", $"Pflichtstunden-View lieferte {matchingRecords.Count} Datensätze für Mitglied/Hauptmitglied {mitgliedId}.");

            PflichtstundenUebersichtRecord? record = null;

            if (currentSeason != null)
                record = matchingRecords.FirstOrDefault(x => x.SaisonId == currentSeason.Id);

            record ??= matchingRecords
                .OrderByDescending(GetPflichtstundenYear)
                .FirstOrDefault(x => GetPflichtstundenYear(x) == year);

            record ??= matchingRecords
                .OrderByDescending(x => x.SaisonId == currentSeason?.Id)
                .ThenByDescending(GetPflichtstundenYear)
                .FirstOrDefault();

            if (record == null)
                return null;

            return new HomeWorkHoursSummary
            {
                Year = GetPflichtstundenYear(record),
                RequiredHours = record.PflichtstundenSoll,
                WorkedHours = record.GeleisteteStunden,
                OpenHours = record.OffeneStunden,
                HasMaintenanceContract = record.HatWartungsvertrag,
                IsAgeExempt = record.Altersbefreit,
                IsExempt = record.IstBefreit,
                RuleReason = record.Regelgrund?.Trim() ?? string.Empty
            };
        }

        private async Task<List<HomeWorkAssignmentItem>> LoadStartseiteArbeitseinsaetzeAsync()
        {
            var client = await EnsureClientAsync();
            var response = await client.From<StartseiteArbeitseinsatzRecord>().Get();

            return response?.Models?
                .OrderBy(x => x.Datum ?? DateTime.MaxValue)
                .ThenBy(x => FirstNonEmpty(x.Titel, x.Thema) ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                .Select(MapHomeWorkAssignment)
                .ToList()
                ?? new List<HomeWorkAssignmentItem>();
        }

        private async Task<List<HomeAppointmentItem>> LoadStartseiteTermineAsync()
        {
            var client = await EnsureClientAsync();
            var response = await client.From<StartseiteTerminRecord>().Get();

            return response?.Models?
                .OrderBy(x => x.Datum ?? DateTime.MaxValue)
                .ThenBy(x => FirstNonEmpty(x.Titel, x.Thema) ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                .Select(MapHomeAppointment)
                .ToList()
                ?? new List<HomeAppointmentItem>();
        }

        private async Task<List<HomeAnnouncementItem>> LoadStartseiteBekanntmachungenAsync()
        {
            var client = await EnsureClientAsync();
            var response = await client.From<StartseiteBekanntmachungRecord>().Get();

            return response?.Models?
                .OrderByDescending(x => x.VeroeffentlichtAm ?? x.UpdatedAt ?? DateTime.MinValue)
                .ThenBy(x => FirstNonEmpty(x.Titel, x.Thema) ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                .Select(MapHomeAnnouncement)
                .ToList()
                ?? new List<HomeAnnouncementItem>();
        }

        private static HomeOperationalItem BuildWorkHoursItem(HomeWorkHoursSummary summary)
        {
            var parts = new List<string>();
            if (summary.RequiredHours.HasValue)
                parts.Add($"Soll {FormatHours(summary.RequiredHours.Value)}");
            if (summary.WorkedHours.HasValue)
                parts.Add($"geleistet {FormatHours(summary.WorkedHours.Value)}");
            if (summary.OpenHours.HasValue)
                parts.Add($"offen {FormatHours(summary.OpenHours.Value)}");
            if (!string.IsNullOrWhiteSpace(summary.RuleReason))
                parts.Add(summary.RuleReason);

            return new HomeOperationalItem
            {
                Title = $"Arbeitsstunden {summary.Year}",
                Message = string.Join(" · ", parts),
                IsWarning = summary.OpenHours.GetValueOrDefault() > 0
            };
        }

        private static HomeWorkAssignmentItem MapHomeWorkAssignment(StartseiteArbeitseinsatzRecord record)
        {
            var details = new List<string>();
            if (!string.IsNullOrWhiteSpace(record.Treffpunkt))
                details.Add($"Treffpunkt: {record.Treffpunkt.Trim()}");

            var description = NormalizeHomeText(record.Beschreibung);
            if (!string.IsNullOrWhiteSpace(description))
                details.Add(description);

            var capacityText = BuildCapacityText(record.AngemeldetCount, record.FreiePlaetze);

            return new HomeWorkAssignmentItem
            {
                Title = FirstNonEmpty(record.Titel, record.Thema) ?? "Arbeitseinsatz",
                Subtitle = BuildDateSubtitle(record.Datum, record.Beginn, record.Ende),
                Details = string.Join(Environment.NewLine, details.Where(x => !string.IsNullOrWhiteSpace(x))),
                RegistrationInfo = !string.IsNullOrWhiteSpace(capacityText)
                    ? capacityText
                    : record.AnmeldungMoeglich == true
                        ? "Eine Anmeldung ist im aktuellen WPF-Stand noch nicht belastbar verdrahtet."
                        : string.Empty,
                CanRegister = false
            };
        }

        private static HomeAppointmentItem MapHomeAppointment(StartseiteTerminRecord record)
        {
            var details = new List<string>();
            if (!string.IsNullOrWhiteSpace(record.Ort))
                details.Add($"Ort: {record.Ort.Trim()}");

            var description = NormalizeHomeText(FirstNonEmpty(record.Inhalt, record.Beschreibung));
            if (!string.IsNullOrWhiteSpace(description))
                details.Add(description);

            return new HomeAppointmentItem
            {
                Title = FirstNonEmpty(record.Titel, record.Thema) ?? "Termin",
                Subtitle = BuildDateSubtitle(record.Datum, record.Beginn, record.Ende),
                Details = string.Join(Environment.NewLine + Environment.NewLine, details.Where(x => !string.IsNullOrWhiteSpace(x)))
            };
        }

        private static HomeAnnouncementItem MapHomeAnnouncement(StartseiteBekanntmachungRecord record)
        {
            var published = record.VeroeffentlichtAm ?? record.Datum ?? record.ErstelltAm ?? record.UpdatedAt;
            return new HomeAnnouncementItem
            {
                Title = FirstNonEmpty(record.Titel, record.Betreff, record.Thema) ?? "Bekanntmachung",
                Subtitle = published.HasValue ? published.Value.ToString("dd.MM.yyyy") : string.Empty,
                Content = NormalizeHomeText(FirstNonEmpty(record.Inhalt, record.Text, record.InhaltHtml, record.Beschreibung, record.Kurztext))
            };
        }

        private async Task<int> ResolveHomeMitgliedIdAsync(int mitgliedId)
        {
            var member = await GetMitgliedByIdAsync(mitgliedId);
            if (member?.HauptmitgliedId is > 0)
                return member.HauptmitgliedId.Value;

            return mitgliedId;
        }

        private static bool MatchesHomeMitglied(PflichtstundenUebersichtRecord record, int mitgliedId)
        {
            return record.HauptmitgliedId == mitgliedId
                || record.MitgliedId == mitgliedId;
        }

        private static int GetPflichtstundenYear(PflichtstundenUebersichtRecord record)
        {
            return record.Jahr ?? record.SaisonJahr ?? 0;
        }

        private static string BuildDateSubtitle(DateTime? date, string? begin, string? end)
        {
            var parts = new List<string>();
            if (date.HasValue)
                parts.Add(date.Value.ToString("dd.MM.yyyy"));

            var timePart = BuildTimeRange(begin, end);
            if (!string.IsNullOrWhiteSpace(timePart))
                parts.Add(timePart);

            return string.Join(" · ", parts);
        }

        private static string BuildTimeRange(string? begin, string? end)
        {
            begin = NormalizeTimeValue(begin);
            end = NormalizeTimeValue(end);

            if (string.IsNullOrWhiteSpace(begin) && string.IsNullOrWhiteSpace(end))
                return string.Empty;
            if (string.IsNullOrWhiteSpace(end))
                return begin ?? string.Empty;
            if (string.IsNullOrWhiteSpace(begin))
                return end ?? string.Empty;

            return $"{begin} – {end}";
        }

        private static string? NormalizeTimeValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            value = value.Trim();
            return TimeSpan.TryParse(value, out var time)
                ? time.ToString(@"hh\:mm")
                : value;
        }

        private static string BuildCapacityText(int? angemeldetCount, int? freiePlaetze)
        {
            var parts = new List<string>();
            if (angemeldetCount.HasValue)
                parts.Add($"Angemeldet: {angemeldetCount.Value}");
            if (freiePlaetze.HasValue)
                parts.Add($"Freie Plätze: {freiePlaetze.Value}");

            return string.Join(" · ", parts);
        }

        private static string NormalizeHomeText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value
                .Replace("<br>", Environment.NewLine, StringComparison.OrdinalIgnoreCase)
                .Replace("<br/>", Environment.NewLine, StringComparison.OrdinalIgnoreCase)
                .Replace("<br />", Environment.NewLine, StringComparison.OrdinalIgnoreCase)
                .Replace("</p>", Environment.NewLine + Environment.NewLine, StringComparison.OrdinalIgnoreCase);

            normalized = Regex.Replace(normalized, "<[^>]+>", string.Empty);
            normalized = normalized.Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase);
            return normalized.Trim();
        }

        private static string FormatHours(decimal value)
        {
            return $"{value:0.##} h";
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
