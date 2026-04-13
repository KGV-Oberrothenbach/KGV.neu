using System;
using System.Globalization;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

public sealed class VereinskonfigurationPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private readonly Entry _vereinsnameEntry;
    private readonly Entry _kurznameEntry;
    private readonly Entry _registerangabeEntry;
    private readonly Entry _strasseEntry;
    private readonly Entry _plzEntry;
    private readonly Entry _ortEntry;
    private readonly Entry _standardEmailEntry;
    private readonly Entry _standardTelefonEntry;
    private readonly Entry _websiteEntry;
    private readonly Entry _kontoinhaberEntry;
    private readonly Entry _banknameEntry;
    private readonly Entry _ibanEntry;
    private readonly Entry _bicEntry;
    private readonly Entry _verwendungszweckMitgliedsantragEntry;
    private readonly Entry _verwendungszweckPachtvertragEntry;
    private readonly Entry _dokumentOrtEntry;
    private readonly Editor _standardHinweistextEditor;
    private readonly Editor _datenschutzTextEditor;
    private readonly Entry _datenschutzVersionEntry;
    private readonly Entry _datenschutzStandEntry;
    private readonly Label _statusLabel;
    private readonly Button _refreshButton;
    private readonly Button _saveButton;

    private VereinskonfigurationRecord _currentRecord = new() { Aktiv = true };
    private bool _isBusy;

    public VereinskonfigurationPage(ISupabaseService supabaseService, UserContextState userContextState)
    {
        _supabaseService = supabaseService;
        _userContextState = userContextState;

        Title = "Vereinskonfiguration";

        _vereinsnameEntry = CreateEntry();
        _kurznameEntry = CreateEntry();
        _registerangabeEntry = CreateEntry();
        _strasseEntry = CreateEntry();
        _plzEntry = CreateEntry();
        _ortEntry = CreateEntry();
        _standardEmailEntry = CreateEntry(keyboard: Keyboard.Email);
        _standardTelefonEntry = CreateEntry(keyboard: Keyboard.Telephone);
        _websiteEntry = CreateEntry(keyboard: Keyboard.Url);
        _kontoinhaberEntry = CreateEntry();
        _banknameEntry = CreateEntry();
        _ibanEntry = CreateEntry();
        _bicEntry = CreateEntry();
        _verwendungszweckMitgliedsantragEntry = CreateEntry();
        _verwendungszweckPachtvertragEntry = CreateEntry();
        _dokumentOrtEntry = CreateEntry();
        _standardHinweistextEditor = CreateEditor("Standard-Hinweistext");
        _datenschutzTextEditor = CreateEditor("Datenschutz-Text", 160);
        _datenschutzVersionEntry = CreateEntry();
        _datenschutzStandEntry = CreateEntry(placeholder: "Optional, z. B. 31.12.2025");

        _statusLabel = new Label
        {
            TextColor = Colors.DarkSlateBlue,
            LineBreakMode = LineBreakMode.WordWrap
        };

        _refreshButton = new Button { Text = "Aktualisieren" };
        _refreshButton.Clicked += async (_, _) => await LoadAsync();

        _saveButton = new Button { Text = "Vereinskonfiguration speichern" };
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
                    new Label { Text = "Vereinskonfiguration", FontSize = 18, FontAttributes = FontAttributes.Bold },
                    new Label
                    {
                        Text = "Die aktive Vereinskonfiguration pflegt Vereinsdaten, Standardtexte und Dokumentmetadaten zentral an einer Stelle.",
                        TextColor = Colors.Gray,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    CreateSection(
                        "Vereinsdaten",
                        CreateField("Vereinsname", _vereinsnameEntry),
                        CreateField("Kurzname", _kurznameEntry),
                        CreateField("Registerangabe", _registerangabeEntry),
                        CreateField("Straße", _strasseEntry),
                        CreateField("PLZ", _plzEntry),
                        CreateField("Ort", _ortEntry),
                        CreateField("Standard-E-Mail", _standardEmailEntry),
                        CreateField("Standard-Telefon", _standardTelefonEntry),
                        CreateField("Website", _websiteEntry)),
                    CreateSection(
                        "Bankdaten",
                        CreateField("Kontoinhaber", _kontoinhaberEntry),
                        CreateField("Bankname", _banknameEntry),
                        CreateField("IBAN", _ibanEntry),
                        CreateField("BIC", _bicEntry)),
                    CreateSection(
                        "Dokumente und Datenschutz",
                        CreateField("Verwendungszweck Mitgliedsantrag", _verwendungszweckMitgliedsantragEntry),
                        CreateField("Verwendungszweck Pachtvertrag", _verwendungszweckPachtvertragEntry),
                        CreateField("Dokument-Ort", _dokumentOrtEntry),
                        CreateField("Standard-Hinweistext", _standardHinweistextEditor),
                        CreateField("Datenschutz-Text", _datenschutzTextEditor),
                        CreateField("Datenschutz-Version", _datenschutzVersionEntry),
                        CreateField("Datenschutz-Stand", _datenschutzStandEntry)),
                    _statusLabel,
                    new HorizontalStackLayout
                    {
                        HorizontalOptions = LayoutOptions.End,
                        Spacing = 10,
                        Children = { _refreshButton, _saveButton }
                    }
                }
            }
        };

        Appearing += async (_, _) => await LoadAsync();
    }

    private bool IsAdmin => _userContextState.CurrentUserContext?.Role is UserRole.Admin;
    private bool IsEditable => IsAdmin && !_isBusy;

    private async Task LoadAsync()
    {
        if (_isBusy)
            return;

        try
        {
            _isBusy = true;
            SetStatus(string.Empty);
            ApplyEditorEnabledState();

            var record = await _supabaseService.GetAktiveVereinskonfigurationAsync();
            _currentRecord = record ?? new VereinskonfigurationRecord { Aktiv = true };
            ApplyEditor(_currentRecord);

            if (record == null)
                SetStatus("Es ist noch keine aktive Vereinskonfiguration hinterlegt. Mit dem ersten Speichern wird sie angelegt.");
        }
        catch (Exception ex)
        {
            SetStatus($"Vereinskonfiguration konnte nicht geladen werden: {ex.Message}", true);
        }
        finally
        {
            _isBusy = false;
            ApplyEditorEnabledState();
        }
    }

    private async Task SaveAsync()
    {
        if (!IsEditable)
        {
            SetStatus("Vereinskonfiguration ist nur für Admin bearbeitbar.", true);
            return;
        }

        if (!TryBuildRecord(out var record, out var validationMessage))
        {
            SetStatus(validationMessage, true);
            return;
        }

        try
        {
            _isBusy = true;
            ApplyEditorEnabledState();

            var saved = await _supabaseService.SaveAktiveVereinskonfigurationAsync(record);
            if (saved == null)
            {
                SetStatus("Vereinskonfiguration konnte nicht gespeichert werden.", true);
                return;
            }

            _currentRecord = saved;
            ApplyEditor(saved);
            SetStatus("Vereinskonfiguration gespeichert.");
        }
        catch (Exception ex)
        {
            SetStatus($"Vereinskonfiguration konnte nicht gespeichert werden: {ex.Message}", true);
        }
        finally
        {
            _isBusy = false;
            ApplyEditorEnabledState();
        }
    }

    private void ApplyEditor(VereinskonfigurationRecord record)
    {
        _vereinsnameEntry.Text = record.Vereinsname ?? string.Empty;
        _kurznameEntry.Text = record.Kurzname ?? string.Empty;
        _registerangabeEntry.Text = record.Registerangabe ?? string.Empty;
        _strasseEntry.Text = record.Strasse ?? string.Empty;
        _plzEntry.Text = record.Plz ?? string.Empty;
        _ortEntry.Text = record.Ort ?? string.Empty;
        _standardEmailEntry.Text = record.StandardEmail ?? string.Empty;
        _standardTelefonEntry.Text = record.StandardTelefon ?? string.Empty;
        _websiteEntry.Text = record.Website ?? string.Empty;
        _kontoinhaberEntry.Text = record.Kontoinhaber ?? string.Empty;
        _banknameEntry.Text = record.Bankname ?? string.Empty;
        _ibanEntry.Text = record.Iban ?? string.Empty;
        _bicEntry.Text = record.Bic ?? string.Empty;
        _verwendungszweckMitgliedsantragEntry.Text = record.VerwendungszweckMitgliedsantrag ?? string.Empty;
        _verwendungszweckPachtvertragEntry.Text = record.VerwendungszweckPachtvertrag ?? string.Empty;
        _dokumentOrtEntry.Text = record.DokumentOrt ?? string.Empty;
        _standardHinweistextEditor.Text = record.StandardHinweistext ?? string.Empty;
        _datenschutzTextEditor.Text = record.DatenschutzText ?? string.Empty;
        _datenschutzVersionEntry.Text = record.DatenschutzVersion ?? string.Empty;
        _datenschutzStandEntry.Text = record.DatenschutzStand?.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture) ?? string.Empty;
    }

    private void ApplyEditorEnabledState()
    {
        var editable = IsEditable;
        _vereinsnameEntry.IsEnabled = editable;
        _kurznameEntry.IsEnabled = editable;
        _registerangabeEntry.IsEnabled = editable;
        _strasseEntry.IsEnabled = editable;
        _plzEntry.IsEnabled = editable;
        _ortEntry.IsEnabled = editable;
        _standardEmailEntry.IsEnabled = editable;
        _standardTelefonEntry.IsEnabled = editable;
        _websiteEntry.IsEnabled = editable;
        _kontoinhaberEntry.IsEnabled = editable;
        _banknameEntry.IsEnabled = editable;
        _ibanEntry.IsEnabled = editable;
        _bicEntry.IsEnabled = editable;
        _verwendungszweckMitgliedsantragEntry.IsEnabled = editable;
        _verwendungszweckPachtvertragEntry.IsEnabled = editable;
        _dokumentOrtEntry.IsEnabled = editable;
        _standardHinweistextEditor.IsEnabled = editable;
        _datenschutzTextEditor.IsEnabled = editable;
        _datenschutzVersionEntry.IsEnabled = editable;
        _datenschutzStandEntry.IsEnabled = editable;
        _refreshButton.IsEnabled = !_isBusy;
        _saveButton.IsEnabled = editable;
    }

    private void SetStatus(string message, bool isError = false)
    {
        _statusLabel.Text = message;
        _statusLabel.TextColor = isError ? Colors.DarkRed : Colors.DarkSlateBlue;
    }

    private bool TryBuildRecord(out VereinskonfigurationRecord record, out string validationMessage)
    {
        validationMessage = string.Empty;
        record = new VereinskonfigurationRecord
        {
            Id = _currentRecord.Id,
            Aktiv = true,
            CreatedAt = _currentRecord.CreatedAt,
            UpdatedAt = _currentRecord.UpdatedAt,
            Vereinsname = _vereinsnameEntry.Text,
            Kurzname = _kurznameEntry.Text,
            Registerangabe = _registerangabeEntry.Text,
            Strasse = _strasseEntry.Text,
            Plz = _plzEntry.Text,
            Ort = _ortEntry.Text,
            StandardEmail = _standardEmailEntry.Text,
            StandardTelefon = _standardTelefonEntry.Text,
            Website = _websiteEntry.Text,
            Kontoinhaber = _kontoinhaberEntry.Text,
            Bankname = _banknameEntry.Text,
            Iban = _ibanEntry.Text,
            Bic = _bicEntry.Text,
            VerwendungszweckMitgliedsantrag = _verwendungszweckMitgliedsantragEntry.Text,
            VerwendungszweckPachtvertrag = _verwendungszweckPachtvertragEntry.Text,
            DokumentOrt = _dokumentOrtEntry.Text,
            StandardHinweistext = _standardHinweistextEditor.Text,
            DatenschutzText = _datenschutzTextEditor.Text,
            DatenschutzVersion = _datenschutzVersionEntry.Text
        };

        if (!TryParseOptionalDate(_datenschutzStandEntry.Text, out var datenschutzStand))
        {
            validationMessage = "Bitte ein gültiges Datum für Datenschutz-Stand eingeben.";
            return false;
        }

        record.DatenschutzStand = datenschutzStand;
        return true;
    }

    private static Entry CreateEntry(Keyboard? keyboard = null, string? placeholder = null)
        => new()
        {
            Keyboard = keyboard ?? Keyboard.Default,
            Placeholder = placeholder
        };

    private static Editor CreateEditor(string placeholder, double heightRequest = 110)
        => new()
        {
            Placeholder = placeholder,
            AutoSize = EditorAutoSizeOption.TextChanges,
            HeightRequest = heightRequest
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

    private static bool TryParseOptionalDate(string? raw, out DateTime? value)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = null;
            return true;
        }

        if (DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var parsed)
            || DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed))
        {
            value = parsed.Date;
            return true;
        }

        value = null;
        return false;
    }
}
