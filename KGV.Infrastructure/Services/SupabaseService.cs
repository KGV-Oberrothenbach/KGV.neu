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
using KGV.Core.Utilities;
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

        public Task<RfidAssignmentCheckResult> CheckParzelleRfidAssignmentAsync(int parzelleId, string medium, string uid) => ExecuteAsync(
            "CheckParzelleRfidAssignmentAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                return await CheckParzelleRfidAssignmentInternalAsync(client, parzelleId, medium, uid);
            },
            new RfidAssignmentCheckResult
            {
                IsValid = false,
                Message = "Die RFID-Zuordnung konnte aktuell nicht geprüft werden."
            });

        public Task<RfidAssignmentResult> AssignParzelleRfidAsync(int parzelleId, string medium, string uid, bool overwriteExisting = false) => ExecuteAsync(
            "AssignParzelleRfidAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var check = await CheckParzelleRfidAssignmentInternalAsync(client, parzelleId, medium, uid);

                if (!check.IsValid)
                {
                    return new RfidAssignmentResult
                    {
                        Success = false,
                        Message = check.Message,
                        NormalizedUid = check.NormalizedUid
                    };
                }

                if (check.AlreadyAssignedToTarget)
                {
                    return new RfidAssignmentResult
                    {
                        Success = true,
                        Message = check.Message,
                        NormalizedUid = check.NormalizedUid,
                        UpdatedParzelle = await GetParzelleByIdInternalAsync(client, parzelleId)
                    };
                }

                if (check.RequiresOverwriteConfirmation && !overwriteExisting)
                {
                    return new RfidAssignmentResult
                    {
                        Success = false,
                        RequiresOverwriteConfirmation = true,
                        Message = check.Message,
                        NormalizedUid = check.NormalizedUid
                    };
                }

                try
                {
                    await client.Rpc<ParzelleRecord>(
                        "assign_parzelle_rfid",
                        new
                        {
                            p_parzelle_id = parzelleId,
                            p_medium = NormalizeRfidMedium(medium),
                            p_rfid_tag_uid = check.NormalizedUid
                        });
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "AssignParzelleRfidAsync RPC failed for parzelle {ParzelleId} and medium {Medium}", parzelleId, medium);

                    var refreshedCheck = await CheckParzelleRfidAssignmentInternalAsync(client, parzelleId, medium, uid);
                    if (refreshedCheck.AlreadyAssignedToTarget)
                    {
                        return new RfidAssignmentResult
                        {
                            Success = true,
                            Message = $"Die RFID {refreshedCheck.NormalizedUid} ist jetzt für {MediumDisplayName(medium)} bei der gewählten Parzelle hinterlegt.",
                            NormalizedUid = refreshedCheck.NormalizedUid,
                            UpdatedParzelle = await GetParzelleByIdInternalAsync(client, parzelleId)
                        };
                    }

                    return new RfidAssignmentResult
                    {
                        Success = false,
                        Message = refreshedCheck.IsValid
                            ? "Die RFID konnte aktuell nicht gespeichert werden. Bitte versuche es erneut."
                            : refreshedCheck.Message,
                        NormalizedUid = refreshedCheck.NormalizedUid
                    };
                }

                var updatedParzelle = await GetParzelleByIdInternalAsync(client, parzelleId);
                return new RfidAssignmentResult
                {
                    Success = true,
                    Message = $"Die RFID {check.NormalizedUid} wurde für {MediumDisplayName(medium)} gespeichert.",
                    NormalizedUid = check.NormalizedUid,
                    UpdatedParzelle = updatedParzelle
                };
            },
            new RfidAssignmentResult
            {
                Success = false,
                Message = "Die RFID konnte aktuell nicht gespeichert werden."
            });

        public Task<List<ZaehlerEichstatusRecord>> GetZaehlerEichstatusAsync() => ExecuteAsync(
            "GetZaehlerEichstatusAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client.From<ZaehlerEichstatusRecord>().Get();

                return response?.Models?
                    .OrderBy(x => x.SortPriority)
                    .ThenBy(x => x.SortDays)
                    .ThenBy(x => x.GartenSortKey, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => x.AnlageDisplay, StringComparer.CurrentCultureIgnoreCase)
                    .ToList()
                    ?? new List<ZaehlerEichstatusRecord>();
            },
            new List<ZaehlerEichstatusRecord>());

        public Task<RfidScanContextResult> ResolveRfidScanContextAsync(string uid) => ExecuteAsync(
            "ResolveRfidScanContextAsync",
            async () =>
            {
                var normalizedUid = NormalizeRfidTagUid(uid);
                if (normalizedUid == null)
                {
                    return new RfidScanContextResult
                    {
                        State = RfidScanContextState.Unknown,
                        Message = "Bitte eine RFID-UID eingeben.",
                        NormalizedUid = string.Empty
                    };
                }

                var client = await EnsureClientAsync();
                var response = await client
                    .From<RfidScanContextRecord>()
                    .Where(x => x.RfidTagUid == normalizedUid)
                    .Get();

                var context = response?.Models?
                    .OrderByDescending(x => x.HasActiveMeter)
                    .FirstOrDefault();

                if (context == null)
                {
                    return new RfidScanContextResult
                    {
                        State = RfidScanContextState.Unknown,
                        Message = $"Für die UID {normalizedUid} wurde kein RFID-Kontext gefunden.",
                        NormalizedUid = normalizedUid
                    };
                }

                var state = context.HasActiveMeter
                    ? RfidScanContextState.KnownWithActiveMeter
                    : RfidScanContextState.KnownWithoutActiveMeter;

                var message = state == RfidScanContextState.KnownWithActiveMeter
                    ? $"RFID-Kontext für {context.ParzelleDisplayName} mit aktivem {context.MediumDisplay.ToLowerInvariant()}zähler geladen."
                    : $"RFID-Kontext für {context.ParzelleDisplayName} geladen, aktuell jedoch ohne aktiven Zähler.";

                return new RfidScanContextResult
                {
                    State = state,
                    Message = message,
                    NormalizedUid = normalizedUid,
                    Context = context
                };
            },
            new RfidScanContextResult
            {
                State = RfidScanContextState.Unknown,
                Message = "Der RFID-Kontext konnte aktuell nicht geladen werden."
            });

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

        public Task<List<ArbeitsstundeDTO>> GetOffeneArbeitsstundenZurFreigabeAsync() => ExecuteAsync(
            "GetOffeneArbeitsstundenZurFreigabeAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client.From<ArbeitsstundeRecord>().Get();
                var records = response?.Models?
                    .Where(IsArbeitsstundeOffen)
                    .OrderBy(x => x.Datum)
                    .ThenBy(x => x.Id)
                    .ToList()
                    ?? new List<ArbeitsstundeRecord>();

                if (records.Count == 0)
                    return new List<ArbeitsstundeDTO>();

                var mitglieder = await GetMitgliederAsync();
                var operativeMitglieder = mitglieder
                    .Where(OperationalDataFilter.IsOperationalMember)
                    .ToDictionary(x => x.Id, x => x);

                records = records
                    .Where(x => operativeMitglieder.ContainsKey(x.MitgliedId))
                    .ToList();

                if (records.Count == 0)
                    return new List<ArbeitsstundeDTO>();

                var saisonById = (await GetSaisonRecordsAsync()).ToDictionary(x => x.Id, x => x);

                return records.Select(record =>
                {
                    operativeMitglieder.TryGetValue(record.MitgliedId, out var mitglied);
                    MitgliedRecord? approver = null;
                    if (record.GenehmigtVon.HasValue)
                        operativeMitglieder.TryGetValue(record.GenehmigtVon.Value, out approver);

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
                var payload = CreateArbeitsstundeInsertPayload(record);
                await client.From<ArbeitsstundeInsertRecord>().Insert(payload);

                return true;
            },
            false);

        private ArbeitsstundeInsertRecord CreateArbeitsstundeInsertPayload(ArbeitsstundeRecord record)
        {
            return new ArbeitsstundeInsertRecord
            {
                MitgliedId = record.MitgliedId,
                SaisonId = record.SaisonId,
                Datum = NormalizeDateOnly(record.Datum),
                Stunden = record.Stunden,
                ArtDerArbeit = record.ArtDerArbeit.Trim(),
                Status = CleanOptionalText(record.Status),
                Freigegeben = record.Freigegeben,
                GenehmigtAm = record.GenehmigtAm,
                GenehmigtVon = record.GenehmigtVon,
                LockedByUserId = null,
                LockedAt = null
            };
        }

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
                    .Set(x => x.Status, CleanOptionalText(record.Status))
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
                    .Where(IsArbeitsstundeOffen)
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

        public Task<ArbeitsstundenReviewLockResult> TryAcquireArbeitsstundenReviewLockAsync(string userId, int timeoutMinutes = 10) => ExecuteAsync(
            "TryAcquireArbeitsstundenReviewLockAsync",
            async () =>
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return new ArbeitsstundenReviewLockResult();

                var client = await EnsureClientAsync();
                var response = await client.From<ArbeitsstundeRecord>().Get();
                var offeneArbeitsstunden = response?.Models?
                    .Where(IsArbeitsstundeOffen)
                    .ToList()
                    ?? new List<ArbeitsstundeRecord>();

                if (offeneArbeitsstunden.Count == 0)
                    return new ArbeitsstundenReviewLockResult { Acquired = true, LockedByUserId = userId };

                var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                var aktiveFremdsperre = offeneArbeitsstunden
                    .FirstOrDefault(x => HasActiveArbeitsstundenLock(x, userId, now, timeoutMinutes));

                if (aktiveFremdsperre != null)
                {
                    var blocked = await BuildArbeitsstundenReviewLockResultAsync(false, aktiveFremdsperre.LockedByUserId, aktiveFremdsperre.LockedAt);
                    _logger?.LogInformation("Arbeitsstunden review lock blocked by {LockedByUserId} since {LockedAt}", blocked.LockedByUserId, blocked.LockedAt);
                    return blocked;
                }

                foreach (var record in offeneArbeitsstunden)
                {
                    await client
                        .From<ArbeitsstundeRecord>()
                        .Where(x => x.Id == record.Id)
                        .Set(x => x.LockedByUserId, userId)
                        .Set(x => x.LockedAt, now)
                        .Update();
                }

                _logger?.LogInformation("Arbeitsstunden review lock acquired by {UserId} for {Count} rows", userId, offeneArbeitsstunden.Count);
                return await BuildArbeitsstundenReviewLockResultAsync(true, userId, now);
            },
            new ArbeitsstundenReviewLockResult());

        public Task<bool> RefreshArbeitsstundenReviewLockAsync(string userId, int timeoutMinutes = 10) => ExecuteAsync(
            "RefreshArbeitsstundenReviewLockAsync",
            async () =>
            {
                var result = await TryAcquireArbeitsstundenReviewLockAsync(userId, timeoutMinutes);
                return result.Acquired;
            },
            false);

        public Task<bool> ReleaseArbeitsstundenReviewLockAsync(string userId, bool force = false) => ExecuteAsync(
            "ReleaseArbeitsstundenReviewLockAsync",
            async () =>
            {
                if (string.IsNullOrWhiteSpace(userId) && !force)
                    return false;

                var client = await EnsureClientAsync();
                var response = await client.From<ArbeitsstundeRecord>().Get();
                var lockedRows = response?.Models?
                    .Where(x => force
                        ? !string.IsNullOrWhiteSpace(x.LockedByUserId)
                        : string.Equals(x.LockedByUserId, userId, StringComparison.OrdinalIgnoreCase))
                    .ToList()
                    ?? new List<ArbeitsstundeRecord>();

                foreach (var record in lockedRows)
                {
                    await client
                        .From<ArbeitsstundeRecord>()
                        .Where(x => x.Id == record.Id)
                        .Set(x => x.LockedByUserId, (string?)null)
                        .Set(x => x.LockedAt, (DateTime?)null)
                        .Update();
                }

                if (lockedRows.Count > 0)
                    _logger?.LogInformation("Arbeitsstunden review lock released by {UserId} for {Count} rows", userId, lockedRows.Count);

                return true;
            },
            false);
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

        public Task<List<HomeWorkAssignmentItem>> GetStartseiteArbeitseinsaetzeAsync() => ExecuteAsync(
            "GetStartseiteArbeitseinsaetzeAsync",
            LoadStartseiteArbeitseinsaetzeAsync,
            new List<HomeWorkAssignmentItem>());

        public Task<HomeWorkAssignmentItem?> GetStartseiteArbeitseinsatzByIdAsync(int arbeitseinsatzId) => ExecuteAsync<HomeWorkAssignmentItem?>(
            "GetStartseiteArbeitseinsatzByIdAsync",
            async () =>
            {
                if (arbeitseinsatzId <= 0)
                    return null;

                var client = await EnsureClientAsync();
                return await TryLoadHomeWorkAssignmentItemAsync(client, arbeitseinsatzId);
            },
            null);

        public Task<List<WorkAssignmentParticipantItem>> GetArbeitseinsatzParticipantsAsync(int arbeitseinsatzId) => ExecuteAsync(
            "GetArbeitseinsatzParticipantsAsync",
            async () =>
            {
                if (arbeitseinsatzId <= 0)
                    return new List<WorkAssignmentParticipantItem>();

                var client = await EnsureClientAsync();
                var activeRegistrations = await GetAktiveArbeitseinsatzAnmeldungenAsync(client, arbeitseinsatzId);
                if (activeRegistrations.Count == 0)
                    return new List<WorkAssignmentParticipantItem>();

                var members = await GetMitgliederAsync();
                var memberById = members.ToDictionary(x => x.Id, x => x);

                return activeRegistrations
                    .OrderBy(x => x.AngemeldetAm)
                    .ThenBy(x => x.MitgliedId)
                    .Select(x => new WorkAssignmentParticipantItem
                    {
                        MitgliedId = x.MitgliedId,
                        DisplayName = memberById.TryGetValue(x.MitgliedId, out var member)
                            ? (FormatMemberName(member) ?? $"Mitglied #{x.MitgliedId}")
                            : $"Mitglied #{x.MitgliedId}",
                        StatusText = $"Angemeldet am {x.AngemeldetAm:dd.MM.yyyy HH:mm}"
                    })
                    .ToList();
            },
            new List<WorkAssignmentParticipantItem>());

        public Task<List<HomeAppointmentItem>> GetStartseiteTermineAsync() => ExecuteAsync(
            "GetStartseiteTermineAsync",
            LoadStartseiteTermineAsync,
            new List<HomeAppointmentItem>());

        public Task<WorkAssignmentRegistrationResult> SignUpForArbeitseinsatzAsync(int arbeitseinsatzId, int mitgliedId) => ExecuteAsync(
            "SignUpForArbeitseinsatzAsync",
            async () =>
            {
                if (arbeitseinsatzId <= 0 || mitgliedId <= 0)
                    return CreateRegistrationResult(false, "Die Anmeldung konnte nicht gestartet werden, weil Arbeitseinsatz oder Mitglied fehlen.");

                var client = await EnsureClientAsync();
                var arbeitseinsatz = await GetArbeitseinsatzByIdAsync(client, arbeitseinsatzId);
                if (arbeitseinsatz == null)
                    return CreateRegistrationResult(false, "Der ausgewählte Arbeitseinsatz konnte nicht geladen werden.");

                var aktiveAnmeldungen = await GetAktiveArbeitseinsatzAnmeldungenAsync(client, arbeitseinsatzId);
                if (aktiveAnmeldungen.Any(x => x.MitgliedId == mitgliedId))
                {
                    var existingItem = await TryLoadHomeWorkAssignmentItemAsync(client, arbeitseinsatzId);
                    return CreateRegistrationResult(false, "Für diesen Arbeitseinsatz besteht bereits eine Anmeldung.", existingItem);
                }

                var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                if (arbeitseinsatz.AnmeldungBis.HasValue && arbeitseinsatz.AnmeldungBis.Value < now)
                {
                    var expiredItem = await TryLoadHomeWorkAssignmentItemAsync(client, arbeitseinsatzId);
                    return CreateRegistrationResult(false, "Die Anmeldefrist für diesen Arbeitseinsatz ist bereits abgelaufen.", expiredItem);
                }

                if (arbeitseinsatz.MaxTeilnehmer.HasValue && aktiveAnmeldungen.Count >= arbeitseinsatz.MaxTeilnehmer.Value)
                {
                    var fullItem = await TryLoadHomeWorkAssignmentItemAsync(client, arbeitseinsatzId);
                    return CreateRegistrationResult(false, "Für diesen Arbeitseinsatz sind aktuell keine freien Plätze mehr verfügbar.", fullItem);
                }

                try
                {
                    await client.Rpc<ArbeitseinsatzAnmeldungRecord>(
                        "sign_up_for_arbeitseinsatz",
                        new
                        {
                            p_arbeitseinsatz_id = arbeitseinsatzId,
                            p_mitglied_id = mitgliedId
                        });
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "SignUpForArbeitseinsatzAsync RPC failed for arbeitseinsatz {ArbeitseinsatzId} and mitglied {MitgliedId}", arbeitseinsatzId, mitgliedId);

                    var refreshedActiveAnmeldungen = await GetAktiveArbeitseinsatzAnmeldungenAsync(client, arbeitseinsatzId);
                    var refreshedItem = await TryLoadHomeWorkAssignmentItemAsync(client, arbeitseinsatzId);

                    if (refreshedActiveAnmeldungen.Any(x => x.MitgliedId == mitgliedId))
                        return CreateRegistrationResult(false, "Für diesen Arbeitseinsatz besteht bereits eine Anmeldung.", refreshedItem);

                    return CreateRegistrationResult(false, "Die Anmeldung konnte aktuell nicht gespeichert werden. Bitte versuche es erneut.", refreshedItem);
                }

                var updatedItem = await TryLoadHomeWorkAssignmentItemAsync(client, arbeitseinsatzId);
                return CreateRegistrationResult(true, "Die Anmeldung zum Arbeitseinsatz wurde gespeichert.", updatedItem);
            },
            new WorkAssignmentRegistrationResult());

        public Task<WorkAssignmentRegistrationResult> SignOffFromArbeitseinsatzAsync(int arbeitseinsatzId, int mitgliedId) => ExecuteAsync(
            "SignOffFromArbeitseinsatzAsync",
            async () =>
            {
                if (arbeitseinsatzId <= 0 || mitgliedId <= 0)
                    return CreateRegistrationResult(false, "Die Abmeldung konnte nicht gestartet werden, weil Arbeitseinsatz oder Mitglied fehlen.");

                var client = await EnsureClientAsync();
                var arbeitseinsatz = await GetArbeitseinsatzByIdAsync(client, arbeitseinsatzId);
                if (arbeitseinsatz == null)
                    return CreateRegistrationResult(false, "Der ausgewählte Arbeitseinsatz konnte nicht geladen werden.");

                var aktiveAnmeldungen = await GetAktiveArbeitseinsatzAnmeldungenAsync(client, arbeitseinsatzId);
                if (!aktiveAnmeldungen.Any(x => x.MitgliedId == mitgliedId))
                {
                    var missingItem = await TryLoadHomeWorkAssignmentItemAsync(client, arbeitseinsatzId);
                    return CreateRegistrationResult(false, "Für dieses Mitglied besteht aktuell keine aktive Anmeldung zu diesem Arbeitseinsatz.", missingItem);
                }

                try
                {
                    await client.Rpc<ArbeitseinsatzAnmeldungRecord>(
                        "sign_off_from_arbeitseinsatz",
                        new
                        {
                            p_arbeitseinsatz_id = arbeitseinsatzId,
                            p_mitglied_id = mitgliedId
                        });
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "SignOffFromArbeitseinsatzAsync RPC failed for arbeitseinsatz {ArbeitseinsatzId} and mitglied {MitgliedId}", arbeitseinsatzId, mitgliedId);

                    var refreshedActiveAnmeldungen = await GetAktiveArbeitseinsatzAnmeldungenAsync(client, arbeitseinsatzId);
                    var refreshedItem = await TryLoadHomeWorkAssignmentItemAsync(client, arbeitseinsatzId);

                    if (!refreshedActiveAnmeldungen.Any(x => x.MitgliedId == mitgliedId))
                        return CreateRegistrationResult(true, "Die Abmeldung vom Arbeitseinsatz wurde gespeichert.", refreshedItem);

                    return CreateRegistrationResult(false, "Die Abmeldung konnte aktuell nicht gespeichert werden. Bitte versuche es erneut.", refreshedItem);
                }

                var updatedItem = await TryLoadHomeWorkAssignmentItemAsync(client, arbeitseinsatzId);
                return CreateRegistrationResult(true, "Die Abmeldung vom Arbeitseinsatz wurde gespeichert.", updatedItem);
            },
            new WorkAssignmentRegistrationResult());

        public Task<List<HomeAnnouncementItem>> GetStartseiteBekanntmachungenAsync() => ExecuteAsync(
            "GetStartseiteBekanntmachungenAsync",
            LoadStartseiteBekanntmachungenAsync,
            new List<HomeAnnouncementItem>());

        public Task<List<ArbeitseinsatzRecord>> GetArbeitseinsaetzeVerwaltungAsync() => ExecuteAsync(
            "GetArbeitseinsaetzeVerwaltungAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client.From<ArbeitseinsatzRecord>().Get();

                return response?.Models?
                    .Select(NormalizeArbeitseinsatzRecord)
                    .OrderBy(x => x.Datum)
                    .ThenBy(x => x.StartUhrzeit ?? TimeSpan.MaxValue)
                    .ThenBy(x => x.Titel ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                    .ToList()
                    ?? new List<ArbeitseinsatzRecord>();
            },
            new List<ArbeitseinsatzRecord>());

        public Task<ArbeitseinsatzRecord?> CreateArbeitseinsatzAsync(ArbeitseinsatzRecord record) => ExecuteAsync<ArbeitseinsatzRecord?>(
            "CreateArbeitseinsatzAsync",
            async () =>
            {
                if (record == null || string.IsNullOrWhiteSpace(record.Titel))
                    return null;

                var client = await EnsureClientAsync();
                var insertRecord = new ArbeitseinsatzRecord
                {
                    Titel = CleanRequiredText(record.Titel),
                    Beschreibung = CleanOptionalText(record.Beschreibung),
                    Datum = NormalizeDateOnly(record.Datum),
                    StartUhrzeit = NormalizeTerminTime(record.StartUhrzeit),
                    EndUhrzeit = NormalizeTerminTime(record.EndUhrzeit),
                    Treffpunkt = CleanOptionalText(record.Treffpunkt),
                    MaxTeilnehmer = record.MaxTeilnehmer,
                    StundenWert = record.StundenWert < 0 ? 0 : record.StundenWert,
                    SichtbarAb = NormalizeTimestampWithoutTimeZone(record.SichtbarAb),
                    SichtbarBis = NormalizeTimestampWithoutTimeZone(record.SichtbarBis),
                    AnmeldungBis = NormalizeTimestampWithoutTimeZone(record.AnmeldungBis),
                    Aktiv = record.Aktiv,
                    IsDemo = record.IsDemo
                };

                await client.From<ArbeitseinsatzRecord>().Insert(insertRecord);
                var reloadResponse = await client.From<ArbeitseinsatzRecord>().Get();
                var created = reloadResponse?.Models?
                    .Select(NormalizeArbeitseinsatzRecord)
                    .Where(x => IsSameArbeitseinsatzForReload(x, insertRecord))
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefault();

                _logger?.LogInformation("CreateArbeitseinsatzAsync created arbeitseinsatz {ArbeitseinsatzId}", created?.Id);
                return created;
            },
            null);

        public Task<bool> UpdateArbeitseinsatzAsync(ArbeitseinsatzRecord record) => ExecuteAsync(
            "UpdateArbeitseinsatzAsync",
            async () =>
            {
                if (record == null || record.Id <= 0 || string.IsNullOrWhiteSpace(record.Titel))
                    return false;

                var client = await EnsureClientAsync();
                await client
                    .From<ArbeitseinsatzRecord>()
                    .Where(x => x.Id == record.Id)
                    .Set(x => x.Titel, CleanRequiredText(record.Titel))
                    .Set(x => x.Beschreibung, CleanOptionalText(record.Beschreibung))
                    .Set(x => x.Datum, NormalizeDateOnly(record.Datum))
                    .Set(x => x.StartUhrzeit, NormalizeTerminTime(record.StartUhrzeit))
                    .Set(x => x.EndUhrzeit, NormalizeTerminTime(record.EndUhrzeit))
                    .Set(x => x.Treffpunkt, CleanOptionalText(record.Treffpunkt))
                    .Set(x => x.MaxTeilnehmer, record.MaxTeilnehmer)
                    .Set(x => x.StundenWert, record.StundenWert < 0 ? 0 : record.StundenWert)
                    .Set(x => x.SichtbarAb, NormalizeTimestampWithoutTimeZone(record.SichtbarAb))
                    .Set(x => x.SichtbarBis, NormalizeTimestampWithoutTimeZone(record.SichtbarBis))
                    .Set(x => x.AnmeldungBis, NormalizeTimestampWithoutTimeZone(record.AnmeldungBis))
                    .Set(x => x.Aktiv, record.Aktiv)
                    .Set(x => x.IsDemo, record.IsDemo)
                    .Update();

                _logger?.LogInformation("UpdateArbeitseinsatzAsync updated arbeitseinsatz {ArbeitseinsatzId}", record.Id);
                return true;
            },
            false);
        public Task<List<TerminRecord>> GetTermineVerwaltungAsync() => ExecuteAsync(
            "GetTermineVerwaltungAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client.From<TerminRecord>().Get();

                return response?.Models?
                    .Select(NormalizeTerminRecord)
                    .OrderBy(x => x.Datum)
                    .ThenBy(x => x.StartUhrzeit ?? TimeSpan.MaxValue)
                    .ThenBy(x => x.Titel ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                    .ToList()
                    ?? new List<TerminRecord>();
            },
            new List<TerminRecord>());

        public Task<TerminRecord?> CreateTerminAsync(TerminRecord record) => ExecuteAsync<TerminRecord?>(
            "CreateTerminAsync",
            async () =>
            {
                if (record == null || string.IsNullOrWhiteSpace(record.Titel))
                    return null;

                var client = await EnsureClientAsync();
                var insertRecord = new TerminRecord
                {
                    Titel = CleanRequiredText(record.Titel),
                    Beschreibung = CleanOptionalText(record.Beschreibung),
                    Datum = NormalizeDateOnly(record.Datum),
                    StartUhrzeit = NormalizeTerminTime(record.StartUhrzeit),
                    EndUhrzeit = NormalizeTerminTime(record.EndUhrzeit),
                    SichtbarAb = NormalizeTimestampWithoutTimeZone(record.SichtbarAb),
                    SichtbarBis = NormalizeTimestampWithoutTimeZone(record.SichtbarBis),
                    Aktiv = record.Aktiv
                };

                await client.From<TerminRecord>().Insert(insertRecord);
                var reloadResponse = await client.From<TerminRecord>().Get();
                var created = reloadResponse?.Models?
                    .Select(NormalizeTerminRecord)
                    .Where(x => IsSameTerminForReload(x, insertRecord))
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefault();

                _logger?.LogInformation("CreateTerminAsync created termin {TerminId}", created?.Id);
                return created;
            },
            null);

        public Task<bool> UpdateTerminAsync(TerminRecord record) => ExecuteAsync(
            "UpdateTerminAsync",
            async () =>
            {
                if (record == null || record.Id <= 0 || string.IsNullOrWhiteSpace(record.Titel))
                    return false;

                var client = await EnsureClientAsync();
                await client
                    .From<TerminRecord>()
                    .Where(x => x.Id == record.Id)
                    .Set(x => x.Titel, CleanRequiredText(record.Titel))
                    .Set(x => x.Beschreibung, CleanOptionalText(record.Beschreibung))
                    .Set(x => x.Datum, NormalizeDateOnly(record.Datum))
                    .Set(x => x.StartUhrzeit, NormalizeTerminTime(record.StartUhrzeit))
                    .Set(x => x.EndUhrzeit, NormalizeTerminTime(record.EndUhrzeit))
                    .Set(x => x.SichtbarAb, NormalizeTimestampWithoutTimeZone(record.SichtbarAb))
                    .Set(x => x.SichtbarBis, NormalizeTimestampWithoutTimeZone(record.SichtbarBis))
                    .Set(x => x.Aktiv, record.Aktiv)
                    .Update();

                _logger?.LogInformation("UpdateTerminAsync updated termin {TerminId}", record.Id);
                return true;
            },
            false);

        public Task<List<BekanntmachungRecord>> GetBekanntmachungenVerwaltungAsync() => ExecuteAsync(
            "GetBekanntmachungenVerwaltungAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client.From<BekanntmachungRecord>().Get();

                return response?.Models?
                    .Select(NormalizeBekanntmachungRecord)
                    .OrderBy(x => x.SortOrder ?? int.MaxValue)
                    .ThenByDescending(x => x.SichtbarAb ?? x.CreatedAt ?? DateTime.MinValue)
                    .ThenBy(x => x.Titel ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                    .ToList()
                    ?? new List<BekanntmachungRecord>();
            },
            new List<BekanntmachungRecord>());

        public Task<BekanntmachungRecord?> CreateBekanntmachungAsync(BekanntmachungRecord record) => ExecuteAsync<BekanntmachungRecord?>(
            "CreateBekanntmachungAsync",
            async () =>
            {
                if (record == null || string.IsNullOrWhiteSpace(record.Titel) || string.IsNullOrWhiteSpace(record.InhaltHtml))
                    return null;

                var client = await EnsureClientAsync();
                var insertRecord = new BekanntmachungRecord
                {
                    Titel = CleanRequiredText(record.Titel),
                    InhaltHtml = CleanRequiredText(record.InhaltHtml),
                    SichtbarAb = NormalizeTimestampWithoutTimeZone(record.SichtbarAb),
                    SichtbarBis = NormalizeTimestampWithoutTimeZone(record.SichtbarBis),
                    SortOrder = record.SortOrder,
                    Aktiv = record.Aktiv
                };

                await client.From<BekanntmachungRecord>().Insert(insertRecord);
                var reloadResponse = await client.From<BekanntmachungRecord>().Get();
                var created = reloadResponse?.Models?
                    .Select(NormalizeBekanntmachungRecord)
                    .Where(x => IsSameBekanntmachungForReload(x, insertRecord))
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefault();

                _logger?.LogInformation("CreateBekanntmachungAsync created bekanntmachung {BekanntmachungId}", created?.Id);
                return created;
            },
            null);

        public Task<bool> UpdateBekanntmachungAsync(BekanntmachungRecord record) => ExecuteAsync(
            "UpdateBekanntmachungAsync",
            async () =>
            {
                if (record == null || record.Id <= 0 || string.IsNullOrWhiteSpace(record.Titel) || string.IsNullOrWhiteSpace(record.InhaltHtml))
                    return false;

                var client = await EnsureClientAsync();
                await client
                    .From<BekanntmachungRecord>()
                    .Where(x => x.Id == record.Id)
                    .Set(x => x.Titel, CleanRequiredText(record.Titel))
                    .Set(x => x.InhaltHtml, CleanRequiredText(record.InhaltHtml))
                    .Set(x => x.SichtbarAb, NormalizeTimestampWithoutTimeZone(record.SichtbarAb))
                    .Set(x => x.SichtbarBis, NormalizeTimestampWithoutTimeZone(record.SichtbarBis))
                    .Set(x => x.SortOrder, record.SortOrder)
                    .Set(x => x.Aktiv, record.Aktiv)
                    .Update();

                _logger?.LogInformation("UpdateBekanntmachungAsync updated bekanntmachung {BekanntmachungId}", record.Id);
                return true;
            },
            false);

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

        private static TimeSpan? NormalizeTerminTime(TimeSpan? value)
        {
            return value.HasValue
                ? new TimeSpan(value.Value.Hours, value.Value.Minutes, 0)
                : null;
        }

        private static DateTime NormalizeDateOnly(DateTime value)
        {
            return new DateTime(value.Year, value.Month, value.Day, 0, 0, 0, DateTimeKind.Unspecified);
        }

        private static DateTime? NormalizeTimestampWithoutTimeZone(DateTime? value)
        {
            if (!value.HasValue)
                return null;

            var timestamp = value.Value;
            return new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, timestamp.Minute, timestamp.Second, timestamp.Millisecond, DateTimeKind.Unspecified);
        }

        private static ArbeitseinsatzRecord NormalizeArbeitseinsatzRecord(ArbeitseinsatzRecord record)
        {
            return new ArbeitseinsatzRecord
            {
                Id = record.Id,
                Titel = record.Titel,
                Beschreibung = record.Beschreibung,
                Datum = NormalizeDateOnly(record.Datum),
                StartUhrzeit = NormalizeTerminTime(record.StartUhrzeit),
                EndUhrzeit = NormalizeTerminTime(record.EndUhrzeit),
                Treffpunkt = record.Treffpunkt,
                MaxTeilnehmer = record.MaxTeilnehmer,
                StundenWert = record.StundenWert,
                SichtbarAb = NormalizeTimestampWithoutTimeZone(record.SichtbarAb),
                SichtbarBis = NormalizeTimestampWithoutTimeZone(record.SichtbarBis),
                AnmeldungBis = NormalizeTimestampWithoutTimeZone(record.AnmeldungBis),
                Aktiv = record.Aktiv,
                CreatedAt = record.CreatedAt,
                UpdatedAt = record.UpdatedAt,
                IsDemo = record.IsDemo
            };
        }

        private static TerminRecord NormalizeTerminRecord(TerminRecord record)
        {
            return new TerminRecord
            {
                Id = record.Id,
                Titel = record.Titel,
                Beschreibung = record.Beschreibung,
                Datum = NormalizeDateOnly(record.Datum),
                StartUhrzeit = NormalizeTerminTime(record.StartUhrzeit),
                EndUhrzeit = NormalizeTerminTime(record.EndUhrzeit),
                SichtbarAb = NormalizeTimestampWithoutTimeZone(record.SichtbarAb),
                SichtbarBis = NormalizeTimestampWithoutTimeZone(record.SichtbarBis),
                Aktiv = record.Aktiv,
                CreatedAt = record.CreatedAt,
                UpdatedAt = record.UpdatedAt
            };
        }

        private static BekanntmachungRecord NormalizeBekanntmachungRecord(BekanntmachungRecord record)
        {
            return new BekanntmachungRecord
            {
                Id = record.Id,
                Titel = record.Titel,
                InhaltHtml = record.InhaltHtml,
                SichtbarAb = NormalizeTimestampWithoutTimeZone(record.SichtbarAb),
                SichtbarBis = NormalizeTimestampWithoutTimeZone(record.SichtbarBis),
                SortOrder = record.SortOrder,
                Aktiv = record.Aktiv,
                CreatedAt = record.CreatedAt,
                UpdatedAt = record.UpdatedAt
            };
        }

        private static bool IsArbeitsstundeOffen(ArbeitsstundeRecord record)
        {
            return !record.Freigegeben;
        }

        private static bool HasActiveArbeitsstundenLock(ArbeitsstundeRecord record, string currentUserId, DateTime now, int timeoutMinutes)
        {
            return !string.IsNullOrWhiteSpace(record.LockedByUserId)
                && !string.Equals(record.LockedByUserId, currentUserId, StringComparison.OrdinalIgnoreCase)
                && record.LockedAt.HasValue
                && record.LockedAt.Value.AddMinutes(timeoutMinutes) > now;
        }

        private async Task<ArbeitsstundenReviewLockResult> BuildArbeitsstundenReviewLockResultAsync(bool acquired, string? lockedByUserId, DateTime? lockedAt)
        {
            var displayName = lockedByUserId;
            if (Guid.TryParse(lockedByUserId, out var authGuid))
            {
                var mitglieder = await GetMitgliederAsync();
                var mitglied = mitglieder.FirstOrDefault(x => x.AuthUserId == authGuid);
                if (mitglied != null)
                    displayName = FormatMemberName(mitglied);
            }

            return new ArbeitsstundenReviewLockResult
            {
                Acquired = acquired,
                LockedByUserId = lockedByUserId,
                LockedByDisplayName = displayName,
                LockedAt = lockedAt
            };
        }

        private static bool IsSameTerminForReload(TerminRecord left, TerminRecord right)
        {
            return string.Equals(CleanRequiredText(left.Titel), CleanRequiredText(right.Titel), StringComparison.CurrentCulture)
                && string.Equals(CleanOptionalText(left.Beschreibung), CleanOptionalText(right.Beschreibung), StringComparison.CurrentCulture)
                && left.Datum.Date == right.Datum.Date
                && NormalizeTerminTime(left.StartUhrzeit) == NormalizeTerminTime(right.StartUhrzeit)
                && NormalizeTerminTime(left.EndUhrzeit) == NormalizeTerminTime(right.EndUhrzeit)
                && left.Aktiv == right.Aktiv
                && left.SichtbarAb == right.SichtbarAb
                && left.SichtbarBis == right.SichtbarBis;
        }

        private static bool IsSameBekanntmachungForReload(BekanntmachungRecord left, BekanntmachungRecord right)
        {
            return string.Equals(CleanRequiredText(left.Titel), CleanRequiredText(right.Titel), StringComparison.CurrentCulture)
                && string.Equals(CleanRequiredText(left.InhaltHtml), CleanRequiredText(right.InhaltHtml), StringComparison.CurrentCulture)
                && left.SichtbarAb == right.SichtbarAb
                && left.SichtbarBis == right.SichtbarBis
                && left.SortOrder == right.SortOrder
                && left.Aktiv == right.Aktiv;
        }

        private static bool IsSameArbeitseinsatzForReload(ArbeitseinsatzRecord left, ArbeitseinsatzRecord right)
        {
            return string.Equals(CleanRequiredText(left.Titel), CleanRequiredText(right.Titel), StringComparison.CurrentCulture)
                && string.Equals(CleanOptionalText(left.Beschreibung), CleanOptionalText(right.Beschreibung), StringComparison.CurrentCulture)
                && left.Datum.Date == right.Datum.Date
                && NormalizeTerminTime(left.StartUhrzeit) == NormalizeTerminTime(right.StartUhrzeit)
                && NormalizeTerminTime(left.EndUhrzeit) == NormalizeTerminTime(right.EndUhrzeit)
                && string.Equals(CleanOptionalText(left.Treffpunkt), CleanOptionalText(right.Treffpunkt), StringComparison.CurrentCulture)
                && left.MaxTeilnehmer == right.MaxTeilnehmer
                && left.StundenWert == right.StundenWert
                && left.SichtbarAb == right.SichtbarAb
                && left.SichtbarBis == right.SichtbarBis
                && left.AnmeldungBis == right.AnmeldungBis
                && left.Aktiv == right.Aktiv
                && left.IsDemo == right.IsDemo;
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
            var records = response?.Models?.ToList() ?? new List<StartseiteArbeitseinsatzRecord>();

            await EnrichStartseiteArbeitseinsatzTimesAsync(client, records);
            await EnrichStartseiteArbeitseinsatzRegistrationStateAsync(client, records);

            return records
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
            var records = response?.Models?.ToList() ?? new List<StartseiteTerminRecord>();

            await EnrichStartseiteTerminTimesAsync(client, records);

            return records
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
                .ThenBy(x => FirstNonEmpty(x.Titel, x.Betreff, x.Thema) ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
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
            var description = NormalizeHomeText(record.Beschreibung);
            var capacityText = BuildCapacityText(record.AngemeldetCount, record.FreiePlaetze);
            var title = FirstNonEmpty(record.Titel, record.Thema) ?? "Arbeitseinsatz";
            var begin = NormalizeTimeValue(record.Beginn);
            var end = NormalizeTimeValue(record.Ende);
            var detailInfoLines = new List<string>();

            AddDetailLine(detailInfoLines, "Thema", record.Thema, value => !string.Equals(value, title, StringComparison.CurrentCultureIgnoreCase));
            AddDetailLine(detailInfoLines, "Datum", record.Datum?.ToString("dd.MM.yyyy"));
            AddDetailLine(detailInfoLines, "Treffpunkt", record.Treffpunkt);

            if (record.AngemeldetCount.HasValue && record.FreiePlaetze.HasValue)
                AddDetailLine(detailInfoLines, "Max. Teilnehmer", (record.AngemeldetCount.Value + record.FreiePlaetze.Value).ToString());

            return new HomeWorkAssignmentItem
            {
                Id = record.Id,
                Title = title,
                Subtitle = record.Datum?.ToString("dd.MM.yyyy") ?? string.Empty,
                StartTimeText = begin ?? string.Empty,
                EndTimeText = end ?? string.Empty,
                Details = description,
                DetailInfo = string.Join(Environment.NewLine, detailInfoLines),
                RegistrationInfo = !string.IsNullOrWhiteSpace(capacityText)
                    ? capacityText
                    : record.AnmeldungMoeglich == true
                        ? "Anmeldung möglich"
                        : string.Empty,
                CanRegister = record.AnmeldungMoeglich == true
            };
        }

        private static HomeAppointmentItem MapHomeAppointment(StartseiteTerminRecord record)
        {
            var title = FirstNonEmpty(record.Titel, record.Thema) ?? "Termin";
            var details = NormalizeHomeText(FirstNonEmpty(record.Inhalt, record.Beschreibung));
            var detailInfoLines = new List<string>();
            var begin = NormalizeTimeValue(record.Beginn);
            var end = NormalizeTimeValue(record.Ende);

            AddDetailLine(detailInfoLines, "Thema", record.Thema, value => !string.Equals(value, title, StringComparison.CurrentCultureIgnoreCase));
            AddDetailLine(detailInfoLines, "Datum", record.Datum?.ToString("dd.MM.yyyy"));
            AddDetailLine(detailInfoLines, "Ort", record.Ort);

            return new HomeAppointmentItem
            {
                Title = title,
                Subtitle = record.Datum?.ToString("dd.MM.yyyy") ?? string.Empty,
                StartTimeText = begin ?? string.Empty,
                EndTimeText = end ?? string.Empty,
                Details = details,
                DetailInfo = string.Join(Environment.NewLine, detailInfoLines)
            };
        }

        private static HomeAnnouncementItem MapHomeAnnouncement(StartseiteBekanntmachungRecord record)
        {
            var published = record.VeroeffentlichtAm ?? record.Datum ?? record.ErstelltAm ?? record.UpdatedAt;
            var title = FirstNonEmpty(record.Titel, record.Betreff, record.Thema) ?? "Bekanntmachung";
            var detailInfoLines = new List<string>();

            AddDetailLine(detailInfoLines, "Betreff", record.Betreff, value => !string.Equals(value, title, StringComparison.CurrentCultureIgnoreCase));
            AddDetailLine(detailInfoLines, "Thema", record.Thema, value => !string.Equals(value, title, StringComparison.CurrentCultureIgnoreCase) && !string.Equals(value, record.Betreff, StringComparison.CurrentCultureIgnoreCase));
            AddDetailLine(detailInfoLines, "Veröffentlicht am", published?.ToString("dd.MM.yyyy HH:mm"));
            AddDetailLine(detailInfoLines, "Kurztext", record.Kurztext);

            return new HomeAnnouncementItem
            {
                Title = title,
                Subtitle = published.HasValue ? published.Value.ToString("dd.MM.yyyy") : string.Empty,
                Content = NormalizeHomeText(FirstNonEmpty(record.Inhalt, record.Text, record.InhaltHtml, record.Beschreibung, record.Kurztext)),
                DetailInfo = string.Join(Environment.NewLine, detailInfoLines)
            };
        }

        private static void AddDetailLine(List<string> lines, string label, string? value, Func<string, bool>? predicate = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            var trimmed = value.Trim();
            if (predicate != null && !predicate(trimmed))
                return;

            lines.Add($"{label}: {trimmed}");
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

        private static WorkAssignmentRegistrationResult CreateRegistrationResult(bool success, string message, HomeWorkAssignmentItem? updatedItem = null)
        {
            return new WorkAssignmentRegistrationResult
            {
                Success = success,
                Message = message,
                UpdatedItem = updatedItem
            };
        }

        private async Task<ArbeitseinsatzRecord?> GetArbeitseinsatzByIdAsync(global::Supabase.Client client, int arbeitseinsatzId)
        {
            var response = await client
                .From<ArbeitseinsatzRecord>()
                .Where(x => x.Id == arbeitseinsatzId)
                .Get();

            return response?.Models?
                .Select(NormalizeArbeitseinsatzRecord)
                .FirstOrDefault();
        }

        private async Task<List<ArbeitseinsatzAnmeldungRecord>> GetAktiveArbeitseinsatzAnmeldungenAsync(global::Supabase.Client client, int arbeitseinsatzId)
        {
            var response = await client
                .From<ArbeitseinsatzAnmeldungRecord>()
                .Where(x => x.ArbeitseinsatzId == arbeitseinsatzId)
                .Where(x => x.Status == "angemeldet")
                .Get();

            return response?.Models?.ToList() ?? new List<ArbeitseinsatzAnmeldungRecord>();
        }

        private async Task<HomeWorkAssignmentItem?> TryLoadHomeWorkAssignmentItemAsync(global::Supabase.Client client, int arbeitseinsatzId)
        {
            var response = await client
                .From<StartseiteArbeitseinsatzRecord>()
                .Where(x => x.Id == arbeitseinsatzId)
                .Get();

            var record = response?.Models?.FirstOrDefault();
            if (record == null)
                return null;

            await EnrichStartseiteArbeitseinsatzTimesAsync(client, new List<StartseiteArbeitseinsatzRecord> { record });
            await EnrichStartseiteArbeitseinsatzRegistrationStateAsync(client, new List<StartseiteArbeitseinsatzRecord> { record });
            return MapHomeWorkAssignment(record);
        }

        private async Task EnrichStartseiteArbeitseinsatzRegistrationStateAsync(global::Supabase.Client client, List<StartseiteArbeitseinsatzRecord> records)
        {
            if (records.Count == 0)
                return;

            var arbeitseinsatzResponse = await client.From<ArbeitseinsatzRecord>().Get();
            var arbeitseinsatzById = arbeitseinsatzResponse?.Models?
                .Where(x => x.Id > 0)
                .Select(NormalizeArbeitseinsatzRecord)
                .ToDictionary(x => (int)x.Id)
                ?? new Dictionary<int, ArbeitseinsatzRecord>();

            var anmeldungenResponse = await client
                .From<ArbeitseinsatzAnmeldungRecord>()
                .Where(x => x.Status == "angemeldet")
                .Get();

            var anmeldungenByArbeitseinsatzId = anmeldungenResponse?.Models?
                .GroupBy(x => x.ArbeitseinsatzId)
                .ToDictionary(x => x.Key, x => x.ToList())
                ?? new Dictionary<int, List<ArbeitseinsatzAnmeldungRecord>>();

            var currentMemberId = TryGetCurrentMitgliedId();
            var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

            foreach (var record in records)
            {
                if (!arbeitseinsatzById.TryGetValue(record.Id, out var arbeitseinsatz))
                    continue;

                anmeldungenByArbeitseinsatzId.TryGetValue(record.Id, out var anmeldungen);
                anmeldungen ??= new List<ArbeitseinsatzAnmeldungRecord>();

                record.AngemeldetCount = anmeldungen.Count;

                if (arbeitseinsatz.MaxTeilnehmer.HasValue)
                    record.FreiePlaetze = Math.Max(0, arbeitseinsatz.MaxTeilnehmer.Value - anmeldungen.Count);

                var isAlreadyRegistered = currentMemberId.HasValue && anmeldungen.Any(x => x.MitgliedId == currentMemberId.Value);
                var isDeadlineOpen = !arbeitseinsatz.AnmeldungBis.HasValue || arbeitseinsatz.AnmeldungBis.Value >= now;
                var hasCapacity = !arbeitseinsatz.MaxTeilnehmer.HasValue || anmeldungen.Count < arbeitseinsatz.MaxTeilnehmer.Value;

                record.AnmeldungMoeglich = currentMemberId.HasValue
                    && arbeitseinsatz.Aktiv
                    && !isAlreadyRegistered
                    && isDeadlineOpen
                    && hasCapacity;
            }
        }

        private int? TryGetCurrentMitgliedId()
        {
            var userContext = _currentUserContextAccessor?.Invoke();
            return userContext?.MitgliedId is > 0 and <= int.MaxValue
                ? (int)userContext.MitgliedId.Value
                : null;
        }

        private static async Task EnrichStartseiteArbeitseinsatzTimesAsync(global::Supabase.Client client, List<StartseiteArbeitseinsatzRecord> records)
        {
            if (records.Count == 0 || records.All(HasStartseiteTimeValues))
                return;

            var response = await client.From<ArbeitseinsatzRecord>().Get();
            var lookup = response?.Models?
                .Where(x => x.Id > 0)
                .ToDictionary(x => (int)x.Id)
                ?? new Dictionary<int, ArbeitseinsatzRecord>();

            foreach (var record in records)
            {
                if (HasStartseiteTimeValues(record) || !lookup.TryGetValue(record.Id, out var source))
                    continue;

                record.Beginn ??= FormatTimeValue(source.StartUhrzeit);
                record.Ende ??= FormatTimeValue(source.EndUhrzeit);
            }
        }

        private static async Task EnrichStartseiteTerminTimesAsync(global::Supabase.Client client, List<StartseiteTerminRecord> records)
        {
            if (records.Count == 0 || records.All(HasStartseiteTimeValues))
                return;

            var response = await client.From<TerminRecord>().Get();
            var lookup = response?.Models?
                .Where(x => x.Id > 0)
                .ToDictionary(x => (int)x.Id)
                ?? new Dictionary<int, TerminRecord>();

            foreach (var record in records)
            {
                if (HasStartseiteTimeValues(record) || !lookup.TryGetValue(record.Id, out var source))
                    continue;

                record.Beginn ??= FormatTimeValue(source.StartUhrzeit);
                record.Ende ??= FormatTimeValue(source.EndUhrzeit);
            }
        }

        private static bool HasStartseiteTimeValues(StartseiteArbeitseinsatzRecord record)
        {
            return !string.IsNullOrWhiteSpace(record.Beginn) || !string.IsNullOrWhiteSpace(record.Ende);
        }

        private static bool HasStartseiteTimeValues(StartseiteTerminRecord record)
        {
            return !string.IsNullOrWhiteSpace(record.Beginn) || !string.IsNullOrWhiteSpace(record.Ende);
        }

        private static string? FormatTimeValue(TimeSpan? value)
        {
            return value?.ToString(@"hh\:mm");
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
            var bucket = (record.Bucket ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(bucket))
                return path;
            if (string.IsNullOrWhiteSpace(path))
                return bucket;

            return $"{bucket}:{path}";
        }

        private static bool TryParseStorageReference(string? storageReference, out string bucket, out string path)
        {
            bucket = string.Empty;
            path = string.Empty;

            if (string.IsNullOrWhiteSpace(storageReference))
                return false;

            var normalized = storageReference.Trim().Replace('\\', '/');
            var separatorIndex = normalized.IndexOf(':');
            if (separatorIndex <= 0 || separatorIndex >= normalized.Length - 1)
                return false;

            bucket = normalized[..separatorIndex].Trim();
            path = normalized[(separatorIndex + 1)..].Trim().TrimStart('/');
            return !string.IsNullOrWhiteSpace(bucket) && !string.IsNullOrWhiteSpace(path);
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

        private async Task<ParzelleRecord?> GetParzelleByIdInternalAsync(Client client, int parzelleId)
        {
            var response = await client
                .From<ParzelleRecord>()
                .Where(x => x.Id == parzelleId)
                .Get();

            return response?.Models?.FirstOrDefault();
        }

        private async Task<RfidAssignmentCheckResult> CheckParzelleRfidAssignmentInternalAsync(Client client, int parzelleId, string medium, string uid)
        {
            var normalizedMedium = NormalizeRfidMedium(medium);
            if (parzelleId <= 0)
            {
                return new RfidAssignmentCheckResult
                {
                    IsValid = false,
                    Message = "Bitte zuerst eine Parzelle auswählen."
                };
            }

            if (normalizedMedium == null)
            {
                return new RfidAssignmentCheckResult
                {
                    IsValid = false,
                    Message = "Bitte ein gültiges Medium auswählen."
                };
            }

            var normalizedUid = NormalizeRfidTagUid(uid);
            if (normalizedUid == null)
            {
                return new RfidAssignmentCheckResult
                {
                    IsValid = false,
                    Message = "Bitte eine RFID-UID eingeben.",
                    NormalizedUid = string.Empty
                };
            }

            var parzelle = await GetParzelleByIdInternalAsync(client, parzelleId);
            if (parzelle == null)
            {
                return new RfidAssignmentCheckResult
                {
                    IsValid = false,
                    Message = "Die gewählte Parzelle konnte nicht geladen werden.",
                    NormalizedUid = normalizedUid
                };
            }

            if (normalizedMedium == "strom" && !parzelle.HatStrom)
            {
                return new RfidAssignmentCheckResult
                {
                    IsValid = false,
                    Message = "Für diese Parzelle ist kein Stromanschluss hinterlegt.",
                    NormalizedUid = normalizedUid
                };
            }

            if (normalizedMedium == "wasser" && !parzelle.HatWasser)
            {
                return new RfidAssignmentCheckResult
                {
                    IsValid = false,
                    Message = "Für diese Parzelle ist kein Wasseranschluss hinterlegt.",
                    NormalizedUid = normalizedUid
                };
            }

            var currentTargetRfid = normalizedMedium == "strom"
                ? NormalizeRfidTagUid(parzelle.RfidStrom)
                : NormalizeRfidTagUid(parzelle.RfidWasser);

            var scanContextResponse = await client.From<RfidScanContextRecord>().Get();
            var scanContexts = scanContextResponse?.Models ?? new List<RfidScanContextRecord>();

            var conflictingAssignment = scanContexts
                .FirstOrDefault(x => NormalizeRfidTagUid(x.RfidTagUid) == normalizedUid
                    && (x.ParzelleId != parzelleId || !string.Equals(NormalizeRfidMedium(x.Medium), normalizedMedium, StringComparison.OrdinalIgnoreCase)));

            if (conflictingAssignment != null)
            {
                return new RfidAssignmentCheckResult
                {
                    IsValid = false,
                    Message = $"Die UID {normalizedUid} ist bereits bei {conflictingAssignment.ParzelleDisplayName} für {MediumDisplayName(conflictingAssignment.Medium)} hinterlegt.",
                    NormalizedUid = normalizedUid,
                    ConflictParzelleId = conflictingAssignment.ParzelleId,
                    ConflictGartenNr = conflictingAssignment.GartenNr ?? string.Empty,
                    ConflictAnlage = conflictingAssignment.Anlage ?? string.Empty,
                    ConflictMedium = NormalizeRfidMedium(conflictingAssignment.Medium) ?? string.Empty
                };
            }

            if (string.Equals(currentTargetRfid, normalizedUid, StringComparison.OrdinalIgnoreCase))
            {
                return new RfidAssignmentCheckResult
                {
                    IsValid = true,
                    AlreadyAssignedToTarget = true,
                    Message = $"Die UID {normalizedUid} ist für {MediumDisplayName(normalizedMedium)} bei {parzelle.DisplayName} bereits hinterlegt.",
                    NormalizedUid = normalizedUid,
                    CurrentTargetRfid = currentTargetRfid ?? string.Empty
                };
            }

            if (!string.IsNullOrWhiteSpace(currentTargetRfid))
            {
                return new RfidAssignmentCheckResult
                {
                    IsValid = true,
                    RequiresOverwriteConfirmation = true,
                    Message = $"Für {MediumDisplayName(normalizedMedium)} ist bei {parzelle.DisplayName} bereits die RFID {currentTargetRfid} hinterlegt. Bitte das Überschreiben ausdrücklich bestätigen.",
                    NormalizedUid = normalizedUid,
                    CurrentTargetRfid = currentTargetRfid ?? string.Empty
                };
            }

            return new RfidAssignmentCheckResult
            {
                IsValid = true,
                Message = $"Prüfung erfolgreich. Die UID {normalizedUid} kann für {MediumDisplayName(normalizedMedium)} bei {parzelle.DisplayName} gespeichert werden.",
                NormalizedUid = normalizedUid,
                CurrentTargetRfid = string.Empty
            };
        }

        private static string? NormalizeRfidTagUid(string? uid)
        {
            if (string.IsNullOrWhiteSpace(uid))
                return null;

            return uid.Trim().ToUpperInvariant();
        }

        private static string? NormalizeRfidMedium(string? medium)
        {
            if (string.IsNullOrWhiteSpace(medium))
                return null;

            var normalized = medium.Trim().ToLowerInvariant();
            return normalized is "strom" or "wasser" ? normalized : null;
        }

        private static string MediumDisplayName(string? medium)
        {
            return NormalizeRfidMedium(medium) == "wasser" ? "Wasser" : "Strom";
        }

        private InvalidOperationException CreateUnavailableException()
        {
            return new InvalidOperationException("SupabaseService ist aktuell nicht initialisiert.");
        }
    }
}
