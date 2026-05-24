using System.Windows;

namespace KantarPro.Desktop
{
    public partial class PlateCorrectionWindow : Window
    {
        public PlateCorrectionWindow(string oldPlate)
        {
            InitializeComponent();
            OldPlateTextBox.Text = oldPlate;
            NewPlateTextBox.Text = oldPlate;
            NewPlateTextBox.SelectAll();
            NewPlateTextBox.Focus();
        }

        public string NewPlate { get; private set; }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var value = (NewPlateTextBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                MessageBox.Show("Yeni plaka bos olamaz.", "Plaka Duzelt", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NewPlate = value;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
