using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace KantarPro.Desktop
{
    public interface IDatabaseInstallationExecutor
    {
        bool HasKantarProCoreSchema(string databaseName);
        void ExecuteBatch(string batch);
    }

    public sealed class DatabaseInstallationResult
    {
        public IList<string> ExecutedScripts { get; set; }
        public IList<string> SkippedScripts { get; set; }
    }

    public sealed class DatabaseInstallationException : Exception
    {
        public DatabaseInstallationException(string scriptName, Exception innerException)
            : base(
                scriptName + " çalıştırılırken hata oluştu: " + innerException.Message,
                innerException)
        {
            ScriptName = scriptName;
        }

        public string ScriptName { get; private set; }
    }

    public sealed class DatabaseInstallationService
    {
        private static readonly Regex GoLineRegex = new Regex(
            @"^\s*GO\s*(?:--.*)?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static IList<string> GetInstallationScripts(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException(
                    "Veritabanı kurulum scriptleri bulunamadı: " + directory);
            }

            return Directory.GetFiles(directory, "*.sql", SearchOption.TopDirectoryOnly)
                .Where(path =>
                {
                    var name = Path.GetFileName(path);
                    return !name.StartsWith("003", StringComparison.OrdinalIgnoreCase) &&
                           !name.Equals(
                               "003_seed_random_demo_data.sql",
                               StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static IList<string> SplitSqlBatches(string sql)
        {
            var batches = new List<string>();
            var current = new StringBuilder();

            using (var reader = new StringReader(sql ?? string.Empty))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (GoLineRegex.IsMatch(line))
                    {
                        AddBatch(batches, current);
                        continue;
                    }

                    current.AppendLine(line);
                }
            }

            AddBatch(batches, current);
            return batches;
        }

        public DatabaseInstallationResult Install(
            string scriptDirectory,
            string databaseName,
            IDatabaseInstallationExecutor executor,
            Action<string> progress)
        {
            if (!string.Equals(databaseName, "KantarPro", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Kurulum scriptleri yalnızca KantarPro veritabanı adı için hazırlanmıştır.");
            }

            if (executor == null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            if (executor.HasKantarProCoreSchema(databaseName))
            {
                throw new InvalidOperationException(
                    "KantarPro şema tabloları zaten mevcut. Güvenlik nedeniyle kurulum yapılmadı.");
            }

            var scripts = GetInstallationScripts(scriptDirectory);
            var result = new DatabaseInstallationResult
            {
                ExecutedScripts = new List<string>(),
                SkippedScripts = new List<string> { "003_seed_random_demo_data.sql" }
            };

            foreach (var path in scripts)
            {
                var name = Path.GetFileName(path);
                progress?.Invoke(name);

                try
                {
                    foreach (var batch in SplitSqlBatches(File.ReadAllText(path, Encoding.UTF8)))
                    {
                        executor.ExecuteBatch(batch);
                    }

                    result.ExecutedScripts.Add(name);
                }
                catch (Exception ex)
                {
                    throw new DatabaseInstallationException(name, ex);
                }
            }

            return result;
        }

        private static void AddBatch(ICollection<string> batches, StringBuilder current)
        {
            var batch = current.ToString().Trim();
            current.Clear();
            if (batch.Length > 0)
            {
                batches.Add(batch);
            }
        }
    }
}
