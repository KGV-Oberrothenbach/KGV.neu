using KGV.Core.Models;
using KGV.Maui.Services.Diagnostics;
using KGV.Maui.State;
using KGV.Maui.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class MemberParzellenDetailPage : ContentPage
{
    private readonly ParzellenViewModel _viewModel;
    private readonly ParzellenContextState _parzellenContextState;
    private readonly Label _contextErrorLabel;
    private bool _initialized;
    private bool _appearingInProgress;

    public MemberParzellenDetailPage(ParzellenViewModel viewModel, ParzellenContextState parzellenContextState)
    {
        _viewModel = viewModel;
        _parzellenContextState = parzellenContextState;
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
        stromButton.Clicked += async (_, _) => await OpenAblesungAsync("strom");

        var wasserButton = new Button();
        wasserButton.SetBinding(Button.TextProperty, nameof(ParzellenViewModel.WasserButtonText));
        wasserButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanOpenWasserAction));
        wasserButton.Clicked += async (_, _) => await OpenAblesungAsync("wasser");

        var dokumenteButton = new Button();
        dokumenteButton.SetBinding(Button.TextProperty, nameof(ParzellenViewModel.DokumenteButtonText));
        dokumenteButton.SetBinding(IsEnabledProperty, nameof(ParzellenViewModel.CanOpenDokumenteAction));
        dokumenteButton.Clicked += async (_, _) => await OpenDokumenteAsync();

        var detailContainer = new VerticalStackLayout { Spacing = 12 };
        detailContainer.SetBinding(IsVisibleProperty, nameof(ParzellenViewModel.HasSelectedDetail));

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
                        new HorizontalStackLayout
                        {
                            Spacing = 8,
                            Children = { stromButton, wasserButton, dokumenteButton }
                        })
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
}
