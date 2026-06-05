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
            ReceiptTypeTextBlock.Text = "Fis Tipi: " + ValueOrDash(_data.FisTipi);
            ContinuousFormNoteTextBlock.Text = _data.SurekliFormNotu;
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
                MessageBox.Show(ex.Message, "Kantar fisi yazdirilamadi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PrintCurrentReceipt()
        {
            var printerName = RawPrinterHelper.GetPreferredPrinterName();
            RawPrinterHelper.PrintTextWithDriver(
                printerName,
                _data.RawText,
                "Kantar Fisi " + ValueOrDash(_data.FisNo),
                topMarginLines: 0,
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
