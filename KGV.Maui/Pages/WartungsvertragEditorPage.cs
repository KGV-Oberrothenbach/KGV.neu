using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class WartungsvertragEditorPage : ContentPage, IQueryAttributable
{
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private readonly Label _headlineLabel;
    private readonly Label _descriptionLabel;
    private readonly Label _statusLabel;
    private readonly Entry _titleEntry;
    private readonly Editor _descriptionEditor;
    private readonly Entry _maxKontingentEntry;
    private readonly Switch _activeSwitch;
    private readonly Button _saveButton;
    private readonly Button _cancelButton;

    private long? _wartungsvertragId;
    private bool _returnToDetail;
    private bool _isLoading;
    private bool _loadScheduled;
    private bool _isAuthorized;

    public WartungsvertragEditorPage(ISupabaseService supabaseService, UserContextState userContextState)
    {
        _supabaseService = supabaseService;
        _userContextState = userContextState;

        Title = "Wartungsvertrag";
        _headlineLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap };
        _descriptionLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        _titleEntry = new Entry { Placeholder = "Titel" };
        _descriptionEditor = new Editor { AutoSize = EditorAutoSizeOption.TextChanges, HeightRequest = 140, Placeholder = "Beschreibung" };
        _maxKontingentEntry = new Entry { Placeholder = "Max. Kontingent", Keyboard = Keyboard.Numeric };
        _activeSwitch = new Switch { IsToggled = true };
        _saveButton = new Button { Text = "Speichern" };
        _saveButton.Clicked += async (_, _) => await SaveAsync();
        _cancelButton = new Button { Text = "Abbrechen" };
        _cancelButton.Clicked += async (_, _) => await CancelAsync();

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
                    CreateField("Titel *", _titleEntry),
                    CreateField("Beschreibung", _descriptionEditor),
                    CreateField("Max. Kontingent *", _maxKontingentEntry),
                    CreateField("Aktiv", _activeSwitch),
                    new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children = { _cancelButton, _saveButton }
                    }
                }
            }
        };
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var wartungsvertragId = TryReadLong(query, "wartungsvertragId");
        _wartungsvertragId = wartungsvertragId is > 0 ? wartungsvertragId : null;
        _returnToDetail = string.Equals(ReadString(query, "origin"), "detail", StringComparison.OrdinalIgnoreCase);
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
        _statusLabel.Text = "Wartungsvertrag wird geladen.";

        try
        {
            _isAuthorized = _userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand;
            if (!_isAuthorized)
            {
                _headlineLabel.Text = "Wartungsvertrag";
                _descriptionLabel.Text = "Dieser Editor ist nur für Admin/Vorstand verfügbar.";
                SetEnabledState(false);
                return;
            }

            if (_wartungsvertragId.HasValue)
                await LoadExistingAsync(_wartungsvertragId.Value);
            else
                ConfigureNew();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
            SetEnabledState(false);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadExistingAsync(long wartungsvertragId)
    {
        var contract = await _supabaseService.GetWartungsvertragByIdAsync(wartungsvertragId);
        if (contract == null)
        {
            _headlineLabel.Text = "Wartungsvertrag bearbeiten";
            _descriptionLabel.Text = "Der ausgewählte Wartungsvertrag konnte nicht geladen werden.";
            SetEnabledState(false);
            return;
        }

        Title = "Wartungsvertrag bearbeiten";
        _headlineLabel.Text = "Wartungsvertrag bearbeiten";
        _descriptionLabel.Text = "Mobiler Editorpfad für den ausgewählten Wartungsvertrag. Nach dem Speichern geht es in die produktive Detailansicht zurück.";
        _titleEntry.Text = contract.Titel ?? string.Empty;
        _descriptionEditor.Text = contract.Beschreibung ?? string.Empty;
        _maxKontingentEntry.Text = Math.Max(1, contract.MaxAktiveZuordnungen).ToString();
        _activeSwitch.IsToggled = contract.Aktiv;
        _statusLabel.Text = string.Empty;
        SetEnabledState(true);
    }

    private void ConfigureNew()
    {
        Title = "Wartungsvertrag neu";
        _headlineLabel.Text = "Neuer Wartungsvertrag";
        _descriptionLabel.Text = "Mobiler Editorpfad für einen neuen Wartungsvertrag mit Titel, Beschreibung, Kontingent und Aktivstatus.";
        _titleEntry.Text = string.Empty;
        _descriptionEditor.Text = string.Empty;
        _maxKontingentEntry.Text = "1";
        _activeSwitch.IsToggled = true;
        _statusLabel.Text = string.Empty;
        SetEnabledState(true);
    }

    private async Task SaveAsync()
    {
        if (!_isAuthorized)
            return;

        _statusLabel.Text = "Wartungsvertrag wird gespeichert.";
        SetEnabledState(false);

        try
        {
            await Task.Yield();
            if (!TryBuildRecord(out var record))
                return;

            long savedId;
            if (_wartungsvertragId.HasValue)
            {
                record.Id = _wartungsvertragId.Value;
                var success = await _supabaseService.UpdateWartungsvertragAsync(record);
                if (!success)
                {
                    _statusLabel.Text = "Wartungsvertrag konnte nicht gespeichert werden.";
                    return;
                }

                savedId = record.Id;
            }
            else
            {
                var created = await _supabaseService.CreateWartungsvertragAsync(record);
                if (created == null)
                {
                    _statusLabel.Text = "Wartungsvertrag konnte nicht erstellt werden.";
                    return;
                }

                savedId = created.Id;
            }

            await ReturnAfterSaveAsync(savedId);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
        finally
        {
            SetEnabledState(true);
        }
    }

    private bool TryBuildRecord(out WartungsvertragRecord record)
    {
        record = new WartungsvertragRecord();

        if (string.IsNullOrWhiteSpace(_titleEntry.Text))
        {
            _statusLabel.Text = "Titel ist ein Pflichtfeld.";
            _titleEntry.Focus();
            return false;
        }

        if (!int.TryParse(_maxKontingentEntry.Text?.Trim(), out var maxKontingent) || maxKontingent <= 0)
        {
            _statusLabel.Text = "Max. Kontingent muss eine ganze Zahl größer als 0 sein.";
            _maxKontingentEntry.Focus();
            return false;
        }

        record = new WartungsvertragRecord
        {
            Titel = _titleEntry.Text.Trim(),
            Beschreibung = string.IsNullOrWhiteSpace(_descriptionEditor.Text) ? null : _descriptionEditor.Text.Trim(),
            MaxAktiveZuordnungen = maxKontingent,
            Aktiv = _activeSwitch.IsToggled
        };
        return true;
    }

    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private async Task ReturnAfterSaveAsync(long wartungsvertragId)
    {
        if (_returnToDetail && _wartungsvertragId.HasValue)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        await Shell.Current.GoToAsync("..");
        await Shell.Current.GoToAsync($"{nameof(WartungsvertragDetailPage)}?wartungsvertragId={wartungsvertragId}&adminMode=1");
    }

    private void SetEnabledState(bool isEnabled)
    {
        _titleEntry.IsEnabled = isEnabled;
        _descriptionEditor.IsEnabled = isEnabled;
        _maxKontingentEntry.IsEnabled = isEnabled;
        _activeSwitch.IsEnabled = isEnabled;
        _saveButton.IsEnabled = isEnabled;
        _cancelButton.IsEnabled = isEnabled;
    }

    private static Border CreateField(string title, View input)
    {
        return new Border
        {
            Stroke = Colors.LightGray,
            Padding = 12,
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
                    input
                }
            }
        };
    }

    private static long TryReadLong(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var value) || value == null)
            return 0;

        return value switch
        {
            long longValue => longValue,
            int intValue => intValue,
            string text when long.TryParse(Uri.UnescapeDataString(text), out var parsed) => parsed,
            _ => 0
        };
    }

    private static string ReadString(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var value) || value == null)
            return string.Empty;

        return value is string text ? Uri.UnescapeDataString(text) : value.ToString() ?? string.Empty;
    }
}
