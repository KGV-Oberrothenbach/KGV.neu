using System.Globalization;
using System.Windows;
using KGV.Core.Utilities;

namespace KGV.Views
{
    public partial class MitgliedsantragDialog : Window
    {
        private static readonly CultureInfo DeCulture = CultureInfo.GetCultureInfo("de-DE");

        public decimal Mitgliedsbeitrag { get; private set; }

        public MitgliedsantragDialog()
        {
            InitializeComponent();
        }

        public void SetInitialValues(string? displayName, MitgliedsantragBeitragVorschlag vorschlag)
        {
            var name = string.IsNullOrWhiteSpace(displayName) ? "das ausgewählte Mitglied" : displayName.Trim();
            BeschreibungTextBlock.Text = $"Der Mitgliedsantrag wird für {name} als rein mitgliedsbezogenes Dokument erzeugt.";
            BeginnTextBlock.Text = vorschlag.BeginnDatum.ToString("dd.MM.yyyy", DeCulture);
            JahresbeitragTextBlock.Text = FormatCurrency(vorschlag.Jahresbeitrag);
            HinweisTextBlock.Text = vorschlag.IstHalberBeitrag
                ? $"Beginn ab 01.07.{vorschlag.SaisonJahr}: Es wird automatisch der halbe Jahresbeitrag vorgeschlagen. Der Wert kann vor dem Erzeugen angepasst werden."
                : $"Beginn vor 01.07.{vorschlag.SaisonJahr}: Es wird automatisch der volle Jahresbeitrag vorgeschlagen. Der Wert kann vor dem Erzeugen angepasst werden.";
            MitgliedsbeitragTextBox.Text = vorschlag.VorgeschlagenerBeitrag.ToString("0.00", DeCulture);
            MitgliedsbeitragTextBox.SelectAll();
            MitgliedsbeitragTextBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseBeitrag(MitgliedsbeitragTextBox.Text, out var mitgliedsbeitrag))
            {
                MessageBox.Show("Bitte einen gültigen Mitgliedsbeitrag eingeben.", "Mitgliedsantrag", MessageBoxButton.OK, MessageBoxImage.Error);
                MitgliedsbeitragTextBox.Focus();
                MitgliedsbeitragTextBox.SelectAll();
                return;
            }

            if (mitgliedsbeitrag < 0m)
            {
                MessageBox.Show("Der Mitgliedsbeitrag darf nicht negativ sein.", "Mitgliedsantrag", MessageBoxButton.OK, MessageBoxImage.Error);
                MitgliedsbeitragTextBox.Focus();
                MitgliedsbeitragTextBox.SelectAll();
                return;
            }

            Mitgliedsbeitrag = MitgliedsantragBeitragHelper.NormalizeBeitrag(mitgliedsbeitrag);
            DialogResult = true;
        }

        private static bool TryParseBeitrag(string? text, out decimal value)
        {
            return decimal.TryParse(text, NumberStyles.Number, DeCulture, out value)
                   || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private static string FormatCurrency(decimal value)
            => MitgliedsantragBeitragHelper.NormalizeBeitrag(value).ToString("0.00 €", DeCulture);
    }
}
