using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KantarPro.Desktop
{
    public static class DailyRevenueTextFormatter
    {
        private const int ReportWidth = 127;

        public static string Build(
            IEnumerable<DailyRevenueRow> rows,
            string title,
            string entryTotal,
            string weighingTotal,
            string waitingTotal,
            string grandTotal)
        {
            var list = (rows ?? Enumerable.Empty<DailyRevenueRow>()).ToList();
            var builder = new StringBuilder();

            AppendTitle(builder, "GUNLUK TAHSILAT DOKUMU", title);
            builder.AppendLine(BuildHeader());
            builder.AppendLine(new string('-', ReportWidth));

            foreach (var row in list)
            {
                builder.AppendLine(BuildRow(
                    row.SiraNo.ToString(),
                    row.IslemNo,
                    row.Plaka,
                    row.FirmaAdi,
                    row.CikisTarihi,
                    row.CikisSaati,
                    row.GirisTarihi,
                    row.GirisSaati,
                    row.OdemeTuru,
                    row.KantarFisNo,
                    row.GirisCikisUcreti,
                    row.TartimUcreti,
                    row.BeklemeUcreti,
                    row.ToplamUcret));
            }

            builder.AppendLine(new string('-', ReportWidth));
            builder.AppendLine(
                "Kayit: " + list.Count +
                "   Giris-Cikis: " + Safe(entryTotal) +
                "   Tartim: " + Safe(weighingTotal) +
                "   Bekleme: " + Safe(waitingTotal) +
                "   Toplam: " + Safe(grandTotal));
            return builder.ToString();
        }

        internal static string BuildHeader()
        {
            return JoinColumns(
                Col("No", 2),
                Col("Islem", 5),
                Col("Plaka", 8),
                Col("Firma", 14),
                Col("Cikis Tar", 10),
                Col("C.Saat", 8),
                Col("Giris Tar", 10),
                Col("G.Saat", 8),
                Col("Odeme", 5),
                Col("Fis No", 12),
                Col("G-Cikis", 8),
                Col("Tartim", 8),
                Col("Bekleme", 8),
                Col("Toplam", 8));
        }

        internal static string BuildRow(
            string sequence,
            string transaction,
            string plate,
            string company,
            string exitDate,
            string exitTime,
            string entryDate,
            string entryTime,
            string payment,
            string receipt,
            string entryExitFee,
            string weighingFee,
            string waitingFee,
            string total)
        {
            return JoinColumns(
                Col(sequence, 2),
                Col(transaction, 5),
                Col(plate, 8),
                Col(company, 14),
                Col(exitDate, 10),
                Col(exitTime, 8),
                Col(entryDate, 10),
                Col(entryTime, 8),
                Col(payment, 5),
                Col(receipt, 12),
                Col(Amount(entryExitFee), 8),
                Col(Amount(weighingFee), 8),
                Col(Amount(waitingFee), 8),
                Col(Amount(total), 8));
        }

        private static void AppendTitle(StringBuilder builder, string reportTitle, string title)
        {
            builder.AppendLine(Center("TURKIYE CUMHURIYETI"));
            builder.AppendLine(Center("TICARET BAKANLIGI"));
            builder.AppendLine(Center(reportTitle));
            builder.AppendLine(Center(string.IsNullOrWhiteSpace(title) ? DateTime.Now.ToString("dd.MM.yyyy") : title));
            builder.AppendLine(new string('-', ReportWidth));
        }

        private static string Col(string value, int width)
        {
            var normalized = Safe(value).Replace(Environment.NewLine, " ");
            if (normalized.Length > width)
            {
                normalized = normalized.Substring(0, width);
            }

            return normalized.PadRight(width);
        }

        private static string JoinColumns(params string[] columns)
        {
            return string.Join("|", columns);
        }

        private static string Center(string text)
        {
            text = Safe(text);
            return text.Length >= ReportWidth
                ? text.Substring(0, ReportWidth)
                : new string(' ', (ReportWidth - text.Length) / 2) + text;
        }

        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }

        private static string Amount(string value)
        {
            var normalized = Safe(value);
            normalized = normalized.EndsWith(" TL", StringComparison.OrdinalIgnoreCase)
                ? normalized.Substring(0, normalized.Length - 3).TrimEnd()
                : normalized;
            return normalized.Replace(".", "");
        }
    }
}
