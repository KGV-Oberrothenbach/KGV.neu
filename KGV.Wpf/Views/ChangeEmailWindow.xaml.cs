using System.Windows;
using KGV.ViewModels;

namespace KGV.Views
{
    public partial class ChangeEmailWindow : Window
    {
        public ChangeEmailWindow()
            : this(null)
        {
        }

        public ChangeEmailWindow(ChangeEmailViewModel? viewModel)
        {
            InitializeComponent();
            DataContext = viewModel ?? new ChangeEmailViewModel();
        }
    }
}
