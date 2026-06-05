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
    }
}
