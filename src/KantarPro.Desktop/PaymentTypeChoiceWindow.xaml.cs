using System.Windows;
using KantarPro.Domain;

namespace KantarPro.Desktop
{
    public partial class PaymentTypeChoiceWindow : Window
    {
        public string SelectedPaymentType { get; private set; }

        public PaymentTypeChoiceWindow()
        {
            InitializeComponent();
        }

        private void CashButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedPaymentType = KantarSabitleri.OdemeTuru.Nakit;
            DialogResult = true;
            Close();
        }

        private void CardButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedPaymentType = KantarSabitleri.OdemeTuru.KrediKarti;
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
