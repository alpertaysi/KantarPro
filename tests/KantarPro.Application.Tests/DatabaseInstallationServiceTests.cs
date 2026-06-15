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

        [TestMethod]
        public void Install_MevcutKantarProSemasiVarsaHicbirScriptCalistirmaz()
        {
            var directory = CreateTempDirectory("000_create.sql", "001_schema.sql");
            var executor = new FakeInstallationExecutor { HasExistingSchema = true };

            try
            {
                new DatabaseInstallationService().Install(
                    directory, "KantarPro", executor, null);
                Assert.Fail("Mevcut şema için kurulum reddedilmeliydi.");
            }
            catch (InvalidOperationException ex)
            {
                StringAssert.Contains(ex.Message, "zaten");
            }

            Assert.AreEqual(0, executor.ExecutedBatches.Count);
        }

        [TestMethod]
        public void Install_BirScriptHataVerirseSonrakiScriptiCalistirmaz()
        {
            var directory = CreateTempDirectory();
            File.WriteAllText(Path.Combine(directory, "000_create.sql"), "SELECT 'ilk';");
            File.WriteAllText(Path.Combine(directory, "001_schema.sql"), "SELECT 'hata';");
            File.WriteAllText(Path.Combine(directory, "002_after.sql"), "SELECT 'sonraki';");
            File.WriteAllText(
                Path.Combine(directory, "003_seed_random_demo_data.sql"),
                "SELECT 'demo';");
            var executor = new FakeInstallationExecutor { FailWhenBatchContains = "hata" };

            try
            {
                new DatabaseInstallationService().Install(
                    directory, "KantarPro", executor, null);
                Assert.Fail("SQL hatası dışarı aktarılmalıydı.");
            }
            catch (DatabaseInstallationException ex)
            {
                Assert.AreEqual("001_schema.sql", ex.ScriptName);
            }

            Assert.AreEqual(2, executor.ExecutedBatches.Count);
            Assert.IsFalse(executor.ExecutedBatches.Any(x => x.Contains("sonraki")));
            Assert.IsFalse(executor.ExecutedBatches.Any(x => x.Contains("demo")));
        }

        [TestMethod]
        public void Install_BosHedefteScriptleriSiraylaCalistirip003uAtlar()
        {
            var directory = CreateTempDirectory();
            File.WriteAllText(Path.Combine(directory, "002_after.sql"), "SELECT 'son';");
            File.WriteAllText(Path.Combine(directory, "000_create.sql"), "SELECT 'ilk';");
            File.WriteAllText(
                Path.Combine(directory, "003_seed_random_demo_data.sql"),
                "SELECT 'demo';");
            var executor = new FakeInstallationExecutor();

            var result = new DatabaseInstallationService().Install(
                directory, "KantarPro", executor, null);

            CollectionAssert.AreEqual(
                new[] { "SELECT 'ilk';", "SELECT 'son';" },
                executor.ExecutedBatches);
            CollectionAssert.AreEqual(
                new[] { "000_create.sql", "002_after.sql" },
                result.ExecutedScripts.ToArray());
            Assert.IsFalse(executor.ExecutedBatches.Any(x => x.Contains("demo")));
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

        private sealed class FakeInstallationExecutor : IDatabaseInstallationExecutor
        {
            public bool HasExistingSchema { get; set; }
            public string FailWhenBatchContains { get; set; }
            public List<string> ExecutedBatches { get; } = new List<string>();

            public bool HasKantarProCoreSchema(string databaseName)
            {
                return HasExistingSchema;
            }

            public void ExecuteBatch(string batch)
            {
                ExecutedBatches.Add(batch);
                if (!string.IsNullOrWhiteSpace(FailWhenBatchContains) &&
                    batch.Contains(FailWhenBatchContains))
                {
                    throw new InvalidOperationException("Planlı SQL hatası");
                }
            }
        }
    }
}
