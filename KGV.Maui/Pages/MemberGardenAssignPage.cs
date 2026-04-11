using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class MemberGardenAssignPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly MemberContextState _memberContextState;
    private readonly UserContextState _userContextState;
    private readonly Label _headlineLabel;
    private readonly Label _hintLabel;
    private readonly Label _statusLabel;
    private readonly Picker _parzellePicker;
    private readonly DatePicker _assignDatePicker;
    private readonly Button _cancelButton;
    private readonly Button _saveButton;

    private readonly List<ParzelleRecord> _availableParzellen = new();
    private bool _isBusy;
    private MitgliedRecord? _memberRecord;

    public MemberGardenAssignPage(
        ISupabaseService supabaseService,
        MemberContextState memberContextState,
        UserContextState userContextState)
    {
        _supabaseService = supabaseService;
        _memberContextState = memberContextState;
        _userContextState = userContextState;

        Title = "Parzelle zuordnen";

        _headlineLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold };
        _hintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _statusLabel = new Label { TextColor = Colors.DarkRed, LineBreakMode = LineBreakMode.WordWrap };
        _parzellePicker = new Picker { Title = "Parzelle wählen" };
        _assignDatePicker = new DatePicker { Date = DateTime.Today };
        _cancelButton = new Button { Text = "Abbrechen" };
        _cancelButton.Clicked += async (_, _) => await Navigation.PopAsync();
        _saveButton = new Button { Text = "Parzelle zuweisen" };
        _saveButton.Clicked += async (_, _) => await SaveAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 14,
                Children =
                {
                    _headlineLabel,
                    _hintLabel,
                    _statusLabel,
                    CreateSection(
                        "Parzellenzuweisung",
                        CreateEditorField("Parzelle", _parzellePicker),
                        CreateEditorField("Zuweisung ab", _assignDatePicker),
                        new HorizontalStackLayout
                        {
                            Spacing = 12,
                            Children = { _cancelButton, _saveButton }
                        })
                }
            }
        };

        Appearing += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isBusy)
            return;

        _isBusy = true;
        try
        {
            _statusLabel.Text = string.Empty;
            _parzellePicker.Items.Clear();
            _availableParzellen.Clear();

            if (!PermissionChecks.CanCreateMitglied(_userContextState.CurrentUserContext))
            {
                _headlineLabel.Text = "Parzelle zuordnen";
                _hintLabel.Text = "Parzellenzuweisungen sind mobil nur mit dem Fachrecht 'CreateMitglied' oder als Admin/Vorstand freigegeben.";
                _statusLabel.Text = "Keine Berechtigung für Parzellenzuweisungen.";
                UpdateUiState();
                return;
            }

            var selectedMember = _memberContextState.SelectedMember;
            if (selectedMember?.Id is not > 0)
            {
                _headlineLabel.Text = "Kein Mitglied ausgewählt";
                _hintLabel.Text = "Bitte zuerst im Mitgliedskontext ein Mitglied auswählen.";
                _statusLabel.Text = "Parzellenzuweisung ist ohne Mitgliedskontext nicht möglich.";
                UpdateUiState();
                return;
            }

            _memberRecord = await _supabaseService.GetMitgliedByIdAsync(selectedMember.Id);
            if (_memberRecord == null)
            {
                _headlineLabel.Text = "Mitglied nicht gefunden";
                _hintLabel.Text = "Das aktuell ausgewählte Mitglied konnte nicht geladen werden.";
                _statusLabel.Text = "Parzellenzuweisung ist aktuell nicht möglich.";
                UpdateUiState();
                return;
            }

            _headlineLabel.Text = string.IsNullOrWhiteSpace(selectedMember.DisplayName)
                ? $"Parzelle zuordnen für Mitglied #{selectedMember.Id}"
                : $"Parzelle zuordnen für {selectedMember.DisplayName}";
            _hintLabel.Text = "Die Zuweisung verwendet den bestehenden Produktivpfad. Nach erfolgreicher Zuweisung kann direkt ein Pachtvertrag erzeugt werden.";

            var parzellen = await _supabaseService.GetAllParzellenAsync();
            var allBelegungen = await _supabaseService.GetAllParzellenBelegungenAsync();
            var today = DateTime.Today;
            var activeToday = (allBelegungen ?? new List<ParzellenBelegungRecord>())
                .GroupBy(x => x.ParzelleId)
                .Select(g => g.Where(x =>
                        (x.VonDatum ?? DateTime.MinValue).Date <= today
                        && (x.BisDatum == null || x.BisDatum.Value.Date >= today))
                    .OrderByDescending(x => x.VonDatum ?? DateTime.MinValue)
                    .FirstOrDefault())
                .Where(x => x != null)
                .ToDictionary(x => x!.ParzelleId, x => x!);

            foreach (var parzelle in (parzellen ?? new List<ParzelleRecord>())
                         .Where(x => x.Id > 0 && !activeToday.ContainsKey(x.Id))
                         .OrderBy(x => GetGartenNrSortKey(x.GartenNr))
                         .ThenBy(x => x.GartenNr, StringComparer.CurrentCultureIgnoreCase))
            {
                _availableParzellen.Add(parzelle);
                _parzellePicker.Items.Add(BuildParzelleDisplayText(parzelle));
            }

            if (_availableParzellen.Count == 0)
                _statusLabel.Text = "Aktuell ist keine freie Parzelle für eine neue Zuweisung verfügbar.";
            else
                _parzellePicker.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
        finally
        {
            _isBusy = false;
            UpdateUiState();
        }
    }

    private async Task SaveAsync()
    {
        if (_isBusy)
            return;

        if (_memberRecord?.Id is not > 0)
        {
            await DisplayAlert("Parzellenzuweisung", "Bitte zuerst ein gültiges Mitglied auswählen.", "OK");
            return;
        }

        if (!PermissionChecks.CanCreateMitglied(_userContextState.CurrentUserContext))
        {
            await DisplayAlert("Parzellenzuweisung", "Parzellenzuweisungen sind mobil nur mit dem Fachrecht 'CreateMitglied' oder als Admin/Vorstand freigegeben.", "OK");
            return;
        }

        if (_parzellePicker.SelectedIndex < 0 || _parzellePicker.SelectedIndex >= _availableParzellen.Count)
        {
            await DisplayAlert("Parzellenzuweisung", "Bitte zuerst eine Parzelle auswählen.", "OK");
            return;
        }

        var selectedParzelle = _availableParzellen[_parzellePicker.SelectedIndex];
        var assignDate = _assignDatePicker.Date.Date;

        _isBusy = true;
        UpdateUiState();
        try
        {
            var ok = await _supabaseService.AssignParzelleToMitgliedAsync(_memberRecord.Id, selectedParzelle.Id, assignDate);
            if (!ok)
            {
                await DisplayAlert("Parzellenzuweisung", "Zuweisung fehlgeschlagen. Der Datensatz konnte nicht gespeichert werden.", "OK");
                return;
            }

            var createContract = await DisplayAlert(
                "Pachtvertrag",
                "Parzelle zugewiesen. Pachtvertrag erstellen?",
                "Ja",
                "Nein");

            if (createContract)
                await CreatePachtvertragAsync(_memberRecord.Id, selectedParzelle.Id, assignDate);

            await DisplayAlert("OK", "Parzelle wurde zugewiesen.", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Parzellenzuweisung", ex.Message, "OK");
        }
        finally
        {
            _isBusy = false;
            UpdateUiState();
        }
    }

    private async Task CreatePachtvertragAsync(int mitgliedId, int parzelleId, DateTime vertragsbeginn)
    {
        var result = await _supabaseService.CreatePachtvertragDokumentAsync(
            mitgliedId,
            parzelleId,
            vertragsbeginn,
            FormularDokumentStatus.Unsigniert);

        if (!result.Success)
        {
            await DisplayAlert("Pachtvertrag", result.Message, "OK");
            return;
        }

        var document = result.Document;
        if (document?.CanOpen != true)
        {
            await DisplayAlert("Pachtvertrag", "Pachtvertrag wurde als Dokument abgelegt.", "OK");
            return;
        }

        var url = await _supabaseService.ResolveDokumentOpenUrlAsync(document, 3600);
        if (string.IsNullOrWhiteSpace(url))
        {
            await DisplayAlert("Pachtvertrag", "Pachtvertrag wurde gespeichert, konnte aber nicht direkt geöffnet werden.", "OK");
            return;
        }

        await Launcher.Default.OpenAsync(url);
    }

    private void UpdateUiState()
    {
        var hasParzellen = _availableParzellen.Count > 0;
        var canEdit = !_isBusy
            && _memberRecord?.Id is > 0
            && PermissionChecks.CanCreateMitglied(_userContextState.CurrentUserContext);

        _parzellePicker.IsEnabled = canEdit && hasParzellen;
        _assignDatePicker.IsEnabled = canEdit;
        _saveButton.IsEnabled = canEdit && hasParzellen;
        _cancelButton.IsEnabled = !_isBusy;
    }

    private static string BuildParzelleDisplayText(ParzelleRecord parzelle)
    {
        var gartenNr = string.IsNullOrWhiteSpace(parzelle.GartenNr) ? $"#{parzelle.Id}" : parzelle.GartenNr.Trim();
        var anlage = string.IsNullOrWhiteSpace(parzelle.Anlage) ? "-" : parzelle.Anlage.Trim();
        return $"Garten {gartenNr} ({anlage})";
    }

    private static string GetGartenNrSortKey(string? gartenNr)
    {
        if (string.IsNullOrWhiteSpace(gartenNr))
            return "999999";

        return int.TryParse(gartenNr.Trim(), out var numericValue)
            ? numericValue.ToString("D6")
            : gartenNr.Trim();
    }

    private static Border CreateSection(string title, params View[] children)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Children.Add(new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 18 });
        foreach (var child in children)
            stack.Children.Add(child);

        return new Border
        {
            Padding = 16,
            Stroke = Colors.LightGray,
            Content = stack
        };
    }

    private static View CreateEditorField(string title, View editor)
    {
        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold },
                editor
            }
        };
    }
}
