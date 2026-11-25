using System.ComponentModel;
using System.Runtime.CompilerServices;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui;

// One identity per authenticated Shell; never derive it from the selected member.
internal sealed class SessionIdentityHeader : INotifyPropertyChanged
{
    private static readonly ConditionalWeakTable<Shell, SessionIdentityHeader> Headers = new();
    private readonly UserContextState _state;
    private readonly ISupabaseService _supabase;
    private readonly IAuthService _auth;
    private readonly Guid? _userId;
    private string _userText;

    private SessionIdentityHeader(UserContextState state, ISupabaseService supabase, IAuthService auth, Vereinskontext? club)
    {
        _state = state;
        _supabase = supabase;
        _auth = auth;
        _userId = state.CurrentUserId;
        _userText = "Benutzer wird geladen …";
        ClubText = Uri.TryCreate(club?.SupabaseUrl, UriKind.Absolute, out var uri)
            ? uri.Host.ToLowerInvariant() switch
            {
                "ymxhszxfutjnljiuciyj.supabase.co" => "Demo DB",
                "itjcabiibuodkxayhvjq.supabase.co" => "KGV Oberrothenbach",
                _ => club?.Kurzname ?? club?.Vereinsname ?? "Verein unbekannt"
            }
            : "Verein unbekannt";
    }

    public string UserText => _userText;
    public string ClubText { get; }
    public event PropertyChangedEventHandler? PropertyChanged;

    public static void Attach(Shell shell, UserContextState state, ISupabaseService supabase, IAuthService auth, Vereinskontext? club)
    {
        var header = new SessionIdentityHeader(state, supabase, auth, club);
        Headers.Add(shell, header);
        // UserShell uses the standard flyout. AdminShell adds the same header to its custom content.
        if (shell is not AdminShell)
            shell.FlyoutHeader = header.CreateView(null, false);
        shell.Navigated += (_, _) => header.UpdateTitle(shell);
        shell.Loaded += async (_, _) =>
        {
            header.UpdateTitle(shell);
            await header.LoadNameAsync();
        };
        header.UpdateTitle(shell);
    }

    public static View CreateFlyoutHeader(Shell shell)
        => Headers.TryGetValue(shell, out var header) ? header.CreateView(null, false) : new Label();

    private async Task LoadNameAsync()
    {
        if (_userId is not Guid id || _state.CurrentUserId != id)
            return;
        try
        {
            var client = await _auth.GetClientAsync();
            var current = client.Auth.CurrentUser;
            if (_state.CurrentUserId == id && Guid.TryParse(current?.Id, out var authId) && authId == id
                && !string.IsNullOrWhiteSpace(current?.Email))
                SetUserText(current.Email);
        }
        catch { /* No fallback to a previous login or selected member. */ }
        try
        {
            var member = await _supabase.GetMitgliedByAuthUserIdAsync(id);
            if (_state.CurrentUserId != id || member == null || member.Id != _state.CurrentMitgliedId)
                return;
            var name = $"{member.Vorname} {member.Name}".Trim();
            if (!string.IsNullOrWhiteSpace(name))
                SetUserText(name);
            else if (!string.IsNullOrWhiteSpace(member.Email))
                SetUserText(member.Email);
        }
        catch
        {
            // Keep the verified account email if the member lookup fails.
        }
        finally
        {
            if (_state.CurrentUserId == id && _userText == "Benutzer wird geladen …")
                SetUserText("Benutzer nicht verfügbar");
        }
    }

    private void SetUserText(string value)
    {
        _userText = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserText)));
    }

    private void UpdateTitle(Shell shell)
    {
        if (shell.CurrentPage is not Page page || _state.CurrentUserId != _userId)
            return;
        if (Shell.GetTitleView(page) is View current && current.BindingContext == this)
            return;
        // Keep an existing custom toolbar rather than silently removing its actions.
        if (Shell.GetTitleView(page) != null)
            return;
        Shell.SetTitleView(page, CreateView(page, true));
    }

    private VerticalStackLayout CreateView(Page? page, bool toolbar)
    {
        var layout = new VerticalStackLayout
        {
            BindingContext = this,
            Spacing = 0,
            Padding = toolbar ? new Microsoft.Maui.Thickness(0) : new Microsoft.Maui.Thickness(8, 4, 8, 12),
            VerticalOptions = LayoutOptions.Center
        };
        var user = new Label { FontSize = toolbar ? 12 : 14, LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 1 };
        user.SetBinding(Label.TextProperty, nameof(UserText));
        var club = new Label { FontSize = toolbar ? 15 : 18, FontAttributes = FontAttributes.Bold, LineBreakMode = toolbar ? LineBreakMode.TailTruncation : LineBreakMode.WordWrap, MaxLines = toolbar ? 1 : 3 };
        club.SetBinding(Label.TextProperty, nameof(ClubText));
        if (toolbar && page != null)
        {
            var color = Shell.GetTitleColor(page) ?? Shell.GetForegroundColor(page);
            if (color != null)
            {
                user.TextColor = color;
                club.TextColor = color;
            }
        }
        layout.Children.Add(club);
        layout.Children.Add(user);
        return layout;
    }
}
