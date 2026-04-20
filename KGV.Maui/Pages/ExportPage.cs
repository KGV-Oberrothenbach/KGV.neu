using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Core.Utilities;
using KGV.Maui.State;
using KGV.Maui.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class ExportPage : ContentPage
{
    private readonly ExportViewModel _vm;
    private readonly UserContextState _userContextState;
    private readonly Picker _definitionPicker;
    private readonly StackLayout _filtersStack;
    private readonly Button _runButton;
    private readonly Button _exportCsvButton;
    private readonly Button _exportPdfButton;
    private readonly Label _statusLabel;
    private readonly Button _prevButton;
    private readonly Button _nextButton;
    private readonly StackLayout _recordView;
    private readonly CollectionView _tableView;

    public ExportPage(ExportViewModel vm, UserContextState userContextState)
    {
        _vm = vm;
        _userContextState = userContextState;
        Title = "Export";

        _definitionPicker = new Picker { Title = "Wähle Exportdefinition" };
        // display the actual definition objects and show their DisplayText
        _definitionPicker.ItemDisplayBinding = new Binding("DisplayText");
        _definitionPicker.SelectedIndexChanged += async (_, _) => await OnDefinitionChanged();

        _filtersStack = new StackLayout { Spacing = 8 };

        _runButton = new Button { Text = "Ausführen" };
        _runButton.Clicked += async (_, _) => await OnRunClicked();

        _exportCsvButton = new Button { Text = "Als CSV exportieren" };
        _exportCsvButton.Clicked += async (_, _) => await OnExportCsvClicked();
        _exportPdfButton = new Button { Text = "Als PDF exportieren" };
        _exportPdfButton.Clicked += async (_, _) => await OnExportPdfClicked();

        _statusLabel = new Label { TextColor = Colors.DarkSlateBlue };

        _prevButton = new Button { Text = "←" };
        _prevButton.Clicked += (_, _) => { if (_vm.MovePrevious()) RenderCurrentRecord(); };
        _nextButton = new Button { Text = "→" };
        _nextButton.Clicked += (_, _) => { if (_vm.MoveNext()) RenderCurrentRecord(); };

        _recordView = new StackLayout { Spacing = 6 };

        _tableView = new CollectionView
        {
            ItemsLayout = new GridItemsLayout(1, ItemsLayoutOrientation.Vertical),
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label { LineBreakMode = LineBreakMode.WordWrap };
                label.SetBinding(Label.TextProperty, new Binding(".", converter: new KGV.Maui.Converters.FuncConverter<object, string>(o => o?.ToString() ?? string.Empty)));
                return new Frame { Content = label, Padding = 6, Margin = 2 };
            })
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 10,
                Children =
                {
                    new Label { Text = "Export", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    _definitionPicker,
                    _filtersStack,
                    new HorizontalStackLayout { Children = { _runButton, _exportCsvButton, _exportPdfButton } },
                    _statusLabel,
                    new HorizontalStackLayout { Children = { _prevButton, _nextButton } },
                    _recordView,
                    _tableView
                }
            }
        };
    }

    private async Task OnExportPdfClicked()
    {
        try
        {
            if (_vm.ProcessedResults.Count == 0)
            {
                _statusLabel.Text = "Keine Daten zum Exportieren.";
                return;
            }

            var filePath = await _vm.ExportToPdfAsync();
            _statusLabel.Text = $"PDF erzeugt: {filePath}";
            await Share.Default.RequestAsync(new ShareFileRequest { Title = "Export (PDF) teilen", File = new ShareFile(filePath) });
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"PDF-Export fehlgeschlagen: {ex.Message}";
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try { Console.WriteLine("EXPORTDBG: ExportPage appearing"); System.Diagnostics.Debug.WriteLine("EXPORTDBG: ExportPage appearing"); } catch {}
        if (_userContextState.CurrentUserContext?.Role is not (UserRole.Admin or UserRole.Vorstand))
        {
            _statusLabel.Text = "Export ist mobil nur für Admin/Vorstand verfügbar.";
            _definitionPicker.IsVisible = false;
            _runButton.IsVisible = false;
            _exportCsvButton.IsVisible = false;
            return;
        }

        await _vm.LoadDefinitionsAsync();
        try { Console.WriteLine($"EXPORTDBG: Definitions loaded count={_vm.Definitions.Count}"); System.Diagnostics.Debug.WriteLine($"EXPORTDBG: Definitions loaded count={_vm.Definitions.Count}"); } catch {}
        // Use the real definition objects as ItemsSource so SelectedItem is the object
        _definitionPicker.ItemsSource = _vm.Definitions;
        // ensure buttons only active if a valid definition selected
        _runButton.IsEnabled = _vm.Definitions.Count > 0 && _definitionPicker.SelectedItem != null;
        _exportCsvButton.IsEnabled = _vm.Definitions.Count > 0 && _definitionPicker.SelectedItem != null;
        _exportPdfButton.IsEnabled = _vm.Definitions.Count > 0 && _definitionPicker.SelectedItem != null;
        if (_vm.Definitions.Count > 0)
        {
            _definitionPicker.SelectedItem = _vm.Definitions.First();
            // log initial selected
            var first = _vm.Definitions.First();
            try { Console.WriteLine($"EXPORTDBG: Initial selected definition Titel={first.Titel ?? ""}, ExportKey={first.ExportKey ?? ""}, QuelleName={first.QuelleName ?? ""}"); System.Diagnostics.Debug.WriteLine($"EXPORTDBG: Initial selected definition Titel={first.Titel ?? ""}, ExportKey={first.ExportKey ?? ""}, QuelleName={first.QuelleName ?? ""}"); } catch {}
            // Ensure selection logic runs (SelectedIndexChanged may not fire for programmatic set)
            await OnDefinitionChanged();
        }

        UpdateLayoutForDevice();
    }

    private void UpdateLayoutForDevice()
    {
        var idiom = DeviceInfo.Idiom;
        var isPhone = idiom == DeviceIdiom.Phone || idiom == DeviceIdiom.Watch;
        _tableView.IsVisible = !isPhone;
        _recordView.IsVisible = isPhone;
        _prevButton.IsVisible = isPhone;
        _nextButton.IsVisible = isPhone;
    }

    private async Task OnDefinitionChanged()
    {
        // prefer SelectedItem as the real model object; fall back to index if needed
        AppExportDefinitionRecord? def = null;
        if (_definitionPicker.SelectedItem is AppExportDefinitionRecord selDef)
            def = selDef;
        else
        {
            var idx = _definitionPicker.SelectedIndex;
            if (idx >= 0 && idx < _vm.Definitions.Count)
                def = _vm.Definitions[idx];
        }

        try { Console.WriteLine($"EXPORTDBG: OnDefinitionChanged SelectedIndex={_definitionPicker.SelectedIndex}"); System.Diagnostics.Debug.WriteLine($"EXPORTDBG: OnDefinitionChanged SelectedIndex={_definitionPicker.SelectedIndex}"); } catch {}

        if (def == null)
        {
            _statusLabel.Text = "Keine gültige Exportdefinition ausgewählt.";
            _runButton.IsEnabled = false;
            _exportCsvButton.IsEnabled = false;
            _exportPdfButton.IsEnabled = false;
            _filtersStack.Children.Clear();
            return;
        }
        try { Console.WriteLine($"EXPORTDBG: OnDefinitionChanged SelectedItem titel={def.Titel ?? ""}, export_key={def.ExportKey ?? ""}, quelle={def.QuelleName ?? ""}"); System.Diagnostics.Debug.WriteLine($"EXPORTDBG: OnDefinitionChanged SelectedItem titel={def.Titel ?? ""}, export_key={def.ExportKey ?? ""}, quelle={def.QuelleName ?? ""}"); } catch {}

        await _vm.SelectDefinitionAsync(def);
        try { Console.WriteLine($"EXPORTDBG: After SelectDefinitionAsync filters={_vm.Filters.Count}, columns={_vm.Columns.Count}"); System.Diagnostics.Debug.WriteLine($"EXPORTDBG: After SelectDefinitionAsync filters={_vm.Filters.Count}, columns={_vm.Columns.Count}"); } catch {}

        RenderFilters();

        try { Console.WriteLine($"EXPORTDBG: After RenderFilters visibleColumns={_vm.ColumnsVisibleOrdered.Count}"); System.Diagnostics.Debug.WriteLine($"EXPORTDBG: After RenderFilters visibleColumns={_vm.ColumnsVisibleOrdered.Count}"); } catch {}

        // enable buttons only when a real definition has been loaded
        var hasDef = _vm.SelectedDefinition != null;
        _runButton.IsEnabled = hasDef;
        _exportCsvButton.IsEnabled = hasDef && (_vm.ColumnsVisibleOrdered?.Count > 0);
        _exportPdfButton.IsEnabled = hasDef && (_vm.ColumnsVisibleOrdered?.Count > 0);
    }

    private void RenderFilters()
    {
        _filtersStack.Children.Clear();
        foreach (var f in _vm.Filters)
        {
            if (string.Equals(f.Typ, "select", StringComparison.OrdinalIgnoreCase))
            {
                var picker = new Picker { Title = f.Label ?? f.FilterKey };
                if (!string.IsNullOrWhiteSpace(f.OptionenJson))
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var opts = new List<string>();
                            var raw = f.OptionenJson.Trim();
                            if (raw.StartsWith("["))
                            {
                                try
                                {
                                    var parsed = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(raw);
                                    if (parsed != null)
                                    {
                                        foreach (var item in parsed)
                                        {
                                            if (item.ValueKind == System.Text.Json.JsonValueKind.String)
                                                opts.Add(item.GetString() ?? string.Empty);
                                            else
                                                opts.Add(item.ToString());
                                        }
                                    }
                                }
                                catch
                                {
                                    // if parsing fails, add raw
                                    opts.Add(raw);
                                }
                            }
                            else if (raw.StartsWith("{"))
                            {
                                opts.Add(raw);
                            }
                            else
                            {
                                // treat as RPC name or plain string
                                var s = raw;
                                var rows = await _vm.ExecuteOptionsRpcAsync(s);
                                opts.AddRange(rows.Select(r => r.ToString()));
                            }

                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                picker.ItemsSource = opts;
                                if (opts.Count > 0) picker.SelectedIndex = 0;
                            });
                        }
                        catch (Exception ex)
                        {
                            try { Console.WriteLine($"EXPORTDBG: RenderFilters optionen_json parse failed: {ex.Message}"); System.Diagnostics.Debug.WriteLine($"EXPORTDBG: RenderFilters optionen_json parse failed: {ex.Message}"); } catch {}
                        }
                    });
                }

                picker.SelectedIndexChanged += (_, _) =>
                {
                    var sel = picker.SelectedIndex >= 0 ? picker.Items[picker.SelectedIndex] : null;
                    _vm.FilterValues[f.FilterKey ?? string.Empty] = sel;
                };

                _filtersStack.Children.Add(picker);
            }
            else if (string.Equals(f.Typ, "boolean", StringComparison.OrdinalIgnoreCase))
            {
                var sw = new Switch();
                sw.Toggled += (_, e) => _vm.FilterValues[f.FilterKey ?? string.Empty] = e.Value;
                _filtersStack.Children.Add(new StackLayout { Orientation = StackOrientation.Horizontal, Children = { new Label { Text = f.Label ?? f.FilterKey }, sw } });
            }
            else
            {
                _filtersStack.Children.Add(new Label { Text = $"Unbekannter Filtertyp: {f.Typ}" });
            }
        }
    }

    private async Task OnRunClicked()
    {
        try
        {
            _statusLabel.Text = "Lade Daten...";
            await _vm.ExecuteAsync();

            if (_vm.ProcessedResults.Count == 0)
            {
                _statusLabel.Text = "Keine Ergebnisse.";
                _tableView.IsVisible = false;
                _recordView.IsVisible = true;
                _recordView.Children.Clear();
                _recordView.Children.Add(new Label { Text = "Keine Ergebnisse gefunden.", TextColor = Colors.Gray });
                return;
            }

            // show concise result summary including RPC diagnostics if useful
            if (!string.IsNullOrWhiteSpace(_vm.LastRpcError))
            {
                _statusLabel.Text = $"Export-Fehler: {_vm.LastRpcError}";
            }
            else if (_vm.ProcessedResults.Count == 0)
            {
                _statusLabel.Text = $"Keine Ergebnisse. RPC={_vm.LastRpcName ?? "?"}, params={_vm.LastRpcParameterSummary ?? "(none)"}";
            }
            else
            {
                _statusLabel.Text = $"Ergebnisse: {_vm.ProcessedResults.Count} (RPC={_vm.LastRpcName ?? "?"})";
            }

            UpdateLayoutForDevice();

            if (_tableView.IsVisible)
            {
                // create simple header + rows view
                var header = new Grid { ColumnSpacing = 8 };
                header.ColumnDefinitions.Clear();
                foreach (var col in _vm.ColumnsVisibleOrdered)
                    header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

                var headerRow = new HorizontalStackLayout { Spacing = 8 };
                foreach (var col in _vm.ColumnsVisibleOrdered)
                    headerRow.Children.Add(new Label { Text = col.Label ?? col.Name, FontAttributes = FontAttributes.Bold });

                var stack = new VerticalStackLayout { Spacing = 6 };
                stack.Children.Add(headerRow);

                foreach (var row in _vm.ProcessedResults)
                {
                    var rowLayout = new HorizontalStackLayout { Spacing = 8 };
                    foreach (var col in _vm.ColumnsVisibleOrdered)
                    {
                        var key = col.Name ?? string.Empty;
                        rowLayout.Children.Add(new Label { Text = row.ContainsKey(key) ? row[key] : string.Empty });
                    }

                    stack.Children.Add(rowLayout);
                }

                _tableView.ItemsSource = stack.Children.Select(c => c).ToList();
                _tableView.IsVisible = true;
                _recordView.IsVisible = false;
            }
            else
            {
                // phone: show first record and enable navigation
                RenderCurrentRecord();
                // add swipe gestures
                var left = new SwipeGestureRecognizer { Direction = SwipeDirection.Left };
                left.Swiped += (_, __) => { if (_vm.MoveNext()) RenderCurrentRecord(); };
                var right = new SwipeGestureRecognizer { Direction = SwipeDirection.Right };
                right.Swiped += (_, __) => { if (_vm.MovePrevious()) RenderCurrentRecord(); };
                _recordView.GestureRecognizers.Clear();
                _recordView.GestureRecognizers.Add(left);
                _recordView.GestureRecognizers.Add(right);
            }
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Fehler: {ex.Message}";
        }
    }

    private void RenderCurrentRecord()
    {
        _recordView.Children.Clear();
        var rec = _vm.CurrentRecord;
        if (rec == null)
        {
            _recordView.Children.Add(new Label { Text = "Keine Ergebnisse.", TextColor = Colors.Gray });
            return;
        }

        foreach (var col in _vm.ColumnsVisibleOrdered)
        {
            var key = col.Name ?? string.Empty;
            var label = new Label { Text = col.Label ?? key, FontAttributes = FontAttributes.Bold };
            var value = new Label { Text = rec.ContainsKey(key) ? rec[key] : string.Empty };
            _recordView.Children.Add(label);
            _recordView.Children.Add(value);
        }
    }

    private async Task OnExportCsvClicked()
    {
        try
        {
            if (_vm.ProcessedResults.Count == 0)
            {
                _statusLabel.Text = "Keine Daten zum Exportieren.";
                return;
            }

            var filePath = await _vm.ExportToCsvAsync();
            _statusLabel.Text = $"Export erzeugt: {filePath}";
            await Share.Default.RequestAsync(new ShareFileRequest { Title = "Export teilen", File = new ShareFile(filePath) });
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Export fehlgeschlagen: {ex.Message}";
        }
    }
}
