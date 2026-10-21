using KGV.Core.Interfaces;
using KGV.Core.Models;
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
    private readonly VerticalStackLayout _kontaktgruppenContainer;
    private readonly Label _statusLabel;
    private readonly Label _vereinsnameLabel;
    private readonly Label _registerLabel;
    private readonly Label _verantwortlichNameLabel;
    private readonly Label _verantwortlichAdresseLabel;
    private readonly Label _emailLabel;
    private readonly UserContextState _userContextState;
    private List<ImpressumKontaktGruppe> _kontaktgruppen = new();
    private bool _isBusy;

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

        _kontaktgruppenContainer = new VerticalStackLayout { Spacing = 12 };
        _vereinsnameLabel = new Label { FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap };
        _registerLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _verantwortlichNameLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _verantwortlichAdresseLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _emailLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };

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
                        Text = "Vereinskopf und Verantwortlichkeit aus der aktiven Vereinskonfiguration sowie den aktuellen Vereinsfunktionen.",
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    CreateClubSection(),
                    _kontaktgruppenContainer,
                    CreateDatenschutzSection(datenschutzButton),
                    _statusLabel
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
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

            var info = await _supabaseService.GetImpressumInfoAsync(_userContextState.CurrentUserContext?.IsDemoAccount == true) ?? new ImpressumInfo();
            ApplyClubInfo(info);
            _kontaktgruppen = info.Kontaktgruppen.ToList();
            ApplyVisibleItems();

            _statusLabel.IsVisible = false;
            _statusLabel.Text = string.Empty;
        }
        catch (Exception)
        {
            _kontaktgruppen = new List<ImpressumKontaktGruppe>();
            ApplyClubInfo(new ImpressumInfo());
            ApplyVisibleItems();
            _statusLabel.Text = "Weitere Impressumskontakte konnten aktuell nicht geladen werden.";
            _statusLabel.IsVisible = true;
        }
        finally
        {
            _isBusy = false;
        }
    }

    private void ApplyVisibleItems()
    {
        _kontaktgruppenContainer.Children.Clear();
        foreach (var gruppe in _kontaktgruppen)
            _kontaktgruppenContainer.Children.Add(CreateDynamicSection(gruppe.Bezeichnung, CreateEntries(gruppe.Kontakte)));
    }

    private static VerticalStackLayout CreateEntries(IEnumerable<ImpressumKontaktItem> items)
    {
        var target = new VerticalStackLayout { Spacing = 12 };
        RenderSection(target, items.ToList(), "Aktuell keine Angaben hinterlegt.");
        return target;
    }

    private void ApplyClubInfo(ImpressumInfo info)
    {
        var configuration = info.Vereinskonfiguration;
        _vereinsnameLabel.Text = string.IsNullOrWhiteSpace(configuration?.Vereinsname) ? "Aktuell nicht hinterlegt" : configuration.Vereinsname.Trim();
        _registerLabel.Text = string.IsNullOrWhiteSpace(configuration?.Registerangabe) ? "Aktuell nicht hinterlegt" : configuration.Registerangabe.Trim();
        _verantwortlichNameLabel.Text = info.Verantwortliche.Count == 0 ? "Aktuell nicht hinterlegt." : string.Join("\n", info.Verantwortliche.Select(x => x.DisplayName));
        _verantwortlichAdresseLabel.Text = string.Join("\n", info.Verantwortliche.Where(x => x.ShowAdresse).Select(x => x.Adresse));
        _emailLabel.Text = string.IsNullOrWhiteSpace(configuration?.StandardEmail) ? "Aktuell nicht hinterlegt" : $"E-Mail: {configuration.StandardEmail.Trim()}";
    }

    private Border CreateClubSection()
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
                    _vereinsnameLabel,
                    _registerLabel,
                    new Label
                    {
                        Text = "Verantwortlich:",
                        Margin = new Thickness(0, 8, 0, 0),
                        FontAttributes = FontAttributes.Bold,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    _verantwortlichNameLabel,
                    _verantwortlichAdresseLabel,
                    _emailLabel
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
            Text = string.IsNullOrWhiteSpace(item.Funktion) ? "Funktion" : item.Funktion,
            LineBreakMode = LineBreakMode.WordWrap
        });
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
