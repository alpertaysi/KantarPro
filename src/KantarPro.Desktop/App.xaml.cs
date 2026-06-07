namespace KantarPro.Desktop
{
    public partial class App : System.Windows.Application
    {
        private static readonly object LogSyncRoot = new object();
        private static readonly string LogDirectory = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "KantarPro",
            "logs");

        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);
            EnsureLogDirectory();

            System.AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            LogInfo("Uygulama başlatıldı.");
        }

        public static void LogInfo(string message)
        {
            WriteLog("INFO", message, null);
        }

        public static void LogError(string source, System.Exception exception)
        {
            WriteLog(source, exception == null ? "Bilinmeyen hata" : exception.Message, exception);
        }

        public static void LogOperation(string userName, string operation, string detail)
        {
            var message = "[" + Safe(userName) + "] " + Safe(operation) + ": " + Safe(detail);
            WriteLog("OPERATION", message, null);
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as System.Exception;
            LogError("AppDomain UnhandledException", exception);

            if (e.IsTerminating)
            {
                System.Windows.MessageBox.Show(
                    "Kritik bir hata oluştu ve program kapatılacak.\n\nHata: " + (exception == null ? "Bilinmeyen hata" : exception.Message) + "\n\nDetaylar log dosyasına yazıldı.",
                    "Kritik Hata",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogError("Dispatcher UnhandledException", e.Exception);
            System.Windows.MessageBox.Show(
                "Beklenmeyen bir hata oluştu:\n\n" + e.Exception.Message + "\n\nDetaylar log dosyasına yazıldı.",
                "Hata",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            e.Handled = true;
        }

        private void TaskScheduler_UnobservedTaskException(object sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            LogError("Task UnobservedTaskException", e.Exception);
            e.SetObserved();
        }

        private static void WriteLog(string source, string message, System.Exception exception)
        {
            try
            {
                EnsureLogDirectory();
                var logFile = System.IO.Path.Combine(LogDirectory, "kantarpro_" + System.DateTime.Now.ToString("yyyyMMdd") + ".log");
                var builder = new System.Text.StringBuilder();
                builder.Append("[");
                builder.Append(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                builder.Append("] ");
                builder.Append(source);
                builder.Append(": ");
                builder.AppendLine(message ?? string.Empty);

                if (exception != null)
                {
                    builder.AppendLine(exception.ToString());
                }

                lock (LogSyncRoot)
                {
                    System.IO.File.AppendAllText(logFile, builder.ToString(), System.Text.Encoding.UTF8);
                }
            }
            catch
            {
                // Log yazılamazsa uygulama akışını bozma.
            }
        }

        private static void EnsureLogDirectory()
        {
            if (!System.IO.Directory.Exists(LogDirectory))
            {
                System.IO.Directory.CreateDirectory(LogDirectory);
            }
        }

        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }
    }
}
