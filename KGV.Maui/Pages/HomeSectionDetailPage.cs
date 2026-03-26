using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using KGV.Maui.ViewModels;
using System.Collections.ObjectModel;

namespace KGV.Maui.Pages;

public sealed class HomeSectionDetailPage : ContentPage
{
    private readonly HomeContextState _homeContextState;
    private readonly ArbeitseinsaetzeUserState _arbeitseinsaetzeUserState;
    private readonly TermineUserState _termineUserState;
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private readonly KGV.Maui.ViewModels.HomeViewModel _homeViewModel;

    private readonly Label _sectionLabel;
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly Label _timeLabel;
    private readonly Label _contentLabel;
    private readonly Label _additionalInfoLabel;
    private readonly Label _registrationInfoLabel;
    private readonly Label _statusLabel;
    private readonly Button _registerButton;
    private readonly Button _signOffButton;
    private readonly Button _newButton;
    private readonly Button _editButton;
    private readonly Button _deleteButton;
    private readonly Button _backButton;
    private readonly Button _previousButton;
    private readonly Button _nextButton;
    private readonly Label _positionLabel;
    private readonly CollectionView _participantsView;
    private readonly Label _participantsEmptyLabel;
    private readonly VerticalStackLayout _participantsSection;
    private readonly ObservableCollection<WorkAssignmentParticipantItem> _participants = new();
    private bool _isBusy;
    private bool _loadScheduled;

    public HomeSectionDetailPage(HomeContextState homeContextState, ArbeitseinsaetzeUserState arbeitseinsaetzeUserState, TermineUserState termineUserState, ISupabaseService supabaseService, UserContextState userContextState, KGV.Maui.ViewModels.HomeViewModel homeViewModel)
    {
        _homeContextState = homeContextState;
        _arbeitseinsaetzeUserState = arbeitseinsaetzeUserState;
        _termineUserState = termineUserState;
        _supabaseService = supabaseService;
        _userContextState = userContextState;
        _homeViewModel = homeViewModel;

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

        _signOffButton = new Button { Text = "Abmelden", IsVisible = false };
        _signOffButton.Clicked += async (_, _) => await SignOffAsync();

        _backButton = new Button { Text = "Zur Startseite" };
        _backButton.Clicked += async (_, _) => await Shell.Current.GoToAsync("//home");

        _newButton = new Button { Text = "Neu", IsVisible = false };
        _newButton.Clicked += async (_, _) => await OpenEditorAsync(isNew: true);

        _editButton = new Button { Text = "Bearbeiten", IsVisible = false };
        _editButton.Clicked += async (_, _) => await OpenEditorAsync(isNew: false);

        _deleteButton = new Button { Text = "Löschen", IsVisible = false };
        _deleteButton.Clicked += async (_, _) => await DeleteAsync();

        _previousButton = new Button { Text = "←", WidthRequest = 56, IsVisible = false };
        _previousButton.Clicked += async (_, _) => await MovePreviousAsync();

        _nextButton = new Button { Text = "→", WidthRequest = 56, IsVisible = false };
        _nextButton.Clicked += async (_, _) => await MoveNextAsync();

        _positionLabel = new Label
        {
            HorizontalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalTextAlignment = TextAlignment.Center,
            IsVisible = false
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
                    new HorizontalStackLayout
                    {
                        HorizontalOptions = LayoutOptions.Start,
                        Children = { _backButton }
                    },
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
                        Children = { _registerButton, _signOffButton, _newButton, _editButton, _deleteButton }
                    },
                    _participantsSection,
                    CreateWorkAssignmentNavigationFooter(),
                    _statusLabel
                }
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_isBusy || _loadScheduled)
            return;

        _loadScheduled = true;
        Dispatcher.Dispatch(async () =>
        {
            await Task.Yield();
            _loadScheduled = false;
            await LoadAsync();
        });
    }

    private async Task LoadAsync()
    {
        if (_isBusy)
            return;

        _statusLabel.Text = string.Empty;
        _participants.Clear();
        _participantsSection.IsVisible = false;
        _participantsEmptyLabel.IsVisible = false;
        _registerButton.IsVisible = false;
        _signOffButton.IsVisible = false;
        _newButton.IsVisible = false;
        _editButton.IsVisible = false;
        _deleteButton.IsVisible = false;
        _previousButton.IsVisible = false;
        _nextButton.IsVisible = false;
        _positionLabel.IsVisible = false;
        SetBusyState(true, "Daten werden geladen.");

        try
        {
            switch (_homeContextState.DetailKind)
            {
                case HomeDetailKind.WorkAssignment when _homeContextState.WorkAssignment != null:
                    var workAssignment = _arbeitseinsaetzeUserState.CurrentEntry ?? _homeContextState.WorkAssignment;
                    _homeContextState.SetWorkAssignment(workAssignment);
                    _sectionLabel.Text = "Arbeitseinsatz";
                    _titleLabel.Text = workAssignment.Title;
                    _subtitleLabel.Text = workAssignment.Subtitle;
                    _timeLabel.Text = workAssignment.TimeText;
                    _contentLabel.Text = workAssignment.Details;
                    _additionalInfoLabel.Text = workAssignment.DetailInfo;
                    _registrationInfoLabel.Text = workAssignment.RegistrationInfo;
                    _registerButton.IsVisible = workAssignment.CanRegister;
                    _signOffButton.IsVisible = workAssignment.CanSignOff;
                    UpdateWorkAssignmentNavigation();
                    await LoadParticipantsAsync(workAssignment.Id);
                    break;
                case HomeDetailKind.Appointment when _homeContextState.Appointment != null:
                    var appointment = _termineUserState.CurrentEntry ?? _homeContextState.Appointment;
                    _homeContextState.SetAppointment(appointment);
                    _sectionLabel.Text = "Termin";
                    _titleLabel.Text = appointment.Title;
                    _subtitleLabel.Text = appointment.Subtitle;
                    _timeLabel.Text = appointment.TimeText;
                    _contentLabel.Text = appointment.Details;
                    _additionalInfoLabel.Text = appointment.DetailInfo;
                    _registrationInfoLabel.Text = string.Empty;
                    UpdateAppointmentNavigation();
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

            var canManage = _userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand;
            _newButton.IsVisible = canManage;
            _editButton.IsVisible = canManage && TryGetCurrentEntryId() > 0;
            _deleteButton.IsVisible = _editButton.IsVisible;
            _statusLabel.Text = string.Empty;
        }
        finally
        {
            SetBusyState(false);
        }
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

        SetBusyState(true, "Daten werden gespeichert.");
        try
        {
            _statusLabel.Text = "Daten werden gespeichert.";
            var result = await _supabaseService.SignUpForArbeitseinsatzAsync(_homeContextState.WorkAssignment.Id, (int)_userContextState.CurrentMitgliedId.Value);
            _statusLabel.Text = result.Message;
            if (result.UpdatedItem != null)
            {
                _arbeitseinsaetzeUserState.ReplaceCurrent(result.UpdatedItem);
                _homeContextState.SetWorkAssignment(result.UpdatedItem);
                _registrationInfoLabel.Text = result.UpdatedItem.RegistrationInfo;
                _registerButton.IsVisible = result.UpdatedItem.CanRegister;
                _signOffButton.IsVisible = result.UpdatedItem.CanSignOff;
            }
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private async Task SignOffAsync()
    {
        if (_isBusy || _homeContextState.WorkAssignment == null)
            return;

        if (_userContextState.CurrentMitgliedId is not > 0 or > int.MaxValue)
        {
            _statusLabel.Text = "Der aktuelle Benutzer ist keinem Mitglied zugeordnet.";
            return;
        }

        SetBusyState(true, "Daten werden gespeichert.");
        try
        {
            _statusLabel.Text = "Daten werden gespeichert.";
            var result = await _supabaseService.SignOffFromArbeitseinsatzAsync(_homeContextState.WorkAssignment.Id, (int)_userContextState.CurrentMitgliedId.Value);
            _statusLabel.Text = result.Message;
            if (result.UpdatedItem != null)
            {
                _arbeitseinsaetzeUserState.ReplaceCurrent(result.UpdatedItem);
                _homeContextState.SetWorkAssignment(result.UpdatedItem);
                _registrationInfoLabel.Text = result.UpdatedItem.RegistrationInfo;
                _registerButton.IsVisible = result.UpdatedItem.CanRegister;
                _signOffButton.IsVisible = result.UpdatedItem.CanSignOff;
            }
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private void UpdateWorkAssignmentNavigation()
    {
        var hasNavigation = _arbeitseinsaetzeUserState.TotalCount > 0;
        _previousButton.IsVisible = hasNavigation;
        _nextButton.IsVisible = hasNavigation;
        _positionLabel.IsVisible = hasNavigation;
        _previousButton.IsEnabled = _arbeitseinsaetzeUserState.CanMovePrevious;
        _nextButton.IsEnabled = _arbeitseinsaetzeUserState.CanMoveNext;
        _positionLabel.Text = hasNavigation
            ? $"{_arbeitseinsaetzeUserState.CurrentIndex + 1}/{_arbeitseinsaetzeUserState.TotalCount}"
            : string.Empty;
    }

    private void UpdateAppointmentNavigation()
    {
        var hasNavigation = _termineUserState.TotalCount > 0;
        _previousButton.IsVisible = hasNavigation;
        _nextButton.IsVisible = hasNavigation;
        _positionLabel.IsVisible = hasNavigation;
        _previousButton.IsEnabled = _termineUserState.CanMovePrevious;
        _nextButton.IsEnabled = _termineUserState.CanMoveNext;
        _positionLabel.Text = hasNavigation
            ? $"{_termineUserState.CurrentIndex + 1}/{_termineUserState.TotalCount}"
            : string.Empty;
    }

    private Task MovePreviousAsync()
    {
        switch (_homeContextState.DetailKind)
        {
            case HomeDetailKind.WorkAssignment:
                if (!_arbeitseinsaetzeUserState.MovePrevious() || _arbeitseinsaetzeUserState.CurrentEntry == null)
                    return Task.CompletedTask;

                _homeContextState.SetWorkAssignment(_arbeitseinsaetzeUserState.CurrentEntry);
                return LoadAsync();
            case HomeDetailKind.Appointment:
                if (!_termineUserState.MovePrevious() || _termineUserState.CurrentEntry == null)
                    return Task.CompletedTask;

                _homeContextState.SetAppointment(_termineUserState.CurrentEntry);
                return LoadAsync();
            default:
                return Task.CompletedTask;
        }
    }

    private Task MoveNextAsync()
    {
        switch (_homeContextState.DetailKind)
        {
            case HomeDetailKind.WorkAssignment:
                if (!_arbeitseinsaetzeUserState.MoveNext() || _arbeitseinsaetzeUserState.CurrentEntry == null)
                    return Task.CompletedTask;

                _homeContextState.SetWorkAssignment(_arbeitseinsaetzeUserState.CurrentEntry);
                return LoadAsync();
            case HomeDetailKind.Appointment:
                if (!_termineUserState.MoveNext() || _termineUserState.CurrentEntry == null)
                    return Task.CompletedTask;

                _homeContextState.SetAppointment(_termineUserState.CurrentEntry);
                return LoadAsync();
            default:
                return Task.CompletedTask;
        }
    }

    private Grid CreateWorkAssignmentNavigationFooter()
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        grid.Add(_previousButton);
        Grid.SetColumn(_previousButton, 0);

        grid.Add(_positionLabel);
        Grid.SetColumn(_positionLabel, 1);

        grid.Add(_nextButton);
        Grid.SetColumn(_nextButton, 2);

        return grid;
    }

    private void SetBusyState(bool isBusy, string? message = null)
    {
        _isBusy = isBusy;
        _registerButton.IsEnabled = !isBusy;
        _signOffButton.IsEnabled = !isBusy;
        _newButton.IsEnabled = !isBusy;
        _editButton.IsEnabled = !isBusy;
        _deleteButton.IsEnabled = !isBusy;
        _backButton.IsEnabled = !isBusy;
        _previousButton.IsEnabled = !isBusy && _previousButton.IsVisible && _arbeitseinsaetzeUserState.CanMovePrevious;
        _nextButton.IsEnabled = !isBusy && _nextButton.IsVisible && (_homeContextState.DetailKind == HomeDetailKind.WorkAssignment ? _arbeitseinsaetzeUserState.CanMoveNext : _termineUserState.CanMoveNext);
        if (!string.IsNullOrWhiteSpace(message))
            _statusLabel.Text = message;
    }

    private int TryGetCurrentEntryId()
    {
        return _homeContextState.DetailKind switch
        {
            HomeDetailKind.WorkAssignment => _homeContextState.WorkAssignment?.Id ?? 0,
            HomeDetailKind.Appointment => _homeContextState.Appointment?.Id ?? 0,
            HomeDetailKind.Announcement => _homeContextState.Announcement?.Id ?? 0,
            _ => 0
        };
    }

    private Task OpenEditorAsync(bool isNew)
    {
        return _homeContextState.DetailKind switch
        {
            HomeDetailKind.WorkAssignment when isNew => Shell.Current.GoToAsync(nameof(ArbeitseinsaetzeEditorPage)),
            HomeDetailKind.WorkAssignment => Shell.Current.GoToAsync($"{nameof(ArbeitseinsaetzeEditorPage)}?entryId={TryGetCurrentEntryId()}"),
            HomeDetailKind.Appointment when isNew => Shell.Current.GoToAsync(nameof(TermineEditorPage)),
            HomeDetailKind.Appointment => Shell.Current.GoToAsync($"{nameof(TermineEditorPage)}?entryId={TryGetCurrentEntryId()}"),
            HomeDetailKind.Announcement when isNew => Shell.Current.GoToAsync(nameof(BekanntmachungEditorPage)),
            HomeDetailKind.Announcement => Shell.Current.GoToAsync($"{nameof(BekanntmachungEditorPage)}?entryId={TryGetCurrentEntryId()}"),
            _ => Shell.Current.GoToAsync("//home")
        };
    }

    private async Task DeleteAsync()
    {
        var entryId = TryGetCurrentEntryId();
        if (entryId <= 0 || _isBusy)
            return;

        var entityName = _homeContextState.DetailKind switch
        {
            HomeDetailKind.WorkAssignment => "Arbeitseinsatz",
            HomeDetailKind.Appointment => "Termin",
            HomeDetailKind.Announcement => "Bekanntmachung",
            _ => "Datensatz"
        };

        var confirmed = await DisplayAlert("Löschen bestätigen", $"{entityName} wirklich löschen?", "Löschen", "Abbrechen");
        if (!confirmed)
            return;

        SetBusyState(true, "Datensatz wird gelöscht.");
        try
        {
            var success = _homeContextState.DetailKind switch
            {
                HomeDetailKind.WorkAssignment => await _supabaseService.DeleteArbeitseinsatzAsync(entryId),
                HomeDetailKind.Appointment => await _supabaseService.DeleteTerminAsync(entryId),
                HomeDetailKind.Announcement => await _supabaseService.DeleteBekanntmachungAsync(entryId),
                _ => false
            };

            if (!success)
            {
                _statusLabel.Text = $"{entityName} konnte nicht gelöscht werden.";
                return;
            }

            _arbeitseinsaetzeUserState.Clear();
            _termineUserState.Clear();
            _homeContextState.Clear();
            await _homeViewModel.ReloadAsync();
            await Shell.Current.GoToAsync("//home");
        }
        finally
        {
            SetBusyState(false);
        }
    }
}

