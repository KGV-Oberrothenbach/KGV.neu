using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Core.Utilities;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

public sealed class SaisonverwaltungPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private readonly ObservableCollection<SaisonRecord> _saisons = new();
    private readonly CollectionView _saisonsView;
    private readonly Entry _jahrEntry;
    private readonly Entry _pachtProQmEntry;
    private readonly Entry _mitgliedsbeitragEntry;
    private readonly Entry _mitgliedsbeitragNebenmitgliedEntry;
    private readonly Entry _aufnahmegebuehrEntry;
    private readonly Entry _gebuehrBauantragEntry;
    private readonly Entry _pflichtstundenSollEntry;
    private readonly Entry _euroProFehlstundeEntry;
    private readonly Editor _bemerkungEditor;
    private readonly Label _statusLabel;
    private readonly Button _suggestButton;
    private readonly Button _saveButton;

    private SaisonRecord? _selectedSaison;
    private bool _isBusy;
    public SaisonverwaltungPage(ISupabaseService supabaseService, UserContextState userContextState)
    {
        _supabaseService = supabaseService;
        _userContextState = userContextState;

        Title = "Saisonverwaltung";

        _saisonsView = new CollectionView
        {
            ItemsSource = _saisons,
            SelectionMode = SelectionMode.Single,
            HeightRequest = 240,
            ItemTemplate = new DataTemplate(() =>
            {
                var yearLabel = new Label
                {
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 16
                };
                yearLabel.SetBinding(Label.TextProperty, nameof(SaisonRecord.Jahr));

                var detailsLabel = new Label
                {
                    FontSize = 12,
                    TextColor = Colors.Gray,
                    LineBreakMode = LineBreakMode.WordWrap
                };
                detailsLabel.SetBinding(Label.TextProperty, new Binding(path: ".", converter: new SaisonSummaryConverter()));

                return new Border
                {
                    Stroke = Colors.LightGray,
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 8),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children = { yearLabel, detailsLabel }
                    }
                };
            })
        };
        _saisonsView.SelectionChanged += OnSaisonSelectionChanged;

        _jahrEntry = CreateEntry(readOnly: true);
        _pachtProQmEntry = CreateEntry(keyboard: Keyboard.Numeric);
        _mitgliedsbeitragEntry = CreateEntry(keyboard: Keyboard.Numeric);
        _mitgliedsbeitragNebenmitgliedEntry = CreateEntry(keyboard: Keyboard.Numeric);
        _aufnahmegebuehrEntry = CreateEntry(keyboard: Keyboard.Numeric);
        _gebuehrBauantragEntry = CreateEntry(keyboard: Keyboard.Numeric);
        _pflichtstundenSollEntry = CreateEntry(keyboard: Keyboard.Numeric);
        _euroProFehlstundeEntry = CreateEntry(keyboard: Keyboard.Numeric);
        _bemerkungEditor = new Editor
        {
            AutoSize = EditorAutoSizeOption.TextChanges,
            HeightRequest = 120,
            Placeholder = "Bemerkung"
        };

        _statusLabel = new Label
        {
            TextColor = Colors.DarkSlateBlue,
            LineBreakMode = LineBreakMode.WordWrap
        };

        _suggestButton = new Button { Text = "Neue Saison vorschlagen" };
        _suggestButton.Clicked += async (_, _) => await SuggestNextSaisonAsync();

        _saveButton = new Button { Text = "Saison speichern" };
        _saveButton.Clicked += async (_, _) => await SaveAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 14,
                Children =
                {
                    new Label { Text = "Verwaltung", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    new Label { Text = "Saisonverwaltung", FontSize = 18, FontAttributes = FontAttributes.Bold },
                    new Label
                    {
                        Text = "Saison-ID und Saisonjahr entsprechen dem Kalenderjahr. Neue Saisons übernehmen automatisch die Werte des Vorjahres als Vorschlag.",
                        TextColor = Colors.Gray,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    new Label
                    {
                        Text = "Vergangene Jahre sind schreibgeschützt. Laufendes und zukünftige Jahre bleiben bearbeitbar.",
                        TextColor = Colors.Gray,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    CreateSection("Saisons", _saisonsView, _suggestButton),
                    CreateSection(
                        "Saison bearbeiten",
                        CreateField("Jahr / ID", _jahrEntry),
                        CreateField("Pacht pro qm", _pachtProQmEntry),
                        CreateField("Mitgliedsbeitrag", _mitgliedsbeitragEntry),
                        CreateField("Nebenmitgliedsbeitrag", _mitgliedsbeitragNebenmitgliedEntry),
                        CreateField("Aufnahmegebühr", _aufnahmegebuehrEntry),
                        CreateField("Gebühr Bauantrag", _gebuehrBauantragEntry),
                        CreateField("Pflichtstunden Soll", _pflichtstundenSollEntry),
                        CreateField("Euro pro Fehlstunde", _euroProFehlstundeEntry),
                        CreateField("Bemerkung", _bemerkungEditor),
                        _statusLabel,
                        _saveButton)
                }
            }
        };

        Appearing += async (_, _) => await LoadAsync();
    }

    private bool IsAdmin => _userContextState.CurrentUserContext?.Role is UserRole.Admin;

    private bool IsEditable
        => IsAdmin
           && !_isBusy
           && int.TryParse(_jahrEntry.Text, out var jahr)
           && jahr >= DateTime.Today.Year;

    private async Task LoadAsync(int? preferredYear = null)
    {
        if (_isBusy)
            return;

        try
        {
            _isBusy = true;
            SetStatus(string.Empty);
            ApplyEditorEnabledState();

            var ordered = (await _supabaseService.GetSaisonRecordsAsync())
                .OrderByDescending(SaisonverwaltungHelper.GetSaisonJahr)
                .ToList();

            _saisons.Clear();
            foreach (var saison in ordered)
                _saisons.Add(saison);

            var selected = preferredYear.HasValue
                ? ordered.FirstOrDefault(x => SaisonverwaltungHelper.GetSaisonJahr(x) == preferredYear.Value)
                : ordered.FirstOrDefault();

            if (selected != null)
            {
                _selectedSaison = selected;
                _saisonsView.SelectedItem = selected;
                ApplyEditor(selected);
                return;
            }

            await SuggestNextSaisonAsync();
        }
        catch (Exception ex)
        {
            SetStatus($"Saisons konnten nicht geladen werden: {ex.Message}", isError: true);
        }
        finally
        {
            _isBusy = false;
            ApplyEditorEnabledState();
        }
    }

    private async Task SuggestNextSaisonAsync()
    {
        if (!IsAdmin)
        {
            SetStatus("Saisonverwaltung ist nur für Admin verfügbar.", isError: true);
            return;
        }

        var proposal = SaisonverwaltungHelper.CreateNextSaisonProposal(_saisons);
        _selectedSaison = null;
        _saisonsView.SelectedItem = null;
        ApplyEditor(proposal);
        SetStatus("Neue Saison wurde auf Basis des Vorjahres vorgeschlagen.");
        ApplyEditorEnabledState();
        await Task.CompletedTask;
    }

    private async Task SaveAsync()
    {
        if (!IsEditable)
        {
            SetStatus("Vergangene Jahre können nicht bearbeitet werden.", isError: true);
            return;
        }

        if (!TryBuildSaisonRecord(out var saison, out var validationMessage))
        {
            SetStatus(validationMessage, isError: true);
            return;
        }

        try
        {
            _isBusy = true;
            ApplyEditorEnabledState();

            var saved = await _supabaseService.SaveSaisonAsync(saison);
            if (saved == null)
            {
                SetStatus("Saison konnte nicht gespeichert werden.", isError: true);
                return;
            }

            await LoadAsync(SaisonverwaltungHelper.GetSaisonJahr(saved));
            SetStatus($"Saison {SaisonverwaltungHelper.GetSaisonJahr(saved)} gespeichert.");
        }
        catch (Exception ex)
        {
            SetStatus($"Saison konnte nicht gespeichert werden: {ex.Message}", isError: true);
        }
        finally
        {
            _isBusy = false;
            ApplyEditorEnabledState();
        }
    }

    private void OnSaisonSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var saison = e.CurrentSelection?.FirstOrDefault() as SaisonRecord;
        if (saison == null)
            return;

        _selectedSaison = saison;
        ApplyEditor(saison);
        SetStatus(string.Empty);
        ApplyEditorEnabledState();
    }

    private void ApplyEditor(SaisonRecord saison)
    {
        _jahrEntry.Text = SaisonverwaltungHelper.GetSaisonJahr(saison).ToString(CultureInfo.InvariantCulture);
        _pachtProQmEntry.Text = FormatDecimal(saison.PachtProQm);
        _mitgliedsbeitragEntry.Text = FormatDecimal(saison.Mitgliedsbeitrag);
        _mitgliedsbeitragNebenmitgliedEntry.Text = FormatDecimal(saison.MitgliedsbeitragNebenmitglied);
        _aufnahmegebuehrEntry.Text = FormatDecimal(saison.Aufnahmegebuehr);
        _gebuehrBauantragEntry.Text = FormatDecimal(saison.GebuehrBauantrag);
        _pflichtstundenSollEntry.Text = saison.PflichtstundenSoll.ToString("0.##", CultureInfo.CurrentCulture);
        _euroProFehlstundeEntry.Text = saison.EuroProFehlstunde.ToString("0.##", CultureInfo.CurrentCulture);
        _bemerkungEditor.Text = saison.Bemerkung ?? string.Empty;
    }

    private void ApplyEditorEnabledState()
    {
        var editable = IsEditable;
        _pachtProQmEntry.IsEnabled = editable;
        _mitgliedsbeitragEntry.IsEnabled = editable;
        _mitgliedsbeitragNebenmitgliedEntry.IsEnabled = editable;
        _aufnahmegebuehrEntry.IsEnabled = editable;
        _gebuehrBauantragEntry.IsEnabled = editable;
        _pflichtstundenSollEntry.IsEnabled = editable;
        _euroProFehlstundeEntry.IsEnabled = editable;
        _bemerkungEditor.IsEnabled = editable;
        _saveButton.IsEnabled = editable;
        _suggestButton.IsEnabled = !_isBusy && IsAdmin;
    }

    private void SetStatus(string message, bool isError = false)
    {
        _statusLabel.Text = message;
        _statusLabel.TextColor = isError ? Colors.DarkRed : Colors.DarkSlateBlue;
    }

    private bool TryBuildSaisonRecord(out SaisonRecord saison, out string validationMessage)
    {
        saison = new SaisonRecord();
        validationMessage = string.Empty;

        if (!int.TryParse(_jahrEntry.Text, out var jahr) || jahr < 1900 || jahr > 3000)
        {
            validationMessage = "Bitte ein gültiges Kalenderjahr angeben.";
            return false;
        }

        if (!TryParseRequiredDecimal(_pflichtstundenSollEntry.Text, out var pflichtstundenSoll))
        {
            validationMessage = "Bitte einen gültigen Wert für Pflichtstunden Soll eingeben.";
            return false;
        }

        if (!TryParseRequiredDecimal(_euroProFehlstundeEntry.Text, out var euroProFehlstunde))
        {
            validationMessage = "Bitte einen gültigen Wert für Euro pro Fehlstunde eingeben.";
            return false;
        }

        if (!TryParseOptionalDecimal(_pachtProQmEntry.Text, out var pachtProQm))
        {
            validationMessage = "Bitte einen gültigen Wert für Pacht pro qm eingeben.";
            return false;
        }

        if (!TryParseOptionalDecimal(_mitgliedsbeitragEntry.Text, out var mitgliedsbeitrag))
        {
            validationMessage = "Bitte einen gültigen Wert für Mitgliedsbeitrag eingeben.";
            return false;
        }

        if (!TryParseOptionalDecimal(_mitgliedsbeitragNebenmitgliedEntry.Text, out var mitgliedsbeitragNebenmitglied))
        {
            validationMessage = "Bitte einen gültigen Wert für Nebenmitgliedsbeitrag eingeben.";
            return false;
        }

        if (!TryParseOptionalDecimal(_aufnahmegebuehrEntry.Text, out var aufnahmegebuehr))
        {
            validationMessage = "Bitte einen gültigen Wert für Aufnahmegebühr eingeben.";
            return false;
        }

        if (!TryParseOptionalDecimal(_gebuehrBauantragEntry.Text, out var gebuehrBauantrag))
        {
            validationMessage = "Bitte einen gültigen Wert für Gebühr Bauantrag eingeben.";
            return false;
        }

        saison = new SaisonRecord
        {
            Id = jahr,
            Jahr = jahr,
            PflichtstundenSoll = pflichtstundenSoll,
            EuroProFehlstunde = euroProFehlstunde,
            PachtProQm = pachtProQm,
            Mitgliedsbeitrag = mitgliedsbeitrag,
            MitgliedsbeitragNebenmitglied = mitgliedsbeitragNebenmitglied,
            Aufnahmegebuehr = aufnahmegebuehr,
            GebuehrBauantrag = gebuehrBauantrag,
            Bemerkung = string.IsNullOrWhiteSpace(_bemerkungEditor.Text) ? null : _bemerkungEditor.Text.Trim()
        };

        return true;
    }

    private static Entry CreateEntry(bool readOnly = false, Keyboard? keyboard = null)
        => new()
        {
            IsReadOnly = readOnly,
            Keyboard = keyboard ?? Keyboard.Default
        };

    private static Border CreateSection(string title, params View[] children)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Children.Add(new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 18 });
        foreach (var child in children)
            stack.Children.Add(child);

        return new Border
        {
            Stroke = Colors.LightGray,
            Padding = 16,
            Content = stack
        };
    }

    private static View CreateField(string title, View editor)
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

    private static string FormatDecimal(decimal? value)
        => value.HasValue ? value.Value.ToString("0.##", CultureInfo.CurrentCulture) : string.Empty;

    private static bool TryParseOptionalDecimal(string? raw, out decimal? value)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = null;
            return true;
        }

        if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
            || decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            value = parsed;
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryParseRequiredDecimal(string? raw, out decimal value)
    {
        if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
            || decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            return true;

        value = 0m;
        return false;
    }

    private sealed class SaisonSummaryConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not SaisonRecord saison)
                return string.Empty;

            return $"Pacht/qm: {FormatDecimal(saison.PachtProQm)} · Beitrag: {FormatDecimal(saison.Mitgliedsbeitrag)} · Nebenmitglied: {FormatDecimal(saison.MitgliedsbeitragNebenmitglied)}";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
