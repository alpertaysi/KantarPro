using System;
using System.Globalization;
using System.Windows;

namespace KantarPro.Desktop
{
    public partial class DoluBosSecondWeighingWindow : Window
    {
        private readonly PendingWeighingPrototypeRow _pendingRow;
        private readonly decimal _scaleWeightKg;
        private readonly DateTime _secondWeighingDate;

        public decimal SecondWeightKg { get; private set; }

        public DoluBosSecondWeighingWindow(PendingWeighingPrototypeRow pendingRow, decimal scaleWeightKg, DateTime secondWeighingDate)
        {
            _pendingRow = pendingRow ?? throw new ArgumentNullException(nameof(pendingRow));
            _scaleWeightKg = scaleWeightKg;
            _secondWeighingDate = secondWeighingDate;

            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            PlateTextBox.Text = _pendingRow.Plaka;
            EntryDateTextBox.Text = string.IsNullOrWhiteSpace(_pendingRow.IlkGirisTarihi) ? _pendingRow.IlkTartimTarihi : _pendingRow.IlkGirisTarihi;
            EntryTimeTextBox.Text = string.IsNullOrWhiteSpace(_pendingRow.IlkGirisSaati) ? _pendingRow.IlkTartimSaati : _pendingRow.IlkGirisSaati;
            ExitDateTextBox.Text = _pendingRow.IlkCikisTarihi;
            ExitTimeTextBox.Text = _pendingRow.IlkCikisSaati;
            CustomerComboBox.Text = _pendingRow.FirmaAdi;
            DescriptionComboBox.Text = _pendingRow.Aciklama;
            FirstWeightTextBox.Text = _pendingRow.IlkAgirlik;
            SecondWeightTextBox.Text = "0";
            NetWeightTextBox.Text = "0";
        }

        private void TakeWeight_Click(object sender, RoutedEventArgs e)
        {
            SecondWeightTextBox.Text = FormatWeight(_scaleWeightKg);
            CalculateNet();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            decimal secondWeight;
            if (!TryParseWeight(SecondWeightTextBox.Text, out secondWeight) || secondWeight <= 0)
            {
                MessageBox.Show("2.KG icin gecerli agirlik girin.", "Dolu-Bos Tartim", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SecondWeightKg = secondWeight;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CalculateNet()
        {
            decimal firstWeight;
            decimal secondWeight;
            if (!TryParseWeight(FirstWeightTextBox.Text, out firstWeight) ||
                !TryParseWeight(SecondWeightTextBox.Text, out secondWeight))
            {
                NetWeightTextBox.Text = "0";
                return;
            }

            NetWeightTextBox.Text = FormatWeight(Math.Abs(firstWeight - secondWeight));
        }

        private static bool TryParseWeight(string text, out decimal value)
        {
            var normalized = (text ?? string.Empty)
                .Replace(".", string.Empty)
                .Replace(',', '.')
                .Trim();
            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private static string FormatWeight(decimal value)
        {
            return value.ToString("N0", CultureInfo.GetCultureInfo("tr-TR"));
        }
    }
}
