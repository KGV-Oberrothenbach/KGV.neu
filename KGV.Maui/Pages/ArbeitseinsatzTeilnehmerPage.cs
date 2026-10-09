using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Core.Utilities;
using KGV.Maui.State;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class ArbeitseinsatzTeilnehmerPage : ContentPage, IQueryAttributable
{
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private readonly Picker _memberPicker = new() { Title = "Aktives Mitglied auswählen" };
    private readonly VerticalStackLayout _items = new() { Spacing = 8 };
    private readonly Label _status = new() { IsVisible = false, LineBreakMode = LineBreakMode.WordWrap };
    private readonly Button _registerButton = new() { Text = "Anmelden" };
    private long _entryId;
    private bool _started;
    private bool _ended;
    private bool _assignmentActive;
    private int? _maxParticipants;
    private int _activeRegistrations;
    private decimal _defaultHours;
    private string _defaultWorkType = string.Empty;
    private bool _isBusy;
    private List<MitgliedRecord> _members = new();

    public ArbeitseinsatzTeilnehmerPage(ISupabaseService supabaseService, UserContextState userContextState)
    {
        _supabaseService = supabaseService;
        _userContextState = userContextState;
        Title = "Teilnehmerverwaltung";
        _registerButton.Clicked += async (_, _) => await RegisterAsync();
        Content = new ScrollView { Content = new VerticalStackLayout
        {
            Padding = 24, Spacing = 12,
            Children = { new Label { Text = "Teilnehmer", FontSize = 24, FontAttributes = FontAttributes.Bold }, _status, _memberPicker, _registerButton, _items }
        }};
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("entryId", out var value) && long.TryParse(value?.ToString(), out var entryId)) _entryId = entryId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isBusy || _entryId <= 0) return;
        _isBusy = true;
        try
        {
            if (!PermissionChecks.CanManageWorkAssignments(_userContextState.CurrentUserContext)) { ShowStatus("Keine Berechtigung.", true); return; }
            var assignments = await _supabaseService.GetArbeitseinsaetzeVerwaltungAsync();
            var assignment = assignments.FirstOrDefault(x => x.Id == _entryId);
            if (assignment == null) { ShowStatus("Arbeitseinsatz nicht gefunden.", true); return; }
            var participants = (await _supabaseService.GetArbeitseinsatzManagementParticipantsAsync((int)_entryId))
                .OrderBy(x => StatusRank(x.Status)).ThenBy(x => x.DisplayName, StringComparer.CurrentCulture).ToList();
            _assignmentActive = assignment.Aktiv;
            _maxParticipants = assignment.MaxTeilnehmer;
            _activeRegistrations = participants.Count(x => x.Status == "angemeldet");
            _started = Vereinszeit.Now >= assignment.Datum.Date.Add(assignment.StartUhrzeit ?? new TimeSpan(23, 59, 0));
            _ended = Vereinszeit.Now >= assignment.Datum.Date.Add(assignment.EndUhrzeit ?? assignment.StartUhrzeit ?? new TimeSpan(23, 59, 0));
            _defaultHours = assignment.StundenWert;
            _defaultWorkType = assignment.Titel ?? string.Empty;
            var registrationByMemberId = participants.ToDictionary(x => x.MitgliedId, x => x);
            _members = (await _supabaseService.GetMitgliederAsync())
                .Where(x => x.Aktiv && (!registrationByMemberId.TryGetValue(x.Id, out var registration) || registration.Status == "abgesagt"))
                .OrderBy(x => x.Name).ThenBy(x => x.Vorname).ToList();
            _memberPicker.ItemsSource = _members.Select(x => $"{x.Name}, {x.Vorname}").ToList();
            _memberPicker.IsEnabled = CanRegister;
            _registerButton.IsEnabled = CanRegister;
            _items.Children.Clear();
            foreach (var participant in participants) _items.Children.Add(CreateParticipantRow(participant));
            if (participants.Count == 0) _items.Children.Add(new Label { Text = "Noch keine Teilnehmer." });
            _status.IsVisible = false;
        }
        catch (Exception ex) { ShowStatus(ex.Message, true); }
        finally { _isBusy = false; }
    }

    private View CreateParticipantRow(WorkAssignmentManagementParticipantItem item)
    {
        var actions = new HorizontalStackLayout { Spacing = 8 };
        if (item.Status == "angemeldet") actions.Children.Add(ActionButton(_started ? "Nicht erschienen" : "Abmelden", item.MitgliedId, _started ? "nicht_erschienen" : "absagen"));
        if (item.Status == "abgesagt" && CanRegister) actions.Children.Add(ActionButton("Wieder anmelden", item.MitgliedId, "anmelden"));
        if (item.Status == "nicht_erschienen") actions.Children.Add(ActionButton("Als abgesagt kennzeichnen", item.MitgliedId, "absagen"));
        if (CanConfirmHours && item.Status != "teilgenommen") actions.Children.Add(ConfirmHoursButton(item));
        return new VerticalStackLayout { Spacing = 4, Children = { new Label { Text = item.DisplayName, FontAttributes = FontAttributes.Bold }, new Label { Text = StatusLabel(item.Status), TextColor = Colors.DimGray }, actions } };
    }

    private Button ActionButton(string text, int memberId, string action)
    {
        var button = new Button { Text = text };
        button.Clicked += async (_, _) => await ManageAsync(memberId, action);
        return button;
    }

    private Button ConfirmHoursButton(WorkAssignmentManagementParticipantItem item)
    {
        var button = new Button { Text = "Arbeitsstunden bestätigen" };
        button.Clicked += async (_, _) => await ConfirmHoursAsync(item);
        return button;
    }

    private async Task ConfirmHoursAsync(WorkAssignmentManagementParticipantItem item)
    {
        var hoursText = await DisplayPromptAsync("Arbeitsstunden bestätigen", "Stunden", initialValue: _defaultHours.ToString("0.##"));
        if (!decimal.TryParse(hoursText, out var hours) || hours <= 0) return;
        var workType = await DisplayPromptAsync("Arbeitsstunden bestätigen", "Art der Arbeit", initialValue: _defaultWorkType);
        if (_isBusy) return;
        _isBusy = true;
        try
        {
            var result = await _supabaseService.ConfirmArbeitseinsatzWorkHoursAsync(item.RegistrationId, hours, workType);
            ShowStatus(result.Message, !result.Success);
        }
        finally { _isBusy = false; }
        await LoadAsync();
    }

    private async Task RegisterAsync()
    {
        if (!CanRegister) { ShowStatus(RegistrationBlockedMessage, true); return; }
        if (_memberPicker.SelectedIndex < 0) { ShowStatus("Bitte ein Mitglied auswählen.", true); return; }
        await ManageAsync(_members[_memberPicker.SelectedIndex].Id, "anmelden");
    }

    private async Task ManageAsync(int memberId, string action)
    {
        if (_isBusy) return;
        _isBusy = true;
        try
        {
            var result = await _supabaseService.ManageArbeitseinsatzParticipantAsync((int)_entryId, memberId, action);
            ShowStatus(result.Message, !result.Success);
        }
        finally { _isBusy = false; }
        await LoadAsync();
    }

    private void ShowStatus(string text, bool error) { _status.Text = text; _status.TextColor = error ? Colors.IndianRed : Colors.DarkGreen; _status.IsVisible = true; }
    private bool CanRegister => _assignmentActive && !_started && (!_maxParticipants.HasValue || _activeRegistrations < _maxParticipants.Value);
    private bool CanConfirmHours => _ended && PermissionChecks.CanManageWorkAssignments(_userContextState.CurrentUserContext) && PermissionChecks.CanManageWorkHours(_userContextState.CurrentUserContext);
    private string RegistrationBlockedMessage => !_assignmentActive ? "Der Arbeitseinsatz ist abgesagt." : _started ? "Der Arbeitseinsatz hat bereits begonnen." : "Die Teilnehmerbegrenzung ist erreicht.";
    private static int StatusRank(string status) => status switch { "angemeldet" => 0, "teilgenommen" => 1, "nicht_erschienen" => 2, "abgesagt" => 3, _ => 4 };
    private static string StatusLabel(string status) => status switch { "angemeldet" => "Angemeldet", "teilgenommen" => "Teilgenommen", "nicht_erschienen" => "Nicht erschienen", "abgesagt" => "Abgesagt", _ => status };
}
