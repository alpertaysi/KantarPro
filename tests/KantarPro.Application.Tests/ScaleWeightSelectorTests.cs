using System;
using KantarPro.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class ScaleWeightSelectorTests
    {
        [TestMethod]
        public void GetWeight_MemurIcinIndikatorDegeriniKullanir()
        {
            Assert.AreEqual(25000m, ScaleWeightSelector.GetWeight(25000m, "34000", false));
        }

        [TestMethod]
        public void GetWeight_AdminIcinManuelDegeriKullanir()
        {
            Assert.AreEqual(34000m, ScaleWeightSelector.GetWeight(null, "34000", true));
        }

        [TestMethod]
        public void GetWeight_MemurIcinIndikatorYoksaHataVerir()
        {
            try
            {
                ScaleWeightSelector.GetWeight(null, "34000", false);
                Assert.Fail("Geçerli indikatör kilosu yokken hata bekleniyordu.");
            }
            catch (InvalidOperationException exception)
            {
                StringAssert.Contains(exception.Message, "İndikatörden geçerli kilo");
            }
        }
    }
}
