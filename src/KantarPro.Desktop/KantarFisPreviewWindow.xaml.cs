using System.Windows;

namespace KantarPro.Desktop
{
    public partial class KantarFisPreviewWindow : Window
    {
        public KantarFisPreviewWindow(string receiptText)
        {
            InitializeComponent();
            ReceiptTextBox.Text = receiptText;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Yazdirma sonraki adimda OKI 5720 yaziciya baglanacak. Simdilik format onizlemesi hazir.", "Kantar Fisi");
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
