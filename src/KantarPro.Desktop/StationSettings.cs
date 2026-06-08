using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace KantarPro.Desktop
{
    public class StationSettings
    {
        public const string EntryStation = "Giris Kantari";
        public const string ExitStation = "Cikis Kantari";
        public const string ReceiptPrintModeOki = "OKI Sürekli Form";
        public const string ReceiptPrintModeLaserA5 = "Lazer A5";

        public string SqlServerAddress { get; set; }
        public string DatabaseName { get; set; }
        public bool UseWindowsAuthentication { get; set; }
        public string SqlUsername { get; set; }
        public string SqlPassword { get; set; }
        public string StationType { get; set; }
        public string ComPort { get; set; }
        public string ReceiptPrintMode { get; set; }

        public static StationSettings CreateDefault()
        {
            return new StationSettings
            {
                SqlServerAddress = @".\SQLEXPRESS",
                DatabaseName = "KantarPro",
                UseWindowsAuthentication = true,
                SqlUsername = "sa",
                SqlPassword = "",
                StationType = EntryStation,
                ComPort = "",
                ReceiptPrintMode = ReceiptPrintModeOki
            };
        }

        public string BuildConnectionString(int connectTimeoutSeconds = 3)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = string.IsNullOrWhiteSpace(SqlServerAddress) ? @".\SQLEXPRESS" : SqlServerAddress.Trim(),
                InitialCatalog = string.IsNullOrWhiteSpace(DatabaseName) ? "KantarPro" : DatabaseName.Trim(),
                MultipleActiveResultSets = true,
                IntegratedSecurity = UseWindowsAuthentication,
                ConnectTimeout = connectTimeoutSeconds
            };

            if (!UseWindowsAuthentication)
            {
                builder.UserID = (SqlUsername ?? string.Empty).Trim();
                builder.Password = SqlPassword ?? string.Empty;
            }

            return builder.ConnectionString;
        }
    }

    public static class StationSettingsStore
    {
        private const string ProtectedPrefix = "dpapi:";

        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KantarPro");

        private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "station-settings.ini");

        public static StationSettings Load()
        {
            var settings = StationSettings.CreateDefault();
            if (!File.Exists(SettingsPath))
            {
                return settings;
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(SettingsPath, Encoding.UTF8))
            {
                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                values[line.Substring(0, separatorIndex).Trim()] = line.Substring(separatorIndex + 1);
            }

            settings.SqlServerAddress = Get(values, "SqlServerAddress", settings.SqlServerAddress);
            settings.DatabaseName = Get(values, "DatabaseName", settings.DatabaseName);
            settings.UseWindowsAuthentication = Get(values, "UseWindowsAuthentication", "True").Equals("True", StringComparison.OrdinalIgnoreCase);
            settings.SqlUsername = Get(values, "SqlUsername", settings.SqlUsername);
            settings.SqlPassword = Decode(Get(values, "SqlPassword", ""));
            settings.StationType = Get(values, "StationType", settings.StationType);
            settings.ComPort = Get(values, "ComPort", settings.ComPort);
            settings.ReceiptPrintMode = Get(values, "ReceiptPrintMode", settings.ReceiptPrintMode);
            return settings;
        }

        public static void Save(StationSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            Directory.CreateDirectory(SettingsDirectory);
            var lines = new[]
            {
                "SqlServerAddress=" + (settings.SqlServerAddress ?? string.Empty).Trim(),
                "DatabaseName=" + (settings.DatabaseName ?? string.Empty).Trim(),
                "UseWindowsAuthentication=" + settings.UseWindowsAuthentication,
                "SqlUsername=" + (settings.SqlUsername ?? string.Empty).Trim(),
                "SqlPassword=" + Encode(settings.SqlPassword ?? string.Empty),
                "StationType=" + (settings.StationType ?? StationSettings.EntryStation),
                "ComPort=" + (settings.ComPort ?? string.Empty).Trim(),
                "ReceiptPrintMode=" + NormalizeReceiptPrintMode(settings.ReceiptPrintMode)
            };
            File.WriteAllLines(SettingsPath, lines, Encoding.UTF8);
        }

        public static string GetSettingsPath()
        {
            return SettingsPath;
        }

        private static string Get(IDictionary<string, string> values, string key, string defaultValue)
        {
            string value;
            return values.TryGetValue(key, out value) ? value : defaultValue;
        }

        private static string NormalizeReceiptPrintMode(string value)
        {
            return string.Equals(value, StationSettings.ReceiptPrintModeLaserA5, StringComparison.OrdinalIgnoreCase)
                ? StationSettings.ReceiptPrintModeLaserA5
                : StationSettings.ReceiptPrintModeOki;
        }

        private static string Encode(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return ProtectedPrefix + Convert.ToBase64String(protectedBytes);
        }

        private static string Decode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            try
            {
                if (value.StartsWith(ProtectedPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var protectedBytes = Convert.FromBase64String(value.Substring(ProtectedPrefix.Length));
                    var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(bytes);
                }

                // Eski ayar dosyalarinda sifre Base64 olarak saklaniyordu; geriye donuk okuma icin korunur.
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}



