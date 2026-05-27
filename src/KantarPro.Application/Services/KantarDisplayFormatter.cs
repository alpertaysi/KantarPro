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
    }
}
