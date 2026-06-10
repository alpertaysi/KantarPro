using System;
using KantarPro.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class SearchDateCriteriaTests
    {
        [TestMethod]
        public void Create_SadeceBaslangicTarihiVerilirseAltSinirOlusturur()
        {
            var criteria = SearchDateCriteria.Create(new DateTime(2026, 6, 1), null, "Giriş");

            Assert.AreEqual(new DateTime(2026, 6, 1), criteria.StartInclusive);
            Assert.IsFalse(criteria.EndExclusive.HasValue);
        }

        [TestMethod]
        public void Create_SadeceBitisTarihiVerilirseGunSonunuKapsar()
        {
            var criteria = SearchDateCriteria.Create(null, new DateTime(2026, 6, 10), "Çıkış");

            Assert.IsFalse(criteria.StartInclusive.HasValue);
            Assert.AreEqual(new DateTime(2026, 6, 11), criteria.EndExclusive);
        }

        [TestMethod]
        public void Create_BaslangicVeBitisTarihiVerilirseIkiSiniriOlusturur()
        {
            var criteria = SearchDateCriteria.Create(
                new DateTime(2026, 6, 1),
                new DateTime(2026, 6, 10),
                "Giriş");

            Assert.AreEqual(new DateTime(2026, 6, 1), criteria.StartInclusive);
            Assert.AreEqual(new DateTime(2026, 6, 11), criteria.EndExclusive);
        }

        [TestMethod]
        public void Create_BitisBaslangictanOnceyseHataVerir()
        {
            try
            {
                SearchDateCriteria.Create(
                    new DateTime(2026, 6, 10),
                    new DateTime(2026, 6, 1),
                    "Giriş");
                Assert.Fail("Geçersiz tarih aralığı için hata bekleniyordu.");
            }
            catch (InvalidOperationException exception)
            {
                StringAssert.Contains(exception.Message, "başlangıç tarihinden önce");
            }
        }
    }
}
