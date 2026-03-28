using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;

namespace KGV.Views
{
    public partial class LoginWindow : Window
    {
        private bool _passwordVisible = false;
        private bool _isSyncingInputs;

        public LoginWindow()
        {
            InitializeComponent();
            DataContextChanged += LoginWindow_DataContextChanged;
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
            if (_isSyncingInputs)
                return;

            if (DataContext is ViewModels.LoginViewModel vm)
                vm.Password = PasswordBox.Password;
        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingInputs)
                return;

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
            if (DataContext is not ViewModels.LoginViewModel vm)
                return;

            var dlg = new ResetPasswordWindow(vm.CreateResetPasswordViewModel());

            dlg.Owner = this;
            dlg.ShowDialog();
        }

        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncingInputs)
                return;

            if (DataContext is ViewModels.LoginViewModel vm && sender is PasswordBox pb)
                vm.NewPassword = pb.Password;
        }

        private void NewPasswordConfirmBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncingInputs)
                return;

            if (DataContext is ViewModels.LoginViewModel vm && sender is PasswordBox pb)
                vm.NewPasswordConfirm = pb.Password;
        }

        private void LoginWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is INotifyPropertyChanged oldVm)
                oldVm.PropertyChanged -= LoginViewModel_PropertyChanged;

            if (e.NewValue is INotifyPropertyChanged newVm)
                newVm.PropertyChanged += LoginViewModel_PropertyChanged;

            SyncPasswordInputs();
        }

        private void LoginViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ViewModels.LoginViewModel.Password)
                or nameof(ViewModels.LoginViewModel.NewPassword)
                or nameof(ViewModels.LoginViewModel.NewPasswordConfirm)
                or nameof(ViewModels.LoginViewModel.IsSetPasswordVisible)
                or nameof(ViewModels.LoginViewModel.IsOtpRequested))
            {
                SyncPasswordInputs();
            }
        }

        private void SyncPasswordInputs()
        {
            if (DataContext is not ViewModels.LoginViewModel vm)
                return;

            _isSyncingInputs = true;
            try
            {
                var password = vm.Password ?? string.Empty;
                if (PasswordBox.Password != password)
                    PasswordBox.Password = password;
                if (PasswordTextBox.Text != password)
                    PasswordTextBox.Text = password;

                var newPassword = vm.NewPassword ?? string.Empty;
                if (NewPasswordBox.Password != newPassword)
                    NewPasswordBox.Password = newPassword;

                var confirmPassword = vm.NewPasswordConfirm ?? string.Empty;
                if (NewPasswordConfirmBox.Password != confirmPassword)
                    NewPasswordConfirmBox.Password = confirmPassword;
            }
            finally
            {
                _isSyncingInputs = false;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is INotifyPropertyChanged vm)
                vm.PropertyChanged -= LoginViewModel_PropertyChanged;

            base.OnClosed(e);
        }
    }
}
