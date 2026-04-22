using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Core.Utilities;
using KGV.Maui.State;
using KGV.Maui.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class ExportPage : ContentPage
{
    private record OptionItem(string Label, string? Value);

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

    private bool _isPageLoading;
    private bool _suppressDefinitionChanged;
    private bool _isHandlingDefinitionChange;
    private string? _lastLoadedDefinitionKey;

    public ExportPage(ExportViewModel vm, UserContextState userContextState)
    {
        _vm = vm;
        _userContextState = userContextState;
        Title = "Export";

        _definitionPicker = new Picker { Title = "Wähle Exportdefinition" };
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
        _prevButton.Clicked += (_, _) =>
        {
            if (_vm.MovePrevious())
                RenderCurrentRecord();
        };

        _nextButton = new Button { Text = "→" };
        _nextButton.Clicked += (_, _) =>
        {
            if (_vm.MoveNext())
                RenderCurrentRecord();
        };

        _recordView = new StackLayout { Spacing = 6 };

        _tableView = new CollectionView
        {
            ItemsLayout = new GridItemsLayout(1, ItemsLayoutOrientation.Vertical),
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label { LineBreakMode = LineBreakMode.WordWrap };
                label.SetBinding(
                    Label.TextProperty,
                    new Binding(
                        ".",
                        converter: new KGV.Maui.Converters.FuncConverter<object, string>(o => o?.ToString() ?? string.Empty)));
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

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isPageLoading)
            return;

        _isPageLoading = true;

        try
        {
            try
            {
                Console.WriteLine("EXPORTDBG: ExportPage appearing");
                System.Diagnostics.Debug.WriteLine("EXPORTDBG: ExportPage appearing");
            }
            catch
            {
            }

            if (_userContextState.CurrentUserContext?.Role is not (UserRole.Admin or UserRole.Vorstand))
            {
                _statusLabel.Text = "Export ist mobil nur für Admin/Vorstand verfügbar.";
                _definitionPicker.IsVisible = false;
                _runButton.IsVisible = false;
                _exportCsvButton.IsVisible = false;
                _exportPdfButton.IsVisible = false;
                return;
            }

            await _vm.LoadDefinitionsAsync();

            try
            {
                Console.WriteLine($"EXPORTDBG: Definitions loaded count={_vm.Definitions.Count}");
                System.Diagnostics.Debug.WriteLine($"EXPORTDBG: Definitions loaded count={_vm.Definitions.Count}");
            }
            catch
            {
            }

            _definitionPicker.ItemsSource = _vm.Definitions;

            if (_vm.Definitions.Count == 0)
            {
                _runButton.IsEnabled = false;
                _exportCsvButton.IsEnabled = false;
                _exportPdfButton.IsEnabled = false;
                _statusLabel.Text = "Keine Exportdefinitionen gefunden.";
                _filtersStack.Children.Clear();
                _recordView.Children.Clear();
                _tableView.ItemsSource = null;
                return;
            }

            var targetDefinition = _definitionPicker.SelectedItem as AppExportDefinitionRecord;
            if (targetDefinition == null || !_vm.Definitions.Contains(targetDefinition))
                targetDefinition = _vm.Definitions.First();

            _suppressDefinitionChanged = true;
            _definitionPicker.SelectedItem = targetDefinition;
            _suppressDefinitionChanged = false;

            try
            {
                Console.WriteLine(
                    $"EXPORTDBG: Initial selected definition Titel={targetDefinition.Titel ?? ""}, ExportKey={targetDefinition.ExportKey ?? ""}, QuelleName={targetDefinition.QuelleName ?? ""}");
                System.Diagnostics.Debug.WriteLine(
                    $"EXPORTDBG: Initial selected definition Titel={targetDefinition.Titel ?? ""}, ExportKey={targetDefinition.ExportKey ?? ""}, QuelleName={targetDefinition.QuelleName ?? ""}");
            }
            catch
            {
            }

            await OnDefinitionChanged();
            UpdateLayoutForDevice();
        }
        finally
        {
            _isPageLoading = false;
        }
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
        if (_suppressDefinitionChanged || _isHandlingDefinitionChange)
            return;

        _isHandlingDefinitionChange = true;

        try
        {
            AppExportDefinitionRecord? def = null;

            if (_definitionPicker.SelectedItem is AppExportDefinitionRecord selDef)
            {
                def = selDef;
            }
            else
            {
                var idx = _definitionPicker.SelectedIndex;
                if (idx >= 0 && idx < _vm.Definitions.Count)
                    def = _vm.Definitions[idx];
            }

            try
            {
                Console.WriteLine($"EXPORTDBG: OnDefinitionChanged SelectedIndex={_definitionPicker.SelectedIndex}");
                System.Diagnostics.Debug.WriteLine($"EXPORTDBG: OnDefinitionChanged SelectedIndex={_definitionPicker.SelectedIndex}");
            }
            catch
            {
            }

            if (def == null)
            {
                _statusLabel.Text = "Keine gültige Exportdefinition ausgewählt.";
                _runButton.IsEnabled = false;
                _exportCsvButton.IsEnabled = false;
                _exportPdfButton.IsEnabled = false;
                _filtersStack.Children.Clear();
                _recordView.Children.Clear();
                _tableView.ItemsSource = null;
                return;
            }

            var currentDefinitionKey =
                $"{def.ExportKey ?? string.Empty}|{def.QuelleName ?? string.Empty}|{def.Titel ?? string.Empty}";

            try
            {
                Console.WriteLine(
                    $"EXPORTDBG: OnDefinitionChanged SelectedItem titel={def.Titel ?? ""}, export_key={def.ExportKey ?? ""}, quelle={def.QuelleName ?? ""}");
                System.Diagnostics.Debug.WriteLine(
                    $"EXPORTDBG: OnDefinitionChanged SelectedItem titel={def.Titel ?? ""}, export_key={def.ExportKey ?? ""}, quelle={def.QuelleName ?? ""}");
            }
            catch
            {
            }

            _filtersStack.Children.Clear();
            _recordView.Children.Clear();
            _tableView.ItemsSource = null;
            _statusLabel.Text = string.Empty;

            if (_lastLoadedDefinitionKey != currentDefinitionKey)
            {
                await _vm.SelectDefinitionAsync(def);
                _lastLoadedDefinitionKey = currentDefinitionKey;
            }

            try
            {
                Console.WriteLine($"EXPORTDBG: After SelectDefinitionAsync filters={_vm.Filters.Count}, columns={_vm.Columns.Count}");
                System.Diagnostics.Debug.WriteLine($"EXPORTDBG: After SelectDefinitionAsync filters={_vm.Filters.Count}, columns={_vm.Columns.Count}");
            }
            catch
            {
            }

            RenderFilters();

            try
            {
                Console.WriteLine(
                    $"EXPORTDBG: After RenderFilters visibleColumns={_vm.ColumnsVisibleOrdered.Count} filterControls={_filtersStack.Children.Count}");
                System.Diagnostics.Debug.WriteLine(
                    $"EXPORTDBG: After RenderFilters visibleColumns={_vm.ColumnsVisibleOrdered.Count} filterControls={_filtersStack.Children.Count}");
            }
            catch
            {
            }

            var hasDef = _vm.SelectedDefinition != null;
            _runButton.IsEnabled = hasDef;
            _exportCsvButton.IsEnabled = hasDef && (_vm.ColumnsVisibleOrdered?.Count > 0);
            _exportPdfButton.IsEnabled = hasDef && (_vm.ColumnsVisibleOrdered?.Count > 0);
        }
        finally
        {
            _isHandlingDefinitionChange = false;
        }
    }

    private void RenderFilters()
    {
        _filtersStack.Children.Clear();
        _vm.FilterValues.Clear();

        var renderedFilterKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var f in _vm.Filters)
        {
            var filterKey = f.FilterKey ?? string.Empty;
            if (string.IsNullOrWhiteSpace(filterKey))
                continue;

            if (!renderedFilterKeys.Add(filterKey))
            {
                try
                {
                    Console.WriteLine($"EXPORTDBG: RenderFilters skipped duplicate filterKey={filterKey}");
                    System.Diagnostics.Debug.WriteLine($"EXPORTDBG: RenderFilters skipped duplicate filterKey={filterKey}");
                }
                catch
                {
                }

                continue;
            }

            if (string.Equals(f.Typ, "select", StringComparison.OrdinalIgnoreCase))
            {
                var picker = new Picker { Title = f.Label ?? filterKey };

                if (f.OptionenJson != null && f.OptionenJson.Type != JTokenType.Null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var optionItems = new List<OptionItem>();
                            var jt = f.OptionenJson;

                            if (jt.Type == JTokenType.Array)
                            {
                                foreach (var item in jt.Children())
                                {
                                    var raw = item.ToString();

                                    if (item.Type == JTokenType.Object || raw.TrimStart().StartsWith("{", StringComparison.Ordinal))
                                    {
                                        try
                                        {
                                            var jo = item.Type == JTokenType.Object
                                                ? (JObject)item
                                                : JObject.Parse(raw);

                                            var label = jo["label"]?.ToString() ?? jo["text"]?.ToString() ?? jo.ToString();
                                            var value = jo["value"]?.ToString() ?? jo["val"]?.ToString() ?? jo["id"]?.ToString() ?? label;
                                            optionItems.Add(new OptionItem(label, value));
                                        }
                                        catch
                                        {
                                            optionItems.Add(new OptionItem(raw, raw));
                                        }
                                    }
                                    else
                                    {
                                        optionItems.Add(new OptionItem(raw, raw));
                                    }
                                }
                            }
                            else if (jt.Type == JTokenType.Object)
                            {
                                var label = jt["label"]?.ToString() ?? jt.ToString();
                                var value = jt["value"]?.ToString() ?? jt["val"]?.ToString() ?? jt.ToString();
                                optionItems.Add(new OptionItem(label, value));
                            }
                            else if (jt.Type == JTokenType.String)
                            {
                                var s = jt.ToString() ?? string.Empty;

                                if (s.TrimStart().StartsWith("[", StringComparison.Ordinal))
                                {
                                    try
                                    {
                                        var parsed = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(s);
                                        if (parsed != null)
                                        {
                                            foreach (var item in parsed)
                                            {
                                                var rawItem = item.ToString();

                                                if (item.ValueKind == System.Text.Json.JsonValueKind.Object ||
                                                    rawItem.TrimStart().StartsWith("{", StringComparison.Ordinal))
                                                {
                                                    try
                                                    {
                                                        var jo = JObject.Parse(rawItem);
                                                        var label = jo["label"]?.ToString() ?? jo["text"]?.ToString() ?? rawItem;
                                                        var value = jo["value"]?.ToString() ?? jo["val"]?.ToString() ?? jo["id"]?.ToString() ?? label;
                                                        optionItems.Add(new OptionItem(label, value));
                                                    }
                                                    catch
                                                    {
                                                        optionItems.Add(new OptionItem(rawItem, rawItem));
                                                    }
                                                }
                                                else if (item.ValueKind == System.Text.Json.JsonValueKind.String)
                                                {
                                                    var vs = item.GetString() ?? string.Empty;
                                                    optionItems.Add(new OptionItem(vs, vs));
                                                }
                                                else
                                                {
                                                    optionItems.Add(new OptionItem(rawItem, rawItem));
                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        optionItems.Add(new OptionItem(s, s));
                                    }
                                }
                                else
                                {
                                    var rows = await _vm.ExecuteOptionsRpcAsync(s);
                                    foreach (var r in rows)
                                    {
                                        var rawR = r.ToString();

                                        if (r.ValueKind == System.Text.Json.JsonValueKind.Object ||
                                            rawR.TrimStart().StartsWith("{", StringComparison.Ordinal))
                                        {
                                            try
                                            {
                                                var jo = JObject.Parse(rawR);
                                                var label = jo["label"]?.ToString() ?? jo["text"]?.ToString() ?? rawR;
                                                var value = jo["value"]?.ToString() ?? jo["val"]?.ToString() ?? jo["id"]?.ToString() ?? label;
                                                optionItems.Add(new OptionItem(label, value));
                                            }
                                            catch
                                            {
                                                optionItems.Add(new OptionItem(rawR, rawR));
                                            }
                                        }
                                        else if (r.ValueKind == System.Text.Json.JsonValueKind.String)
                                        {
                                            var vs = r.GetString() ?? string.Empty;
                                            optionItems.Add(new OptionItem(vs, vs));
                                        }
                                        else
                                        {
                                            optionItems.Add(new OptionItem(rawR, rawR));
                                        }
                                    }
                                }
                            }

                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                picker.ItemDisplayBinding = new Binding("Label");
                                picker.ItemsSource = optionItems;

                                if (optionItems.Count > 0)
                                    picker.SelectedIndex = 0;
                            });
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                Console.WriteLine($"EXPORTDBG: RenderFilters optionen_json parse failed: {ex.Message}");
                                System.Diagnostics.Debug.WriteLine($"EXPORTDBG: RenderFilters optionen_json parse failed: {ex.Message}");
                            }
                            catch
                            {
                            }
                        }
                    });
                }

                picker.SelectedIndexChanged += (_, _) =>
                {
                    var sel = picker.SelectedItem as OptionItem;
                    string? rawVal = sel?.Value;
                    object? finalVal = rawVal;

                    if (rawVal != null)
                    {
                        var ts = rawVal.Trim();

                        if (string.Equals(ts, "null", StringComparison.OrdinalIgnoreCase) || ts == string.Empty)
                            finalVal = null;
                        else if (string.Equals(ts, "true", StringComparison.OrdinalIgnoreCase))
                            finalVal = true;
                        else if (string.Equals(ts, "false", StringComparison.OrdinalIgnoreCase))
                            finalVal = false;
                        else
                            finalVal = ts;
                    }

                    _vm.FilterValues[filterKey] = finalVal;

                    try
                    {
                        Console.WriteLine($"EXPORTDBG: FILTER selected filterKey={filterKey} value={finalVal}");
                        System.Diagnostics.Debug.WriteLine($"EXPORTDBG: FILTER selected filterKey={filterKey} value={finalVal}");
                    }
                    catch
                    {
                    }
                };

                _filtersStack.Children.Add(picker);
            }
            else if (string.Equals(f.Typ, "boolean", StringComparison.OrdinalIgnoreCase))
            {
                var sw = new Switch();
                sw.Toggled += (_, e) => _vm.FilterValues[filterKey] = e.Value;

                _filtersStack.Children.Add(
                    new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        Children =
                        {
                            new Label { Text = f.Label ?? filterKey },
                            sw
                        }
                    });
            }
            else
            {
                _filtersStack.Children.Add(new Label { Text = $"Unbekannter Filtertyp: {f.Typ}" });
            }
        }

        try
        {
            Console.WriteLine($"EXPORTDBG: RenderFilters renderedCount={_filtersStack.Children.Count}");
            System.Diagnostics.Debug.WriteLine($"EXPORTDBG: RenderFilters renderedCount={_filtersStack.Children.Count}");
        }
        catch
        {
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
                var stack = new VerticalStackLayout { Spacing = 6 };
                var visibleColumns = GetDistinctVisibleColumns();

                var headerRow = new HorizontalStackLayout { Spacing = 8 };
                foreach (var vmcol in visibleColumns)
                {
                    var col = vmcol.Column;
                    headerRow.Children.Add(new Label { Text = col.Label ?? col.Name, FontAttributes = FontAttributes.Bold });
                }

                stack.Children.Add(headerRow);

                foreach (var row in _vm.ProcessedResults)
                {
                    var rowLayout = new HorizontalStackLayout { Spacing = 8 };

                    foreach (var vmcol in visibleColumns)
                    {
                        var col = vmcol.Column;
                        var value = _vm.ResolveColumnValue(row, col);
                        rowLayout.Children.Add(new Label { Text = value ?? string.Empty });
                    }

                    stack.Children.Add(rowLayout);
                }

                _tableView.ItemsSource = stack.Children.Select(c => c).ToList();
                _tableView.IsVisible = true;
                _recordView.IsVisible = false;
            }
            else
            {
                RenderCurrentRecord();

                try
                {
                    _exportCsvButton.IsEnabled = _vm.ProcessedResults.Count > 0;
                    _exportPdfButton.IsEnabled = _vm.ProcessedResults.Count > 0;
                    Console.WriteLine($"EXPORTDBG: CSV/PDF enabled state: CSV={_exportCsvButton.IsEnabled}, PDF={_exportPdfButton.IsEnabled}");
                    System.Diagnostics.Debug.WriteLine($"EXPORTDBG: CSV/PDF enabled state: CSV={_exportCsvButton.IsEnabled}, PDF={_exportPdfButton.IsEnabled}");
                }
                catch
                {
                }

                var left = new SwipeGestureRecognizer { Direction = SwipeDirection.Left };
                left.Swiped += (_, __) =>
                {
                    if (_vm.MoveNext())
                        RenderCurrentRecord();
                };

                var right = new SwipeGestureRecognizer { Direction = SwipeDirection.Right };
                right.Swiped += (_, __) =>
                {
                    if (_vm.MovePrevious())
                        RenderCurrentRecord();
                };

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

        var visibleColumns = GetDistinctVisibleColumns();
        var firstLogged = false;

        foreach (var vmcol in visibleColumns)
        {
            var col = vmcol.Column;
            var labelText = col.Label ?? col.Name ?? string.Empty;
            var valueText = _vm.ResolveColumnValue(rec, col) ?? string.Empty;

            var label = new Label { Text = labelText, FontAttributes = FontAttributes.Bold };
            var value = new Label { Text = valueText };

            _recordView.Children.Add(label);
            _recordView.Children.Add(value);

            if (!firstLogged)
            {
                firstLogged = true;
                try
                {
                    Console.WriteLine($"EXPORTDBG: Preview first field label={labelText} value={valueText}");
                    System.Diagnostics.Debug.WriteLine($"EXPORTDBG: Preview first field label={labelText} value={valueText}");
                }
                catch
                {
                }
            }
        }

        try
        {
            Console.WriteLine($"EXPORTDBG: RenderCurrentRecord fieldCount={visibleColumns.Count} childCount={_recordView.Children.Count}");
            System.Diagnostics.Debug.WriteLine($"EXPORTDBG: RenderCurrentRecord fieldCount={visibleColumns.Count} childCount={_recordView.Children.Count}");
        }
        catch
        {
        }
    }

    private List<dynamic> GetDistinctVisibleColumns()
    {
        var result = new List<dynamic>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var vmcol in _vm.VisibleColumnsMapped)
        {
            var col = vmcol.Column;
            var key = col?.ColumnKey ?? col?.Name ?? string.Empty;

            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (!seen.Add(key))
                continue;

            result.Add(vmcol);
        }

        return result;
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
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Export teilen",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Export fehlgeschlagen: {ex.Message}";
        }
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
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Export (PDF) teilen",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"PDF-Export fehlgeschlagen: {ex.Message}";
        }
    }
}