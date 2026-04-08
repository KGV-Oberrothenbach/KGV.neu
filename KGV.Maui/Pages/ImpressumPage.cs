using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class ImpressumPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private readonly VerticalStackLayout _weitereVorstandContainer;
    private readonly VerticalStackLayout _bauausschussContainer;
    private readonly HorizontalStackLayout _demoToggleRow;
    private readonly Switch _showDemoDataSwitch;
    private readonly Label _statusLabel;
    private List<ImpressumKontaktItem> _allWeitereVorstandsmitglieder = new();
    private List<ImpressumKontaktItem> _allBauausschussmitglieder = new();
    private bool _isBusy;

    private bool IsDemoToggleVisible => _userContextState.CurrentUserContext?.Role == UserRole.Admin;
    private bool ShowDemoData => IsDemoToggleVisible && _showDemoDataSwitch.IsToggled;

    public ImpressumPage(ISupabaseService supabaseService, UserContextState userContextState)
    {
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        _userContextState = userContextState ?? throw new ArgumentNullException(nameof(userContextState));
        Title = "Impressum";

        _statusLabel = new Label
        {
            TextColor = Colors.DarkSlateBlue,
            LineBreakMode = LineBreakMode.WordWrap,
            IsVisible = false
        };

        _weitereVorstandContainer = new VerticalStackLayout { Spacing = 12 };
        _bauausschussContainer = new VerticalStackLayout { Spacing = 12 };
        _showDemoDataSwitch = new Switch
        {
            IsToggled = false
        };
        _showDemoDataSwitch.Toggled += (_, _) => ApplyVisibleItems();
        _demoToggleRow = new HorizontalStackLayout
        {
            Spacing = 10,
            IsVisible = false,
            Children =
            {
                _showDemoDataSwitch,
                new Label
                {
                    Text = "Demo-Datensätze einblenden",
                    VerticalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.WordWrap
                }
            }
        };

        var datenschutzButton = new Button
        {
            Text = "Datenschutzerklärung öffnen"
        };
        datenschutzButton.Clicked += async (_, _) => await OpenDatenschutzAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 16,
                Children =
                {
                    new Label
                    {
                        Text = "Impressum",
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = "Fester Vereinskopf mit Verantwortlichkeit sowie – falls vorhanden – weitere Vorstands- und Bauausschusskontakte aus dem bestehenden Datenpfad.",
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    _demoToggleRow,
                    CreateStaticSection(),
                    CreateDynamicSection("Weitere Vorstandsmitglieder", _weitereVorstandContainer),
                    CreateDynamicSection("Bauausschuss", _bauausschussContainer),
                    CreateDatenschutzSection(datenschutzButton),
                    _statusLabel
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        UpdateDemoToggleVisibility();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isBusy)
            return;

        _isBusy = true;
        try
        {
            _statusLabel.Text = "Impressum wird geladen.";
            _statusLabel.IsVisible = true;

            var info = await _supabaseService.GetImpressumInfoAsync() ?? new ImpressumInfo();
            _allWeitereVorstandsmitglieder = info.WeitereVorstandsmitglieder.ToList();
            _allBauausschussmitglieder = info.WeitereBauausschussmitglieder.ToList();
            ApplyVisibleItems();

            _statusLabel.IsVisible = false;
            _statusLabel.Text = string.Empty;
        }
        catch (Exception)
        {
            _allWeitereVorstandsmitglieder = new List<ImpressumKontaktItem>();
            _allBauausschussmitglieder = new List<ImpressumKontaktItem>();
            ApplyVisibleItems();
            _statusLabel.Text = "Weitere Impressumskontakte konnten aktuell nicht geladen werden.";
            _statusLabel.IsVisible = true;
        }
        finally
        {
            _isBusy = false;
        }
    }

    private void UpdateDemoToggleVisibility()
    {
        _demoToggleRow.IsVisible = IsDemoToggleVisible;
        if (!IsDemoToggleVisible)
            _showDemoDataSwitch.IsToggled = false;
    }

    private void ApplyVisibleItems()
    {
        RenderSection(
            _weitereVorstandContainer,
            FilterVisibleItems(_allWeitereVorstandsmitglieder),
            "Aktuell keine weiteren Vorstandsangaben hinterlegt.");
        RenderSection(
            _bauausschussContainer,
            FilterVisibleItems(_allBauausschussmitglieder),
            "Aktuell keine Angaben zum Bauausschuss hinterlegt.");
    }

    private IReadOnlyCollection<ImpressumKontaktItem> FilterVisibleItems(IEnumerable<ImpressumKontaktItem> items)
    {
        if (ShowDemoData)
            return items.ToList();

        return items.Where(OperationalDataFilter.IsOperationalImpressumKontakt).ToList();
    }

    private static Border CreateStaticSection()
    {
        return new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            BackgroundColor = Colors.White,
            Padding = 16,
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label
                    {
                        Text = "Impressum",
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = ImpressumInfo.VereinsName,
                        FontAttributes = FontAttributes.Bold,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    new Label
                    {
                        Text = ImpressumInfo.VereinsRegister,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    new Label
                    {
                        Text = "Verantwortlich:",
                        Margin = new Thickness(0, 8, 0, 0),
                        FontAttributes = FontAttributes.Bold,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    new Label { Text = ImpressumInfo.VerantwortlichName, LineBreakMode = LineBreakMode.WordWrap },
                    new Label { Text = ImpressumInfo.VerantwortlichStrasse, LineBreakMode = LineBreakMode.WordWrap },
                    new Label { Text = ImpressumInfo.VerantwortlichOrt, LineBreakMode = LineBreakMode.WordWrap },
                    new Label
                    {
                        Text = $"E-Mail: {ImpressumInfo.VereinsEmail}",
                        LineBreakMode = LineBreakMode.WordWrap
                    }
                }
            }
        };
    }

    private static Border CreateDatenschutzSection(Button datenschutzButton)
    {
        return new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            BackgroundColor = Colors.White,
            Padding = 16,
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new Label
                    {
                        Text = "Datenschutz",
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = ImpressumInfo.DatenschutzHinweis,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    datenschutzButton
                }
            }
        };
    }

    private static Border CreateDynamicSection(string title, VerticalStackLayout content)
    {
        return new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            BackgroundColor = Color.FromArgb("#F3F6FA"),
            Padding = 16,
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Text = title,
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold
                    },
                    content
                }
            }
        };
    }

    private static void RenderSection(VerticalStackLayout target, IReadOnlyCollection<ImpressumKontaktItem> items, string fallbackText)
    {
        target.Children.Clear();

        if (items.Count == 0)
        {
            target.Children.Add(new Label
            {
                Text = fallbackText,
                LineBreakMode = LineBreakMode.WordWrap
            });
            return;
        }

        foreach (var item in items)
            target.Children.Add(CreateEntryView(item));
    }

    private static Border CreateEntryView(ImpressumKontaktItem item)
    {
        var layout = new VerticalStackLayout { Spacing = 4 };
        layout.Children.Add(new Label
        {
            Text = item.DisplayName,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.WordWrap
        });

        layout.Children.Add(new Label
        {
            Text = item.DisplayHandyText,
            LineBreakMode = LineBreakMode.WordWrap
        });

        return new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            BackgroundColor = Colors.White,
            Padding = 12,
            Content = layout
        };
    }

    private async Task OpenDatenschutzAsync()
    {
        try
        {
            await Launcher.Default.OpenAsync(ImpressumInfo.DatenschutzUrl);
            _statusLabel.Text = string.Empty;
            _statusLabel.IsVisible = false;
        }
        catch
        {
            _statusLabel.Text = "Datenschutzerklärung konnte nicht geöffnet werden.";
            _statusLabel.IsVisible = true;
        }
    }
}
