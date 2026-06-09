using System;
using System.Linq;
using KantarPro.Application.Services;
using KantarPro.Application.Tests.Fakes;
using KantarPro.Domain;
using KantarPro.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class IslemServisiTests
    {
        [TestMethod]
        public void GirisYap_TartimsizGiris_GirisCikisUcretiTahakkukEder()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new IslemServisi(uow);

            var islem = servis.GirisYap("16 ABC 123", false, null, 1, new DateTime(2026, 5, 10, 10, 0, 0));

            Assert.AreEqual("16ABC123", islem.Arac.Plaka);
            Assert.AreEqual(KantarSabitleri.IslemDurumu.Iceride, islem.Durum);
            Assert.AreEqual(366m, islem.ToplamTahakkuk);
            Assert.AreEqual(1, uow.IslemUcretiListesi.Count);
            Assert.AreEqual(KantarSabitleri.UcretKodu.GirisCikis, uow.IslemUcretiListesi.Single().Ucret.UcretKodu);
        }

        [TestMethod]
        public void GirisYap_TartimliGiris_TartimKaydiVeTartimUcretiEkler()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new IslemServisi(uow);

            var islem = servis.GirisYap("16 KNT 001", true, 18500m, 1, new DateTime(2026, 5, 10, 13, 20, 0));

            Assert.AreEqual(732m, islem.ToplamTahakkuk);
            Assert.AreEqual(1, uow.TartimListesi.Count);
            Assert.AreEqual(18500m, uow.TartimListesi.Single().AgirlikKg);
            Assert.AreEqual("00001", uow.TartimListesi.Single().KantarFisNo);
            Assert.AreEqual(2, uow.IslemUcretiListesi.Count);
            Assert.IsTrue(uow.IslemUcretiListesi.Any(x => x.Ucret.UcretKodu == KantarSabitleri.UcretKodu.GirisCikis));
            Assert.IsTrue(uow.IslemUcretiListesi.Any(x => x.Ucret.UcretKodu == KantarSabitleri.UcretKodu.Tartim));
            Assert.AreEqual(1, uow.BekleyenTartimListesi.Count);
            Assert.AreEqual(KantarSabitleri.BekleyenTartimDurumu.Bekliyor, uow.BekleyenTartimListesi.Single().Durum);
        }

        [TestMethod]
        public void GirisYapVeBekleyenTartimiTamamla_EskiIlkTartimiTamamlar()
        {
            var uow = new InMemoryUnitOfWork();
            var arac = new Arac { AracId = 20, Plaka = "16ABC123", FirmaAdi = "X Firma", AktifMi = true };
            var oncekiIslem = new Islem
            {
                IslemId = 10,
                Arac = arac,
                AracId = arac.AracId,
                Durum = KantarSabitleri.IslemDurumu.CikisYapti,
                GirisTarihi = new DateTime(2026, 5, 1, 9, 0, 0),
                CikisTarihi = new DateTime(2026, 5, 1, 18, 0, 0),
                GirisKullaniciId = 1,
                CikisKullaniciId = 1,
                ToplamTahakkuk = 732m,
                ToplamTahsilat = 732m
            };
            var ilkTartim = new Tartim { TartimId = 30, Islem = oncekiIslem, IslemId = oncekiIslem.IslemId, Arac = arac, AracId = arac.AracId, TartimTipi = KantarSabitleri.TartimTipi.Giris, AgirlikKg = 16500m, TartimTarihi = new DateTime(2026, 5, 1, 9, 0, 0), KullaniciId = 1 };
            var bekleyen = new BekleyenTartim { BekleyenTartimId = 40, Arac = arac, AracId = arac.AracId, IlkTartim = ilkTartim, IlkTartimId = ilkTartim.TartimId, IlkAgirlikKg = ilkTartim.AgirlikKg, IlkTartimTarihi = ilkTartim.TartimTarihi, Durum = KantarSabitleri.BekleyenTartimDurumu.Bekliyor };
            var ilkGirisCikisUcreti = new IslemUcreti { Islem = oncekiIslem, Ucret = uow.UcretListesi[0], UcretAdi = uow.UcretListesi[0].UcretAdi, Tutar = 366m, TahakkukTarihi = oncekiIslem.GirisTarihi, TahsilEdildiMi = true, TahsilTarihi = oncekiIslem.CikisTarihi, TahsilEdenKullaniciId = 1 };
            var ilkTartimUcreti = new IslemUcreti { Islem = oncekiIslem, Ucret = uow.UcretListesi[1], UcretAdi = uow.UcretListesi[1].UcretAdi, Tutar = 366m, TahakkukTarihi = oncekiIslem.GirisTarihi, TahsilEdildiMi = true, TahsilTarihi = oncekiIslem.CikisTarihi, TahsilEdenKullaniciId = 1 };
            oncekiIslem.Tartimlar.Add(ilkTartim);
            oncekiIslem.Ucretler.Add(ilkGirisCikisUcreti);
            oncekiIslem.Ucretler.Add(ilkTartimUcreti);
            uow.AracListesi.Add(arac);
            uow.IslemListesi.Add(oncekiIslem);
            uow.TartimListesi.Add(ilkTartim);
            uow.IslemUcretiListesi.Add(ilkGirisCikisUcreti);
            uow.IslemUcretiListesi.Add(ilkTartimUcreti);
            uow.BekleyenTartimListesi.Add(bekleyen);
            var servis = new IslemServisi(uow);

            var islem = servis.GirisYapVeBekleyenTartimiTamamla("16 ABC 123", "Y Firma", "Bos tartim", 40, 8200m, 1, new DateTime(2026, 5, 11, 10, 0, 0));

            Assert.AreSame(oncekiIslem, islem);
            Assert.AreEqual(1, uow.IslemListesi.Count);
            Assert.AreEqual(KantarSabitleri.IslemDurumu.Iceride, islem.Durum);
            Assert.AreEqual(new DateTime(2026, 5, 11, 10, 0, 0), islem.GirisTarihi);
            Assert.IsNull(islem.CikisTarihi);
            Assert.IsNull(islem.CikisKullaniciId);
            Assert.AreEqual(1464m, islem.ToplamTahakkuk);
            Assert.AreEqual(732m, islem.ToplamTahsilat);
            Assert.AreEqual(4, uow.IslemUcretiListesi.Count);
            Assert.AreEqual(KantarSabitleri.BekleyenTartimDurumu.Tamamlandi, bekleyen.Durum);
            Assert.IsNotNull(bekleyen.TamamlayanTartim);
            Assert.AreEqual(KantarSabitleri.TartimTipi.Sonradan, bekleyen.TamamlayanTartim.TartimTipi);
            Assert.AreEqual(2, islem.Tartimlar.Count);
            Assert.AreEqual(0, uow.BekleyenTartimListesi.Count(x => x.Durum == KantarSabitleri.BekleyenTartimDurumu.Bekliyor));
        }

        [TestMethod]
        public void CikisYap_IkinciTartimSonrasi_CikisYapDeninceKesinCikisaAlir()
        {
            var uow = new InMemoryUnitOfWork();
            var arac = new Arac { AracId = 22, Plaka = "45OLK456", FirmaAdi = "Test Firma", AktifMi = true };
            var islem = new Islem
            {
                IslemId = 11,
                Arac = arac,
                AracId = arac.AracId,
                Durum = KantarSabitleri.IslemDurumu.CikisYapti,
                GirisTarihi = new DateTime(2026, 5, 12, 11, 0, 0),
                CikisTarihi = new DateTime(2026, 5, 12, 19, 30, 0),
                GirisKullaniciId = 1,
                CikisKullaniciId = 1,
                ToplamTahakkuk = 732m,
                ToplamTahsilat = 732m
            };
            var ilkTartim = new Tartim { TartimId = 32, Islem = islem, IslemId = islem.IslemId, Arac = arac, AracId = arac.AracId, TartimTipi = KantarSabitleri.TartimTipi.Giris, AgirlikKg = 27000m, TartimTarihi = islem.GirisTarihi, KullaniciId = 1 };
            var bekleyen = new BekleyenTartim { BekleyenTartimId = 42, Arac = arac, AracId = arac.AracId, IlkTartim = ilkTartim, IlkTartimId = ilkTartim.TartimId, IlkAgirlikKg = ilkTartim.AgirlikKg, IlkTartimTarihi = ilkTartim.TartimTarihi, Durum = KantarSabitleri.BekleyenTartimDurumu.Bekliyor };
            var ilkGirisCikisUcreti = new IslemUcreti { Islem = islem, Ucret = uow.UcretListesi[0], UcretAdi = uow.UcretListesi[0].UcretAdi, Tutar = 366m, TahakkukTarihi = islem.GirisTarihi, TahsilEdildiMi = true, TahsilTarihi = islem.CikisTarihi, TahsilEdenKullaniciId = 1 };
            var ilkTartimUcreti = new IslemUcreti { Islem = islem, Ucret = uow.UcretListesi[1], UcretAdi = uow.UcretListesi[1].UcretAdi, Tutar = 366m, TahakkukTarihi = islem.GirisTarihi, TahsilEdildiMi = true, TahsilTarihi = islem.CikisTarihi, TahsilEdenKullaniciId = 1 };
            islem.Tartimlar.Add(ilkTartim);
            islem.Ucretler.Add(ilkGirisCikisUcreti);
            islem.Ucretler.Add(ilkTartimUcreti);
            uow.AracListesi.Add(arac);
            uow.IslemListesi.Add(islem);
            uow.TartimListesi.Add(ilkTartim);
            uow.IslemUcretiListesi.Add(ilkGirisCikisUcreti);
            uow.IslemUcretiListesi.Add(ilkTartimUcreti);
            uow.BekleyenTartimListesi.Add(bekleyen);
            var servis = new IslemServisi(uow);

            servis.GirisYapVeBekleyenTartimiTamamla("45 OLK 456", "Test Firma", "Ikinci tartim", 42, 12000m, 1, new DateTime(2026, 5, 14, 10, 0, 0));
            var sonuc = servis.CikisYap("45 OLK 456", false, null, 1, new DateTime(2026, 5, 14, 18, 0, 0));

            Assert.AreSame(islem, sonuc);
            Assert.AreEqual(KantarSabitleri.IslemDurumu.CikisYapti, sonuc.Durum);
            Assert.AreEqual(new DateTime(2026, 5, 14, 18, 0, 0), sonuc.CikisTarihi);
            Assert.AreEqual(1464m, sonuc.ToplamTahakkuk);
            Assert.AreEqual(1464m, sonuc.ToplamTahsilat);
            Assert.AreEqual(4, uow.IslemUcretiListesi.Count);
            Assert.IsTrue(islem.Ucretler.All(x => x.TahsilEdildiMi));
        }

        [TestMethod]
        public void IceridekiBekleyenTartimiTamamla_AcikIslemeIkinciTartimEkler()
        {
            var uow = new InMemoryUnitOfWork();
            var arac = new Arac { AracId = 21, Plaka = "79KPT896", AktifMi = true };
            var islem = new Islem { Arac = arac, AracId = arac.AracId, Durum = KantarSabitleri.IslemDurumu.Iceride, GirisTarihi = new DateTime(2026, 5, 11, 9, 0, 0), GirisKullaniciId = 1, ToplamTahakkuk = 732m };
            var ilkTartim = new Tartim { TartimId = 31, Islem = islem, Arac = arac, AracId = arac.AracId, TartimTipi = KantarSabitleri.TartimTipi.Giris, AgirlikKg = 10000m, TartimTarihi = islem.GirisTarihi, KullaniciId = 1 };
            var bekleyen = new BekleyenTartim { BekleyenTartimId = 41, Arac = arac, AracId = arac.AracId, IlkTartim = ilkTartim, IlkTartimId = ilkTartim.TartimId, IlkAgirlikKg = ilkTartim.AgirlikKg, IlkTartimTarihi = ilkTartim.TartimTarihi, Durum = KantarSabitleri.BekleyenTartimDurumu.Bekliyor };
            islem.Tartimlar.Add(ilkTartim);
            uow.AracListesi.Add(arac);
            uow.IslemListesi.Add(islem);
            uow.TartimListesi.Add(ilkTartim);
            uow.BekleyenTartimListesi.Add(bekleyen);
            var servis = new IslemServisi(uow);

            var sonuc = servis.IceridekiBekleyenTartimiTamamla("79 KPT 896", 41, 6200m, 1, new DateTime(2026, 5, 11, 12, 0, 0));

            Assert.AreSame(islem, sonuc);
            Assert.AreEqual(KantarSabitleri.IslemDurumu.Iceride, sonuc.Durum);
            Assert.AreEqual(1098m, sonuc.ToplamTahakkuk);
            Assert.AreEqual(KantarSabitleri.BekleyenTartimDurumu.Tamamlandi, bekleyen.Durum);
            Assert.IsNotNull(bekleyen.TamamlayanTartim);
            Assert.AreEqual(KantarSabitleri.TartimTipi.Sonradan, bekleyen.TamamlayanTartim.TartimTipi);
            Assert.AreEqual(2, sonuc.Tartimlar.Count);
        }

        [TestMethod]
        public void GirisYap_AyniPlakaIcerideyse_IkinciGirisiReddeder()
        {
            var uow = new InMemoryUnitOfWork();
            var arac = new Arac { AracId = 10, Plaka = "16ABC123", AktifMi = true };
            uow.AracListesi.Add(arac);
            uow.IslemListesi.Add(new Islem { Arac = arac, AracId = arac.AracId, Durum = KantarSabitleri.IslemDurumu.Iceride, GirisTarihi = new DateTime(2026, 5, 10), GirisKullaniciId = 1 });
            var servis = new IslemServisi(uow);

            AssertInvalidOperation(() =>
                servis.GirisYap("16 ABC 123", false, null, 1, new DateTime(2026, 5, 10, 11, 0, 0)));
        }

        [TestMethod]
        public void CikisYap_ErtesiGunVeTartimli_TartimVeBeklemeUcretleriEklenir()
        {
            var uow = new InMemoryUnitOfWork();
            var arac = new Arac { AracId = 11, Plaka = "16DEF456", AktifMi = true };
            var islem = new Islem { Arac = arac, AracId = arac.AracId, Durum = KantarSabitleri.IslemDurumu.Iceride, GirisTarihi = new DateTime(2026, 5, 10, 23, 30, 0), GirisKullaniciId = 1, ToplamTahakkuk = 366m };
            uow.AracListesi.Add(arac);
            uow.IslemListesi.Add(islem);
            var servis = new IslemServisi(uow);

            var sonuc = servis.CikisYap("16 DEF 456", true, 12000m, 2, new DateTime(2026, 5, 11, 0, 15, 0));

            Assert.AreEqual(KantarSabitleri.IslemDurumu.CikisYapti, sonuc.Durum);
            Assert.AreEqual(1548m, sonuc.ToplamTahakkuk);
            Assert.AreEqual(1548m, sonuc.ToplamTahsilat);
            Assert.AreEqual(2, uow.IslemUcretiListesi.Count);
            Assert.IsTrue(uow.IslemUcretiListesi.Any(x => x.Ucret.UcretKodu == KantarSabitleri.UcretKodu.Tartim));
            Assert.IsTrue(uow.IslemUcretiListesi.Any(x => x.Ucret.UcretKodu == KantarSabitleri.UcretKodu.Bekleme));
            Assert.IsTrue(uow.IslemUcretiListesi.All(x => x.TahsilEdildiMi));
            Assert.IsTrue(uow.IslemUcretiListesi.All(x => !string.IsNullOrWhiteSpace(x.FaturaId)));
            Assert.AreEqual(1, uow.IslemUcretiListesi.Select(x => x.FaturaId).Distinct().Count());
        }

        [TestMethod]
        public void CikisYap_BesGunSonraCikarsa_BesBeklemeUcretiEkler()
        {
            var uow = new InMemoryUnitOfWork();
            var arac = new Arac { AracId = 12, Plaka = "16DMG087", AktifMi = true };
            var islem = new Islem { Arac = arac, AracId = arac.AracId, Durum = KantarSabitleri.IslemDurumu.Iceride, GirisTarihi = new DateTime(2026, 5, 6, 11, 7, 25), GirisKullaniciId = 1, ToplamTahakkuk = 366m };
            uow.AracListesi.Add(arac);
            uow.IslemListesi.Add(islem);
            var servis = new IslemServisi(uow);

            var sonuc = servis.CikisYap("16 DMG 087", false, null, 2, new DateTime(2026, 5, 11, 13, 41, 46));

            Assert.AreEqual(4446m, sonuc.ToplamTahakkuk);
            Assert.AreEqual(5, uow.IslemUcretiListesi.Count(x => x.Ucret.UcretKodu == KantarSabitleri.UcretKodu.Bekleme));
        }

        [TestMethod]
        public void CikisYap_IkinciTartimYapilirsa_BekleyenIlkTartimiTamamlar()
        {
            var uow = new InMemoryUnitOfWork();
            var arac = new Arac { AracId = 13, Plaka = "16TST001", AktifMi = true };
            var islem = new Islem { Arac = arac, AracId = arac.AracId, Durum = KantarSabitleri.IslemDurumu.Iceride, GirisTarihi = new DateTime(2026, 5, 12, 11, 0, 0), GirisKullaniciId = 1, ToplamTahakkuk = 732m };
            var ilkTartim = new Tartim { TartimId = 70, Islem = islem, Arac = arac, AracId = arac.AracId, TartimTipi = KantarSabitleri.TartimTipi.Giris, AgirlikKg = 20000m, TartimTarihi = islem.GirisTarihi, KullaniciId = 1 };
            var bekleyen = new BekleyenTartim { BekleyenTartimId = 71, Arac = arac, AracId = arac.AracId, IlkTartim = ilkTartim, IlkTartimId = ilkTartim.TartimId, IlkAgirlikKg = ilkTartim.AgirlikKg, IlkTartimTarihi = ilkTartim.TartimTarihi, Durum = KantarSabitleri.BekleyenTartimDurumu.Bekliyor };
            islem.Tartimlar.Add(ilkTartim);
            uow.AracListesi.Add(arac);
            uow.IslemListesi.Add(islem);
            uow.TartimListesi.Add(ilkTartim);
            uow.BekleyenTartimListesi.Add(bekleyen);
            var servis = new IslemServisi(uow);

            var sonuc = servis.CikisYap("16 TST 001", true, 8500m, 2, new DateTime(2026, 5, 12, 19, 30, 0));

            Assert.AreEqual(1098m, sonuc.ToplamTahakkuk);
            Assert.AreEqual(KantarSabitleri.BekleyenTartimDurumu.Tamamlandi, bekleyen.Durum);
            Assert.IsNotNull(bekleyen.TamamlayanTartim);
            Assert.AreEqual(KantarSabitleri.TartimTipi.Cikis, bekleyen.TamamlayanTartim.TartimTipi);
            Assert.AreEqual("00001", bekleyen.TamamlayanTartim.KantarFisNo);
        }

        [TestMethod]
        public void SonradanTartimEkle_TartimsizGirisSonrasi_TartimUcretiVeBekleyenTartimEkler()
        {
            var uow = new InMemoryUnitOfWork();
            var arac = new Arac { AracId = 14, Plaka = "16TST002", AktifMi = true };
            var islem = new Islem { Arac = arac, AracId = arac.AracId, Durum = KantarSabitleri.IslemDurumu.Iceride, GirisTarihi = new DateTime(2026, 5, 12, 11, 0, 0), GirisKullaniciId = 1, ToplamTahakkuk = 366m };
            uow.AracListesi.Add(arac);
            uow.IslemListesi.Add(islem);
            var servis = new IslemServisi(uow);

            var sonuc = servis.SonradanTartimEkle("16 TST 002", 17800m, 1, new DateTime(2026, 5, 12, 12, 10, 0));

            Assert.AreEqual(732m, sonuc.ToplamTahakkuk);
            Assert.AreEqual(1, sonuc.Tartimlar.Count);
            Assert.AreEqual(KantarSabitleri.TartimTipi.Giris, sonuc.Tartimlar.Single().TartimTipi);
            Assert.AreEqual(1, uow.BekleyenTartimListesi.Count);
            Assert.AreEqual(KantarSabitleri.BekleyenTartimDurumu.Bekliyor, uow.BekleyenTartimListesi.Single().Durum);
        }

        [TestMethod]
        public void SonradanTartimEkle_IkinciTartimiOlanAcikIsleme_UcuncuTartimiReddeder()
        {
            var uow = new InMemoryUnitOfWork();
            var arac = new Arac { AracId = 16, Plaka = "16TST004", AktifMi = true };
            var islem = new Islem { Arac = arac, AracId = arac.AracId, Durum = KantarSabitleri.IslemDurumu.Iceride, GirisTarihi = new DateTime(2026, 5, 14, 10, 0, 0), GirisKullaniciId = 1, ToplamTahakkuk = 1464m };
            var ilkTartim = new Tartim { TartimId = 92, Islem = islem, Arac = arac, AracId = arac.AracId, TartimTipi = KantarSabitleri.TartimTipi.Giris, AgirlikKg = 28000m, TartimTarihi = new DateTime(2026, 5, 12, 11, 0, 0), KullaniciId = 1 };
            var ikinciTartim = new Tartim { TartimId = 93, Islem = islem, Arac = arac, AracId = arac.AracId, TartimTipi = KantarSabitleri.TartimTipi.Sonradan, AgirlikKg = 12000m, TartimTarihi = new DateTime(2026, 5, 14, 10, 0, 0), KullaniciId = 1 };
            islem.Tartimlar.Add(ilkTartim);
            islem.Tartimlar.Add(ikinciTartim);
            uow.AracListesi.Add(arac);
            uow.IslemListesi.Add(islem);
            uow.TartimListesi.Add(ilkTartim);
            uow.TartimListesi.Add(ikinciTartim);
            var servis = new IslemServisi(uow);

            AssertInvalidOperation(() =>
                servis.SonradanTartimEkle("16 TST 004", 11000m, 1, new DateTime(2026, 5, 14, 11, 0, 0)));

            Assert.AreEqual(2, islem.Tartimlar.Count);
        }

        [TestMethod]
        public void SuresiDolanBekleyenTartimlariKapat_OnGunuDolduranlariKapatir()
        {
            var uow = new InMemoryUnitOfWork();
            var arac = new Arac { AracId = 15, Plaka = "16TST003", AktifMi = true };
            uow.BekleyenTartimListesi.Add(new BekleyenTartim { BekleyenTartimId = 80, Arac = arac, AracId = arac.AracId, IlkTartimId = 90, IlkAgirlikKg = 19000m, IlkTartimTarihi = new DateTime(2026, 5, 1, 11, 0, 0), Durum = KantarSabitleri.BekleyenTartimDurumu.Bekliyor });
            uow.BekleyenTartimListesi.Add(new BekleyenTartim { BekleyenTartimId = 81, Arac = arac, AracId = arac.AracId, IlkTartimId = 91, IlkAgirlikKg = 18000m, IlkTartimTarihi = new DateTime(2026, 5, 8, 11, 0, 0), Durum = KantarSabitleri.BekleyenTartimDurumu.Bekliyor });
            var servis = new IslemServisi(uow);

            var kapanan = servis.SuresiDolanBekleyenTartimlariKapat(new DateTime(2026, 5, 11, 9, 0, 0), 10);

            Assert.AreEqual(1, kapanan);
            Assert.AreEqual(KantarSabitleri.BekleyenTartimDurumu.SuresiDoldu, uow.BekleyenTartimListesi[0].Durum);
            Assert.AreEqual(KantarSabitleri.BekleyenTartimDurumu.Bekliyor, uow.BekleyenTartimListesi[1].Durum);
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
