using System.Windows;

namespace KantarPro.Desktop
{
    public enum EntrySaveChoice
    {
        None,
        WeighAndSave,
        SaveWithoutWeighing,
        Exempt
    }

    public partial class EntrySaveChoiceWindow : Window
    {
        public EntrySaveChoice Choice { get; private set; }

        public EntrySaveChoiceWindow(bool showExempt = true)
        {
            InitializeComponent();
            Choice = EntrySaveChoice.None;
            ExemptButton.Visibility = showExempt ? Visibility.Visible : Visibility.Collapsed;
        }

        private void WeighAndSave_Click(object sender, RoutedEventArgs e)
        {
            Choice = EntrySaveChoice.WeighAndSave;
            DialogResult = true;
            Close();
        }

        private void SaveWithoutWeighing_Click(object sender, RoutedEventArgs e)
        {
            Choice = EntrySaveChoice.SaveWithoutWeighing;
            DialogResult = true;
            Close();
        }

        private void Exempt_Click(object sender, RoutedEventArgs e)
        {
            Choice = EntrySaveChoice.Exempt;
            DialogResult = true;
            Close();
        }
    }
}



