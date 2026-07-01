using KantarPro.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class AutoRefreshPolicyTests
    {
        [TestMethod]
        public void ShouldRefresh_FormdaYaziYazilirkenYenilemez()
        {
            var result = AutoRefreshPolicy.ShouldRefresh(isEditingInput: true, isModalDialogOpen: false);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ShouldRefresh_PencereAcikkenYenilemez()
        {
            var result = AutoRefreshPolicy.ShouldRefresh(isEditingInput: false, isModalDialogOpen: true);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ShouldRefresh_SatirSeciliykenYenilemez()
        {
            var result = AutoRefreshPolicy.ShouldRefresh(isEditingInput: false, isModalDialogOpen: false, hasActiveSelection: true);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ShouldRefresh_IslemYokkenYeniler()
        {
            var result = AutoRefreshPolicy.ShouldRefresh(isEditingInput: false, isModalDialogOpen: false);

            Assert.IsTrue(result);
        }
    }
}
