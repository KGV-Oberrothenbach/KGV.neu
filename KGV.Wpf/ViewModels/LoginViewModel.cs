using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KGV;
using KGV.Core.Interfaces;
using System.Threading.Tasks;
using System;

namespace KGV.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        public event Action? LoginSucceeded;

        private readonly IAuthService _authService;

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;

            LoginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);
            RequestOtpCommand = new AsyncRelayCommand(RequestOtpAsync, CanRequestOtp);
            VerifyOtpCommand = new AsyncRelayCommand(VerifyOtpAsync, CanVerifyOtp);
            SetPasswordCommand = new AsyncRelayCommand(SetPasswordAsync, CanSetPassword);
        }

        [ObservableProperty]
        private string email = "";

        [ObservableProperty]
        private string password = "";

        [ObservableProperty]
        private string statusMessage = "";

        [ObservableProperty]
        private string otpCode = "";

        [ObservableProperty]
        private string newPassword = "";

        [ObservableProperty]
        private string newPasswordConfirm = "";

        [ObservableProperty]
        private bool isOtpRequested = false;

        [ObservableProperty]
        private bool isSetPasswordVisible = false;

        public bool IsNormalLoginVisible => !IsOtpRequested && !IsSetPasswordVisible;
        public bool IsOtpEntryVisible => IsOtpRequested && !IsSetPasswordVisible;
        public bool IsStatusVisible => !string.IsNullOrWhiteSpace(StatusMessage);
        public bool IsPasswordRequirementMet => !string.IsNullOrWhiteSpace(NewPassword) && NewPassword.Length >= 8;
        public bool IsPasswordConfirmationMet => !string.IsNullOrWhiteSpace(NewPassword) && NewPassword == NewPasswordConfirm;

        public IAsyncRelayCommand LoginCommand { get; }
        public IAsyncRelayCommand RequestOtpCommand { get; }
        public IAsyncRelayCommand VerifyOtpCommand { get; }
        public IAsyncRelayCommand SetPasswordCommand { get; }

        private bool CanLogin()
        {
            return !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Password);
        }

        private bool CanRequestOtp()
        {
            return !string.IsNullOrWhiteSpace(Email) && Email.Contains('@');
        }

        private async Task RequestOtpAsync()
        {
            StatusMessage = string.Empty;

            var emailTrim = Email?.Trim();
            if (string.IsNullOrEmpty(emailTrim))
            {
                StatusMessage = "E‑Mail leer.";
                return;
            }

            var ok = await _authService.RequestOtpAsync(emailTrim);
            if (ok)
            {
                IsOtpRequested = true;
                IsSetPasswordVisible = false;
                OtpCode = string.Empty;
                NewPassword = string.Empty;
                NewPasswordConfirm = string.Empty;
                StatusMessage = "Code wurde versendet. Bitte OTP eingeben.";
            }
            else
            {
                StatusMessage = "OTP-Anforderung fehlgeschlagen.";
            }
        }

        private bool CanVerifyOtp()
        {
            return !string.IsNullOrWhiteSpace(OtpCode);
        }

        private async Task VerifyOtpAsync()
        {
            StatusMessage = string.Empty;
            var emailTrim = Email?.Trim();
            if (string.IsNullOrEmpty(emailTrim))
            {
                StatusMessage = "E‑Mail leer.";
                return;
            }

            var ok = await _authService.VerifyOtpAsync(emailTrim, OtpCode);
            if (ok)
            {
                IsOtpRequested = false;
                IsSetPasswordVisible = true;
                StatusMessage = "Code bestätigt. Neues Passwort setzen.";
            }
            else
            {
                StatusMessage = "Code ungültig.";
            }
        }

        private bool CanSetPassword()
        {
            return !string.IsNullOrWhiteSpace(NewPassword) && NewPassword.Length >= 8 && NewPassword == NewPasswordConfirm;
        }

        private async Task SetPasswordAsync()
        {
            StatusMessage = string.Empty;
            var emailTrim = Email?.Trim();
            if (string.IsNullOrEmpty(emailTrim))
            {
                StatusMessage = "E‑Mail leer.";
                return;
            }

            var ok = await _authService.SetPasswordWithOtpAsync(emailTrim, OtpCode, NewPassword);
            if (ok)
            {
                ResetOtpFlow(clearLoginPassword: true);
                StatusMessage = "Passwort wurde gesetzt. Bitte normal anmelden.";
            }
            else
            {
                StatusMessage = "Neues Passwort konnte nicht gesetzt werden.";
            }
        }

        private async Task LoginAsync()
        {
            StatusMessage = "";

            // Trim
            var emailTrim = Email.Trim();
            var pwdTrim = Password.Trim();

            if (string.IsNullOrEmpty(emailTrim) ||
                string.IsNullOrEmpty(pwdTrim))
            {
                StatusMessage = "E‑Mail oder Passwort leer.";
                return;
            }

            try
            {
                bool success = await _authService.LoginAsync(emailTrim, pwdTrim);

                if (success)
                {
                    ResetOtpFlow(clearLoginPassword: false);

                    // Email speichern
                    AppSettings.LastEmail = emailTrim;
                    AppSettings.Save();

                    StatusMessage = "Login erfolgreich!";
                    // Ereignis für erfolgreiche Anmeldung auslösen
                    LoginSucceeded?.Invoke();
                    return;
                }
                else
                {
                    StatusMessage = "Login fehlgeschlagen.";
                }
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Fehler: {ex.Message}";
            }
        }

        partial void OnEmailChanged(string value)
        {
            RaiseCommandStates();
        }

        partial void OnPasswordChanged(string value)
        {
            RaiseCommandStates();
        }

        partial void OnOtpCodeChanged(string value)
        {
            RaiseCommandStates();
        }

        partial void OnNewPasswordChanged(string value)
        {
            RaiseCommandStates();
            OnPropertyChanged(nameof(IsPasswordRequirementMet));
            OnPropertyChanged(nameof(IsPasswordConfirmationMet));
        }

        partial void OnNewPasswordConfirmChanged(string value)
        {
            RaiseCommandStates();
            OnPropertyChanged(nameof(IsPasswordConfirmationMet));
        }

        partial void OnIsOtpRequestedChanged(bool value)
        {
            OnPropertyChanged(nameof(IsNormalLoginVisible));
            OnPropertyChanged(nameof(IsOtpEntryVisible));
        }

        partial void OnIsSetPasswordVisibleChanged(bool value)
        {
            OnPropertyChanged(nameof(IsNormalLoginVisible));
            OnPropertyChanged(nameof(IsOtpEntryVisible));
        }

        partial void OnStatusMessageChanged(string value)
        {
            OnPropertyChanged(nameof(IsStatusVisible));
        }

        public ResetPasswordViewModel CreateResetPasswordViewModel()
        {
            return new ResetPasswordViewModel(_authService, Email?.Trim());
        }

        private void ResetOtpFlow(bool clearLoginPassword)
        {
            IsSetPasswordVisible = false;
            IsOtpRequested = false;
            OtpCode = string.Empty;
            NewPassword = string.Empty;
            NewPasswordConfirm = string.Empty;

            if (clearLoginPassword)
                Password = string.Empty;
        }

        private void RaiseCommandStates()
        {
            LoginCommand.NotifyCanExecuteChanged();
            RequestOtpCommand.NotifyCanExecuteChanged();
            VerifyOtpCommand.NotifyCanExecuteChanged();
            SetPasswordCommand.NotifyCanExecuteChanged();
        }
    }
}