using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.Services.Diagnostics;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class MemberGardensPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private readonly MemberContextState _memberContextState;
    private readonly ParzellenContextState _parzellenContextState;
    private readonly ObservableCollection<GartenAssignmentItem> _gardenAssignments = new();

    private readonly Label _headlineLabel;
    private readonly Label _statusLabel;
    private readonly Label _emptyLabel;
    private readonly Button _assignGardenButton;
    private readonly CollectionView _gardensView;

    private bool _isBusy;
    private bool _gardenNavigationInProgress;

    public MemberGardensPage(
        ISupabaseService supabaseService,
        UserContextState userContextState,
        MemberContextState memberContextState,
        ParzellenContextState parzellenContextState)
    {
        _supabaseService = supabaseService;
        _userContextState = userContextState;
        _memberContextState = memberContextState;
        _parzellenContextState = parzellenContextState;

        Title = "Gärten";

        _headlineLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold };
        _statusLabel = new Label { TextColor = Colors.DarkRed, LineBreakMode = LineBreakMode.WordWrap };
        _emptyLabel = new Label
        {
            Text = "Keine aktiven oder historischen Garten-Zuordnungen geladen.",
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap
        };

        _assignGardenButton = new Button { Text = "Parzelle zuordnen", IsVisible = false };
        _assignGardenButton.Clicked += OnAssignGardenClicked;

        _gardensView = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            ItemsSource = _gardenAssignments,
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label { FontAttributes = FontAttributes.Bold };
                title.SetBinding(Label.TextProperty, nameof(GartenAssignmentItem.Title));

                var subtitle = new Label
                {
                    FontSize = 12,
                    TextColor = Colors.Gray,
                    LineBreakMode = LineBreakMode.WordWrap
                };
                subtitle.SetBinding(Label.TextProperty, nameof(GartenAssignmentItem.Subtitle));

                return new Border
                {
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 8),
                    Stroke = Colors.LightGray,
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children = { title, subtitle }
                    }
                };
            })
        };

        _gardensView.SelectionChanged += OnGardenSelectionChanged;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 14,
                Children =
                {
                    _headlineLabel,
                    _statusLabel,
                    CreateSection(
                        "Gärten",
                        new Label
                        {
                            Text = "Diese Ansicht zeigt nur die dem aktuell ausgewählten Mitglied zugewiesenen Parzellen. Tippen öffnet die mitgliedsbezogene Parzellen-Detailansicht.",
                            TextColor = Colors.Gray,
                            LineBreakMode = LineBreakMode.WordWrap
                        },
                        _gardensView,
                        _emptyLabel,
                        _assignGardenButton)
                }
            }
        };

        Appearing += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isBusy)
            return;

        _isBusy = true;
        try
        {
            _statusLabel.Text = string.Empty;
            _gardenAssignments.Clear();

            var selectedMember = _memberContextState.SelectedMember;
            if (selectedMember?.Id is not > 0)
            {
                _headlineLabel.Text = "Kein Mitglied ausgewählt";
                _statusLabel.Text = "Bitte zuerst in der Mitgliedersuche ein Mitglied auswählen.";
                _emptyLabel.IsVisible = true;
                _assignGardenButton.IsVisible = false;
                return;
            }

            var member = await _supabaseService.GetMitgliedByIdAsync(selectedMember.Id);
            if (member == null)
            {
                _headlineLabel.Text = "Gärten";
                _statusLabel.Text = "Das ausgewählte Mitglied konnte nicht geladen werden.";
                _emptyLabel.IsVisible = true;
                _assignGardenButton.IsVisible = false;
                return;
            }

            var contextMember = MapMember(member);
            _memberContextState.SetSelectedMember(contextMember);
            _headlineLabel.Text = string.IsNullOrWhiteSpace(contextMember.DisplayName)
                ? $"Gärten von Mitglied #{contextMember.Id}"
                : $"Gärten von {contextMember.DisplayName}";

            await LoadAssignmentsAsync(contextMember.Id);
            UpdateAssignGardenButtonVisibility(contextMember.Id);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task LoadAssignmentsAsync(int mitgliedId)
    {
        var parzellen = await _supabaseService.GetAllParzellenAsync();
        var belegungen = await _supabaseService.GetBelegungenForMitgliedAsync(mitgliedId);
        var parzellenById = (parzellen ?? new List<ParzelleRecord>())
            .Where(x => x.Id > 0)
            .ToDictionary(x => x.Id);

        foreach (var belegung in (belegungen ?? new List<ParzellenBelegungRecord>()).OrderByDescending(x => x.VonDatum ?? DateTime.MinValue))
        {
            parzellenById.TryGetValue(belegung.ParzelleId, out var parzelle);
            var gartenNr = string.IsNullOrWhiteSpace(parzelle?.GartenNr) ? belegung.ParzelleId.ToString() : parzelle!.GartenNr!;
            var anlage = string.IsNullOrWhiteSpace(parzelle?.Anlage) ? "-" : parzelle.Anlage;
            var bisText = belegung.BisDatum.HasValue ? belegung.BisDatum.Value.ToString("dd.MM.yyyy") : "aktiv";

            _gardenAssignments.Add(new GartenAssignmentItem(
                belegung.ParzelleId,
                $"Garten {gartenNr} ({anlage})",
                $"Von {FormatDate(belegung.VonDatum)} bis {bisText}"));
        }

        _emptyLabel.IsVisible = _gardenAssignments.Count == 0;
    }

    private async void OnGardenSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selected = e.CurrentSelection?.FirstOrDefault() as GartenAssignmentItem;
        _gardensView.SelectedItem = null;
        if (selected == null)
            return;

        if (_gardenNavigationInProgress)
        {
            AppFileLog.Warning("KGV.Navigation", $"Mitgliedsgarten-Navigation unterdrückt. Parzelle {selected.ParzelleId} ist bereits in Bearbeitung.");
            return;
        }

        var selectedMember = _memberContextState.SelectedMember;
        if (selectedMember?.Id is not > 0)
            return;

        _gardenNavigationInProgress = true;
        _gardensView.IsEnabled = false;

        try
        {
            AppFileLog.Info("KGV.Navigation", $"Mitgliedsgarten-Navigation angefordert. Mitglied={selectedMember.Id}, Parzelle={selected.ParzelleId}.");
            _parzellenContextState.SetMemberContext(selectedMember.Id, selected.ParzelleId, _headlineLabel.Text);
            AppFileLog.Info("KGV.Navigation", $"Mitgliedsgarten-Navigation gestartet. Route={nameof(MemberParzellenDetailPage)}, Parzelle={selected.ParzelleId}.");
            await Shell.Current.GoToAsync(nameof(MemberParzellenDetailPage));
        }
        catch (Exception ex)
        {
            AppFileLog.Error("KGV.Navigation", $"Mitgliedsgarten-Navigation fehlgeschlagen. Mitglied={selectedMember.Id}, Parzelle={selected.ParzelleId}.", ex);
            _statusLabel.Text = "Die Parzellen-Detailansicht konnte nicht geöffnet werden.";
        }
        finally
        {
            _gardenNavigationInProgress = false;
            _gardensView.IsEnabled = true;
        }
    }

    private async void OnAssignGardenClicked(object? sender, EventArgs e)
    {
        var selectedMember = _memberContextState.SelectedMember;
        if (selectedMember?.Id is not > 0)
        {
            await DisplayAlert("Hinweis", "Bitte zuerst ein Mitglied auswählen.", "OK");
            return;
        }

        if (!PermissionChecks.CanCreateMitglied(_userContextState.CurrentUserContext))
        {
            await DisplayAlert("Hinweis", "Parzellenzuweisung ist mobil nur mit dem Fachrecht 'CreateMitglied' oder als Admin/Vorstand freigegeben.", "OK");
            return;
        }

        await Navigation.PushAsync(new MemberGardenAssignPage(_supabaseService, _memberContextState, _userContextState));
    }

    private void UpdateAssignGardenButtonVisibility(int mitgliedId)
    {
        _assignGardenButton.IsVisible = mitgliedId > 0
            && PermissionChecks.CanCreateMitglied(_userContextState.CurrentUserContext);
    }

    private static Border CreateSection(string title, params View[] children)
    {
        var content = new VerticalStackLayout { Spacing = 8 };
        content.Children.Add(new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 18 });
        foreach (var child in children)
            content.Children.Add(child);

        return new Border
        {
            Padding = 16,
            Stroke = Colors.LightGray,
            Content = content
        };
    }

    private static string FormatDate(DateTime? value) => value?.ToString("dd.MM.yyyy") ?? "-";

    private static MemberDTO MapMember(MitgliedRecord rec)
    {
        return new MemberDTO
        {
            Id = rec.Id,
            Vorname = rec.Vorname ?? string.Empty,
            Nachname = rec.Name ?? string.Empty,
            Email = rec.Email ?? string.Empty,
            Telefon = rec.Telefon ?? string.Empty,
            Mobilnummer = rec.Handy ?? string.Empty,
            Strasse = rec.Adresse ?? string.Empty,
            PLZ = rec.Plz ?? string.Empty,
            Ort = rec.Ort ?? string.Empty,
            Geburtsdatum = rec.Geburtsdatum,
            MitgliedSeit = rec.MitgliedSeit,
            MitgliedEnde = rec.MitgliedEnde,
            Bemerkungen = rec.Bemerkung ?? string.Empty,
            WhatsappEinwilligung = rec.WhatsappEinwilligung,
            IstHauptmitglied = !rec.HauptmitgliedId.HasValue || rec.HauptmitgliedId.Value <= 0,
            Role = rec.Role ?? string.Empty
        };
    }

    private sealed record GartenAssignmentItem(int ParzelleId, string Title, string Subtitle);
}
