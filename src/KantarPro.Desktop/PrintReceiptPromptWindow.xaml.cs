using System.Windows;

namespace KantarPro.Desktop
{
    public partial class PrintReceiptPromptWindow : Window
    {
        public PrintReceiptPromptWindow(string plaka)
        {
            InitializeComponent();
            MessageText.Text = string.IsNullOrWhiteSpace(plaka)
                ? "Tartimli kayit icin kantar fisi hemen yazdirilabilir."
                : plaka.Trim() + " plakali tartimli kayit icin kantar fisi hemen yazdirilabilir.";
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}



