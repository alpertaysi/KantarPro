namespace KantarPro.Domain
{
    public static class KantarSabitleri
    {
        public static class IslemDurumu
        {
            public const string Iceride = "Iceride";
            public const string CikisYapti = "CikisYapti";
            public const string Iptal = "Iptal";
        }

        public static class TartimTipi
        {
            public const string Giris = "Giris";
            public const string Cikis = "Cikis";
            public const string Sonradan = "Sonradan";
        }

        public static class GelisTuru
        {
            public const string Dolu = "Dolu";
            public const string Bos = "Bos";
            public const string Tartimsiz = "Tartimsiz";
        }

        public static class YukDurumu
        {
            public const string Dolu = "Dolu";
            public const string Bos = "Bos";
        }

        public static class KantarDosyasiDurumu
        {
            public const string KarsiTartimBekleniyor = "KarsiTartimBekleniyor";
            public const string Tamamlandi = "Tamamlandi";
            public const string SuresiDoldu = "SuresiDoldu";
        }

        public static class UcretKodu
        {
            public const string GirisCikis = "GIRIS_CIKIS";
            public const string Tartim = "TARTIM";
            public const string Bekleme = "BEKLEME";
        }

        public static class BekleyenTartimDurumu
        {
            public const string Bekliyor = "Bekliyor";
            public const string Tamamlandi = "Tamamlandi";
            public const string Iptal = "Iptal";
            public const string SuresiDoldu = "SuresiDoldu";
        }

        public static class OdemeTuru
        {
            public const string Nakit = "Nakit";
            public const string KrediKarti = "Kredi Kartı";
        }
    }
}
