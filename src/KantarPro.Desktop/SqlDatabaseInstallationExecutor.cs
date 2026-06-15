using System;
using System.Data;
using System.Data.SqlClient;

namespace KantarPro.Desktop
{
    public sealed class SqlDatabaseInstallationExecutor :
        IDatabaseInstallationExecutor, IDisposable
    {
        private readonly SqlConnection _connection;

        public SqlDatabaseInstallationExecutor(StationSettings settings)
        {
            _connection = new SqlConnection(BuildMasterConnectionString(settings, 15));
            _connection.Open();
        }

        public static string BuildMasterConnectionString(
            StationSettings settings,
            int connectTimeoutSeconds)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var builder = new SqlConnectionStringBuilder(
                settings.BuildConnectionString(connectTimeoutSeconds))
            {
                InitialCatalog = "master",
                MultipleActiveResultSets = false
            };

            return builder.ConnectionString;
        }

        public bool HasKantarProCoreSchema(string databaseName)
        {
            const string sql = @"
IF DB_ID(@databaseName) IS NULL
    SELECT CAST(0 AS BIT);
ELSE
BEGIN
    DECLARE @statement NVARCHAR(MAX);
    SET @statement =
        N'SELECT CAST(CASE WHEN EXISTS (
            SELECT 1 FROM ' + QUOTENAME(@databaseName) + N'.sys.tables
            WHERE name IN (
                N''Araclar'', N''Kullanicilar'', N''Ucretler'', N''Islemler'',
                N''Tartimlar'', N''IslemUcretleri'', N''Loglar'',
                N''BekleyenTartimlar'', N''KantarDosyalari'', N''Ayarlar''
            )
        ) THEN 1 ELSE 0 END AS BIT)';
    EXEC sp_executesql @statement;
END";

            using (var command = new SqlCommand(sql, _connection))
            {
                command.Parameters.Add("@databaseName", SqlDbType.NVarChar, 128)
                    .Value = databaseName;
                return Convert.ToBoolean(command.ExecuteScalar());
            }
        }

        public void ExecuteBatch(string batch)
        {
            using (var command = new SqlCommand(batch, _connection))
            {
                command.CommandTimeout = 120;
                command.ExecuteNonQuery();
            }
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
