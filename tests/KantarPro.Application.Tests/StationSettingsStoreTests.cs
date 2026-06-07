using System.Reflection;
using KantarPro.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class StationSettingsStoreTests
    {
        [TestMethod]
        public void SqlSifresi_DpapiIleSifrelenirVeGeriCozulur()
        {
            var encode = typeof(StationSettingsStore).GetMethod("Encode", BindingFlags.NonPublic | BindingFlags.Static);
            var decode = typeof(StationSettingsStore).GetMethod("Decode", BindingFlags.NonPublic | BindingFlags.Static);

            var plainText = "KantarPro2026!";
            var encrypted = (string)encode.Invoke(null, new object[] { plainText });
            var decrypted = (string)decode.Invoke(null, new object[] { encrypted });

            Assert.IsTrue(encrypted.StartsWith("dpapi:"));
            Assert.IsFalse(encrypted.Contains(plainText));
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void EskiBase64Sifreleri_GeriyeDonukOkur()
        {
            var decode = typeof(StationSettingsStore).GetMethod("Decode", BindingFlags.NonPublic | BindingFlags.Static);
            var oldValue = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("eski-sifre"));

            var decoded = (string)decode.Invoke(null, new object[] { oldValue });

            Assert.AreEqual("eski-sifre", decoded);
        }
    }
}
