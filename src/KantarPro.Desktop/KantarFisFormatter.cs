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
            var doluBosFisi = !string.IsNullOrWhiteSpace(ikinciTartim) &&
                !IsNoWeighingText(ikinciTartim) &&
                !string.IsNullOrWhiteSpace(net) &&
                !IsNoWeighingText(net);

            var builder = new StringBuilder();
            AppendHeader(builder);
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine();

            if (doluBosFisi)
            {
                AppendDoluBosFis(builder, row, birinciTartim, ikinciTartim, net);
            }
            else
            {
                AppendTekTartimFis(builder, row, birinciTartim);
            }

            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine("   MEMUR IMZA: ____________________");

            return builder.ToString();
        }

        public static string BuildFromPendingRow(PendingWeighingPrototypeRow row)
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            var birinciTartim = CleanWeight(row.IlkAgirlik);
            if (string.IsNullOrWhiteSpace(birinciTartim) || IsNoWeighingText(birinciTartim))
            {
                throw new InvalidOperationException("Bu kayitta kantar tartimi yok. Tartimsiz girisler icin kantar fisi olusturulmaz.");
            }

            var fisRow = new VehicleMovementRow
            {
                IslemNo = row.IslemNo,
                KantarFisNo = row.KantarFisNo,
                Plaka = row.Plaka,
                GirisTarihi = FirstNonEmpty(row.IlkTartimTarihi, row.IlkGirisTarihi),
                GirisSaati = FirstNonEmpty(row.IlkTartimSaati, row.IlkGirisSaati),
                Tartim = birinciTartim
            };

            return BuildFromRow(fisRow);
        }

        private static void AppendHeader(StringBuilder builder)
        {
            builder.AppendLine(Center("TICARET BAKANLIGI", 72));
            builder.AppendLine(Center("ULUDAG GUMRUK VE TICARET BOLGE MUDURLUGU", 72));
            builder.AppendLine(Center("BURSATASFIYE ISLETME MUDURLUGU", 72));
        }

        private static void AppendTekTartimFis(StringBuilder builder, VehicleMovementRow row, string birinciTartim)
        {
            builder.AppendLine("   " + Pair("PLAKA NO.....:", row.Plaka, "FIS NO...:", FormatFisNo(row)));
            builder.AppendLine();
            builder.AppendLine("   " + Pair("GIRIS TARIHI:", row.GirisTarihi, "SAATI....:", row.GirisSaati));
            builder.AppendLine();
            builder.AppendLine("   " + Field("1.TARTI", FormatKg(birinciTartim)));
        }

        private static void AppendDoluBosFis(StringBuilder builder, VehicleMovementRow row, string birinciTartim, string ikinciTartim, string net)
        {
            var ikinciGirisTarihi = FirstNonEmpty(row.BosGelisTarihi, row.IkinciTartimTarihi);
            var ikinciGirisSaati = FirstNonEmpty(row.BosGelisSaati, row.IkinciTartimSaati);

            builder.AppendLine("   " + Pair("PLAKA NO.....:", row.Plaka, "FIS NO...:", FormatFisNo(row)));
            builder.AppendLine();
            builder.AppendLine("   " + Pair("1.GIRIS TARIHI:", row.GirisTarihi, "SAATI....:", row.GirisSaati));
            builder.AppendLine("   " + Pair("2.GIRIS TARIHI:", ikinciGirisTarihi, "SAATI....:", ikinciGirisSaati));
            builder.AppendLine();
            builder.AppendLine("   " + Field("1.TARTI", FormatKg(birinciTartim)));
            builder.AppendLine("   " + Field("2.TARTI", FormatKg(ikinciTartim)));
            builder.AppendLine();
            builder.AppendLine("   " + Field("NET", FormatKg(net)));
        }

        private static string FormatFisNo(VehicleMovementRow row)
        {
            return KantarFisPreviewData.FormatFisNo(row);
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
