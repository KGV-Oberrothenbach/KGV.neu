using System.Windows;
using KGV.ViewModels;

namespace KGV.Views
{
    public partial class ChangeEmailWindow : Window
    {
        public ChangeEmailWindow()
        {
            InitializeComponent();
            DataContext ??= new ChangeEmailViewModel();
        }
    }
}
