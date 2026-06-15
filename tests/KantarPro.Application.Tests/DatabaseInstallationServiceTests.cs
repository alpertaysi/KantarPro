using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KantarPro.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class DatabaseInstallationServiceTests
    {
        private readonly List<string> _temporaryDirectories = new List<string>();

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var directory in _temporaryDirectories)
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [TestMethod]
        public void GetInstallationScripts_Siralayip003DemoScriptiniAtlar()
        {
            var directory = CreateTempDirectory(
                "010_fix.sql",
                "003_seed_random_demo_data.sql",
                "001_create_schema.sql",
                "000_create_migration_history.sql",
                "not-a-script.txt");

            var scripts = DatabaseInstallationService.GetInstallationScripts(directory)
                .Select(Path.GetFileName)
                .ToArray();

            CollectionAssert.AreEqual(
                new[]
                {
                    "000_create_migration_history.sql",
                    "001_create_schema.sql",
                    "010_fix.sql"
                },
                scripts);
        }

        [TestMethod]
        public void SplitSqlBatches_TekBasinaGoSatirlariniAyirir()
        {
            var sql = "SELECT 1;\r\nGO\r\nSELECT 'GO metin icinde';\r\ngo -- batch\r\nSELECT 3;";

            var batches = DatabaseInstallationService.SplitSqlBatches(sql).ToArray();

            Assert.AreEqual(3, batches.Length);
            StringAssert.Contains(batches[0], "SELECT 1");
            StringAssert.Contains(batches[1], "'GO metin icinde'");
            StringAssert.Contains(batches[2], "SELECT 3");
        }

        [TestMethod]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void GetInstallationScripts_KlasorYoksaAnlasilirHataVerir()
        {
            DatabaseInstallationService.GetInstallationScripts(
                Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        }

        private string CreateTempDirectory(params string[] fileNames)
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "KantarProDatabaseInstallationTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            _temporaryDirectories.Add(directory);

            foreach (var fileName in fileNames)
            {
                File.WriteAllText(Path.Combine(directory, fileName), "SELECT 1;");
            }

            return directory;
        }
    }
}
