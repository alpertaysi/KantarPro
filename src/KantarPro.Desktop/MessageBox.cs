using System.Windows;

namespace KantarPro.Desktop
{
    internal static class MessageBox
    {
        public static MessageBoxResult Show(string messageBoxText, string caption)
        {
            return Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            var owner = System.Windows.Application.Current != null ? System.Windows.Application.Current.MainWindow : null;
            return Show(owner, messageBoxText, caption, button, icon);
        }

        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            var dialog = new ModernMessageBox(caption, messageBoxText, ToKind(messageBoxText, icon), button);
            if (owner != null && owner.IsVisible && !ReferenceEquals(owner, dialog))
            {
                dialog.Owner = owner;
            }

            dialog.ShowDialog();
            return dialog.Result;
        }

        private static ModernMessageKind ToKind(string message, MessageBoxImage icon)
        {
            if (icon == MessageBoxImage.Warning || icon == MessageBoxImage.Exclamation)
            {
                return ModernMessageKind.Warning;
            }

            if (icon == MessageBoxImage.Error || icon == MessageBoxImage.Hand || icon == MessageBoxImage.Stop)
            {
                return ModernMessageKind.Error;
            }

            var normalized = (message ?? string.Empty).ToUpperInvariant();
            if (normalized.Contains("OLUSTURULDU") ||
                normalized.Contains("TAMAMLANDI") ||
                normalized.Contains("KAYDEDILDI") ||
                normalized.Contains("DUZELTILDI") ||
                normalized.Contains("GUNCELLENDI") ||
                normalized.Contains("EKLENDI"))
            {
                return ModernMessageKind.Success;
            }

            return ModernMessageKind.Info;
        }
    }
}



