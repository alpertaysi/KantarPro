using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class DatabaseSchemaScriptTests
    {
        [TestMethod]
        public void FaturaId_BirTahsilatinBirdenFazlaUcretSatirindaKullanilabilir()
        {
            var repositoryRoot = FindRepositoryRoot();
            var createSchema = File.ReadAllText(Path.Combine(repositoryRoot, "database", "001_create_schema.sql"));
            var addFaturaId = File.ReadAllText(Path.Combine(repositoryRoot, "database", "004_add_fatura_id.sql"));
            var repairMigrationPath = Path.Combine(repositoryRoot, "database", "010_fix_fatura_id_index.sql");

            Assert.IsFalse(
                createSchema.Contains("CREATE UNIQUE INDEX UX_IslemUcretleri_FaturaId"),
                "Yeni kurulum semasi FaturaId alanini tekil yapmamali.");
            Assert.IsFalse(
                addFaturaId.Contains("CREATE UNIQUE INDEX UX_IslemUcretleri_FaturaId"),
                "FaturaId gecis scripti ayni tahsilata ait birden fazla ucret satirina izin vermeli.");
            Assert.IsTrue(
                File.Exists(repairMigrationPath),
                "Mevcut kurulumlar icin FaturaId indeksini onaran migration bulunmali.");

            var repairMigration = File.ReadAllText(repairMigrationPath);
            StringAssert.Contains(repairMigration, "DROP INDEX UX_IslemUcretleri_FaturaId");
            StringAssert.Contains(repairMigration, "CREATE INDEX IX_IslemUcretleri_FaturaId");
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "database")) &&
                    Directory.Exists(Path.Combine(directory.FullName, "src")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Proje kok dizini bulunamadi.");
        }
    }
}
