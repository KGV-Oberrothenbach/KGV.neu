using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Threading.Tasks;
using KGV.Maui.Settings;
using KGV.Maui.Services.PendingPhotos;

namespace KGV.Maui.Pages;

public sealed class AblesenOverviewPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly PendingPhotoSyncService _pendingPhotoSyncService;
    private readonly PendingPhotoMenuState _pendingPhotoMenuState;
    private readonly KGV.Maui.State.UserContextState _userContextState;
    private readonly Switch _wifiOnlySwitch;
    private readonly Label _wifiOnlyHelpLabel;
    private readonly View _ablesungTile;
    private readonly View _zaehlerwechselTile;
    private readonly View _rfidTile;
    private readonly View _faelligeZaehlerTile;
    private readonly View _ablesungenFreigebenTile;
    private readonly Label _accessHintLabel;
    private bool _allowUserMeterReadingSubmissions;

    public AblesenOverviewPage(ISupabaseService supabaseService, PendingPhotoSyncService pendingPhotoSyncService, PendingPhotoMenuState pendingPhotoMenuState, KGV.Maui.State.UserContextState userContextState)
    {
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        _pendingPhotoSyncService = pendingPhotoSyncService;
        _pendingPhotoMenuState = pendingPhotoMenuState;
        _userContextState = userContextState;
        Title = "Ablesen";

        _wifiOnlySwitch = new Switch { IsToggled = PhotoUploadPreferences.WifiOnly };
        _wifiOnlySwitch.Toggled += (_, e) => PhotoUploadPreferences.WifiOnly = e.Value;

        _wifiOnlyHelpLabel = new Label
        {
            Text = "Wenn aktiviert, werden Fotos lokal zwischengespeichert und erst bei WLAN hochgeladen.",
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap
        };

        _ablesungTile = CreateTile("Ablesung erfassen", "RFID-Tag am Gerät scannen; wenn NFC nicht nutzbar ist, steht ein fachlicher Ersatzweg über Parzelle und Medium bereit.", () => Shell.Current.GoToAsync(nameof(AblesungErfassenPage)));

        _zaehlerwechselTile = CreateTile("Zählerwechsel", "RFID-Tag am Gerät scannen; wenn NFC nicht nutzbar ist, steht ein fachlicher Ersatzweg über Parzelle und Medium bereit.", () => Shell.Current.GoToAsync(nameof(ZaehlerwechselPage)));

        _rfidTile = CreateTile("RFID einrichten", "RFID-Tag am Gerät scannen und der gewählten Parzelle für das gewählte Medium zuordnen.", () => Shell.Current.GoToAsync(nameof(RfidEinrichtenPage)));

        _faelligeZaehlerTile = CreateTile("Fällige Zähler", "Zähler mit naher Eichfälligkeit anzeigen", () => Shell.Current.GoToAsync(nameof(FaelligeZaehlerPage)));

        _ablesungenFreigebenTile = CreateTile(
            "Ablesungen freigeben",
            "Eingereichte Ablesungen mit Pflichtkommentar freigeben, korrigieren oder aus dem offenen Prüfprozess entfernen.",
            () => Shell.Current.GoToAsync(nameof(AblesungenFreigabePage)));

        _accessHintLabel = new Label
        {
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap,
            IsVisible = false
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Ablesen", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    new VerticalStackLayout
                    {
                        Spacing = 6,
                        Children =
                        {
                            new HorizontalStackLayout
                            {
                                Spacing = 12,
                                Children =
                                {
                                    new Label
                                    {
                                        Text = "Fotos nur über WLAN hochladen",
                                        VerticalOptions = LayoutOptions.Center
                                    },
                                    _wifiOnlySwitch
                                }
                            },
                            _wifiOnlyHelpLabel
                        }
                    },
                    new Label { Text = "Bitte wähle eine Funktion.", LineBreakMode = LineBreakMode.WordWrap },
                    _accessHintLabel,
                    _ablesungTile,
                    _zaehlerwechselTile,
                    _rfidTile,
                    _faelligeZaehlerTile,
                    _ablesungenFreigebenTile
                }
            }
        };

        UpdateAccessUi();
    }

    private bool CanReadMeters => PermissionChecks.CanReadMeters(_userContextState.CurrentUserContext);
    private bool CanSubmitOwnMeterReadings => PermissionChecks.CanSubmitOwnMeterReadings(_userContextState.CurrentUserContext);
    private bool EffectiveCanSubmitOwnMeterReadings => CanSubmitOwnMeterReadings && _allowUserMeterReadingSubmissions;
    private bool CanManageMeterChanges => PermissionChecks.CanManageMeterChanges(_userContextState.CurrentUserContext);
    private bool CanApproveMeterReadings => PermissionChecks.CanApproveMeterReadings(_userContextState.CurrentUserContext);
    private bool HasAnyVisibleMeterAccess => CanReadMeters || EffectiveCanSubmitOwnMeterReadings || CanManageMeterChanges || CanApproveMeterReadings;

    private async Task<bool> LoadAllowUserMeterReadingSubmissionsAsync()
    {
        try
        {
            return await _supabaseService.GetAllowUserMeterReadingSubmissionsAsync();
        }
        catch
        {
            return false;
        }
    }

    private void UpdateAccessUi()
    {
        _ablesungTile.IsVisible = CanReadMeters || EffectiveCanSubmitOwnMeterReadings;
        _zaehlerwechselTile.IsVisible = CanManageMeterChanges;
        _rfidTile.IsVisible = CanManageMeterChanges;
        _faelligeZaehlerTile.IsVisible = CanReadMeters;
        _ablesungenFreigebenTile.IsVisible = CanApproveMeterReadings;

        if (!HasAnyVisibleMeterAccess)
        {
            _accessHintLabel.Text = CanSubmitOwnMeterReadings
                ? "Eigene Zählerablesungen sind aktuell zentral deaktiviert. Weitere Funktionen sind mit dem aktuellen Kontext nicht freigeschaltet."
                : "Mit den aktuellen Fachrechten ist im Bereich `Ablesen` derzeit keine Funktion freigeschaltet.";
            _accessHintLabel.IsVisible = true;
            return;
        }

        _accessHintLabel.Text = EffectiveCanSubmitOwnMeterReadings && !CanReadMeters && !CanManageMeterChanges && !CanApproveMeterReadings
            ? "Eigene Zählerablesungen werden in diesem Kontext als Einreichung gespeichert und erst über den Prüfprozess wirksam."
            : "Die sichtbaren Funktionen folgen der zentralen Rechtebasis für Nutzerablesung, Freigabe und Zählerwechsel.";
        _accessHintLabel.IsVisible = true;
    }

    private static View CreateTile(string title, string subtitle, Func<Task> navigateAsync)
    {
        var border = new Border
        {
            Padding = 18,
            Stroke = Colors.LightGray,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(16) },
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = title, FontSize = 18, FontAttributes = FontAttributes.Bold },
                    new Label { Text = subtitle, LineBreakMode = LineBreakMode.WordWrap, TextColor = Colors.Gray }
                }
            }
        };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (_, _) => await navigateAsync();
        border.GestureRecognizers.Add(tapGesture);
        return border;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _allowUserMeterReadingSubmissions = await LoadAllowUserMeterReadingSubmissionsAsync();
        UpdateAccessUi();

        try
        {
            await _pendingPhotoSyncService.TrySyncOnceAsync();
            _pendingPhotoMenuState.Refresh();
        }
        catch
        {
        }
    }
}
