using KantarPro.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class KantarSerialReaderTests
    {
        [TestMethod]
        public void TryParseWeight_BaykonFrameIkinciAlandakiAgirligiOkur()
        {
            decimal agirlik;

            var parsed = KantarSerialReader.TryParseWeight("4\u0002qr0     12560     0\r\n", out agirlik);

            Assert.IsTrue(parsed);
            Assert.AreEqual(12560m, agirlik);
        }

        [TestMethod]
        public void TryParseWeight_CerceveIcindeSonGecerliSatiriOkur()
        {
            decimal agirlik;

            var parsed = KantarSerialReader.TryParseWeight("\u0002IA\u0002?j\n4\u0002qr0     0     0\r\n4\u0002qp0     34220     0\r\n", out agirlik);

            Assert.IsTrue(parsed);
            Assert.AreEqual(34220m, agirlik);
        }

        [TestMethod]
        public void TryParseWeight_BaykonFrameDurumAlaniDoluOlsaBileIkinciAlaniOkur()
        {
            decimal agirlik;

            var parsed = KantarSerialReader.TryParseWeight("\u0002qp6    18420     0\r\n", out agirlik);

            Assert.IsTrue(parsed);
            Assert.AreEqual(18420m, agirlik);
        }

        [TestMethod]
        public void TryParseWeight_BaykonFrameHareketliDurumHarfiniDeOkur()
        {
            decimal agirlik;

            var parsed = KantarSerialReader.TryParseWeight("\u0002qg0    16480     0\r\n", out agirlik);

            Assert.IsTrue(parsed);
            Assert.AreEqual(16480m, agirlik);
        }

        [TestMethod]
        public void TryParseWeight_BaykonFrameTumAlanlarSifirsaSifirOkur()
        {
            decimal agirlik;

            var parsed = KantarSerialReader.TryParseWeight("\u0002qr0     0     0\r\n", out agirlik);

            Assert.IsTrue(parsed);
            Assert.AreEqual(0m, agirlik);
        }

        [TestMethod]
        public void SortPortNames_ComNumarasinaGoreSiralayipBosDegerleriAtar()
        {
            var ports = KantarSerialReader.SortPortNames(new[] { "COM10", "", "COM2", null, "COM1" }).ToArray();

            CollectionAssert.AreEqual(new[] { "COM1", "COM2", "COM10" }, ports);
        }
    }
}
