using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KantarPro.Desktop
{
    public static class SearchResultsTextFormatter
    {
        public static string Build(IEnumerable<SearchResultRow> rows, string title)
        {
            var list = (rows ?? Enumerable.Empty<SearchResultRow>()).ToList();
            var builder = new StringBuilder();

            const int reportWidth = 176;

            builder.AppendLine(Center("TURKIYE CUMHURIYETI", reportWidth));
            builder.AppendLine(Center("TICARET BAKANLIGI", reportWidth));
            builder.AppendLine(Center("GECMIS KAYIT ARASTIRMA DOKUMU", reportWidth));
            builder.AppendLine(Center(string.IsNullOrWhiteSpace(title) ? DateTime.Now.ToString("dd.MM.yyyy HH:mm") : title, reportWidth));
            builder.AppendLine(new string('-', reportWidth));
            builder.AppendLine(
                Col("Sira", 5) +
                Col("Islem", 8) +
                Col("Durum", 16) +
                Col("Plaka", 12) +
                Col("Firma", 22) +
                Col("Giris", 12) +
                Col("Saat", 9) +
                Col("Cikis", 12) +
                Col("Saat", 9) +
                Col("1.Tartim", 11) +
                Col("2.Tartim", 11) +
                Col("Net", 10) +
                Col("Tahsilat", 12) +
                Col("Odeme", 12) +
                Col("Fis", 8) +
                Col("Kullanici", 16) +
                Col("Toplam", 11));
            builder.AppendLine(new string('-', reportWidth));

            foreach (var row in list)
            {
                builder.AppendLine(
                    Col(row.SiraNo.ToString(), 5) +
                    Col(row.IslemNo, 8) +
                    Col(row.Durum, 16) +
                    Col(row.Plaka, 12) +
                    Col(row.Firma, 22) +
                    Col(row.GirisTarihi, 12) +
                    Col(row.GirisSaati, 9) +
                    Col(row.CikisTarihi, 12) +
                    Col(row.CikisSaati, 9) +
                    Col(row.BirinciTartim, 11) +
                    Col(row.IkinciTartim, 11) +
                    Col(row.Net, 10) +
                    Col(row.TahsilatNo, 12) +
                    Col(row.OdemeTuru, 12) +
                    Col(row.KantarFisNo, 8) +
                    Col(row.Kullanici, 16) +
                    Col(row.ToplamUcret, 11));
            }

            builder.AppendLine(new string('-', reportWidth));
            builder.AppendLine("Kayit: " + list.Count);
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
