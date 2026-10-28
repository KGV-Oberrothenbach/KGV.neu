using System.Collections.ObjectModel;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.Services.Documents;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

/// <summary>Mobile surface for the server-owned membership-admission board workflow.</summary>
public sealed class MembershipBoardReviewPage : ContentPage
{
    private readonly ISupabaseService _supabase;
    private readonly UserContextState _user;
    private readonly ObservableCollection<MembershipBoardReviewRecord> _items = [];
    private readonly CollectionView _list;
    private readonly Label _status = new() { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
    private bool _loading;

    public MembershipBoardReviewPage(ISupabaseService supabase, UserContextState user)
    {
        _supabase = supabase;
        _user = user;
        Title = "Vorstand";
        _list = new CollectionView { ItemsSource = _items, SelectionMode = SelectionMode.Single, ItemTemplate = new DataTemplate(CreateItem) };
        _list.SelectionChanged += async (_, args) =>
        {
            if (args.CurrentSelection.FirstOrDefault() is MembershipBoardReviewRecord review) await OpenAsync(review);
            _list.SelectedItem = null;
        };
        var refresh = new RefreshView { Content = _list };
        refresh.Refreshing += async (_, _) => { await LoadAsync(); refresh.IsRefreshing = false; };
        Content = new VerticalStackLayout { Padding = 20, Spacing = 12, Children =
        {
            new Label { Text = "Vorstand · Mitgliedsanträge", FontSize = 24, FontAttributes = FontAttributes.Bold },
            new Label { Text = "Stimmen und Entscheidungen werden serverseitig geprüft.", TextColor = Colors.Gray }, _status, refresh
        }};
    }

    protected override async void OnAppearing() { base.OnAppearing(); await LoadAsync(); }

    private async Task LoadAsync()
    {
        if (_loading) return;
        _loading = true; _status.Text = "Vorstandsbearbeitungen werden geladen...";
        try
        {
            var rows = await _supabase.GetMembershipBoardReviewsAsync();
            _items.Clear(); foreach (var row in rows) _items.Add(row);
            _status.Text = rows.Count == 0 ? "Keine für Sie verfügbaren Mitgliedsanträge." : string.Empty;
        }
        catch { _status.Text = "Vorstandsbearbeitungen konnten aktuell nicht geladen werden."; }
        finally { _loading = false; }
    }

    private View CreateItem()
    {
        var name = new Label { FontSize = 17, FontAttributes = FontAttributes.Bold }; name.SetBinding(Label.TextProperty, nameof(MembershipBoardReviewRecord.Antragsteller));
        var progress = new Label { TextColor = Colors.Gray }; progress.SetBinding(Label.TextProperty, new Binding(".", converter: new BoardReviewProgressConverter()));
        var state = new Label { FontSize = 12 }; state.SetBinding(Label.TextProperty, new Binding(".", converter: new BoardReviewStateConverter()));
        return new Border { Stroke = Colors.LightGray, Padding = 12, Margin = new Thickness(0, 0, 0, 8), Content = new VerticalStackLayout { Spacing = 4, Children = { name, progress, state } } };
    }

    private async Task OpenAsync(MembershipBoardReviewRecord review)
    {
        MembershipBoardReviewDetail? detail;
        try { detail = await _supabase.GetMembershipBoardReviewDetailAsync(review.BearbeitungId); }
        catch { await DisplayAlertAsync("Vorstand", "Der Mitgliedsantrag ist nicht verfügbar.", "OK"); return; }
        if (detail is null) { await DisplayAlertAsync("Vorstand", "Der Mitgliedsantrag ist nicht verfügbar.", "OK"); return; }
        await Navigation.PushAsync(new MembershipBoardReviewDetailPage(_supabase, _user, detail, LoadAsync));
    }
}

internal sealed class MembershipBoardReviewDetailPage : ContentPage
{
    private readonly ISupabaseService _supabase;
    private readonly UserContextState _user;
    private readonly Func<Task> _onChanged;
    private MembershipBoardReviewDetail _detail;
    private bool _canDecide;
    private readonly VerticalStackLayout _content = new() { Padding = 20, Spacing = 10 };

    public MembershipBoardReviewDetailPage(ISupabaseService supabase, UserContextState user, MembershipBoardReviewDetail detail, Func<Task> onChanged)
    { _supabase = supabase; _user = user; _detail = detail; _onChanged = onChanged; Title = "Mitgliedsantrag"; Content = new ScrollView { Content = _content }; Build(); }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try { _canDecide = await _supabase.CanDecideMembershipBoardReviewAsync(); }
        catch { _canDecide = false; }
        Build();
    }

    private void Build()
    {
        _content.Children.Clear(); var review = _detail.Review;
        _content.Children.Add(new Label { Text = review.Antragsteller, FontSize = 24, FontAttributes = FontAttributes.Bold });
        _content.Children.Add(new Label { Text = BoardReviewText.Progress(review), TextColor = Colors.Gray });
        _content.Children.Add(new Label { Text = BoardReviewText.State(review), TextColor = review.Konflikt ? Colors.DarkRed : Colors.Gray });
        var document = new Button { Text = "Mitgliedsantrag öffnen" }; document.Clicked += async (_, _) => await OpenDocumentAsync(); _content.Children.Add(document);
        _content.Children.Add(new Label { Text = "Snapshot-Stimmen", FontAttributes = FontAttributes.Bold, FontSize = 18 });
        foreach (var vote in _detail.Stimmen)
        {
            var source = vote.Berechtigungsquellen.Count == 0 ? "Berechtigungsquelle nicht verfügbar" : string.Join(" · ", vote.Berechtigungsquellen);
            var text = $"{vote.Name}: {VoteText(vote.Stimme)}\n{source}";
            if (!string.IsNullOrWhiteSpace(vote.NeinBegruendung)) text += $"\nBegründung: {vote.NeinBegruendung}";
            _content.Children.Add(new Border { Stroke = Colors.LightGray, Padding = 10, Content = new Label { Text = text, LineBreakMode = LineBreakMode.WordWrap } });
        }
        if (review.Entscheidung != "offen") return;
        var ownVote = _detail.Stimmen.FirstOrDefault(x => x.MitgliedId == _user.CurrentMitgliedId);
        if (ownVote is not null)
        {
            _content.Children.Add(new Label { Text = "Eigene Stimme", FontAttributes = FontAttributes.Bold, FontSize = 18 });
            var yes = new Button { Text = ownVote.Stimme == "ja" ? "Ja bestätigt" : "Ja stimmen" }; yes.Clicked += async (_, _) => await VoteAsync("ja");
            var no = new Button { Text = ownVote.Stimme == "nein" ? "Nein ändern" : "Nein stimmen", TextColor = Colors.DarkRed }; no.Clicked += async (_, _) => await VoteAsync("nein");
            _content.Children.Add(yes); _content.Children.Add(no);
        }
        if (!_canDecide) return;
        _content.Children.Add(new Label { Text = "Entscheidung", FontAttributes = FontAttributes.Bold, FontSize = 18 });
        var accept = new Button { Text = "Aufnahme entscheiden" }; accept.Clicked += async (_, _) => await DecideAsync("annehmen");
        var reject = new Button { Text = "Antrag ablehnen", TextColor = Colors.DarkRed }; reject.Clicked += async (_, _) => await DecideAsync("ablehnen");
        _content.Children.Add(accept); _content.Children.Add(reject);
    }

    private async Task VoteAsync(string vote)
    {
        var reason = vote == "nein" ? await DisplayPromptAsync("Nein-Stimme", "Persönliche Begründung (optional):", "Speichern", "Abbrechen") : null;
        try { await _supabase.VoteMembershipBoardReviewAsync(_detail.Review.BearbeitungId, vote, reason); await RefreshAsync(); }
        catch (Exception ex) { await DisplayAlertAsync("Stimmabgabe", VoteError(ex), "OK"); }
    }

    private async Task DecideAsync(string result)
    {
        bool permitted;
        try { permitted = await _supabase.CanDecideMembershipBoardReviewAsync(); }
        catch { permitted = false; }
        if (!permitted) { await DisplayAlertAsync("Entscheidung", "Sie sind aktuell nicht entscheidungsberechtigt.", "OK"); return; }
        var unanimous = _detail.Review.EinstimmigJa;
        var path = unanimous ? "einstimmig" : "vorstandssitzung";
        DateOnly? meeting = null;
        if (!unanimous)
        {
            meeting = await PromptDateAsync("Vorstandssitzung", "Sitzungsdatum (JJJJ-MM-TT):", required: true);
            if (meeting is null) return;
        }
        var reason = result == "ablehnen" ? await DisplayPromptAsync("Ablehnung", "Abschlussbegründung:", "Weiter", "Abbrechen") : null;
        if (result == "ablehnen" && string.IsNullOrWhiteSpace(reason)) { await DisplayAlertAsync("Ablehnung", "Eine Abschlussbegründung ist erforderlich.", "OK"); return; }
        var admission = result == "annehmen" ? await PromptDateAsync("Aufnahme", "Aufnahmedatum (JJJJ-MM-TT):", required: true) : null;
        if (result == "annehmen" && admission is null) return;
        var title = result == "annehmen" ? "Aufnahme bestätigen" : "Antrag ablehnen";
        if (!await DisplayAlertAsync(title, unanimous ? "Die einstimmige Aufnahme jetzt verbindlich entscheiden?" : "Die Entscheidung aus der Vorstandssitzung jetzt verbindlich speichern?", "Entscheiden", "Abbrechen")) return;
        try { await _supabase.DecideMembershipBoardReviewAsync(_detail.Review.BearbeitungId, result, path, meeting, reason, admission); await RefreshAsync(); await DisplayAlertAsync("Entscheidung", result == "annehmen" ? "Die Aufnahme wurde entschieden." : "Der Antrag wurde abgelehnt.", "OK"); }
        catch (Exception ex) { await DisplayAlertAsync("Entscheidung", DecisionError(ex), "OK"); }
    }

    private async Task RefreshAsync()
    {
        var refreshed = await _supabase.GetMembershipBoardReviewDetailAsync(_detail.Review.BearbeitungId);
        if (refreshed is not null) { _detail = refreshed; Build(); }
        await _onChanged();
    }

    private async Task OpenDocumentAsync()
    {
        try
        {
            if (_detail.Review.FinalDokumentId is not long documentId) throw new InvalidOperationException();
            var document = await _supabase.GetDokumentByIdAsync(documentId);
            if (document is null) throw new InvalidOperationException();
            var bytes = await _supabase.DownloadDokumentContentAsync(document);
            if (bytes is not { Length: > 0 }) throw new InvalidOperationException();
            if (string.Equals(document.MimeType, "application/pdf", StringComparison.OrdinalIgnoreCase) || document.Dateiname.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) await Navigation.PushAsync(new PdfViewerPage(document.Dateiname, bytes));
            else if (!await DocumentStorage.OpenTemporaryFileAsync(document.Dateiname, document.MimeType, bytes)) throw new InvalidOperationException();
        }
        catch { await DisplayAlertAsync("Mitgliedsantrag", "Das Dokument ist nicht verfügbar.", "OK"); }
    }

    private async Task<DateOnly?> PromptDateAsync(string title, string message, bool required)
    {
        var value = await DisplayPromptAsync(title, message, "Weiter", "Abbrechen", initialValue: DateTime.Today.ToString("yyyy-MM-dd"));
        if (string.IsNullOrWhiteSpace(value)) { if (required) await DisplayAlertAsync(title, "Ein Datum ist erforderlich.", "OK"); return null; }
        if (!DateOnly.TryParse(value, out var date)) { await DisplayAlertAsync(title, "Bitte ein gültiges Datum im Format JJJJ-MM-TT eingeben.", "OK"); return null; }
        return date;
    }
    private static string VoteText(string? vote) => vote == "ja" ? "Ja" : vote == "nein" ? "Nein" : "Noch keine Stimme";
    private static string VoteError(Exception ex) => ex.Message.Contains("abgeschlossen", StringComparison.OrdinalIgnoreCase) ? "Die Bearbeitung ist bereits abgeschlossen." : "Sie sind nicht stimmberechtigt oder die Stimme konnte nicht gespeichert werden.";
    private static string DecisionError(Exception ex) => ex.Message.Contains("Sitzungsdatum", StringComparison.OrdinalIgnoreCase) ? "Ein Sitzungsdatum ist erforderlich." : ex.Message.Contains("Begründung", StringComparison.OrdinalIgnoreCase) ? "Eine Abschlussbegründung ist erforderlich." : ex.Message.Contains("Aufnahmedatum", StringComparison.OrdinalIgnoreCase) ? "Ein Aufnahmedatum ist erforderlich." : ex.Message.Contains("Einstimmig", StringComparison.OrdinalIgnoreCase) ? "Der einstimmige Weg ist nicht möglich." : ex.Message.Contains("Berechtigung", StringComparison.OrdinalIgnoreCase) ? "Sie sind aktuell nicht entscheidungsberechtigt." : "Die Entscheidung konnte nicht gespeichert werden.";
}

internal static class BoardReviewText
{
    public static string Progress(MembershipBoardReviewRecord review) => $"Stimmen: {review.AbgegebeneStimmen}/{review.SnapshotPersonen} · Ja {review.JaStimmen} · Nein {review.NeinStimmen}";
    public static string State(MembershipBoardReviewRecord review) => review.Entscheidung == "angenommen" ? "Aufnahme bestätigt" : review.Entscheidung == "abgelehnt" ? "Aufnahme abgelehnt" : review.EinstimmigJa ? "Einstimmige Zustimmung" : review.Konflikt ? "Konflikt: Gegenstimme vorhanden" : "Entscheidung offen";
}
internal sealed class BoardReviewProgressConverter : IValueConverter { public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => value is MembershipBoardReviewRecord review ? BoardReviewText.Progress(review) : string.Empty; public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotSupportedException(); }
internal sealed class BoardReviewStateConverter : IValueConverter { public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => value is MembershipBoardReviewRecord review ? BoardReviewText.State(review) : string.Empty; public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotSupportedException(); }
