using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public class DokumentePage : ContentPage, IQueryAttributable
{
    private readonly ISupabaseService _supabaseService;
    private readonly MemberContextState _memberContextState;
    private readonly ParzellenContextState _parzellenContextState;
    private readonly ObservableCollection<DocumentInfo> _documents = new();
    private readonly Label _headlineLabel;
    private readonly Label _contextLabel;
    private readonly Label _hintLabel;
    private readonly Label _statusLabel;
    private readonly Label _emptyLabel;
    private readonly CollectionView _documentsView;
    private bool _isBusy;
    private bool _loadParzelleDocuments;
    private int? _requestedParzelleId;

    public DokumentePage(ISupabaseService supabaseService, MemberContextState memberContextState, ParzellenContextState parzellenContextState)
    {
        _supabaseService = supabaseService;
        _memberContextState = memberContextState;
        _parzellenContextState = parzellenContextState;

        Title = "Dokumente";

        _headlineLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold };
        _contextLabel = new Label { FontAttributes = FontAttributes.Bold, FontSize = 16 };
        _hintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        _emptyLabel = new Label { Text = "Keine Mitgliedsdokumente gefunden.", TextColor = Colors.Gray, IsVisible = false };

        _documentsView = new CollectionView
        {
            ItemsSource = _documents,
            SelectionMode = SelectionMode.None,
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label { FontAttributes = FontAttributes.Bold };
                title.SetBinding(Label.TextProperty, nameof(DocumentInfo.Name));

                var subtitle = new Label { FontSize = 12, TextColor = Colors.Gray };
                subtitle.SetBinding(Label.TextProperty, new Binding(nameof(DocumentInfo.UpdatedAt), stringFormat: "Aktualisiert: {0:dd.MM.yyyy HH:mm}"));

                var actionButton = new Button { Text = "Einsehen / Download" };
                actionButton.Clicked += async (_, _) =>
                {
                    if (actionButton.BindingContext is DocumentInfo document)
                        await OpenDocumentAsync(document);
                };

                return new Border
                {
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 8),
                    Stroke = Colors.LightGray,
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children = { title, subtitle, actionButton }
                    }
                };
            })
        };

        var refreshButton = new Button { Text = "Dokumente aktualisieren" };
        refreshButton.Clicked += async (_, _) => await LoadAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    _headlineLabel,
                    _contextLabel,
                    _hintLabel,
                    refreshButton,
                    _statusLabel,
                    _emptyLabel,
                    _documentsView
                }
            }
        };

        Appearing += async (_, _) => await LoadAsync();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var scope = TryGetQueryString(query, "scope");
        _loadParzelleDocuments = string.Equals(scope, "parzelle", StringComparison.OrdinalIgnoreCase);
        _requestedParzelleId = TryGetQueryInt(query, "parzelleId");
    }

    private async Task LoadAsync()
    {
        if (_isBusy)
            return;

        _isBusy = true;
        try
        {
            _statusLabel.Text = string.Empty;
            _documents.Clear();
            _emptyLabel.IsVisible = false;

            if (_loadParzelleDocuments)
            {
                await LoadParzelleDocumentsAsync();
                return;
            }

            var member = _memberContextState.SelectedMember;
            if (member?.Id is not > 0)
            {
                _headlineLabel.Text = "Keine Mitgliedsdokumente verfügbar";
                _contextLabel.Text = string.Empty;
                _hintLabel.Text = "Bitte zuerst in der Mitgliedersuche ein Mitglied auswählen.";
                return;
            }

            _headlineLabel.Text = $"Dokumente – {member.DisplayName}";
            _contextLabel.Text = member.DisplayName;
            _hintLabel.Text = "Es werden nur die Dokumente des aktuell ausgewählten Mitglieds angezeigt. Einsehen und Download laufen über den bestehenden Dokumentpfad.";
            _emptyLabel.Text = "Keine Mitgliedsdokumente gefunden.";
            var documents = await _supabaseService.GetMitgliedDokumenteAsync(member.Id);
            foreach (var document in documents)
                _documents.Add(document);

            _emptyLabel.IsVisible = _documents.Count == 0;
        }
        catch (Exception)
        {
            _statusLabel.Text = "Dokumente konnten nicht geladen werden.";
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task LoadParzelleDocumentsAsync()
    {
        var parzelleId = _requestedParzelleId ?? _parzellenContextState.SelectedParzelleId;
        if (!_parzellenContextState.IsFromMemberContext || parzelleId is not > 0)
        {
            _headlineLabel.Text = "Keine Parzellendokumente verfügbar";
            _contextLabel.Text = string.Empty;
            _hintLabel.Text = "Bitte zuerst im Pfad `Gärten des Mitgliedes` eine Parzelle auswählen.";
            return;
        }

        var detail = await _supabaseService.GetParzelleDetailAsync(parzelleId.Value);
        _headlineLabel.Text = detail == null
            ? $"Parzellen-Dokumente – Parzelle #{parzelleId.Value}"
            : $"Parzellen-Dokumente – {detail.DisplayName}";
        _contextLabel.Text = detail?.DisplayName ?? $"Parzelle #{parzelleId.Value}";
        _hintLabel.Text = "Es werden nur die Dokumente dieser aktuell ausgewählten Parzelle angezeigt. Einsehen und Download laufen über den bestehenden Dokumentpfad.";
        _emptyLabel.Text = "Keine Dokumente für diese Parzelle gefunden.";

        var documents = await _supabaseService.GetParzelleDokumenteAsync(parzelleId.Value);
        foreach (var document in documents)
            _documents.Add(document);

        _emptyLabel.IsVisible = _documents.Count == 0;
    }

    private async Task OpenDocumentAsync(DocumentInfo document)
    {
        try
        {
            var url = await _supabaseService.ResolveDokumentOpenUrlAsync(document, 3600);
            if (string.IsNullOrWhiteSpace(url))
            {
                _statusLabel.Text = "Dokument konnte nicht geöffnet werden.";
                return;
            }

            await Launcher.Default.OpenAsync(url);
        }
        catch (Exception)
        {
            _statusLabel.Text = "Dokument konnte nicht geöffnet werden.";
        }
    }

    private static int? TryGetQueryInt(IDictionary<string, object> query, string key)
    {
        var raw = TryGetQueryString(query, key);
        return int.TryParse(raw, out var value) && value > 0 ? value : null;
    }

    private static string? TryGetQueryString(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var raw) || raw == null)
            return null;

        var value = raw.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : Uri.UnescapeDataString(value);
    }
}
