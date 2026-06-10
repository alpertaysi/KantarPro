using System;
using System.Globalization;
using System.Windows;

namespace KantarPro.Desktop
{
    public partial class KantarFisPreviewWindow : Window
    {
        private readonly KantarFisPreviewData _data;

        public KantarFisPreviewWindow(KantarFisPreviewData data)
        {
            _data = data;
            InitializeComponent();
            LoadPreview();
        }

        private void LoadPreview()
        {
            var settings = StationSettingsStore.Load();
            ReceiptTypeTextBlock.Text = "Fiş Tipi: " + ValueOrDash(_data.FisTipi);
            ContinuousFormNoteTextBlock.Text = string.Equals(settings.ReceiptPrintMode, StationSettings.ReceiptPrintModeLaserA5, StringComparison.OrdinalIgnoreCase)
                ? "Lazer A5 modunda aynı fiş metni A5 sayfa düzeninde yazdırılır."
                : _data.SurekliFormNotu;
            ReceiptTextBox.Text = _data.RawText;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintCurrentReceipt();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Kantar fişi yazdırılamadı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PrintCurrentReceipt()
        {
            var settings = StationSettingsStore.Load();
            var printerName = RawPrinterHelper.GetPreferredPrinterName();
            if (string.Equals(settings.ReceiptPrintMode, StationSettings.ReceiptPrintModeLaserA5, StringComparison.OrdinalIgnoreCase))
            {
                RawPrinterHelper.PrintA5TextWithDriver(
                    printerName,
                    _data.RawText,
                    "Kantar Fisi " + ValueOrDash(_data.FisNo),
                    ParseFontSize(DriverFontSizeTextBox.Text));
                return;
            }

            RawPrinterHelper.PrintTextWithDriver(
                printerName,
                _data.RawText,
                "Kantar Fisi " + ValueOrDash(_data.FisNo),
                topMarginLines: -1,
                leftMarginColumns: 2,
                fontSize: ParseFontSize(DriverFontSizeTextBox.Text));
        }

        private static string ValueOrDash(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }

        private static float ParseFontSize(string value)
        {
            float parsed;
            return float.TryParse((value ?? "").Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                ? parsed
                : 12.0f;
        }
    }
}



