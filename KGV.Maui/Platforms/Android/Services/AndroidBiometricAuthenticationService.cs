using Android.Hardware.Biometrics;
using Android.OS;
using KGV.Maui.Services;
using Microsoft.Maui.ApplicationModel;

namespace KGV.Maui.Platforms.Android.Services;

public sealed class AndroidBiometricAuthenticationService : IBiometricAuthenticationService
{
    public Task<bool> IsAvailableAsync()
    {
        var activity = Platform.CurrentActivity;
        return Task.FromResult(Build.VERSION.SdkInt >= BuildVersionCodes.P && activity != null);
    }
    public Task<bool> AuthenticateAsync(string reason)
    {
        var activity = Platform.CurrentActivity;
        if (activity == null || Build.VERSION.SdkInt < BuildVersionCodes.P) return Task.FromResult(false);
        var source = new TaskCompletionSource<bool>();
        var prompt = new BiometricPrompt.Builder(activity).SetTitle("KGV-App entsperren").SetSubtitle(reason)
            .SetNegativeButton("Abbrechen", activity.MainExecutor, new CancelListener(source)).Build();
        prompt.Authenticate(new CancellationSignal(), activity.MainExecutor, new ResultListener(source));
        return source.Task;
    }
    private sealed class CancelListener(TaskCompletionSource<bool> source) : Java.Lang.Object, global::Android.Content.IDialogInterfaceOnClickListener { public void OnClick(global::Android.Content.IDialogInterface? dialog, int which) => source.TrySetResult(false); }
    private sealed class ResultListener(TaskCompletionSource<bool> source) : BiometricPrompt.AuthenticationCallback { public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult? result) => source.TrySetResult(true); public override void OnAuthenticationError(BiometricErrorCode errorCode, Java.Lang.ICharSequence? errString) => source.TrySetResult(false); public override void OnAuthenticationFailed() { } }
}
