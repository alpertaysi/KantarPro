using System;
using System.IO;
using KantarPro.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class DailyRevenueExcelExporterTests
    {
        [TestMethod]
        public void Export_CsvDosyasiOlustururVeToplamUcretiYazar()
        {
            var path = Path.Combine(Path.GetTempPath(), "kantarpro-gunluk-tahsilat-" + Guid.NewGuid().ToString("N") + ".csv");
            try
            {
                DailyRevenueExcelExporter.Export(
                    path,
                    new[]
                    {
                        new DailyRevenueRow
                        {
                            SiraNo = 1,
                            IslemNo = "0017",
                            IslemTipi = "Tartimsiz",
                            KantarFisNo = "-",
                            OdemeTuru = "Nakit",
                            FirmaAdi = "FARUK",
                            Plaka = "16DENEME1616",
                            CikisTarihi = "07.06.2026",
                            CikisSaati = "16:35:45",
                            GirisCikisUcreti = "366,00 TL",
                            TartimUcreti = "366,00 TL",
                            BeklemeUcreti = "0,00 TL",
                            ToplamUcret = "732,00 TL"
                        }
                    });

                Assert.IsTrue(File.Exists(path));
                var content = File.ReadAllText(path);
                StringAssert.Contains(content, "16DENEME1616");
                StringAssert.Contains(content, "Toplam Ücret");
                StringAssert.Contains(content, "732,00 TL");
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}
