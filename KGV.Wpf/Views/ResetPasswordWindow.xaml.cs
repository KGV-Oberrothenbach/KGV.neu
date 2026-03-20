using System.Windows;
using KGV.ViewModels;

namespace KGV.Views
{
    public partial class ResetPasswordWindow : Window
    {
        public ResetPasswordWindow()
            : this(null)
        {
        }

        public ResetPasswordWindow(ResetPasswordViewModel? viewModel)
        {
            InitializeComponent();
            DataContext = viewModel ?? new ResetPasswordViewModel();
        }
    }
}
