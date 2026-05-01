using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using KGV.Maui.Services.Diagnostics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

public sealed class ArbeitsstundenEditorPage : ContentPage, IQueryAttributable
{
    private readonly ISupabaseService _supabaseService;
    private readonly IAuthService _authService;
    private readonly UserContextState _state;
    private readonly MemberContextState _memberContextState;

    private readonly Label _headlineLabel;
    private readonly Label _descriptionLabel;
    private readonly Label _statusLabel;
    private readonly Label _readonlyHintLabel;
    private readonly VerticalStackLayout _editableSection;
    private readonly VerticalStackLayout _readonlySection;
    private readonly Picker _forWhomPicker;
    private readonly DatePicker _datePicker;
    private readonly Entry _hoursEntry;
    private readonly Editor _descEditor;
    private readonly Label _readonlyMemberLabel;
    private readonly Label _readonlyDateLabel;
    private readonly Label _readonlyHoursLabel;
    private readonly Label _readonlyDescriptionLabel;
    private readonly Label _readonlyStatusValueLabel;
    private readonly Label _readonlyApprovalLabel;
    private readonly Button _saveButton;
    private readonly Button _cancelButton;
    private readonly Button _backToOverviewButton;

    private readonly List<MemberOption> _memberOptions = new();

    private bool _isLoading;
    private bool _loadScheduled;
    private int? _entryId;
    private int? _currentSaisonId;
    private ArbeitsstundeDTO? _existingEntry;
    private bool _forceOwnContext;
    private bool _isReadOnly;

    public ArbeitsstundenEditorPage(ISupabaseService supabaseService, IAuthService authService, UserContextState state, MemberContextState memberContextState)
    {
        _supabaseService = supabaseService;
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _state = state;
        _memberContextState = memberContextState;

        Title = "Arbeitsstunden";

        _headlineLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap };
        _descriptionLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _statusLabel = new Label { TextColor = Colors.DarkRed, LineBreakMode = LineBreakMode.WordWrap };
        _readonlyHintLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };

        _forWhomPicker = new Picker { Title = "Für wen?" };
        _forWhomPicker.ItemDisplayBinding = new Binding(nameof(MemberOption.Display));

        _datePicker = new DatePicker { Date = DateTime.Today };
        _hoursEntry = new Entry { Placeholder = "Stunden (z.B. 2,5)", Keyboard = Keyboard.Numeric };
        _descEditor = new Editor { Placeholder = "Art der Arbeit", AutoSize = EditorAutoSizeOption.TextChanges, HeightRequest = 140 };

        _readonlyMemberLabel = CreateValueLabel();
        _readonlyDateLabel = CreateValueLabel();
        _readonlyHoursLabel = CreateValueLabel();
        _readonlyDescriptionLabel = CreateValueLabel();
        _readonlyStatusValueLabel = CreateValueLabel();
        _readonlyApprovalLabel = CreateValueLabel();

        _saveButton = new Button { Text = "Speichern" };
        _saveButton.Clicked += async (_, _) => await SaveAsync();

        _cancelButton = new Button { Text = "Abbrechen" };
        _cancelButton.Clicked += async (_, _) => await NavigateToOverviewAsync();

        _backToOverviewButton = new Button { Text = "Zur Übersicht" };
        _backToOverviewButton.Clicked += async (_, _) => await NavigateToOverviewAsync();

        _editableSection = new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                new Label { Text = "Mitglied", FontAttributes = FontAttributes.Bold },
                _forWhomPicker,
                new Label { Text = "Datum", FontAttributes = FontAttributes.Bold },
                _datePicker,
                new Label { Text = "Stunden", FontAttributes = FontAttributes.Bold },
                _hoursEntry,
                new Label { Text = "Art der Arbeit", FontAttributes = FontAttributes.Bold },
                _descEditor,
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children = { _cancelButton, _saveButton }
                }
            }
        };

        _readonlySection = new VerticalStackLayout
        {
            Spacing = 10,
            IsVisible = false,
            Children =
            {
                _readonlyHintLabel,
                CreateReadonlyField("Mitglied", _readonlyMemberLabel),
                CreateReadonlyField("Datum", _readonlyDateLabel),
                CreateReadonlyField("Stunden", _readonlyHoursLabel),
                CreateReadonlyField("Art der Arbeit", _readonlyDescriptionLabel),
                CreateReadonlyField("Status", _readonlyStatusValueLabel),
                CreateReadonlyField("Freigabe", _readonlyApprovalLabel),
                _backToOverviewButton
            }
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    _headlineLabel,
                    _descriptionLabel,
                    _statusLabel,
                    _editableSection,
                    _readonlySection
                }
            }
        };
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var entryId = TryReadInt(query, "entryId");
        _entryId = entryId is > 0 ? entryId : null;
        _forceOwnContext = string.Equals(TryReadString(query, "context"), "self", StringComparison.OrdinalIgnoreCase);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_isLoading || _loadScheduled)
            return;

        _loadScheduled = true;
        Dispatcher.Dispatch(async () =>
        {
            await Task.Yield();
            _loadScheduled = false;
            await LoadAsync();
        });
    }

    private async Task LoadAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        _statusLabel.Text = "Daten werden geladen.";

        try
        {
            await EnsureSeasonAsync();
            await EnsureMemberOptionsAsync();

            if (_entryId.HasValue && _entryId.Value > 0)
            {
                await LoadExistingEntryAsync(_entryId.Value);
            }
            else
            {
                ConfigureNewEntry();
            }

            if (_statusLabel.Text == "Daten werden geladen.")
                _statusLabel.Text = string.Empty;
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
            ShowReadOnlyState(false);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task EnsureSeasonAsync()
    {
        if (_currentSaisonId.HasValue)
            return;

        var saisonen = await _supabaseService.GetSaisonRecordsAsync();
        if (saisonen.Count == 0)
            return;

        var currentYear = DateTime.Today.Year;
        var selected = saisonen.FirstOrDefault(x => x.Jahr == currentYear)
            ?? saisonen.OrderByDescending(x => x.Jahr).First();

        _currentSaisonId = selected.Id;
    }

    private async Task EnsureMemberOptionsAsync()
    {
        _memberOptions.Clear();

        var contextMemberId = GetContextMemberId();
        if (!contextMemberId.HasValue)
            return;

        var hauptmitgliedId = contextMemberId.Value;
        var selectedMember = _memberContextState.SelectedMember;
        var useSelectedMemberContext = UsesSelectedMemberContext(selectedMember);

        var mainLabel = useSelectedMemberContext && selectedMember?.IstHauptmitglied == false
            ? "Ausgewähltes Mitglied"
            : "Hauptmitglied";

        _memberOptions.Add(new MemberOption(hauptmitgliedId, mainLabel));

        var allowNebenmitglied = !useSelectedMemberContext || selectedMember?.IstHauptmitglied != false;
        if (allowNebenmitglied)
        {
            var nebenmitglied = await _supabaseService.GetNebenmitgliedByHauptmitgliedIdAsync(hauptmitgliedId);
            if (nebenmitglied != null)
                _memberOptions.Add(new MemberOption(nebenmitglied.Id, $"Nebenmitglied: {nebenmitglied.Name} {nebenmitglied.Vorname}".Trim()));
        }

        _forWhomPicker.ItemsSource = null;
        _forWhomPicker.ItemsSource = _memberOptions;
        _forWhomPicker.IsVisible = _memberOptions.Count > 1;
    }

    private async Task LoadExistingEntryAsync(int entryId)
    {
        var ids = _memberOptions.Select(x => x.MitgliedId).Distinct().ToArray();
        var entries = await _supabaseService.GetArbeitsstundenAsync(ids);
        _existingEntry = entries.FirstOrDefault(x => x.Id == entryId);

        if (_existingEntry == null)
        {
            _headlineLabel.Text = "Arbeitsstunde";
            _descriptionLabel.Text = "Der angeforderte Eintrag konnte im eigenen Nutzerpfad nicht geladen werden.";
            _statusLabel.Text = "Bitte kehre zur Übersicht zurück und öffne den Eintrag erneut.";
            ShowReadOnlyState(false);
            return;
        }

        if (_existingEntry.Freigegeben)
        {
            ConfigureReadonlyEntry(_existingEntry);
            return;
        }

        ConfigureEditableEntry(_existingEntry);
    }

    private void ConfigureNewEntry()
    {
        _existingEntry = null;
        _isReadOnly = false;
        Title = "Arbeitsstunde erfassen";
        _headlineLabel.Text = "Arbeitsstunde erfassen";
        _descriptionLabel.Text = "Erfasse eine neue Arbeitsstunde in einem eigenen mobilen Schritt statt direkt in der Übersicht.";
        _datePicker.Date = DateTime.Today;
        _hoursEntry.Text = string.Empty;
        _descEditor.Text = string.Empty;
        _forWhomPicker.SelectedItem = _memberOptions.FirstOrDefault();
        _cancelButton.Text = "Abbrechen";
        _saveButton.Text = "Speichern";
        ShowReadOnlyState(false);
    }

    private void ConfigureEditableEntry(ArbeitsstundeDTO entry)
    {
        _isReadOnly = false;
        Title = "Arbeitsstunde bearbeiten";
        _headlineLabel.Text = "Arbeitsstunde bearbeiten";
        _descriptionLabel.Text = "Unbestätigte Einträge werden in einem eigenen mobilen Bearbeitungsschritt geöffnet und nicht mehr direkt in der Übersicht bearbeitet.";
        _datePicker.Date = entry.Datum.Date;
        _hoursEntry.Text = entry.Stunden.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture);
        _descEditor.Text = entry.Beschreibung ?? string.Empty;
        _forWhomPicker.SelectedItem = _memberOptions.FirstOrDefault(x => x.MitgliedId == entry.MitgliedId) ?? _memberOptions.FirstOrDefault();
        _cancelButton.Text = "Abbrechen";
        _saveButton.Text = "Speichern";
        ShowReadOnlyState(false);
    }

    private void ConfigureReadonlyEntry(ArbeitsstundeDTO entry)
    {
        _isReadOnly = true;
        Title = "Arbeitsstunde ansehen";
        _headlineLabel.Text = "Arbeitsstunde ansehen";
        _descriptionLabel.Text = "Freigegebene Einträge sind im normalen Nutzerpfad nur noch lesbar und nicht mehr bearbeitbar.";
        _readonlyHintLabel.Text = "Dieser Eintrag wurde bereits bestätigt/freigegeben. Bearbeiten ist für den Nutzer deshalb gesperrt.";
        _readonlyMemberLabel.Text = BuildMemberDisplay(entry);
        _readonlyDateLabel.Text = entry.Datum.ToString("dd.MM.yyyy");
        _readonlyHoursLabel.Text = entry.Stunden.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture);
        _readonlyDescriptionLabel.Text = string.IsNullOrWhiteSpace(entry.Beschreibung) ? "-" : entry.Beschreibung.Trim();
        _readonlyStatusValueLabel.Text = BuildStatusText(entry);
        _readonlyApprovalLabel.Text = BuildApprovalText(entry);
        ShowReadOnlyState(true);
    }

    private void ShowReadOnlyState(bool isReadOnly)
    {
        _editableSection.IsVisible = !isReadOnly;
        _readonlySection.IsVisible = isReadOnly;
    }

    private async Task SaveAsync()
    {
        _statusLabel.Text = string.Empty;

        if (_isReadOnly)
            return;

        if (!_currentSaisonId.HasValue)
        {
            _statusLabel.Text = "Saison konnte aktuell nicht ermittelt werden.";
            return;
        }

        var member = _forWhomPicker.SelectedItem as MemberOption;
        if (member == null)
        {
            _statusLabel.Text = "Bitte zuerst den Mitgliedskontext wählen.";
            return;
        }

        var description = (_descEditor.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(description))
        {
            _statusLabel.Text = "Bitte Art der Arbeit angeben.";
            return;
        }

        if (!TryParseHours(_hoursEntry.Text, out var hours) || hours <= 0)
        {
            _statusLabel.Text = "Stunden müssen als Zahl größer als 0 eingegeben werden.";
            return;
        }

        _saveButton.IsEnabled = false;
        _cancelButton.IsEnabled = false;
        _statusLabel.Text = "Daten werden gespeichert.";

        try
        {
            await Task.Yield();

            // Determine whether current user is privileged (Vorstand/Admin)
            var isPrivileged = false;
            try
            {
                isPrivileged = _authService.IsAdmin || _authService.IsVorstand;
            }
            catch
            {
                isPrivileged = false;
            }

            var record = new ArbeitsstundeRecord
            {
                MitgliedId = member.MitgliedId,
                SaisonId = _currentSaisonId.Value,
                Datum = _datePicker.Date.Date,
                Stunden = hours,
                ArtDerArbeit = description,
                Status = _existingEntry?.Status,
                // Default freigabe values will be set below depending on role
                Freigegeben = _existingEntry?.Freigegeben ?? false,
                GenehmigtAm = _existingEntry?.FreigegebenAm,
                GenehmigtVon = _existingEntry?.FreigegebenVonId
            };

            if (_existingEntry != null)
                record.Id = _existingEntry.Id;

            // If this is a new entry and the current user is privileged, auto-approve
            if (_existingEntry == null && isPrivileged)
            {
                // Ensure we have a current member id for the approving user
                if (!_state.CurrentMitgliedId.HasValue)
                {
                    // Log and abort - we require CurrentMitgliedId for audit
                    try { AppFileLog.Warning("ArbeitsstundenEditorPage", "Arbeitsstunde Save aborted: CurrentMitgliedId missing for privileged user."); } catch { }
                    throw new InvalidOperationException("Aktueller Benutzer ist keinem Mitglied zugeordnet. Freigabe nicht möglich.");
                }

                record.Freigegeben = true;
                record.GenehmigtAm = DateTime.UtcNow;
                record.GenehmigtVon = (int?)_state.CurrentMitgliedId.Value;
                try { AppFileLog.Info("ArbeitsstundenEditorPage", $"Arbeitsstunde gespeichert – Role={(_authService.IsAdmin?"Admin":(_authService.IsVorstand?"Vorstand":"User"))} – AutoFreigabe=true"); } catch { }
            }
            else
            {
                try { AppFileLog.Info("ArbeitsstundenEditorPage", $"Arbeitsstunde gespeichert – Role={( isPrivileged? ( _authService.IsAdmin?"Admin":(_authService.IsVorstand?"Vorstand":"User")) : "Member" )} – Status={(record.Freigegeben?"Freigegeben":"in Prüfung")} "); } catch { }
            }

            var success = _existingEntry == null
                ? await _supabaseService.AddArbeitsstundeAsync(record.ToInsertRecord())
                : await _supabaseService.UpdateArbeitsstundeAsync(record);

            if (!success)
            {
                _statusLabel.Text = "Arbeitsstunde konnte nicht gespeichert werden.";
                return;
            }

            await NavigateToOverviewAsync();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
        finally
        {
            _saveButton.IsEnabled = true;
            _cancelButton.IsEnabled = true;
        }
    }

    private Task NavigateToOverviewAsync()
    {
        var targetRoute = ResolveOverviewRoute();
        return Shell.Current.GoToAsync(targetRoute);
    }

    private string ResolveOverviewRoute()
    {
        if (Shell.Current is Shell shell)
        {
            if (ShellNavigationHelper.HasVisibleShellContentRoute(shell, "member_workhours"))
                return "//member_workhours";

            if (ShellNavigationHelper.HasVisibleShellContentRoute(shell, "workhours"))
                return "//workhours";
        }

        return nameof(MyArbeitsstundenPage);
    }

    private static int? TryReadInt(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var raw) || raw == null)
            return null;

        return raw switch
        {
            int intValue => intValue,
            long longValue when longValue <= int.MaxValue && longValue >= int.MinValue => (int)longValue,
            string text when int.TryParse(Uri.UnescapeDataString(text), out var parsed) => parsed,
            _ => null
        };
    }

    private static string? TryReadString(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var raw) || raw == null)
            return null;

        return raw switch
        {
            string text => Uri.UnescapeDataString(text),
            _ => raw.ToString()
        };
    }

    private static bool TryParseHours(string? input, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        if (decimal.TryParse(input, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.CurrentCulture, out value))
            return true;

        if (decimal.TryParse(input, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out value))
            return true;

        return decimal.TryParse(input.Replace(',', '.'), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    private int? GetContextMemberId()
    {
        if (!_forceOwnContext && PermissionChecks.CanManageWorkHours(_state.CurrentUserContext))
        {
            var selectedId = _memberContextState.SelectedMember?.Id;
            if (selectedId is > 0)
                return selectedId.Value;
        }

        return _state.CurrentMitgliedId is > 0 and <= int.MaxValue
            ? (int)_state.CurrentMitgliedId.Value
            : null;
    }

    private bool UsesSelectedMemberContext(MemberDTO? selectedMember)
    {
        return !_forceOwnContext
            && PermissionChecks.CanManageWorkHours(_state.CurrentUserContext)
            && selectedMember?.Id is > 0;
    }

    private static string BuildMemberDisplay(ArbeitsstundeDTO entry)
    {
        var display = $"{entry.Nachname} {entry.Vorname}".Trim();
        return string.IsNullOrWhiteSpace(display) ? "-" : display;
    }

    private static string BuildStatusText(ArbeitsstundeDTO entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Status))
            return entry.Status.Trim();

        return entry.Freigegeben ? "freigegeben" : "offen";
    }

    private static string BuildApprovalText(ArbeitsstundeDTO entry)
    {
        if (!entry.Freigegeben)
            return "Noch nicht freigegeben";

        var approvedAt = entry.FreigegebenAm?.ToString("dd.MM.yyyy HH:mm") ?? "ohne Datum";
        var approvedBy = string.IsNullOrWhiteSpace(entry.FreigegebenVonName) ? "unbekannt" : entry.FreigegebenVonName.Trim();
        return $"Freigegeben am {approvedAt} durch {approvedBy}";
    }

    private static Label CreateValueLabel()
    {
        return new Label { LineBreakMode = LineBreakMode.WordWrap };
    }

    private static View CreateReadonlyField(string title, View valueView)
    {
        return new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
                valueView
            }
        };
    }

    private sealed record MemberOption(int MitgliedId, string Display);
}
