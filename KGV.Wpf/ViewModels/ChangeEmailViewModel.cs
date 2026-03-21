using CommunityToolkit.Mvvm.Input;
using KGV.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public sealed class ChangeEmailViewModel : BaseViewModel
    {
        private readonly IAuthService? _authService;
        private string _currentEmail = string.Empty;
        private string _newEmail = string.Empty;
        private string _otpCode = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isBusy;
        private bool _isOtpRequested;

        public string Title => "E-Mail ändern";
        public string Description => "Startet einen separaten codebasierten OTP-Flow für die Mailadressänderung des aktuell angemeldeten Benutzers.";
        public string RecoveryHint => CanEditEmail
            ? "Nach dem Anfordern wird der OTP-Code an die neue E-Mail-Adresse gesendet und hier direkt verifiziert. Der Login-/Recovery-Flow bleibt davon getrennt."
            : "Für fremde Benutzer ist im aktuellen Wiederaufbau keine belastbar ableitbare Admin-E-Mail-Änderung vorhanden. Der Dialog bleibt deshalb für andere Konten schreibgeschützt.";

        public string CurrentEmail
        {
            get => _currentEmail;
            private set => SetProperty(ref _currentEmail, value);
        }

        public string NewEmail
        {
            get => _newEmail;
            set
            {
                if (!SetProperty(ref _newEmail, value))
                    return;

                RequestCodeCommand.NotifyCanExecuteChanged();
                VerifyCodeCommand.NotifyCanExecuteChanged();
            }
        }

        public string OtpCode
        {
            get => _otpCode;
            set
            {
                if (!SetProperty(ref _otpCode, value))
                    return;

                VerifyCodeCommand.NotifyCanExecuteChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (!SetProperty(ref _isBusy, value))
                    return;

                RequestCodeCommand.NotifyCanExecuteChanged();
                VerifyCodeCommand.NotifyCanExecuteChanged();
            }
        }

        public bool IsOtpRequested
        {
            get => _isOtpRequested;
            private set => SetProperty(ref _isOtpRequested, value);
        }

        public bool CanEditEmail { get; }

        public IAsyncRelayCommand RequestCodeCommand { get; }
        public IAsyncRelayCommand VerifyCodeCommand { get; }

        public ChangeEmailViewModel()
        {
            CurrentEmail = string.Empty;
            CanEditEmail = false;
            RequestCodeCommand = new AsyncRelayCommand(RequestCodeAsync, CanRequestCode);
            VerifyCodeCommand = new AsyncRelayCommand(VerifyCodeAsync, CanVerifyCode);
        }

        public ChangeEmailViewModel(IAuthService authService, string? currentEmail, bool canEditEmail = true)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            CurrentEmail = currentEmail?.Trim() ?? string.Empty;
            CanEditEmail = canEditEmail;
            RequestCodeCommand = new AsyncRelayCommand(RequestCodeAsync, CanRequestCode);
            VerifyCodeCommand = new AsyncRelayCommand(VerifyCodeAsync, CanVerifyCode);
        }

        private bool CanRequestCode()
        {
            return !IsBusy
                && CanEditEmail
                && !string.IsNullOrWhiteSpace(NewEmail)
                && !string.Equals(NewEmail.Trim(), CurrentEmail, StringComparison.OrdinalIgnoreCase);
        }

        private bool CanVerifyCode()
        {
            return !IsBusy
                && CanEditEmail
                && IsOtpRequested
                && !string.IsNullOrWhiteSpace(NewEmail)
                && !string.IsNullOrWhiteSpace(OtpCode);
        }

        private async Task RequestCodeAsync()
        {
            if (_authService == null)
            {
                StatusMessage = "Auth-Kontext fehlt.";
                return;
            }

            IsBusy = true;
            StatusMessage = string.Empty;

            try
            {
                var success = await _authService.RequestEmailChangeAsync(NewEmail.Trim());
                StatusMessage = success
                    ? "OTP-Code wurde an die neue E-Mail-Adresse gesendet. Bitte hier eingeben und bestätigen."
                    : "E-Mail-Änderung konnte nicht angestoßen werden.";
                IsOtpRequested = success;
                if (!success)
                    OtpCode = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"E-Mail-Änderung fehlgeschlagen: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task VerifyCodeAsync()
        {
            if (_authService == null)
            {
                StatusMessage = "Auth-Kontext fehlt.";
                return;
            }

            IsBusy = true;
            StatusMessage = string.Empty;

            try
            {
                var targetEmail = NewEmail.Trim();
                var success = await _authService.VerifyEmailChangeOtpAsync(targetEmail, OtpCode.Trim());
                StatusMessage = success
                    ? "Mailadresse erfolgreich geändert."
                    : "OTP-Code konnte nicht bestätigt werden.";

                if (success)
                {
                    CurrentEmail = targetEmail;
                    NewEmail = string.Empty;
                    OtpCode = string.Empty;
                    IsOtpRequested = false;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"OTP-Prüfung fehlgeschlagen: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
