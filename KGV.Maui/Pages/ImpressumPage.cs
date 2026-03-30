using KGV.Core.Interfaces;
using KGV.Core.Models;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class ImpressumPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly VerticalStackLayout _vorstandContainer;
    private readonly VerticalStackLayout _bauausschussContainer;
    private readonly Label _statusLabel;
    private bool _isBusy;

    public ImpressumPage(ISupabaseService supabaseService)
    {
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        Title = "Impressum";

        _statusLabel = new Label
        {
            TextColor = Colors.DarkSlateBlue,
            LineBreakMode = LineBreakMode.WordWrap,
            IsVisible = false
        };

        _vorstandContainer = new VerticalStackLayout { Spacing = 12 };
        _bauausschussContainer = new VerticalStackLayout { Spacing = 12 };

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
                        Text = "Reiner Informationsbereich mit den statischen Vereinsangaben sowie den aktuell in Supabase hinterlegten Funktionen für Vorstand und Bauausschuss.",
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    CreateStaticSection(),
                    CreateDynamicSection("Vorstand", _vorstandContainer),
                    CreateDynamicSection("Bauausschuss", _bauausschussContainer),
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

            var info = await _supabaseService.GetImpressumInfoAsync() ?? new ImpressumInfo();
            RenderSection(_vorstandContainer, info.Vorstand, "Aktuell keine Vorstandsangaben hinterlegt.");
            RenderSection(_bauausschussContainer, info.Bauausschuss, "Aktuell keine Angaben zum Bauausschuss hinterlegt.");

            _statusLabel.IsVisible = false;
            _statusLabel.Text = string.Empty;
        }
        catch (Exception ex)
        {
            RenderSection(_vorstandContainer, Array.Empty<ImpressumKontaktItem>(), "Aktuell keine Vorstandsangaben hinterlegt.");
            RenderSection(_bauausschussContainer, Array.Empty<ImpressumKontaktItem>(), "Aktuell keine Angaben zum Bauausschuss hinterlegt.");
            _statusLabel.Text = $"Impressum konnte nicht geladen werden: {ex.Message}";
            _statusLabel.IsVisible = true;
        }
        finally
        {
            _isBusy = false;
        }
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
                        Text = "Verein",
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = "Kleingartenverein Oberrothenbach e.V.",
                        FontAttributes = FontAttributes.Bold,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    new Label
                    {
                        Text = "Amtsgericht Chemnitz VR 70502",
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    new Label
                    {
                        Text = $"E-Mail: {ImpressumInfo.VereinsEmail}",
                        LineBreakMode = LineBreakMode.WordWrap
                    }
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
        if (item.IsVorstandsvorsitzende)
        {
            layout.Children.Add(new Label
            {
                Text = item.Funktion,
                FontAttributes = FontAttributes.Bold,
                LineBreakMode = LineBreakMode.WordWrap
            });
        }

        layout.Children.Add(new Label
        {
            Text = item.DisplayName,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.WordWrap
        });

        if (item.ShowAdresse)
        {
            layout.Children.Add(new Label
            {
                Text = $"Adresse: {item.Adresse}",
                LineBreakMode = LineBreakMode.WordWrap
            });
        }

        if (item.HasHandy)
        {
            layout.Children.Add(new Label
            {
                Text = $"Handynummer: {item.Handy}",
                LineBreakMode = LineBreakMode.WordWrap
            });
        }

        return new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            BackgroundColor = Colors.White,
            Padding = 12,
            Content = layout
        };
    }
}
