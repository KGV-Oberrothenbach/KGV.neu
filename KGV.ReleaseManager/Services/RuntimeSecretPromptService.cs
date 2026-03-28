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
        var samePasswordCheckBox = new CheckBox
        {
            Content = "Key-Passwort = Keystore-Passwort",
            Margin = new Thickness(0, 0, 0, 8),
            IsChecked = true
        };

        void ApplyPasswordMode()
        {
            var useSamePassword = samePasswordCheckBox.IsChecked == true;
            keyPasswordBox.IsEnabled = !useSamePassword;
            keyPasswordBox.Opacity = useSamePassword ? 0.65 : 1.0;

            if (useSamePassword)
            {
                keyPasswordBox.Password = string.Empty;
            }
        }

        samePasswordCheckBox.Checked += (_, _) => ApplyPasswordMode();
        samePasswordCheckBox.Unchecked += (_, _) => ApplyPasswordMode();
        ApplyPasswordMode();

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
                samePasswordCheckBox,
                new TextBlock { Text = "Key-Passwort" },
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
            var useSamePassword = samePasswordCheckBox.IsChecked == true;
            if (string.IsNullOrWhiteSpace(storePasswordBox.Password))
            {
                MessageBox.Show(window, "Das Keystore-Passwort ist erforderlich.", "Android-Signierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!useSamePassword && string.IsNullOrWhiteSpace(keyPasswordBox.Password))
            {
                MessageBox.Show(window, "Das Key-Passwort ist erforderlich, wenn kein gemeinsames Passwort verwendet wird.", "Android-Signierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            result = new AndroidSigningSecrets
            {
                StorePassword = storePasswordBox.Password,
                KeyPassword = useSamePassword ? storePasswordBox.Password : keyPasswordBox.Password,
                UseSamePasswordForKey = useSamePassword
            };

            window.DialogResult = true;
            window.Close();
        };

        return window.ShowDialog() == true ? result : null;
    }
}
