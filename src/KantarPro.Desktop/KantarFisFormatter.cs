using System;
using System.Globalization;
using System.Text;

namespace KantarPro.Desktop
{
    public static class KantarFisFormatter
    {
        public static string BuildFromRow(VehicleMovementRow row)
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            var birinciTartim = CleanWeight(row.Tartim);
            if (string.IsNullOrWhiteSpace(birinciTartim) || IsNoWeighingText(birinciTartim))
            {
                throw new InvalidOperationException("Bu kayitta kantar tartimi yok. Tartimsiz girisler icin kantar fisi olusturulmaz.");
            }

            var ikinciTartim = CleanWeight(row.IkinciTartim);
            var net = CleanWeight(row.NetAgirlik);
            var cikisTarihi = FirstNonEmpty(row.CikisTarihi, row.DoluCikisTarihi);
            var cikisSaati = FirstNonEmpty(row.CikisSaati, row.DoluCikisSaati);

            var builder = new StringBuilder();
            builder.AppendLine(Center("TICARET BAKANLIGI", 72));
            builder.AppendLine(Center("ULUDAG GUMRUK VE TICARET BOLGE MUDURLUGU", 72));
            builder.AppendLine(Center("BURSATASFIYE ISLETME MUDURLUGU", 72));
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine("   " + Pair("PLAKA NO.....:", row.Plaka, "BILET NO.:", "-"));
            builder.AppendLine();
            builder.AppendLine("   " + Pair("GIRIS TARIHI:", row.GirisTarihi, "SAATI....:", row.GirisSaati));
            builder.AppendLine("   " + Pair("CIKIS TARIHI:", cikisTarihi, "SAATI....:", cikisSaati));
            builder.AppendLine();
            builder.AppendLine("   " + Field("MUSTERI ADI", row.FirmaAdi));
            builder.AppendLine("   " + Field("MAL CINSI", ""));
            builder.AppendLine("   " + Field("ACIKLAMA", row.Durum));
            builder.AppendLine("   " + Field("GITTIGI YER", ""));
            builder.AppendLine("   " + Field("GELDIGI YER", ""));
            builder.AppendLine();
            builder.AppendLine("   " + Field("1.TARTI", FormatKg(birinciTartim)));

            if (!string.IsNullOrWhiteSpace(ikinciTartim) && !IsNoWeighingText(ikinciTartim))
            {
                builder.AppendLine("   " + Field("2.TARTI", FormatKg(ikinciTartim)));
            }
            else
            {
                builder.AppendLine("   " + Field("2.TARTI", ""));
            }

            builder.AppendLine();
            builder.AppendLine("   " + Field("NET", string.IsNullOrWhiteSpace(net) || IsNoWeighingText(net) ? "" : FormatKg(net)));
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine("   MEMUR IMZA: ____________________");

            return builder.ToString();
        }

        private static string Pair(string leftLabel, string leftValue, string rightLabel, string rightValue)
        {
            var left = leftLabel.PadRight(14) + " " + Safe(leftValue).PadRight(16);
            var right = rightLabel.PadRight(10) + " " + Safe(rightValue);
            return left + "     " + right;
        }

        private static string Field(string label, string value)
        {
            return label.PadRight(13) + ": " + Safe(value);
        }

        private static string Center(string text, int width)
        {
            text = Safe(text);
            if (text.Length >= width)
            {
                return text;
            }

            return new string(' ', (width - text.Length) / 2) + text;
        }

        private static string FormatKg(string value)
        {
            decimal parsed;
            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out parsed) ||
                decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed.ToString("N0", CultureInfo.GetCultureInfo("tr-TR")).PadLeft(8) + " Kg";
            }

            return value.PadLeft(8) + " Kg";
        }

        private static string CleanWeight(string value)
        {
            return Safe(value)
                .Replace("kg", "")
                .Replace("KG", "")
                .Replace("Kg", "")
                .Trim();
        }

        private static bool IsNoWeighingText(string value)
        {
            return value.IndexOf("tartimsiz", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("tartim yok", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value == "0" ||
                   value == "0,00" ||
                   value == "-";
        }

        private static string FirstNonEmpty(string first, string second)
        {
            return !string.IsNullOrWhiteSpace(first) ? first : Safe(second);
        }

        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
        }
    }
}
