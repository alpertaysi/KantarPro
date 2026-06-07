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
        private readonly MessageBoxButton _buttons;

        public MessageBoxResult Result { get; private set; }

        public ModernMessageBox(string title, string message, ModernMessageKind kind)
            : this(title, message, kind, MessageBoxButton.OK)
        {
        }

        public ModernMessageBox(string title, string message, ModernMessageKind kind, MessageBoxButton buttons)
        {
            InitializeComponent();
            _buttons = buttons;
            Result = GetDefaultResult(buttons);
            Title = string.IsNullOrWhiteSpace(title) ? "Kantar Pro" : title;
            TitleText.Text = Title;
            MessageText.Text = message ?? string.Empty;
            ApplyKind(kind);
            ApplyButtons(buttons);
        }

        private void ApplyKind(ModernMessageKind kind)
        {
            switch (kind)
            {
                case ModernMessageKind.Success:
                    AccentBar.Background = Brush("#0F766E");
                    IconCircle.Background = Brush("#DCFCE7");
                    IconText.Foreground = Brush("#047857");
                    IconText.Text = "\u2713";
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

        private static MessageBoxResult GetDefaultResult(MessageBoxButton buttons)
        {
            switch (buttons)
            {
                case MessageBoxButton.YesNo:
                    return MessageBoxResult.No;
                case MessageBoxButton.OKCancel:
                    return MessageBoxResult.Cancel;
                case MessageBoxButton.YesNoCancel:
                    return MessageBoxResult.Cancel;
                default:
                    return MessageBoxResult.OK;
            }
        }

        private void ApplyButtons(MessageBoxButton buttons)
        {
            SecondaryButton.Visibility = Visibility.Collapsed;
            SecondaryButton.IsCancel = false;
            PrimaryButton.IsCancel = false;

            switch (buttons)
            {
                case MessageBoxButton.YesNo:
                    SecondaryButton.Visibility = Visibility.Visible;
                    SecondaryButton.Content = "Vazgeç";
                    PrimaryButton.Content = "Evet";
                    SecondaryButton.IsCancel = true;
                    break;
                case MessageBoxButton.OKCancel:
                    SecondaryButton.Visibility = Visibility.Visible;
                    SecondaryButton.Content = "Vazgeç";
                    PrimaryButton.Content = "Tamam";
                    SecondaryButton.IsCancel = true;
                    break;
                case MessageBoxButton.YesNoCancel:
                    SecondaryButton.Visibility = Visibility.Visible;
                    SecondaryButton.Content = "Vazgeç";
                    PrimaryButton.Content = "Evet";
                    SecondaryButton.IsCancel = true;
                    break;
                default:
                    PrimaryButton.Content = "Tamam";
                    PrimaryButton.IsCancel = true;
                    break;
            }
        }

        private void PrimaryButton_Click(object sender, RoutedEventArgs e)
        {
            Result = _buttons == MessageBoxButton.YesNo || _buttons == MessageBoxButton.YesNoCancel
                ? MessageBoxResult.Yes
                : MessageBoxResult.OK;
            DialogResult = true;
            Close();
        }

        private void SecondaryButton_Click(object sender, RoutedEventArgs e)
        {
            Result = _buttons == MessageBoxButton.YesNo
                ? MessageBoxResult.No
                : MessageBoxResult.Cancel;
            DialogResult = false;
            Close();
        }
    }
}



