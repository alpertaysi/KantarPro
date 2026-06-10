using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KantarPro.Desktop
{
    public static class SearchResultsTextFormatter
    {
        private const int ReportWidth = 127;

        public static string Build(IEnumerable<SearchResultRow> rows, string title)
        {
            var list = (rows ?? Enumerable.Empty<SearchResultRow>()).ToList();
            var builder = new StringBuilder();

            builder.AppendLine(Center("TURKIYE CUMHURIYETI"));
            builder.AppendLine(Center("TICARET BAKANLIGI"));
            builder.AppendLine(Center("GECMIS KAYIT ARASTIRMA DOKUMU"));
            builder.AppendLine(Center(string.IsNullOrWhiteSpace(title) ? DateTime.Now.ToString("dd.MM.yyyy HH:mm") : title));
            builder.AppendLine(new string('-', ReportWidth));
            builder.AppendLine(DailyRevenueTextFormatter.BuildHeader());
            builder.AppendLine(new string('-', ReportWidth));

            foreach (var row in list)
            {
                builder.AppendLine(DailyRevenueTextFormatter.BuildRow(
                    row.SiraNo.ToString(),
                    row.IslemNo,
                    row.Plaka,
                    row.Firma,
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
            builder.AppendLine("Kayit: " + list.Count);
            return builder.ToString();
        }

        private static string Center(string text)
        {
            text = string.IsNullOrWhiteSpace(text) ? "-" : text.Trim();
            return text.Length >= ReportWidth
                ? text.Substring(0, ReportWidth)
                : new string(' ', (ReportWidth - text.Length) / 2) + text;
        }
    }
}
