using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KantarPro.Desktop
{
    public static class DailyRevenueTextFormatter
    {
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

            builder.AppendLine(Center("TURKIYE CUMHURIYETI", 132));
            builder.AppendLine(Center("TICARET BAKANLIGI", 132));
            builder.AppendLine(Center("GUNLUK TAHSILAT DOKUMU", 132));
            builder.AppendLine(Center(string.IsNullOrWhiteSpace(title) ? DateTime.Now.ToString("dd.MM.yyyy") : title, 132));
            builder.AppendLine(new string('-', 132));
            builder.AppendLine(
                Col("Sira", 5) +
                Col("Islem", 8) +
                Col("Tip", 12) +
                Col("Fis", 7) +
                Col("Odeme", 12) +
                Col("Firma", 22) +
                Col("Plaka", 12) +
                Col("Cikis", 12) +
                Col("Saat", 8) +
                Col("Giris", 10) +
                Col("Tartim", 10) +
                Col("Isgaliye", 11) +
                Col("Toplam", 12));
            builder.AppendLine(new string('-', 132));

            foreach (var row in list)
            {
                builder.AppendLine(
                    Col(row.SiraNo.ToString(), 5) +
                    Col(row.IslemNo, 8) +
                    Col(row.IslemTipi, 12) +
                    Col(row.KantarFisNo, 7) +
                    Col(row.OdemeTuru, 12) +
                    Col(row.FirmaAdi, 22) +
                    Col(row.Plaka, 12) +
                    Col(row.CikisTarihi, 12) +
                    Col(row.CikisSaati, 8) +
                    Col(row.GirisCikisUcreti, 10) +
                    Col(row.TartimUcreti, 10) +
                    Col(row.BeklemeUcreti, 11) +
                    Col(row.ToplamUcret, 12));
            }

            builder.AppendLine(new string('-', 132));
            builder.AppendLine("Kayit: " + list.Count);
            builder.AppendLine("Giris: " + Safe(entryTotal) + "   Tartim: " + Safe(weighingTotal) + "   Isgaliye: " + Safe(waitingTotal) + "   Toplam: " + Safe(grandTotal));
            return builder.ToString();
        }

        private static string Col(string value, int width)
        {
            value = Safe(value).Replace(Environment.NewLine, " ");
            if (value.Length > width - 1)
            {
                value = value.Substring(0, width - 1);
            }

            return value.PadRight(width);
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

        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
        }
    }
}
