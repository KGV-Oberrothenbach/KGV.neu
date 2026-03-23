using KGV.Core.Interfaces;
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

        public string SectionTitle => _context.SectionTitle;
        public string Title => _context.Title;
        public string Subtitle => _context.Subtitle;
        public string StartTimeText => _context.StartTimeText;
        public string EndTimeText => _context.EndTimeText;
        public string Content => _context.Content;
        public string AdditionalInfo => _context.AdditionalInfo;
        public string RegistrationInfo => _context.RegistrationInfo;
        public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);
        public bool HasStartTimeText => !string.IsNullOrWhiteSpace(StartTimeText);
        public bool HasEndTimeText => !string.IsNullOrWhiteSpace(EndTimeText);
        public bool HasContent => !string.IsNullOrWhiteSpace(Content);
        public bool HasAdditionalInfo => !string.IsNullOrWhiteSpace(AdditionalInfo);
        public bool HasRegistrationInfo => !string.IsNullOrWhiteSpace(RegistrationInfo);
        public bool ShowRegisterButton => _context.ShowRegisterButton;

        public RelayCommand<object?> ZurueckCommand { get; }
        public RelayCommand<object?> AnmeldenCommand { get; }

        public HomeSectionDetailViewModel(MainWindowViewModel mainVm, HomeSectionDetailContext context)
        {
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            ZurueckCommand = new RelayCommand<object?>(_ => _ = ZurueckAsync());
            AnmeldenCommand = new RelayCommand<object?>(_ => ShowRegistrationHint(), _ => ShowRegisterButton);
        }

        public Task OnNavigatedToAsync() => Task.CompletedTask;

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task ZurueckAsync()
        {
            var created = _mainVm.NavigateToHomeViewModel();
            if (created != null)
                await _mainVm.NavigateToAsync(created);
        }

        private static void ShowRegistrationHint()
        {
            MessageBox.Show(
                "Die Anmeldung zu Arbeitseinsätzen ist im aktuellen WPF-Stand noch nicht an einen belastbaren Schreibpfad angebunden.",
                "Anmeldung",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
