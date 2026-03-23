using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using KGV.ViewModels;

namespace KGV.Views
{
    public partial class ArbeitsstundenErfassungView : UserControl
    {
        private INotifyPropertyChanged? _currentViewModel;

        public ArbeitsstundenErfassungView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_currentViewModel != null)
                _currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;

            _currentViewModel = DataContext as INotifyPropertyChanged;
            if (_currentViewModel != null)
                _currentViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ArbeitsstundenErfassungViewModel.FocusRequestToken))
                return;

            if (DataContext is not ArbeitsstundenErfassungViewModel vm)
                return;

            Dispatcher.BeginInvoke(() => FocusRequestedControl(vm.FocusTarget), DispatcherPriority.Input);
        }

        private void FocusRequestedControl(string target)
        {
            switch (target)
            {
                case ArbeitsstundenErfassungViewModel.FocusDatum:
                    DatumPicker.Focus();
                    break;
                case ArbeitsstundenErfassungViewModel.FocusStunden:
                    StundenTextBox.Focus();
                    StundenTextBox.SelectAll();
                    break;
                case ArbeitsstundenErfassungViewModel.FocusArtDerArbeit:
                    ArtDerArbeitTextBox.Focus();
                    ArtDerArbeitTextBox.SelectAll();
                    break;
                case ArbeitsstundenErfassungViewModel.FocusStatus:
                    StatusTextBox.Focus();
                    StatusTextBox.SelectAll();
                    break;
            }
        }
    }
}
