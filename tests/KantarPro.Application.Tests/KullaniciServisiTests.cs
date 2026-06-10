using System;
using System.Linq;
using KantarPro.Application.Services;
using KantarPro.Application.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class KullaniciServisiTests
    {
        [TestMethod]
        public void VarsayilanKullanicilariOlustur_TabloBossa_AdminVeMemurEkler()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new KullaniciServisi(uow);

            servis.VarsayilanKullanicilariOlustur();

            Assert.AreEqual(2, uow.KullaniciListesi.Count);
            Assert.IsTrue(uow.KullaniciListesi.Any(x => x.KullaniciAdi == "admin" && x.Rol == KullaniciRolleri.Admin));
            Assert.IsTrue(uow.KullaniciListesi.Any(x => x.KullaniciAdi == "memur" && x.Rol == KullaniciRolleri.Memur));
        }

        [TestMethod]
        public void VarsayilanKullanicilariOlustur_TabloBosDegilseSilinenMemuruGeriGetirmez()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new KullaniciServisi(uow);
            servis.VarsayilanKullanicilariOlustur();
            var memur = uow.KullaniciListesi.Single(x => x.KullaniciAdi == "memur");
            uow.KullaniciListesi.Remove(memur);

            servis.VarsayilanKullanicilariOlustur();

            Assert.IsFalse(uow.KullaniciListesi.Any(x => x.KullaniciAdi == "memur"));
        }

        [TestMethod]
        public void GirisYap_DogruSifreyle_OturumDondurur()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new KullaniciServisi(uow);
            servis.VarsayilanKullanicilariOlustur();

            var oturum = servis.GirisYap("admin", "admin");

            Assert.AreEqual("admin", oturum.KullaniciAdi);
            Assert.IsTrue(oturum.AdminMi);
            Assert.IsTrue(uow.KullaniciListesi.Single(x => x.KullaniciAdi == "admin").SonGirisTarihi.HasValue);
        }

        [TestMethod]
        public void GirisYap_YanlisSifreyi_Reddeder()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new KullaniciServisi(uow);
            servis.VarsayilanKullanicilariOlustur();

            try
            {
                servis.GirisYap("admin", "yanlis");
                Assert.Fail("Yanlis sifre kabul edilmemeliydi.");
            }
            catch (InvalidOperationException)
            {
            }
        }

        [TestMethod]
        public void ParolaDegistir_MevcutSifreDogruysa_YeniSifreyleGirisYapar()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new KullaniciServisi(uow);
            servis.VarsayilanKullanicilariOlustur();
            AssignKullaniciIds(uow);
            var admin = servis.GirisYap("admin", "admin");

            servis.ParolaDegistir(admin.KullaniciId, "admin", "YeniSifre123");

            var oturum = servis.GirisYap("admin", "YeniSifre123");
            Assert.AreEqual("admin", oturum.KullaniciAdi);
            Assert.IsFalse(KullaniciServisi.VerifyPassword("admin", uow.KullaniciListesi.Single(x => x.KullaniciAdi == "admin").ParolaHash));
        }

        [TestMethod]
        public void ParolaDegistir_MevcutSifreYanlissa_Reddeder()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new KullaniciServisi(uow);
            servis.VarsayilanKullanicilariOlustur();
            AssignKullaniciIds(uow);
            var admin = servis.GirisYap("admin", "admin");

            try
            {
                servis.ParolaDegistir(admin.KullaniciId, "yanlis", "YeniSifre123");
                Assert.Fail("Yanlis mevcut sifre ile parola degismemeliydi.");
            }
            catch (InvalidOperationException)
            {
            }

            var oturum = servis.GirisYap("admin", "admin");
            Assert.AreEqual("admin", oturum.KullaniciAdi);
        }

        [TestMethod]
        public void KullaniciEkle_YeniMemurEklerVeGirisYapabilir()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new KullaniciServisi(uow);
            servis.VarsayilanKullanicilariOlustur();

            var kullanici = servis.KullaniciEkle("giris_memuru", "Giris Memuru", KullaniciRolleri.Memur, "Memur123");

            Assert.AreEqual("giris_memuru", kullanici.KullaniciAdi);
            Assert.AreEqual(KullaniciRolleri.Memur, kullanici.Rol);
            Assert.IsTrue(kullanici.AktifMi);

            var oturum = servis.GirisYap("giris_memuru", "Memur123");
            Assert.AreEqual("Giris Memuru", oturum.AdSoyad);
            Assert.IsFalse(oturum.AdminMi);
        }

        [TestMethod]
        public void KullaniciEkle_AyniKullaniciAdiniReddeder()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new KullaniciServisi(uow);
            servis.VarsayilanKullanicilariOlustur();

            try
            {
                servis.KullaniciEkle("admin", "Baska Admin", KullaniciRolleri.Admin, "Admin123");
                Assert.Fail("Ayni kullanici adi ikinci kez eklenmemeliydi.");
            }
            catch (InvalidOperationException)
            {
            }
        }

        [TestMethod]
        public void KullaniciEkle_PasifKullaniciAyniAdlaEklenirseYenidenEtkinlestirir()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new KullaniciServisi(uow);
            servis.VarsayilanKullanicilariOlustur();
            AssignKullaniciIds(uow);
            var kullanici = servis.KullaniciEkle(
                "Güvenlik",
                "Eski Güvenlik Personeli",
                KullaniciRolleri.Memur,
                "EskiSifre");
            kullanici.KullaniciId = 3;
            servis.KullaniciSil(kullanici.KullaniciId, 1);

            var yenidenEtkinlesen = servis.KullaniciEkle(
                "Güvenlik",
                "Güvenlik Personeli",
                KullaniciRolleri.Memur,
                "Güvenlik");

            Assert.AreSame(kullanici, yenidenEtkinlesen);
            Assert.AreEqual(3, uow.KullaniciListesi.Count);
            Assert.IsTrue(yenidenEtkinlesen.AktifMi);
            Assert.AreEqual("Güvenlik Personeli", yenidenEtkinlesen.AdSoyad);
            Assert.AreEqual(KullaniciRolleri.Memur, yenidenEtkinlesen.Rol);
            var oturum = servis.GirisYap("Güvenlik", "Güvenlik");
            Assert.AreEqual("Güvenlik Personeli", oturum.AdSoyad);
        }

        [TestMethod]
        public void KullaniciSil_KullaniciyiPasifeAlirVeGirisiniEngeller()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new KullaniciServisi(uow);
            servis.VarsayilanKullanicilariOlustur();
            AssignKullaniciIds(uow);
            var kullanici = servis.KullaniciEkle("vezne", "Vezne Memuru", KullaniciRolleri.Memur, "Vezne123");
            kullanici.KullaniciId = 3;

            servis.KullaniciSil(kullanici.KullaniciId, 1);

            Assert.IsFalse(kullanici.AktifMi);
            try
            {
                servis.GirisYap("vezne", "Vezne123");
                Assert.Fail("Pasife alinan kullanici giris yapamamaliydi.");
            }
            catch (InvalidOperationException)
            {
            }
        }

        [TestMethod]
        public void KullaniciSil_AdminKendisiniSilemez()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new KullaniciServisi(uow);
            servis.VarsayilanKullanicilariOlustur();
            AssignKullaniciIds(uow);

            try
            {
                servis.KullaniciSil(1, 1);
                Assert.Fail("Admin kendi hesabini silememeliydi.");
            }
            catch (InvalidOperationException)
            {
            }
        }

        [TestMethod]
        public void KullanicilariListele_AktifleriAdaGoreDondurur()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new KullaniciServisi(uow);
            servis.VarsayilanKullanicilariOlustur();
            var pasif = servis.KullaniciEkle("pasif", "Pasif Kullanici", KullaniciRolleri.Memur, "Pasif123");
            pasif.AktifMi = false;

            var sonuc = servis.KullanicilariListele(true);

            Assert.AreEqual(2, sonuc.Count);
            Assert.IsTrue(sonuc.All(x => x.AktifMi));
            Assert.AreEqual("admin", sonuc.First().KullaniciAdi);
        }

        private static void AssignKullaniciIds(InMemoryUnitOfWork uow)
        {
            for (var i = 0; i < uow.KullaniciListesi.Count; i++)
            {
                uow.KullaniciListesi[i].KullaniciId = i + 1;
            }
        }
    }
}
