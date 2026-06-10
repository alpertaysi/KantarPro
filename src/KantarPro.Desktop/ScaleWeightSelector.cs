using System;
using System.Globalization;

namespace KantarPro.Desktop
{
    public static class ScaleWeightSelector
    {
        public static decimal GetWeight(
            decimal? indicatorWeightKg,
            string manualText,
            bool adminManualAllowed)
        {
            if (adminManualAllowed)
            {
                decimal manualWeight;
                if (TryParsePositiveWeight(manualText, out manualWeight))
                {
                    return manualWeight;
                }
            }

            if (indicatorWeightKg.HasValue && indicatorWeightKg.Value > 0)
            {
                return indicatorWeightKg.Value;
            }

            throw new InvalidOperationException(
                "İndikatörden geçerli kilo alınmadan tartımlı kayıt yapılamaz.");
        }

        private static bool TryParsePositiveWeight(string value, out decimal weight)
        {
            var text = (value ?? string.Empty).Trim();
            if (decimal.TryParse(
                    text,
                    NumberStyles.Number,
                    CultureInfo.GetCultureInfo("tr-TR"),
                    out weight) &&
                weight > 0)
            {
                return true;
            }

            return decimal.TryParse(
                       text,
                       NumberStyles.Number,
                       CultureInfo.InvariantCulture,
                       out weight) &&
                   weight > 0;
        }
    }
}
