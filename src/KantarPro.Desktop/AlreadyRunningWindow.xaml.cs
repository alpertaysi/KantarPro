using System.Windows;

namespace KantarPro.Desktop
{
    public partial class AlreadyRunningWindow : Window
    {
        public AlreadyRunningWindow()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
