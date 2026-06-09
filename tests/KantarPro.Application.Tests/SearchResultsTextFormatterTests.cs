using KantarPro.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class SearchResultsTextFormatterTests
    {
        [TestMethod]
        public void Build_PlakaFirmaTahsilatVeNetBilgisiniYazar()
        {
            var text = SearchResultsTextFormatter.Build(
                new[]
                {
                    new SearchResultRow
                    {
                        SiraNo = 1,
                        IslemNo = "0042",
                        Durum = "Çıkış Yapıldı",
                        Plaka = "16ABC123",
                        Firma = "BURSA LOJISTIK",
                        GirisTarihi = "09.06.2026",
                        GirisSaati = "08:15:00",
                        CikisTarihi = "09.06.2026",
                        CikisSaati = "11:40:00",
                        BirinciTartim = "12000 kg",
                        IkinciTartim = "7000 kg",
                        Net = "5000 kg",
                        TahsilatNo = "THS-42",
                        OdemeTuru = "Nakit",
                        KantarFisNo = "77",
                        Kullanici = "Admin Kullanici",
                        ToplamUcret = "732,00 TL",
                        Notlar = "Deneme"
                    }
                },
                "Gecmis Arama");

            StringAssert.Contains(text, "16ABC123");
            StringAssert.Contains(text, "BURSA LOJISTIK");
            StringAssert.Contains(text, "THS-42");
            StringAssert.Contains(text, "5000 kg");
        }
    }
}
