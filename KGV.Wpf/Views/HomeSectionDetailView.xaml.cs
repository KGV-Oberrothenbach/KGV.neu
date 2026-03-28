using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using KGV.ViewModels;

namespace KGV.Views
{
    public partial class HomeSectionDetailView : UserControl
    {
        private INotifyPropertyChanged? _currentViewModel;

        public HomeSectionDetailView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += (_, _) => RefreshHtmlContent();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_currentViewModel != null)
                _currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;

            _currentViewModel = DataContext as INotifyPropertyChanged;
            if (_currentViewModel != null)
                _currentViewModel.PropertyChanged += OnViewModelPropertyChanged;

            RefreshHtmlContent();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HomeSectionDetailViewModel.HtmlDocument)
                || e.PropertyName == nameof(HomeSectionDetailViewModel.ShowHtmlContent))
            {
                RefreshHtmlContent();
            }
        }

        private void RefreshHtmlContent()
        {
            if (DataContext is not HomeSectionDetailViewModel vm || !vm.ShowHtmlContent)
            {
                HtmlContentBrowser.NavigateToString("<html><body></body></html>");
                return;
            }

            HtmlContentBrowser.NavigateToString(vm.HtmlDocument);
        }
    }
}
