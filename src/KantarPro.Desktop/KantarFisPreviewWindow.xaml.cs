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
            PlateTextBlock.Text = ValueOrDash(_data.Plaka);
            ReceiptNoTextBlock.Text = ValueOrDash(_data.FisNo);
            FirstDateTextBlock.Text = CombineDateTime(_data.GirisTarihi, _data.GirisSaati);
            SecondDateTextBlock.Text = CombineDateTime(_data.IkinciGirisTarihi, _data.IkinciGirisSaati);
            FirstWeightTextBlock.Text = ValueOrDash(_data.BirinciTartim);
            SecondWeightTextBlock.Text = ValueOrDash(_data.IkinciTartim) + " / " + ValueOrDash(_data.Net);
            ReceiptTextBox.Text = _data.RawText;

            if (!_data.DoluBosMu)
            {
                SecondVisitBorder.Visibility = Visibility.Collapsed;
                SecondWeightBorder.Visibility = Visibility.Collapsed;
                FirstDateLabelTextBlock.Text = "GIRIS TARIHI / SAATI";
            }
            else
            {
                SecondVisitBorder.Visibility = Visibility.Visible;
                SecondWeightBorder.Visibility = Visibility.Visible;
                FirstDateLabelTextBlock.Text = "1. GIRIS TARIHI / SAATI";
            }
        }

        private void ShowRawText_Click(object sender, RoutedEventArgs e)
        {
            PreviewTabs.SelectedIndex = 1;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Yazdirma sonraki adimda OKI 5720 yaziciya ham metin olarak baglanacak. Ham Metin sekmesindeki icerik yaziciya gidecek cikti taslagidir.", "Kantar Fisi");
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static string CombineDateTime(string date, string time)
        {
            var value = (date ?? "").Trim() + " " + (time ?? "").Trim();
            return ValueOrDash(value);
        }

        private static string ValueOrDash(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }
    }
}
