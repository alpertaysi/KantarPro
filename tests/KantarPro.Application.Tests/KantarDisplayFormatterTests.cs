using KantarPro.Application.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class KantarDisplayFormatterTests
    {
        [TestMethod]
        public void FormatNetAgirlik_TartimsizCikisIcinTartimsizYazar()
        {
            var sonuc = KantarDisplayFormatter.FormatNetAgirlik(true, null, null);

            Assert.AreEqual("Tartimsiz", sonuc);
        }

        [TestMethod]
        public void IsTartimsizMovement_TartimsizGirisDurumunuYakalaydi()
        {
            var sonuc = KantarDisplayFormatter.IsTartimsizMovement("Tartimsiz giris", "", "");

            Assert.IsTrue(sonuc);
        }
    }
}
