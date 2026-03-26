using KGV.Maui.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;

namespace KGV.Maui.Pages;

public sealed class FaelligeZaehlerPage : ContentPage
{
    private readonly FaelligeZaehlerViewModel _viewModel;
    private bool _initialized;

    public FaelligeZaehlerPage()
    {
        var services = IPlatformApplication.Current?.Services
            ?? throw new InvalidOperationException("MAUI-Services sind aktuell nicht verfügbar.");

        _viewModel = services.GetRequiredService<FaelligeZaehlerViewModel>();
        BindingContext = _viewModel;
        Title = "Fällige Zähler";

        var titleLabel = new Label { Text = "Fällige Zähler", FontSize = 24, FontAttributes = FontAttributes.Bold };
        var descriptionLabel = new Label
        {
            Text = "Übersicht zu Eichfälligkeit und Status der aktiven Zähler auf Basis von v_zaehler_eichstatus.",
            LineBreakMode = LineBreakMode.WordWrap
        };

        var filterEntry = new Entry { Placeholder = "Nach Garten, Anlage, Medium oder Zähler filtern" };
        filterEntry.SetBinding(Entry.TextProperty, nameof(FaelligeZaehlerViewModel.FilterText), BindingMode.TwoWay);

        var statusPicker = new Picker { Title = "Status" };
        statusPicker.SetBinding(Picker.ItemsSourceProperty, nameof(FaelligeZaehlerViewModel.StatusFilters));
        statusPicker.SetBinding(Picker.SelectedItemProperty, nameof(FaelligeZaehlerViewModel.SelectedStatusFilter), BindingMode.TwoWay);

        var refreshButton = new Button { Text = "Aktualisieren" };
        refreshButton.Clicked += async (_, _) => await _viewModel.RefreshAsync();

        var statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        statusLabel.SetBinding(Label.TextProperty, nameof(FaelligeZaehlerViewModel.StatusMessage));
        statusLabel.SetBinding(IsVisibleProperty, nameof(FaelligeZaehlerViewModel.HasStatusMessage));

        var emptyLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        emptyLabel.SetBinding(Label.TextProperty, nameof(FaelligeZaehlerViewModel.EmptyStateMessage));
        emptyLabel.SetBinding(IsVisibleProperty, nameof(FaelligeZaehlerViewModel.HasEmptyState));

        var collectionView = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            ItemTemplate = new DataTemplate(() => CreateItemTemplate())
        };
        collectionView.SetBinding(ItemsView.ItemsSourceProperty, nameof(FaelligeZaehlerViewModel.Items));
        collectionView.SetBinding(IsVisibleProperty, nameof(FaelligeZaehlerViewModel.HasItems));

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    titleLabel,
                    descriptionLabel,
                    filterEntry,
                    statusPicker,
                    statusLabel,
                    emptyLabel,
                    refreshButton,
                    collectionView
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_initialized)
        {
            await _viewModel.InitializeAsync();
            _initialized = true;
            return;
        }

        await _viewModel.RefreshAsync();
    }

    private static View CreateItemTemplate()
    {
        static Label ValueLabel(string path, bool bold = false)
        {
            var label = new Label { LineBreakMode = LineBreakMode.WordWrap };
            if (bold)
                label.FontAttributes = FontAttributes.Bold;
            label.SetBinding(Label.TextProperty, path);
            return label;
        }

        return new Border
        {
            Stroke = Colors.LightGray,
            Padding = 14,
            Margin = new Thickness(0, 0, 0, 10),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(14) },
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    ValueLabel(nameof(KGV.Core.Models.ZaehlerEichstatusRecord.GartenDisplay), true),
                    ValueLabel(nameof(KGV.Core.Models.ZaehlerEichstatusRecord.AnlageDisplay)),
                    CreateDetailRow("Medium", nameof(KGV.Core.Models.ZaehlerEichstatusRecord.MediumDisplay)),
                    CreateDetailRow("Zähler", nameof(KGV.Core.Models.ZaehlerEichstatusRecord.ZaehlerDisplay)),
                    CreateDetailRow("Eichdatum", nameof(KGV.Core.Models.ZaehlerEichstatusRecord.EichdatumDisplay)),
                    CreateDetailRow("Eichfälligkeit", nameof(KGV.Core.Models.ZaehlerEichstatusRecord.EichfaelligDisplay)),
                    CreateDetailRow("Status", nameof(KGV.Core.Models.ZaehlerEichstatusRecord.EichstatusDisplay)),
                    CreateDetailRow("Tage", nameof(KGV.Core.Models.ZaehlerEichstatusRecord.TageDisplay))
                }
            }
        };
    }

    private static View CreateDetailRow(string title, string bindingPath)
    {
        var valueLabel = new Label { HorizontalOptions = LayoutOptions.End, HorizontalTextAlignment = TextAlignment.End };
        valueLabel.SetBinding(Label.TextProperty, bindingPath);

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold },
                valueLabel
            }
        };

        Grid.SetColumn(valueLabel, 1);
        return grid;
    }
}
