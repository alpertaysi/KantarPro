using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KantarPro.Domain.Entities;

namespace KantarPro.Application.Services
{
    internal static class KantarFisNoUretici
    {
        public static string GarantiEt(Tartim tartim, IEnumerable<Tartim> tumTartimlar)
        {
            if (tartim == null)
            {
                return null;
            }

            var mevcutFisNo = (tartim.KantarFisNo ?? string.Empty).Trim();
            int mevcutNumara;
            if (int.TryParse(mevcutFisNo, NumberStyles.None, CultureInfo.InvariantCulture, out mevcutNumara))
            {
                tartim.KantarFisNo = mevcutNumara.ToString("00000", CultureInfo.InvariantCulture);
                return tartim.KantarFisNo;
            }

            if (!string.IsNullOrWhiteSpace(mevcutFisNo))
            {
                tartim.KantarFisNo = mevcutFisNo;
                return tartim.KantarFisNo;
            }

            var sonNumara = (tumTartimlar ?? Enumerable.Empty<Tartim>())
                .Where(x => !ReferenceEquals(x, tartim))
                .Select(x =>
                {
                    int fisNo;
                    return int.TryParse((x.KantarFisNo ?? string.Empty).Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out fisNo)
                        ? fisNo
                        : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            tartim.KantarFisNo = (sonNumara + 1).ToString("00000", CultureInfo.InvariantCulture);
            return tartim.KantarFisNo;
        }
    }
}
