using System.Collections.ObjectModel;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

internal sealed class MemberClubDocumentsSection : Border
{
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private readonly Page _page;
    private readonly ObservableCollection<StatusItem> _statusItems = new();
    private readonly ObservableCollection<HistoryItem> _historyItems = new();
    private readonly Label _summary = new() { FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap };
    private readonly Label _notice = new() { TextColor = Colors.DarkOrange, LineBreakMode = LineBreakMode.WordWrap };
    private readonly Label _message = new() { TextColor = Colors.DarkRed, LineBreakMode = LineBreakMode.WordWrap };
    private readonly VerticalStackLayout _selectionHost = new() { Spacing = 4 };
    private readonly Button _digitalButton = new() { Text = "Fehlende Unterlagen digital senden" };
    private readonly Button _paperButton = new() { Text = "Papierübergabe erfassen" };
    private readonly Border _paperForm = new() { IsVisible = false, Stroke = Colors.LightGray, Padding = 10 };
    private readonly DatePicker _paperDate = new() { Date = DateTime.Today };
    private readonly TimePicker _paperTime = new() { Time = DateTime.Now.TimeOfDay };
    private readonly Switch _paperConfirmed = new();
    private MitgliedRecord? _member;
    private bool _busy;

    public MemberClubDocumentsSection(ISupabaseService supabaseService, UserContextState userContextState, Page page)
    {
        _supabaseService = supabaseService;
        _userContextState = userContextState;
        _page = page;
        Stroke = Colors.LightGray; StrokeThickness = 1; Padding = 14;
        _digitalButton.Clicked += async (_, _) => await SendDigitalAsync();
        _paperButton.Clicked += (_, _) => OpenPaperForm();
        var savePaper = new Button { Text = "Übergabe erfassen" };
        savePaper.Clicked += async (_, _) => await RecordPaperAsync();
        var cancelPaper = new Button { Text = "Abbrechen" };
        cancelPaper.Clicked += (_, _) => _paperForm.IsVisible = false;
        _paperForm.Content = new VerticalStackLayout { Spacing = 8, Children =
        {
            new Label { Text = "Papierübergabe bestätigen", FontAttributes = FontAttributes.Bold },
            new Label { Text = "Tatsächliches Übergabedatum" }, _paperDate,
            new Label { Text = "Tatsächliche Übergabezeit" }, _paperTime,
            new HorizontalStackLayout { Spacing = 8, Children = { _paperConfirmed, new Label { Text = "Die ausgewählten Unterlagen wurden dem Mitglied tatsächlich in Papierform übergeben.", LineBreakMode = LineBreakMode.WordWrap, VerticalTextAlignment = TextAlignment.Center } } },
            new HorizontalStackLayout { Spacing = 8, Children = { savePaper, cancelPaper } }
        }};
        Content = new VerticalStackLayout { Spacing = 10, Children =
        {
            new Label { Text = "Vereinsunterlagen", FontSize = 18, FontAttributes = FontAttributes.Bold },
            _summary, _notice, _message,
            new Label { Text = "Aktueller Status", FontAttributes = FontAttributes.Bold },
            CreateCollection(_statusItems, CreateStatusView, 280),
            new Label { Text = "Fehlende aktuelle Unterlagen auswählen", FontAttributes = FontAttributes.Bold },
            _selectionHost,
            new FlexLayout { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, Children = { _digitalButton, _paperButton } },
            _paperForm,
            new Label { Text = "Bereitstellungshistorie", FontAttributes = FontAttributes.Bold },
            CreateCollection(_historyItems, CreateHistoryView, 240)
        }};
    }

    private bool CanManage => _userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand;
    private static CollectionView CreateCollection<T>(ObservableCollection<T> source, Func<View> template, double height) => new() { ItemsSource = source, SelectionMode = SelectionMode.None, HeightRequest = height, ItemTemplate = new DataTemplate(template) };

    public async Task LoadAsync(MitgliedRecord? member)
    {
        _member = member; IsVisible = CanManage && member?.Id > 0;
        if (!IsVisible || _busy) return;
        _busy = true; _message.Text = string.Empty; _paperForm.IsVisible = false;
        try
        {
            var status = await _supabaseService.GetMitgliedVereinsdokumentStatusAsync(member!.Id, DateOnly.FromDateTime(DateTime.Today));
            var history = await _supabaseService.GetMitgliedVereinsdokumentBereitstellungHistorieAsync(member.Id);
            _statusItems.Clear(); foreach (var item in status.Dokumente) _statusItems.Add(new StatusItem(item));
            _historyItems.Clear(); foreach (var item in history.OrderByDescending(item => item.BereitgestelltAm).ThenByDescending(item => item.BereitstellungId)) _historyItems.Add(new HistoryItem(item));
            BuildSelection(status.Dokumente);
            _summary.Text = status.Vollstaendig ? "Pflichtunterlagen vollständig" : status.Dokumente.Any(item => item.Status == "fehlt") ? "Unterlagen fehlen beim Mitglied" : "Keine aktuellen Pflichtunterlagen fehlen.";
            _notice.Text = status.HatFehlendeVereinsfassungen ? "Für mindestens eine Pflichtunterlage ist keine aktuelle Vereinsfassung hinterlegt." : string.Empty;
        }
        catch (Exception ex) { _message.Text = ToUserMessage(ex); }
        finally { _busy = false; }
    }

    private void BuildSelection(IReadOnlyList<MitgliedVereinsdokumentStatusRecord> documents)
    {
        _selectionHost.Children.Clear();
        foreach (var document in documents.Where(item => item.Status == "fehlt" && item.VereinsdokumentFassungId.HasValue))
        {
            var toggle = new Switch { IsToggled = true };
            toggle.BindingContext = document;
            toggle.Toggled += (_, _) => UpdateActionState();
            _selectionHost.Children.Add(new HorizontalStackLayout { Spacing = 8, Children = { toggle, new Label { Text = $"{document.Bezeichnung} · Fassung {document.Fassung}", VerticalTextAlignment = TextAlignment.Center, LineBreakMode = LineBreakMode.WordWrap } } });
        }
        UpdateActionState();
    }

    private IReadOnlyList<MitgliedVereinsdokumentStatusRecord> SelectedDocuments() => _selectionHost.Children.OfType<HorizontalStackLayout>()
        .Where(row => row.Children.OfType<Switch>().FirstOrDefault()?.IsToggled == true)
        .Select(row => row.Children.OfType<Switch>().First().BindingContext as MitgliedVereinsdokumentStatusRecord)
        .Where(item => item?.VereinsdokumentFassungId.HasValue == true).Cast<MitgliedVereinsdokumentStatusRecord>().ToList();
    private void UpdateActionState() { var enabled = !_busy && SelectedDocuments().Count > 0; _digitalButton.IsEnabled = enabled; _paperButton.IsEnabled = enabled; }

    private async Task SendDigitalAsync()
    {
        var selected = SelectedDocuments(); if (_member == null || selected.Count == 0) return;
        var summary = string.Join("\n", selected.Select(item => $"• {item.Bezeichnung} ({item.Fassung})"));
        if (!await _page.DisplayAlertAsync("Digital senden", $"Mitglied: {_member.Vorname} {_member.Name}\nE-Mail: {_member.Email ?? "nicht hinterlegt"}\n\n{summary}", "Senden", "Abbrechen")) return;
        _busy = true; UpdateActionState(); _message.Text = string.Empty;
        try
        {
            var result = await _supabaseService.SendMitgliedVereinsunterlagenDigitalAsync(_member.Id, selected.Select(item => item.VereinsdokumentFassungId!.Value).ToList());
            if (!result.Success) { _message.Text = ToUserMessage(result); return; }
            _busy = false; UpdateActionState(); await LoadAsync(_member); _message.Text = "Die Vereinsunterlagen wurden digital bereitgestellt.";
        }
        catch (Exception ex) { _message.Text = ToUserMessage(ex); }
        finally { _busy = false; UpdateActionState(); }
    }

    private void OpenPaperForm()
    {
        if (SelectedDocuments().Count == 0) return;
        _paperDate.Date = DateTime.Today; _paperTime.Time = DateTime.Now.TimeOfDay; _paperConfirmed.IsToggled = false; _paperForm.IsVisible = true;
    }

    private async Task RecordPaperAsync()
    {
        var selected = SelectedDocuments(); if (_member == null || selected.Count == 0) return;
        var deliveredAt = _paperDate.Date!.Value.Date.Add(_paperTime.Time);
        if (deliveredAt > DateTime.Now) { _message.Text = "Eine Papierübergabe darf nicht in der Zukunft liegen."; return; }
        if (!_paperConfirmed.IsToggled) { _message.Text = "Bitte bestätigen Sie die tatsächliche Papierübergabe."; return; }
        _busy = true; UpdateActionState(); _message.Text = string.Empty;
        try
        {
            await _supabaseService.RecordMitgliedVereinsunterlagenPapierAsync(new MitgliedVereinsunterlagenBereitstellungRequest { MitgliedId = _member.Id, VereinsdokumentFassungIds = selected.Select(item => item.VereinsdokumentFassungId!.Value).ToList(), BereitgestelltAm = deliveredAt });
            _busy = false; UpdateActionState(); await LoadAsync(_member); _message.Text = "Die Papierübergabe wurde erfasst.";
        }
        catch (Exception ex) { _message.Text = ToUserMessage(ex); }
        finally { _busy = false; UpdateActionState(); }
    }

    private static View CreateStatusView() { var item = new Label { LineBreakMode = LineBreakMode.WordWrap }; item.SetBinding(Label.TextProperty, nameof(StatusItem.Text)); return new Border { Stroke = Colors.LightGray, Padding = 8, Margin = new Thickness(0, 0, 0, 5), Content = item }; }
    private static View CreateHistoryView() { var item = new Label { LineBreakMode = LineBreakMode.WordWrap }; item.SetBinding(Label.TextProperty, nameof(HistoryItem.Text)); return new Border { Stroke = Colors.LightGray, Padding = 8, Margin = new Thickness(0, 0, 0, 5), Content = item }; }
    private static string ToUserMessage(MitgliedVereinsunterlagenBereitstellungResult result) => result.DiagnosticCode switch { "DOCUMENT_FILE_MISSING" => "Mindestens eine ausgewählte Fassung hat keine hinterlegte PDF-Datei.", "DIGITAL_SEND_FAILED" => "Der Versand wurde vom Maildienst nicht bestätigt.", "DELIVERY_RECORD_FAILED" => "Die E-Mail wurde versandt, aber der Bereitstellungsnachweis konnte nicht gespeichert werden.", "AUTH_REQUIRED" or "AUTH" => "Für diese Bereitstellung fehlt die Berechtigung.", "VALIDATION" => "Die ausgewählten Dokumentfassungen sind ungültig.", _ => result.Message };
    private static string ToUserMessage(Exception exception) => exception.Message.Contains("E-Mail", StringComparison.OrdinalIgnoreCase) ? "Für dieses Mitglied ist keine E-Mail-Adresse hinterlegt." : exception.Message.Contains("Berechtigung", StringComparison.OrdinalIgnoreCase) ? "Für diese Bereitstellung fehlt die Berechtigung." : exception.Message;

    private sealed class StatusItem
    {
        public StatusItem(MitgliedVereinsdokumentStatusRecord item) => Text = $"{item.Bezeichnung}\nFassung: {item.Fassung ?? "–"} · gültig ab: {item.GueltigAb:dd.MM.yyyy}{(item.GueltigBis.HasValue ? $" · bis: {item.GueltigBis:dd.MM.yyyy}" : string.Empty)}\n{(item.Status == "bereitgestellt" ? "Aktuell bereitgestellt" : item.Status == "fehlt" ? "Aktuelle Fassung fehlt beim Mitglied" : "Keine aktuelle Vereinsfassung hinterlegt")}{(item.BereitgestelltAm.HasValue ? $" · {item.Bereitstellungsart} am {item.BereitgestelltAm:dd.MM.yyyy HH:mm}" : string.Empty)}";
        public string Text { get; }
    }
    private sealed class HistoryItem
    {
        public HistoryItem(MitgliedVereinsdokumentBereitstellungHistorieRecord item) => Text = $"{item.Bezeichnung} · Fassung {item.Fassung}\n{item.Bereitstellungsart} · {item.BereitgestelltAm:dd.MM.yyyy HH:mm}{(item.StorniertAm.HasValue ? $"\nStorniert am {item.StorniertAm:dd.MM.yyyy HH:mm}{(!string.IsNullOrWhiteSpace(item.StornoGrund) ? $" · {item.StornoGrund}" : string.Empty)}" : string.Empty)}";
        public string Text { get; }
    }
}
