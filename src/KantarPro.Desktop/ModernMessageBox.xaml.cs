using System.Windows;
using System.Windows.Media;

namespace KantarPro.Desktop
{
    public enum ModernMessageKind
    {
        Info,
        Success,
        Warning,
        Error
    }

    public partial class ModernMessageBox : Window
    {
        public ModernMessageBox(string title, string message, ModernMessageKind kind)
        {
            InitializeComponent();
            Title = string.IsNullOrWhiteSpace(title) ? "Kantar Pro" : title;
            TitleText.Text = Title;
            MessageText.Text = message ?? string.Empty;
            ApplyKind(kind);
        }

        private void ApplyKind(ModernMessageKind kind)
        {
            switch (kind)
            {
                case ModernMessageKind.Success:
                    AccentBar.Background = Brush("#0F766E");
                    IconCircle.Background = Brush("#DCFCE7");
                    IconText.Foreground = Brush("#047857");
                    IconText.Text = "âœ“";
                    break;
                case ModernMessageKind.Warning:
                    AccentBar.Background = Brush("#D97706");
                    IconCircle.Background = Brush("#FEF3C7");
                    IconText.Foreground = Brush("#B45309");
                    IconText.Text = "!";
                    break;
                case ModernMessageKind.Error:
                    AccentBar.Background = Brush("#DC2626");
                    IconCircle.Background = Brush("#FEE2E2");
                    IconText.Foreground = Brush("#B91C1C");
                    IconText.Text = "!";
                    break;
                default:
                    AccentBar.Background = Brush("#0284C7");
                    IconCircle.Background = Brush("#E0F2FE");
                    IconText.Foreground = Brush("#0369A1");
                    IconText.Text = "i";
                    break;
            }
        }

        private static SolidColorBrush Brush(string color)
        {
            return (SolidColorBrush)new BrushConverter().ConvertFromString(color);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}



