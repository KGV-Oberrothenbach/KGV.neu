using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using System.Collections.ObjectModel;

namespace KGV.Maui.Pages;

public sealed class HomeSectionDetailPage : ContentPage
{
    private readonly HomeContextState _homeContextState;
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;

    private readonly Label _sectionLabel;
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly Label _timeLabel;
    private readonly Label _contentLabel;
    private readonly Label _additionalInfoLabel;
    private readonly Label _registrationInfoLabel;
    private readonly Label _statusLabel;
    private readonly Button _registerButton;
    private readonly Button _manageButton;
    private readonly CollectionView _participantsView;
    private readonly Label _participantsEmptyLabel;
    private readonly VerticalStackLayout _participantsSection;
    private readonly ObservableCollection<WorkAssignmentParticipantItem> _participants = new();
    private bool _isBusy;

    public HomeSectionDetailPage(HomeContextState homeContextState, ISupabaseService supabaseService, UserContextState userContextState)
    {
        _homeContextState = homeContextState;
        _supabaseService = supabaseService;
        _userContextState = userContextState;

        Title = "Detail";

        _sectionLabel = new Label { FontSize = 12, TextColor = Colors.Gray };
        _titleLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap };
        _subtitleLabel = new Label { FontSize = 14, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _timeLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _contentLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _additionalInfoLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _registrationInfoLabel = new Label { LineBreakMode = LineBreakMode.WordWrap, TextColor = Colors.DarkSlateBlue };
        _statusLabel = new Label { LineBreakMode = LineBreakMode.WordWrap, TextColor = Colors.DarkRed };

        _registerButton = new Button { Text = "Anmelden", IsVisible = false };
        _registerButton.Clicked += async (_, _) => await RegisterAsync();

        _manageButton = new Button { Text = "Bearbeiten", IsVisible = false };
        _manageButton.Clicked += async (_, _) =>
        {
            var section = _homeContextState.DetailKind switch
            {
                HomeDetailKind.WorkAssignment => "workassignments",
                HomeDetailKind.Appointment => "appointments",
                HomeDetailKind.Announcement => "announcements",
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(section))
                await Shell.Current.GoToAsync($"{nameof(HomeManagementPage)}?section={section}");
        };

        _participantsEmptyLabel = new Label
        {
            Text = "Aktuell keine angemeldeten Teilnehmer.",
            TextColor = Colors.Gray,
            IsVisible = false
        };

        _participantsView = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            HeightRequest = 180,
            ItemsSource = _participants,
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label { FontAttributes = FontAttributes.Bold };
                title.SetBinding(Label.TextProperty, nameof(WorkAssignmentParticipantItem.DisplayName));

                var subtitle = new Label { FontSize = 12, TextColor = Colors.Gray };
                subtitle.SetBinding(Label.TextProperty, nameof(WorkAssignmentParticipantItem.StatusText));

                return new VerticalStackLayout
                {
                    Padding = new Thickness(0, 6),
                    Children = { title, subtitle }
                };
            })
        };

        _participantsSection = new VerticalStackLayout
        {
            Spacing = 8,
            IsVisible = false,
            Children =
            {
                new Label { Text = "Teilnehmer", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                _participantsView,
                _participantsEmptyLabel
            }
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    _sectionLabel,
                    _titleLabel,
                    _subtitleLabel,
                    _timeLabel,
                    _contentLabel,
                    _additionalInfoLabel,
                    _registrationInfoLabel,
                    new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children = { _registerButton, _manageButton }
                    },
                    _participantsSection,
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
        _statusLabel.Text = string.Empty;
        _participants.Clear();
        _participantsSection.IsVisible = false;
        _participantsEmptyLabel.IsVisible = false;
        _registerButton.IsVisible = false;
        _manageButton.IsVisible = false;

        switch (_homeContextState.DetailKind)
        {
            case HomeDetailKind.WorkAssignment when _homeContextState.WorkAssignment != null:
                var workAssignment = _homeContextState.WorkAssignment;
                _sectionLabel.Text = "Arbeitseinsatz";
                _titleLabel.Text = workAssignment.Title;
                _subtitleLabel.Text = workAssignment.Subtitle;
                _timeLabel.Text = workAssignment.TimeText;
                _contentLabel.Text = workAssignment.Details;
                _additionalInfoLabel.Text = workAssignment.DetailInfo;
                _registrationInfoLabel.Text = workAssignment.RegistrationInfo;
                _registerButton.IsVisible = workAssignment.CanRegister;
                await LoadParticipantsAsync(workAssignment.Id);
                break;
            case HomeDetailKind.Appointment when _homeContextState.Appointment != null:
                var appointment = _homeContextState.Appointment;
                _sectionLabel.Text = "Termin";
                _titleLabel.Text = appointment.Title;
                _subtitleLabel.Text = appointment.Subtitle;
                _timeLabel.Text = appointment.TimeText;
                _contentLabel.Text = appointment.Details;
                _additionalInfoLabel.Text = appointment.DetailInfo;
                _registrationInfoLabel.Text = string.Empty;
                break;
            case HomeDetailKind.Announcement when _homeContextState.Announcement != null:
                var announcement = _homeContextState.Announcement;
                _sectionLabel.Text = "Bekanntmachung";
                _titleLabel.Text = announcement.Title;
                _subtitleLabel.Text = announcement.Subtitle;
                _timeLabel.Text = string.Empty;
                _contentLabel.Text = announcement.Content;
                _additionalInfoLabel.Text = announcement.DetailInfo;
                _registrationInfoLabel.Text = string.Empty;
                break;
            default:
                _sectionLabel.Text = string.Empty;
                _titleLabel.Text = "Kein Detail ausgewählt";
                _subtitleLabel.Text = string.Empty;
                _timeLabel.Text = string.Empty;
                _contentLabel.Text = "Bitte zuerst auf der Startseite einen Eintrag auswählen.";
                _additionalInfoLabel.Text = string.Empty;
                _registrationInfoLabel.Text = string.Empty;
                return;
        }

        _manageButton.IsVisible = _userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand;
    }

    private async Task LoadParticipantsAsync(int arbeitseinsatzId)
    {
        if (_userContextState.CurrentUserContext?.Role is not (UserRole.Admin or UserRole.Vorstand))
            return;

        var participants = await _supabaseService.GetArbeitseinsatzParticipantsAsync(arbeitseinsatzId);
        foreach (var participant in participants)
            _participants.Add(participant);

        _participantsSection.IsVisible = true;
        _participantsEmptyLabel.IsVisible = _participants.Count == 0;
    }

    private async Task RegisterAsync()
    {
        if (_isBusy || _homeContextState.WorkAssignment == null)
            return;

        if (_userContextState.CurrentMitgliedId is not > 0 or > int.MaxValue)
        {
            _statusLabel.Text = "Der aktuelle Benutzer ist keinem Mitglied zugeordnet.";
            return;
        }

        _isBusy = true;
        try
        {
            var result = await _supabaseService.SignUpForArbeitseinsatzAsync(_homeContextState.WorkAssignment.Id, (int)_userContextState.CurrentMitgliedId.Value);
            _statusLabel.Text = result.Message;
            if (result.UpdatedItem != null)
            {
                _homeContextState.SetWorkAssignment(result.UpdatedItem);
                _registrationInfoLabel.Text = result.UpdatedItem.RegistrationInfo;
                _registerButton.IsVisible = result.UpdatedItem.CanRegister;
            }
        }
        finally
        {
            _isBusy = false;
        }
    }
}
