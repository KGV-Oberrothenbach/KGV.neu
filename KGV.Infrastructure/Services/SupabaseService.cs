using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Core.Utilities;
using Microsoft.Extensions.Logging;
using Supabase;
using Supabase.Postgrest.Exceptions;

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

        public Task<ImpressumInfo> GetImpressumInfoAsync() => ExecuteAsync(
            "GetImpressumInfoAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var slotsResponse = await client.From<ImpressumFunktionSlotRecord>().Get();
                var slots = slotsResponse?.Models?
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.SlotKey ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                    .ToList()
                    ?? new List<ImpressumFunktionSlotRecord>();

                if (slots.Count == 0)
                    return new ImpressumInfo();

                var members = await GetMitgliederAsync();
                var membersById = members
                    .Where(x => x.Id > 0)
                    .ToDictionary(x => (long)x.Id, x => x);

                var info = new ImpressumInfo();
                foreach (var slot in slots)
                {
                    var item = CreateImpressumKontaktItem(slot, membersById);
                    if (IsBauausschussSlot(slot))
                        info.Bauausschuss.Add(item);
                    else
                        info.Vorstand.Add(item);
                }

                return info;
            },
            new ImpressumInfo());

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
                    .Set(x => x.Email, existing.AuthUserId.HasValue ? existing.Email : CleanOptionalText(dto.Email))
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
                    FlaecheQm = parzelle.FlaecheQm,
                    HatStrom = parzelle.HatStrom,
                    HatWasser = parzelle.HatWasser,
                    RfidStrom = parzelle.RfidStrom,
                    RfidWasser = parzelle.RfidWasser,
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

        public Task<bool> UpdateParzelleStammdatenAsync(ParzelleRecord record) => ExecuteAsync(
            "UpdateParzelleStammdatenAsync",
            async () =>
            {
                if (record == null || record.Id <= 0 || string.IsNullOrWhiteSpace(record.GartenNr))
                    return false;

                var client = await EnsureClientAsync();
                await client
                    .From<ParzelleRecord>()
                    .Where(x => x.Id == record.Id)
                    .Set(x => x.GartenNr, record.GartenNr.Trim())
                    .Set(x => x.Anlage, record.Anlage?.Trim() ?? string.Empty)
                    .Set(x => x.FlaecheQm, record.FlaecheQm)
                    .Set(x => x.HatStrom, record.HatStrom)
                    .Set(x => x.HatWasser, record.HatWasser)
                    .Set(x => x.RfidStrom, NormalizeRfidTagUid(record.RfidStrom))
                    .Set(x => x.RfidWasser, NormalizeRfidTagUid(record.RfidWasser))
                    .Update();

                return true;
            },
            false);

        public Task<List<RfidMediumOption>> GetAvailableRfidMediumOptionsForParzelleAsync(int parzelleId) => ExecuteAsync(
            "GetAvailableRfidMediumOptionsForParzelleAsync",
            async () =>
            {
                if (parzelleId <= 0)
                    return new List<RfidMediumOption>();

                var client = await EnsureClientAsync();
                var parzelle = await GetParzelleByIdInternalAsync(client, parzelleId);
                if (parzelle == null)
                    return new List<RfidMediumOption>();

                return await GetAvailableRfidMediumOptionsInternalAsync(parzelle);
            },
            new List<RfidMediumOption>());

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

        public Task<bool> AddStromzaehlerAsync(StromzaehlerInsertRecord request) => ExecuteAsync(
            "AddStromzaehlerAsync",
            async () =>
            {
                if (request == null || request.ParzelleId <= 0 || string.IsNullOrWhiteSpace(request.Zaehlernummer))
                    return false;

                var client = await EnsureClientAsync();
                await client.From<StromzaehlerInsertRecord>().Insert(new StromzaehlerInsertRecord
                {
                    ParzelleId = request.ParzelleId,
                    Zaehlernummer = request.Zaehlernummer.Trim(),
                    Eichdatum = NormalizeMeterEichjahr(request.Eichdatum),
                    EingebautAm = NormalizeDateTime(request.EingebautAm.Date)
                });

                return true;
            },
            false);

        public Task<bool> AddWasserzaehlerAsync(WasserzaehlerInsertRecord request) => ExecuteAsync(
            "AddWasserzaehlerAsync",
            async () =>
            {
                if (request == null || request.ParzelleId <= 0 || string.IsNullOrWhiteSpace(request.Zaehlernummer))
                    return false;

                var client = await EnsureClientAsync();
                await client.From<WasserzaehlerInsertRecord>().Insert(new WasserzaehlerInsertRecord
                {
                    ParzelleId = request.ParzelleId,
                    Zaehlernummer = request.Zaehlernummer.Trim(),
                    Eichdatum = NormalizeMeterEichjahr(request.Eichdatum),
                    EingebautAm = NormalizeDateTime(request.EingebautAm.Date)
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

        public Task<bool> AddAblesungAsync(AblesungInsertRecord request) => ExecuteAsync(
            "AddAblesungAsync",
            async () =>
            {
                if (request == null || request.ZaehlerId <= 0)
                    return false;

                var client = await EnsureClientAsync();
                await client.From<AblesungInsertRecord>().Insert(new AblesungInsertRecord
                {
                    ZaehlerTyp = request.ZaehlerTyp,
                    ZaehlerId = request.ZaehlerId,
                    Ablesedatum = NormalizeDateTime(request.Ablesedatum),
                    Stand = request.Stand,
                    Art = AblesungArt.Normalize(request.Art),
                    Freigegeben = request.Freigegeben,
                    FotoPfad = CleanOptionalText(request.FotoPfad)
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
        public Task<MitgliedRecord?> CreateNebenmitgliedAsync(NebenmitgliedCreateDTO request) => ExecuteAsync<MitgliedRecord?>(
            "CreateNebenmitgliedAsync",
            async () =>
            {
                if (request == null || request.HauptmitgliedId <= 0 || string.IsNullOrWhiteSpace(request.Vorname) || string.IsNullOrWhiteSpace(request.Nachname))
                    return null;

                var client = await EnsureClientAsync();
                var hauptmitglied = await GetMitgliedByIdAsync(request.HauptmitgliedId);
                if (hauptmitglied == null)
                    return null;

                var insertRecord = CreateNebenmitgliedInsertPayload(request, hauptmitglied);
                await client.From<MitgliedInsertRecord>().Insert(insertRecord);

                var response = await client
                    .From<MitgliedRecord>()
                    .Where(x => x.HauptmitgliedId == request.HauptmitgliedId)
                    .Get();

                var created = response?.Models?
                    .Where(x => x.HauptmitgliedId == request.HauptmitgliedId)
                    .Where(x => string.Equals(CleanRequiredText(x.Vorname), insertRecord.Vorname, StringComparison.CurrentCulture))
                    .Where(x => string.Equals(CleanRequiredText(x.Name), insertRecord.Name, StringComparison.CurrentCulture))
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefault();

                _logger?.LogInformation("CreateNebenmitgliedAsync created nebenmitglied {MitgliedId} for hauptmitglied {HauptmitgliedId}", created?.Id, request.HauptmitgliedId);
                return created;
            },
            null);
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
        public Task<bool> AddArbeitsstundeAsync(ArbeitsstundeInsertRecord request) => ExecuteAsync(
            "AddArbeitsstundeAsync",
            async () =>
            {
                if (request == null || request.MitgliedId <= 0 || request.SaisonId <= 0 || request.Stunden <= 0 || string.IsNullOrWhiteSpace(request.ArtDerArbeit))
                    return false;

                var client = await EnsureClientAsync();
                await client.From<ArbeitsstundeInsertRecord>().Insert(CreateArbeitsstundeInsertPayload(request));

                return true;
            },
            false);

        private ArbeitsstundeInsertRecord CreateArbeitsstundeInsertPayload(ArbeitsstundeInsertRecord request)
        {
            return new ArbeitsstundeInsertRecord
            {
                MitgliedId = request.MitgliedId,
                SaisonId = request.SaisonId,
                Datum = NormalizeDateOnly(request.Datum),
                Stunden = request.Stunden,
                ArtDerArbeit = request.ArtDerArbeit.Trim(),
                Status = CleanOptionalText(request.Status),
                Freigegeben = request.Freigegeben,
                GenehmigtAm = request.GenehmigtAm,
                GenehmigtVon = request.GenehmigtVon,
                LockedByUserId = null,
                LockedAt = null
            };
        }

        private MitgliedInsertRecord CreateNebenmitgliedInsertPayload(NebenmitgliedCreateDTO request, MitgliedRecord hauptmitglied)
        {
            return new MitgliedInsertRecord
            {
                HauptmitgliedId = request.HauptmitgliedId,
                Name = CleanRequiredText(request.Nachname),
                Vorname = CleanRequiredText(request.Vorname),
                Adresse = request.AdresseUebernehmen ? CleanOptionalText(hauptmitglied.Adresse) : null,
                Plz = request.AdresseUebernehmen ? CleanOptionalText(hauptmitglied.Plz) : null,
                Ort = request.AdresseUebernehmen ? CleanOptionalText(hauptmitglied.Ort) : null,
                Telefon = null,
                Handy = null,
                Email = null
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

        public Task<PflichtstundenUebersichtRecord?> GetPflichtstundenUebersichtForMitgliedAsync(int mitgliedId) => ExecuteAsync<PflichtstundenUebersichtRecord?>(
            "GetPflichtstundenUebersichtForMitgliedAsync",
            async () =>
            {
                if (mitgliedId <= 0)
                    return null;

                var client = await EnsureClientAsync();
                var response = await client.From<PflichtstundenUebersichtRecord>().Get();
                var homeMitgliedId = await ResolveHomeMitgliedIdAsync(mitgliedId);

                return response?.Models?
                    .Where(x => MatchesPflichtstundenMitglied(x, mitgliedId, homeMitgliedId))
                    .OrderByDescending(x => x.MitgliedId == mitgliedId)
                    .ThenByDescending(GetPflichtstundenYear)
                    .ThenByDescending(x => x.SaisonId ?? 0)
                    .FirstOrDefault();
            },
            null);

        public Task<WartungsvertragRecord?> GetWartungsvertragByIdAsync(long wartungsvertragId) => ExecuteAsync<WartungsvertragRecord?>(
            "GetWartungsvertragByIdAsync",
            async () =>
            {
                if (wartungsvertragId <= 0)
                    return null;

                var client = await EnsureClientAsync();
                var contract = await GetWartungsvertragByIdInternalAsync(client, wartungsvertragId);
                return contract != null && !contract.IsDemo ? contract : null;
            },
            null);

        public Task<List<WartungsvertragOverviewItem>> GetWartungsvertraegeOverviewAsync() => ExecuteAsync(
            "GetWartungsvertraegeOverviewAsync",
            async () =>
            {
                var bundle = await LoadWartungsvertragBundleAsync();
                var countsByContractId = bundle.ActiveAssignments
                    .GroupBy(x => x.WartungsvertragId)
                    .ToDictionary(x => x.Key, x => x.Count());

                return bundle.Contracts
                    .OrderByDescending(x => x.Aktiv)
                    .ThenBy(x => x.Titel ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                    .Select(contract => CreateWartungsvertragOverviewItem(contract, countsByContractId))
                    .ToList();
            },
            new List<WartungsvertragOverviewItem>());

        public Task<WartungsvertragDetailItem?> GetWartungsvertragDetailAsync(long wartungsvertragId) => ExecuteAsync<WartungsvertragDetailItem?>(
            "GetWartungsvertragDetailAsync",
            async () =>
            {
                if (wartungsvertragId <= 0)
                    return null;

                var bundle = await LoadWartungsvertragBundleAsync();
                var contract = bundle.Contracts.FirstOrDefault(x => x.Id == wartungsvertragId);
                if (contract == null)
                    return null;

                var countsByContractId = bundle.ActiveAssignments
                    .GroupBy(x => x.WartungsvertragId)
                    .ToDictionary(x => x.Key, x => x.Count());

                var assignedMembers = bundle.ActiveAssignments
                    .Where(x => x.WartungsvertragId == wartungsvertragId)
                    .OrderBy(x => bundle.MembersById.TryGetValue(x.HauptmitgliedId, out var member)
                        ? GetMemberLastNameSortKey(member)
                        : string.Empty,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => bundle.MembersById.TryGetValue(x.HauptmitgliedId, out var member)
                        ? GetMemberFirstNameSortKey(member)
                        : string.Empty,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => x.HauptmitgliedId)
                    .Select(x => CreateWartungsvertragAssignedMemberItem(x, bundle))
                    .ToList();

                var overview = CreateWartungsvertragOverviewItem(contract, countsByContractId);
                return new WartungsvertragDetailItem
                {
                    Id = overview.Id,
                    Titel = overview.Titel,
                    Kurzbeschreibung = overview.Kurzbeschreibung,
                    MaxKontingent = overview.MaxKontingent,
                    Belegt = overview.Belegt,
                    Frei = overview.Frei,
                    Aktiv = overview.Aktiv,
                    Beschreibung = CleanWartungsvertragText(FirstNonEmpty(contract.Beschreibung, contract.Bereich, contract.Bemerkung)),
                    ZugeordneteMitglieder = assignedMembers
                };
            },
            null);

        public Task<List<MemberWartungsvertragItem>> GetWartungsvertraegeForMitgliedAsync(int mitgliedId) => ExecuteAsync(
            "GetWartungsvertraegeForMitgliedAsync",
            async () =>
            {
                if (mitgliedId <= 0)
                    return new List<MemberWartungsvertragItem>();

                var bundle = await LoadWartungsvertragBundleAsync();
                var countsByContractId = bundle.ActiveAssignments
                    .GroupBy(x => x.WartungsvertragId)
                    .ToDictionary(x => x.Key, x => x.Count());
                var contractsById = bundle.Contracts.ToDictionary(x => x.Id);

                return bundle.ActiveAssignments
                    .Where(x => x.HauptmitgliedId == mitgliedId)
                    .OrderBy(x => contractsById.TryGetValue(x.WartungsvertragId, out var contract)
                        ? contract.Titel ?? string.Empty
                        : string.Empty,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => x.GueltigAb)
                    .Select(x => CreateMemberWartungsvertragItem(x, contractsById, countsByContractId))
                    .Where(x => x != null)
                    .Cast<MemberWartungsvertragItem>()
                    .ToList();
            },
            new List<MemberWartungsvertragItem>());

        public Task<List<WartungsvertragOverviewItem>> GetAssignableWartungsvertraegeForMitgliedAsync(int mitgliedId) => ExecuteAsync(
            "GetAssignableWartungsvertraegeForMitgliedAsync",
            async () =>
            {
                if (mitgliedId <= 0)
                    return new List<WartungsvertragOverviewItem>();

                var bundle = await LoadWartungsvertragBundleAsync();
                var countsByContractId = bundle.ActiveAssignments
                    .GroupBy(x => x.WartungsvertragId)
                    .ToDictionary(x => x.Key, x => x.Count());
                var assignedContractIds = bundle.ActiveAssignments
                    .Where(x => x.HauptmitgliedId == mitgliedId)
                    .Select(x => x.WartungsvertragId)
                    .ToHashSet();

                return bundle.Contracts
                    .Where(x => x.Aktiv)
                    .Select(contract => CreateWartungsvertragOverviewItem(contract, countsByContractId))
                    .Where(item => item.Frei > 0)
                    .Where(item => !assignedContractIds.Contains(item.Id))
                    .OrderBy(x => x.Titel, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            },
            new List<WartungsvertragOverviewItem>());

        public async Task<WartungsvertragRecord?> CreateWartungsvertragAsync(WartungsvertragInsertRecord request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Titel))
                    return null;

                var client = await EnsureClientAsync();
                var now = DateTime.UtcNow;
                var insertRecord = new WartungsvertragInsertRecord
                {
                    Titel = CleanRequiredText(request.Titel),
                    Beschreibung = CleanOptionalText(request.Beschreibung),
                    Bereich = CleanOptionalText(request.Bereich),
                    MaxAktiveZuordnungen = NormalizeWartungsvertragKontingent(request.MaxAktiveZuordnungen),
                    BefreitVonPflichtstunden = request.BefreitVonPflichtstunden,
                    Aktiv = request.Aktiv,
                    Bemerkung = CleanOptionalText(request.Bemerkung),
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsDemo = false
                };

                await client.From<WartungsvertragInsertRecord>().Insert(insertRecord);
                var reloadCandidate = new WartungsvertragRecord
                {
                    Titel = insertRecord.Titel,
                    Beschreibung = insertRecord.Beschreibung,
                    Bereich = insertRecord.Bereich,
                    MaxAktiveZuordnungen = insertRecord.MaxAktiveZuordnungen,
                    BefreitVonPflichtstunden = insertRecord.BefreitVonPflichtstunden,
                    Aktiv = insertRecord.Aktiv,
                    Bemerkung = insertRecord.Bemerkung,
                    CreatedAt = insertRecord.CreatedAt,
                    UpdatedAt = insertRecord.UpdatedAt,
                    IsDemo = insertRecord.IsDemo
                };
                var response = await client.From<WartungsvertragRecord>().Get();
                return response?.Models?
                    .Where(x => !x.IsDemo)
                    .Where(x => IsSameWartungsvertragForReload(x, reloadCandidate))
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefault();
            }
            catch (PostgrestException ex)
            {
                LogPostgrestFailure("CreateWartungsvertragAsync", ex);
                throw CreatePostgrestSaveException("Der Wartungsvertrag konnte nicht gespeichert werden.", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "CreateWartungsvertragAsync failed.");
                throw;
            }
        }

        public async Task<bool> UpdateWartungsvertragAsync(WartungsvertragRecord record)
        {
            try
            {
                if (record == null || record.Id <= 0 || string.IsNullOrWhiteSpace(record.Titel))
                    return false;

                var client = await EnsureClientAsync();
                await client
                    .From<WartungsvertragRecord>()
                    .Where(x => x.Id == record.Id)
                    .Set(x => x.Titel, CleanRequiredText(record.Titel))
                    .Set(x => x.Beschreibung, CleanOptionalText(record.Beschreibung))
                    .Set(x => x.MaxAktiveZuordnungen, NormalizeWartungsvertragKontingent(record.MaxAktiveZuordnungen))
                    .Set(x => x.Aktiv, record.Aktiv)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow)
                    .Update();

                return true;
            }
            catch (PostgrestException ex)
            {
                LogPostgrestFailure("UpdateWartungsvertragAsync", ex);
                throw CreatePostgrestSaveException("Der Wartungsvertrag konnte nicht gespeichert werden.", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "UpdateWartungsvertragAsync failed.");
                throw;
            }
        }

        public async Task<WartungsvertragAssignmentSaveResult> AssignMitgliederToWartungsvertragAsync(long wartungsvertragId, DateTime gueltigAb, IReadOnlyCollection<int> mitgliedIds)
        {
            var requestedIds = (mitgliedIds ?? Array.Empty<int>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            try
            {
                if (wartungsvertragId <= 0)
                    return CreateWartungsvertragAssignmentSaveResult(false, "Der Wartungsvertrag fehlt.", 0, 0, 0);

                if (requestedIds.Count == 0)
                    return CreateWartungsvertragAssignmentSaveResult(false, "Bitte mindestens ein Mitglied auswählen.", 0, 0, 0);

                var client = await EnsureClientAsync();
                var contract = await GetWartungsvertragByIdInternalAsync(client, wartungsvertragId);
                if (contract == null || contract.IsDemo)
                    return CreateWartungsvertragAssignmentSaveResult(false, "Der ausgewählte Wartungsvertrag konnte nicht geladen werden.", requestedIds.Count, 0, 0);

                var bundle = await LoadWartungsvertragBundleAsync();
                var activeAssignments = bundle.ActiveAssignments
                    .Where(x => x.WartungsvertragId == wartungsvertragId)
                    .ToList();
                var activeMemberIds = activeAssignments
                    .Select(x => x.HauptmitgliedId)
                    .ToHashSet();

                var normalizedMemberIds = new List<long>();
                foreach (var requestedId in requestedIds)
                {
                    if (bundle.MembersById.ContainsKey(requestedId)
                        && !normalizedMemberIds.Contains(requestedId))
                    {
                        normalizedMemberIds.Add(requestedId);
                    }
                }

                var newMemberIds = normalizedMemberIds
                    .Where(x => !activeMemberIds.Contains(x))
                    .ToList();
                var maxKontingent = NormalizeWartungsvertragKontingent(contract.MaxAktiveZuordnungen);
                var freiePlaetze = Math.Max(0, maxKontingent - activeAssignments.Count);

                if (newMemberIds.Count == 0)
                    return CreateWartungsvertragAssignmentSaveResult(false, "Die ausgewählten Mitglieder sind bereits aktiv zugeordnet oder derzeit nicht zuweisbar.", requestedIds.Count, 0, freiePlaetze);

                if (newMemberIds.Count > freiePlaetze)
                {
                    return CreateWartungsvertragAssignmentSaveResult(
                        false,
                        freiePlaetze <= 0
                            ? "Für diesen Wartungsvertrag sind aktuell keine freien Plätze mehr verfügbar."
                            : $"Es sind nur noch {freiePlaetze} freie Plätze verfügbar.",
                        requestedIds.Count,
                        0,
                        freiePlaetze);
                }

                var normalizedStartDate = gueltigAb.Date;
                var now = DateTime.UtcNow;
                var insertRecords = newMemberIds
                    .Select(x => new WartungsvertragZuordnungInsertRecord
                    {
                        WartungsvertragId = wartungsvertragId,
                        HauptmitgliedId = x,
                        GueltigAb = normalizedStartDate,
                        CreatedAt = now,
                        UpdatedAt = now
                    })
                    .ToList();

                await client.From<WartungsvertragZuordnungInsertRecord>().Insert(insertRecords);

                var remainingFreeSlots = Math.Max(0, freiePlaetze - newMemberIds.Count);
                return CreateWartungsvertragAssignmentSaveResult(
                    true,
                    newMemberIds.Count == 1
                        ? "1 Zuordnung wurde gespeichert."
                        : $"{newMemberIds.Count} Zuordnungen wurden gespeichert.",
                    requestedIds.Count,
                    newMemberIds.Count,
                    remainingFreeSlots);
            }
            catch (PostgrestException ex)
            {
                LogPostgrestFailure("AssignMitgliederToWartungsvertragAsync", ex);
                return CreateWartungsvertragAssignmentSaveResult(
                    false,
                    BuildPostgrestUserMessage("Die Zuordnungen konnten nicht gespeichert werden.", ex),
                    requestedIds.Count,
                    0,
                    0);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AssignMitgliederToWartungsvertragAsync failed.");
                return CreateWartungsvertragAssignmentSaveResult(false, "Die Zuordnungen konnten aktuell nicht gespeichert werden.", requestedIds.Count, 0, 0);
            }
        }

        public async Task<WartungsvertragAssignmentSaveResult> AssignWartungsvertraegeToMitgliedAsync(int mitgliedId, DateTime gueltigAb, IReadOnlyCollection<long> wartungsvertragIds)
        {
            var requestedContractIds = (wartungsvertragIds ?? Array.Empty<long>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            try
            {
                if (mitgliedId <= 0)
                    return CreateWartungsvertragAssignmentSaveResult(false, "Das ausgewählte Mitglied fehlt.", 0, 0, 0);

                if (requestedContractIds.Count == 0)
                    return CreateWartungsvertragAssignmentSaveResult(false, "Bitte mindestens einen Wartungsvertrag auswählen.", 0, 0, 0);

                var bundle = await LoadWartungsvertragBundleAsync();
                if (!bundle.MembersById.ContainsKey(mitgliedId))
                    return CreateWartungsvertragAssignmentSaveResult(false, "Das ausgewählte Mitglied konnte nicht belastbar aufgelöst werden.", requestedContractIds.Count, 0, 0);

                var countsByContractId = bundle.ActiveAssignments
                    .GroupBy(x => x.WartungsvertragId)
                    .ToDictionary(x => x.Key, x => x.Count());
                var activeContractIds = bundle.ActiveAssignments
                    .Where(x => x.HauptmitgliedId == mitgliedId)
                    .Select(x => x.WartungsvertragId)
                    .ToHashSet();
                var normalizedStartDate = gueltigAb.Date;
                var now = DateTime.UtcNow;
                var insertRecords = new List<WartungsvertragZuordnungInsertRecord>();

                foreach (var contractId in requestedContractIds)
                {
                    var contract = bundle.Contracts.FirstOrDefault(x => x.Id == contractId);
                    if (contract == null || contract.IsDemo || !contract.Aktiv)
                        continue;

                    if (activeContractIds.Contains(contractId))
                        continue;

                    var activeCount = countsByContractId.TryGetValue(contractId, out var count) ? count : 0;
                    var freiePlaetze = Math.Max(0, NormalizeWartungsvertragKontingent(contract.MaxAktiveZuordnungen) - activeCount);
                    if (freiePlaetze <= 0)
                        continue;

                    insertRecords.Add(new WartungsvertragZuordnungInsertRecord
                    {
                        WartungsvertragId = contractId,
                        HauptmitgliedId = mitgliedId,
                        GueltigAb = normalizedStartDate,
                        CreatedAt = now,
                        UpdatedAt = now
                    });

                    countsByContractId[contractId] = activeCount + 1;
                }

                if (insertRecords.Count == 0)
                {
                    return CreateWartungsvertragAssignmentSaveResult(
                        false,
                        "Die ausgewählten Wartungsverträge sind bereits aktiv zugeordnet oder aktuell nicht mehr frei.",
                        requestedContractIds.Count,
                        0,
                        0);
                }

                var client = await EnsureClientAsync();
                await client.From<WartungsvertragZuordnungInsertRecord>().Insert(insertRecords);

                var skippedCount = requestedContractIds.Count - insertRecords.Count;
                var message = skippedCount <= 0
                    ? insertRecords.Count == 1
                        ? "1 Zuordnung wurde gespeichert."
                        : $"{insertRecords.Count} Zuordnungen wurden gespeichert."
                    : $"{insertRecords.Count} Zuordnung(en) wurden gespeichert, {skippedCount} konnten nicht übernommen werden.";

                return CreateWartungsvertragAssignmentSaveResult(
                    true,
                    message,
                    requestedContractIds.Count,
                    insertRecords.Count,
                    0);
            }
            catch (PostgrestException ex)
            {
                LogPostgrestFailure("AssignWartungsvertraegeToMitgliedAsync", ex);
                return CreateWartungsvertragAssignmentSaveResult(
                    false,
                    BuildPostgrestUserMessage("Die Zuordnungen konnten nicht gespeichert werden.", ex),
                    requestedContractIds.Count,
                    0,
                    0);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AssignWartungsvertraegeToMitgliedAsync failed.");
                return CreateWartungsvertragAssignmentSaveResult(false, "Die Zuordnungen konnten aktuell nicht gespeichert werden.", requestedContractIds.Count, 0, 0);
            }
        }

        public Task<bool> EndWartungsvertragZuordnungAsync(long wartungsvertragZuordnungId, DateTime gueltigBis) => ExecuteAsync(
            "EndWartungsvertragZuordnungAsync",
            async () =>
            {
                if (wartungsvertragZuordnungId <= 0)
                    return false;

                var client = await EnsureClientAsync();
                var response = await client
                    .From<WartungsvertragZuordnungRecord>()
                    .Where(x => x.Id == wartungsvertragZuordnungId)
                    .Get();

                var assignment = response?.Models?.FirstOrDefault();
                if (assignment == null)
                    return false;

                var effectiveEndDate = gueltigBis.Date.AddDays(-1);
                if (effectiveEndDate < assignment.GueltigAb.Date)
                    effectiveEndDate = assignment.GueltigAb.Date;

                await client
                    .From<WartungsvertragZuordnungRecord>()
                    .Where(x => x.Id == wartungsvertragZuordnungId)
                    .Set(x => x.GueltigBis, effectiveEndDate)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow)
                    .Update();

                return true;
            },
            false);

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

                Task<(HomeWorkHoursSummary? Result, bool Success)> summaryTask = Task.FromResult(((HomeWorkHoursSummary?)null, true));

                if (mitgliedId is > 0)
                {
                    var homeMitgliedId = await ResolveHomeMitgliedIdAsync(mitgliedId.Value);
                    summaryTask = TryLoadHomeSectionAsync(
                        "LoadPflichtstundenSummaryAsync",
                        () => LoadPflichtstundenSummaryAsync(homeMitgliedId, DateTime.Today.Year),
                        (HomeWorkHoursSummary?)null);
                }

                var workAssignmentsTask = TryLoadHomeSectionAsync(
                    "LoadStartseiteArbeitseinsaetzeAsync",
                    LoadStartseiteArbeitseinsaetzeAsync,
                    new List<HomeWorkAssignmentItem>());

                var appointmentsTask = TryLoadHomeSectionAsync(
                    "LoadStartseiteTermineAsync",
                    LoadStartseiteTermineAsync,
                    new List<HomeAppointmentItem>());

                var announcementsTask = TryLoadHomeSectionAsync(
                    "LoadStartseiteBekanntmachungenAsync",
                    LoadStartseiteBekanntmachungenAsync,
                    new List<HomeAnnouncementItem>());

                await Task.WhenAll(summaryTask, workAssignmentsTask, appointmentsTask, announcementsTask);

                var (loadedSummary, summaryLoaded) = await summaryTask;
                workHoursSummary = loadedSummary;
                if (workHoursSummary != null)
                    operationalItems.Add(BuildWorkHoursItem(workHoursSummary));
                else if (mitgliedId is > 0 && !summaryLoaded)
                    workHoursSummary = new HomeWorkHoursSummary { Year = DateTime.Today.Year, RuleReason = "Pflichtstunden konnten aktuell nicht geladen werden. Details stehen im Debug-/Anwendungslog." };

                var (workAssignments, workAssignmentsLoaded) = await workAssignmentsTask;
                var (appointments, appointmentsLoaded) = await appointmentsTask;
                var (announcements, announcementsLoaded) = await announcementsTask;

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

        public Task<ArbeitseinsatzRecord?> CreateArbeitseinsatzAsync(ArbeitseinsatzInsertRecord request) => ExecuteAsync<ArbeitseinsatzRecord?>(
            "CreateArbeitseinsatzAsync",
            async () =>
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Titel))
                    return null;

                var client = await EnsureClientAsync();
                var now = DateTime.UtcNow;
                var insertRecord = new ArbeitseinsatzInsertRecord
                {
                    Titel = CleanRequiredText(request.Titel),
                    Beschreibung = CleanOptionalText(request.Beschreibung),
                    Datum = NormalizeDateOnly(request.Datum),
                    StartUhrzeit = NormalizeTerminTime(request.StartUhrzeit),
                    EndUhrzeit = NormalizeTerminTime(request.EndUhrzeit),
                    Treffpunkt = CleanOptionalText(request.Treffpunkt),
                    MaxTeilnehmer = request.MaxTeilnehmer,
                    StundenWert = request.StundenWert < 0 ? 0 : request.StundenWert,
                    SichtbarAb = NormalizeTimestampWithoutTimeZone(request.SichtbarAb),
                    SichtbarBis = NormalizeTimestampWithoutTimeZone(request.SichtbarBis),
                    AnmeldungBis = NormalizeTimestampWithoutTimeZone(request.AnmeldungBis),
                    Aktiv = request.Aktiv,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsDemo = request.IsDemo
                };

                await client.From<ArbeitseinsatzInsertRecord>().Insert(insertRecord);
                var reloadCandidate = new ArbeitseinsatzRecord
                {
                    Titel = insertRecord.Titel,
                    Beschreibung = insertRecord.Beschreibung,
                    Datum = insertRecord.Datum,
                    StartUhrzeit = insertRecord.StartUhrzeit,
                    EndUhrzeit = insertRecord.EndUhrzeit,
                    Treffpunkt = insertRecord.Treffpunkt,
                    MaxTeilnehmer = insertRecord.MaxTeilnehmer,
                    StundenWert = insertRecord.StundenWert,
                    SichtbarAb = insertRecord.SichtbarAb,
                    SichtbarBis = insertRecord.SichtbarBis,
                    AnmeldungBis = insertRecord.AnmeldungBis,
                    Aktiv = insertRecord.Aktiv,
                    CreatedAt = insertRecord.CreatedAt,
                    UpdatedAt = insertRecord.UpdatedAt,
                    IsDemo = insertRecord.IsDemo
                };
                var reloadResponse = await client
                    .From<ArbeitseinsatzRecord>()
                    .Where(x => x.Titel == insertRecord.Titel)
                    .Where(x => x.Datum == insertRecord.Datum)
                    .Get();
                var created = reloadResponse?.Models?
                    .Select(NormalizeArbeitseinsatzRecord)
                    .Where(x => IsSameArbeitseinsatzForReload(x, reloadCandidate))
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
                    .Set(x => x.UpdatedAt, DateTime.UtcNow)
                    .Set(x => x.IsDemo, record.IsDemo)
                    .Update();

                _logger?.LogInformation("UpdateArbeitseinsatzAsync updated arbeitseinsatz {ArbeitseinsatzId}", record.Id);
                return true;
            },
            false);

        public Task<bool> DeleteArbeitseinsatzAsync(long id) => ExecuteAsync(
            "DeleteArbeitseinsatzAsync",
            async () =>
            {
                if (id <= 0)
                    return false;

                var client = await EnsureClientAsync();
                await client
                    .From<ArbeitseinsatzRecord>()
                    .Where(x => x.Id == id)
                    .Delete();

                _logger?.LogInformation("DeleteArbeitseinsatzAsync deleted arbeitseinsatz {ArbeitseinsatzId}", id);
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

        public Task<TerminRecord?> CreateTerminAsync(TerminInsertRecord request) => ExecuteAsync<TerminRecord?>(
            "CreateTerminAsync",
            async () =>
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Titel))
                    return null;

                var client = await EnsureClientAsync();
                var now = DateTime.UtcNow;
                var insertRecord = new TerminInsertRecord
                {
                    Titel = CleanRequiredText(request.Titel),
                    Beschreibung = CleanOptionalText(request.Beschreibung),
                    Datum = NormalizeDateOnly(request.Datum),
                    StartUhrzeit = NormalizeTerminTime(request.StartUhrzeit),
                    EndUhrzeit = NormalizeTerminTime(request.EndUhrzeit),
                    SichtbarAb = NormalizeTimestampWithoutTimeZone(request.SichtbarAb),
                    SichtbarBis = NormalizeTimestampWithoutTimeZone(request.SichtbarBis),
                    Aktiv = request.Aktiv,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await client.From<TerminInsertRecord>().Insert(insertRecord);
                var reloadCandidate = new TerminRecord
                {
                    Titel = insertRecord.Titel,
                    Beschreibung = insertRecord.Beschreibung,
                    Datum = insertRecord.Datum,
                    StartUhrzeit = insertRecord.StartUhrzeit,
                    EndUhrzeit = insertRecord.EndUhrzeit,
                    SichtbarAb = insertRecord.SichtbarAb,
                    SichtbarBis = insertRecord.SichtbarBis,
                    Aktiv = insertRecord.Aktiv,
                    CreatedAt = insertRecord.CreatedAt,
                    UpdatedAt = insertRecord.UpdatedAt
                };
                var reloadResponse = await client
                    .From<TerminRecord>()
                    .Where(x => x.Titel == insertRecord.Titel)
                    .Where(x => x.Datum == insertRecord.Datum)
                    .Get();
                var created = reloadResponse?.Models?
                    .Select(NormalizeTerminRecord)
                    .Where(x => IsSameTerminForReload(x, reloadCandidate))
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
                    .Set(x => x.UpdatedAt, DateTime.UtcNow)
                    .Update();

                _logger?.LogInformation("UpdateTerminAsync updated termin {TerminId}", record.Id);
                return true;
            },
            false);

        public Task<bool> DeleteTerminAsync(long id) => ExecuteAsync(
            "DeleteTerminAsync",
            async () =>
            {
                if (id <= 0)
                    return false;

                var client = await EnsureClientAsync();
                await client
                    .From<TerminRecord>()
                    .Where(x => x.Id == id)
                    .Delete();

                _logger?.LogInformation("DeleteTerminAsync deleted termin {TerminId}", id);
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

        public Task<BekanntmachungRecord?> CreateBekanntmachungAsync(BekanntmachungInsertRecord request) => ExecuteAsync<BekanntmachungRecord?>(
            "CreateBekanntmachungAsync",
            async () =>
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Titel) || string.IsNullOrWhiteSpace(request.InhaltHtml))
                    return null;

                var client = await EnsureClientAsync();
                var now = DateTime.UtcNow;
                var insertRecord = new BekanntmachungInsertRecord
                {
                    Titel = CleanRequiredText(request.Titel),
                    InhaltHtml = CleanRequiredText(request.InhaltHtml),
                    SichtbarAb = NormalizeTimestampWithoutTimeZone(request.SichtbarAb),
                    SichtbarBis = NormalizeTimestampWithoutTimeZone(request.SichtbarBis),
                    SortOrder = request.SortOrder,
                    Aktiv = request.Aktiv,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await client.From<BekanntmachungInsertRecord>().Insert(insertRecord);
                var reloadCandidate = new BekanntmachungRecord
                {
                    Titel = insertRecord.Titel,
                    InhaltHtml = insertRecord.InhaltHtml,
                    SichtbarAb = insertRecord.SichtbarAb,
                    SichtbarBis = insertRecord.SichtbarBis,
                    SortOrder = insertRecord.SortOrder,
                    Aktiv = insertRecord.Aktiv,
                    CreatedAt = insertRecord.CreatedAt,
                    UpdatedAt = insertRecord.UpdatedAt
                };
                var reloadResponse = await client.From<BekanntmachungRecord>().Get();
                var created = reloadResponse?.Models?
                    .Select(NormalizeBekanntmachungRecord)
                    .Where(x => IsSameBekanntmachungForReload(x, reloadCandidate))
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

        public Task<bool> DeleteBekanntmachungAsync(long id) => ExecuteAsync(
            "DeleteBekanntmachungAsync",
            async () =>
            {
                if (id <= 0)
                    return false;

                var client = await EnsureClientAsync();
                await client
                    .From<BekanntmachungRecord>()
                    .Where(x => x.Id == id)
                    .Delete();

                _logger?.LogInformation("DeleteBekanntmachungAsync deleted bekanntmachung {BekanntmachungId}", id);
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

        private static DateTime NormalizeMeterEichjahr(DateTime value)
        {
            var normalized = value.Date.AddHours(12);
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

        private static DateTime CreateEditorNowDefault()
        {
            var now = DateTime.Now;
            return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Unspecified);
        }

        private static DateTime CreateEndOfDayTimestamp(DateTime date)
        {
            var normalized = NormalizeDateOnly(date);
            return new DateTime(normalized.Year, normalized.Month, normalized.Day, 23, 59, 0, DateTimeKind.Unspecified);
        }

        private static TimeSpan CreateDefaultTerminStartTime()
        {
            var now = DateTime.Now;
            return new TimeSpan(now.Hour, now.Minute, 0);
        }

        private static TimeSpan CreateDefaultTerminEndTime(TimeSpan startTime)
        {
            var candidate = startTime.Add(TimeSpan.FromHours(1));
            return candidate > new TimeSpan(23, 59, 0) ? new TimeSpan(23, 59, 0) : candidate;
        }

        private static bool IsCurrentlyVisible(bool aktiv, DateTime? sichtbarAb, DateTime? sichtbarBis, DateTime referenceTime)
        {
            return aktiv
                && (!sichtbarAb.HasValue || sichtbarAb.Value <= referenceTime)
                && (!sichtbarBis.HasValue || sichtbarBis.Value >= referenceTime);
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

        private static string GetMemberLastNameSortKey(MitgliedRecord? member)
        {
            if (!string.IsNullOrWhiteSpace(member?.Name))
                return member.Name.Trim();

            return FormatMemberName(member) ?? string.Empty;
        }

        private static string GetMemberFirstNameSortKey(MitgliedRecord? member)
        {
            if (!string.IsNullOrWhiteSpace(member?.Vorname))
                return member.Vorname.Trim();

            return string.Empty;
        }

        private void LogPostgrestFailure(string operation, PostgrestException ex)
        {
            var detail = BuildPostgrestDiagnosticDetail(ex);
            _logger?.LogError(ex, "{Operation} failed. {Detail}", operation, detail);
            Debug.WriteLine($"[{operation}] {detail}");
        }

        private static InvalidOperationException CreatePostgrestSaveException(string fallbackMessage, PostgrestException ex)
            => new(BuildPostgrestUserMessage(fallbackMessage, ex), ex);

        private static string BuildPostgrestUserMessage(string fallbackMessage, PostgrestException ex)
        {
            var detail = ExtractPostgrestRelevantMessage(ex);
            return string.IsNullOrWhiteSpace(detail)
                ? fallbackMessage
                : $"{fallbackMessage} {detail}";
        }

        private static string BuildPostgrestDiagnosticDetail(PostgrestException ex)
        {
            var parts = new List<string>();
            if ((int)ex.StatusCode > 0)
                parts.Add($"HTTP {(int)ex.StatusCode}");

            if (!string.IsNullOrWhiteSpace(ex.Reason.ToString()))
                parts.Add($"Reason={ex.Reason}");

            var relevantMessage = ExtractPostgrestRelevantMessage(ex);
            if (!string.IsNullOrWhiteSpace(relevantMessage))
                parts.Add(relevantMessage);

            return string.Join(" | ", parts.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.CurrentCulture));
        }

        private static string ExtractPostgrestRelevantMessage(PostgrestException ex)
        {
            var contentMessage = ExtractPostgrestContentMessage(ex.Content);
            if (!string.IsNullOrWhiteSpace(contentMessage))
                return contentMessage;

            return string.IsNullOrWhiteSpace(ex.Message)
                ? string.Empty
                : Regex.Replace(ex.Message.Trim(), "\\s+", " ");
        }

        private static string ExtractPostgrestContentMessage(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            try
            {
                using var document = JsonDocument.Parse(content);
                var values = new[]
                {
                    TryGetJsonString(document.RootElement, "message"),
                    TryGetJsonString(document.RootElement, "details"),
                    TryGetJsonString(document.RootElement, "hint"),
                    TryGetJsonString(document.RootElement, "code")
                };

                return string.Join(" | ", values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
            }
            catch
            {
                return Regex.Replace(content.Trim(), "\\s+", " ");
            }
        }

        private static string? TryGetJsonString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
                return null;

            return property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : property.ToString();
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
                .Where(x => MatchesPflichtstundenMitglied(x, mitgliedId, mitgliedId))
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

        private async Task<WartungsvertragBundle> LoadWartungsvertragBundleAsync()
        {
            var client = await EnsureClientAsync();
            var wartungsvertraegeResponse = await client.From<WartungsvertragRecord>().Get();
            var wartungsvertraege = wartungsvertraegeResponse?.Models?
                .Where(x => !x.IsDemo)
                .ToList()
                ?? new List<WartungsvertragRecord>();

            if (wartungsvertraege.Count == 0)
                return new WartungsvertragBundle(new List<WartungsvertragRecord>(), new List<WartungsvertragZuordnungRecord>(), new Dictionary<long, MitgliedRecord>(), new Dictionary<long, string>());

            var contractIds = wartungsvertraege.Select(x => x.Id).ToHashSet();
            var zuordnungenResponse = await client.From<WartungsvertragZuordnungRecord>().Get();
            var members = await GetMitgliederAsync();
            var operationalMembers = members
                .Where(OperationalDataFilter.IsOperationalMember)
                .Where(x => x.Id > 0)
                .ToDictionary(x => (long)x.Id, x => x);

            var activeDate = DateTime.Today;
            var activeAssignments = zuordnungenResponse?.Models?
                .Where(x => x.WartungsvertragId > 0)
                .Where(x => x.HauptmitgliedId > 0)
                .Where(x => contractIds.Contains(x.WartungsvertragId))
                .Where(x => operationalMembers.ContainsKey(x.HauptmitgliedId))
                .Where(x => IsWartungsvertragZuordnungAktivOn(x, activeDate))
                .OrderBy(x => x.WartungsvertragId)
                .ThenBy(x => x.GueltigAb)
                .ThenBy(x => x.Id)
                .ToList()
                ?? new List<WartungsvertragZuordnungRecord>();

            var parzellen = await GetAllParzellenAsync();
            var belegungen = await GetAllParzellenBelegungenAsync();
            var gardenLookup = BuildWartungsvertragGardenLookup(parzellen, belegungen, operationalMembers.Keys, activeDate);

            return new WartungsvertragBundle(wartungsvertraege, activeAssignments, operationalMembers, gardenLookup);
        }

        private static WartungsvertragAssignmentSaveResult CreateWartungsvertragAssignmentSaveResult(bool success, string message, int requestedCount, int addedCount, int remainingFreeSlots)
        {
            return new WartungsvertragAssignmentSaveResult
            {
                Success = success,
                Message = message,
                RequestedCount = requestedCount,
                AddedCount = addedCount,
                RemainingFreeSlots = remainingFreeSlots
            };
        }

        private async Task<WartungsvertragRecord?> GetWartungsvertragByIdInternalAsync(Client client, long wartungsvertragId)
        {
            var response = await client
                .From<WartungsvertragRecord>()
                .Where(x => x.Id == wartungsvertragId)
                .Get();

            return response?.Models?.FirstOrDefault();
        }

        private static bool IsWartungsvertragZuordnungAktivOn(WartungsvertragZuordnungRecord zuordnung, DateTime date)
        {
            var target = date.Date;
            var start = zuordnung.GueltigAb.Date;
            var end = zuordnung.GueltigBis?.Date;

            return start <= target && (!end.HasValue || end.Value >= target);
        }

        private static Dictionary<long, string> BuildWartungsvertragGardenLookup(
            IReadOnlyCollection<ParzelleRecord> parzellen,
            IReadOnlyCollection<ParzellenBelegungRecord> belegungen,
            IEnumerable<long> operationalMemberIds,
            DateTime activeDate)
        {
            var memberIds = operationalMemberIds.ToHashSet();
            var parzellenById = parzellen
                .Where(x => x.Id > 0 && !x.IsDemo)
                .ToDictionary(x => x.Id);

            return belegungen
                .Where(x => x.MitgliedId > 0 && memberIds.Contains(x.MitgliedId))
                .Where(x => x.ParzelleId > 0 && parzellenById.ContainsKey(x.ParzelleId))
                .Where(x => IsWartungsvertragBelegungAktivOn(x, activeDate))
                .GroupBy(x => (long)x.MitgliedId)
                .ToDictionary(
                    x => x.Key,
                    x => string.Join(", ", x
                        .Select(b => parzellenById[b.ParzelleId].GartenNr)
                        .Where(g => !string.IsNullOrWhiteSpace(g))
                        .Select(g => g.Trim())
                        .Distinct(StringComparer.CurrentCultureIgnoreCase)
                        .OrderBy(GetWartungsvertragGartenNrSortKey)
                        .ThenBy(g => g, StringComparer.CurrentCultureIgnoreCase)));
        }

        private static bool IsWartungsvertragBelegungAktivOn(ParzellenBelegungRecord belegung, DateTime date)
        {
            var target = date.Date;
            var start = belegung.VonDatum?.Date;
            var end = belegung.BisDatum?.Date;

            return (!start.HasValue || start.Value <= target)
                && (!end.HasValue || end.Value >= target);
        }

        private static ImpressumKontaktItem CreateImpressumKontaktItem(ImpressumFunktionSlotRecord slot, IReadOnlyDictionary<long, MitgliedRecord> membersById)
        {
            MitgliedRecord? member = null;
            if (slot.MitgliedId.HasValue && membersById.TryGetValue(slot.MitgliedId.Value, out var resolvedMember))
                member = resolvedMember;

            var isVorstandsvorsitzende = IsVorstandsvorsitzSlot(slot);

            return new ImpressumKontaktItem
            {
                Funktion = string.IsNullOrWhiteSpace(slot.Funktion) ? "Funktion" : slot.Funktion.Trim(),
                Name = BuildImpressumDisplayName(member),
                Email = string.Empty,
                Telefon = string.Empty,
                Handy = BuildImpressumHandy(member),
                Adresse = isVorstandsvorsitzende ? BuildImpressumAdresse(member) : string.Empty,
                IsVorstandsvorsitzende = isVorstandsvorsitzende
            };
        }

        private static bool IsBauausschussSlot(ImpressumFunktionSlotRecord slot)
        {
            var slotKey = (slot.SlotKey ?? string.Empty).Trim().ToLowerInvariant();
            var funktion = (slot.Funktion ?? string.Empty).Trim().ToLowerInvariant();

            return slotKey.Contains("bauausschuss", StringComparison.Ordinal)
                || slotKey.Contains("ausschuss", StringComparison.Ordinal)
                || slotKey.Contains("bau", StringComparison.Ordinal)
                || funktion.Contains("bauausschuss", StringComparison.Ordinal)
                || funktion.Contains("bau-ausschuss", StringComparison.Ordinal)
                || funktion.Contains("bau ausschuss", StringComparison.Ordinal)
                || (funktion.Contains("bau", StringComparison.Ordinal) && funktion.Contains("ausschuss", StringComparison.Ordinal));
        }

        private static bool IsVorstandsvorsitzSlot(ImpressumFunktionSlotRecord slot)
        {
            var combined = string.Join(" ", new[] { slot.SlotKey, slot.Funktion }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim().ToLowerInvariant()));

            if (string.IsNullOrWhiteSpace(combined) || !combined.Contains("vorsitz", StringComparison.Ordinal))
                return false;

            return !combined.Contains("stellv", StringComparison.Ordinal)
                && !combined.Contains("stellvertr", StringComparison.Ordinal)
                && !combined.Contains("stv", StringComparison.Ordinal)
                && !combined.Contains("2. vorsitz", StringComparison.Ordinal)
                && !combined.Contains("zweite vorsitz", StringComparison.Ordinal);
        }

        private static string BuildImpressumDisplayName(MitgliedRecord? member)
        {
            if (member == null)
                return "Aktuell nicht hinterlegt.";

            var fullName = string.Join(" ", new[] { member.Vorname, member.Name }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim()));

            return string.IsNullOrWhiteSpace(fullName)
                ? "Aktuell nicht hinterlegt."
                : fullName;
        }

        private static string BuildImpressumHandy(MitgliedRecord? member)
        {
            if (member == null)
                return string.Empty;

            return string.IsNullOrWhiteSpace(member.Handy)
                ? string.Empty
                : member.Handy.Trim();
        }

        private static string BuildImpressumAdresse(MitgliedRecord? member)
        {
            if (member == null)
                return string.Empty;

            var addressParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(member.Adresse))
                addressParts.Add(member.Adresse.Trim());

            var cityParts = new[] { member.Plz, member.Ort }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .ToList();

            if (cityParts.Count > 0)
                addressParts.Add(string.Join(" ", cityParts));

            return string.Join(", ", addressParts);
        }

        private static WartungsvertragOverviewItem CreateWartungsvertragOverviewItem(
            WartungsvertragRecord contract,
            IReadOnlyDictionary<long, int> countsByContractId)
        {
            var belegt = countsByContractId.TryGetValue(contract.Id, out var count) ? count : 0;
            var maxKontingent = Math.Max(1, contract.MaxAktiveZuordnungen);

            return new WartungsvertragOverviewItem
            {
                Id = contract.Id,
                Titel = CleanWartungsvertragText(contract.Titel, "Wartungsvertrag"),
                Kurzbeschreibung = BuildWartungsvertragKurzbeschreibung(contract),
                MaxKontingent = maxKontingent,
                Belegt = belegt,
                Frei = Math.Max(0, maxKontingent - belegt),
                Aktiv = contract.Aktiv
            };
        }

        private static MemberWartungsvertragItem? CreateMemberWartungsvertragItem(
            WartungsvertragZuordnungRecord zuordnung,
            IReadOnlyDictionary<long, WartungsvertragRecord> contractsById,
            IReadOnlyDictionary<long, int> countsByContractId)
        {
            if (!contractsById.TryGetValue(zuordnung.WartungsvertragId, out var contract))
                return null;

            var overview = CreateWartungsvertragOverviewItem(contract, countsByContractId);
            return new MemberWartungsvertragItem
            {
                ZuordnungId = zuordnung.Id,
                Id = overview.Id,
                Titel = overview.Titel,
                Kurzbeschreibung = overview.Kurzbeschreibung,
                MaxKontingent = overview.MaxKontingent,
                Belegt = overview.Belegt,
                Frei = overview.Frei,
                Aktiv = overview.Aktiv,
                GueltigAb = zuordnung.GueltigAb,
                GueltigBis = zuordnung.GueltigBis
            };
        }

        private static WartungsvertragAssignedMemberItem CreateWartungsvertragAssignedMemberItem(
            WartungsvertragZuordnungRecord zuordnung,
            WartungsvertragBundle bundle)
        {
            bundle.MembersById.TryGetValue(zuordnung.HauptmitgliedId, out var member);

            return new WartungsvertragAssignedMemberItem
            {
                MitgliedId = zuordnung.HauptmitgliedId is > 0 and <= int.MaxValue ? (int)zuordnung.HauptmitgliedId : 0,
                DisplayName = BuildWartungsvertragMemberDisplayName(member, zuordnung.HauptmitgliedId),
                GartenNummern = bundle.GardenNumbersByMemberId.TryGetValue(zuordnung.HauptmitgliedId, out var gardens)
                    ? gardens
                    : string.Empty,
                MitgliedskontextText = member?.HauptmitgliedId is > 0
                    ? "Nebenmitglied"
                    : "Hauptmitglied",
                GueltigAb = zuordnung.GueltigAb,
                GueltigBis = zuordnung.GueltigBis
            };
        }

        private static string BuildWartungsvertragMemberDisplayName(MitgliedRecord? member, long fallbackMemberId)
        {
            return FormatMemberName(member) ?? $"Mitglied #{fallbackMemberId}";
        }

        private static string BuildWartungsvertragKurzbeschreibung(WartungsvertragRecord contract)
        {
            var text = CleanWartungsvertragText(FirstNonEmpty(contract.Beschreibung, contract.Bereich, contract.Bemerkung));
            if (text.Length <= 140)
                return text;

            return text[..137].TrimEnd() + "...";
        }

        private static string CleanWartungsvertragText(string? text, string fallback = "-")
        {
            if (string.IsNullOrWhiteSpace(text))
                return fallback;

            var cleaned = Regex.Replace(text.Trim(), "\\s+", " ");
            return string.IsNullOrWhiteSpace(cleaned) ? fallback : cleaned;
        }

        private static int GetWartungsvertragGartenNrSortKey(string? gartenNr)
        {
            if (string.IsNullOrWhiteSpace(gartenNr))
                return int.MaxValue;

            var digits = new string(gartenNr.TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digits, out var number) ? number : int.MaxValue;
        }

        private static int NormalizeWartungsvertragKontingent(int maxAktiveZuordnungen)
            => Math.Max(1, maxAktiveZuordnungen);

        private static bool IsSameWartungsvertragForReload(WartungsvertragRecord existing, WartungsvertragRecord candidate)
        {
            return string.Equals(NormalizeComparableWartungsvertragText(existing.Titel), NormalizeComparableWartungsvertragText(candidate.Titel), StringComparison.CurrentCulture)
                && string.Equals(NormalizeComparableWartungsvertragText(existing.Beschreibung), NormalizeComparableWartungsvertragText(candidate.Beschreibung), StringComparison.CurrentCulture)
                && existing.MaxAktiveZuordnungen == candidate.MaxAktiveZuordnungen
                && existing.Aktiv == candidate.Aktiv;
        }

        private static string NormalizeComparableWartungsvertragText(string? value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        private sealed record WartungsvertragBundle(
            List<WartungsvertragRecord> Contracts,
            List<WartungsvertragZuordnungRecord> ActiveAssignments,
            Dictionary<long, MitgliedRecord> MembersById,
            Dictionary<long, string> GardenNumbersByMemberId);

        private async Task<List<HomeWorkAssignmentItem>> LoadStartseiteArbeitseinsaetzeAsync()
        {
            var client = await EnsureClientAsync();
            var response = await client.From<StartseiteArbeitseinsatzRecord>().Get();
            var records = response?.Models?.ToList() ?? new List<StartseiteArbeitseinsatzRecord>();

            await EnrichStartseiteArbeitseinsatzTimesAsync(client, records);
            await EnrichStartseiteArbeitseinsatzRegistrationStateAsync(client, records);
            records = await FilterVisibleStartseiteArbeitseinsaetzeAsync(client, records);

            return records
                .OrderBy(x => x.Datum ?? DateTime.MaxValue)
                .ThenBy(x => NormalizeTimeValue(x.Beginn) ?? "99:99")
                .ThenBy(x => NormalizeTimeValue(x.Ende) ?? "99:99")
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
            records = await FilterVisibleStartseiteTermineAsync(client, records);

            return records
                .OrderBy(x => x.Datum ?? DateTime.MaxValue)
                .ThenBy(x => NormalizeTimeValue(x.Beginn) ?? "99:99", StringComparer.Ordinal)
                .ThenBy(x => NormalizeTimeValue(x.Ende) ?? "99:99", StringComparer.Ordinal)
                .ThenBy(x => FirstNonEmpty(x.Titel, x.Thema) ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x.Id)
                .Select(MapHomeAppointment)
                .ToList()
                ?? new List<HomeAppointmentItem>();
        }

        private async Task<List<HomeAnnouncementItem>> LoadStartseiteBekanntmachungenAsync()
        {
            var client = await EnsureClientAsync();
            var response = await client.From<StartseiteBekanntmachungRecord>().Get();
            var records = response?.Models?.ToList() ?? new List<StartseiteBekanntmachungRecord>();
            records = await FilterVisibleStartseiteBekanntmachungenAsync(client, records);

            return records
                .OrderByDescending(x => x.VeroeffentlichtAm ?? x.UpdatedAt ?? DateTime.MinValue)
                .ThenBy(x => FirstNonEmpty(x.Titel, x.Betreff, x.Thema) ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                .Select(MapHomeAnnouncement)
                .ToList()
                ?? new List<HomeAnnouncementItem>();
        }

        private async Task<List<StartseiteArbeitseinsatzRecord>> FilterVisibleStartseiteArbeitseinsaetzeAsync(global::Supabase.Client client, List<StartseiteArbeitseinsatzRecord> records)
        {
            if (records.Count == 0)
                return records;

            var response = await client.From<ArbeitseinsatzRecord>().Get();
            var byId = response?.Models?
                .Select(NormalizeArbeitseinsatzRecord)
                .ToDictionary(x => x.Id) ?? new Dictionary<long, ArbeitseinsatzRecord>();
            var now = CreateEditorNowDefault();

            return records
                .Where(x => byId.TryGetValue(x.Id, out var record)
                            && IsCurrentlyVisible(record.Aktiv, record.SichtbarAb, record.SichtbarBis, now))
                .ToList();
        }

        private async Task<List<StartseiteTerminRecord>> FilterVisibleStartseiteTermineAsync(global::Supabase.Client client, List<StartseiteTerminRecord> records)
        {
            if (records.Count == 0)
                return records;

            var response = await client.From<TerminRecord>().Get();
            var byId = response?.Models?
                .Select(NormalizeTerminRecord)
                .ToDictionary(x => x.Id) ?? new Dictionary<long, TerminRecord>();
            var now = CreateEditorNowDefault();

            return records
                .Where(x => byId.TryGetValue(x.Id, out var record)
                            && IsCurrentlyVisible(record.Aktiv, record.SichtbarAb, record.SichtbarBis, now))
                .ToList();
        }

        private async Task<List<StartseiteBekanntmachungRecord>> FilterVisibleStartseiteBekanntmachungenAsync(global::Supabase.Client client, List<StartseiteBekanntmachungRecord> records)
        {
            if (records.Count == 0)
                return records;

            var response = await client.From<BekanntmachungRecord>().Get();
            var byId = response?.Models?
                .Select(NormalizeBekanntmachungRecord)
                .ToDictionary(x => x.Id) ?? new Dictionary<long, BekanntmachungRecord>();
            var now = CreateEditorNowDefault();

            return records
                .Where(x => byId.TryGetValue(x.BekanntmachungId ?? x.Id, out var record)
                            && IsCurrentlyVisible(record.Aktiv, record.SichtbarAb, record.SichtbarBis, now))
                .ToList();
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
                RegistrationInfo = BuildWorkAssignmentRegistrationInfo(record, capacityText),
                CanRegister = record.AnmeldungMoeglich == true,
                CanSignOff = record.IstAngemeldet
            };
        }

        private static string BuildWorkAssignmentRegistrationInfo(StartseiteArbeitseinsatzRecord record, string capacityText)
        {
            if (record.IstAngemeldet)
            {
                return string.IsNullOrWhiteSpace(capacityText)
                    ? "Du bist angemeldet"
                    : $"Du bist angemeldet · {capacityText}";
            }

            if (!string.IsNullOrWhiteSpace(capacityText))
                return capacityText;

            return record.AnmeldungMoeglich == true
                ? "Anmeldung möglich"
                : string.Empty;
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
                Id = record.Id,
                Title = title,
                Subtitle = published.HasValue ? published.Value.ToString("dd.MM.yyyy") : string.Empty,
                Content = NormalizeHomeText(FirstNonEmpty(record.Inhalt, record.Text, record.InhaltHtml, record.Beschreibung, record.Kurztext)),
                HtmlContent = FirstNonEmpty(record.InhaltHtml, record.Inhalt, record.Text, record.Beschreibung, record.Kurztext) ?? string.Empty,
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

        private static bool MatchesPflichtstundenMitglied(PflichtstundenUebersichtRecord record, int mitgliedId, int homeMitgliedId)
        {
            return record.MitgliedId == mitgliedId
                || record.HauptmitgliedId == mitgliedId
                || record.HauptmitgliedId == homeMitgliedId;
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

                record.IstAngemeldet = isAlreadyRegistered;
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

            var availableMediums = await GetAvailableRfidMediumOptionsInternalAsync(parzelle);
            if (availableMediums.All(x => !string.Equals(x.Key, normalizedMedium, StringComparison.OrdinalIgnoreCase)))
            {
                return new RfidAssignmentCheckResult
                {
                    IsValid = false,
                    Message = "Das gewählte Medium ist für diese Parzelle aktuell nicht auswählbar.",
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

        private async Task<List<RfidMediumOption>> GetAvailableRfidMediumOptionsInternalAsync(ParzelleRecord parzelle)
        {
            var options = new List<RfidMediumOption>();
            if (parzelle == null || parzelle.Id <= 0)
                return options;

            var today = DateTime.Today;
            var hasStromContext = parzelle.HatStrom
                || !string.IsNullOrWhiteSpace(NormalizeRfidTagUid(parzelle.RfidStrom))
                || await GetActiveStromzaehlerAsync(parzelle.Id, today) != null;

            var hasWasserContext = parzelle.HatWasser
                || !string.IsNullOrWhiteSpace(NormalizeRfidTagUid(parzelle.RfidWasser))
                || await GetActiveWasserzaehlerAsync(parzelle.Id, today) != null;

            if (hasStromContext)
                options.Add(new RfidMediumOption("strom", "Strom"));

            if (hasWasserContext)
                options.Add(new RfidMediumOption("wasser", "Wasser"));

            if (options.Count == 0)
            {
                options.Add(new RfidMediumOption("strom", "Strom"));
                options.Add(new RfidMediumOption("wasser", "Wasser"));
            }

            return options;
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
