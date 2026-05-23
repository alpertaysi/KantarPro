using System;
using System.Globalization;
using System.Windows;

namespace KantarPro.Desktop
{
    public partial class DateInputWindow : Window
    {
        public DateInputWindow(DateTime initialValue)
        {
            InitializeComponent();
            DateTextBox.Text = initialValue.ToString("dd.MM.yyyy HH:mm:ss");
            DateTextBox.SelectAll();
            DateTextBox.Focus();
        }

        public DateTime SelectedDate { get; private set; }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DateTime value;
            if (!DateTime.TryParse(DateTextBox.Text, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.AllowWhiteSpaces, out value))
            {
                MessageBox.Show("Geçerli tarih ve saat girin.", "Geliş Tarihi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedDate = value;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
