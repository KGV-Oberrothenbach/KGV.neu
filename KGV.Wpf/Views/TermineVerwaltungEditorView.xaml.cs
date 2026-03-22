using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using KGV.ViewModels;

namespace KGV.Views
{
    public partial class TermineVerwaltungEditorView : UserControl
    {
        private INotifyPropertyChanged? _currentViewModel;

        public TermineVerwaltungEditorView()
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
            if (e.PropertyName != nameof(TermineVerwaltungViewModel.FocusRequestToken))
                return;

            if (DataContext is not TermineVerwaltungViewModel vm)
                return;

            Dispatcher.BeginInvoke(() => FocusRequestedControl(vm.FocusTarget), DispatcherPriority.Input);
        }

        private void FocusRequestedControl(string target)
        {
            switch (target)
            {
                case TermineVerwaltungViewModel.FocusTitel:
                    TitelTextBox.Focus();
                    TitelTextBox.SelectAll();
                    break;
                case TermineVerwaltungViewModel.FocusDatum:
                    DatumPicker.Focus();
                    break;
                case TermineVerwaltungViewModel.FocusStartUhrzeit:
                    StartUhrzeitTextBox.Focus();
                    StartUhrzeitTextBox.SelectAll();
                    break;
                case TermineVerwaltungViewModel.FocusEndUhrzeit:
                    EndUhrzeitTextBox.Focus();
                    EndUhrzeitTextBox.SelectAll();
                    break;
                case TermineVerwaltungViewModel.FocusSichtbarAb:
                    SichtbarAbDatePicker.Focus();
                    break;
                case TermineVerwaltungViewModel.FocusSichtbarBis:
                    SichtbarBisDatePicker.Focus();
                    break;
            }
        }
    }
}
