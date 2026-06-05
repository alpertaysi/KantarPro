using System;

namespace KantarPro.Application.Services
{
    public static class KantarDisplayFormatter
    {
        public static string FormatNetAgirlik(bool tartimsizCikis, decimal? ilkAgirlikKg, decimal? ikinciAgirlikKg)
        {
            if (!ilkAgirlikKg.HasValue || !ikinciAgirlikKg.HasValue)
            {
                return tartimsizCikis ? "Tartimsiz" : "";
            }

            return Math.Abs(ilkAgirlikKg.Value - ikinciAgirlikKg.Value).ToString("N0") + " kg";
        }

        public static bool IsTartimsizMovement(string durum, string tartim, string netAgirlik)
        {
            return string.Equals(durum, "Tartimsiz giris", StringComparison.OrdinalIgnoreCase)
                || string.Equals(durum, "Tartimsiz cikis", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tartim, "Tartimsiz", StringComparison.OrdinalIgnoreCase)
                || string.Equals(netAgirlik, "Tartimsiz", StringComparison.OrdinalIgnoreCase);
        }
    }
}
