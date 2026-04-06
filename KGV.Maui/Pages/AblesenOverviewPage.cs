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
    private readonly PendingPhotoSyncService _pendingPhotoSyncService;
    private readonly PendingPhotoMenuState _pendingPhotoMenuState;
    private readonly KGV.Maui.State.UserContextState _userContextState;
    private readonly Switch _wifiOnlySwitch;
    private readonly Label _wifiOnlyHelpLabel;

  public AblesenOverviewPage(PendingPhotoSyncService pendingPhotoSyncService, PendingPhotoMenuState pendingPhotoMenuState, KGV.Maui.State.UserContextState userContextState)
    {
        _pendingPhotoSyncService = pendingPhotoSyncService;
        _pendingPhotoMenuState = pendingPhotoMenuState;
        _userContextState = userContextState;
        Title = "Ablesen";

        var canReadMeters = PermissionChecks.CanReadMeters(_userContextState.CurrentUserContext);
        var canSubmitOwnMeterReadings = PermissionChecks.CanSubmitOwnMeterReadings(_userContextState.CurrentUserContext);
        var canManageMeterChanges = PermissionChecks.CanManageMeterChanges(_userContextState.CurrentUserContext);
        var canApproveMeterReadings = PermissionChecks.CanApproveMeterReadings(_userContextState.CurrentUserContext);
        var hasAnyMeterAccess = PermissionChecks.HasAnyMeterAccess(_userContextState.CurrentUserContext);

        _wifiOnlySwitch = new Switch { IsToggled = PhotoUploadPreferences.WifiOnly };
        _wifiOnlySwitch.Toggled += (_, e) => PhotoUploadPreferences.WifiOnly = e.Value;

        _wifiOnlyHelpLabel = new Label
        {
            Text = "Wenn aktiviert, werden Fotos lokal zwischengespeichert und erst bei WLAN hochgeladen.",
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var ablesungTile = CreateTile("Ablesung erfassen", "RFID-Tag am Gerät scannen; wenn NFC nicht nutzbar ist, steht ein fachlicher Ersatzweg über Parzelle und Medium bereit.", () => Shell.Current.GoToAsync(nameof(AblesungErfassenPage)));
        ablesungTile.IsVisible = canReadMeters || canSubmitOwnMeterReadings;

        var zaehlerwechselTile = CreateTile("Zählerwechsel", "RFID-Tag am Gerät scannen; wenn NFC nicht nutzbar ist, steht ein fachlicher Ersatzweg über Parzelle und Medium bereit.", () => Shell.Current.GoToAsync(nameof(ZaehlerwechselPage)));
        zaehlerwechselTile.IsVisible = canManageMeterChanges;

        var rfidTile = CreateTile("RFID einrichten", "RFID-Tag am Gerät scannen und der gewählten Parzelle für das gewählte Medium zuordnen.", () => Shell.Current.GoToAsync(nameof(RfidEinrichtenPage)));
        rfidTile.IsVisible = canManageMeterChanges;

        var faelligeZaehlerTile = CreateTile("Fällige Zähler", "Zähler mit naher Eichfälligkeit anzeigen", () => Shell.Current.GoToAsync(nameof(FaelligeZaehlerPage)));
        faelligeZaehlerTile.IsVisible = canReadMeters;

        var ablesungenFreigebenTile = CreateTile(
            "Ablesungen freigeben",
            "Eingereichte Ablesungen mit Pflichtkommentar freigeben, korrigieren oder aus dem offenen Prüfprozess entfernen.",
            () => Shell.Current.GoToAsync(nameof(AblesungenFreigabePage)));
        ablesungenFreigebenTile.IsVisible = canApproveMeterReadings;

        var accessHintLabel = new Label
        {
            Text = hasAnyMeterAccess
                ? "Die sichtbaren Funktionen folgen bereits der zentralen Rechtebasis V1 für Ablesen und Zählerwechsel."
                : "Mit den aktuellen Fachrechten ist im Bereich `Ablesen` derzeit keine Funktion freigeschaltet.",
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap,
            IsVisible = !hasAnyMeterAccess
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
                    accessHintLabel,
                    ablesungTile,
                    zaehlerwechselTile,
                    rfidTile,
                    faelligeZaehlerTile,
                    ablesungenFreigebenTile
                }
            }
        };
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
