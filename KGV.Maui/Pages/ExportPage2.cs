using KGV.Core.Models;
using KGV.Maui.State;
using Microsoft.Maui.ApplicationModel;
using KGV.Maui.ViewModels;
using KGV.Core.Security;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class ExportPage2 : ContentPage
{
    private readonly ExportViewModel _vm;
    private readonly UserContextState _userContextState;
    private readonly Picker _definitionPicker;
    private readonly StackLayout _filtersStack;
    private readonly Button _runButton;
    private readonly Label _statusLabel;
    private readonly CollectionView _resultsView;

    public ExportPage2(ExportViewModel vm, UserContextState userContextState)
    {
        _vm = vm;
        _userContextState = userContextState;

        Title = "Export";

        _definitionPicker = new Picker { Title = "Wähle Exportdefinition" };
        _definitionPicker.SelectedIndexChanged += async (_, _) => await OnDefinitionChanged();

        _filtersStack = new StackLayout { Spacing = 8 };

        _runButton = new Button { Text = "Ausführen" };
        _runButton.Clicked += async (_, _) => await OnRunClicked();

        _statusLabel = new Label { TextColor = Colors.DarkSlateBlue };

        _resultsView = new CollectionView
        {
            ItemsLayout = new GridItemsLayout(1, ItemsLayoutOrientation.Vertical),
            ItemTemplate = new DataTemplate(() =>
            {
                var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star) } };
                var label = new Label { LineBreakMode = LineBreakMode.WordWrap };
                label.SetBinding(Label.TextProperty, 
                    new Binding(".", converter: new JsonElementToStringConverter()));
                grid.Add(label);
                return grid;
            })
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Export (Test) ", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    _definitionPicker,
                    _filtersStack,
                    _runButton,
                    _statusLabel,
                    _resultsView
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_userContextState.CurrentUserContext?.Role is not (UserRole.Admin or UserRole.Vorstand))
        {
            _statusLabel.Text = "Export ist mobil nur für Admin/Vorstand verfügbar.";
            _definitionPicker.IsVisible = false;
            _runButton.IsVisible = false;
            return;
        }

        await _vm.LoadDefinitionsAsync();
        _definitionPicker.ItemsSource = _vm.Definitions.Select(d => d.Title ?? d.Name ?? d.Id.ToString()).ToList();
        _definitionPicker.SelectedIndex = 0;
    }

    private async Task OnDefinitionChanged()
    {
        var idx = _definitionPicker.SelectedIndex;
        if (idx < 0)
            return;

        var def = _vm.Definitions[idx];
        await _vm.SelectDefinitionAsync(def);
        RenderFilters();
    }

    private void RenderFilters()
    {
        _filtersStack.Children.Clear();
        foreach (var f in _vm.Filters)
        {
            if (string.Equals(f.Type, "select", StringComparison.OrdinalIgnoreCase))
            {
                var picker = new Picker { Title = f.Label ?? f.Name };
                // minimal: call rpc from service to get options if provided
                if (!string.IsNullOrWhiteSpace(f.OptionsRpc))
                {
                    // Use supabase service via viewmodel
                    Task.Run(async () =>
                    {
                        var rows = await _vm.ExecuteOptionsRpcAsync(f.OptionsRpc);
                        var opts = rows.Select(r =>
                        {
                            try
                            {
                                if (r.ValueKind == System.Text.Json.JsonValueKind.Object && r.TryGetProperty("label", out var lab))
                                    return lab.GetString() ?? r.ToString();
                            }
                            catch { }
                            return r.ToString();
                        }).ToList();

                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            picker.ItemsSource = opts;
                            if (opts.Count > 0) picker.SelectedIndex = 0;
                        });
                    });
                }

                picker.SelectedIndexChanged += (_, _) =>
                {
                    var sel = picker.SelectedIndex >= 0 ? picker.Items[picker.SelectedIndex] : null;
                    _vm.FilterValues[f.Name ?? string.Empty] = sel;
                };

                _filtersStack.Children.Add(picker);
            }
            else if (string.Equals(f.Type, "boolean", StringComparison.OrdinalIgnoreCase))
            {
                var sw = new Switch();
                sw.Toggled += (_, e) => _vm.FilterValues[f.Name ?? string.Empty] = e.Value;
                _filtersStack.Children.Add(new StackLayout { Orientation = StackOrientation.Horizontal, Children = { new Label { Text = f.Label ?? f.Name }, sw } });
            }
            else
            {
                _filtersStack.Children.Add(new Label { Text = $"Unbekannter Filtertyp: {f.Type}" });
            }
        }
    }

    private async Task OnRunClicked()
    {
        try
        {
            _statusLabel.Text = "Lade Daten...";
            await _vm.ExecuteAsync();
            _resultsView.ItemsSource = _vm.Results.Select(r => r);
            _statusLabel.Text = $"Ergebnisse: {_vm.Results.Count}";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Fehler: {ex.Message}";
        }
    }
}

public class JsonElementToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is JsonElement je)
        {
            try
            {
                if (je.ValueKind == JsonValueKind.Object)
                {
                    if (je.TryGetProperty("display", out var disp))
                        return disp.ToString();
                    return je.ToString();
                }

                return je.ToString();
            }
            catch
            {
                return je.ToString();
            }
        }

        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
