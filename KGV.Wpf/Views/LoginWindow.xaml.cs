using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KGV.Views
{
    public partial class LoginWindow : Window
    {
        private bool _passwordVisible = false;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is ViewModels.LoginViewModel vm)
            {
                if (vm.IsSetPasswordVisible)
                {
                    if (vm.SetPasswordCommand.CanExecute(null))
                        vm.SetPasswordCommand.Execute(null);

                    return;
                }

                if (vm.IsOtpEntryVisible)
                {
                    if (vm.VerifyOtpCommand.CanExecute(null))
                        vm.VerifyOtpCommand.Execute(null);

                    return;
                }

                if (vm.LoginCommand.CanExecute(null))
                    vm.LoginCommand.Execute(null);
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.LoginViewModel vm)
                vm.Password = PasswordBox.Password;
        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is ViewModels.LoginViewModel vm)
                vm.Password = PasswordTextBox.Text;
        }

        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            if (!_passwordVisible)
            {
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
                PasswordTextBox.Focus();
                _passwordVisible = true;
            }
            else
            {
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                PasswordBox.Focus();
                _passwordVisible = false;
            }
        }

        private void OpenResetPassword_Click(object sender, RoutedEventArgs e)
        {
            var email = string.Empty;
            if (DataContext is ViewModels.LoginViewModel vm)
                email = vm.Email ?? string.Empty;

            var dlg = new ResetPasswordWindow();
            if (dlg.DataContext is ViewModels.ResetPasswordViewModel rvm)
            {
                rvm.Email = email;
            }

            dlg.Owner = this;
            dlg.ShowDialog();
        }

        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.LoginViewModel vm && sender is PasswordBox pb)
                vm.NewPassword = pb.Password;
        }

        private void NewPasswordConfirmBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.LoginViewModel vm && sender is PasswordBox pb)
                vm.NewPasswordConfirm = pb.Password;
        }
    }
}
