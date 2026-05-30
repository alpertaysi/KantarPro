using System.Windows;

namespace KantarPro.Desktop
{
    public partial class ExemptionReasonWindow : Window
    {
        public ExemptionReasonWindow(string plate)
        {
            InitializeComponent();
            PlateTextBox.Text = plate;
            ReasonTextBox.Focus();
        }

        public string ExemptionReason { get; private set; }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var reason = (ReasonTextBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("Muafiyet nedeni boş olamaz. Lütfen geçerli bir açıklama yazın.", "Ücretten Muafiyet", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ExemptionReason = reason;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
