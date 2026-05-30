using System;
using System.Linq;
using KantarPro.Application.Services;
using KantarPro.Application.Tests.Fakes;
using KantarPro.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class UcretAyarlariServisiTests
    {
        [TestMethod]
        public void Guncelle_AyniYilUcretleriniYeniSatirEklemedenGunceller()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new UcretAyarlariServisi(uow);
            var tarih = new DateTime(2026, 5, 26, 15, 30, 0);

            servis.Guncelle(tarih, 500m, 750m, 1000m);

            Assert.AreEqual(3, uow.UcretListesi.Count);
            Assert.AreEqual(3, uow.UcretListesi.Count(x => x.AktifMi));
            Assert.AreEqual(500m, uow.UcretListesi.Single(x => x.AktifMi && x.UcretKodu == KantarSabitleri.UcretKodu.GirisCikis).Tutar);
            Assert.AreEqual(750m, uow.UcretListesi.Single(x => x.AktifMi && x.UcretKodu == KantarSabitleri.UcretKodu.Tartim).Tutar);
            Assert.AreEqual(1000m, uow.UcretListesi.Single(x => x.AktifMi && x.UcretKodu == KantarSabitleri.UcretKodu.Bekleme).Tutar);
            Assert.IsTrue(uow.UcretListesi.All(x => x.GecerlilikBaslangic == tarih));
        }
    }
}
