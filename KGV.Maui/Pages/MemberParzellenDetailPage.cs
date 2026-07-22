using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.Services.Diagnostics;
using KGV.Maui.State;
using KGV.Maui.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class MemberParzellenDetailPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly ParzellenViewModel _viewModel;
    private readonly ParzellenContextState _parzellenContextState;
    private readonly UserContextState _userContextState;
    private readonly Label _contextErrorLabel;
    private readonly Label _pachtvertragDiagnoseLabel;
    private readonly Button _pachtvertragButton;
    private readonly Button _openPachtvertragButton;
    private readonly Button _discardPachtvertragButton;
    private bool _initialized;
    private bool _appearingInProgress;
    private bool _contractCreationInProgress;

    public MemberParzellenDetailPage(ISupabaseService supabaseService, ParzellenViewModel viewModel, ParzellenContextState parzellenContextState, UserContextState userContextState)
    {
        _supabaseService = supabaseService;
        _viewModel = viewModel;
        _parzellenContextState = parzellenContextState;
        _userContextState = userContextState;
        BindingContext = _viewModel;
        Title = "Parzellen-Details";

        var titleLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold };
        titleLabel.SetBinding(Label.TextProperty, nameof(ParzellenViewModel.Title));

        var hintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        hintLabel.SetBinding(Label.TextProperty, nameof(ParzellenViewModel.DetailHint));

        _contextErrorLabel = new Label
        {
            Text = "Diese Seite ist nur im Pfad 'Gärten des Mitglieds' verfügbar.",
            TextColor = Colors.DarkRed,
            LineBreakMode = LineBreakMode.WordWrap,
            IsVisible = false
        };

        var currentParzelleLabel = new Label { FontSize = 20, FontAttributes = FontAttributes.Bold };
        currentParzelleLabel.SetBinding(Label.TextProperty, nameof(ParzellenViewModel.SelectedParzelleDisplayName));

        var stromButton = new Button();
        stromButton.SetBinding(Button.TextProperty, nameof(ParzellenViewModel.StromButtonText));
        stromButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanOpenStromAction));

        var wasserButton = new Button();
        wasserButton.SetBinding(Button.TextProperty, nameof(ParzellenViewModel.WasserButtonText));
        wasserButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanOpenWasserAction));

        var dokumenteButton = new Button();
        dokumenteButton.SetBinding(Button.TextProperty, nameof(ParzellenViewModel.DokumenteButtonText));
        dokumenteButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanOpenDokumenteAction));

        stromButton.Clicked += async (_, _) => await OpenAblesungAsync("strom");
        wasserButton.Clicked += async (_, _) => await OpenAblesungAsync("wasser");
        dokumenteButton.Clicked += async (_, _) => await OpenDokumenteAsync();

        _pachtvertragButton = new Button
        {
            Text = "Pachtvertrag als PDF",
            IsVisible = PermissionChecks.CanCreateMitglied(_userContextState.CurrentUserContext)
        };
        _pachtvertragButton.Clicked += async (_, _) => await CreatePachtvertragAsync();

        _openPachtvertragButton = new Button
        {
            Text = "Pachtvertrag öffnen",
            IsVisible = false
        };
        _openPachtvertragButton.Clicked += async (_, _) => await OpenSignedPachtvertragAsync();

        _discardPachtvertragButton = new Button
        {
            Text = "Unsignierten Pachtvertrag verwerfen",
            TextColor = Colors.Red,
            IsVisible = false
        };
        _discardPachtvertragButton.Clicked += async (_, _) => await DiscardUnsigniertesPachtvertragAsync();

        _pachtvertragDiagnoseLabel = new Label { TextColor = Colors.DarkOrange, LineBreakMode = LineBreakMode.WordWrap, FontSize = 12 };

        _viewModel.PropertyChanged += (_, _) => UpdatePachtvertragButtons();

        var detailContainer = new VerticalStackLayout { Spacing = 12 };
        detailContainer.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.ShowMemberContextDetail));

        var actionsLayout = new HorizontalStackLayout
        {
            Spacing = 8,
            Children = { stromButton, wasserButton, dokumenteButton }
        };

        var pachtButtonsLayout = new HorizontalStackLayout
        {
            Spacing = 8,
            Children =
            {
                _pachtvertragButton,
                _openPachtvertragButton,
                _discardPachtvertragButton
            }
        };

        // Diagnose-Label direkt unter den Pachtvertrag-Buttons (analog Mitgliedsantrag-Diagnose)
        var pachtDiagnoseContainer = new VerticalStackLayout { Spacing = 2 };
        pachtDiagnoseContainer.Children.Add(pachtButtonsLayout);
        pachtDiagnoseContainer.Children.Add(_pachtvertragDiagnoseLabel);

        detailContainer.Children.Add(new Border
        {
            Stroke = Colors.LightGray,
            Padding = 12,
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    currentParzelleLabel,
                    CreateSection("Parzellen-Details",
                        CreateValueLabel("ID", "SelectedDetail.ParzelleId"),
                        CreateValueLabel("Garten Nr", "SelectedDetail.GartenNr"),
                        CreateValueLabel("Größe / Fläche", nameof(ParzellenViewModel.SelectedParzelleSizeText)),
                        CreateValueLabel("Strom", nameof(ParzellenViewModel.SelectedParzelleStromAvailabilityText)),
                        CreateValueLabel("Wasser", nameof(ParzellenViewModel.SelectedParzelleWasserAvailabilityText)),
                        CreateValueLabel("rfid Wasser", "SelectedDetail.RfidWasserText"),
                        CreateValueLabel("rfid Strom", "SelectedDetail.RfidStromText"),
                        CreateValueLabel("Anlage", "SelectedDetail.Anlage")),
                        CreateSection("Aktionen",
                        new Label
                        {
                            Text = "Die Aktionen beziehen sich nur auf die aktuell ausgewählte Parzelle dieses Mitglieds.",
                            TextColor = Colors.Gray,
                            LineBreakMode = LineBreakMode.WordWrap
                        },
                        actionsLayout,
                        pachtDiagnoseContainer)
                }
            }
        });

        var previousButton = new Button { Text = "Vorherige" };
        previousButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanSelectPrevious));
        previousButton.Clicked += async (_, _) => await _viewModel.SelectPreviousAsync();

        var navigationLabel = new Label
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };
        navigationLabel.SetBinding(Label.TextProperty, nameof(ParzellenViewModel.NavigationText));

        var nextButton = new Button { Text = "Nächste" };
        nextButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanSelectNext));
        nextButton.Clicked += async (_, _) => await _viewModel.SelectNextAsync();

        var navigationGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 8
        };
        navigationGrid.Children.Add(previousButton);
        Grid.SetColumn(previousButton, 0);
        navigationGrid.Children.Add(navigationLabel);
        Grid.SetColumn(navigationLabel, 1);
        navigationGrid.Children.Add(nextButton);
        Grid.SetColumn(nextButton, 2);

        detailContainer.Children.Add(CreateSection("Navigation",
            new Label
            {
                Text = "Per Buttons zwischen den Parzellen dieses Mitglieds wechseln.",
                TextColor = Colors.Gray,
                LineBreakMode = LineBreakMode.WordWrap
            },
            navigationGrid));

        var statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        statusLabel.SetBinding(Label.TextProperty, nameof(ParzellenViewModel.StatusMessage));
        statusLabel.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.HasStatusMessage));

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    titleLabel,
                    hintLabel,
                    _contextErrorLabel,
                    detailContainer,
                    statusLabel
                }
            }
        };
    }

    private void UpdatePachtvertragButtons()
    {
        try
        {
            // Default: determine basic visibility based on selection
            if (!_viewModel.HasSelectedDetail)
            {
                _pachtvertragButton.IsVisible = false;
                _openPachtvertragButton.IsVisible = false;
                _discardPachtvertragButton.IsVisible = false;
                _pachtvertragDiagnoseLabel.Text = BuildPachtvertragDiagnoseText();
                return;
            }

            // If there is a signed contract -> only open
            if (_viewModel.HasSignedPachtvertrag)
            {
                _pachtvertragButton.IsVisible = false;
                _openPachtvertragButton.IsVisible = true;
                _discardPachtvertragButton.IsVisible = false;
                _pachtvertragDiagnoseLabel.Text = BuildPachtvertragDiagnoseText();
                // enable state
                _openPachtvertragButton.IsEnabled = _openPachtvertragButton.IsVisible;
                _pachtvertragButton.IsEnabled = false;
                _discardPachtvertragButton.IsEnabled = false;
                return;
            }

            // If there is an existing unsigniertes Pachtvertrag, offer open and discard actions
            var hasUnsigniertes = _viewModel.Dokumente?.Any(d => string.Equals(d.FormularDokumentTypKey, "Pachtvertrag", StringComparison.Ordinal) && string.Equals(d.FormularDokumentStatusKey, "Unsigniert", StringComparison.Ordinal)) == true;
            if (hasUnsigniertes)
            {
                _pachtvertragButton.IsVisible = false;
                _openPachtvertragButton.IsVisible = true; // open unsigniertes
                _discardPachtvertragButton.IsVisible = PermissionChecks.CanCreateMitglied(_userContextState.CurrentUserContext);
                _pachtvertragDiagnoseLabel.Text = BuildPachtvertragDiagnoseText();
                // enable state
                _openPachtvertragButton.IsEnabled = _openPachtvertragButton.IsVisible && !_contractCreationInProgress;
                _discardPachtvertragButton.IsEnabled = _discardPachtvertragButton.IsVisible && !_contractCreationInProgress;
                _pachtvertragButton.IsEnabled = false;
                return;
            }

            // Default creation state
            var canCreate = PermissionChecks.CanCreateMitglied(_userContextState.CurrentUserContext);
            _pachtvertragButton.IsVisible = canCreate;
            _openPachtvertragButton.IsVisible = false;
            _discardPachtvertragButton.IsVisible = false;
            _pachtvertragButton.IsEnabled = _pachtvertragButton.IsVisible && !_contractCreationInProgress;
            _pachtvertragDiagnoseLabel.Text = BuildPachtvertragDiagnoseText();
        }
        catch
        {
            var canCreate = PermissionChecks.CanCreateMitglied(_userContextState.CurrentUserContext);
            _pachtvertragButton.IsVisible = canCreate;
            _openPachtvertragButton.IsVisible = false;
            _discardPachtvertragButton.IsVisible = false;
            _pachtvertragButton.IsEnabled = _pachtvertragButton.IsVisible && !_contractCreationInProgress;
            _pachtvertragDiagnoseLabel.Text = BuildPachtvertragDiagnoseText();
        }
    }

    private string BuildPachtvertragDiagnoseText()
    {
        try
        {
            var reasons = new System.Collections.Generic.List<string>();
            if (!_viewModel.HasSelectedDetail)
                reasons.Add("Keine Parzelle ausgewählt");

            if (_parzellenContextState.ContextMitgliedId is not > 0)
                reasons.Add("Mitgliedskontext fehlt");

            var canCreate = PermissionChecks.CanCreateMitglied(_userContextState.CurrentUserContext);
            if (!canCreate)
                reasons.Add("CanCreateMitglied = false");

            var detailId = _viewModel.SelectedDetail?.ParzelleId ?? 0;
            var reasonText = reasons.Count == 0 ? "Button sollte sichtbar sein." : $"Button unsichtbar wegen: {string.Join(", ", reasons)}";
            return $"[TEMP Diagnose Pachtvertrag] Parzelle={detailId}, Mitglied={_parzellenContextState.ContextMitgliedId ?? 0}. {reasonText}";
        }
        catch
        {
            return string.Empty;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_appearingInProgress)
        {
            AppFileLog.Warning("KGV.Navigation", "MemberParzellenDetailPage.OnAppearing unterdrückt, weil bereits ein Ladepfad aktiv ist.");
            return;
        }

        _appearingInProgress = true;

        try
        {
            if (!_parzellenContextState.IsFromMemberContext)
            {
                _contextErrorLabel.IsVisible = true;
                AppFileLog.Warning("KGV.Navigation", "MemberParzellenDetailPage ohne Mitgliedskontext geöffnet.");
                return;
            }

            _contextErrorLabel.IsVisible = false;
            AppFileLog.Info("KGV.Navigation", $"MemberParzellenDetailPage.OnAppearing gestartet. Initialized={_initialized}, Parzelle={_parzellenContextState.SelectedParzelleId?.ToString() ?? "<none>"}, Mitglied={_parzellenContextState.ContextMitgliedId?.ToString() ?? "<none>"}.");

            if (!_initialized)
            {
                await _viewModel.InitializeAsync();
                _initialized = true;
                return;
            }

            await _viewModel.ApplyRequestedContextAsync();
            await _viewModel.RefreshSelectedDetailAsync();
        }
        catch (Exception ex)
        {
            AppFileLog.Error("KGV.Navigation", "MemberParzellenDetailPage.OnAppearing ist fehlgeschlagen.", ex);
        }
        finally
        {
            _appearingInProgress = false;
        }
    }

    private async Task OpenAblesungAsync(string medium)
    {
        var detail = _viewModel.SelectedDetail;
        if (detail == null)
            return;

        var hasMedium = string.Equals(medium, "wasser", StringComparison.OrdinalIgnoreCase)
            ? detail.HatWasser
            : detail.HatStrom;
        if (!hasMedium)
        {
            await DisplayAlert("Hinweis", string.Equals(medium, "wasser", StringComparison.OrdinalIgnoreCase)
                ? "Für diese Parzelle ist kein Wasseranschluss hinterlegt."
                : "Für diese Parzelle ist kein Stromanschluss hinterlegt.", "OK");
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(ParzellenAblesungenPage)}?parzelleId={detail.ParzelleId}&medium={medium}");
    }

    private async Task OpenDokumenteAsync()
    {
        var detail = _viewModel.SelectedDetail;
        if (detail == null)
            return;

        await Shell.Current.GoToAsync($"{nameof(DokumentePage)}?scope=parzelle&parzelleId={detail.ParzelleId}");
    }

    private async Task OpenSignedPachtvertragAsync()
    {
        var detail = _viewModel.SelectedDetail;
        if (detail == null)
            return;

        try
        {
            // Try to find a signiertes Pachtvertrags-Dokument in den bereits geladenen Dokumenten
            var doc = _viewModel.Dokumente
                .FirstOrDefault(d => string.Equals(d.FormularDokumentTypKey, FormularDokumentTyp.Pachtvertrag, StringComparison.Ordinal)
                                     && string.Equals(d.FormularDokumentStatusKey, FormularDokumentStatus.Signiert, StringComparison.Ordinal));

            if (doc != null)
            {
                await _viewModel.OpenDocumentAsync(doc);
                return;
            }
        }
        catch
        {
            // Fallthrough to DokumentePage
        }

        // Fallback: open Dokumente page for the parcel
        await Shell.Current.GoToAsync($"{nameof(DokumentePage)}?scope=parzelle&parzelleId={detail.ParzelleId}");
    }

    private async Task CreatePachtvertragAsync(bool manageBusyState = true)
    {
        if (manageBusyState && _contractCreationInProgress)
            return;

        var detail = _viewModel.SelectedDetail;
        if (detail == null)
            return;

        if (_parzellenContextState.ContextMitgliedId is not > 0)
        {
            await DisplayAlert("Pachtvertrag", "Für den aktuellen Mitgliedskontext fehlt die Mitglieds-ID.", "OK");
            return;
        }

        if (!detail.VonDatum.HasValue)
        {
            await DisplayAlert("Pachtvertrag", "Für diese Parzellenzuordnung fehlt das Startdatum. Pachtvertrag kann hier nicht erzeugt werden.", "OK");
            return;
        }

        if (manageBusyState)
        {
            _contractCreationInProgress = true;
            _pachtvertragButton.IsEnabled = false;
        }

        try
        {
            await PachtvertragFlowHelper.RunAsync(
                Navigation,
                _supabaseService,
                _parzellenContextState.ContextMitgliedId.Value,
                detail.ParzelleId,
                detail.VonDatum.Value.Date);

            // Nach erfolgreichem Erstellen/Signieren neu laden, damit HasSignedPachtvertrag aktualisiert wird
            // ReloadSelectedDetailAsync ist internal; nutze öffentliche RefreshSelectedDetailAsync
            await _viewModel.RefreshSelectedDetailAsync();
            UpdatePachtvertragButtons();

            // Inform the user whether the created Pachtvertrag was persistently stored
            try
            {
                var savedSigned = _viewModel.Dokumente?.FirstOrDefault(d => string.Equals(d.FormularDokumentTypKey, FormularDokumentTyp.Pachtvertrag, StringComparison.Ordinal)
                                                                              && string.Equals(d.FormularDokumentStatusKey, FormularDokumentStatus.Signiert, StringComparison.Ordinal));
                if (savedSigned != null)
                {
                    await DisplayAlert("Pachtvertrag", "Pachtvertrag persistent gespeichert.", "OK");
                }
                else
                {
                    var savedUnsign = _viewModel.Dokumente?.FirstOrDefault(d => string.Equals(d.FormularDokumentTypKey, FormularDokumentTyp.Pachtvertrag, StringComparison.Ordinal)
                                                                                     && string.Equals(d.FormularDokumentStatusKey, FormularDokumentStatus.Unsigniert, StringComparison.Ordinal));
                    if (savedUnsign != null)
                        await DisplayAlert("Pachtvertrag", "Unsignierte Pachtvertragsfassung persistent gespeichert.", "OK");
                    else
                        await DisplayAlert("Pachtvertrag", "Pachtvertrag wurde erzeugt, aber kein Dokumenteintrag gefunden.", "OK");
                }
            }
            catch { }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Pachtvertrag", ex.Message, "OK");
        }
        finally
        {
            if (manageBusyState)
            {
                _contractCreationInProgress = false;
                _pachtvertragButton.IsEnabled = _pachtvertragButton.IsVisible;
            }
        }
    }

    private static Border CreateSection(string title, params View[] children)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Children.Add(new Label { Text = title, FontAttributes = FontAttributes.Bold });
        foreach (var child in children)
            stack.Children.Add(child);

        return new Border
        {
            Stroke = Colors.LightGray,
            Padding = 12,
            Content = stack
        };
    }

    private static View CreateValueLabel(string title, string path)
    {
        return new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
                CreateBoundLabel(path)
            }
        };
    }

    private static Label CreateBoundLabel(string path)
    {
        var label = new Label { LineBreakMode = LineBreakMode.WordWrap };
        label.SetBinding(Label.TextProperty, path);
        return label;
    }
    private async Task DiscardUnsigniertesPachtvertragAsync()
    {
        var detail = _viewModel.SelectedDetail;
        if (detail == null)
            return;

        try
        {
            // Find unsigniertes Pachtvertrag document
            var doc = _viewModel.Dokumente
                .FirstOrDefault(d => string.Equals(d.FormularDokumentTypKey, "Pachtvertrag", StringComparison.Ordinal) && string.Equals(d.FormularDokumentStatusKey, "Unsigniert", StringComparison.Ordinal));

            if (doc == null)
            {
                await DisplayAlert("Pachtvertrag", "Kein unsignierter Pachtvertrag gefunden.", "OK");
                return;
            }

            var confirm = await DisplayAlert("Pachtvertrag verwerfen", "Soll die vorhandene unsignierte Pachtvertragsfassung verworfen werden? Diese Aktion kann nicht rückgängig gemacht werden.", "Ja", "Nein");
            if (!confirm)
                return;

            var result = await _supabaseService.DeleteDokumentAsync(doc);
            if (!result.Success)
            {
                await DisplayAlert("Pachtvertrag", "Das Dokument konnte nicht gelöscht werden: " + result.Message, "OK");
                return;
            }

            await _viewModel.RefreshSelectedDetailAsync();
            UpdatePachtvertragButtons();
            await DisplayAlert("Pachtvertrag", "Unsignierte Pachtvertragsfassung verworfen.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Pachtvertrag", ex.Message, "OK");
        }
    }
}
