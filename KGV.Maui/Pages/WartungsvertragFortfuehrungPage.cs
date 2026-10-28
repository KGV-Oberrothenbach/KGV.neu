using KGV.Core.Interfaces;
using KGV.Core.Models;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

/// <summary>Server-driven continuation UI. It never writes an assignment directly.</summary>
public sealed class WartungsvertragFortfuehrungPage : ContentPage, IQueryAttributable
{
    private readonly ISupabaseService _service;
    private readonly VerticalStackLayout _content = new() { Padding = 24, Spacing = 10 };
    private readonly Label _status = new() { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
    private long _assignmentId;
    private WartungsvertragFortfuehrungDetail? _detail;
    private int? _contractNumber;
    private bool _busy;

    public WartungsvertragFortfuehrungPage(ISupabaseService service)
    {
        _service = service;
        Title = "Vertragsfortführung";
        Content = new ScrollView { Content = _content };
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("zuordnungId", out var raw) && long.TryParse(Uri.UnescapeDataString(raw?.ToString() ?? ""), out var id))
        { _assignmentId = id; _ = LoadAsync(); }
    }

    protected override async void OnAppearing() { base.OnAppearing(); await LoadAsync(); }

    private async Task LoadAsync()
    {
        if (_busy || _assignmentId <= 0) return;
        _busy = true;
        try { _detail = await _service.GetWartungsvertragFortfuehrungDetailAsync(_assignmentId); _contractNumber = _detail is null ? null : await _service.GetWartungsvertragFachnummerAsync(_detail.WartungsvertragId); Render(); }
        catch (Exception ex) { _status.Text = FriendlyError(ex); Render(); }
        finally { _busy = false; }
    }

    private void Render()
    {
        _content.Children.Clear();
        _content.Children.Add(new Label { Text = "Wartungsvertrag fortführen", FontSize = 24, FontAttributes = FontAttributes.Bold });
        if (_detail == null) { _content.Children.Add(new Label { Text = "Die Folgeentscheidung konnte nicht geladen werden." }); return; }
        var d = _detail;
        if (_contractNumber is { } number) _content.Children.Add(Text($"Wartungsvertrag Nr. {number}"));
        _content.Children.Add(Text($"Bisherige Laufzeit: {d.GueltigAb:dd.MM.yyyy} – {(d.GueltigBis.HasValue ? d.GueltigBis.Value.ToString("dd.MM.yyyy") : "offen")}"));
        _content.Children.Add(Text(DecisionText(d)));
        if (d.FolgezuordnungId is > 0) _content.Children.Add(Text($"Folgezuordnung: {d.FolgeWartungsvertragTitel ?? "Wartungsvertrag"} · {d.FolgeGueltigAb:dd.MM.yyyy} – {d.FolgeGueltigBis:dd.MM.yyyy}"));
        if (!string.IsNullOrWhiteSpace(d.WorkflowStatus)) _content.Children.Add(Text($"Verlängerungsmitteilung: {WorkflowText(d.WorkflowStatus)}"));
        if (d.ZurueckgenommenAm.HasValue) _content.Children.Add(Text($"Zurückgenommen am {d.ZurueckgenommenAm.Value:dd.MM.yyyy}{(string.IsNullOrWhiteSpace(d.RuecknahmeBegruendung) ? "" : $" · {d.RuecknahmeBegruendung}")}"));
        if (!string.IsNullOrWhiteSpace(d.Begruendung)) _content.Children.Add(Text($"Begründung: {d.Begruendung}"));
        _content.Children.Add(_status);
        if (d.GueltigBis is null || d.GueltigBis >= DateTime.Today)
            _content.Children.Add(Button("Neuen Wartungsvertrag wegen grundlegender Änderung erstellen", StartMaterialChangeAsync));
        if (d.CanDecide) { _content.Children.Add(Button("Verlängern", ExtendAsync)); _content.Children.Add(Button("Nicht verlängern", NonExtendAsync)); }
        if (d.CanWithdrawNichtverlaengerung) _content.Children.Add(Button("Nichtverlängerung zurücknehmen", WithdrawAsync));
        if (d.Entscheidung == "verlaengern" && d.DokumentId is not > 0 && d.KommunikationsvorgangId is > 0) _content.Children.Add(Button("Dokumenterzeugung erneut versuchen", RetryDocumentAsync));
        if (d.DokumentId is > 0) _content.Children.Add(Button("Verlängerungsmitteilung öffnen", OpenDocumentAsync));
        _content.Children.Add(Button("Aktualisieren", LoadAsync));
    }

    private async Task ExtendAsync()
    {
        if (_detail?.GueltigBis is not { } end) return;
        var newEnd = await DisplayPromptAsync("Verlängern", $"Der bestehende Wartungsvertrag wird unverändert fortgeführt.\nNeuer Beginn (serverseitig bestimmt): {end.AddDays(1):dd.MM.yyyy}\nNeues Vertragsende (TT.MM.JJJJ)", initialValue: "");
        var board = await DisplayPromptAsync("Verlängern", "Vorstandsbeschlussdatum (TT.MM.JJJJ)", initialValue: "");
        if (!DateTime.TryParse(newEnd, out var validUntil) || !DateTime.TryParse(board, out var boardDate)) { _status.Text = "Neues Vertragsende und Vorstandsbeschlussdatum sind erforderlich."; Render(); return; }
        var note = await DisplayPromptAsync("Verlängern", "Bemerkung (optional)", initialValue: "");
        await RunAsync(async () =>
        {
            var result = await _service.VerlaengereWartungsvertragAsync(_assignmentId, validUntil, boardDate, note);
            // The continuation RPC is idempotent. Only its confirmed communication
            // process may be finalized; never derive it from the stale predecessor detail.
            await _service.DokumenterzeugungWartungsvertragVerlaengerungWiederholenAsync(result.VertragsparteiMitgliedId, result.KommunikationsvorgangId);
        });
    }

    private async Task StartMaterialChangeAsync()
    {
        if (_detail is not { } detail) return;
        var old = await _service.GetWartungsvertragByIdAsync(detail.WartungsvertragId);
        if (old is null) { _status.Text = "Die Ausgangsdefinition des Wartungsvertrags wurde nicht gefunden."; Render(); return; }
        var title = await DisplayPromptAsync("Grundlegende Änderung", "Titel des neuen Wartungsvertrags", initialValue: old.Titel ?? "");
        if (title is null) return;
        var description = await DisplayPromptAsync("Grundlegende Änderung", "Beschreibung (optional)", initialValue: old.Beschreibung ?? "");
        if (description is null) return;
        var area = await DisplayPromptAsync("Grundlegende Änderung", "Bereich (optional)", initialValue: old.Bereich ?? "");
        if (area is null) return;
        var maximumText = await DisplayPromptAsync("Grundlegende Änderung", "Maximale Zuordnungen", initialValue: old.MaxAktiveZuordnungen.ToString());
        if (!int.TryParse(maximumText, out var maximum) || maximum < 1) { _status.Text = "Das Kontingent muss mindestens 1 betragen."; Render(); return; }
        var hourChoice = await DisplayActionSheetAsync("Arbeitsstundenwirkung", "Abbrechen", null, "Vollbefreiung von Pflichtstunden", "Gutschrift oder keine Befreiung");
        if (hourChoice is null or "Abbrechen") return;
        var exempt = hourChoice == "Vollbefreiung von Pflichtstunden";
        var creditText = exempt ? "0" : await DisplayPromptAsync("Arbeitsstundenwirkung", "Gutschrift pro Saison (0 für keine)", initialValue: old.ArbeitsstundenGutschrift.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (creditText is null || !decimal.TryParse(creditText, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var credit) || credit < 0)
        { _status.Text = "Die Gutschrift muss eine nichtnegative Zahl sein."; Render(); return; }
        var minimumStart = new[] { DateTime.Today.AddDays(1), detail.GueltigAb.Date.AddDays(1) }.Max();
        var startText = await DisplayPromptAsync("Grundlegende Änderung", "Neuer Vertragsbeginn (TT.MM.JJJJ)", initialValue: minimumStart.ToString("dd.MM.yyyy"));
        if (!DateTime.TryParse(startText, out var start) || start.Date < minimumStart)
        { _status.Text = "Der neue Vertragsbeginn muss nach dem bisherigen Beginn und frühestens morgen liegen."; Render(); return; }
        var note = await DisplayPromptAsync("Grundlegende Änderung", "Bemerkung (optional)", initialValue: old.Bemerkung ?? "");
        if (note is null) return;

        await RunAsync(async () =>
        {
            var result = await _service.StarteWartungsvertragGrundlegendeAenderungAsync(new WartungsvertragGrundlegendeAenderungRequest(
                detail.VorgaengerZuordnungId, title, description, area, maximum, exempt, credit, start.Date, note));
            await DisplayAlertAsync("Grundlegende Änderung", $"Prepared-Vertrag erstellt (Vorgang {result.KommunikationsvorgangId}). Die 90xxx wird erst beim Signaturstart reserviert. Zum Unterschreiben den Vorgang unter „Vorgänge“ öffnen.", "OK");
        });
    }

    private async Task NonExtendAsync()
    {
        var initiator = await DisplayActionSheetAsync("Initiator", "Abbrechen", null, "Verein", "Mitglied", "Beidseitig");
        if (initiator is null or "Abbrechen") return;
        var decision = await DisplayPromptAsync("Nicht verlängern", initiator == "Mitglied" ? "Eingangs-/Entscheidungsdatum (TT.MM.JJJJ)" : "Entscheidungsdatum (TT.MM.JJJJ)", initialValue: DateTime.Today.ToString("dd.MM.yyyy"));
        if (!DateTime.TryParse(decision, out var decisionDate)) { _status.Text = "Ein Entscheidungsdatum ist erforderlich."; Render(); return; }
        DateTime? board = null;
        if (initiator != "Mitglied") { var text = await DisplayPromptAsync("Nicht verlängern", "Vorstandsbeschlussdatum (TT.MM.JJJJ)"); if (!DateTime.TryParse(text, out var value)) { _status.Text = "Das Vorstandsbeschlussdatum ist erforderlich."; Render(); return; } board = value; }
        var reason = await DisplayPromptAsync("Nicht verlängern", "Begründung (optional)");
        await RunAsync(() => _service.NichtverlaengerungEntscheidenAsync(_assignmentId, initiator.ToLowerInvariant() switch { "verein" => "verein", "mitglied" => "mitglied", _ => "beidseitig" }, decisionDate, board, reason));
    }

    private async Task WithdrawAsync()
    {
        if (_detail?.FolgeentscheidungId is not > 0 || !await DisplayAlertAsync("Rücknahme", "Nichtverlängerung wirklich zurücknehmen?", "Zurücknehmen", "Abbrechen")) return;
        var reason = await DisplayPromptAsync("Rücknahme", "Begründung (optional)");
        await RunAsync(() => _service.NichtverlaengerungZuruecknehmenAsync(_detail.FolgeentscheidungId.Value, reason));
    }

    private Task RetryDocumentAsync() => RunAsync(RetryDocumentIfPossibleAsync);
    private async Task RetryDocumentIfPossibleAsync() { if (_detail?.KommunikationsvorgangId is > 0) await _service.DokumenterzeugungWartungsvertragVerlaengerungWiederholenAsync(_detail.VertragsparteiMitgliedId, _detail.KommunikationsvorgangId.Value); }
    private async Task OpenDocumentAsync() { if (_detail?.DokumentId is not > 0) return; var doc = await _service.GetDokumentByIdAsync(_detail.DokumentId.Value); if (doc == null) throw new InvalidOperationException("Das finale Dokument konnte nicht geladen werden."); var content = await _service.DownloadDokumentContentAsync(doc); if (content is not { Length: > 0 }) throw new InvalidOperationException("Das finale Dokument konnte nicht geladen werden."); await Navigation.PushAsync(new PdfViewerPage(doc.Title, content)); }
    private async Task RunAsync(Func<Task> action) { if (_busy) return; _busy = true; try { _status.Text = "Wird gespeichert."; await action(); _status.Text = "Gespeichert."; } catch (Exception ex) { _status.Text = FriendlyError(ex); } finally { _busy = false; await LoadAsync(); } }
    private static Label Text(string value) => new() { Text = value, LineBreakMode = LineBreakMode.WordWrap };
    private static Button Button(string text, Func<Task> action) { var button = new Button { Text = text, HorizontalOptions = LayoutOptions.Start }; button.Clicked += async (_, _) => await action(); return button; }
    private static string DecisionText(WartungsvertragFortfuehrungDetail d) => d.Entscheidung switch { "verlaengern" => "Verlängert.", "nicht_verlaengern" when d.ZurueckgenommenAm.HasValue => "Nichtverlängerung zurückgenommen.", "nicht_verlaengern" => $"Nichtverlängerung beschlossen. Vertrag läuft bis {d.GueltigBis:dd.MM.yyyy}.", _ => "Noch keine Folgeentscheidung." };
    private static string WorkflowText(string status) => status switch { "dokument_ausstehend" => "Verlängerungsmitteilung wird erstellt", "abgeschlossen" => "Verlängerungsmitteilung erstellt", "fehlgeschlagen" => "Dokumenterzeugung fehlgeschlagen", _ => status };
    private static string FriendlyError(Exception ex) { var m = ex.Message; if (m.Contains("42501") || m.Contains("Berechtigung", StringComparison.OrdinalIgnoreCase)) return "Keine Berechtigung für diese Wartungsvertragsentscheidung."; if (m.Contains("bereits", StringComparison.OrdinalIgnoreCase)) return "Für diese Laufzeit liegt bereits eine wirksame Entscheidung vor."; if (m.Contains("Kapazit", StringComparison.OrdinalIgnoreCase) || m.Contains("Überschneid", StringComparison.OrdinalIgnoreCase)) return "Die Verlängerung ist wegen Kapazität oder Überschneidung nicht zulässig."; return "Die Aktion konnte nicht durchgeführt werden. " + m; }
}
