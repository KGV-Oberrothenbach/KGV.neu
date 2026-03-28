using System.Windows;
using System.Windows.Controls;
using KGV.ReleaseManager.Models;

namespace KGV.ReleaseManager.Services;

public sealed class RuntimeSecretPromptService
{
    public AndroidSigningSecrets? PromptForAndroidSigningSecrets(Window owner)
    {
        var storePasswordBox = new PasswordBox { Margin = new Thickness(0, 4, 0, 12), MinWidth = 260 };
        var keyPasswordBox = new PasswordBox { Margin = new Thickness(0, 4, 0, 12), MinWidth = 260 };

        var okButton = new Button { Content = "OK", Width = 90, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
        var cancelButton = new Button { Content = "Abbrechen", Width = 90, IsCancel = true };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { okButton, cancelButton }
        };

        var panel = new StackPanel
        {
            Margin = new Thickness(16),
            Children =
            {
                new TextBlock
                {
                    Text = "Android-Signierung: Passwörter werden nur für diesen Lauf verwendet und nicht gespeichert.",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 12)
                },
                new TextBlock { Text = "Keystore-Passwort" },
                storePasswordBox,
                new TextBlock { Text = "Key-Passwort (leer = Keystore-Passwort verwenden)" },
                keyPasswordBox,
                buttonPanel
            }
        };

        var window = new Window
        {
            Title = "Android-Signierung",
            Owner = owner,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.WidthAndHeight,
            Content = panel,
            MinWidth = 420,
            ShowInTaskbar = false
        };

        AndroidSigningSecrets? result = null;

        okButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(storePasswordBox.Password))
            {
                MessageBox.Show(window, "Das Keystore-Passwort ist erforderlich.", "Android-Signierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            result = new AndroidSigningSecrets
            {
                StorePassword = storePasswordBox.Password,
                KeyPassword = string.IsNullOrWhiteSpace(keyPasswordBox.Password) ? storePasswordBox.Password : keyPasswordBox.Password
            };

            window.DialogResult = true;
            window.Close();
        };

        return window.ShowDialog() == true ? result : null;
    }
}
