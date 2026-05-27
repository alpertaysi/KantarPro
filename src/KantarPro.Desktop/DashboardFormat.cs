using System;
using KantarPro.Domain.Entities;

namespace KantarPro.Desktop
{
    internal static class DashboardFormat
    {
        public static string BosDeger(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }

        public static string Para(decimal tutar)
        {
            return tutar.ToString("N2") + " TL";
        }

        public static string Saat(DateTime tarih)
        {
            if (tarih.Date == DateTime.Today)
            {
                return tarih.ToString("HH:mm");
            }

            return tarih.ToString("dd.MM HH:mm");
        }

        public static string SadeceSaat(DateTime tarih)
        {
            return tarih.ToString("HH:mm");
        }

        public static string SaatSaniyeli(DateTime tarih)
        {
            return tarih.ToString("HH:mm:ss");
        }

        public static string TartimTarihi(Tartim tartim)
        {
            return tartim == null ? "" : tartim.TartimTarihi.ToString("dd.MM.yyyy");
        }

        public static string TartimSaati(Tartim tartim)
        {
            return tartim == null ? "" : tartim.TartimTarihi.ToString("HH:mm:ss");
        }

        public static string SonTartim(Tartim tartim)
        {
            return tartim == null ? "Tartim Yok" : tartim.AgirlikKg.ToString("N0") + " kg";
        }

        public static string TartimDegeri(Tartim tartim)
        {
            return tartim == null ? "" : tartim.AgirlikKg.ToString("N0") + " kg";
        }
    }
}
