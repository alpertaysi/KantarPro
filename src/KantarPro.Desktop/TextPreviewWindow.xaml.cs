using System;
using System.Globalization;
using System.Windows;

namespace KantarPro.Desktop
{
    public partial class TextPreviewWindow : Window
    {
        private readonly string _text;
        private readonly string _documentName;

        public TextPreviewWindow(string title, string text)
        {
            _documentName = string.IsNullOrWhiteSpace(title) ? "Metin Onizleme" : title.Trim();
            _text = text ?? string.Empty;
            InitializeComponent();
            Title = _documentName;
            TitleTextBlock.Text = _documentName;
            PreviewTextBox.Text = _text;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RawPrinterHelper.PrintTextWithDriver(
                    RawPrinterHelper.GetPreferredPrinterName(),
                    _text,
                    _documentName,
                    topMarginLines: 0,
                    leftMarginColumns: 0,
                    fontSize: ParseFontSize(DriverFontSizeTextBox.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Metin yazdırılamadı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static float ParseFontSize(string value)
        {
            float parsed;
            return float.TryParse((value ?? "").Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                ? parsed
                : 9.0f;
        }
    }
}
