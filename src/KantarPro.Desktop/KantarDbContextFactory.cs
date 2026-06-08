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
            return TestConnection(3, 3000);
        }

        public static bool TestConnection(int connectTimeoutSeconds, int maxWaitMs)
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
                App.LogError("SQL baglanti testi", ex);
                return false;
            }
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
