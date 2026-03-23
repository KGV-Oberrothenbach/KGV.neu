using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Helpers;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace KGV.ViewModels
{
    public sealed class HomeSectionDetailViewModel : BaseViewModel, INavigationAware
    {
        private readonly MainWindowViewModel _mainVm;
        private readonly HomeSectionDetailContext _context;
        private string _registrationInfo;
        private bool _showRegisterButton;

        public string SectionTitle => _context.SectionTitle;
        public string Title => _context.Title;
        public string Subtitle => _context.Subtitle;
        public string StartTimeText => _context.StartTimeText;
        public string EndTimeText => _context.EndTimeText;
        public string Content => _context.Content;
        public string AdditionalInfo => _context.AdditionalInfo;
        public string RegistrationInfo
        {
            get => _registrationInfo;
            private set => SetProperty(ref _registrationInfo, value);
        }
        public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);
        public bool HasStartTimeText => !string.IsNullOrWhiteSpace(StartTimeText);
        public bool HasEndTimeText => !string.IsNullOrWhiteSpace(EndTimeText);
        public bool HasContent => !string.IsNullOrWhiteSpace(Content);
        public bool HasAdditionalInfo => !string.IsNullOrWhiteSpace(AdditionalInfo);
        public bool HasRegistrationInfo => !string.IsNullOrWhiteSpace(RegistrationInfo);
        public bool ShowRegisterButton
        {
            get => _showRegisterButton;
            private set => SetProperty(ref _showRegisterButton, value);
        }

        public RelayCommand<object?> ZurueckCommand { get; }
        public RelayCommand<object?> AnmeldenCommand { get; }

        public HomeSectionDetailViewModel(MainWindowViewModel mainVm, HomeSectionDetailContext context)
        {
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _registrationInfo = context.RegistrationInfo;
            _showRegisterButton = context.ShowRegisterButton;
            ZurueckCommand = new RelayCommand<object?>(_ => _ = ZurueckAsync());
            AnmeldenCommand = new RelayCommand<object?>(_ => _ = RegisterAsync(), _ => ShowRegisterButton);
        }

        public Task OnNavigatedToAsync() => Task.CompletedTask;

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task ZurueckAsync()
        {
            var created = _mainVm.NavigateToHomeViewModel();
            if (created != null)
                await _mainVm.NavigateToAsync(created);
        }

        private async Task RegisterAsync()
        {
            if (_context.WorkAssignmentId <= 0)
                return;

            var mitgliedId = await ResolveCurrentMemberIdAsync();
            if (!mitgliedId.HasValue)
            {
                MessageBox.Show(
                    "Der aktuelle Benutzer ist keinem Mitglied zugeordnet.",
                    "Anmeldung",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = await _mainVm.SupabaseService.SignUpForArbeitseinsatzAsync(_context.WorkAssignmentId, mitgliedId.Value);
            if (result.UpdatedItem != null)
                ApplyRegistrationUpdate(result.UpdatedItem, disableButton: true);

            MessageBox.Show(
                result.Message,
                "Anmeldung",
                MessageBoxButton.OK,
                result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }

        private async Task<int?> ResolveCurrentMemberIdAsync()
        {
            if (_mainVm.UserContext.MitgliedId is > 0 and <= int.MaxValue)
                return (int)_mainVm.UserContext.MitgliedId.Value;

            var member = await _mainVm.EnsureCurrentMemberSelectedAsync();
            return member?.Id > 0 ? member.Id : null;
        }

        private void ApplyRegistrationUpdate(HomeWorkAssignmentItem item, bool disableButton)
        {
            RegistrationInfo = item.RegistrationInfo;
            ShowRegisterButton = disableButton ? false : item.CanRegister;
            AnmeldenCommand.RaiseCanExecuteChanged();
        }
    }
}
