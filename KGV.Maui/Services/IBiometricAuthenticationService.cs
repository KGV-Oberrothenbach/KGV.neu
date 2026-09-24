namespace KGV.Maui.Services;

public interface IBiometricAuthenticationService
{
    Task<bool> IsAvailableAsync();
    Task<bool> AuthenticateAsync(string reason);
}
