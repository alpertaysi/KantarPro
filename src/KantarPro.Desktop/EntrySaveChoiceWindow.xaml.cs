using System.Windows;

namespace KantarPro.Desktop
{
    public enum EntrySaveChoice
    {
        None,
        WeighAndSave,
        SaveWithoutWeighing
    }

    public partial class EntrySaveChoiceWindow : Window
    {
        public EntrySaveChoice Choice { get; private set; }

        public EntrySaveChoiceWindow()
        {
            InitializeComponent();
            Choice = EntrySaveChoice.None;
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
    }
}
