using System.Windows;

namespace KantarPro.Desktop
{
    public partial class FirmaUpdateWindow : Window
    {
        public FirmaUpdateWindow(string plate, string company)
        {
            InitializeComponent();
            PlateTextBox.Text = plate;
            CompanyTextBox.Text = company ?? string.Empty;
            CompanyTextBox.SelectAll();
            CompanyTextBox.Focus();
        }

        public string FirmaAdi { get; private set; }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            FirmaAdi = (CompanyTextBox.Text ?? string.Empty).Trim();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}



