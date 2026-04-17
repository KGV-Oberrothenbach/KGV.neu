using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using KGV.Core.Diagnostics;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Core.Utilities;
using KGV.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Supabase;
using Supabase.Postgrest.Exceptions;

namespace KGV.Infrastructure.Services
{
    public class SupabaseService : ISupabaseService
    {
        private const string DokumentUploadFunctionName = "kgv-upload-document";
        private const string AllowUserMeterReadingSubmissionsSettingKey = "allow_user_meter_reading_submissions";
        private readonly ISupabaseClientFactory _clientFactory;
        private readonly ILogger<SupabaseService>? _logger;
        private readonly Func<UserContext?>? _currentUserContextAccessor;
        private readonly IAuthService _authService;
        private readonly string _supabaseUrl;
        private readonly string _publishableKey;
        private readonly HttpClient _documentUploadHttpClient;
        private Client? _client;

        public SupabaseService(
            ISupabaseClientFactory clientFactory,
            ILogger<SupabaseService>? logger,
            Func<UserContext?>? currentUserContextAccessor,
            IAuthService authService,
            string supabaseUrl,
            string publishableKey)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _logger = logger;
            _currentUserContextAccessor = currentUserContextAccessor;
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _supabaseUrl = (supabaseUrl ?? string.Empty).Trim();
            _publishableKey = (publishableKey ?? string.Empty).Trim();
            _documentUploadHttpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(3)
            };
        }

        public async Task<DokumentUploadResult> CreatePachtvertragDokumentAsync(PachtvertragDokumentRequest request)
        {
            if (request == null)
                return DokumentUploadResult.Fail("Bitte zuerst einen gültigen Pachtvertrag vorbereiten.", "VALIDATION");

            try
            {
                var context = await ResolvePachtvertragRequestAsync(request);

                var uploadRequest = PachtvertragDokumentFactory.CreateUploadRequest(
                    context.Member,
                    context.SecondaryMember,
                    context.Parzelle,
                    context.Saison,
                    context.Vertragsbeginn,
                    altvertragDatum: request.AltvertragDatum,
                    context.GesetzlicherVertreterSnapshot,
                    context.BankverbindungSnapshot,
                    request.Status);

                return await CreateDokumentAsync(uploadRequest);
            }
            catch (InvalidOperationException ex)
            {
                _logger?.LogWarning(ex, "CreatePachtvertragDokumentAsync(request) validation failed");
                return DokumentUploadResult.Fail(ex.Message, "VALIDATION");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "CreatePachtvertragDokumentAsync(request) failed");
                return DokumentUploadResult.Fail("Pachtvertrag konnte aktuell nicht erzeugt werden.", "UNEXPECTED");
            }
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
                var members = response?.Models?
                    .OrderBy(x => x.Name ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => x.Vorname ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => x.Email ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                    .ToList()

                    ?? new List<MitgliedRecord>();

                await ApplyAppUserRolesAsync(client, members);
                return members
                    .Where(OperationalDataFilter.IsOperationalMember)
                    .ToList();
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

                var member = response?.Models?.FirstOrDefault();
                return await ApplyAppUserRoleAsync(client, member);
            },
            null);

        public Task<MitgliedGesetzlicherVertreterRecord?> GetAktivenGesetzlichenVertreterAsync(int minderjaehrigesMitgliedId, DateTime? stichtag = null) => ExecuteAsync<MitgliedGesetzlicherVertreterRecord?>(
            "GetAktivenGesetzlichenVertreterAsync",
            async () =>
            {
                if (minderjaehrigesMitgliedId <= 0)
                    return null;

                var client = await EnsureClientAsync();
                var response = await client
                    .From<MitgliedGesetzlicherVertreterRecord>()
                    .Where(x => x.MinderjaehrigesMitgliedId == minderjaehrigesMitgliedId)
                    .Get();

                return ResolveAktivenGesetzlichenVertreter(response?.Models, stichtag);
            },
            null);

        public Task<MitgliedGesetzlicherVertreterRecord?> SaveGesetzlichenVertreterAsync(GesetzlicherVertreterSaveRequest request) => ExecuteAsync<MitgliedGesetzlicherVertreterRecord?>(
            "SaveGesetzlichenVertreterAsync",
            async () =>
            {
                if (request == null)
                    return null;

                var minderjaehrigesMitgliedId = request.MinderjaehrigesMitgliedId;
                var vertreterMitgliedId = request.VertreterMitgliedId;
                if (minderjaehrigesMitgliedId <= 0 || vertreterMitgliedId <= 0 || minderjaehrigesMitgliedId == vertreterMitgliedId)
                    return null;

                var minderjaehrigesMitglied = await GetMitgliedByIdAsync(minderjaehrigesMitgliedId);
                var vertreterMitglied = await GetMitgliedByIdAsync(vertreterMitgliedId);
                if (minderjaehrigesMitglied == null || vertreterMitglied == null)
                    return null;

                var client = await EnsureClientAsync();
                var normalizedGueltigAb = NormalizeDate(request.GueltigAb) ?? DateTime.Today;
                var normalizedBemerkung = CleanOptionalText(request.Bemerkung);
                var existingResponse = await client
                    .From<MitgliedGesetzlicherVertreterRecord>()
                    .Where(x => x.MinderjaehrigesMitgliedId == minderjaehrigesMitgliedId)
                    .Get();
                var current = ResolveAktivenGesetzlichenVertreter(existingResponse?.Models, normalizedGueltigAb);

                if (current != null && current.VertreterMitgliedId == vertreterMitgliedId)
                {
                    await client
                        .From<MitgliedGesetzlicherVertreterRecord>()
                        .Where(x => x.Id == current.Id)
                        .Set(x => x.GueltigAb, normalizedGueltigAb)
                        .Set(x => x.Bemerkung, normalizedBemerkung)
                        .Update();
                }
                else
                {
                    if (current != null)
                    {
                        var gueltigBis = normalizedGueltigAb > current.GueltigAb.Date
                            ? normalizedGueltigAb.AddDays(-1)
                            : current.GueltigAb.Date;

                        await client
                            .From<MitgliedGesetzlicherVertreterRecord>()
                            .Where(x => x.Id == current.Id)
                            .Set(x => x.GueltigBis, gueltigBis)
                            .Update();
                    }

                    await client
                        .From<MitgliedGesetzlicherVertreterInsertRecord>()
                        .Insert(new MitgliedGesetzlicherVertreterInsertRecord
                        {
                            MinderjaehrigesMitgliedId = minderjaehrigesMitgliedId,
                            VertreterMitgliedId = vertreterMitgliedId,
                            GueltigAb = normalizedGueltigAb,
                            GueltigBis = null,
                            Bemerkung = normalizedBemerkung
                        });
                }

                var reloadResponse = await client
                    .From<MitgliedGesetzlicherVertreterRecord>()
                    .Where(x => x.MinderjaehrigesMitgliedId == minderjaehrigesMitgliedId)
                    .Get();
                return ResolveAktivenGesetzlichenVertreter(reloadResponse?.Models, normalizedGueltigAb);
            },
            null);

        public Task<GesetzlicherVertreterAufloesung> ResolveGesetzlicherVertreterAsync(int mitgliedId, DateTime? stichtag = null) => ExecuteAsync(
            "ResolveGesetzlicherVertreterAsync",
            async () =>
            {
                var member = await GetMitgliedByIdAsync(mitgliedId);
                if (member == null)
                    return GesetzlicherVertreterResolver.Resolve(null, null, null, stichtag);

                var relation = await GetAktivenGesetzlichenVertreterAsync(mitgliedId, stichtag);
                var vertreter = relation?.VertreterMitgliedId is > 0 and <= int.MaxValue
                    ? await GetMitgliedByIdAsync((int)relation.VertreterMitgliedId)
                    : null;

                return GesetzlicherVertreterResolver.Resolve(member, relation, vertreter, stichtag);
            },
            GesetzlicherVertreterResolver.Resolve(null, null, null, stichtag));

        public Task<VereinskonfigurationRecord?> GetAktiveVereinskonfigurationAsync() => ExecuteAsync<VereinskonfigurationRecord?>(
            "GetAktiveVereinskonfigurationAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client
                    .From<VereinskonfigurationRecord>()
                    .Where(x => x.Aktiv == true)
                    .Get();

                return response?.Models?
                    .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt ?? DateTime.MinValue)
                    .ThenByDescending(x => x.Id)
                    .FirstOrDefault();
            },
            null);

        public Task<VereinskonfigurationRecord?> SaveAktiveVereinskonfigurationAsync(VereinskonfigurationRecord vereinskonfiguration) => ExecuteAsync<VereinskonfigurationRecord?>(
            "SaveAktiveVereinskonfigurationAsync",
            async () =>
            {
                if (vereinskonfiguration == null)
                    throw new ArgumentNullException(nameof(vereinskonfiguration));

                var client = await EnsureClientAsync();
                var normalized = new VereinskonfigurationRecord
                {
                    Id = vereinskonfiguration.Id,
                    Vereinsname = CleanOptionalText(vereinskonfiguration.Vereinsname),
                    Kurzname = CleanOptionalText(vereinskonfiguration.Kurzname),
                    Registerangabe = CleanOptionalText(vereinskonfiguration.Registerangabe),
                    Strasse = CleanOptionalText(vereinskonfiguration.Strasse),
                    Plz = CleanOptionalText(vereinskonfiguration.Plz),
                    Ort = CleanOptionalText(vereinskonfiguration.Ort),
                    StandardEmail = CleanOptionalText(vereinskonfiguration.StandardEmail),
                    StandardTelefon = CleanOptionalText(vereinskonfiguration.StandardTelefon),
                    Website = CleanOptionalText(vereinskonfiguration.Website),
                    Aktiv = true,
                    Kontoinhaber = CleanOptionalText(vereinskonfiguration.Kontoinhaber),
                    Bankname = CleanOptionalText(vereinskonfiguration.Bankname),
                    Iban = CleanOptionalText(vereinskonfiguration.Iban),
                    Bic = CleanOptionalText(vereinskonfiguration.Bic),
                    VerwendungszweckMitgliedsantrag = CleanOptionalText(vereinskonfiguration.VerwendungszweckMitgliedsantrag),
                    VerwendungszweckPachtvertrag = CleanOptionalText(vereinskonfiguration.VerwendungszweckPachtvertrag),
                    DokumentOrt = CleanOptionalText(vereinskonfiguration.DokumentOrt),
                    StandardHinweistext = CleanOptionalText(vereinskonfiguration.StandardHinweistext),
                    DatenschutzText = CleanOptionalText(vereinskonfiguration.DatenschutzText),
                    DatenschutzVersion = CleanOptionalText(vereinskonfiguration.DatenschutzVersion),
                    DatenschutzStand = NormalizeDate(vereinskonfiguration.DatenschutzStand)
                };

                var response = await client
                    .From<VereinskonfigurationRecord>()
                    .Where(x => x.Aktiv == true)
                    .Get();

                var existing = response?.Models?
                    .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt ?? DateTime.MinValue)
                    .ThenByDescending(x => x.Id)
                    .FirstOrDefault();

                if (existing == null)
                {
                    var insertResponse = await client
                        .From<VereinskonfigurationRecord>()
                        .Insert(normalized);

                    return insertResponse?.Models?.FirstOrDefault() ?? normalized;
                }

                await client
                    .From<VereinskonfigurationRecord>()
                    .Where(x => x.Id == existing.Id)
                    .Set(x => x.Vereinsname, normalized.Vereinsname)
                    .Set(x => x.Kurzname, normalized.Kurzname)
                    .Set(x => x.Registerangabe, normalized.Registerangabe)
                    .Set(x => x.Strasse, normalized.Strasse)
                    .Set(x => x.Plz, normalized.Plz)
                    .Set(x => x.Ort, normalized.Ort)
                    .Set(x => x.StandardEmail, normalized.StandardEmail)
                    .Set(x => x.StandardTelefon, normalized.StandardTelefon)
                    .Set(x => x.Website, normalized.Website)
                    .Set(x => x.Aktiv, true)
                    .Set(x => x.Kontoinhaber, normalized.Kontoinhaber)
                    .Set(x => x.Bankname, normalized.Bankname)
                    .Set(x => x.Iban, normalized.Iban)
                    .Set(x => x.Bic, normalized.Bic)
                    .Set(x => x.VerwendungszweckMitgliedsantrag, normalized.VerwendungszweckMitgliedsantrag)
                    .Set(x => x.VerwendungszweckPachtvertrag, normalized.VerwendungszweckPachtvertrag)
                    .Set(x => x.DokumentOrt, normalized.DokumentOrt)
                    .Set(x => x.StandardHinweistext, normalized.StandardHinweistext)
                    .Set(x => x.DatenschutzText, normalized.DatenschutzText)
                    .Set(x => x.DatenschutzVersion, normalized.DatenschutzVersion)
                    .Set(x => x.DatenschutzStand, normalized.DatenschutzStand)
                    .Update();

                var reloadResponse = await client
                    .From<VereinskonfigurationRecord>()
                    .Where(x => x.Id == existing.Id)
                    .Get();

                return reloadResponse?.Models?.FirstOrDefault() ?? normalized;
            },
            null);

        public Task<MitgliedRecord?> CreateMitgliedAsync(MemberDTO dto) => ExecuteAsync<MitgliedRecord?>(
            "CreateMitgliedAsync",
            async () =>
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Vorname) || string.IsNullOrWhiteSpace(dto.Nachname))
                    return null;

                if (dto.IstHauptmitglied && !MemberDTO.HauptmitgliedArbeitsstundenAltersregelTypOptions.Contains(dto.ArbeitsstundenAltersregelTyp, StringComparer.Ordinal))
                    return null;

                var client = await EnsureClientAsync();
                var insertRecord = new MitgliedInsertRecord
                {
                    Vorname = dto.Vorname.Trim(),
                    Name = dto.Nachname.Trim(),
                    Adresse = CleanOptionalText(dto.Strasse),
                    Plz = CleanOptionalText(dto.PLZ),
                    Ort = CleanOptionalText(dto.Ort),
                    Telefon = CleanOptionalText(dto.Telefon),
                    Handy = CleanOptionalText(dto.Mobilnummer),
                    Email = CleanOptionalText(dto.Email),
                    Geburtsdatum = dto.Geburtsdatum,
                    Bemerkung = CleanOptionalText(dto.Bemerkungen),
                    WhatsappEinwilligung = dto.WhatsappEinwilligung,
                    EmailRechnungEinwilligung = dto.EmailRechnungEinwilligung,
                    EmailInfoEinwilligung = dto.EmailInfoEinwilligung,
                    ArbeitsstundenAltersregelTyp = dto.IstHauptmitglied ? dto.ArbeitsstundenAltersregelTyp : "keine",
                    MitgliedSeit = dto.MitgliedSeit,
                    MitgliedEnde = dto.MitgliedEnde,
                    Aktiv = dto.Aktiv
                };

                await client.From<MitgliedInsertRecord>().Insert(insertRecord);

                var response = await client
                    .From<MitgliedRecord>()
                    .Where(x => x.Vorname == insertRecord.Vorname)
                    .Where(x => x.Name == insertRecord.Name)
                    .Get();

                var created = response?.Models?
                    .Where(x => string.Equals(CleanRequiredText(x.Vorname), insertRecord.Vorname, StringComparison.CurrentCulture))
                    .Where(x => string.Equals(CleanRequiredText(x.Name), insertRecord.Name, StringComparison.CurrentCulture))
                    .Where(x => string.Equals(CleanOptionalText(x.Email), insertRecord.Email, StringComparison.CurrentCulture))
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefault();

                return await ApplyAppUserRoleAsync(client, created);
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

                if (dto.IstHauptmitglied && !MemberDTO.HauptmitgliedArbeitsstundenAltersregelTypOptions.Contains(dto.ArbeitsstundenAltersregelTyp, StringComparer.Ordinal))
                    return false;

                var client = await EnsureClientAsync();
                await client
                    .From<MitgliedRecord>()
                    .Where(x => x.Id == dto.Id)
                    .Set(x => x.Vorname, CleanRequiredText(dto.Vorname))
                    .Set(x => x.Name, CleanRequiredText(dto.Nachname))
                    .Set(x => x.Email, existing.AuthUserId.HasValue ? existing.Email : CleanOptionalText(dto.Email))
                    .Set(x => x.Geburtsdatum, NormalizeDate(dto.Geburtsdatum))
                    .Set(x => x.Adresse, CleanOptionalText(dto.Strasse))
                    .Set(x => x.Plz, CleanOptionalText(dto.PLZ))
                    .Set(x => x.Ort, CleanOptionalText(dto.Ort))
                    .Set(x => x.Telefon, CleanOptionalText(dto.Telefon))
                    .Set(x => x.Handy, CleanOptionalText(dto.Mobilnummer))
                    .Set(x => x.Bemerkung, CleanOptionalText(dto.Bemerkungen))
                    .Set(x => x.WhatsappEinwilligung, dto.WhatsappEinwilligung)
                    .Set(x => x.EmailRechnungEinwilligung, dto.EmailRechnungEinwilligung)
                    .Set(x => x.EmailInfoEinwilligung, dto.EmailInfoEinwilligung)
                    .Set(x => x.ArbeitsstundenAltersregelTyp, dto.IstHauptmitglied ? dto.ArbeitsstundenAltersregelTyp : existing.ArbeitsstundenAltersregelTyp)
                    .Set(x => x.MitgliedSeit, NormalizeDate(dto.MitgliedSeit))
                    .Set(x => x.MitgliedEnde, NormalizeDate(dto.MitgliedEnde))
                    .Set(x => x.Aktiv, dto.MitgliedEnde == null)
                    .Update();

                return true;
            },
            false);

        public Task<MembershipEndResult> EndMembershipAsync(int mainMemberId, DateTime endDate, MembershipEndDecision? secondaryDecision, string userId, int timeoutMinutes = 10) => ExecuteAsync(
            "EndMembershipAsync",
            async () =>
            {
                if (mainMemberId <= 0)
                    return MembershipEndResult.Failure("Hauptmitglied ist ungültig.");

                if (!Guid.TryParse(userId, out var userGuid))
                    return MembershipEndResult.Failure("Aktueller Benutzer ist ungültig.");

                var client = await EnsureClientAsync();
                var mainMember = await GetMitgliedByIdAsync(mainMemberId);
                if (mainMember == null)
                    return MembershipEndResult.Failure("Hauptmitglied konnte nicht geladen werden.");

                if (mainMember.HauptmitgliedId.HasValue && mainMember.HauptmitgliedId.Value > 0)
                    return MembershipEndResult.Failure("Der Folgeentscheid ist nur für Hauptmitglieder verfügbar.");

                if (mainMember.MitgliedEnde.HasValue)
                    return MembershipEndResult.Failure("Die Mitgliedschaft ist bereits beendet.");

                if (mainMember.LockedByUserId != userGuid)
                    return MembershipEndResult.Failure("Kein gültiger Lock auf dem Hauptmitglied.");

                var normalizedEndDate = NormalizeDate(endDate) ?? DateTime.Today;
                var secondaryMember = await GetNebenmitgliedByHauptmitgliedIdAsync(mainMemberId);
                if (secondaryMember != null && !secondaryDecision.HasValue)
                    return MembershipEndResult.Failure("Für das vorhandene Nebenmitglied ist eine Folgeentscheidung erforderlich.");

                if (secondaryMember != null && HasActiveForeignMitgliedLock(secondaryMember, userGuid, timeoutMinutes))
                    return MembershipEndResult.Failure("Das Nebenmitglied ist aktuell gesperrt.");

                if (secondaryMember != null)
                {
                    switch (secondaryDecision)
                    {
                        case MembershipEndDecision.EndSecondaryMember:
                            await client
                                .From<MitgliedRecord>()
                                .Where(x => x.Id == secondaryMember.Id)
                                .Set(x => x.MitgliedEnde, normalizedEndDate)
                                .Set(x => x.Aktiv, false)
                                .Update();
                            break;
                        case MembershipEndDecision.PromoteSecondaryMember:
                            await client
                                .From<MitgliedRecord>()
                                .Where(x => x.Id == secondaryMember.Id)
                                .Set(x => x.HauptmitgliedId, (int?)null)
                                .Set(x => x.MitgliedEnde, (DateTime?)null)
                                .Set(x => x.Aktiv, true)
                                .Update();
                            break;
                    }
                }

                await client
                    .From<MitgliedRecord>()
                    .Where(x => x.Id == mainMemberId)
                    .Set(x => x.MitgliedEnde, normalizedEndDate)
                    .Set(x => x.Aktiv, false)
                    .Update();

                var updatedMainMember = await GetMitgliedByIdAsync(mainMemberId);
                MitgliedRecord? updatedSecondaryMember = null;
                if (secondaryMember != null)
                    updatedSecondaryMember = await GetMitgliedByIdAsync(secondaryMember.Id);

                var message = secondaryDecision switch
                {
                    MembershipEndDecision.EndSecondaryMember => "Haupt- und Nebenmitglied wurden beendet.",
                    MembershipEndDecision.PromoteSecondaryMember => "Hauptmitglied wurde beendet und das Nebenmitglied zum Hauptmitglied gemacht.",
                    _ => "Mitgliedschaft wurde beendet."
                };

                return MembershipEndResult.SuccessResult(message, updatedMainMember, updatedSecondaryMember, secondaryDecision);
            },
            MembershipEndResult.Failure("Mitgliedschaft konnte nicht beendet werden."));

        public Task<bool> GetAllowUserMeterReadingSubmissionsAsync() => ExecuteAsync(
            "GetAllowUserMeterReadingSubmissionsAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client
                    .From<AppSettingRecord>()
                    .Where(x => x.SettingKey == AllowUserMeterReadingSubmissionsSettingKey)
                    .Get();

                return response?.Models?.FirstOrDefault()?.BoolValue ?? false;
            },
            false);

        public Task<bool> SetAllowUserMeterReadingSubmissionsAsync(bool allowed) => ExecuteAsync(
            "SetAllowUserMeterReadingSubmissionsAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client
                    .From<AppSettingRecord>()
                    .Where(x => x.SettingKey == AllowUserMeterReadingSubmissionsSettingKey)
                    .Get();

                var existing = response?.Models?.FirstOrDefault();
                if (existing == null)
                {
                    await client.From<AppSettingRecord>().Insert(new AppSettingRecord
                    {
                        SettingKey = AllowUserMeterReadingSubmissionsSettingKey,
                        BoolValue = allowed,
                        UpdatedAt = DateTime.UtcNow
                    });

                    return true;
                }

                await client
                    .From<AppSettingRecord>()
                    .Where(x => x.SettingKey == AllowUserMeterReadingSubmissionsSettingKey)
                    .Set(x => x.BoolValue, allowed)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow)
                    .Update();

                return true;
            },
            false);

        public Task<UserPermissionSettings?> GetUserPermissionSettingsAsync(int mitgliedId) => ExecuteAsync<UserPermissionSettings?>(
            "GetUserPermissionSettingsAsync",
            async () =>
            {
                try
                {
                    AppLocalFileLog.Info("PermissionSettings.Load.Service", $"Started. MitgliedId={mitgliedId}");

                    if (mitgliedId <= 0)
                    {
                        AppLocalFileLog.Warning("PermissionSettings.Load.Service", $"Aborted because MitgliedId is invalid. MitgliedId={mitgliedId}");
                        return null;
                    }

                    var client = await EnsureClientAsync();
                    var mitglied = await GetMitgliedByIdAsync(mitgliedId);
                    if (mitglied == null)
                    {
                        AppLocalFileLog.Warning("PermissionSettings.Load.Service", $"No member record found. MitgliedId={mitgliedId}");
                        return null;
                    }

                    var appUser = await GetAppUserByMitgliedIdAsync(client, mitgliedId, mitglied.AuthUserId);
                    var role = NormalizeAppUserRole(appUser?.Role);
                    var grantedPermissions = PermissionService.NormalizeStoredPermissions(appUser?.PermissionGrants);
                    var revokedPermissions = PermissionService.NormalizeStoredPermissions(appUser?.PermissionRevocations);

                    AppLocalFileLog.Info(
                        "PermissionSettings.Load.Service",
                        $"Resolved. MitgliedId={mitgliedId}, MemberAuthUserId={mitglied.AuthUserId}, HasAppUserRecord={appUser != null}, AppUserUserId={appUser?.UserId}, Role={role}, PermissionGrants={(long)grantedPermissions}, PermissionRevocations={(long)revokedPermissions}, AppUserUpdatedAt={FormatNullableDateTime(appUser?.UpdatedAt)}");

                    return new UserPermissionSettings
                    {
                        AuthUserId = appUser?.UserId ?? mitglied.AuthUserId,
                        MitgliedId = mitgliedId,
                        Role = role,
                        HasAppUserRecord = appUser != null,
                        UpdatedAt = appUser?.UpdatedAt,
                        GrantedPermissions = grantedPermissions,
                        RevokedPermissions = revokedPermissions
                    };
                }
                catch (Exception ex)
                {
                    AppLocalFileLog.Error("PermissionSettings.Load.Service", $"Failed. MitgliedId={mitgliedId}", ex);
                    throw;
                }
            },
            null);

        public Task<bool> SetAppUserRoleAsync(int mitgliedId, string role) => ExecuteAsync(
            "SetAppUserRoleAsync",
            async () =>
            {
                if (mitgliedId <= 0)
                    return false;

                var client = await EnsureClientAsync();
                var mitglied = await GetMitgliedByIdAsync(mitgliedId);
                if (mitglied == null)
                    return false;

                var appUser = await GetAppUserByMitgliedIdAsync(client, mitgliedId, mitglied.AuthUserId);
                if (appUser == null)
                    return false;

                var normalizedRole = UserRoles.ToStorageValue(UserRoles.Parse(role));

                await client
                    .From<AppUserRecord>()
                    .Where(x => x.UserId == appUser.UserId)
                    .Set(x => x.MitgliedId, mitgliedId)
                    .Set(x => x.Role, normalizedRole)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow)
                    .Update();

                return true;
            },
            false);

        public Task<bool> SetUserPermissionSettingsAsync(int mitgliedId, string role, long grantedPermissions, long revokedPermissions) => ExecuteAsync(
            "SetUserPermissionSettingsAsync",
            async () =>
            {
                try
                {
                    AppLocalFileLog.Info(
                        "PermissionSettings.Save.Service",
                        $"Started. MitgliedId={mitgliedId}, Role={role}, PermissionGrants={grantedPermissions}, PermissionRevocations={revokedPermissions}");

                    if (mitgliedId <= 0)
                    {
                        AppLocalFileLog.Warning("PermissionSettings.Save.Service", $"Aborted because MitgliedId is invalid. MitgliedId={mitgliedId}");
                        return false;
                    }

                    var client = await EnsureClientAsync();
                    var mitglied = await GetMitgliedByIdAsync(mitgliedId);
                    if (mitglied == null)
                    {
                        AppLocalFileLog.Warning("PermissionSettings.Save.Service", $"Aborted because member record could not be loaded. MitgliedId={mitgliedId}");
                        return false;
                    }

                    var appUser = await GetAppUserByMitgliedIdAsync(client, mitgliedId, mitglied.AuthUserId);
                    AppLocalFileLog.Info(
                        "PermissionSettings.Save.Service",
                        $"AppUser lookup completed. MitgliedId={mitgliedId}, MemberAuthUserId={mitglied.AuthUserId}, HasAppUserRecord={appUser != null}, AppUserUserId={appUser?.UserId}, AppUserUpdatedAtBeforeSave={FormatNullableDateTime(appUser?.UpdatedAt)}");

                    if (appUser == null)
                    {
                        _logger?.LogWarning(
                            "SetUserPermissionSettingsAsync aborted because no app_user record could be resolved. MitgliedId={MitgliedId}, AuthUserId={AuthUserId}, Role={Role}, PermissionGrants={PermissionGrants}, PermissionRevocations={PermissionRevocations}",
                            mitgliedId,
                            mitglied.AuthUserId,
                            role,
                            grantedPermissions,
                            revokedPermissions);
                        AppLocalFileLog.Warning(
                            "PermissionSettings.Save.Service",
                            $"Aborted because no app_user record could be resolved. MitgliedId={mitgliedId}, MemberAuthUserId={mitglied.AuthUserId}, Role={role}, PermissionGrants={grantedPermissions}, PermissionRevocations={revokedPermissions}");
                        return false;
                    }

                    var normalizedRole = UserRoles.ToStorageValue(UserRoles.Parse(role));
                    var normalizedGrantedPermissions = (long)PermissionService.NormalizeStoredPermissions(grantedPermissions);
                    var normalizedRevokedPermissions = (long)PermissionService.NormalizeStoredPermissions(revokedPermissions);
                    long? storedMitgliedId = mitgliedId;
                    long? storedGrantedPermissions = normalizedGrantedPermissions;
                    long? storedRevokedPermissions = normalizedRevokedPermissions;
                    var previousUpdatedAt = appUser.UpdatedAt;
                    var updateTimestamp = DateTime.UtcNow;

                    _logger?.LogInformation(
                        "SetUserPermissionSettingsAsync started. MitgliedId={MitgliedId}, AppUserUserId={AppUserUserId}, PreviousUpdatedAt={PreviousUpdatedAt}, Role={Role}, PermissionGrants={PermissionGrants}, PermissionRevocations={PermissionRevocations}",
                        mitgliedId,
                        appUser.UserId,
                        previousUpdatedAt,
                        normalizedRole,
                        normalizedGrantedPermissions,
                        normalizedRevokedPermissions);

                    AppLocalFileLog.Info(
                        "PermissionSettings.Save.Service",
                        $"Update payload prepared. MitgliedId={mitgliedId}, AppUserUserId={appUser.UserId}, Role={normalizedRole}, PermissionGrants={normalizedGrantedPermissions}, PermissionRevocations={normalizedRevokedPermissions}, UpdatedAtBeforeSave={FormatNullableDateTime(previousUpdatedAt)}");

                    await client
                        .From<AppUserRecord>()
                        .Where(x => x.UserId == appUser.UserId)
                        .Set(x => x.MitgliedId, storedMitgliedId)
                        .Set(x => x.Role, normalizedRole)
                        .Set(x => x.PermissionGrants, storedGrantedPermissions)
                        .Set(x => x.PermissionRevocations, storedRevokedPermissions)
                        .Set(x => x.UpdatedAt, updateTimestamp)
                        .Update();

                    var persistedAppUser = await GetAppUserByUserIdAsync(client, appUser.UserId);
                    if (persistedAppUser == null)
                    {
                        _logger?.LogError(
                            "SetUserPermissionSettingsAsync failed verification because the updated app_user record could not be reloaded. MitgliedId={MitgliedId}, AppUserUserId={AppUserUserId}",
                            mitgliedId,
                            appUser.UserId);
                        AppLocalFileLog.Error(
                            "PermissionSettings.Save.Service",
                            $"Verification failed because persisted app_user could not be reloaded. MitgliedId={mitgliedId}, AppUserUserId={appUser.UserId}");
                        return false;
                    }

                    var persistedRole = NormalizeAppUserRole(persistedAppUser.Role);
                    var persistedGrantedPermissions = (long)PermissionService.NormalizeStoredPermissions(persistedAppUser.PermissionGrants);
                    var persistedRevokedPermissions = (long)PermissionService.NormalizeStoredPermissions(persistedAppUser.PermissionRevocations);
                    var updatedAtChanged = persistedAppUser.UpdatedAt != previousUpdatedAt;
                    var persistedAsExpected = persistedAppUser.MitgliedId == mitgliedId
                                              && string.Equals(persistedRole, normalizedRole, StringComparison.OrdinalIgnoreCase)
                                              && persistedGrantedPermissions == normalizedGrantedPermissions
                                              && persistedRevokedPermissions == normalizedRevokedPermissions;

                    AppLocalFileLog.Info(
                        "PermissionSettings.Save.Service",
                        $"Verification reloaded persisted app_user. MitgliedId={mitgliedId}, AppUserUserId={persistedAppUser.UserId}, PersistedRole={persistedRole}, PermissionGrants={persistedGrantedPermissions}, PermissionRevocations={persistedRevokedPermissions}, UpdatedAtBeforeSave={FormatNullableDateTime(previousUpdatedAt)}, UpdatedAtAfterSave={FormatNullableDateTime(persistedAppUser.UpdatedAt)}, UpdatedAtChanged={updatedAtChanged}, PersistedAsExpected={persistedAsExpected}");

                    if (!persistedAsExpected || !updatedAtChanged)
                    {
                        _logger?.LogError(
                            "SetUserPermissionSettingsAsync failed verification after update. MitgliedId={MitgliedId}, AppUserUserId={AppUserUserId}, ExpectedRole={ExpectedRole}, PersistedRole={PersistedRole}, ExpectedPermissionGrants={ExpectedPermissionGrants}, PersistedPermissionGrants={PersistedPermissionGrants}, ExpectedPermissionRevocations={ExpectedPermissionRevocations}, PersistedPermissionRevocations={PersistedPermissionRevocations}, PreviousUpdatedAt={PreviousUpdatedAt}, PersistedUpdatedAt={PersistedUpdatedAt}",
                            mitgliedId,
                            appUser.UserId,
                            normalizedRole,
                            persistedRole,
                            normalizedGrantedPermissions,
                            persistedGrantedPermissions,
                            normalizedRevokedPermissions,
                            persistedRevokedPermissions,
                            previousUpdatedAt,
                            persistedAppUser.UpdatedAt);
                        AppLocalFileLog.Error(
                            "PermissionSettings.Save.Service",
                            $"Verification failed after update. MitgliedId={mitgliedId}, AppUserUserId={appUser.UserId}, ExpectedRole={normalizedRole}, PersistedRole={persistedRole}, ExpectedPermissionGrants={normalizedGrantedPermissions}, PersistedPermissionGrants={persistedGrantedPermissions}, ExpectedPermissionRevocations={normalizedRevokedPermissions}, PersistedPermissionRevocations={persistedRevokedPermissions}, UpdatedAtBeforeSave={FormatNullableDateTime(previousUpdatedAt)}, UpdatedAtAfterSave={FormatNullableDateTime(persistedAppUser.UpdatedAt)}");
                        return false;
                    }

                    _logger?.LogInformation(
                        "SetUserPermissionSettingsAsync completed successfully. MitgliedId={MitgliedId}, AppUserUserId={AppUserUserId}, PermissionGrants={PermissionGrants}, PermissionRevocations={PermissionRevocations}, UpdatedAt={UpdatedAt}",
                        mitgliedId,
                        appUser.UserId,
                        persistedGrantedPermissions,
                        persistedRevokedPermissions,
                        persistedAppUser.UpdatedAt);

                    AppLocalFileLog.Info(
                        "PermissionSettings.Save.Service",
                        $"Completed successfully. MitgliedId={mitgliedId}, AppUserUserId={appUser.UserId}, PermissionGrants={persistedGrantedPermissions}, PermissionRevocations={persistedRevokedPermissions}, UpdatedAtBeforeSave={FormatNullableDateTime(previousUpdatedAt)}, UpdatedAtAfterSave={FormatNullableDateTime(persistedAppUser.UpdatedAt)}");

                    return true;
                }
                catch (Exception ex)
                {
                    AppLocalFileLog.Error(
                        "PermissionSettings.Save.Service",
                        $"Failed. MitgliedId={mitgliedId}, Role={role}, PermissionGrants={grantedPermissions}, PermissionRevocations={revokedPermissions}",
                        ex);
                    throw;
                }
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
                return await GetAblesungenAsync(meters);
            },
            new List<ZaehlerAblesungDTO>());

        public Task<List<ZaehlerAblesungDTO>> GetWasserAblesungenAsync(int parzelleId) => ExecuteAsync(
            "GetWasserAblesungenAsync",
            async () =>
            {
                var meters = await GetWasserzaehlerForParzelleAsync(parzelleId);
                return await GetAblesungenAsync(meters);
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

        public Task<bool> AddStromzaehlerAsync(StromzaehlerInsertRecord request)
            => AddZaehlerCoreAsync("strom", request?.ParzelleId ?? 0, request?.Zaehlernummer, request?.Eichdatum ?? default, request?.EingebautAm ?? default);

        public Task<bool> AddWasserzaehlerAsync(WasserzaehlerInsertRecord request)
            => AddZaehlerCoreAsync("wasser", request?.ParzelleId ?? 0, request?.Zaehlernummer, request?.Eichdatum ?? default, request?.EingebautAm ?? default);

        public Task<ZaehlerInsertResult> TryAddStromzaehlerAsync(StromzaehlerInsertRecord request)
            => TryAddZaehlerCoreAsync("strom", request?.ParzelleId ?? 0, request?.Zaehlernummer, request?.Eichdatum ?? default, request?.EingebautAm ?? default);

        public Task<ZaehlerInsertResult> TryAddWasserzaehlerAsync(WasserzaehlerInsertRecord request)
            => TryAddZaehlerCoreAsync("wasser", request?.ParzelleId ?? 0, request?.Zaehlernummer, request?.Eichdatum ?? default, request?.EingebautAm ?? default);

        public Task<bool> SetStromzaehlerAusgebautAmAsync(long stromzaehlerId, DateTime ausgebautAm) => ExecuteAsync(
            "SetStromzaehlerAusgebautAmAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                await client
                    .From<ZaehlerRecord>()
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
                    .From<ZaehlerRecord>()
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
                var normalizedPruefstatus = AblesungPruefstatus.Normalize(request.Pruefstatus, request.Freigegeben);
                var insertRecord = new AblesungInsertRecord
                {
                    ZaehlerId = request.ZaehlerId,
                    Ablesedatum = NormalizeDateTime(request.Ablesedatum),
                    Stand = request.Stand,
                    Art = AblesungArt.Normalize(request.Art),
                    Freigegeben = AblesungPruefstatus.IsFreigegeben(normalizedPruefstatus),
                    Pruefstatus = normalizedPruefstatus,
                    Pruefkommentar = CleanOptionalText(request.Pruefkommentar),
                    GeprueftVon = request.GeprueftVon,
                    GeprueftAm = request.GeprueftAm,
                    FotoPfad = CleanOptionalText(request.FotoPfad),
                    FotoDateiname = CleanOptionalText(request.FotoDateiname),
                    FotoDriveFileId = CleanOptionalText(request.FotoDriveFileId)
                };

                await client.From<AblesungInsertRecord>().Insert(insertRecord);
                _logger?.LogInformation(
                    "AddAblesungAsync saved reading to zaehler_ablesung. ZaehlerId={ZaehlerId}, Art={Art}, Ablesedatum={Ablesedatum}, FotoPfadPresent={FotoPfadPresent}, FotoDriveFileIdPresent={FotoDriveFileIdPresent}",
                    insertRecord.ZaehlerId,
                    insertRecord.Art,
                    insertRecord.Ablesedatum,
                    !string.IsNullOrWhiteSpace(insertRecord.FotoPfad),
                    !string.IsNullOrWhiteSpace(insertRecord.FotoDriveFileId));

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
                    .Set(x => x.Pruefstatus, AblesungPruefstatus.Freigegeben)
                    .Update();

                _logger?.LogInformation(
                    "UpdateAblesungAsync updated reading in zaehler_ablesung. AblesungId={AblesungId}, Ablesedatum={Ablesedatum}, FotoPfadPresent={FotoPfadPresent}",
                    ablesungId,
                    NormalizeDateTime(ablesedatum),
                    !string.IsNullOrWhiteSpace(fotoPfad));

                return true;
            },
            false);

        public Task<List<AblesungReviewItem>> GetOffeneAblesungenZurFreigabeAsync() => ExecuteAsync(
            "GetOffeneAblesungenZurFreigabeAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var ablesungenResponse = await client.From<AblesungRecord>().Get();
                var offeneAblesungen = ablesungenResponse?.Models?
                    .Where(x => !x.Freigegeben)
                    .Where(x => string.Equals(AblesungPruefstatus.Normalize(x.Pruefstatus, x.Freigegeben), AblesungPruefstatus.Eingereicht, StringComparison.Ordinal))
                    .ToList()
                    ?? new List<AblesungRecord>();

                if (offeneAblesungen.Count == 0)
                    return new List<AblesungReviewItem>();

                var meterIds = offeneAblesungen
                    .Select(x => x.ZaehlerId)
                    .Distinct()
                    .ToHashSet();

                var zaehlerResponse = await client.From<ZaehlerRecord>().Get();
                var meterById = (zaehlerResponse?.Models ?? new List<ZaehlerRecord>())
                    .Where(x => meterIds.Contains(x.Id))
                    .ToDictionary(x => x.Id, x => x);

                if (meterById.Count == 0)
                    return new List<AblesungReviewItem>();

                var parzelleIds = meterById.Values
                    .Select(x => (int)x.ParzelleId)
                    .Distinct()
                    .ToHashSet();

                var parzellenById = (await GetAllParzellenAsync())
                    .Where(x => parzelleIds.Contains(x.Id))
                    .ToDictionary(x => x.Id, x => x);

                var belegungen = await GetAllParzellenBelegungenAsync();
                var operativeMembersById = (await GetMitgliederAsync())
                    .Where(OperationalDataFilter.IsOperationalMember)
                    .ToDictionary(x => x.Id, x => x);

                var result = new List<AblesungReviewItem>();
                foreach (var record in offeneAblesungen)
                {
                    if (!meterById.TryGetValue(record.ZaehlerId, out var meter))
                        continue;

                    var parzelleId = (int)meter.ParzelleId;
                    parzellenById.TryGetValue(parzelleId, out var parzelle);

                    if (!OperationalDataFilter.IsOperationalText(parzelle?.GartenNr)
                        || !OperationalDataFilter.IsOperationalText(parzelle?.Anlage)
                        || !OperationalDataFilter.IsOperationalText(meter.Zaehlernummer))
                    {
                        continue;
                    }

                    var activeBelegung = belegungen
                        .Where(x => x.ParzelleId == parzelleId)
                        .Where(x => IsBelegungActiveOn(x, record.Ablesedatum))
                        .OrderByDescending(x => x.VonDatum ?? DateTime.MinValue)
                        .FirstOrDefault();

                    MitgliedRecord? member = null;
                    if (activeBelegung != null)
                    {
                        if (!operativeMembersById.TryGetValue(activeBelegung.MitgliedId, out member))
                            continue;
                    }

                    result.Add(new AblesungReviewItem
                    {
                        AblesungId = record.Id,
                        ZaehlerId = record.ZaehlerId,
                        ParzelleId = parzelleId,
                        GartenNr = parzelle?.GartenNr ?? parzelleId.ToString(),
                        Anlage = parzelle?.Anlage ?? string.Empty,
                        Medium = NormalizeZaehlerMedium(meter.Medium) ?? meter.Medium,
                        Zaehlernummer = meter.Zaehlernummer,
                        Ablesedatum = record.Ablesedatum,
                        Stand = record.Stand,
                        Pruefstatus = AblesungPruefstatus.Normalize(record.Pruefstatus, record.Freigegeben),
                        Pruefkommentar = record.Pruefkommentar,
                        GeprueftVon = record.GeprueftVon,
                        GeprueftAm = record.GeprueftAm,
                        MitgliedName = FormatMemberName(member),
                        QuelleHinweis = activeBelegung == null ? "Quelle im Modell nicht verfügbar" : "Aktive Belegung zur Ablesung",
                        FotoPfad = record.FotoPfad,
                        FotoDateiname = record.FotoDateiname,
                        FotoDriveFileId = record.FotoDriveFileId
                    });
                }

                return result
                    .OrderBy(x => x.Ablesedatum)
                    .ThenBy(x => GetGartenNrSortKey(x.GartenNr))
                    .ThenBy(x => x.GartenNr, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => x.MediumDisplay, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            },
            new List<AblesungReviewItem>());

        public Task<bool> CorrectAblesungImPruefprozessAsync(long ablesungId, DateTime ablesedatum, decimal stand, string korrekturkommentar, int geprueftVon, DateTime? geprueftAm = null) => ExecuteAsync(
            "CorrectAblesungImPruefprozessAsync",
            async () =>
            {
                if (ablesungId <= 0 || geprueftVon <= 0 || stand < 0)
                    return false;

                var normalizedKommentar = CleanOptionalText(korrekturkommentar);
                if (string.IsNullOrWhiteSpace(normalizedKommentar))
                    return false;

                var existing = await GetOffeneReviewAblesungAsync(ablesungId);
                if (existing == null)
                    return false;

                var normalizedGeprueftAm = geprueftAm ?? DateTime.UtcNow;
                var client = await EnsureClientAsync();
                await client
                    .From<AblesungRecord>()
                    .Where(x => x.Id == ablesungId)
                    .Set(x => x.Ablesedatum, NormalizeDateTime(ablesedatum))
                    .Set(x => x.Stand, stand)
                    .Set(x => x.Pruefstatus, AblesungPruefstatus.Freigegeben)
                    .Set(x => x.Pruefkommentar, BuildReviewCorrectionComment(normalizedKommentar))
                    .Set(x => x.GeprueftVon, geprueftVon)
                    .Set(x => x.GeprueftAm, normalizedGeprueftAm)
                    .Set(x => x.Freigegeben, true)
                    .Update();

                _logger?.LogInformation(
                    "CorrectAblesungImPruefprozessAsync corrected and approved submitted reading. AblesungId={AblesungId}, Ablesedatum={Ablesedatum}, Stand={Stand}, GeprueftVon={GeprueftVon}",
                    ablesungId,
                    NormalizeDateTime(ablesedatum),
                    stand,
                    geprueftVon);

                return true;
            },
            false);

        public Task<bool> RemoveAblesungImPruefprozessAsync(long ablesungId, string begruendung, int geprueftVon, DateTime? geprueftAm = null) => ExecuteAsync(
            "RemoveAblesungImPruefprozessAsync",
            async () =>
            {
                if (ablesungId <= 0 || geprueftVon <= 0)
                    return false;

                var normalizedBegruendung = CleanOptionalText(begruendung);
                if (string.IsNullOrWhiteSpace(normalizedBegruendung))
                    return false;

                var existing = await GetOffeneReviewAblesungAsync(ablesungId);
                if (existing == null)
                    return false;

                var normalizedGeprueftAm = geprueftAm ?? DateTime.UtcNow;
                var client = await EnsureClientAsync();
                await client
                    .From<AblesungRecord>()
                    .Where(x => x.Id == ablesungId)
                    .Set(x => x.Pruefstatus, AblesungPruefstatus.Abgelehnt)
                    .Set(x => x.Pruefkommentar, BuildReviewRemovalComment(normalizedBegruendung))
                    .Set(x => x.GeprueftVon, geprueftVon)
                    .Set(x => x.GeprueftAm, normalizedGeprueftAm)
                    .Set(x => x.Freigegeben, false)
                    .Update();

                _logger?.LogInformation(
                    "RemoveAblesungImPruefprozessAsync removed submitted reading from active process via rejected review state. AblesungId={AblesungId}, GeprueftVon={GeprueftVon}",
                    ablesungId,
                    geprueftVon);

                return true;
            },
            false);

        public Task<bool> UpdateAblesungPruefstatusAsync(long ablesungId, string pruefstatus, string? pruefkommentar, int? geprueftVon, DateTime? geprueftAm = null) => ExecuteAsync(
            "UpdateAblesungPruefstatusAsync",
            async () =>
            {
                if (ablesungId <= 0)
                    return false;

                var normalizedPruefstatus = AblesungPruefstatus.Normalize(pruefstatus);
                var normalizedPruefkommentar = CleanOptionalText(pruefkommentar);
                if (normalizedPruefstatus != AblesungPruefstatus.Eingereicht)
                {
                    if (string.IsNullOrWhiteSpace(normalizedPruefkommentar) || !geprueftVon.HasValue || geprueftVon.Value <= 0)
                        return false;
                }

                DateTime? normalizedGeprueftAm = normalizedPruefstatus == AblesungPruefstatus.Eingereicht
                    ? null
                    : geprueftAm ?? DateTime.UtcNow;

                var client = await EnsureClientAsync();
                await client
                    .From<AblesungRecord>()
                    .Where(x => x.Id == ablesungId)
                    .Set(x => x.Pruefstatus, normalizedPruefstatus)
                    .Set(x => x.Pruefkommentar, normalizedPruefkommentar)
                    .Set(x => x.GeprueftVon, normalizedPruefstatus == AblesungPruefstatus.Eingereicht ? null : geprueftVon)
                    .Set(x => x.GeprueftAm, normalizedGeprueftAm)
                    .Set(x => x.Freigegeben, AblesungPruefstatus.IsFreigegeben(normalizedPruefstatus))
                    .Update();

                _logger?.LogInformation(
                    "UpdateAblesungPruefstatusAsync updated reading review state. AblesungId={AblesungId}, Pruefstatus={Pruefstatus}, GeprueftVon={GeprueftVon}, GeprueftAm={GeprueftAm}",
                    ablesungId,
                    normalizedPruefstatus,
                    geprueftVon,
                    normalizedGeprueftAm);

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

                var member = response?.Models?
                    .OrderBy(x => x.Id)
                    .FirstOrDefault();

                return await ApplyAppUserRoleAsync(client, member);
            },
            null);

        public Task<string?> ResolveDokumentOpenUrlAsync(DocumentInfo? document, int expiresInSeconds = 3600) => ExecuteAsync<string?>(
            "ResolveDokumentOpenUrlAsync",
            async () =>
            {
                if (document == null)
                    return null;

                var normalizedDriveFileId = CleanOptionalText(document.DriveFileId);
                if (!string.IsNullOrWhiteSpace(normalizedDriveFileId))
                    return BuildGoogleDriveFileViewUrl(normalizedDriveFileId);

                var storagePath = CleanOptionalText(document.StoragePath);
                if (Uri.TryCreate(storagePath, UriKind.Absolute, out var absoluteUri)
                    && (string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
                {
                    return absoluteUri.ToString();
                }

                var normalizedBucket = CleanOptionalText(document.Bucket);
                if (!string.IsNullOrWhiteSpace(normalizedBucket) && !string.IsNullOrWhiteSpace(storagePath))
                {
                    var client = await EnsureClientAsync();
                    return await client.Storage.From(normalizedBucket).CreateSignedUrl(storagePath.TrimStart('/').Replace('\\', '/'), expiresInSeconds);
                }

                if (TryParseStorageReference(storagePath, out var bucket, out var path))
                {
                    var client = await EnsureClientAsync();
                    return await client.Storage.From(bucket).CreateSignedUrl(path, expiresInSeconds);
                }

                return null;
            },
            null);

        public Task<string?> ResolveAblesungFotoOpenUrlAsync(string? fotoPfad, string? fotoDriveFileId, int expiresInSeconds = 3600) => ExecuteAsync<string?>(
            "ResolveAblesungFotoOpenUrlAsync",
            async () =>
            {
                var normalizedFotoPfad = CleanOptionalText(fotoPfad);
                if (Uri.TryCreate(normalizedFotoPfad, UriKind.Absolute, out var absoluteUri)
                    && (string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
                {
                    return absoluteUri.ToString();
                }

                if (TryParseStorageReference(normalizedFotoPfad, out var bucket, out var path))
                {
                    var client = await EnsureClientAsync();
                    return await client.Storage.From(bucket).CreateSignedUrl(path, expiresInSeconds);
                }

                var normalizedDriveFileId = CleanOptionalText(fotoDriveFileId);
                if (!string.IsNullOrWhiteSpace(normalizedDriveFileId))
                    return BuildGoogleDriveFileViewUrl(normalizedDriveFileId);

                return null;
            },
            null);
        public Task<MitgliedRecord?> CreateNebenmitgliedAsync(NebenmitgliedCreateDTO request) => ExecuteAsync<MitgliedRecord?>(
            "CreateNebenmitgliedAsync",
            async () =>
            {
                if (request == null || request.HauptmitgliedId <= 0 || string.IsNullOrWhiteSpace(request.Vorname) || string.IsNullOrWhiteSpace(request.Nachname) || !request.MitgliedSeit.HasValue)
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
                return await ApplyAppUserRoleAsync(client, created);
            },
            null);
        public Task<List<SaisonRecord>> GetSaisonRecordsAsync() => ExecuteAsync(
            "GetSaisonRecordsAsync",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client.From<SaisonRecord>().Get();

                return response?.Models?
                    .OrderByDescending(SaisonverwaltungHelper.GetSaisonJahr)
                    .ToList()
                    ?? new List<SaisonRecord>();
            },
            new List<SaisonRecord>());

        public Task<SaisonRecord?> SaveSaisonAsync(SaisonRecord saison) => ExecuteAsync<SaisonRecord?>(
            "SaveSaisonAsync",
            async () =>
            {
                var normalized = SaisonverwaltungHelper.NormalizeForSave(saison);
                if (!SaisonverwaltungHelper.IsEditable(normalized))
                    throw new InvalidOperationException("Vergangene Jahre dürfen nicht bearbeitet werden.");

                var client = await EnsureClientAsync();
                var response = await client.From<SaisonRecord>().Get();
                var existing = response?.Models?
                    .FirstOrDefault(x => x.Id == normalized.Id || x.Jahr == normalized.Jahr);

                if (existing == null)
                {
                    var insertResponse = await client
                        .From<SaisonRecord>()
                        .Insert(normalized);

                    return insertResponse?.Models?.FirstOrDefault() ?? normalized;
                }

                await client
                    .From<SaisonRecord>()
                    .Where(x => x.Id == existing.Id)
                    .Set(x => x.Id, normalized.Id)
                    .Set(x => x.Jahr, normalized.Jahr)
                    .Set(x => x.PflichtstundenSoll, normalized.PflichtstundenSoll)
                    .Set(x => x.EuroProFehlstunde, normalized.EuroProFehlstunde)
                    .Set(x => x.Bemerkung, normalized.Bemerkung)
                    .Set(x => x.PachtProQm, normalized.PachtProQm)
                    .Set(x => x.Mitgliedsbeitrag, normalized.Mitgliedsbeitrag)
                    .Set(x => x.MitgliedsbeitragNebenmitglied, normalized.MitgliedsbeitragNebenmitglied)
                    .Set(x => x.Aufnahmegebuehr, normalized.Aufnahmegebuehr)
                    .Set(x => x.GebuehrBauantrag, normalized.GebuehrBauantrag)
                    .Update();

                var reloadResponse = await client
                    .From<SaisonRecord>()
                    .Where(x => x.Id == normalized.Id)
                    .Get();

                return reloadResponse?.Models?.FirstOrDefault() ?? normalized;
            },
            null);

        public Task<MitgliedRecord?> GetMitgliedByAuthUserIdAsync(Guid authUserId) => ExecuteAsync<MitgliedRecord?>(
            "GetMitgliedByAuthUserIdAsync(Guid)",
            async () =>
            {
                var client = await EnsureClientAsync();
                var response = await client
                    .From<MitgliedRecord>()
                    .Where(x => x.AuthUserId == authUserId)
                    .Get();

                var member = response?.Models?.FirstOrDefault();
                return await ApplyAppUserRoleAsync(client, member);
            },
            null);

        public async Task<MitgliedRecord?> GetMitgliedByAuthUserIdAsync(string authUserId)
        {
            if (!Guid.TryParse(authUserId, out var parsed))
                return null;

            return await GetMitgliedByAuthUserIdAsync(parsed);
        }

        public Task<bool> UpdateOwnContactAsync(int mitgliedId, string? telefon, string? handy, string? adresse, string? plz, string? ort) => ExecuteAsync(
            "UpdateOwnContactAsync",
            async () =>
            {
                if (mitgliedId <= 0)
                    return false;

                var client = await EnsureClientAsync();
                await client
                    .From<MitgliedRecord>()
                    .Where(x => x.Id == mitgliedId)
                    .Set(x => x.Telefon, CleanOptionalText(telefon))
                    .Set(x => x.Handy, CleanOptionalText(handy))
                    .Set(x => x.Adresse, CleanOptionalText(adresse))
                    .Set(x => x.Plz, CleanOptionalText(plz))
                    .Set(x => x.Ort, CleanOptionalText(ort))
                    .Update();

                return true;
            },
            false);

        public Task<bool> UpdateOwnContactAsync(int mitgliedId, string? telefon, string? handy, string? adresse, string? plz, string? ort, string? email, DateTime? geburtsdatum, DateTime? mitgliedSeit, bool whatsappEinwilligung) => ExecuteAsync(
            "UpdateOwnContactAsync(extended)",
            async () =>
            {
                if (mitgliedId <= 0)
                    return false;

                var existing = await GetMitgliedByIdAsync(mitgliedId);
                if (existing == null)
                    return false;

                var client = await EnsureClientAsync();
                await client
                    .From<MitgliedRecord>()
                    .Where(x => x.Id == mitgliedId)
                    .Set(x => x.Telefon, CleanOptionalText(telefon))
                    .Set(x => x.Handy, CleanOptionalText(handy))
                    .Set(x => x.Adresse, CleanOptionalText(adresse))
                    .Set(x => x.Plz, CleanOptionalText(plz))
                    .Set(x => x.Ort, CleanOptionalText(ort))
                    .Set(x => x.Email, existing.AuthUserId.HasValue ? existing.Email : CleanOptionalText(email))
                    .Set(x => x.Geburtsdatum, NormalizeDate(geburtsdatum))
                    .Set(x => x.MitgliedSeit, NormalizeDate(mitgliedSeit))
                    .Set(x => x.WhatsappEinwilligung, whatsappEinwilligung)
                    .Update();

                return true;
            },
            false);

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
                Status = ArbeitsstundenPruefprozess.NormalizeStatus(request.Status, request.Freigegeben),
                Freigegeben = request.Freigegeben,
                GenehmigtAm = request.GenehmigtAm,
                GenehmigtVon = request.GenehmigtVon,
                LockedByUserId = null,
                LockedAt = null
            };
        }

        private MitgliedInsertRecord CreateNebenmitgliedInsertPayload(NebenmitgliedCreateDTO request, MitgliedRecord hauptmitglied)
        {
            var adresse = request.AdresseUebernehmen
                ? CleanOptionalText(hauptmitglied.Adresse)
                : CleanOptionalText(request.Adresse);
            var plz = request.AdresseUebernehmen
                ? CleanOptionalText(hauptmitglied.Plz)
                : CleanOptionalText(request.Plz);
            var ort = request.AdresseUebernehmen
                ? CleanOptionalText(hauptmitglied.Ort)
                : CleanOptionalText(request.Ort);

            return new MitgliedInsertRecord
            {
                HauptmitgliedId = request.HauptmitgliedId,
                Name = CleanRequiredText(request.Nachname),
                Vorname = CleanRequiredText(request.Vorname),
                Adresse = adresse,
                Plz = plz,
                Ort = ort,
                Telefon = CleanOptionalText(request.Telefon),
                Handy = CleanOptionalText(request.Handy),
                Email = CleanOptionalText(request.Email),
                Geburtsdatum = request.Geburtsdatum.HasValue ? NormalizeDateOnly(request.Geburtsdatum.Value) : null,
                MitgliedSeit = NormalizeDateOnly(request.MitgliedSeit.Value),
                WhatsappEinwilligung = request.WhatsappEinwilligung,
                Aktiv = true
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
                    .Set(x => x.Status, ArbeitsstundenPruefprozess.NormalizeStatus(record.Status, record.Freigegeben))
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
        public Task<List<ArbeitsstundenPruefverlaufItem>> GetArbeitsstundenPruefverlaufAsync(int arbeitsstundeId) => ExecuteAsync(
            "GetArbeitsstundenPruefverlaufAsync",
            async () =>
            {
                if (arbeitsstundeId <= 0)
                    return new List<ArbeitsstundenPruefverlaufItem>();

                var client = await EnsureClientAsync();
                var response = await client
                    .From<ArbeitsstundenPruefverlaufRecord>()
                    .Where(x => x.ArbeitsstundeId == arbeitsstundeId)
                    .Get();

                var records = response?.Models?
                    .OrderByDescending(x => x.GeprueftAm)
                    .ThenByDescending(x => x.Id)
                    .ToList()
                    ?? new List<ArbeitsstundenPruefverlaufRecord>();

                if (records.Count == 0)
                    return new List<ArbeitsstundenPruefverlaufItem>();

                var mitglieder = await GetMitgliederAsync();
                var mitgliederById = mitglieder.ToDictionary(x => x.Id, x => x);

                return records.Select(record =>
                {
                    var vorherSnapshot = DeserializeArbeitsstundenPruefSnapshot(record.VorherSnapshot);
                    var nachherSnapshot = DeserializeArbeitsstundenPruefSnapshot(record.NachherSnapshot);
                    var vorherName = ResolveArbeitsstundenSnapshotMitgliedName(vorherSnapshot, mitgliederById);
                    var nachherName = ResolveArbeitsstundenSnapshotMitgliedName(nachherSnapshot, mitgliederById);
                    mitgliederById.TryGetValue(record.GeprueftVon, out var pruefer);

                    return new ArbeitsstundenPruefverlaufItem
                    {
                        Id = record.Id,
                        ArbeitsstundeId = record.ArbeitsstundeId,
                        Aktion = record.Aktion,
                        Begruendung = record.Begruendung,
                        GeprueftVon = record.GeprueftVon,
                        GeprueftVonName = FormatMemberName(pruefer) ?? record.GeprueftVon.ToString(),
                        GeprueftAm = record.GeprueftAm,
                        VorherSnapshot = vorherSnapshot,
                        NachherSnapshot = nachherSnapshot,
                        VorherSummary = vorherSnapshot?.ToSummary(vorherName) ?? string.Empty,
                        NachherSummary = nachherSnapshot?.ToSummary(nachherName)
                    };
                }).ToList();
            },
            new List<ArbeitsstundenPruefverlaufItem>());

        public Task<bool> ApproveArbeitsstundeImPruefprozessAsync(int arbeitsstundeId, string begruendung, int geprueftVon, DateTime? geprueftAm = null) => ExecuteAsync(
            "ApproveArbeitsstundeImPruefprozessAsync",
            async () =>
            {
                var action = CreateArbeitsstundenPruefaktionRequest(arbeitsstundeId, ArbeitsstundenPruefprozess.AktionFreigegeben, begruendung, geprueftVon, geprueftAm);
                if (!IsValidArbeitsstundenPruefaktion(action))
                    return false;

                var client = await EnsureClientAsync();
                var existing = await GetOffeneArbeitsstundeImPruefprozessAsync(client, arbeitsstundeId);
                if (existing == null)
                    return false;

                var normalizedGeprueftAm = NormalizeArbeitsstundenPruefzeitpunkt(action.GeprueftAm);
                var updatedRecord = CloneArbeitsstundeForReview(existing);
                updatedRecord.Status = ArbeitsstundenPruefprozess.BuildFreigegebenStatus(action.Kommentar);
                updatedRecord.Freigegeben = true;
                updatedRecord.GenehmigtVon = action.GeprueftVon;
                updatedRecord.GenehmigtAm = normalizedGeprueftAm;
                updatedRecord.LockedByUserId = null;
                updatedRecord.LockedAt = null;

                await client
                    .From<ArbeitsstundeRecord>()
                    .Where(x => x.Id == arbeitsstundeId)
                    .Set(x => x.Status, updatedRecord.Status)
                    .Set(x => x.Freigegeben, updatedRecord.Freigegeben)
                    .Set(x => x.GenehmigtVon, updatedRecord.GenehmigtVon)
                    .Set(x => x.GenehmigtAm, updatedRecord.GenehmigtAm)
                    .Set(x => x.LockedByUserId, (string?)null)
                    .Set(x => x.LockedAt, (DateTime?)null)
                    .Update();

                await AppendArbeitsstundenPruefverlaufAsync(client, action, existing, updatedRecord, normalizedGeprueftAm);
                return true;
            },
            false);

        public Task<bool> RejectArbeitsstundeImPruefprozessAsync(int arbeitsstundeId, string begruendung, int geprueftVon, DateTime? geprueftAm = null) => ExecuteAsync(
            "RejectArbeitsstundeImPruefprozessAsync",
            async () =>
            {
                var action = CreateArbeitsstundenPruefaktionRequest(arbeitsstundeId, ArbeitsstundenPruefprozess.AktionAbgelehnt, begruendung, geprueftVon, geprueftAm);
                if (!IsValidArbeitsstundenPruefaktion(action))
                    return false;

                var client = await EnsureClientAsync();
                var existing = await GetOffeneArbeitsstundeImPruefprozessAsync(client, arbeitsstundeId);
                if (existing == null)
                    return false;

                var normalizedGeprueftAm = NormalizeArbeitsstundenPruefzeitpunkt(action.GeprueftAm);
                var updatedRecord = CloneArbeitsstundeForReview(existing);
                updatedRecord.Status = ArbeitsstundenPruefprozess.BuildAbgelehntStatus(action.Kommentar);
                updatedRecord.Freigegeben = false;
                updatedRecord.GenehmigtVon = null;
                updatedRecord.GenehmigtAm = null;
                updatedRecord.LockedByUserId = null;
                updatedRecord.LockedAt = null;

                await client
                    .From<ArbeitsstundeRecord>()
                    .Where(x => x.Id == arbeitsstundeId)
                    .Set(x => x.Status, updatedRecord.Status)
                    .Set(x => x.Freigegeben, updatedRecord.Freigegeben)
                    .Set(x => x.GenehmigtVon, (int?)null)
                    .Set(x => x.GenehmigtAm, (DateTime?)null)
                    .Set(x => x.LockedByUserId, (string?)null)
                    .Set(x => x.LockedAt, (DateTime?)null)
                    .Update();

                await AppendArbeitsstundenPruefverlaufAsync(client, action, existing, updatedRecord, normalizedGeprueftAm);
                return true;
            },
            false);

        public Task<bool> CorrectArbeitsstundeImPruefprozessAsync(ArbeitsstundenPruefkorrekturRequest request) => ExecuteAsync(
            "CorrectArbeitsstundeImPruefprozessAsync",
            async () =>
            {
                if (request == null || request.ArbeitsstundeId <= 0 || request.GeprueftVon <= 0 || request.Stunden <= 0 || string.IsNullOrWhiteSpace(request.ArtDerArbeit))
                    return false;

                var action = CreateArbeitsstundenPruefaktionRequest(
                    request.ArbeitsstundeId,
                    ArbeitsstundenPruefprozess.AktionKorrigiert,
                    request.Begruendung,
                    request.GeprueftVon,
                    request.GeprueftAm);

                if (!IsValidArbeitsstundenPruefaktion(action))
                    return false;

                var client = await EnsureClientAsync();
                var existing = await GetOffeneArbeitsstundeImPruefprozessAsync(client, request.ArbeitsstundeId);
                if (existing == null)
                    return false;

                var normalizedGeprueftAm = NormalizeArbeitsstundenPruefzeitpunkt(action.GeprueftAm);
                var updatedRecord = CloneArbeitsstundeForReview(existing);
                updatedRecord.Datum = NormalizeDateOnly(request.Datum);
                updatedRecord.Stunden = request.Stunden;
                updatedRecord.ArtDerArbeit = CleanRequiredText(request.ArtDerArbeit);
                updatedRecord.Status = ArbeitsstundenPruefprozess.BuildKorrigiertStatus(action.Kommentar);
                updatedRecord.Freigegeben = true;
                updatedRecord.GenehmigtVon = action.GeprueftVon;
                updatedRecord.GenehmigtAm = normalizedGeprueftAm;
                updatedRecord.LockedByUserId = null;
                updatedRecord.LockedAt = null;

                await client
                    .From<ArbeitsstundeRecord>()
                    .Where(x => x.Id == request.ArbeitsstundeId)
                    .Set(x => x.Datum, updatedRecord.Datum)
                    .Set(x => x.Stunden, updatedRecord.Stunden)
                    .Set(x => x.ArtDerArbeit, updatedRecord.ArtDerArbeit)
                    .Set(x => x.Status, updatedRecord.Status)
                    .Set(x => x.Freigegeben, updatedRecord.Freigegeben)
                    .Set(x => x.GenehmigtVon, updatedRecord.GenehmigtVon)
                    .Set(x => x.GenehmigtAm, updatedRecord.GenehmigtAm)
                    .Set(x => x.LockedByUserId, (string?)null)
                    .Set(x => x.LockedAt, (DateTime?)null)
                    .Update();

                await AppendArbeitsstundenPruefverlaufAsync(client, action, existing, updatedRecord, normalizedGeprueftAm);
                return true;
            },
            false);

        public Task<bool> DeleteArbeitsstundeImPruefprozessAsync(int arbeitsstundeId, string begruendung, int geprueftVon, DateTime? geprueftAm = null) => ExecuteAsync(
            "DeleteArbeitsstundeImPruefprozessAsync",
            async () =>
            {
                var action = CreateArbeitsstundenPruefaktionRequest(arbeitsstundeId, ArbeitsstundenPruefprozess.AktionGeloescht, begruendung, geprueftVon, geprueftAm);
                if (!IsValidArbeitsstundenPruefaktion(action))
                    return false;

                var client = await EnsureClientAsync();
                var existing = await GetOffeneArbeitsstundeImPruefprozessAsync(client, arbeitsstundeId);
                if (existing == null)
                    return false;

                var normalizedGeprueftAm = NormalizeArbeitsstundenPruefzeitpunkt(action.GeprueftAm);

                await client
                    .From<ArbeitsstundeRecord>()
                    .Where(x => x.Id == arbeitsstundeId)
                    .Delete();

                await AppendArbeitsstundenPruefverlaufAsync(client, action, existing, null, normalizedGeprueftAm);
                return true;
            },
            false);

        private ArbeitsstundenPruefSnapshot? DeserializeArbeitsstundenPruefSnapshot(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<ArbeitsstundenPruefSnapshot>(json);
            }
            catch (JsonException ex)
            {
                _logger?.LogWarning(ex, "Arbeitsstunden-Prüfsnapshot konnte nicht deserialisiert werden.");
                return null;
            }
        }

        private string? ResolveArbeitsstundenSnapshotMitgliedName(ArbeitsstundenPruefSnapshot? snapshot, IReadOnlyDictionary<int, MitgliedRecord> mitgliederById)
        {
            if (snapshot == null || snapshot.MitgliedId <= 0)
                return null;

            return mitgliederById.TryGetValue(snapshot.MitgliedId, out var mitglied)
                ? FormatMemberName(mitglied)
                : $"Mitglied {snapshot.MitgliedId}";
        }

        private static ArbeitsstundenPruefaktionRequest CreateArbeitsstundenPruefaktionRequest(int arbeitsstundeId, string aktion, string? kommentar, int geprueftVon, DateTime? geprueftAm)
        {
            return new ArbeitsstundenPruefaktionRequest
            {
                ArbeitsstundeId = arbeitsstundeId,
                Aktion = string.IsNullOrWhiteSpace(aktion) ? string.Empty : aktion.Trim(),
                Kommentar = ArbeitsstundenPruefprozess.NormalizeKommentar(kommentar),
                GeprueftVon = geprueftVon,
                GeprueftAm = geprueftAm
            };
        }

        private static bool IsValidArbeitsstundenPruefaktion(ArbeitsstundenPruefaktionRequest? action)
        {
            if (action == null || action.ArbeitsstundeId <= 0 || action.GeprueftVon <= 0)
                return false;

            var isKnownAction = string.Equals(action.Aktion, ArbeitsstundenPruefprozess.AktionFreigegeben, StringComparison.Ordinal)
                || string.Equals(action.Aktion, ArbeitsstundenPruefprozess.AktionAbgelehnt, StringComparison.Ordinal)
                || string.Equals(action.Aktion, ArbeitsstundenPruefprozess.AktionKorrigiert, StringComparison.Ordinal)
                || string.Equals(action.Aktion, ArbeitsstundenPruefprozess.AktionGeloescht, StringComparison.Ordinal);

            return isKnownAction
                && ArbeitsstundenPruefprozess.HasRequiredKommentar(action.Kommentar);
        }

        private async Task<ArbeitsstundeRecord?> GetOffeneArbeitsstundeImPruefprozessAsync(Client client, int arbeitsstundeId)
        {
            if (arbeitsstundeId <= 0)
                return null;

            var response = await client
                .From<ArbeitsstundeRecord>()
                .Where(x => x.Id == arbeitsstundeId)
                .Get();

            var existing = response?.Models?.FirstOrDefault();
            return existing != null && ArbeitsstundenPruefprozess.IsOffenerPrueffall(existing.Status, existing.Freigegeben)
                ? existing
                : null;
        }

        private static DateTime NormalizeArbeitsstundenPruefzeitpunkt(DateTime? value)
        {
            var timestamp = value ?? DateTime.UtcNow;
            return NormalizeTimestampWithoutTimeZone(timestamp) ?? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        }

        private static ArbeitsstundeRecord CloneArbeitsstundeForReview(ArbeitsstundeRecord source)
        {
            return new ArbeitsstundeRecord
            {
                Id = source.Id,
                MitgliedId = source.MitgliedId,
                SaisonId = source.SaisonId,
                Datum = NormalizeDateOnly(source.Datum),
                Stunden = source.Stunden,
                ArtDerArbeit = source.ArtDerArbeit,
                Status = source.Status,
                Freigegeben = source.Freigegeben,
                GenehmigtVon = source.GenehmigtVon,
                GenehmigtAm = source.GenehmigtAm,
                LockedByUserId = source.LockedByUserId,
                LockedAt = source.LockedAt
            };
        }

        private async Task AppendArbeitsstundenPruefverlaufAsync(Client client, ArbeitsstundenPruefaktionRequest action, ArbeitsstundeRecord vorher, ArbeitsstundeRecord? nachher, DateTime geprueftAm)
        {
            if (!IsValidArbeitsstundenPruefaktion(action))
                return;

            var verlaufRecord = new ArbeitsstundenPruefverlaufRecord
            {
                ArbeitsstundeId = action.ArbeitsstundeId,
                Aktion = action.Aktion,
                Begruendung = action.Kommentar,
                GeprueftVon = action.GeprueftVon,
                GeprueftAm = geprueftAm,
                VorherSnapshot = JsonSerializer.Serialize(ArbeitsstundenPruefSnapshot.FromRecord(vorher)),
                NachherSnapshot = nachher == null
                    ? null
                    : JsonSerializer.Serialize(ArbeitsstundenPruefSnapshot.FromRecord(nachher))
            };

            await client.From<ArbeitsstundenPruefverlaufRecord>().Insert(verlaufRecord);
        }

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

        public async Task<DokumentUploadResult> CreateMitgliedsantragDokumentAsync(int mitgliedId, string status = FormularDokumentStatus.Unsigniert)
        {
            if (mitgliedId <= 0)
                return DokumentUploadResult.Fail("Bitte zuerst ein gültiges Mitglied auswählen.", "VALIDATION");

            try
            {
                var member = await GetMitgliedByIdAsync(mitgliedId);
                if (member == null)
                    return DokumentUploadResult.Fail("Mitglied konnte nicht geladen werden.", "NOT_FOUND");

                if (!OperationalDataFilter.IsOperationalMember(member))
                    return DokumentUploadResult.Fail("Für dieses Mitglied kann aktuell kein Antrag erzeugt werden.", "NOT_OPERATIONAL");

                var saisons = await GetSaisonRecordsAsync();
                var vorschlag = MitgliedsantragBeitragHelper.CreateSuggestion(member, saisons);
                return await CreateMitgliedsantragDokumentInternalAsync(
                    member,
                    MitgliedsantragBeitragHelper.NormalizeBeitrag(vorschlag.VorgeschlagenerBeitrag),
                    vorschlag.BeginnDatum,
                    status);
            }
            catch (InvalidOperationException ex)
            {
                _logger?.LogWarning(ex, "CreateMitgliedsantragDokumentAsync validation failed for MitgliedId={MitgliedId}", mitgliedId);
                return DokumentUploadResult.Fail(ex.Message, "VALIDATION");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "CreateMitgliedsantragDokumentAsync failed for MitgliedId={MitgliedId}", mitgliedId);
                return DokumentUploadResult.Fail("Mitgliedsantrag konnte aktuell nicht erzeugt werden.", "UNEXPECTED");
            }
        }

        public async Task<DokumentUploadResult> CreateMitgliedsantragDokumentAsync(MitgliedsantragDokumentRequest request)
        {
            if (request == null || request.MitgliedId <= 0)
                return DokumentUploadResult.Fail("Bitte zuerst ein gültiges Mitglied auswählen.", "VALIDATION");

            try
            {
                var member = await GetMitgliedByIdAsync(request.MitgliedId);
                if (member == null)
                    return DokumentUploadResult.Fail("Mitglied konnte nicht geladen werden.", "NOT_FOUND");

                if (!OperationalDataFilter.IsOperationalMember(member))
                    return DokumentUploadResult.Fail("Für dieses Mitglied kann aktuell kein Antrag erzeugt werden.", "NOT_OPERATIONAL");

                var beginnDatum = request.BeginnDatum == default
                    ? (member.MitgliedSeit ?? DateTime.Today)
                    : request.BeginnDatum;
                var mitgliedsbeitrag = MitgliedsantragBeitragHelper.NormalizeBeitrag(request.Mitgliedsbeitrag);
                return await CreateMitgliedsantragDokumentInternalAsync(member, mitgliedsbeitrag, beginnDatum, request.Status);
            }
            catch (InvalidOperationException ex)
            {
                _logger?.LogWarning(ex, "CreateMitgliedsantragDokumentAsync(request) validation failed for MitgliedId={MitgliedId}", request?.MitgliedId);
                return DokumentUploadResult.Fail(ex.Message, "VALIDATION");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "CreateMitgliedsantragDokumentAsync(request) failed for MitgliedId={MitgliedId}", request?.MitgliedId);
                return DokumentUploadResult.Fail("Mitgliedsantrag konnte aktuell nicht erzeugt werden.", "UNEXPECTED");
            }
        }

        public Task<DokumentUploadRequest?> BuildMitgliedsantragPreviewAsync(MitgliedsantragDokumentRequest request) => ExecuteAsync<DokumentUploadRequest?>(
            "BuildMitgliedsantragPreviewAsync",
            async () =>
            {
                var context = await ResolveMitgliedsantragRequestAsync(request);
                return MitgliedsantragDokumentFactory.CreateUploadRequest(context.Member, context.Mitgliedsbeitrag, context.Aufnahmegebuehr, context.BeginnDatum, context.GesetzlicherVertreterSnapshot, context.BankverbindungSnapshot, FormularDokumentStatus.Unsigniert);
            },
            null);

        public Task<DokumentUploadResult> CreateSignedMitgliedsantragDokumentAsync(MitgliedsantragDokumentRequest request, DigitalSignatureCapture signatureCapture, DigitalSignatureCapture? gesetzlicherVertreterSignatureCapture = null) => ExecuteAsync(
            "CreateSignedMitgliedsantragDokumentAsync",
            async () =>
            {
                if (signatureCapture == null || !signatureCapture.HasContent)
                    return DokumentUploadResult.Fail("Bitte zuerst eine digitale Signatur erfassen.", "VALIDATION");

                var context = await ResolveMitgliedsantragRequestAsync(request);
                if (context.IstMinderjaehrig && (gesetzlicherVertreterSignatureCapture == null || !gesetzlicherVertreterSignatureCapture.HasContent))
                    return DokumentUploadResult.Fail("Für Minderjährige ist zusätzlich die digitale Unterschrift des gesetzlichen Vertreters erforderlich.", "VALIDATION");

                if (context.IstMinderjaehrig)
                {
                    var vertreterMitgliedId = await EnsureGesetzlicherVertreterMitgliedAsync(context, request);
                    var savedRelation = await SaveGesetzlichenVertreterAsync(new GesetzlicherVertreterSaveRequest
                    {
                        MinderjaehrigesMitgliedId = context.Member.Id,
                        VertreterMitgliedId = vertreterMitgliedId,
                        GueltigAb = context.BeginnDatum,
                        Bemerkung = "Automatisch aus Mitgliedsantrag übernommen."
                    });

                    if (savedRelation == null)
                        return DokumentUploadResult.Fail("Gesetzlicher Vertreter konnte nicht gespeichert werden.", "VALIDATION");
                }

                var previewUploadRequest = MitgliedsantragDokumentFactory.CreateUploadRequest(context.Member, context.Mitgliedsbeitrag, context.Aufnahmegebuehr, context.BeginnDatum, context.GesetzlicherVertreterSnapshot, context.BankverbindungSnapshot, FormularDokumentStatus.Unsigniert);
                var finalUploadRequest = MitgliedsantragDokumentFactory.CreateUploadRequest(context.Member, context.Mitgliedsbeitrag, context.Aufnahmegebuehr, context.BeginnDatum, context.GesetzlicherVertreterSnapshot, context.BankverbindungSnapshot, FormularDokumentStatus.Signiert);
                var sourceDocument = CreatePreviewDocumentInfo(previewUploadRequest, FormularDokumentTyp.Mitgliedsantrag, FormularDokumentStatus.Unsigniert);
                finalUploadRequest.FileContent = SignedVertragsdokumentPdfBuilder.Build(
                    context.Member,
                    sourceDocument,
                    previewUploadRequest.FileContent,
                    signatureCapture,
                    gesetzlicherVertreterSignatureCapture,
                    "Unterschrift Antragsteller/in",
                    context.IstMinderjaehrig ? "Unterschrift gesetzliche/r Vertreter/in" : null);
                return await CreateDokumentAsync(finalUploadRequest);
            },
            DokumentUploadResult.Fail("Mitgliedsantrag konnte aktuell nicht signiert gespeichert werden.", "UNEXPECTED"));

        private async Task<DokumentUploadResult> CreateMitgliedsantragDokumentInternalAsync(MitgliedRecord member, decimal mitgliedsbeitrag, DateTime beginnDatum, string? status)
        {
            var gesetzlicherVertreter = await ResolveGesetzlicherVertreterAsync(member.Id, beginnDatum);
            var bankverbindungSnapshot = await ResolveVereinsBankverbindungSnapshotAsync();
            var aufnahmegebuehr = await ResolveMitgliedsantragAufnahmegebuehrAsync(beginnDatum);
            var uploadRequest = MitgliedsantragDokumentFactory.CreateUploadRequest(member, mitgliedsbeitrag, aufnahmegebuehr, beginnDatum, gesetzlicherVertreter.IstMinderjaehrig ? BuildMitgliedsantragVertreterSnapshot(gesetzlicherVertreter.Vorbelegung) : null, bankverbindungSnapshot, status);
            return await CreateDokumentAsync(uploadRequest);
        }

        private async Task<(MitgliedRecord Member, decimal Mitgliedsbeitrag, decimal Aufnahmegebuehr, DateTime BeginnDatum, bool IstMinderjaehrig, MitgliedsantragVertreterSnapshot? GesetzlicherVertreterSnapshot, int? GesetzlicherVertreterMitgliedId, MitgliedsantragBankverbindungSnapshot BankverbindungSnapshot)> ResolveMitgliedsantragRequestAsync(MitgliedsantragDokumentRequest request)
        {
            if (request == null || request.MitgliedId <= 0)
                throw new InvalidOperationException("Bitte zuerst ein gültiges Mitglied auswählen.");

            var member = await GetMitgliedByIdAsync(request.MitgliedId);
            if (member == null)
                throw new InvalidOperationException("Mitglied konnte nicht geladen werden.");

            if (!OperationalDataFilter.IsOperationalMember(member))
                throw new InvalidOperationException("Für dieses Mitglied kann aktuell kein Antrag erzeugt werden.");

            var beginnDatum = request.BeginnDatum == default
                ? (member.MitgliedSeit ?? DateTime.Today)
                : request.BeginnDatum;
            var bankverbindungSnapshot = request.BankverbindungSnapshot != null
                ? NormalizeVereinsBankverbindungSnapshot(request.BankverbindungSnapshot)
                : await ResolveVereinsBankverbindungSnapshotAsync();
            request.BankverbindungSnapshot = bankverbindungSnapshot;
            var aufnahmegebuehr = request.Aufnahmegebuehr.HasValue
                ? MitgliedsantragBeitragHelper.NormalizeBeitrag(request.Aufnahmegebuehr.Value)
                : await ResolveMitgliedsantragAufnahmegebuehrAsync(beginnDatum);
            request.Aufnahmegebuehr = aufnahmegebuehr;

            var istMinderjaehrig = GesetzlicherVertreterResolver.IsMinderjaehrig(member, beginnDatum);
            if (!istMinderjaehrig)
                return (member, MitgliedsantragBeitragHelper.NormalizeBeitrag(request.Mitgliedsbeitrag), aufnahmegebuehr, beginnDatum, false, null, null, bankverbindungSnapshot);

            MitgliedsantragVertreterSnapshot? vertreterSnapshot = null;
            int? vertreterMitgliedId = null;

            if (request.GesetzlicherVertreterAusBestehendemMitglied && request.GesetzlicherVertreterMitgliedId is > 0)
            {
                var vertreterMitglied = await GetMitgliedByIdAsync(request.GesetzlicherVertreterMitgliedId.Value);
                if (vertreterMitglied == null)
                    throw new InvalidOperationException("Gesetzlicher Vertreter konnte nicht geladen werden.");
                if (vertreterMitglied.Id == member.Id)
                    throw new InvalidOperationException("Das aufzunehmende Mitglied kann nicht gleichzeitig eigener gesetzlicher Vertreter sein.");

                vertreterSnapshot = BuildMitgliedsantragVertreterSnapshot(vertreterMitglied);
                vertreterMitgliedId = vertreterMitglied.Id;
            }
            else if (request.GesetzlicherVertreterSnapshot != null)
            {
                vertreterSnapshot = NormalizeMitgliedsantragVertreterSnapshot(request.GesetzlicherVertreterSnapshot, member, request.GesetzlicherVertreterAdresseAbweichend);
            }
            else
            {
                var aufloesung = await ResolveGesetzlicherVertreterAsync(member.Id, beginnDatum);
                if (aufloesung.HatAktivenGesetzlichenVertreter)
                {
                    vertreterSnapshot = BuildMitgliedsantragVertreterSnapshot(aufloesung.Vorbelegung);
                    vertreterMitgliedId = aufloesung.VertreterMitglied?.Id;
                }
            }

            if (vertreterSnapshot == null || string.IsNullOrWhiteSpace(vertreterSnapshot.Vorname) || string.IsNullOrWhiteSpace(vertreterSnapshot.Nachname))
                throw new InvalidOperationException("Für Minderjährige ist ein gesetzlicher Vertreter mit Vor- und Nachname erforderlich.");

            return (member, MitgliedsantragBeitragHelper.NormalizeBeitrag(request.Mitgliedsbeitrag), aufnahmegebuehr, beginnDatum, true, vertreterSnapshot, vertreterMitgliedId, bankverbindungSnapshot);
        }

        private async Task<int> EnsureGesetzlicherVertreterMitgliedAsync((MitgliedRecord Member, decimal Mitgliedsbeitrag, decimal Aufnahmegebuehr, DateTime BeginnDatum, bool IstMinderjaehrig, MitgliedsantragVertreterSnapshot? GesetzlicherVertreterSnapshot, int? GesetzlicherVertreterMitgliedId, MitgliedsantragBankverbindungSnapshot BankverbindungSnapshot) context, MitgliedsantragDokumentRequest request)
        {
            if (context.GesetzlicherVertreterMitgliedId is > 0)
                return context.GesetzlicherVertreterMitgliedId.Value;

            var snapshot = context.GesetzlicherVertreterSnapshot;
            if (snapshot == null)
                throw new InvalidOperationException("Gesetzlicher Vertreter konnte nicht vorbereitet werden.");

            return await EnsureGesetzlicherVertreterMitgliedInternalAsync(context.Member, context.BeginnDatum, snapshot, request.GesetzlicherVertreterAdresseAbweichend);
        }

        private async Task<int> EnsureGesetzlicherVertreterMitgliedAsync((MitgliedRecord Member, MitgliedRecord? SecondaryMember, ParzelleRecord Parzelle, SaisonRecord Saison, DateTime Vertragsbeginn, bool IstMinderjaehrig, MitgliedsantragVertreterSnapshot? GesetzlicherVertreterSnapshot, int? GesetzlicherVertreterMitgliedId, MitgliedsantragBankverbindungSnapshot BankverbindungSnapshot) context, PachtvertragDokumentRequest request)
        {
            if (context.GesetzlicherVertreterMitgliedId is > 0)
                return context.GesetzlicherVertreterMitgliedId.Value;

            var snapshot = context.GesetzlicherVertreterSnapshot;
            if (snapshot == null)
                throw new InvalidOperationException("Gesetzlicher Vertreter konnte nicht vorbereitet werden.");

            return await EnsureGesetzlicherVertreterMitgliedInternalAsync(context.Member, context.Vertragsbeginn, snapshot, request.GesetzlicherVertreterAdresseAbweichend);
        }

        private async Task<int> EnsureGesetzlicherVertreterMitgliedInternalAsync(MitgliedRecord member, DateTime effectiveDate, MitgliedsantragVertreterSnapshot snapshot, bool adresseAbweichend)
        {
            var parentMitgliedId = member.HauptmitgliedId is > 0
                ? member.HauptmitgliedId.Value
                : member.Id;
            var created = await CreateNebenmitgliedAsync(new NebenmitgliedCreateDTO
            {
                HauptmitgliedId = parentMitgliedId,
                Vorname = snapshot.Vorname,
                Nachname = snapshot.Nachname,
                AdresseUebernehmen = !adresseAbweichend,
                Telefon = string.IsNullOrWhiteSpace(snapshot.Telefon) ? null : snapshot.Telefon,
                Handy = string.IsNullOrWhiteSpace(snapshot.Handy) ? null : snapshot.Handy,
                Adresse = string.IsNullOrWhiteSpace(snapshot.Adresse) ? null : snapshot.Adresse,
                Plz = string.IsNullOrWhiteSpace(snapshot.Plz) ? null : snapshot.Plz,
                Ort = string.IsNullOrWhiteSpace(snapshot.Ort) ? null : snapshot.Ort,
                Email = string.IsNullOrWhiteSpace(snapshot.Email) ? null : snapshot.Email,
                MitgliedSeit = effectiveDate,
                WhatsappEinwilligung = false
            });

            if (created == null)
                throw new InvalidOperationException("Gesetzlicher Vertreter konnte nicht als Nebenmitglied angelegt werden.");

            return created.Id;
        }

        private async Task<MitgliedsantragBankverbindungSnapshot> ResolveVereinsBankverbindungSnapshotAsync()
        {
            var vereinskonfiguration = await GetAktiveVereinskonfigurationAsync();
            if (vereinskonfiguration == null)
                throw new InvalidOperationException("Es ist keine aktive Vereinskonfiguration für den Mitgliedsantrag hinterlegt.");

            return new MitgliedsantragBankverbindungSnapshot
            {
                VereinName = CleanRequiredText(string.IsNullOrWhiteSpace(vereinskonfiguration.Vereinsname) ? vereinskonfiguration.Kurzname : vereinskonfiguration.Vereinsname),
                VereinRegisterangabe = CleanRequiredText(vereinskonfiguration.Registerangabe),
                VereinEmail = CleanRequiredText(vereinskonfiguration.StandardEmail),
                Kontoinhaber = CleanRequiredText(vereinskonfiguration.Kontoinhaber),
                Bankname = CleanRequiredText(vereinskonfiguration.Bankname),
                Iban = CleanRequiredText(vereinskonfiguration.Iban),
                Bic = CleanRequiredText(vereinskonfiguration.Bic),
                VerwendungszweckMitgliedsantrag = CleanRequiredText(vereinskonfiguration.VerwendungszweckMitgliedsantrag),
                DokumentOrt = CleanRequiredText(vereinskonfiguration.DokumentOrt),
                StandardHinweistext = CleanRequiredText(vereinskonfiguration.StandardHinweistext),
                DatenschutzText = CleanRequiredText(vereinskonfiguration.DatenschutzText),
                DatenschutzVersion = CleanRequiredText(vereinskonfiguration.DatenschutzVersion),
                DatenschutzStand = vereinskonfiguration.DatenschutzStand?.Date
            };
        }

        private static MitgliedsantragBankverbindungSnapshot NormalizeVereinsBankverbindungSnapshot(MitgliedsantragBankverbindungSnapshot snapshot)
        {
            if (snapshot == null)
                throw new InvalidOperationException("Es ist keine aktive Vereinskonfiguration für den Mitgliedsantrag hinterlegt.");

            return new MitgliedsantragBankverbindungSnapshot
            {
                VereinName = CleanRequiredText(snapshot.VereinName),
                VereinRegisterangabe = CleanRequiredText(snapshot.VereinRegisterangabe),
                VereinEmail = CleanRequiredText(snapshot.VereinEmail),
                Kontoinhaber = CleanRequiredText(snapshot.Kontoinhaber),
                Bankname = CleanRequiredText(snapshot.Bankname),
                Iban = CleanRequiredText(snapshot.Iban),
                Bic = CleanRequiredText(snapshot.Bic),
                VerwendungszweckMitgliedsantrag = CleanRequiredText(snapshot.VerwendungszweckMitgliedsantrag),
                DokumentOrt = CleanRequiredText(snapshot.DokumentOrt),
                StandardHinweistext = CleanRequiredText(snapshot.StandardHinweistext),
                DatenschutzText = CleanRequiredText(snapshot.DatenschutzText),
                DatenschutzVersion = CleanRequiredText(snapshot.DatenschutzVersion),
                DatenschutzStand = snapshot.DatenschutzStand?.Date
            };
        }

        private async Task<decimal> ResolveMitgliedsantragAufnahmegebuehrAsync(DateTime beginnDatum)
        {
            var saisonJahr = beginnDatum.Date.Year;
            var saisonen = await GetSaisonRecordsAsync();
            var saison = saisonen.FirstOrDefault(x => SaisonverwaltungHelper.GetSaisonJahr(x) == saisonJahr);
            if (saison == null)
                throw new InvalidOperationException($"Für die Saison {saisonJahr} fehlt die Aufnahmegebühr.");
            if (!saison.Aufnahmegebuehr.HasValue)
                throw new InvalidOperationException($"Für die Saison {saisonJahr} fehlt die Aufnahmegebühr.");
            if (saison.Aufnahmegebuehr.Value < 0m)
                throw new InvalidOperationException($"Für die Saison {saisonJahr} ist die Aufnahmegebühr ungültig.");

            return MitgliedsantragBeitragHelper.NormalizeBeitrag(saison.Aufnahmegebuehr.Value);
        }

        private static MitgliedsantragVertreterSnapshot? BuildMitgliedsantragVertreterSnapshot(MitgliedRecord? member)
            => member == null ? null : new MitgliedsantragVertreterSnapshot
            {
                VertreterMitgliedId = member.Id,
                Vorname = member.Vorname?.Trim() ?? string.Empty,
                Nachname = member.Name?.Trim() ?? string.Empty,
                Adresse = member.Adresse?.Trim() ?? string.Empty,
                Plz = member.Plz?.Trim() ?? string.Empty,
                Ort = member.Ort?.Trim() ?? string.Empty,
                Telefon = member.Telefon?.Trim() ?? string.Empty,
                Handy = member.Handy?.Trim() ?? string.Empty,
                Email = member.Email?.Trim() ?? string.Empty
            };

        private static MitgliedsantragVertreterSnapshot? BuildMitgliedsantragVertreterSnapshot(GesetzlicherVertreterVorbelegung? vorbelegung)
            => vorbelegung == null ? null : new MitgliedsantragVertreterSnapshot
            {
                VertreterMitgliedId = vorbelegung.VertreterMitgliedId > 0 ? vorbelegung.VertreterMitgliedId : null,
                Vorname = vorbelegung.Vorname,
                Nachname = vorbelegung.Nachname,
                Adresse = vorbelegung.Adresse,
                Plz = vorbelegung.Plz,
                Ort = vorbelegung.Ort,
                Telefon = vorbelegung.Telefon,
                Handy = vorbelegung.Handy,
                Email = vorbelegung.Email
            };

        private static MitgliedsantragVertreterSnapshot NormalizeMitgliedsantragVertreterSnapshot(MitgliedsantragVertreterSnapshot snapshot, MitgliedRecord member, bool adresseAbweichend)
        {
            return new MitgliedsantragVertreterSnapshot
            {
                VertreterMitgliedId = snapshot.VertreterMitgliedId,
                Vorname = CleanRequiredText(snapshot.Vorname),
                Nachname = CleanRequiredText(snapshot.Nachname),
                Adresse = adresseAbweichend ? CleanOptionalText(snapshot.Adresse) ?? string.Empty : member.Adresse?.Trim() ?? string.Empty,
                Plz = adresseAbweichend ? CleanOptionalText(snapshot.Plz) ?? string.Empty : member.Plz?.Trim() ?? string.Empty,
                Ort = adresseAbweichend ? CleanOptionalText(snapshot.Ort) ?? string.Empty : member.Ort?.Trim() ?? string.Empty,
                Telefon = CleanOptionalText(snapshot.Telefon) ?? string.Empty,
                Handy = CleanOptionalText(snapshot.Handy) ?? string.Empty,
                Email = CleanOptionalText(snapshot.Email) ?? string.Empty
            };
        }

        private static DocumentInfo CreatePreviewDocumentInfo(DokumentUploadRequest uploadRequest, string dokumenttyp, string status)
        {
            return new DocumentInfo
            {
                Title = uploadRequest.Titel,
                Dateiname = uploadRequest.FileName,
                Name = uploadRequest.FileName,
                MimeType = uploadRequest.MimeType,
                StoragePath = uploadRequest.FileName,
                Bucket = string.Empty,
                DriveFileId = string.Empty
            };
        }

        public async Task<DokumentUploadResult> CreateMitgliedsvertragDokumentAsync(int mitgliedId, string status = FormularDokumentStatus.Unsigniert)
        {
            if (mitgliedId <= 0)
                return DokumentUploadResult.Fail("Bitte zuerst ein gültiges Mitglied auswählen.", "VALIDATION");

            try
            {
                var member = await GetMitgliedByIdAsync(mitgliedId);
                if (member == null)
                    return DokumentUploadResult.Fail("Mitglied konnte nicht geladen werden.", "NOT_FOUND");

                if (!OperationalDataFilter.IsOperationalMember(member))
                    return DokumentUploadResult.Fail("Für dieses Mitglied kann aktuell kein Vertrag erzeugt werden.", "NOT_OPERATIONAL");

                var uploadRequest = MitgliedsvertragDokumentFactory.CreateUploadRequest(member, status);
                return await CreateDokumentAsync(uploadRequest);
            }
            catch (InvalidOperationException ex)
            {
                _logger?.LogWarning(ex, "CreateMitgliedsvertragDokumentAsync validation failed for MitgliedId={MitgliedId}", mitgliedId);
                return DokumentUploadResult.Fail(ex.Message, "VALIDATION");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "CreateMitgliedsvertragDokumentAsync failed for MitgliedId={MitgliedId}", mitgliedId);
                return DokumentUploadResult.Fail("Mitgliedsvertrag konnte aktuell nicht erzeugt werden.", "UNEXPECTED");
            }
        }

        public async Task<DokumentUploadResult> CreatePachtvertragDokumentAsync(int mitgliedId, int parzelleId, DateTime vertragsbeginn, string status = FormularDokumentStatus.Unsigniert)
        {
            if (mitgliedId <= 0)
                return DokumentUploadResult.Fail("Bitte zuerst ein gültiges Mitglied auswählen.", "VALIDATION");

            if (parzelleId <= 0)
                return DokumentUploadResult.Fail("Bitte zuerst eine gültige Parzelle auswählen.", "VALIDATION");

            var vertragsbeginnDatum = vertragsbeginn.Date;
            if (vertragsbeginnDatum == DateTime.MinValue)
                return DokumentUploadResult.Fail("Bitte ein gültiges Vertragsbeginn-Datum angeben.", "VALIDATION");

            try
            {
                var member = await GetMitgliedByIdAsync(mitgliedId);
                if (member == null)
                    return DokumentUploadResult.Fail("Mitglied konnte nicht geladen werden.", "NOT_FOUND");

                if (!OperationalDataFilter.IsOperationalMember(member))
                    return DokumentUploadResult.Fail("Für dieses Mitglied kann aktuell kein Pachtvertrag erzeugt werden.", "NOT_OPERATIONAL");

                if (member.HauptmitgliedId.HasValue && member.HauptmitgliedId.Value > 0)
                    return DokumentUploadResult.Fail("Pachtvertrag kann nur aus dem Hauptmitglied-Kontext erzeugt werden.", "NOT_MAIN_MEMBER");

                var client = await EnsureClientAsync();
                var parzelle = await LoadParzelleByIdAsync(client, parzelleId);
                if (parzelle == null)
                    return DokumentUploadResult.Fail("Parzelle konnte nicht geladen werden.", "PARCEL_NOT_FOUND");

                var saison = (await GetSaisonRecordsAsync())
                    .FirstOrDefault(x => x.Jahr == vertragsbeginnDatum.Year);
                if (saison == null)
                    return DokumentUploadResult.Fail($"Für das Vertragsjahr {vertragsbeginnDatum.Year} ist keine Saison hinterlegt.", "SAISON_NOT_FOUND");

                if (!saison.PachtProQm.HasValue)
                    return DokumentUploadResult.Fail($"Für die Saison {saison.Jahr} fehlt pacht_pro_qm.", "Pacht_PRO_QM_MISSING");

                var context = await ResolvePachtvertragRequestAsync(new PachtvertragDokumentRequest
                {
                    MitgliedId = mitgliedId,
                    ParzelleId = parzelleId,
                    Vertragsbeginn = vertragsbeginnDatum,
                    Status = status
                });
                var uploadRequest = PachtvertragDokumentFactory.CreateUploadRequest(context.Member, context.SecondaryMember, context.Parzelle, context.Saison, context.Vertragsbeginn, altvertragDatum: null, context.GesetzlicherVertreterSnapshot, context.BankverbindungSnapshot, status);
                return await CreateDokumentAsync(uploadRequest);
            }
            catch (InvalidOperationException ex)
            {
                _logger?.LogWarning(ex, "CreatePachtvertragDokumentAsync validation failed for MitgliedId={MitgliedId}, ParzelleId={ParzelleId}", mitgliedId, parzelleId);
                return DokumentUploadResult.Fail(ex.Message, "VALIDATION");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "CreatePachtvertragDokumentAsync failed for MitgliedId={MitgliedId}, ParzelleId={ParzelleId}", mitgliedId, parzelleId);
                return DokumentUploadResult.Fail("Pachtvertrag konnte aktuell nicht erzeugt werden.", "UNEXPECTED");
            }
        }

        public Task<DokumentUploadRequest?> BuildPachtvertragPreviewAsync(int mitgliedId, int parzelleId, DateTime vertragsbeginn) => ExecuteAsync<DokumentUploadRequest?>(
            "BuildPachtvertragPreviewAsync",
            async () =>
            {
                var context = await ResolvePachtvertragRequestAsync(new PachtvertragDokumentRequest
                {
                    MitgliedId = mitgliedId,
                    ParzelleId = parzelleId,
                    Vertragsbeginn = vertragsbeginn,
                    Status = FormularDokumentStatus.Unsigniert
                });
                return PachtvertragDokumentFactory.CreateUploadRequest(
                    context.Member,
                    context.SecondaryMember,
                    context.Parzelle,
                    context.Saison,
                    context.Vertragsbeginn,
                    altvertragDatum: null,
                    context.GesetzlicherVertreterSnapshot,
                    context.BankverbindungSnapshot,
                    FormularDokumentStatus.Unsigniert);
            },
            null);

        public Task<DokumentUploadRequest?> BuildPachtvertragPreviewAsync(PachtvertragDokumentRequest request) => ExecuteAsync<DokumentUploadRequest?>(
            "BuildPachtvertragPreviewAsync(request)",
            async () =>
            {
                var context = await ResolvePachtvertragRequestAsync(request);
                return PachtvertragDokumentFactory.CreateUploadRequest(
                    context.Member,
                    context.SecondaryMember,
                    context.Parzelle,
                    context.Saison,
                    context.Vertragsbeginn,
                    altvertragDatum: request.AltvertragDatum,
                    context.GesetzlicherVertreterSnapshot,
                    context.BankverbindungSnapshot,
                    FormularDokumentStatus.Unsigniert);
            },
            null);

        public Task<DokumentUploadResult> CreateSignedPachtvertragDokumentAsync(int mitgliedId, int parzelleId, DateTime vertragsbeginn, DigitalSignatureCapture signatureCapture) => ExecuteAsync(
            "CreateSignedPachtvertragDokumentAsync",
            async () =>
            {
                if (signatureCapture == null || !signatureCapture.HasContent)
                    return DokumentUploadResult.Fail("Bitte zuerst eine digitale Signatur erfassen.", "VALIDATION");

                var context = await ResolvePachtvertragRequestAsync(new PachtvertragDokumentRequest
                {
                    MitgliedId = mitgliedId,
                    ParzelleId = parzelleId,
                    Vertragsbeginn = vertragsbeginn,
                    Status = FormularDokumentStatus.Signiert
                });
                if (context.IstMinderjaehrig)
                    return DokumentUploadResult.Fail("Für Minderjährige ist zusätzlich die digitale Unterschrift des gesetzlichen Vertreters erforderlich.", "VALIDATION");

                var previewUploadRequest = PachtvertragDokumentFactory.CreateUploadRequest(
                    context.Member,
                    context.SecondaryMember,
                    context.Parzelle,
                    context.Saison,
                    context.Vertragsbeginn,
                    altvertragDatum: null,
                    context.GesetzlicherVertreterSnapshot,
                    context.BankverbindungSnapshot,
                    FormularDokumentStatus.Unsigniert);
                var finalUploadRequest = PachtvertragDokumentFactory.CreateUploadRequest(
                    context.Member,
                    context.SecondaryMember,
                    context.Parzelle,
                    context.Saison,
                    context.Vertragsbeginn,
                    altvertragDatum: null,
                    context.GesetzlicherVertreterSnapshot,
                    context.BankverbindungSnapshot,
                    FormularDokumentStatus.Signiert);
                var sourceDocument = CreatePreviewDocumentInfo(previewUploadRequest, FormularDokumentTyp.Pachtvertrag, FormularDokumentStatus.Unsigniert);
                finalUploadRequest.FileContent = SignedVertragsdokumentPdfBuilder.Build(context.Member, sourceDocument, previewUploadRequest.FileContent, signatureCapture, null, "Unterschrift Pächter/in", null);
                return await CreateDokumentAsync(finalUploadRequest);
            },
            DokumentUploadResult.Fail("Pachtvertrag konnte aktuell nicht signiert gespeichert werden.", "UNEXPECTED"));

        public Task<DokumentUploadResult> CreateSignedPachtvertragDokumentAsync(PachtvertragDokumentRequest request, DigitalSignatureCapture signatureCapture, DigitalSignatureCapture? gesetzlicherVertreterSignatureCapture = null) => ExecuteAsync(
            "CreateSignedPachtvertragDokumentAsync(request)",
            async () =>
            {
                if (signatureCapture == null || !signatureCapture.HasContent)
                    return DokumentUploadResult.Fail("Bitte zuerst eine digitale Signatur erfassen.", "VALIDATION");

                var context = await ResolvePachtvertragRequestAsync(request);
                if (context.IstMinderjaehrig && (gesetzlicherVertreterSignatureCapture == null || !gesetzlicherVertreterSignatureCapture.HasContent))
                    return DokumentUploadResult.Fail("Für Minderjährige ist zusätzlich die digitale Unterschrift des gesetzlichen Vertreters erforderlich.", "VALIDATION");

                if (context.IstMinderjaehrig)
                {
                    var vertreterMitgliedId = await EnsureGesetzlicherVertreterMitgliedAsync(context, request);
                    var savedRelation = await SaveGesetzlichenVertreterAsync(new GesetzlicherVertreterSaveRequest
                    {
                        MinderjaehrigesMitgliedId = context.Member.Id,
                        VertreterMitgliedId = vertreterMitgliedId,
                        GueltigAb = context.Vertragsbeginn,
                        Bemerkung = "Automatisch aus Pachtvertrag übernommen."
                    });

                    if (savedRelation == null)
                        return DokumentUploadResult.Fail("Gesetzlicher Vertreter konnte nicht gespeichert werden.", "VALIDATION");
                }

                var previewUploadRequest = PachtvertragDokumentFactory.CreateUploadRequest(
                    context.Member,
                    context.SecondaryMember,
                    context.Parzelle,
                    context.Saison,
                    context.Vertragsbeginn,
                    request.AltvertragDatum,
                    context.GesetzlicherVertreterSnapshot,
                    context.BankverbindungSnapshot,
                    FormularDokumentStatus.Unsigniert);
                var finalUploadRequest = PachtvertragDokumentFactory.CreateUploadRequest(
                    context.Member,
                    context.SecondaryMember,
                    context.Parzelle,
                    context.Saison,
                    context.Vertragsbeginn,
                    request.AltvertragDatum,
                    context.GesetzlicherVertreterSnapshot,
                    context.BankverbindungSnapshot,
                    FormularDokumentStatus.Signiert);
                var sourceDocument = CreatePreviewDocumentInfo(previewUploadRequest, FormularDokumentTyp.Pachtvertrag, FormularDokumentStatus.Unsigniert);
                finalUploadRequest.FileContent = SignedVertragsdokumentPdfBuilder.Build(
                    context.Member,
                    sourceDocument,
                    previewUploadRequest.FileContent,
                    signatureCapture,
                    gesetzlicherVertreterSignatureCapture,
                    "Unterschrift Pächter/in",
                    context.IstMinderjaehrig ? "Unterschrift gesetzliche/r Vertreter/in" : null);
                return await CreateDokumentAsync(finalUploadRequest);
            },
            DokumentUploadResult.Fail("Pachtvertrag konnte aktuell nicht signiert gespeichert werden.", "UNEXPECTED"));

        private async Task<(MitgliedRecord Member, MitgliedRecord? SecondaryMember, ParzelleRecord Parzelle, SaisonRecord Saison, DateTime Vertragsbeginn, bool IstMinderjaehrig, MitgliedsantragVertreterSnapshot? GesetzlicherVertreterSnapshot, int? GesetzlicherVertreterMitgliedId, MitgliedsantragBankverbindungSnapshot BankverbindungSnapshot)> ResolvePachtvertragRequestAsync(PachtvertragDokumentRequest request)
        {
            if (request == null)
                throw new InvalidOperationException("Bitte zuerst einen gültigen Pachtvertrag vorbereiten.");

            if (request.MitgliedId <= 0)
                throw new InvalidOperationException("Bitte zuerst ein gültiges Mitglied auswählen.");

            if (request.ParzelleId <= 0)
                throw new InvalidOperationException("Bitte zuerst eine gültige Parzelle auswählen.");

            var vertragsbeginnDatum = request.Vertragsbeginn.Date;
            if (vertragsbeginnDatum == DateTime.MinValue)
                throw new InvalidOperationException("Bitte ein gültiges Vertragsbeginn-Datum angeben.");

            var member = await GetMitgliedByIdAsync(request.MitgliedId);
            if (member == null)
                throw new InvalidOperationException("Mitglied konnte nicht geladen werden.");

            if (!OperationalDataFilter.IsOperationalMember(member))
                throw new InvalidOperationException("Für dieses Mitglied kann aktuell kein Pachtvertrag erzeugt werden.");

            if (member.HauptmitgliedId.HasValue && member.HauptmitgliedId.Value > 0)
                throw new InvalidOperationException("Pachtvertrag kann nur aus dem Hauptmitglied-Kontext erzeugt werden.");

            var client = await EnsureClientAsync();
            var parzelle = await LoadParzelleByIdAsync(client, request.ParzelleId);
            if (parzelle == null)
                throw new InvalidOperationException("Parzelle konnte nicht geladen werden.");

            var saison = (await GetSaisonRecordsAsync())
                .FirstOrDefault(x => x.Jahr == vertragsbeginnDatum.Year);
            if (saison == null)
                throw new InvalidOperationException($"Für das Vertragsjahr {vertragsbeginnDatum.Year} ist keine Saison hinterlegt.");

            if (!saison.PachtProQm.HasValue)
                throw new InvalidOperationException($"Für die Saison {saison.Jahr} fehlt pacht_pro_qm.");

            var bankverbindungSnapshot = request.BankverbindungSnapshot != null
                ? NormalizeVereinsBankverbindungSnapshot(request.BankverbindungSnapshot)
                : await ResolveVereinsBankverbindungSnapshotAsync();
            request.BankverbindungSnapshot = bankverbindungSnapshot;

            var secondaryMember = await GetNebenmitgliedByHauptmitgliedIdAsync(member.Id);
            var istMinderjaehrig = GesetzlicherVertreterResolver.IsMinderjaehrig(member, vertragsbeginnDatum);
            if (!istMinderjaehrig)
                return (member, secondaryMember, parzelle, saison, vertragsbeginnDatum, false, null, null, bankverbindungSnapshot);

            MitgliedsantragVertreterSnapshot? vertreterSnapshot = null;
            int? vertreterMitgliedId = null;

            if (request.GesetzlicherVertreterAusBestehendemMitglied && request.GesetzlicherVertreterMitgliedId is > 0)
            {
                var vertreterMitglied = await GetMitgliedByIdAsync(request.GesetzlicherVertreterMitgliedId.Value);
                if (vertreterMitglied == null)
                    throw new InvalidOperationException("Gesetzlicher Vertreter konnte nicht geladen werden.");
                if (vertreterMitglied.Id == member.Id)
                    throw new InvalidOperationException("Das aufzunehmende Mitglied kann nicht gleichzeitig eigener gesetzlicher Vertreter sein.");

                vertreterSnapshot = BuildMitgliedsantragVertreterSnapshot(vertreterMitglied);
                vertreterMitgliedId = vertreterMitglied.Id;
            }
            else if (request.GesetzlicherVertreterSnapshot != null)
            {
                vertreterSnapshot = NormalizeMitgliedsantragVertreterSnapshot(request.GesetzlicherVertreterSnapshot, member, request.GesetzlicherVertreterAdresseAbweichend);
            }
            else
            {
                var aufloesung = await ResolveGesetzlicherVertreterAsync(member.Id, vertragsbeginnDatum);
                if (aufloesung.HatAktivenGesetzlichenVertreter)
                {
                    vertreterSnapshot = BuildMitgliedsantragVertreterSnapshot(aufloesung.Vorbelegung);
                    vertreterMitgliedId = aufloesung.VertreterMitglied?.Id;
                }
            }

            if (vertreterSnapshot == null || string.IsNullOrWhiteSpace(vertreterSnapshot.Vorname) || string.IsNullOrWhiteSpace(vertreterSnapshot.Nachname))
                throw new InvalidOperationException("Für Minderjährige ist ein gesetzlicher Vertreter mit Vor- und Nachname erforderlich.");

            return (member, secondaryMember, parzelle, saison, vertragsbeginnDatum, true, vertreterSnapshot, vertreterMitgliedId, bankverbindungSnapshot);
        }

        public async Task<DokumentUploadResult> UploadSignedVertragsdokumentAsync(int mitgliedId, DocumentInfo sourceDocument, byte[] fileContent, string originalFileName, string mimeType = "application/pdf")
        {
            if (mitgliedId <= 0)
                return DokumentUploadResult.Fail("Bitte zuerst ein gültiges Mitglied auswählen.", "VALIDATION");

            if (sourceDocument == null)
                return DokumentUploadResult.Fail("Bitte zuerst ein unsigniertes Vertragsdokument auswählen.", "VALIDATION");

            if ((fileContent?.Length ?? 0) <= 0)
                return DokumentUploadResult.Fail("Bitte eine signierte PDF-Datei auswählen.", "VALIDATION");

            if (!string.Equals(sourceDocument.FormularDokumentStatusKey, FormularDokumentStatus.Unsigniert, StringComparison.Ordinal))
                return DokumentUploadResult.Fail("Als Quelle muss eine vorhandene unsignierte Vertragsfassung ausgewählt werden.", "STATUS_INVALID");

            var dokumenttyp = FormularDokumentTyp.Normalize(sourceDocument.FormularDokumentTypKey);
            if (dokumenttyp is not FormularDokumentTyp.Mitgliedsvertrag and not FormularDokumentTyp.Pachtvertrag)
                return DokumentUploadResult.Fail("Signierte Fassungen können in diesem Block nur für Mitgliedsvertrag oder Pachtvertrag abgelegt werden.", "TYPE_INVALID");

            var normalizedMimeType = string.IsNullOrWhiteSpace(mimeType) ? "application/pdf" : mimeType.Trim();
            var originalName = CleanRequiredText(originalFileName);
            var isPdfUpload = string.Equals(normalizedMimeType, "application/pdf", StringComparison.OrdinalIgnoreCase)
                || originalName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
            if (!isPdfUpload)
                return DokumentUploadResult.Fail("Bitte eine signierte PDF-Datei hochladen.", "PDF_REQUIRED");

            try
            {
                var member = await GetMitgliedByIdAsync(mitgliedId);
                if (member == null)
                    return DokumentUploadResult.Fail("Mitglied konnte nicht geladen werden.", "NOT_FOUND");

                if (!OperationalDataFilter.IsOperationalMember(member))
                    return DokumentUploadResult.Fail("Für dieses Mitglied kann aktuell keine signierte Vertragsfassung abgelegt werden.", "NOT_OPERATIONAL");

                var uploadRequest = BuildSignedVertragsdokumentUploadRequest(member, dokumenttyp, fileContent);
                return await CreateDokumentAsync(uploadRequest);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "UploadSignedVertragsdokumentAsync failed for MitgliedId={MitgliedId}, SourceDocumentId={DokumentId}", mitgliedId, sourceDocument.Id);
                return DokumentUploadResult.Fail("Signierte Vertragsfassung konnte aktuell nicht abgelegt werden.", "UNEXPECTED");
            }
        }

        public async Task<DokumentUploadResult> CreateSignedVertragsdokumentAsync(int mitgliedId, DocumentInfo sourceDocument, DigitalSignatureCapture signatureCapture)
        {
            if (mitgliedId <= 0)
                return DokumentUploadResult.Fail("Bitte zuerst ein gültiges Mitglied auswählen.", "VALIDATION");

            if (sourceDocument == null)
                return DokumentUploadResult.Fail("Bitte zuerst ein unsigniertes Vertragsdokument auswählen.", "VALIDATION");

            if (signatureCapture == null || !signatureCapture.HasContent)
                return DokumentUploadResult.Fail("Bitte zuerst eine digitale Signatur erfassen.", "VALIDATION");

            var dokumenttyp = FormularDokumentTyp.Normalize(sourceDocument.FormularDokumentTypKey);
            if (!string.Equals(sourceDocument.FormularDokumentStatusKey, FormularDokumentStatus.Unsigniert, StringComparison.Ordinal))
                return DokumentUploadResult.Fail("Als Quelle muss eine vorhandene unsignierte Vertragsfassung ausgewählt werden.", "STATUS_INVALID");
            if (dokumenttyp is not FormularDokumentTyp.Mitgliedsvertrag and not FormularDokumentTyp.Pachtvertrag)
                return DokumentUploadResult.Fail("Digitale Signaturen sind in diesem Block nur für Mitgliedsvertrag oder Pachtvertrag verfügbar.", "TYPE_INVALID");

            try
            {
                var member = await GetMitgliedByIdAsync(mitgliedId);
                if (member == null)
                    return DokumentUploadResult.Fail("Mitglied konnte nicht geladen werden.", "NOT_FOUND");

                if (!OperationalDataFilter.IsOperationalMember(member))
                    return DokumentUploadResult.Fail("Für dieses Mitglied kann aktuell keine digitale Signatur abgelegt werden.", "NOT_OPERATIONAL");

                var originalPdf = await DownloadDokumentContentAsync(sourceDocument);
                if ((originalPdf?.Length ?? 0) <= 0)
                    return DokumentUploadResult.Fail("Die unsignierte Vertragsfassung konnte nicht als PDF geladen werden.", "SOURCE_DOWNLOAD_FAILED");

                var signedPdf = SignedVertragsdokumentPdfBuilder.Build(member, sourceDocument, originalPdf, signatureCapture);
                var uploadRequest = BuildSignedVertragsdokumentUploadRequest(member, dokumenttyp, signedPdf);
                return await CreateDokumentAsync(uploadRequest);
            }
            catch (InvalidOperationException ex)
            {
                _logger?.LogWarning(ex, "CreateSignedVertragsdokumentAsync validation failed for MitgliedId={MitgliedId}, SourceDocumentId={DokumentId}", mitgliedId, sourceDocument.Id);
                return DokumentUploadResult.Fail(ex.Message, "VALIDATION");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "CreateSignedVertragsdokumentAsync failed for MitgliedId={MitgliedId}, SourceDocumentId={DokumentId}", mitgliedId, sourceDocument.Id);
                return DokumentUploadResult.Fail("Digital signierte Vertragsfassung konnte aktuell nicht erzeugt werden.", "UNEXPECTED");
            }
        }

        public async Task<DokumentUploadResult> CreateDokumentAsync(DokumentUploadRequest request)
        {
            if (request == null)
                return DokumentUploadResult.Fail("Dokumentdaten fehlen.", "VALIDATION");

            var normalizedTitle = CleanRequiredText(request.Titel);
            if (string.IsNullOrWhiteSpace(normalizedTitle))
                return DokumentUploadResult.Fail("Bitte einen Dokumenttitel eingeben.", "VALIDATION");

            if ((request.FileContent?.Length ?? 0) <= 0)
                return DokumentUploadResult.Fail("Bitte eine Dokumentdatei auswählen.", "VALIDATION");

            var ownerCount = (request.MitgliedId.HasValue && request.MitgliedId.Value > 0 ? 1 : 0)
                + (request.ParzelleId.HasValue && request.ParzelleId.Value > 0 ? 1 : 0);
            if (ownerCount != 1)
                return DokumentUploadResult.Fail("Ein Dokument muss genau einem Mitglied oder genau einer Parzelle zugeordnet sein.", "VALIDATION");

            var uploadResult = await UploadDokumentToDriveAsync(request, normalizedTitle);
            if (!uploadResult.Success)
                return uploadResult;

            try
            {
                var client = await EnsureClientAsync();
                var now = DateTime.UtcNow;
                var insertRecord = new DokumentInsertRecord
                {
                    MitgliedId = request.MitgliedId > 0 ? request.MitgliedId : null,
                    ParzelleId = request.ParzelleId > 0 ? request.ParzelleId : null,
                    StoragePath = uploadResult.Document?.StoragePath ?? string.Empty,
                    Titel = normalizedTitle,
                    Dateiname = uploadResult.Document?.Dateiname,
                    MimeType = uploadResult.Document?.MimeType,
                    SizeBytes = uploadResult.Document?.Size,
                    DriveFileId = uploadResult.Document?.DriveFileId,
                    CreatedBy = ResolveCurrentAuthUserId(),
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await client.From<DokumentInsertRecord>().Insert(insertRecord);

                var createdDocument = await ReloadInsertedDokumentAsync(client, insertRecord)
                    ?? new DocumentInfo
                    {
                        Title = normalizedTitle,
                        Name = normalizedTitle,
                        Bucket = insertRecord.Bucket,
                        StoragePath = insertRecord.StoragePath,
                        Dateiname = insertRecord.Dateiname ?? string.Empty,
                        MimeType = insertRecord.MimeType ?? string.Empty,
                        Size = insertRecord.SizeBytes,
                        DriveFileId = insertRecord.DriveFileId ?? string.Empty,
                        CreatedBy = insertRecord.CreatedBy,
                        CreatedAt = insertRecord.CreatedAt,
                        UpdatedAt = insertRecord.UpdatedAt
                    };

                return DokumentUploadResult.Ok(createdDocument, uploadResult.RequestId);
            }
            catch (PostgrestException ex)
            {
                LogPostgrestFailure("CreateDokumentAsync", ex);
                return DokumentUploadResult.Fail("Dokumentmetadaten konnten nach dem Upload nicht gespeichert werden.", "POSTGREST");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "CreateDokumentAsync failed after upload.");
                return DokumentUploadResult.Fail("Dokument konnte aktuell nicht gespeichert werden.", "UNEXPECTED");
            }
        }

        public async Task<DokumentDeleteResult> DeleteDokumentAsync(DocumentInfo? document)
        {
            if (document == null || document.Id <= 0)
                return DokumentDeleteResult.Fail("Dokument konnte nicht gelöscht werden.", "VALIDATION");

            try
            {
                var client = await EnsureClientAsync();
                var existingRecord = await LoadDokumentRecordByIdAsync(client, document.Id);
                if (existingRecord == null)
                    return DokumentDeleteResult.Fail("Dokument ist bereits entfernt.", "NOT_FOUND");

                var driveFileId = CleanOptionalText(existingRecord.DriveFileId);
                if (!string.IsNullOrWhiteSpace(driveFileId))
                {
                    var driveDeleteResult = await DeleteDokumentFromDriveAsync(driveFileId);
                    if (!driveDeleteResult.Success)
                        return driveDeleteResult;
                }

                await client
                    .From<DokumentRecord>()
                    .Where(x => x.Id == existingRecord.Id)
                    .Delete();

                _logger?.LogInformation("DeleteDokumentAsync deleted dokument {DokumentId}", existingRecord.Id);
                return DokumentDeleteResult.Ok();
            }
            catch (PostgrestException ex)
            {
                LogPostgrestFailure("DeleteDokumentAsync", ex);
                return DokumentDeleteResult.Fail("Dokument konnte aktuell nicht gelöscht werden.", "POSTGREST");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "DeleteDokumentAsync failed.");
                return DokumentDeleteResult.Fail("Dokument konnte aktuell nicht gelöscht werden.", "UNEXPECTED");
            }
        }

        private async Task<ParzelleRecord?> LoadParzelleByIdAsync(Client client, int parzelleId)
        {
            var response = await client
                .From<ParzelleRecord>()
                .Where(x => x.Id == parzelleId)
                .Get();

            return response?.Models?.FirstOrDefault();
        }

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

                var normalizedMitgliedId = await ResolveHomeMitgliedIdAsync(mitgliedId);

                var bundle = await LoadWartungsvertragBundleAsync();
                var countsByContractId = bundle.ActiveAssignments
                    .GroupBy(x => x.WartungsvertragId)
                    .ToDictionary(x => x.Key, x => x.Count());
                var contractsById = bundle.Contracts.ToDictionary(x => x.Id);

                return bundle.ActiveAssignments
                    .Where(x => x.HauptmitgliedId == normalizedMitgliedId)
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

                var normalizedMitgliedId = await ResolveHomeMitgliedIdAsync(mitgliedId);

                var bundle = await LoadWartungsvertragBundleAsync();
                var countsByContractId = bundle.ActiveAssignments
                    .GroupBy(x => x.WartungsvertragId)
                    .ToDictionary(x => x.Key, x => x.Count());
                var assignedContractIds = bundle.ActiveAssignments
                    .Where(x => x.HauptmitgliedId == normalizedMitgliedId)
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

        private async Task<ZaehlerInsertResult> TryAddZaehlerCoreAsync(string medium, long parzelleId, string? zaehlernummer, DateTime eichdatum, DateTime eingebautAm)
        {
            var normalizedMedium = NormalizeZaehlerMedium(medium);
            if (string.IsNullOrWhiteSpace(normalizedMedium) || parzelleId <= 0)
                return ZaehlerInsertResult.Fail("Bitte Parzelle und Medium prüfen.", "VALIDATION");

            if (string.IsNullOrWhiteSpace(zaehlernummer))
                return ZaehlerInsertResult.Fail("Bitte eine Zählernummer eingeben.", "VALIDATION");

            try
            {
                await ValidateZaehlerInsertPreconditionsAsync(parzelleId, normalizedMedium);

                var client = await EnsureClientAsync();
                await client.From<ZaehlerInsertRecord>().Insert(new ZaehlerInsertRecord
                {
                    ParzelleId = parzelleId,
                    Medium = normalizedMedium,
                    Zaehlernummer = zaehlernummer.Trim(),
                    Eichdatum = NormalizeMeterEichjahr(eichdatum),
                    EingebautAm = NormalizeDateTime(eingebautAm.Date)
                });

                return ZaehlerInsertResult.Ok();
            }
            catch (InvalidOperationException ex)
            {
                var message = string.IsNullOrWhiteSpace(ex.Message)
                    ? "Der Zähler konnte nicht angelegt werden."
                    : ex.Message.Trim();
                return ZaehlerInsertResult.Fail(message, "PRECONDITION", ex.Message);
            }
            catch (PostgrestException ex) when (IsMissingZaehlerRfidPrecondition(ex))
            {
                return ZaehlerInsertResult.Fail(BuildMissingZaehlerRfidMessage(normalizedMedium), "RFID_MISSING", BuildPostgrestDiagnosticDetail(ex));
            }
            catch (PostgrestException ex) when (IsZaehlerMediumNotAllowedPrecondition(ex))
            {
                return ZaehlerInsertResult.Fail(BuildZaehlerMediumNotAllowedMessage(normalizedMedium), "MEDIUM_NOT_ALLOWED", BuildPostgrestDiagnosticDetail(ex));
            }
            catch (PostgrestException ex) when (IsUniqueViolation(ex))
            {
                return ZaehlerInsertResult.Fail("Diese Zählernummer ist bereits vorhanden.", "DUPLICATE", BuildPostgrestDiagnosticDetail(ex));
            }
            catch (PostgrestException ex) when (IsForeignKeyViolation(ex))
            {
                return ZaehlerInsertResult.Fail("Der Zähler konnte nicht angelegt werden (Bezug fehlt oder Parzelle ungültig).", "FOREIGN_KEY", BuildPostgrestDiagnosticDetail(ex));
            }
            catch (PostgrestException ex) when (IsCheckViolation(ex))
            {
                return ZaehlerInsertResult.Fail("Die Eingaben sind fachlich nicht zulässig. Bitte Daten prüfen.", "CHECK_VIOLATION", BuildPostgrestDiagnosticDetail(ex));
            }
            catch (PostgrestException ex) when (IsPermissionDenied(ex))
            {
                return ZaehlerInsertResult.Fail("Keine Berechtigung zum Anlegen des Zählers.", "PERMISSION", BuildPostgrestDiagnosticDetail(ex));
            }
            catch (PostgrestException ex)
            {
                LogPostgrestFailure($"TryAddZaehlerCoreAsync({normalizedMedium})", ex);
                return ZaehlerInsertResult.Fail("Der Zähler konnte nicht gespeichert werden.", "POSTGREST", BuildPostgrestDiagnosticDetail(ex));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "TryAddZaehlerCoreAsync({Medium}) failed.", normalizedMedium);
                return ZaehlerInsertResult.Fail("Der Zähler konnte nicht gespeichert werden.", "ERROR", ex.Message);
            }
        }

        private static bool IsUniqueViolation(PostgrestException ex)
        {
            var payload = TryParsePostgrestErrorPayload(ex.Content);
            if (string.Equals(payload?.Code, "23505", StringComparison.OrdinalIgnoreCase))
                return true;

            var content = (ex.Content ?? string.Empty).ToLowerInvariant();
            if (content.Contains("23505") || content.Contains("unique") || content.Contains("duplicate"))
                return true;

            var msg = ExtractPostgrestRelevantMessage(ex).ToLowerInvariant();
            return msg.Contains("23505") || msg.Contains("unique") || msg.Contains("duplicate");
        }

        private static bool IsForeignKeyViolation(PostgrestException ex)
        {
            var payload = TryParsePostgrestErrorPayload(ex.Content);
            if (string.Equals(payload?.Code, "23503", StringComparison.OrdinalIgnoreCase))
                return true;

            var content = (ex.Content ?? string.Empty).ToLowerInvariant();
            return content.Contains("23503") || content.Contains("foreign key") || content.Contains("violates foreign key");
        }

        private static bool IsCheckViolation(PostgrestException ex)
        {
            var payload = TryParsePostgrestErrorPayload(ex.Content);
            if (string.Equals(payload?.Code, "23514", StringComparison.OrdinalIgnoreCase))
                return true;

            var content = (ex.Content ?? string.Empty).ToLowerInvariant();
            return content.Contains("23514") || content.Contains("check constraint") || content.Contains("violates check");
        }

        private static bool IsPermissionDenied(PostgrestException ex)
        {
            var payload = TryParsePostgrestErrorPayload(ex.Content);
            if (string.Equals(payload?.Code, "42501", StringComparison.OrdinalIgnoreCase))
                return true;

            var content = (ex.Content ?? string.Empty).ToLowerInvariant();
            return content.Contains("42501") || content.Contains("permission denied") || content.Contains("not authorized") || content.Contains("not allowed");
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
                    if (!bundle.MembersById.ContainsKey(requestedId))
                        continue;

                    var normalizedId = await ResolveHomeMitgliedIdAsync(requestedId);
                    if (normalizedId > 0 && !normalizedMemberIds.Contains(normalizedId))
                        normalizedMemberIds.Add(normalizedId);
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

                var normalizedMitgliedId = await ResolveHomeMitgliedIdAsync(mitgliedId);
                if (normalizedMitgliedId <= 0)
                    return CreateWartungsvertragAssignmentSaveResult(false, "Das ausgewählte Mitglied konnte nicht belastbar aufgelöst werden.", requestedContractIds.Count, 0, 0);

                var bundle = await LoadWartungsvertragBundleAsync();
                if (!bundle.MembersById.ContainsKey(mitgliedId))
                    return CreateWartungsvertragAssignmentSaveResult(false, "Das ausgewählte Mitglied konnte nicht belastbar aufgelöst werden.", requestedContractIds.Count, 0, 0);

                var countsByContractId = bundle.ActiveAssignments
                    .GroupBy(x => x.WartungsvertragId)
                    .ToDictionary(x => x.Key, x => x.Count());
                var activeContractIds = bundle.ActiveAssignments
                    .Where(x => x.HauptmitgliedId == normalizedMitgliedId)
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
                        HauptmitgliedId = normalizedMitgliedId,
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
                    summaryTask = TryLoadHomeSectionAsync(
                        "LoadPflichtstundenSummaryAsync",
                        () => LoadPflichtstundenSummaryAsync(mitgliedId.Value, DateTime.Today.Year),
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
                    .Where(OperationalDataFilter.IsOperationalArbeitseinsatz)
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

                if (!await InsertArbeitseinsatzAsync(insertRecord))
                    return null;

                var client = await EnsureClientAsync();
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
                var reloadItems = reloadResponse?.Models?
                    .Select(NormalizeArbeitseinsatzRecord)
                    .ToList()
                    ?? new List<ArbeitseinsatzRecord>();
                var created = reloadItems
                    .Where(x => IsSameArbeitseinsatzForReload(x, reloadCandidate))
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefault()
                    ?? reloadItems
                        .OrderByDescending(x => x.Id)
                        .FirstOrDefault();

                if (created == null)
                {
                    _logger?.LogWarning("CreateArbeitseinsatzAsync insert succeeded, but reload returned no matching row. Returning fallback record without reloaded id.");
                    created = NormalizeArbeitseinsatzRecord(reloadCandidate);
                }

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

                if (!await UpdateArbeitseinsatzPostgrestAsync(record))
                    return false;

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
                    .Where(OperationalDataFilter.IsOperationalTermin)
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

                if (!await InsertTerminAsync(insertRecord))
                    return null;

                var client = await EnsureClientAsync();
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
                var reloadItems = reloadResponse?.Models?
                    .Select(NormalizeTerminRecord)
                    .ToList()
                    ?? new List<TerminRecord>();
                var created = reloadItems
                    .Where(x => IsSameTerminForReload(x, reloadCandidate))
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefault()
                    ?? reloadItems
                        .OrderByDescending(x => x.Id)
                        .FirstOrDefault();

                if (created == null)
                {
                    _logger?.LogWarning("CreateTerminAsync insert succeeded, but reload returned no matching row. Returning fallback record without reloaded id.");
                    created = NormalizeTerminRecord(reloadCandidate);
                }

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

                if (!await UpdateTerminPostgrestAsync(record))
                    return false;

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
                    .Where(OperationalDataFilter.IsOperationalBekanntmachung)
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
                var reloadItems = reloadResponse?.Models?
                    .Select(NormalizeBekanntmachungRecord)
                    .ToList()
                    ?? new List<BekanntmachungRecord>();
                var created = reloadItems
                    .Where(x => IsSameBekanntmachungForReload(x, reloadCandidate))
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefault()
                    ?? reloadItems
                        .Where(x => string.Equals(CleanRequiredText(x.Titel), insertRecord.Titel, StringComparison.CurrentCulture))
                        .OrderByDescending(x => x.Id)
                        .FirstOrDefault();

                if (created == null)
                {
                    _logger?.LogWarning("CreateBekanntmachungAsync insert succeeded, but reload returned no matching row. Returning fallback record without reloaded id.");
                    created = NormalizeBekanntmachungRecord(reloadCandidate);
                }

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

        private static async Task<AppUserRecord?> GetAppUserByMitgliedIdAsync(Client client, int mitgliedId, Guid? authUserId = null)
        {
            AppUserRecord? appUser = null;

            if (mitgliedId > 0)
            {
                var response = await client
                    .From<AppUserRecord>()
                    .Get();

                appUser = response?.Models?
                    .Where(x => x.MitgliedId == mitgliedId)
                    .OrderByDescending(x => x.UpdatedAt)
                    .FirstOrDefault();
            }

            if (appUser != null || !authUserId.HasValue)
                return appUser;

            return await GetAppUserByUserIdAsync(client, authUserId.Value);
        }

        private static async Task<AppUserRecord?> GetAppUserByUserIdAsync(Client client, Guid userId)
        {
            var response = await client
                .From<AppUserRecord>()
                .Where(x => x.UserId == userId)
                .Get();

            return response?.Models?.FirstOrDefault();
        }

        private static async Task ApplyAppUserRolesAsync(Client client, IReadOnlyCollection<MitgliedRecord> members)
        {
            if (members == null || members.Count == 0)
                return;

            try
            {
                var appUsersResponse = await client.From<AppUserRecord>().Get();
                var appUsers = appUsersResponse?.Models?.ToList() ?? new List<AppUserRecord>();

                foreach (var member in members)
                    ApplyAppUserRole(member, appUsers);
            }
            catch
            {
                foreach (var member in members)
                    member.Role = NormalizeAppUserRole(null);
            }
        }

        private static async Task<MitgliedRecord?> ApplyAppUserRoleAsync(Client client, MitgliedRecord? member)
        {
            if (member == null)
                return null;

            try
            {
                var appUser = await GetAppUserByMitgliedIdAsync(client, member.Id, member.AuthUserId);
                member.Role = NormalizeAppUserRole(appUser?.Role);
            }
            catch
            {
                member.Role = NormalizeAppUserRole(null);
            }

            return member;
        }

        private static void ApplyAppUserRole(MitgliedRecord member, IReadOnlyCollection<AppUserRecord> appUsers)
        {
            if (member == null)
                return;

            var appUser = appUsers.FirstOrDefault(x => x.MitgliedId == (long?)member.Id);
            if (appUser == null && member.AuthUserId.HasValue)
                appUser = appUsers.FirstOrDefault(x => x.UserId == member.AuthUserId.Value);

            member.Role = NormalizeAppUserRole(appUser?.Role);
        }

        private static string NormalizeAppUserRole(string? role)
        {
            return UserRoles.ToStorageValue(UserRoles.Parse(role));
        }

        private static DateTime? NormalizeDate(DateTime? value)
        {
            if (!value.HasValue)
                return null;

            var normalized = value.Value.Date.AddHours(12);
            return DateTime.SpecifyKind(normalized, DateTimeKind.Unspecified);
        }

        private static bool HasActiveForeignMitgliedLock(MitgliedRecord member, Guid userGuid, int timeoutMinutes)
        {
            var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            return member.LockedByUserId.HasValue
                && member.LockedByUserId.Value != userGuid
                && (!member.LockedAt.HasValue || member.LockedAt.Value.AddMinutes(timeoutMinutes) > now);
        }

        private static DateTime NormalizeMeterEichjahr(DateTime value)
        {
            var normalized = new DateTime(value.Year, 1, 1, 12, 0, 0);
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

        private static DateTime? NormalizeDateOnly(DateTime? value)
        {
            return value.HasValue
                ? NormalizeDateOnly(value.Value)
                : null;
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

        private async Task<bool> InsertArbeitseinsatzAsync(ArbeitseinsatzInsertRecord record)
        {
            var payload = new Dictionary<string, object?>
            {
                ["titel"] = record.Titel,
                ["beschreibung"] = record.Beschreibung,
                ["datum"] = FormatPostgresDate(NormalizeDateOnly(record.Datum)),
                ["start_uhrzeit"] = FormatPostgresTime(record.StartUhrzeit),
                ["end_uhrzeit"] = FormatPostgresTime(record.EndUhrzeit),
                ["treffpunkt"] = record.Treffpunkt,
                ["max_teilnehmer"] = record.MaxTeilnehmer,
                ["stunden_wert"] = record.StundenWert,
                ["sichtbar_ab"] = FormatPostgresTimestampWithoutTimeZone(record.SichtbarAb),
                ["sichtbar_bis"] = FormatPostgresTimestampWithoutTimeZone(record.SichtbarBis),
                ["anmeldung_bis"] = FormatPostgresTimestampWithoutTimeZone(record.AnmeldungBis),
                ["aktiv"] = record.Aktiv,
                ["created_at"] = FormatPostgresTimestampWithoutTimeZone(record.CreatedAt),
                ["updated_at"] = FormatPostgresTimestampWithoutTimeZone(record.UpdatedAt),
                ["is_demo"] = record.IsDemo
            };

            return await SendPostgrestWriteAsync(HttpMethod.Post, "arbeitseinsatz", payload);
        }

        private async Task<bool> UpdateArbeitseinsatzPostgrestAsync(ArbeitseinsatzRecord record)
        {
            var payload = new Dictionary<string, object?>
            {
                ["titel"] = CleanRequiredText(record.Titel),
                ["beschreibung"] = CleanOptionalText(record.Beschreibung),
                ["datum"] = FormatPostgresDate(NormalizeDateOnly(record.Datum)),
                ["start_uhrzeit"] = FormatPostgresTime(NormalizeTerminTime(record.StartUhrzeit)),
                ["end_uhrzeit"] = FormatPostgresTime(NormalizeTerminTime(record.EndUhrzeit)),
                ["treffpunkt"] = CleanOptionalText(record.Treffpunkt),
                ["max_teilnehmer"] = record.MaxTeilnehmer,
                ["stunden_wert"] = record.StundenWert < 0 ? 0 : record.StundenWert,
                ["sichtbar_ab"] = FormatPostgresTimestampWithoutTimeZone(NormalizeTimestampWithoutTimeZone(record.SichtbarAb)),
                ["sichtbar_bis"] = FormatPostgresTimestampWithoutTimeZone(NormalizeTimestampWithoutTimeZone(record.SichtbarBis)),
                ["anmeldung_bis"] = FormatPostgresTimestampWithoutTimeZone(NormalizeTimestampWithoutTimeZone(record.AnmeldungBis)),
                ["aktiv"] = record.Aktiv,
                ["updated_at"] = FormatPostgresTimestampWithoutTimeZone(NormalizeTimestampWithoutTimeZone(DateTime.UtcNow)),
                ["is_demo"] = record.IsDemo
            };

            return await SendPostgrestWriteAsync(HttpMethod.Patch, $"arbeitseinsatz?id=eq.{record.Id}", payload);
        }

        private async Task<bool> InsertTerminAsync(TerminInsertRecord record)
        {
            var payload = new Dictionary<string, object?>
            {
                ["titel"] = record.Titel,
                ["beschreibung"] = record.Beschreibung,
                ["datum"] = FormatPostgresDate(NormalizeDateOnly(record.Datum)),
                ["start_uhrzeit"] = FormatPostgresTime(record.StartUhrzeit),
                ["end_uhrzeit"] = FormatPostgresTime(record.EndUhrzeit),
                ["sichtbar_ab"] = FormatPostgresTimestampWithoutTimeZone(record.SichtbarAb),
                ["sichtbar_bis"] = FormatPostgresTimestampWithoutTimeZone(record.SichtbarBis),
                ["aktiv"] = record.Aktiv,
                ["created_at"] = FormatPostgresTimestampWithoutTimeZone(record.CreatedAt),
                ["updated_at"] = FormatPostgresTimestampWithoutTimeZone(record.UpdatedAt)
            };

            return await SendPostgrestWriteAsync(HttpMethod.Post, "termin", payload);
        }

        private async Task<bool> UpdateTerminPostgrestAsync(TerminRecord record)
        {
            var payload = new Dictionary<string, object?>
            {
                ["titel"] = CleanRequiredText(record.Titel),
                ["beschreibung"] = CleanOptionalText(record.Beschreibung),
                ["datum"] = FormatPostgresDate(NormalizeDateOnly(record.Datum)),
                ["start_uhrzeit"] = FormatPostgresTime(NormalizeTerminTime(record.StartUhrzeit)),
                ["end_uhrzeit"] = FormatPostgresTime(NormalizeTerminTime(record.EndUhrzeit)),
                ["sichtbar_ab"] = FormatPostgresTimestampWithoutTimeZone(NormalizeTimestampWithoutTimeZone(record.SichtbarAb)),
                ["sichtbar_bis"] = FormatPostgresTimestampWithoutTimeZone(NormalizeTimestampWithoutTimeZone(record.SichtbarBis)),
                ["aktiv"] = record.Aktiv,
                ["updated_at"] = FormatPostgresTimestampWithoutTimeZone(NormalizeTimestampWithoutTimeZone(DateTime.UtcNow))
            };

            return await SendPostgrestWriteAsync(HttpMethod.Patch, $"termin?id=eq.{record.Id}", payload);
        }

        private async Task<bool> SendPostgrestWriteAsync(HttpMethod method, string relativePathAndQuery, IReadOnlyDictionary<string, object?> payload)
        {
            var accessToken = await _authService.GetAccessTokenAsync();
            using var request = new HttpRequestMessage(method, BuildPostgrestUri(relativePathAndQuery));
            request.Headers.Add("apikey", _publishableKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", string.IsNullOrWhiteSpace(accessToken) ? _publishableKey : accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation("Prefer", "return=minimal");
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var response = await _documentUploadHttpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
                return true;

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger?.LogWarning("PostgREST write failed for {RelativePath}. Status={StatusCode} Content={Content}", relativePathAndQuery, (int)response.StatusCode, responseContent);
            return false;
        }

        private Uri BuildPostgrestUri(string relativePathAndQuery)
            => new($"{_supabaseUrl.TrimEnd('/')}/rest/v1/{relativePathAndQuery.TrimStart('/')}", UriKind.Absolute);

        private static string FormatPostgresDate(DateTime value)
            => NormalizeDateOnly(value).ToString("yyyy-MM-dd");

        private static string? FormatPostgresTimestampWithoutTimeZone(DateTime? value)
        {
            var normalized = NormalizeTimestampWithoutTimeZone(value);
            return normalized?.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff");
        }

        private static string? FormatPostgresTime(TimeSpan? value)
            => value.HasValue ? value.Value.ToString(@"hh\:mm\:ss") : null;

        private static DateTime CreateEndOfDayTimestamp(DateTime date)
        {
            var normalized = NormalizeDateOnly(date);
            return new DateTime(normalized.Year, normalized.Month, normalized.Day, 23, 59, 0, DateTimeKind.Unspecified);
        }

        private static string FormatNullableDateTime(DateTime? value)
            => value.HasValue ? value.Value.ToString("O") : "null";

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

        private static MitgliedGesetzlicherVertreterRecord? ResolveAktivenGesetzlichenVertreter(IEnumerable<MitgliedGesetzlicherVertreterRecord>? records, DateTime? stichtag = null)
        {
            var referenceDate = (stichtag ?? DateTime.Today).Date;
            return (records ?? Enumerable.Empty<MitgliedGesetzlicherVertreterRecord>())
                .Where(x => x.MinderjaehrigesMitgliedId > 0 && x.VertreterMitgliedId > 0)
                .Where(x => x.GueltigAb.Date <= referenceDate)
                .Where(x => !x.GueltigBis.HasValue || x.GueltigBis.Value.Date >= referenceDate)
                .OrderByDescending(x => !x.GueltigBis.HasValue)
                .ThenByDescending(x => x.GueltigAb)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();
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
            => ArbeitsstundenPruefprozess.IsOffenerPrueffall(record.Status, record.Freigegeben);

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
            => (await GetZaehlerForParzelleAsync(parzelleId, "strom"))
                .Select(MapStromzaehlerRecord)
                .ToList();

        private async Task<List<WasserzaehlerRecord>> GetWasserzaehlerForParzelleAsync(int parzelleId)
            => (await GetZaehlerForParzelleAsync(parzelleId, "wasser"))
                .Select(MapWasserzaehlerRecord)
                .ToList();

        private async Task<bool> AddZaehlerCoreAsync(string medium, long parzelleId, string? zaehlernummer, DateTime eichdatum, DateTime eingebautAm)
        {
            var normalizedMedium = NormalizeZaehlerMedium(medium);
            if (string.IsNullOrWhiteSpace(normalizedMedium) || parzelleId <= 0 || string.IsNullOrWhiteSpace(zaehlernummer))
                return false;

            try
            {
                await ValidateZaehlerInsertPreconditionsAsync(parzelleId, normalizedMedium);

                var client = await EnsureClientAsync();
                await client.From<ZaehlerInsertRecord>().Insert(new ZaehlerInsertRecord
                {
                    ParzelleId = parzelleId,
                    Medium = normalizedMedium,
                    Zaehlernummer = zaehlernummer.Trim(),
                    Eichdatum = NormalizeMeterEichjahr(eichdatum),
                    EingebautAm = NormalizeDateTime(eingebautAm.Date)
                });

                return true;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (PostgrestException ex) when (IsMissingZaehlerRfidPrecondition(ex))
            {
                throw new InvalidOperationException(BuildMissingZaehlerRfidMessage(normalizedMedium), ex);
            }
            catch (PostgrestException ex) when (IsZaehlerMediumNotAllowedPrecondition(ex))
            {
                throw new InvalidOperationException(BuildZaehlerMediumNotAllowedMessage(normalizedMedium), ex);
            }
            catch (PostgrestException ex)
            {
                LogPostgrestFailure($"AddZaehlerCoreAsync({normalizedMedium})", ex);
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AddZaehlerCoreAsync({Medium}) failed.", normalizedMedium);
                return false;
            }
        }

        private async Task ValidateZaehlerInsertPreconditionsAsync(long parzelleId, string normalizedMedium)
        {
            var parzelle = await GetParzelleRecordByIdAsync(parzelleId)
                ?? throw new InvalidOperationException("Die gewählte Parzelle wurde nicht gefunden.");

            if (string.Equals(normalizedMedium, "wasser", StringComparison.OrdinalIgnoreCase))
            {
                if (!parzelle.HatWasser)
                    throw new InvalidOperationException($"Für {parzelle.DisplayName} ist Wasser nicht freigeschaltet.");

                if (string.IsNullOrWhiteSpace(parzelle.RfidWasser))
                    throw new InvalidOperationException(BuildMissingZaehlerRfidMessage(normalizedMedium));

                return;
            }

            if (!parzelle.HatStrom)
                throw new InvalidOperationException($"Für {parzelle.DisplayName} ist Strom nicht freigeschaltet.");

            if (string.IsNullOrWhiteSpace(parzelle.RfidStrom))
                throw new InvalidOperationException(BuildMissingZaehlerRfidMessage(normalizedMedium));
        }

        private async Task<ParzelleRecord?> GetParzelleRecordByIdAsync(long parzelleId)
        {
            if (parzelleId <= 0 || parzelleId > int.MaxValue)
                return null;

            var client = await EnsureClientAsync();
            var response = await client
                .From<ParzelleRecord>()
                .Where(x => x.Id == (int)parzelleId)
                .Get();

            return response?.Models?.FirstOrDefault();
        }

        private async Task<List<ZaehlerRecord>> GetZaehlerForParzelleAsync(int parzelleId, string medium)
        {
            var normalizedMedium = NormalizeZaehlerMedium(medium);
            if (string.IsNullOrWhiteSpace(normalizedMedium))
                return new List<ZaehlerRecord>();

            var client = await EnsureClientAsync();
            var response = await client
                .From<ZaehlerRecord>()
                .Where(x => x.ParzelleId == parzelleId)
                .Where(x => x.Medium == normalizedMedium)
                .Get();

            return response?.Models?
                .OrderByDescending(x => x.EingebautAm)
                .ThenByDescending(x => x.Id)
                .ToList()
                ?? new List<ZaehlerRecord>();
        }

        private static StromzaehlerRecord MapStromzaehlerRecord(ZaehlerRecord source)
        {
            return new StromzaehlerRecord
            {
                Id = source.Id,
                ParzelleId = source.ParzelleId,
                Zaehlernummer = source.Zaehlernummer,
                Eichdatum = source.Eichdatum,
                EingebautAm = source.EingebautAm,
                AusgebautAm = source.AusgebautAm
            };
        }

        private static WasserzaehlerRecord MapWasserzaehlerRecord(ZaehlerRecord source)
        {
            return new WasserzaehlerRecord
            {
                Id = source.Id,
                ParzelleId = source.ParzelleId,
                Zaehlernummer = source.Zaehlernummer,
                Eichdatum = source.Eichdatum,
                EingebautAm = source.EingebautAm,
                AusgebautAm = source.AusgebautAm
            };
        }

        private async Task<List<ZaehlerAblesungDTO>> GetAblesungenAsync<TMeter>(IReadOnlyCollection<TMeter> meters)
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
                    .Where(x => meterById.ContainsKey(x.ZaehlerId))
                    .Where(x => !AblesungPruefstatus.IsAbgelehnt(x.Pruefstatus))
                    .OrderByDescending(x => x.Ablesedatum)
                    .ThenByDescending(x => x.Id)
                    .Select(x => MapZaehlerAblesungDto(x, meterById[x.ZaehlerId].Zaehlernummer, meterById[x.ZaehlerId].Eichdatum))
                    .ToList();
            }

            var wasserById = meters.Cast<WasserzaehlerRecord>().ToDictionary(x => x.Id, x => x);
            return ablesungen
                .Where(x => wasserById.ContainsKey(x.ZaehlerId))
                .Where(x => !AblesungPruefstatus.IsAbgelehnt(x.Pruefstatus))
                .OrderByDescending(x => x.Ablesedatum)
                .ThenByDescending(x => x.Id)
                .Select(x => MapZaehlerAblesungDto(x, wasserById[x.ZaehlerId].Zaehlernummer, wasserById[x.ZaehlerId].Eichdatum))
                .ToList();
        }

        private async Task<AblesungRecord?> GetOffeneReviewAblesungAsync(long ablesungId)
        {
            if (ablesungId <= 0)
                return null;

            var client = await EnsureClientAsync();
            var response = await client
                .From<AblesungRecord>()
                .Where(x => x.Id == ablesungId)
                .Get();

            var existing = response?.Models?.FirstOrDefault();
            return existing != null
                   && string.Equals(AblesungPruefstatus.Normalize(existing.Pruefstatus, existing.Freigegeben), AblesungPruefstatus.Eingereicht, StringComparison.Ordinal)
                ? existing
                : null;
        }

        private static string BuildReviewCorrectionComment(string kommentar)
            => $"Korrigiert im Prüfprozess: {kommentar}";

        private static string BuildReviewRemovalComment(string begruendung)
            => $"Im Prüfprozess entfernt: {begruendung}";

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
                Freigegeben = record.Freigegeben,
                Pruefstatus = AblesungPruefstatus.Normalize(record.Pruefstatus, record.Freigegeben),
                Pruefkommentar = record.Pruefkommentar,
                GeprueftVon = record.GeprueftVon,
                GeprueftAm = record.GeprueftAm,
                FotoPfad = record.FotoPfad,
                FotoDateiname = record.FotoDateiname,
                FotoDriveFileId = record.FotoDriveFileId
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

            var payload = TryParsePostgrestErrorPayload(ex.Content);
            if (payload != null)
            {
                if (!string.IsNullOrWhiteSpace(payload.Code))
                    parts.Add($"Code={payload.Code}");

                if (!string.IsNullOrWhiteSpace(payload.Message))
                    parts.Add($"Message={payload.Message}");

                if (!string.IsNullOrWhiteSpace(payload.Details))
                    parts.Add($"Details={payload.Details}");

                if (!string.IsNullOrWhiteSpace(payload.Hint))
                    parts.Add($"Hint={payload.Hint}");
            }
            else
            {
                var relevantMessage = ExtractPostgrestRelevantMessage(ex);
                if (!string.IsNullOrWhiteSpace(relevantMessage))
                    parts.Add(relevantMessage);
            }

            return string.Join(" | ", parts.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.CurrentCulture));
        }

        private static string ExtractPostgrestRelevantMessage(PostgrestException ex)
        {
            var payload = TryParsePostgrestErrorPayload(ex.Content);
            if (!string.IsNullOrWhiteSpace(payload?.Message))
                return payload!.Message!;

            if (!string.IsNullOrWhiteSpace(payload?.Details))
                return payload!.Details!;

            return string.IsNullOrWhiteSpace(ex.Message)
                ? string.Empty
                : Regex.Replace(ex.Message.Trim(), "\\s+", " ");
        }

        private static bool IsMissingZaehlerRfidPrecondition(PostgrestException ex)
        {
            var message = ExtractPostgrestRelevantMessage(ex).ToLowerInvariant();
            return message.Contains("rfid") && (message.Contains("parzelle") || message.Contains("medium") || message.Contains("zaehler"));
        }

        private static bool IsZaehlerMediumNotAllowedPrecondition(PostgrestException ex)
        {
            var message = ExtractPostgrestRelevantMessage(ex).ToLowerInvariant();
            return message.Contains("medium") && (message.Contains("not allowed") || message.Contains("nicht") || message.Contains("parzelle"));
        }

        private static string BuildMissingZaehlerRfidMessage(string medium)
            => $"Für diese Parzelle ist noch keine RFID für {MediumDisplayName(medium)} hinterlegt. Bitte zuerst RFID einrichten.";

        private static string BuildZaehlerMediumNotAllowedMessage(string medium)
            => $"Für diese Parzelle ist {MediumDisplayName(medium)} nicht freigeschaltet.";

        private static string? NormalizeZaehlerMedium(string? medium)
        {
            if (string.IsNullOrWhiteSpace(medium))
                return null;

            var normalized = medium.Trim().ToLowerInvariant();
            return normalized is "strom" or "wasser" ? normalized : null;
        }

        private static PostgrestErrorPayload? TryParsePostgrestErrorPayload(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            try
            {
                using var document = JsonDocument.Parse(content);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                    return null;

                return new PostgrestErrorPayload(
                    TryGetJsonString(document.RootElement, "code"),
                    TryGetJsonString(document.RootElement, "message"),
                    TryGetJsonString(document.RootElement, "details"),
                    TryGetJsonString(document.RootElement, "hint"));
            }
            catch
            {
                return null;
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

        private sealed record PostgrestErrorPayload(string? Code, string? Message, string? Details, string? Hint);

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
            var records = response?.Models?
                .Select(NormalizeStartseiteArbeitseinsatzRecord)
                .ToList()
                ?? new List<StartseiteArbeitseinsatzRecord>();

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
            var records = response?.Models?
                .Select(NormalizeStartseiteTerminRecord)
                .ToList()
                ?? new List<StartseiteTerminRecord>();

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
                            && OperationalDataFilter.IsOperationalArbeitseinsatz(record)
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
                            && OperationalDataFilter.IsOperationalTermin(record)
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
                            && OperationalDataFilter.IsOperationalBekanntmachung(record)
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
                Id = record.Id,
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

            var record = response?.Models?
                .Select(NormalizeArbeitseinsatzRecord)
                .FirstOrDefault();

            return OperationalDataFilter.IsOperationalArbeitseinsatz(record)
                ? record
                : null;
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
            var arbeitseinsatz = await GetArbeitseinsatzByIdAsync(client, arbeitseinsatzId);
            var now = CreateEditorNowDefault();
            if (arbeitseinsatz == null
                || !OperationalDataFilter.IsOperationalArbeitseinsatz(arbeitseinsatz)
                || !IsCurrentlyVisible(arbeitseinsatz.Aktiv, arbeitseinsatz.SichtbarAb, arbeitseinsatz.SichtbarBis, now))
            {
                return null;
            }

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
            var storagePath = CleanOptionalText(record.StoragePath) ?? string.Empty;
            var fallbackName = string.IsNullOrWhiteSpace(storagePath)
                ? string.Empty
                : Path.GetFileName(storagePath.Replace('\\', '/'));

            return new DocumentInfo
            {
                Id = record.Id,
                Title = FirstNonEmpty(record.Titel, record.Dateiname, fallbackName) ?? string.Empty,
                Name = FirstNonEmpty(record.Titel, record.Dateiname, fallbackName) ?? string.Empty,
                Bucket = CleanOptionalText(record.Bucket) ?? string.Empty,
                StoragePath = storagePath,
                Dateiname = FirstNonEmpty(record.Dateiname, fallbackName) ?? string.Empty,
                MimeType = CleanOptionalText(record.MimeType) ?? string.Empty,
                DriveFileId = CleanOptionalText(record.DriveFileId) ?? string.Empty,
                Size = record.SizeBytes,
                CreatedBy = record.CreatedBy,
                CreatedAt = record.CreatedAt,
                UpdatedAt = record.UpdatedAt
            };
        }

        private static StartseiteTerminRecord NormalizeStartseiteTerminRecord(StartseiteTerminRecord record)
        {
            return new StartseiteTerminRecord
            {
                Id = record.Id,
                Titel = record.Titel,
                Thema = record.Thema,
                Datum = NormalizeDateOnly(record.Datum),
                Beginn = record.Beginn,
                Ende = record.Ende,
                Ort = record.Ort,
                Beschreibung = record.Beschreibung,
                Inhalt = record.Inhalt
            };
        }

        private static StartseiteArbeitseinsatzRecord NormalizeStartseiteArbeitseinsatzRecord(StartseiteArbeitseinsatzRecord record)
        {
            return new StartseiteArbeitseinsatzRecord
            {
                Id = record.Id,
                Titel = record.Titel,
                Thema = record.Thema,
                Datum = NormalizeDateOnly(record.Datum),
                Beginn = record.Beginn,
                Ende = record.Ende,
                Treffpunkt = record.Treffpunkt,
                Beschreibung = record.Beschreibung,
                FreiePlaetze = record.FreiePlaetze,
                AngemeldetCount = record.AngemeldetCount,
                AnmeldungMoeglich = record.AnmeldungMoeglich,
                IstAngemeldet = record.IstAngemeldet
            };
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

        private async Task<byte[]?> DownloadDokumentContentAsync(DocumentInfo document)
        {
            if (document == null)
                return null;

            if (!string.IsNullOrWhiteSpace(document.DriveFileId))
                return await _documentUploadHttpClient.GetByteArrayAsync(BuildGoogleDriveFileDownloadUrl(document.DriveFileId));

            var url = await ResolveDokumentOpenUrlAsync(document, 600);
            if (string.IsNullOrWhiteSpace(url))
                return null;

            return await _documentUploadHttpClient.GetByteArrayAsync(url);
        }

        private static DokumentUploadRequest BuildSignedVertragsdokumentUploadRequest(MitgliedRecord member, string dokumenttyp, byte[] fileContent)
        {
            return new DokumentUploadRequest
            {
                MitgliedId = member.Id,
                Titel = FormularDokumentDateiname.BuildTitel(dokumenttyp, FormularDokumentStatus.Signiert),
                FileName = FormularDokumentDateiname.BuildMitgliedDateiname(member, dokumenttyp, FormularDokumentStatus.Signiert, DateTime.Today),
                MimeType = "application/pdf",
                FileContent = fileContent
            };
        }

        private static string BuildGoogleDriveFileViewUrl(string driveFileId)
            => $"https://drive.google.com/file/d/{Uri.EscapeDataString(driveFileId)}/view";

        private static string BuildGoogleDriveFileDownloadUrl(string driveFileId)
            => $"https://drive.google.com/uc?export=download&id={Uri.EscapeDataString(driveFileId)}";

        private static string BuildExpectedDokumentStoragePrefix(DokumentUploadRequest request)
        {
            if (request.MitgliedId.HasValue && request.MitgliedId.Value > 0)
                return $"KGV-APP/Dokumente/Mitglieder/{request.MitgliedId.Value}/";

            return $"KGV-App/Dokumente/Parzellen/{request.ParzelleId.GetValueOrDefault()}/";
        }

        private static bool IsValidDokumentStorageContract(string? storagePath, string? dateiname, DokumentUploadRequest request)
        {
            var normalizedStoragePath = CleanOptionalText(storagePath)?.Replace('\\', '/').TrimStart('/') ?? string.Empty;
            var normalizedDateiname = CleanOptionalText(dateiname) ?? string.Empty;
            var expectedPrefix = BuildExpectedDokumentStoragePrefix(request);
            if (string.IsNullOrWhiteSpace(normalizedStoragePath)
                || string.IsNullOrWhiteSpace(normalizedDateiname)
                || !normalizedStoragePath.StartsWith(expectedPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            var segments = normalizedStoragePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.Length == 5
                && string.Equals(segments[4], normalizedDateiname, StringComparison.Ordinal)
                && Regex.IsMatch(normalizedDateiname, @"^.+_\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}\.[^./\\]+$", RegexOptions.CultureInvariant);
        }

        private async Task<DokumentDeleteResult> DeleteDokumentFromDriveAsync(string driveFileId)
        {
            var token = await _authService.GetAccessTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
                return DokumentDeleteResult.Fail("Die aktuelle Anmeldung ist abgelaufen. Bitte erneut anmelden.", "DELETE_AUTH_TOKEN_MISSING");

            if (string.IsNullOrWhiteSpace(_supabaseUrl) || string.IsNullOrWhiteSpace(_publishableKey))
                return DokumentDeleteResult.Fail("Supabase-URL oder Publishable Key ist nicht konfiguriert.", "DELETE_CONFIG_MISSING");

            var endpoint = new Uri(new Uri(_supabaseUrl.TrimEnd('/') + "/"), $"functions/v1/{DokumentUploadFunctionName}");

            try
            {
                using var message = new HttpRequestMessage(HttpMethod.Delete, endpoint)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { drive_file_id = driveFileId }))
                };
                message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                message.Headers.Add("apikey", _publishableKey);

                using var response = await _documentUploadHttpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);
                var rawBody = await response.Content.ReadAsStringAsync();
                var deleteResponse = DeserializeDokumentUploadResponse(rawBody);
                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogWarning(
                        "DeleteDokumentFromDriveAsync failed. StatusCode={StatusCode} DiagnosticCode={DiagnosticCode} RequestId={RequestId}",
                        (int)response.StatusCode,
                        deleteResponse?.ErrorCode,
                        deleteResponse?.RequestId);

                    return DokumentDeleteResult.Fail(
                        deleteResponse?.Message ?? "Dokumentdatei konnte aktuell nicht entfernt werden.",
                        deleteResponse?.ErrorCode ?? $"DELETE_HTTP_{(int)response.StatusCode}",
                        deleteResponse?.RequestId);
                }

                return DokumentDeleteResult.Ok(deleteResponse?.RequestId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "DeleteDokumentFromDriveAsync transport failure.");
                return DokumentDeleteResult.Fail("Dokumentdatei konnte aktuell nicht entfernt werden.", "DELETE_SEND_FAIL");
            }
        }

        private async Task<DokumentUploadResult> UploadDokumentToDriveAsync(DokumentUploadRequest request, string normalizedTitle)
        {
            var token = await _authService.GetAccessTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
                return DokumentUploadResult.Fail("Die aktuelle Anmeldung ist abgelaufen. Bitte erneut anmelden.", "UPLOAD_AUTH_TOKEN_MISSING");

            if (string.IsNullOrWhiteSpace(_supabaseUrl) || string.IsNullOrWhiteSpace(_publishableKey))
                return DokumentUploadResult.Fail("Supabase-URL oder Publishable Key ist nicht konfiguriert.", "UPLOAD_CONFIG_MISSING");

            var ownerKind = request.MitgliedId.HasValue && request.MitgliedId.Value > 0 ? "mitglied" : "parzelle";
            var ownerId = request.MitgliedId.HasValue && request.MitgliedId.Value > 0
                ? request.MitgliedId.Value
                : request.ParzelleId.GetValueOrDefault();

            var endpoint = new Uri(new Uri(_supabaseUrl.TrimEnd('/') + "/"), $"functions/v1/{DokumentUploadFunctionName}");

            try
            {
                using var multipart = new MultipartFormDataContent();
                var fileName = string.IsNullOrWhiteSpace(request.FileName) ? "dokument.bin" : request.FileName.Trim();
                var contentType = string.IsNullOrWhiteSpace(request.MimeType) ? "application/octet-stream" : request.MimeType.Trim();
                var fileContent = new ByteArrayContent(request.FileContent);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

                multipart.Add(fileContent, "file", fileName);
                multipart.Add(new StringContent(ownerKind), "owner_kind");
                multipart.Add(new StringContent(ownerId.ToString()), "owner_id");
                multipart.Add(new StringContent(normalizedTitle), "titel");

                using var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = multipart
                };
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                message.Headers.Add("apikey", _publishableKey);

                using var response = await _documentUploadHttpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);
                var rawBody = await response.Content.ReadAsStringAsync();
                var uploadResponse = DeserializeDokumentUploadResponse(rawBody);

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogWarning(
                        "UploadDokumentToDriveAsync failed. StatusCode={StatusCode} DiagnosticCode={DiagnosticCode} RequestId={RequestId}",
                        (int)response.StatusCode,
                        uploadResponse?.ErrorCode,
                        uploadResponse?.RequestId);

                    return DokumentUploadResult.Fail(
                        uploadResponse?.Message ?? "Dokument-Upload wurde vom Server abgelehnt.",
                        uploadResponse?.ErrorCode ?? $"UPLOAD_HTTP_{(int)response.StatusCode}",
                        uploadResponse?.RequestId);
                }

                if (uploadResponse == null
                    || string.IsNullOrWhiteSpace(uploadResponse.DriveFileId)
                    || string.IsNullOrWhiteSpace(uploadResponse.StoragePath)
                    || string.IsNullOrWhiteSpace(uploadResponse.Dateiname))
                {
                    _logger?.LogWarning("UploadDokumentToDriveAsync returned incomplete payload. RawBodyLength={RawBodyLength}", rawBody?.Length ?? 0);
                    return DokumentUploadResult.Fail("Dokument-Upload lieferte keine vollständigen Metadaten zurück.", "UPLOAD_RESPONSE_INVALID", uploadResponse?.RequestId);
                }

                if (!IsValidDokumentStorageContract(uploadResponse.StoragePath, uploadResponse.Dateiname, request))
                {
                    _logger?.LogWarning(
                        "UploadDokumentToDriveAsync returned unexpected storage contract. StoragePath={StoragePath} Dateiname={Dateiname}",
                        uploadResponse.StoragePath,
                        uploadResponse.Dateiname);
                    return DokumentUploadResult.Fail("Dokument-Upload lieferte einen ungültigen Pfadvertrag zurück.", "UPLOAD_RESPONSE_PATH_INVALID", uploadResponse.RequestId);
                }

                return DokumentUploadResult.Ok(
                    new DocumentInfo
                    {
                        Title = normalizedTitle,
                        Name = normalizedTitle,
                        StoragePath = uploadResponse.StoragePath.Trim(),
                        Dateiname = uploadResponse.Dateiname.Trim(),
                        MimeType = CleanOptionalText(uploadResponse.MimeType) ?? contentType,
                        Size = uploadResponse.SizeBytes,
                        DriveFileId = uploadResponse.DriveFileId.Trim()
                    },
                    uploadResponse.RequestId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "UploadDokumentToDriveAsync transport failure.");
                return DokumentUploadResult.Fail("Dokument-Upload ist aktuell nicht erreichbar.", "UPLOAD_SEND_FAIL");
            }
        }

        private Guid? ResolveCurrentAuthUserId()
        {
            var userContext = _currentUserContextAccessor?.Invoke();
            if (userContext != null && userContext.UserId != Guid.Empty)
                return userContext.UserId;

            return Guid.TryParse(_authService.CurrentUserId, out var authUserId)
                ? authUserId
                : null;
        }

        private async Task<DocumentInfo?> ReloadInsertedDokumentAsync(Client client, DokumentInsertRecord insertRecord)
        {
            var response = await client.From<DokumentRecord>().Get();
            var models = response?.Models;
            if (models == null)
                return null;

            return models
                .Where(x => !insertRecord.MitgliedId.HasValue || x.MitgliedId == insertRecord.MitgliedId.Value)
                .Where(x => !insertRecord.ParzelleId.HasValue || x.ParzelleId == insertRecord.ParzelleId.Value)
                .Where(x => IsSameDokumentForReload(x, insertRecord))
                .OrderByDescending(x => x.Id)
                .Select(MapDocumentInfo)
                .FirstOrDefault();
        }

        private async Task<DokumentRecord?> LoadDokumentRecordByIdAsync(Client client, long dokumentId)
        {
            var response = await client
                .From<DokumentRecord>()
                .Where(x => x.Id == dokumentId)
                .Get();

            return response?.Models?.FirstOrDefault();
        }

        private static bool IsSameDokumentForReload(DokumentRecord existing, DokumentInsertRecord candidate)
        {
            return existing.MitgliedId == candidate.MitgliedId
                && existing.ParzelleId == candidate.ParzelleId
                && string.Equals(CleanOptionalText(existing.StoragePath), CleanOptionalText(candidate.StoragePath), StringComparison.CurrentCulture)
                && string.Equals(CleanOptionalText(existing.Titel), CleanOptionalText(candidate.Titel), StringComparison.CurrentCulture)
                && string.Equals(CleanOptionalText(existing.Dateiname), CleanOptionalText(candidate.Dateiname), StringComparison.CurrentCulture)
                && string.Equals(CleanOptionalText(existing.MimeType), CleanOptionalText(candidate.MimeType), StringComparison.CurrentCulture)
                && existing.SizeBytes == candidate.SizeBytes
                && string.Equals(CleanOptionalText(existing.DriveFileId), CleanOptionalText(candidate.DriveFileId), StringComparison.CurrentCulture)
                && existing.CreatedBy == candidate.CreatedBy;
        }

        private static DokumentUploadFunctionResponse? DeserializeDokumentUploadResponse(string? rawBody)
        {
            if (string.IsNullOrWhiteSpace(rawBody))
                return null;

            try
            {
                return JsonSerializer.Deserialize<DokumentUploadFunctionResponse>(rawBody);
            }
            catch
            {
                return null;
            }
        }

        private sealed class DokumentUploadFunctionResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("message")]
            public string? Message { get; set; }

            [JsonPropertyName("request_id")]
            public string? RequestId { get; set; }

            [JsonPropertyName("error_code")]
            public string? ErrorCode { get; set; }

            [JsonPropertyName("drive_file_id")]
            public string? DriveFileId { get; set; }

            [JsonPropertyName("fileId")]
            public string? DriveFileIdAlias
            {
                get => DriveFileId;
                set => DriveFileId ??= value;
            }

            [JsonPropertyName("storage_path")]
            public string? StoragePath { get; set; }

            [JsonPropertyName("relativePath")]
            public string? StoragePathAlias
            {
                get => StoragePath;
                set => StoragePath ??= value;
            }

            [JsonPropertyName("dateiname")]
            public string? Dateiname { get; set; }

            [JsonPropertyName("fileName")]
            public string? DateinameAlias
            {
                get => Dateiname;
                set => Dateiname ??= value;
            }

            [JsonPropertyName("mime_type")]
            public string? MimeType { get; set; }

            [JsonPropertyName("mimeType")]
            public string? MimeTypeAlias
            {
                get => MimeType;
                set => MimeType ??= value;
            }

            [JsonPropertyName("size_bytes")]
            public long? SizeBytes { get; set; }

            [JsonPropertyName("sizeBytes")]
            public long? SizeBytesAlias
            {
                get => SizeBytes;
                set => SizeBytes ??= value;
            }
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
