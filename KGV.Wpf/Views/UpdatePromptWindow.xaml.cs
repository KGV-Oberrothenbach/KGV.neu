using KGV.Wpf.Models;
using System;
using System.ComponentModel;
using System.Windows;

namespace KGV.Views
{
    public partial class UpdatePromptWindow : Window
    {
        private readonly bool _mandatory;
        private bool _allowClose;

        public UpdatePromptWindow(string currentVersion, AppUpdateInfo updateInfo)
        {
            if (updateInfo == null)
                throw new ArgumentNullException(nameof(updateInfo));

            InitializeComponent();

            _mandatory = updateInfo.Mandatory;

            CurrentVersionTextBlock.Text = string.IsNullOrWhiteSpace(currentVersion)
                ? "Unbekannt"
                : currentVersion.Trim();

            NewVersionTextBlock.Text = string.IsNullOrWhiteSpace(updateInfo.Version)
                ? "Unbekannt"
                : updateInfo.Version.Trim();

            PublishedAtTextBlock.Text = string.IsNullOrWhiteSpace(updateInfo.PublishedAt)
                ? "Nicht angegeben"
                : updateInfo.PublishedAt.Trim();

            MandatoryTextBlock.Text = _mandatory
                ? "Verpflichtendes Update"
                : "Optionales Update";

            IntroTextBlock.Text = _mandatory
                ? "Für diese Version ist ein verpflichtendes Update verfügbar. Bitte installiere die neue Version, um die Anwendung weiter zu nutzen."
                : "Es ist eine neue Version der Anwendung verfügbar. Du kannst das Update jetzt installieren oder später fortfahren.";

            var notesText = updateInfo.GetNotesText();
            NotesTextBlock.Text = string.IsNullOrWhiteSpace(notesText)
                ? "Für diese Version wurden keine zusätzlichen Hinweise hinterlegt."
                : notesText;

            if (_mandatory)
            {
                LaterButton.Visibility = Visibility.Collapsed;
                FooterTextBlock.Text = "Dieses Update muss installiert werden, bevor du weiterarbeiten kannst.";
            }
            else
            {
                FooterTextBlock.Text = "Beim Klick auf „Update installieren“ wird die Download-Seite geöffnet.";
            }

            Closing += UpdatePromptWindow_Closing;
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            _allowClose = true;
            DialogResult = true;
        }

        private void LaterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mandatory)
                return;

            _allowClose = true;
            DialogResult = false;
        }

        private void UpdatePromptWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (_mandatory && !_allowClose)
                e.Cancel = true;
        }
    }
}