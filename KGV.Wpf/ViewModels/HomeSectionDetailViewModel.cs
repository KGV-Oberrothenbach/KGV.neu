using KGV.Core.Interfaces;
using KGV.Helpers;
using System;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public sealed class HomeSectionDetailViewModel : BaseViewModel, INavigationAware
    {
        private readonly MainWindowViewModel _mainVm;
        private readonly HomeSectionDetailContext _context;

        public string SectionTitle => _context.SectionTitle;
        public string Title => _context.Title;
        public string Subtitle => _context.Subtitle;
        public string Content => _context.Content;
        public string AdditionalInfo => _context.AdditionalInfo;
        public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);
        public bool HasContent => !string.IsNullOrWhiteSpace(Content);
        public bool HasAdditionalInfo => !string.IsNullOrWhiteSpace(AdditionalInfo);

        public RelayCommand<object?> ZurueckCommand { get; }

        public HomeSectionDetailViewModel(MainWindowViewModel mainVm, HomeSectionDetailContext context)
        {
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            ZurueckCommand = new RelayCommand<object?>(_ => _ = ZurueckAsync());
        }

        public Task OnNavigatedToAsync() => Task.CompletedTask;

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task ZurueckAsync()
        {
            var created = _mainVm.NavigateToHomeViewModel();
            if (created != null)
                await _mainVm.NavigateToAsync(created);
        }
    }
}
