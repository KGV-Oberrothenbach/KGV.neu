using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using KGV.Core.Models;

namespace KGV.Views
{
    public partial class ArbeitsstundeDialog : Window
    {
        public bool DeleteRequested { get; private set; }

        public int? SelectedMitgliedId
        {
            get
            {
                if (MitgliedComboBox.SelectedValue is int i) return i;
                return null;
            }
        }

        public int? SelectedSaisonId
        {
            get
            {
                if (SaisonComboBox.SelectedValue is int i) return i;
                return null;
            }
        }

        public DateTime? Datum => DatumPicker.SelectedDate;

        public decimal? Stunden
        {
            get
            {
                if (decimal.TryParse(StundenTextBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var v))
                    return v;
                return null;
            }
        }

        public string? Beschreibung => BeschreibungTextBox.Text;

        public bool Freigegeben => FreigegebenCheckBox.IsChecked == true;

        public ArbeitsstundeDialog()
        {
            InitializeComponent();
            DatumPicker.SelectedDate = DateTime.Today;
        }

        public void SetOptions(IReadOnlyList<MemberDTO> mitglieder, IReadOnlyList<SaisonRecord> saisons)
        {
            MitgliedComboBox.ItemsSource = mitglieder;
            SaisonComboBox.ItemsSource = saisons;
        }

        public void SetInitialValues(int? memberId, int? saisonId, DateTime? datum, decimal? stunden, string? beschreibung)
        {
            if (memberId.HasValue)
                MitgliedComboBox.SelectedValue = memberId.Value;

            if (saisonId.HasValue)
                SaisonComboBox.SelectedValue = saisonId.Value;

            DatumPicker.SelectedDate = (datum ?? DateTime.Today).Date;

            StundenTextBox.Text = stunden.HasValue
                ? stunden.Value.ToString(CultureInfo.CurrentCulture)
                : string.Empty;

            BeschreibungTextBox.Text = beschreibung ?? string.Empty;
        }

        public void SetFreigabeMode(bool canApprove, bool defaultFreigegeben)
        {
            FreigegebenCheckBox.IsChecked = defaultFreigegeben;
            FreigegebenCheckBox.IsEnabled = canApprove;
        }

        public void SetDeleteEnabled(bool enabled)
        {
            DeleteButton.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            DialogResult = true;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("Datensatz wirklich löschen?", "Bestätigung", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes)
                return;

            DeleteRequested = true;
            DialogResult = true;
        }

        private bool ValidateInput()
        {
            var membershipValid = SelectedMitgliedId.HasValue;
            var dateValid = Datum.HasValue;
            var saisonValid = SelectedSaisonId.HasValue;
            var stundenValid = Stunden.HasValue && Stunden.Value > 0;
            var beschreibungValid = !string.IsNullOrWhiteSpace(Beschreibung);

            HighlightControl(MitgliedComboBox, membershipValid);
            HighlightControl(DatumPicker, dateValid);
            HighlightControl(SaisonComboBox, saisonValid);
            HighlightControl(StundenTextBox, stundenValid);
            HighlightControl(BeschreibungTextBox, beschreibungValid);

            if (membershipValid && dateValid && saisonValid && stundenValid && beschreibungValid)
                return true;

            MessageBox.Show("Bitte alle Pflichtfelder ausfüllen. Fehlende Felder sind rot markiert.", "Pflichtfelder fehlen", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private void HighlightControl(System.Windows.Controls.Control control, bool isValid)
        {
            control.BorderThickness = isValid ? new Thickness(1) : new Thickness(2);
            control.BorderBrush = isValid ? ResolveBrush("KgvBorderBrush", Brushes.Gray) : Brushes.IndianRed;
        }

        private Brush ResolveBrush(string resourceKey, Brush fallback)
        {
            return TryFindResource(resourceKey) as Brush ?? fallback;
        }
    }
}
