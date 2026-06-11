using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using KantarPro.Desktop;
using KantarPro.Domain;
using KantarPro.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class KantarFisKaynakResolverTests
    {
        [TestMethod]
        public void SatirinGercekTartimiVar_GercekTartimdaTrueDondurur()
        {
            var selected = new VehicleMovementRow
            {
                Plaka = "34 ABC 123",
                Tartim = "12.500 kg"
            };

            var result = KantarFisKaynakResolver.SatirinGercekTartimiVar(selected);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Resolve_SeciliSatirinGercekTartimiVarsaAyniNesneyiDondurur()
        {
            var selected = new VehicleMovementRow
            {
                Plaka = "34 ABC 123",
                Tartim = "12.500 kg"
            };

            var result = KantarFisKaynakResolver.Resolve(selected, new KantarDosyasi[0]);

            Assert.AreSame(selected, result);
        }

        [TestMethod]
        public void Resolve_TartimsizSatirIcinTekAktifIlkTartimdanYeniSatirOlusturur()
        {
            var ilkGiris = new DateTime(2026, 6, 1, 8, 15, 20);
            var ilkTartimTarihi = new DateTime(2026, 6, 1, 8, 20, 30);
            var islem = new Islem
            {
                IslemId = 41,
                IslemNo = "00041",
                GirisTarihi = ilkGiris
            };
            var arac = new Arac
            {
                Plaka = "34ABC123",
                FirmaAdi = "Ornek Lojistik"
            };
            var ilkTartim = new Tartim
            {
                IslemId = islem.IslemId,
                Islem = islem,
                Arac = arac,
                KantarFisNo = "00117",
                AgirlikKg = 18450m,
                TartimTarihi = ilkTartimTarihi
            };
            var dosya = AktifDosya(arac, ilkTartim);
            var selected = new VehicleMovementRow
            {
                IslemId = 99,
                IslemNo = "00099",
                Plaka = " 34 abc 123 ",
                Tartim = "Tartımsız"
            };

            var result = KantarFisKaynakResolver.Resolve(selected, new[] { dosya });

            Assert.AreNotSame(selected, result);
            Assert.AreEqual(41, result.IslemId);
            Assert.AreEqual("00041", result.IslemNo);
            Assert.AreEqual("00117", result.KantarFisNo);
            Assert.AreEqual("01.06.2026", result.GirisTarihi);
            Assert.AreEqual("08:20:30", result.GirisSaati);
            Assert.AreEqual("01.06.2026", result.IlkTartimTarihi);
            Assert.AreEqual("08:20:30", result.IlkTartimSaati);
            Assert.AreEqual("34ABC123", result.Plaka);
            Assert.AreEqual("Ornek Lojistik", result.FirmaAdi);
            Assert.AreEqual("18.450 kg", result.Tartim);
        }

        [TestMethod]
        public void Resolve_AgirligiSistemKulturundenBagimsizTurkceFormatlar()
        {
            var originalCulture = Thread.CurrentThread.CurrentCulture;
            var originalUiCulture = Thread.CurrentThread.CurrentUICulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
                var candidate = AdayOlustur(1, "34ABC123");
                candidate.IlkTartim.AgirlikKg = 34000m;
                var selected = new VehicleMovementRow { Plaka = "34ABC123", Tartim = "Tartımsız" };

                var result = KantarFisKaynakResolver.Resolve(selected, new[] { candidate });

                Assert.AreEqual("34.000 kg", result.Tartim);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
                Thread.CurrentThread.CurrentUICulture = originalUiCulture;
            }
        }

        [TestMethod]
        public void ResolveVeFormatla_TartimsizIkinciZiyaretteEskiFisNoVeAgirligiKorur()
        {
            var candidate = AdayOlustur(1, "34ABC123");
            candidate.IlkTartim.KantarFisNo = "00117";
            candidate.IlkTartim.AgirlikKg = 34000m;
            var selected = new VehicleMovementRow
            {
                IslemId = 99,
                IslemNo = "00099",
                Plaka = "34 ABC 123",
                Tartim = "Tartimsiz"
            };

            var resolved = KantarFisKaynakResolver.Resolve(selected, new[] { candidate });
            var receipt = KantarFisFormatter.BuildFromRow(resolved);

            StringAssert.Contains(receipt, "00117");
            StringAssert.Contains(receipt, "34.000 Kg");
            StringAssert.Contains(receipt, candidate.IlkTartim.TartimTarihi.ToString("HH:mm:ss"));
        }

        [TestMethod]
        public void Resolve_CanonicalPlakaIleEskiUiPlakasiniDegistirmedenGuncelAdayiSecer()
        {
            var selected = new VehicleMovementRow
            {
                IslemId = 99,
                IslemNo = "00099",
                Plaka = "34 ESKI 123",
                Tartim = "Tartımsız"
            };
            var candidate = AdayOlustur(1, "34 YENI 456");

            var result = KantarFisKaynakResolver.Resolve(
                selected,
                new[] { candidate },
                canonicalPlaka: "34 YENI 456");

            Assert.AreEqual(candidate.IlkTartim.IslemId, result.IslemId);
            Assert.AreEqual("34 YENI 456", result.Plaka);
            Assert.AreEqual("34 ESKI 123", selected.Plaka);
        }

        [TestMethod]
        public void Resolve_EskiTartiminFisNumarasiYoksaYeniNumaraUretmedenHataVerir()
        {
            var candidate = AdayOlustur(1, "34ABC123");
            candidate.IlkTartim.KantarFisNo = "";
            var selected = new VehicleMovementRow
            {
                IslemId = 99,
                IslemNo = "00099",
                KantarFisNo = "",
                Plaka = "34ABC123",
                FirmaAdi = "Seçili Firma",
                Tartim = "Tartımsız"
            };
            var selectedSnapshot = BasitAlanlariAl(selected);
            var dosyaSnapshot = BasitAlanlariAl(candidate);
            var tartimSnapshot = BasitAlanlariAl(candidate.IlkTartim);
            var islemSnapshot = BasitAlanlariAl(candidate.IlkTartim.Islem);
            var aracSnapshot = BasitAlanlariAl(candidate.Arac);

            var exception = BeklenenHata(
                () => KantarFisKaynakResolver.Resolve(selected, new[] { candidate }));

            StringAssert.Contains(exception.Message, "fiş numarası");
            StringAssert.Contains(exception.Message, "yeni numara");
            CollectionAssert.AreEqual(selectedSnapshot, BasitAlanlariAl(selected));
            CollectionAssert.AreEqual(dosyaSnapshot, BasitAlanlariAl(candidate));
            CollectionAssert.AreEqual(tartimSnapshot, BasitAlanlariAl(candidate.IlkTartim));
            CollectionAssert.AreEqual(islemSnapshot, BasitAlanlariAl(candidate.IlkTartim.Islem));
            CollectionAssert.AreEqual(aracSnapshot, BasitAlanlariAl(candidate.Arac));
        }

        [TestMethod]
        public void AktifAdaylariFiltrele_AyniPlakadakiFarkliAracKimlikleriniBirlikteDondurur()
        {
            var first = AdayOlustur(1, "34 ABC 123");
            var second = AdayOlustur(2, "34ABC123");
            var wrongPlate = AdayOlustur(3, "06XYZ987");

            var result = KantarFisKaynakResolver.AktifAdaylariFiltrele(
                    new[] { first, second, wrongPlate }.AsQueryable(),
                    " 34 abc 123 ")
                .ToList();

            Assert.AreEqual(2, result.Count);
            CollectionAssert.AreEquivalent(
                new[] { first.AracId, second.AracId },
                result.Select(x => x.AracId).ToArray());
        }

        [TestMethod]
        public void Resolve_AdayYoksaMevcutTartimsizFisUyarisiVerir()
        {
            var selected = new VehicleMovementRow { Plaka = "34ABC123", Tartim = "Tartimsiz" };

            var exception = BeklenenHata(
                () => KantarFisKaynakResolver.Resolve(selected, new KantarDosyasi[0]));

            Assert.AreEqual(
                "Bu kayıtta kantar tartımı yok. Tartımsız girişler için kantar fişi oluşturulmaz.",
                exception.Message);
        }

        [TestMethod]
        public void Resolve_BirdenFazlaAktifAdayVarsaTahminYurutmez()
        {
            var selected = new VehicleMovementRow { Plaka = "34ABC123", Tartim = "-" };
            var first = AdayOlustur(1, "34ABC123");
            var second = AdayOlustur(2, "34 ABC 123");

            var exception = BeklenenHata(
                () => KantarFisKaynakResolver.Resolve(selected, new[] { first, second }));

            StringAssert.Contains(exception.Message, "birden fazla");
            StringAssert.Contains(exception.Message, "kantar");
        }

        [TestMethod]
        public void Resolve_SilinmisIlkIslemAdayOlmaz()
        {
            var selected = new VehicleMovementRow { Plaka = "34ABC123", Tartim = "0" };
            var deleted = AdayOlustur(1, "34ABC123");
            deleted.IlkTartim.Islem.SilindiMi = true;

            var exception = BeklenenHata(
                () => KantarFisKaynakResolver.Resolve(selected, new[] { deleted }));

            Assert.AreEqual(
                "Bu kayıtta kantar tartımı yok. Tartımsız girişler için kantar fişi oluşturulmaz.",
                exception.Message);
        }

        [TestMethod]
        public void Resolve_AracReferansiOlmayanAdayiGecersizSayipStandartUyariVerir()
        {
            var selected = new VehicleMovementRow { Plaka = "34ABC123", Tartim = "Tartımsız" };
            var candidate = AdayOlustur(1, "34ABC123");
            candidate.Arac = null;
            candidate.IlkTartim.Arac = null;
            candidate.IlkTartim.Islem.Arac = null;

            var exception = BeklenenHata(
                () => KantarFisKaynakResolver.Resolve(selected, new[] { candidate }));

            Assert.AreEqual(
                "Bu kayıtta kantar tartımı yok. Tartımsız girişler için kantar fişi oluşturulmaz.",
                exception.Message);
        }

        [TestMethod]
        public void Resolve_YanlisPlakaliAdayiSecmez()
        {
            var selected = new VehicleMovementRow { Plaka = "34ABC123", Tartim = "Tartımsız" };
            var wrongPlate = AdayOlustur(1, "06XYZ987");

            var exception = BeklenenHata(
                () => KantarFisKaynakResolver.Resolve(selected, new[] { wrongPlate }));

            Assert.AreEqual(
                "Bu kayıtta kantar tartımı yok. Tartımsız girişler için kantar fişi oluşturulmaz.",
                exception.Message);
        }

        [TestMethod]
        public void Resolve_KarsiTartimBekleniyorDisindakiAdayiSecmez()
        {
            var selected = new VehicleMovementRow { Plaka = "34ABC123", Tartim = "Tartımsız" };
            var completed = AdayOlustur(1, "34ABC123");
            completed.Durum = KantarSabitleri.KantarDosyasiDurumu.Tamamlandi;

            var exception = BeklenenHata(
                () => KantarFisKaynakResolver.Resolve(selected, new[] { completed }));

            Assert.AreEqual(
                "Bu kayıtta kantar tartımı yok. Tartımsız girişler için kantar fişi oluşturulmaz.",
                exception.Message);
        }

        [TestMethod]
        public void Resolve_TartimsizIfadeleriGercekTartimSaymaz()
        {
            var tartimsizDegerler = new[]
            {
                "Tartımsız",
                "tartımsız giriş",
                "Tartimsiz",
                "tartimsiz giris",
                "Tartım yok",
                "Tartim yok",
                "0",
                "-"
            };

            foreach (var tartim in tartimsizDegerler)
            {
                var selected = new VehicleMovementRow { Plaka = "34ABC123", Tartim = tartim };
                var candidate = AdayOlustur(1, "34ABC123");

                var result = KantarFisKaynakResolver.Resolve(selected, new[] { candidate });

                Assert.AreEqual(candidate.IlkTartim.IslemId, result.IslemId, tartim);
            }
        }

        [TestMethod]
        public void Resolve_AyristirilamayanVePozitifOlmayanDegerlerdeBekleyenDosyayaFallbackYapar()
        {
            var gercekOlmayanDegerler = new[] { "--", "bilinmiyor", "-12 kg" };

            foreach (var tartim in gercekOlmayanDegerler)
            {
                var selected = new VehicleMovementRow { Plaka = "34ABC123", Tartim = tartim };
                var candidate = AdayOlustur(1, "34ABC123");

                var result = KantarFisKaynakResolver.Resolve(selected, new[] { candidate });

                Assert.AreEqual(candidate.IlkTartim.IslemId, result.IslemId, tartim);
            }
        }

        [TestMethod]
        public void Resolve_TrTrVeInvariantPozitifAgirliklariGercekTartimSayipAyniSatiriDondurur()
        {
            var agirliklar = new[] { "12.500 kg", "12,500 kg" };

            foreach (var agirlik in agirliklar)
            {
                var selected = new VehicleMovementRow { Plaka = "34ABC123", Tartim = agirlik };

                var result = KantarFisKaynakResolver.Resolve(selected, new KantarDosyasi[0]);

                Assert.AreSame(selected, result, agirlik);
            }
        }

        private static KantarDosyasi AdayOlustur(int id, string plaka)
        {
            var arac = new Arac { AracId = id, Plaka = plaka, FirmaAdi = "Firma " + id };
            var islem = new Islem
            {
                IslemId = id,
                IslemNo = id.ToString("00000"),
                GirisTarihi = new DateTime(2026, 6, id, 9, 0, 0),
                Arac = arac,
                AracId = arac.AracId
            };
            var tartim = new Tartim
            {
                TartimId = id,
                IslemId = id,
                Islem = islem,
                AracId = arac.AracId,
                Arac = arac,
                KantarFisNo = (100 + id).ToString("00000"),
                AgirlikKg = 12000m + id,
                TartimTarihi = islem.GirisTarihi.AddMinutes(5)
            };

            return AktifDosya(arac, tartim);
        }

        private static InvalidOperationException BeklenenHata(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException exception)
            {
                return exception;
            }

            Assert.Fail("InvalidOperationException bekleniyordu.");
            return null;
        }

        private static string[] BasitAlanlariAl(object source)
        {
            return source.GetType()
                .GetProperties()
                .Where(x =>
                    x.PropertyType.IsValueType ||
                    x.PropertyType == typeof(string))
                .OrderBy(x => x.Name)
                .Select(x => x.Name + "=" + Convert.ToString(
                    x.GetValue(source, null),
                    CultureInfo.InvariantCulture))
                .ToArray();
        }

        private static KantarDosyasi AktifDosya(Arac arac, Tartim ilkTartim)
        {
            return new KantarDosyasi
            {
                Arac = arac,
                AracId = arac.AracId,
                IlkTartim = ilkTartim,
                IlkTartimId = ilkTartim.TartimId,
                Durum = KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor
            };
        }
    }
}
