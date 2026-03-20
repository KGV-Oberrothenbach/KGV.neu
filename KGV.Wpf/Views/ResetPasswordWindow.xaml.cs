using System.Windows;
using KGV.ViewModels;

namespace KGV.Views
{
    public partial class ResetPasswordWindow : Window
    {
        public ResetPasswordWindow()
        {
            InitializeComponent();
            DataContext ??= new ResetPasswordViewModel();
        }
    }
}
