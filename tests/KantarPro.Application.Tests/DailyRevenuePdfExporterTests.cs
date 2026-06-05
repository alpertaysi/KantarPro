using System;
using System.IO;
using System.Text.RegularExpressions;
using KantarPro.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class DailyRevenuePdfExporterTests
    {
        [TestMethod]
        public void Export_DogrudanPdfDosyasiOlusturur()
        {
            var path = Path.Combine(Path.GetTempPath(), "kantarpro-gunluk-tahsilat-" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                var exporter = new DailyRevenuePdfExporter(
                    new[]
                    {
                        new DailyRevenueRow
                        {
                            SiraNo = 1,
                            IslemNo = "0001",
                            IslemTipi = "Tek Tartim",
                            KantarFisNo = "0001",
                            OdemeTuru = "Nakit",
                            FirmaAdi = "DENEME FIRMA",
                            Plaka = "16TEST001",
                            CikisTarihi = "05.06.2026",
                            CikisSaati = "10:30:00",
                            IlkTartim = "34.000 kg",
                            GirisCikisUcreti = "366,00 TL",
                            TartimUcreti = "366,00 TL",
                            BeklemeUcreti = "0,00 TL",
                            ToplamUcret = "732,00 TL"
                        }
                    },
                    "Gunluk Tahsilat Dokumu",
                    "366,00 TL",
                    "366,00 TL",
                    "0,00 TL",
                    "732,00 TL");

                exporter.Export(path);

                Assert.IsTrue(File.Exists(path));
                using (var stream = File.OpenRead(path))
                {
                    var signature = new byte[4];
                    Assert.AreEqual(4, stream.Read(signature, 0, signature.Length));
                    CollectionAssert.AreEqual(new byte[] { 0x25, 0x50, 0x44, 0x46 }, signature);
                }
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [TestMethod]
        public void Export_TabloKolonlariSayfaDisinaTasmaz()
        {
            var path = Path.Combine(Path.GetTempPath(), "kantarpro-gunluk-tahsilat-fit-" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                var exporter = new DailyRevenuePdfExporter(
                    new[]
                    {
                        new DailyRevenueRow
                        {
                            SiraNo = 1,
                            IslemNo = "9999",
                            IslemTipi = "Dolu-Bos",
                            KantarFisNo = "9999",
                            OdemeTuru = "Kredi Karti",
                            FirmaAdi = "COK UZUN FIRMA ADI ILE DENEME",
                            Plaka = "16UZUN001",
                            CikisTarihi = "05.06.2026",
                            CikisSaati = "16:37:15",
                            IlkTartim = "34.000 kg",
                            GirisCikisUcreti = "366,00 TL",
                            TartimUcreti = "366,00 TL",
                            BeklemeUcreti = "3.264,00 TL",
                            ToplamUcret = "3.996,00 TL"
                        }
                    },
                    "Gunluk Tahsilat Dokumu",
                    "366,00 TL",
                    "366,00 TL",
                    "3.264,00 TL",
                    "3.996,00 TL");

                exporter.Export(path);

                var content = File.ReadAllText(path);
                var matches = Regex.Matches(content, @"(?<x>\d+(?:\.\d+)?) (?<y>\d+(?:\.\d+)?) (?<w>\d+(?:\.\d+)?) (?<h>\d+(?:\.\d+)?) re S");
                Assert.IsTrue(matches.Count > 0);

                foreach (Match match in matches)
                {
                    var x = decimal.Parse(match.Groups["x"].Value, System.Globalization.CultureInfo.InvariantCulture);
                    var w = decimal.Parse(match.Groups["w"].Value, System.Globalization.CultureInfo.InvariantCulture);
                    Assert.IsTrue(x + w <= 814m, "Kolon sayfa disina tasiyor: " + (x + w));
                }
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
