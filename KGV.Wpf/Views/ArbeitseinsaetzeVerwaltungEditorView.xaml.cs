using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using KGV.ViewModels;

namespace KGV.Views
{
    public partial class ArbeitseinsaetzeVerwaltungEditorView : UserControl
    {
        private INotifyPropertyChanged? _currentViewModel;

        public ArbeitseinsaetzeVerwaltungEditorView()
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
            if (e.PropertyName != nameof(ArbeitseinsaetzeVerwaltungViewModel.FocusRequestToken))
                return;

            if (DataContext is not ArbeitseinsaetzeVerwaltungViewModel vm)
                return;

            Dispatcher.BeginInvoke(() => FocusRequestedControl(vm.FocusTarget), DispatcherPriority.Input);
        }

        private void FocusRequestedControl(string target)
        {
            switch (target)
            {
                case ArbeitseinsaetzeVerwaltungViewModel.FocusTitel:
                    TitelTextBox.Focus();
                    TitelTextBox.SelectAll();
                    break;
                case ArbeitseinsaetzeVerwaltungViewModel.FocusDatum:
                    DatumPicker.Focus();
                    break;
                case ArbeitseinsaetzeVerwaltungViewModel.FocusStartUhrzeit:
                    StartUhrzeitTextBox.Focus();
                    StartUhrzeitTextBox.SelectAll();
                    break;
                case ArbeitseinsaetzeVerwaltungViewModel.FocusEndUhrzeit:
                    EndUhrzeitTextBox.Focus();
                    EndUhrzeitTextBox.SelectAll();
                    break;
                case ArbeitseinsaetzeVerwaltungViewModel.FocusMaxTeilnehmer:
                    MaxTeilnehmerTextBox.Focus();
                    MaxTeilnehmerTextBox.SelectAll();
                    break;
                case ArbeitseinsaetzeVerwaltungViewModel.FocusStundenWert:
                    StundenWertTextBox.Focus();
                    StundenWertTextBox.SelectAll();
                    break;
                case ArbeitseinsaetzeVerwaltungViewModel.FocusSichtbarAb:
                    SichtbarAbDatePicker.Focus();
                    break;
                case ArbeitseinsaetzeVerwaltungViewModel.FocusSichtbarBis:
                    SichtbarBisDatePicker.Focus();
                    break;
                case ArbeitseinsaetzeVerwaltungViewModel.FocusAnmeldungBis:
                    AnmeldungBisDatePicker.Focus();
                    break;
            }
        }
    }
}
