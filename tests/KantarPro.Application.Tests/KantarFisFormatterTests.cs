using System;
using KantarPro.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class KantarFisFormatterTests
    {
        [TestMethod]
        public void BuildFromRow_TekTartimFisi_SadeceGirisVeBirinciTartimiYazar()
        {
            var row = new VehicleMovementRow
            {
                IslemNo = "0060",
                Plaka = "34TCL633",
                GirisTarihi = "31.05.2026",
                GirisSaati = "18:20:00",
                CikisTarihi = "02.06.2026",
                CikisSaati = "16:09:00",
                Tartim = "24.500 kg",
                IkinciTartim = "",
                NetAgirlik = "",
                FirmaAdi = "TEST FIRMA",
                Durum = "Dolu Tartim Yapildi"
            };

            var fis = KantarFisFormatter.BuildFromRow(row);

            StringAssert.Contains(fis, "PLAKA NO");
            StringAssert.Contains(fis, "34TCL633");
            StringAssert.Contains(fis, "FIS NO");
            StringAssert.Contains(fis, "0060");
            StringAssert.Contains(fis, "GIRIS TARIHI");
            StringAssert.Contains(fis, "1.TARTI");
            StringAssert.Contains(fis, "MEMUR IMZA");
            Assert.IsFalse(fis.Contains("CIKIS TARIHI"));
            Assert.IsFalse(fis.Contains("MAL CINSI"));
            Assert.IsFalse(fis.Contains("GITTIGI YER"));
            Assert.IsFalse(fis.Contains("GELDIGI YER"));
            Assert.IsFalse(fis.Contains("2.TARTI"));
            Assert.IsFalse(fis.Contains("NET"));
        }

        [TestMethod]
        public void BuildFromRow_DoluBosFisi_IkiGirisIkiTartimVeNetYazar()
        {
            var row = new VehicleMovementRow
            {
                IslemNo = "0061",
                Plaka = "34TCL633",
                GirisTarihi = "31.05.2026",
                GirisSaati = "18:20:00",
                BosGelisTarihi = "02.06.2026",
                BosGelisSaati = "16:09:00",
                Tartim = "24.500 kg",
                IkinciTartim = "9.500 kg",
                NetAgirlik = "15.000 kg"
            };

            var fis = KantarFisFormatter.BuildFromRow(row);

            StringAssert.Contains(fis, "1.GIRIS TARIHI");
            StringAssert.Contains(fis, "2.GIRIS TARIHI");
            StringAssert.Contains(fis, "1.TARTI");
            StringAssert.Contains(fis, "2.TARTI");
            StringAssert.Contains(fis, "NET");
            StringAssert.Contains(fis, "15.000 Kg");
            StringAssert.Contains(fis, "0061");
            Assert.IsFalse(fis.Contains("MAL CINSI"));
            Assert.IsFalse(fis.Contains("GITTIGI YER"));
            Assert.IsFalse(fis.Contains("GELDIGI YER"));
        }

        [TestMethod]
        public void PreviewData_FromVehicleRow_TekTartimAlanlariniHazirlar()
        {
            var row = new VehicleMovementRow
            {
                IslemNo = "0060",
                Plaka = "34TCL633",
                GirisTarihi = "31.05.2026",
                GirisSaati = "18:20:00",
                Tartim = "24.500 kg",
                IkinciTartim = "",
                NetAgirlik = ""
            };
            var rawText = KantarFisFormatter.BuildFromRow(row);

            var data = KantarFisPreviewData.FromVehicleRow(row, rawText);

            Assert.AreEqual("Tek Tartim", data.FisTipi);
            Assert.AreEqual("34TCL633", data.Plaka);
            Assert.AreEqual("0060", data.FisNo);
            Assert.AreEqual("31.05.2026", data.GirisTarihi);
            Assert.AreEqual("18:20:00", data.GirisSaati);
            Assert.AreEqual("24.500 kg", data.BirinciTartim);
            Assert.AreEqual("", data.IkinciTartim);
            Assert.AreEqual("", data.Net);
            Assert.AreEqual(rawText, data.RawText);
        }

        [TestMethod]
        public void PreviewData_FromVehicleRow_DoluBosAlanlariniHazirlar()
        {
            var row = new VehicleMovementRow
            {
                IslemNo = "0061",
                Plaka = "34TCL633",
                GirisTarihi = "31.05.2026",
                GirisSaati = "18:20:00",
                BosGelisTarihi = "02.06.2026",
                BosGelisSaati = "16:09:00",
                Tartim = "24.500 kg",
                IkinciTartim = "9.500 kg",
                NetAgirlik = "15.000 kg"
            };
            var rawText = KantarFisFormatter.BuildFromRow(row);

            var data = KantarFisPreviewData.FromVehicleRow(row, rawText);

            Assert.AreEqual("Dolu-Bos", data.FisTipi);
            Assert.AreEqual("02.06.2026", data.IkinciGirisTarihi);
            Assert.AreEqual("16:09:00", data.IkinciGirisSaati);
            Assert.AreEqual("9.500 kg", data.IkinciTartim);
            Assert.AreEqual("15.000 kg", data.Net);
        }

        [TestMethod]
        public void PreviewData_FromVehicleRow_UzunTeknikIslemNoYerineDortHaneliFisNoKullanir()
        {
            var row = new VehicleMovementRow
            {
                IslemId = 10,
                IslemNo = "ZYR20260604112753000",
                Plaka = "16DUZELT002",
                GirisTarihi = "04.06.2026",
                GirisSaati = "11:27:53",
                Tartim = "34.000 kg"
            };
            var rawText = KantarFisFormatter.BuildFromRow(row);

            var data = KantarFisPreviewData.FromVehicleRow(row, rawText);

            Assert.AreEqual("0010", data.FisNo);
            StringAssert.Contains(rawText, "0010");
            Assert.IsFalse(rawText.Contains("ZYR20260604112753000"));
        }

        [TestMethod]
        public void PreviewData_FromVehicleRow_KantarFisNoVarsaOnuKullanir()
        {
            var row = new VehicleMovementRow
            {
                IslemId = 10,
                IslemNo = "ZYR20260604112753000",
                KantarFisNo = "0003",
                Plaka = "16DUZELT002",
                GirisTarihi = "04.06.2026",
                GirisSaati = "11:27:53",
                Tartim = "34.000 kg"
            };

            var rawText = KantarFisFormatter.BuildFromRow(row);
            var data = KantarFisPreviewData.FromVehicleRow(row, rawText);

            Assert.AreEqual("0003", data.FisNo);
            StringAssert.Contains(rawText, "0003");
            Assert.IsFalse(rawText.Contains("0010"));
            Assert.IsFalse(rawText.Contains("ZYR20260604112753000"));
        }

        [TestMethod]
        public void BuildFromRow_TartimsizKayit_FisOlusturmaz()
        {
            var row = new VehicleMovementRow
            {
                Plaka = "34TCL633",
                GirisTarihi = "31.05.2026",
                GirisSaati = "18:20:00",
                Tartim = "Tartim Yok"
            };

            AssertInvalidOperation(() => KantarFisFormatter.BuildFromRow(row));
        }

        [TestMethod]
        public void BuildFromPendingRow_IkinciTartimBekleyenIcinIlkTartimFisiYazar()
        {
            var row = new PendingWeighingPrototypeRow
            {
                IslemNo = "0062",
                Plaka = "34TCL633",
                IlkGirisTarihi = "31.05.2026",
                IlkGirisSaati = "18:20:00",
                IlkTartimTarihi = "31.05.2026",
                IlkTartimSaati = "18:22:00",
                IlkAgirlik = "24.500"
            };

            var fis = KantarFisFormatter.BuildFromPendingRow(row);

            StringAssert.Contains(fis, "0062");
            StringAssert.Contains(fis, "34TCL633");
            StringAssert.Contains(fis, "1.TARTI");
            StringAssert.Contains(fis, "24.500 Kg");
            Assert.IsFalse(fis.Contains("2.TARTI"));
            Assert.IsFalse(fis.Contains("NET"));
        }

        private static void AssertInvalidOperation(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            Assert.Fail("InvalidOperationException bekleniyordu.");
        }
    }
}
