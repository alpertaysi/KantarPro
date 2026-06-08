using System;
using System.Data.SqlClient;
using System.Net.Sockets;
using System.Threading.Tasks;
using KantarPro.Infrastructure.Data;

namespace KantarPro.Desktop
{
    public static class KantarDbContextFactory
    {
        public static KantarDbContext Create()
        {
            var settings = StationSettingsStore.Load();
            return new KantarDbContext(settings.BuildConnectionString());
        }

        public static bool TestConnection()
        {
            return TestConnection(3, 3000, true);
        }

        public static bool TestConnection(int connectTimeoutSeconds, int maxWaitMs)
        {
            return TestConnection(connectTimeoutSeconds, maxWaitMs, true);
        }

        public static bool TestConnection(int connectTimeoutSeconds, int maxWaitMs, bool logError)
        {
            try
            {
                var settings = StationSettingsStore.Load();
                var connectionString = settings.BuildConnectionString(connectTimeoutSeconds);

                if (!ProbeSqlServerEndpoint(connectionString, maxWaitMs))
                {
                    return false;
                }

                using (var context = new KantarDbContext(connectionString))
                {
                    var openTask = Task.Run(() => context.Database.Connection.Open());
                    if (!openTask.Wait(maxWaitMs))
                    {
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                if (logError)
                {
                    App.LogError("SQL baglanti testi", ex);
                }

                return false;
            }
        }

        public static bool IsDatabaseConnectionException(Exception exception)
        {
            if (exception == null)
            {
                return false;
            }

            var aggregate = exception as AggregateException;
            if (aggregate != null)
            {
                foreach (var inner in aggregate.Flatten().InnerExceptions)
                {
                    if (IsDatabaseConnectionException(inner))
                    {
                        return true;
                    }
                }
            }

            if (exception is SqlException || exception is SocketException || exception is TimeoutException)
            {
                return true;
            }

            var message = (exception.Message ?? string.Empty).ToUpperInvariant();
            return message.Contains("UNDERLYING PROVIDER FAILED ON OPEN") ||
                   message.Contains("EXECUTING THE COMMAND DEFINITION") ||
                   message.Contains("SQL NETWORK INTERFACES") ||
                   message.Contains("TCP PROVIDER") ||
                   message.Contains("TRANSPORT-LEVEL ERROR") ||
                   message.Contains("SERVER WAS NOT FOUND") ||
                   message.Contains("SERVER WAS NOT ACCESSIBLE") ||
                   message.Contains("SUNUCU BULUNAMADI") ||
                   message.Contains("SUNUCUYA ERISILEMIYOR") ||
                   message.Contains("SUNUCUYA ERİŞİLEMİYOR") ||
                   IsDatabaseConnectionException(exception.InnerException);
        }

        public static string BuildConnectionLostMessage(string operationName, bool committed)
        {
            if (committed)
            {
                return operationName + " veritabanına kaydedildi; ancak bağlantı koptuğu için ekran yenilenemedi. Bağlantı geri geldiğinde Yenile butonuna basarak kaydı listede görebilirsiniz.";
            }

            return operationName + " tamamlanmadı. SQL bağlantısı kesildiği için işlem veritabanına yazılamadı. Bağlantı geri geldikten sonra işlemi tekrar deneyin.";
        }

        public static string BuildOperationErrorMessage(string operationName, Exception exception)
        {
            if (IsDatabaseConnectionException(exception) || !TestConnection(1, 1200, false))
            {
                return BuildConnectionLostMessage(operationName, false);
            }

            return exception == null ? "İşlem tamamlanamadı." : exception.Message;
        }

        private static bool ProbeSqlServerEndpoint(string connectionString, int timeoutMs)
        {
            string host;
            int port;

            if (!TryGetTcpEndpoint(connectionString, out host, out port))
            {
                return true;
            }

            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(host, port);
                    return connectTask.Wait(timeoutMs) && client.Connected;
                }
            }
            catch (Exception ex)
            {
                App.LogError("SQL TCP yoklama", ex);
                return false;
            }
        }

        private static bool TryGetTcpEndpoint(string connectionString, out string host, out int port)
        {
            host = null;
            port = 1433;

            var builder = new SqlConnectionStringBuilder(connectionString);
            var dataSource = (builder.DataSource ?? string.Empty).Trim();
            if (dataSource.Length == 0)
            {
                return false;
            }

            if (dataSource.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
            {
                dataSource = dataSource.Substring(4);
            }

            var commaIndex = dataSource.LastIndexOf(',');
            if (commaIndex >= 0)
            {
                var portText = dataSource.Substring(commaIndex + 1).Trim();
                dataSource = dataSource.Substring(0, commaIndex).Trim();
                int parsedPort;
                if (int.TryParse(portText, out parsedPort))
                {
                    port = parsedPort;
                }
            }

            if (dataSource.StartsWith(".", StringComparison.Ordinal) ||
                dataSource.StartsWith("(local)", StringComparison.OrdinalIgnoreCase) ||
                dataSource.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) ||
                dataSource.Contains(@"\"))
            {
                return false;
            }

            host = dataSource;
            return host.Length > 0;
        }
    }
}
