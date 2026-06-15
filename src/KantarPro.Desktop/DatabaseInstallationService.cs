using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace KantarPro.Desktop
{
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
                           !name.Equals("003_seed_random_demo_data.sql", StringComparison.OrdinalIgnoreCase);
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
