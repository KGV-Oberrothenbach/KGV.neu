using System;
using System.Windows;
using KGV.ViewModels;

namespace KGV.Views
{
    public partial class ArbeitsstundenErfassungWindow : Window
    {
        public ArbeitsstundenErfassungWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Closed += OnClosed;
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ArbeitsstundenErfassungViewModel oldVm)
                oldVm.CloseRequested -= OnCloseRequested;

            if (e.NewValue is ArbeitsstundenErfassungViewModel newVm)
                newVm.CloseRequested += OnCloseRequested;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is KGV.Core.Interfaces.INavigationAware vm)
                await vm.OnNavigatedToAsync();
        }

        private async void OnClosed(object? sender, EventArgs e)
        {
            if (DataContext is ArbeitsstundenErfassungViewModel vm)
                vm.CloseRequested -= OnCloseRequested;

            if (DataContext is KGV.Core.Interfaces.INavigationAware navigationAware)
                await navigationAware.OnNavigatedFromAsync();
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
