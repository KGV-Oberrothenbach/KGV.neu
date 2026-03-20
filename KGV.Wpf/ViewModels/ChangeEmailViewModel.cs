using CommunityToolkit.Mvvm.Input;
using KGV.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public sealed class ChangeEmailViewModel : BaseViewModel
    {
        private readonly IAuthService? _authService;
        private string _newEmail = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isBusy;

        public string Title => "E-Mail ändern";
        public string Description => "Stoßt für den aktuell angemeldeten Benutzer den vorhandenen Supabase-E-Mail-Änderungspfad an.";
        public string RecoveryHint => CanEditEmail
            ? "Nach erfolgreichem Request ist der weitere OTP-/Bestätigungsschritt außerhalb dieses WPF-Blocks weiterhin Supabase-seitig zu Ende zu führen."
            : "Für fremde Benutzer ist im aktuellen Wiederaufbau keine belastbar ableitbare Admin-E-Mail-Änderung vorhanden. Der Dialog bleibt deshalb für andere Konten schreibgeschützt.";

        public string CurrentEmail { get; }

        public string NewEmail
        {
            get => _newEmail;
            set
            {
                if (!SetProperty(ref _newEmail, value))
                    return;

                SubmitCommand.NotifyCanExecuteChanged();
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

                SubmitCommand.NotifyCanExecuteChanged();
            }
        }

        public bool CanEditEmail { get; }

        public IAsyncRelayCommand SubmitCommand { get; }

        public ChangeEmailViewModel()
        {
            CurrentEmail = string.Empty;
            CanEditEmail = false;
            SubmitCommand = new AsyncRelayCommand(SubmitAsync, CanSubmit);
        }

        public ChangeEmailViewModel(IAuthService authService, string? currentEmail, bool canEditEmail = true)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            CurrentEmail = currentEmail?.Trim() ?? string.Empty;
            CanEditEmail = canEditEmail;
            SubmitCommand = new AsyncRelayCommand(SubmitAsync, CanSubmit);
        }

        private bool CanSubmit()
        {
            return !IsBusy
                && CanEditEmail
                && !string.IsNullOrWhiteSpace(NewEmail)
                && !string.Equals(NewEmail.Trim(), CurrentEmail, StringComparison.OrdinalIgnoreCase);
        }

        private async Task SubmitAsync()
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
                var success = await _authService.ChangeEmailAsync(NewEmail.Trim());
                StatusMessage = success
                    ? "E-Mail-Änderung angestoßen. Der weitere Bestätigungs-/OTP-Schritt läuft Supabase-seitig außerhalb dieses Blocks."
                    : "E-Mail-Änderung konnte nicht angestoßen werden.";
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
    }
}
