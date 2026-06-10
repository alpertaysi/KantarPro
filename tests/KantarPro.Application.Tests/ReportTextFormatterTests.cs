using System;
using System.Linq;
using KantarPro.Desktop;
using KantarPro.Domain;
using KantarPro.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class ReportTextFormatterTests
    {
        [TestMethod]
        public void DailyRevenueBuild_YatayKlasikTabloyuIstenenKolonSirasiylaYazar()
        {
            var text = DailyRevenueTextFormatter.Build(
                new[]
                {
                    new DailyRevenueRow
                    {
                        SiraNo = 1,
                        IslemNo = "00033",
                        IslemTipi = "Tek Tartim",
                        KantarFisNo = "00011, 00012",
                        OdemeTuru = "Kredi Karti",
                        FirmaAdi = "UZUN FIRMA ADI ILE OKUNAKLILIK DENEMESI",
                        Plaka = "34EFE456",
                        GirisTarihi = "10.06.2026",
                        GirisSaati = "09:00",
                        CikisTarihi = "10.06.2026",
                        CikisSaati = "11:05",
                        GirisCikisUcreti = "366,00 TL",
                        TartimUcreti = "366,00 TL",
                        BeklemeUcreti = "12.894,00 TL",
                        ToplamUcret = "13.626,00 TL"
                    }
                },
                "10.06.2026",
                "366,00 TL",
                "366,00 TL",
                "12.894,00 TL",
                "13.626,00 TL");

            var header = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Single(x => x.Contains("No") && x.Contains("Islem") && x.Contains("Plaka"));

            Assert.IsTrue(header.IndexOf("No", StringComparison.Ordinal) < header.IndexOf("Islem", StringComparison.Ordinal));
            Assert.IsTrue(header.IndexOf("Islem", StringComparison.Ordinal) < header.IndexOf("Plaka", StringComparison.Ordinal));
            Assert.IsTrue(header.IndexOf("Plaka", StringComparison.Ordinal) < header.IndexOf("Firma", StringComparison.Ordinal));
            Assert.IsTrue(header.IndexOf("Cikis Tar", StringComparison.Ordinal) < header.IndexOf("Giris Tar", StringComparison.Ordinal));
            Assert.IsTrue(header.IndexOf("Odeme", StringComparison.Ordinal) < header.IndexOf("Fis", StringComparison.Ordinal));
            Assert.IsTrue(header.IndexOf("G-Cikis", StringComparison.Ordinal) < header.IndexOf("Tartim", StringComparison.Ordinal));
            StringAssert.Contains(text, "10.06.2026");
            StringAssert.Contains(text, "09:00");
            StringAssert.Contains(text, "00011, 00012");
            StringAssert.Contains(text, "12.894,00");
            StringAssert.Contains(text, "13.626,00");
            StringAssert.Contains(header, "|");
            Assert.IsTrue(text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).All(x => x.Length <= 127));
        }

        [TestMethod]
        public void SearchBuild_GunlukTahsilatlaAyniYatayKolonDuzeniniKullanir()
        {
            var text = SearchResultsTextFormatter.Build(
                new[]
                {
                    new SearchResultRow
                    {
                        SiraNo = 1,
                        IslemNo = "00033",
                        Durum = "Cikis Yapildi",
                        Plaka = "34EFE456",
                        Firma = "UZUN FIRMA ADI ILE ARASTIRMA DENEMESI",
                        GirisTarihi = "10.06.2026",
                        GirisSaati = "09:00:00",
                        CikisTarihi = "10.06.2026",
                        CikisSaati = "11:05:00",
                        BirinciTartim = "22000 kg",
                        IkinciTartim = "9000 kg",
                        Net = "13000 kg",
                        TahsilatNo = "00033",
                        OdemeTuru = "Nakit",
                        KantarFisNo = "00011, 00012",
                        Kullanici = "Murat Tasdemir",
                        ToplamUcret = "732,00 TL",
                        GirisCikisUcreti = "366,00 TL",
                        TartimUcreti = "366,00 TL",
                        BeklemeUcreti = "0,00 TL",
                        Notlar = "Arastirma deneme kaydi"
                    }
                },
                "Arastirma Dokumu");

            var header = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Single(x => x.Contains("No") && x.Contains("Islem") && x.Contains("Plaka"));

            Assert.IsTrue(header.IndexOf("Cikis Tar", StringComparison.Ordinal) < header.IndexOf("Giris Tar", StringComparison.Ordinal));
            StringAssert.Contains(text, "34EFE456");
            StringAssert.Contains(text, "00011, 00012");
            StringAssert.Contains(text, "366,00");
            Assert.IsTrue(text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).All(x => x.Length <= 127));
        }

        [TestMethod]
        public void DotMatrixLayout_OnIkiCpiIcinOnPuntoVeKesimsizUzunSayfaHesaplar()
        {
            Assert.AreEqual(10.0f, DotMatrixReportLayout.FontPointSizeForCpi(12), 0.01f);
            Assert.IsTrue(DotMatrixReportLayout.PageLengthHundredthsForLines(120) >= 2000);
        }

        [TestMethod]
        public void DotMatrixLayout_KantarFisindeEnFazlaBirSatirYukariOfseteIzinVerir()
        {
            Assert.AreEqual(-12.0f, DotMatrixReportLayout.ReceiptTopOffset(12.0f, -1), 0.01f);
            Assert.AreEqual(-12.0f, DotMatrixReportLayout.ReceiptTopOffset(12.0f, -5), 0.01f);
            Assert.AreEqual(240.0f, DotMatrixReportLayout.ReceiptTopOffset(12.0f, 25), 0.01f);
        }

        [TestMethod]
        public void SearchReceiptRowBuilder_SecilenIsleminDoluBosFisBilgileriniHazirlar()
        {
            var arac = new Arac { AracId = 7, Plaka = "16TEST001", FirmaAdi = "DENEME" };
            var ilkIslem = new Islem { IslemId = 10, IslemNo = "00010", Arac = arac, GirisTarihi = new DateTime(2026, 6, 1, 9, 15, 0) };
            var ikinciIslem = new Islem { IslemId = 20, IslemNo = "00020", Arac = arac, GirisTarihi = new DateTime(2026, 6, 3, 14, 30, 0) };
            var ilkTartim = new Tartim
            {
                TartimId = 101,
                IslemId = ilkIslem.IslemId,
                Islem = ilkIslem,
                TartimTipi = KantarSabitleri.TartimTipi.Giris,
                TartimTarihi = ilkIslem.GirisTarihi,
                AgirlikKg = 34000m,
                KantarFisNo = "00011"
            };
            var ikinciTartim = new Tartim
            {
                TartimId = 102,
                IslemId = ikinciIslem.IslemId,
                Islem = ikinciIslem,
                TartimTipi = KantarSabitleri.TartimTipi.Giris,
                TartimTarihi = ikinciIslem.GirisTarihi,
                AgirlikKg = 8900m,
                KantarFisNo = "00012"
            };
            ikinciIslem.Tartimlar.Add(ikinciTartim);
            var dosya = new KantarDosyasi
            {
                IlkTartim = ilkTartim,
                KarsiTartim = ikinciTartim,
                NetAgirlikKg = 25100m,
                Durum = KantarSabitleri.KantarDosyasiDurumu.Tamamlandi
            };

            var row = SearchReceiptRowBuilder.Build(ikinciIslem, dosya);

            Assert.AreEqual(20, row.IslemId);
            Assert.AreEqual("00012", row.KantarFisNo);
            Assert.AreEqual("01.06.2026", row.GirisTarihi);
            Assert.AreEqual("03.06.2026", row.BosGelisTarihi);
            Assert.AreEqual("34.000 kg", row.Tartim);
            Assert.AreEqual("8.900 kg", row.IkinciTartim);
            Assert.AreEqual("25.100 kg", row.NetAgirlik);
        }
    }
}
