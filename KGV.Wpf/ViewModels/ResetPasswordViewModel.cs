using CommunityToolkit.Mvvm.Input;
using KGV.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public sealed class ResetPasswordViewModel : BaseViewModel
    {
        private readonly IAuthService? _authService;
        private string _email = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isBusy;

        public string Title => "Passwort zurücksetzen";
        public string Description => "Startet den vorhandenen Supabase-Reset-Mailpfad für die ausgewählte E-Mail-Adresse.";
        public string RecoveryHint => "Der Versand nutzt den bereits vorhandenen Supabase-Reset-Flow. Link- oder Browser-Folgeschritte werden in diesem Block nicht neu erfunden.";

        public string Email
        {
            get => _email;
            set
            {
                if (!SetProperty(ref _email, value))
                    return;

                SendCommand.NotifyCanExecuteChanged();
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

                SendCommand.NotifyCanExecuteChanged();
            }
        }

        public IAsyncRelayCommand SendCommand { get; }

        public ResetPasswordViewModel()
        {
            SendCommand = new AsyncRelayCommand(SendAsync, CanSend);
        }

        public ResetPasswordViewModel(IAuthService authService, string? email)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _email = email?.Trim() ?? string.Empty;
            SendCommand = new AsyncRelayCommand(SendAsync, CanSend);
        }

        private bool CanSend()
        {
            return !IsBusy && !string.IsNullOrWhiteSpace(Email) && Email.Contains('@');
        }

        private async Task SendAsync()
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
                var success = await _authService.SendPasswordResetEmailAsync(Email.Trim());
                StatusMessage = success
                    ? "Passwort-Reset-Mail wurde angestoßen. Die weitere Abwicklung bleibt beim bestehenden Supabase-Reset-Flow."
                    : "Passwort-Reset-Mail konnte nicht angestoßen werden.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Passwort-Reset fehlgeschlagen: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
