using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Security;
using KGV.Maui.Services;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Newtonsoft.Json;
using Supabase;

namespace KGV.Maui.Pages;

// AP-02.3C: unabhängige MAUI-Verwaltung des neuen Schattenmodells.
// Durchsetzung / Audit erfolgen ausschließlich in den ap02_* RPCs der DemoDB.
// Die bestehenden produktiven App-Rechte und Menüs werden NICHT überschrieben.
public sealed class FachrechteV2AdministrationPage : ContentPage
{
    public sealed class Account
    {
        [JsonProperty("user_id")] public Guid UserId { get; set; }
        [JsonProperty("mitglied_id")] public long? MitgliedId { get; set; }
        [JsonProperty("legacy_role")] public string? LegacyRole { get; set; }
        [JsonProperty("hauptrolle_code")] public string? HauptrolleCode { get; set; }
        [JsonProperty("zusatzrollen")] public string[]? Zusatzrollen { get; set; }
        [JsonProperty("anzahl_ausnahmen")] public int AnzahlAusnahmen { get; set; }
        [JsonProperty("ist_geschuetzt")] public bool IstGeschuetzt { get; set; }

        public string Display => $"Mitglied {MitgliedId?.ToString() ?? "–"} · {HauptrolleCode ?? "ohne Hauptrolle"} · {UserId.ToString("N")[..8]}";
    }

    private const string RolesPermission = "berechtigungen.verwalten";

    private static readonly (string Code, string Label)[] MainRoles =
    {
        ("mitglied", "Mitglied"),
        ("bauausschuss", "Bauausschuss"),
        ("vorstand", "Vorstand"),
        ("vorstand_schriftfuehrer", "Vorstand-Schriftführer"),
        ("vorstand_kassenwart", "Vorstand-Kassenwart"),
    };

    private static readonly (string Code, string Label)[] Groups =
    {
        ("A", "Mitglieder & Mitgliedschaft"),
        ("B", "Parzellen, Pacht & Wertermittlung"),
        ("C", "Zähler & Ablesungen"),
        ("D", "Finanzen & Jahresabschluss"),
        ("E", "Arbeitsstunden & Arbeitseinsätze"),
        ("F", "Dokumente & Vorlagen"),
        ("G", "Verträge, Vorgänge & Kommunikation"),
        ("H", "Vereinsorganisation"),
        ("I", "Auswertungen & Export"),
        ("J", "Benutzer & Systemsicherheit"),
    };

    private static readonly HashSet<string> SystemRights = new(StringComparer.Ordinal)
    {
        "benutzer.lesen", "benutzer.verwalten", "berechtigungen.lesen",
        "berechtigungen.verwalten", "vereinsdaten.alle_lesen",
    };

    private static readonly HashSet<string> PendingClosingRights = new(StringComparer.Ordinal)
    {
        "jahresabschluss.finalisieren", "jahresabschluss.korrigieren",
    };

    private static readonly HashSet<string> KnownRights = typeof(FachrechteV2)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.IsLiteral && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToHashSet(StringComparer.Ordinal);

    private readonly ISupabaseClientFactory _clientFactory;
    private readonly long? _initialMemberId;
    private readonly Picker _accountPicker = new() { Title = "App-Benutzer" };
    private readonly Picker _rolePicker = new() { Title = "Hauptrolle" };
    private readonly Entry _reason = new() { Placeholder = "Begründung (mindestens drei Zeichen)", MaxLength = 250 };
    private readonly Label _status = new() { LineBreakMode = LineBreakMode.WordWrap, TextColor = Colors.DarkSlateBlue };
    private readonly Label _accountInfo = new() { LineBreakMode = LineBreakMode.WordWrap };
    private readonly Button _saveMainRoleButton = new() { Text = "Hauptrolle speichern" };
    private readonly Button _adminButton = new();
    private readonly Button _valuationButton = new();
    private readonly VerticalStackLayout _rightsArea = new() { Spacing = 10 };
    private readonly List<Account> _accounts = new();
    private readonly List<Action> _refreshRightButtons = new();
    private Account? _selected;
    private bool _busy;
    private bool _populating;

    public FachrechteV2AdministrationPage(ISupabaseClientFactory clientFactory, long? initialMemberId = null)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _initialMemberId = initialMemberId;
        Title = "Fachrechte 2.0";
        _accountPicker.SelectedIndexChanged += async (_, _) =>
        {
            if (!_populating) await LoadSelectedAsync();
        };
        _rolePicker.ItemsSource = MainRoles.Select(r => r.Label).ToList();
        _saveMainRoleButton.Clicked += async (_, _) => await SaveMainRoleAsync();
        _adminButton.Clicked += async (_, _) => await ChangeExtraRoleAsync("admin");
        _valuationButton.Clicked += async (_, _) => await ChangeExtraRoleAsync("wertermittlung");
        _reason.TextChanged += (_, _) => UpdateButtons();
        _rolePicker.SelectedIndexChanged += (_, _) => UpdateButtons();

        var refresh = new Button { Text = "Vom Server aktualisieren" };
        refresh.Clicked += async (_, _) => await LoadAccountsAsync();
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 22,
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Fachrechte 2.0", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    new Label { Text = "Neues Rechtesystem im Schattenbetrieb: Bestehende Funktionen verwenden bis zur serverseitigen Umstellung noch die bisherigen Rechte.", LineBreakMode = LineBreakMode.WordWrap },
                    _status,
                    refresh,
                    _accountPicker,
                    _accountInfo,
                    _reason,
                    new Label { Text = "Hauptrolle", FontAttributes = FontAttributes.Bold },
                    _rolePicker,
                    _saveMainRoleButton,
                    new Label { Text = "Zusatzrollen", FontAttributes = FontAttributes.Bold },
                    _valuationButton,
                    _adminButton,
                    new Label { Text = "Individuelle Fachrechte – 10 Gruppen", FontAttributes = FontAttributes.Bold, FontSize = 19 },
                    new Label { Text = "Entziehen hat Vorrang. Systemrechte nur über Admin. Die Abschlussrechte D5/D6 bleiben bis zur Fachentscheidung für neue Gewährungen gesperrt.", TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap },
                    _rightsArea,
                }
            }
        };
        UpdateButtons();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAccountsAsync();
    }

    private bool HasReason => (_reason.Text?.Trim().Length ?? 0) >= 3;
    private string Reason => _reason.Text!.Trim();

    private void UpdateButtons()
    {
        var ready = !_busy && _selected != null && HasReason;
        var roleIndex = _rolePicker.SelectedIndex;
        _saveMainRoleButton.IsEnabled = ready && roleIndex >= 0 && roleIndex < MainRoles.Length
            && _selected!.HauptrolleCode != MainRoles[roleIndex].Code;

        var extras = _selected?.Zusatzrollen ?? Array.Empty<string>();
        var adminActive = extras.Contains("admin", StringComparer.Ordinal);
        var valuationActive = extras.Contains("wertermittlung", StringComparer.Ordinal);
        _adminButton.Text = adminActive ? "Admin-Zusatzrolle entziehen" : "Admin-Zusatzrolle zuweisen";
        _adminButton.IsEnabled = ready && !(adminActive && _selected!.IstGeschuetzt)
            && (adminActive || _selected!.HauptrolleCode != null);
        _valuationButton.Text = valuationActive ? "Wertermittlung entziehen" : "Wertermittlung zuweisen";
        _valuationButton.IsEnabled = ready && (valuationActive || _selected!.HauptrolleCode == "mitglied"
            || _selected!.HauptrolleCode == "bauausschuss");
        foreach (var refresh in _refreshRightButtons) refresh();
    }

    private async Task LoadAccountsAsync()
    {
        if (_busy) return;
        _busy = true;
        _status.Text = "Lade Benutzerverwaltung …";
        UpdateButtons();
        try
        {
            Client client = await _clientFactory.CreateAsync();
            var rows = await client.Rpc<Account[]>("ap02_list_user_access", new { })
                ?? Array.Empty<Account>();
            if (rows.Any(r => r == null || r.UserId == Guid.Empty))
                throw new InvalidOperationException("Ungültige Benutzerliste.");
            var prior = _selected?.UserId;
            _accounts.Clear();
            _accounts.AddRange(rows);
            _populating = true;
            _accountPicker.ItemsSource = _accounts.Select(a => a.Display).ToList();
            var preferred = prior.HasValue ? _accounts.FindIndex(a => a.UserId == prior.Value) : -1;
            if (preferred < 0 && _initialMemberId.HasValue)
                preferred = _accounts.FindIndex(a => a.MitgliedId == _initialMemberId.Value);
            _accountPicker.SelectedIndex = preferred >= 0 ? preferred : (_accounts.Count > 0 ? 0 : -1);
            _populating = false;
            _status.Text = _accounts.Count == 0 ? "Keine verknüpften App-Benutzer vorhanden." : "Benutzer geladen.";
        }
        catch (Exception ex)
        {
            _populating = false;
            _accounts.Clear();
            _selected = null;
            _rightsArea.Children.Clear();
            _refreshRightButtons.Clear();
            _accountPicker.ItemsSource = null;
            _status.Text = "Fachrechte 2.0 nicht verfügbar oder keine Admin-Berechtigung: " + ex.Message;
        }
        finally
        {
            _busy = false;
            UpdateButtons();
        }
        if (_accounts.Count > 0)
            await LoadSelectedAsync();
    }

    private async Task LoadSelectedAsync()
    {
        if (_busy) return;
        _busy = true;
        UpdateButtons();
        _rightsArea.Children.Clear();
        _refreshRightButtons.Clear();
        var index = _accountPicker.SelectedIndex;
        _selected = index >= 0 && index < _accounts.Count ? _accounts[index] : null;
        if (_selected == null)
        {
            _busy = false;
            UpdateButtons();
            return;
        }
        _status.Text = "Berechne effektive Fachrechte …";
        try
        {
            Client client = await _clientFactory.CreateAsync();
            var rights = await client.Rpc<FachrechtV2Record[]>("ap02_admin_preview_user_rights",
                new { p_user_id = _selected.UserId }) ?? Array.Empty<FachrechtV2Record>();
            if (rights.Length != KnownRights.Count
                || rights.Any(r => r == null || r.Code == null || !KnownRights.Contains(r.Code)
                    || r.Gruppe == null || r.Titel == null || r.Quellen == null)
                || rights.Select(r => r.Code).Distinct(StringComparer.Ordinal).Count() != KnownRights.Count)
                throw new InvalidOperationException("Fachrechte-Antwort weicht vom Katalog ab.");

            var extra = _selected.Zusatzrollen ?? Array.Empty<string>();
            _accountInfo.Text = $"Hauptrolle: {_selected.HauptrolleCode ?? "noch nicht zugeteilt"} · {rights.Count(r => r.Erlaubt)}/55 erlaubt · {_selected.AnzahlAusnahmen} Ausnahmen" + (_selected.IstGeschuetzt ? " · geschütztes Admin-Konto" : "");
            _rolePicker.SelectedIndex = Array.FindIndex(MainRoles, r => r.Code == _selected.HauptrolleCode);
            foreach (var group in Groups)
            {
                var groupRows = rights.Where(r => r.Gruppe == group.Code).OrderBy(r => r.Code).ToArray();
                var panel = new VerticalStackLayout { Spacing = 8, IsVisible = false };
                var toggle = new Button
                {
                    Text = $"{group.Code} · {group.Label} ({groupRows.Count(r => r.Erlaubt)}/{groupRows.Length}) ▸",
                    BackgroundColor = Colors.LightGray,
                    TextColor = Colors.Black,
                };
                toggle.Clicked += (_, _) => panel.IsVisible = !panel.IsVisible;
                _rightsArea.Children.Add(toggle);
                _rightsArea.Children.Add(panel);
                foreach (var right in groupRows)
                    AddRightEditor(panel, right);
            }
            _status.Text = "Fachrechte vom Server geladen. Änderungen benötigen eine Begründung.";
        }
        catch (Exception ex)
        {
            _rightsArea.Children.Clear();
            _refreshRightButtons.Clear();
            _status.Text = "Fachrechte konnten nicht geladen werden: " + ex.Message;
        }
        finally
        {
            _busy = false;
            UpdateButtons();
        }
    }

    private void AddRightEditor(VerticalStackLayout host, FachrechtV2Record right)
    {
        var current = right.IndividuellEntzogen ? "deny"
            : right.Quellen?.Contains("individuelle_gewaehrung", StringComparer.Ordinal) == true ? "grant" : "inherit";
        var forbidGrant = SystemRights.Contains(right.Code!) || PendingClosingRights.Contains(right.Code!);
        var forbidDeny = _selected!.IstGeschuetzt && right.Code == RolesPermission;
        var allowed = new List<string> { "inherit" };
        if (!forbidGrant) allowed.Add("grant");
        if (!forbidDeny) allowed.Add("deny");
        var picker = new Picker { Title = "Individuelle Ausnahme", ItemsSource = allowed };
        picker.SelectedItem = allowed.Contains(current) ? current : "inherit";
        var save = new Button { Text = "Ausnahme speichern" };
        save.Clicked += async (_, _) =>
        {
            if (picker.SelectedItem is not string decision || decision == current) return;
            await ExecuteChangeAsync(client => client.Rpc<bool>("ap02_set_permission_override",
                new { p_user_id = _selected!.UserId, p_fachrecht = right.Code,
                      p_entscheidung = decision, p_grund = Reason }));
        };
        host.Children.Add(new Border
        {
            Padding = 10,
            Stroke = Colors.LightGray,
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = right.Titel, FontAttributes = FontAttributes.Bold },
                    new Label { Text = right.Code, FontSize = 11, TextColor = Colors.Gray },
                    new Label { Text = (right.Erlaubt ? "Erlaubt" : right.IndividuellEntzogen ? "Entzogen" : "Nicht erlaubt")
                        + " · " + (right.Quellen?.Length > 0 ? string.Join(", ", right.Quellen) : "kein Grundrecht"), FontSize = 12 },
                    picker,
                    save,
                }
            }
        });
        void RefreshSave() => save.IsEnabled = !_busy && HasReason
            && picker.SelectedItem is string selection && selection != current;
        picker.SelectedIndexChanged += (_, _) => RefreshSave();
        _refreshRightButtons.Add(RefreshSave);
        RefreshSave();
    }

    private async Task SaveMainRoleAsync()
    {
        if (_selected == null || _rolePicker.SelectedIndex < 0 || _rolePicker.SelectedIndex >= MainRoles.Length) return;
        await ExecuteChangeAsync(client => client.Rpc<bool>("ap02_assign_main_role",
            new { p_user_id = _selected.UserId,
                  p_hauptrolle = MainRoles[_rolePicker.SelectedIndex].Code, p_grund = Reason }));
    }

    private async Task ChangeExtraRoleAsync(string code)
    {
        if (_selected == null) return;
        var enabled = (_selected.Zusatzrollen ?? Array.Empty<string>()).Contains(code, StringComparer.Ordinal);
        await ExecuteChangeAsync(client => client.Rpc<bool>("ap02_set_additional_role",
            new { p_user_id = _selected.UserId, p_zusatzrolle = code, p_aktiv = !enabled, p_grund = Reason }));
    }

    private async Task ExecuteChangeAsync(Func<Client, Task> operation)
    {
        if (_busy || _selected == null || !HasReason) return;
        _busy = true;
        UpdateButtons();
        try
        {
            Client client = await _clientFactory.CreateAsync();
            await operation(client);
            _status.Text = "Vom Server gespeichert und auditiert.";
        }
        catch (Exception ex)
        {
            _status.Text = "Änderung abgelehnt: " + ex.Message;
            return;
        }
        finally
        {
            _busy = false;
            UpdateButtons();
        }
        await LoadAccountsAsync();
    }
}
