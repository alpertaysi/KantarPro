using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KantarPro.Desktop
{
    public static class SearchResultsTextFormatter
    {
        private const int SiraWidth = 5;
        private const int IslemWidth = 8;
        private const int DurumWidth = 16;
        private const int PlakaWidth = 12;
        private const int FirmaWidth = 22;
        private const int TarihWidth = 12;
        private const int SaatWidth = 9;
        private const int TartimWidth = 11;
        private const int NetWidth = 10;
        private const int TahsilatWidth = 12;
        private const int OdemeWidth = 12;
        private const int FisWidth = 8;
        private const int KullaniciWidth = 16;
        private const int ToplamWidth = 11;
        private const int ReportWidth =
            SiraWidth +
            IslemWidth +
            DurumWidth +
            PlakaWidth +
            FirmaWidth +
            TarihWidth +
            SaatWidth +
            TarihWidth +
            SaatWidth +
            TartimWidth +
            TartimWidth +
            NetWidth +
            TahsilatWidth +
            OdemeWidth +
            FisWidth +
            KullaniciWidth +
            ToplamWidth;

        public static string Build(IEnumerable<SearchResultRow> rows, string title)
        {
            var list = (rows ?? Enumerable.Empty<SearchResultRow>()).ToList();
            var builder = new StringBuilder();

            builder.AppendLine(Center("TURKIYE CUMHURIYETI", ReportWidth));
            builder.AppendLine(Center("TICARET BAKANLIGI", ReportWidth));
            builder.AppendLine(Center("GECMIS KAYIT ARASTIRMA DOKUMU", ReportWidth));
            builder.AppendLine(Center(string.IsNullOrWhiteSpace(title) ? DateTime.Now.ToString("dd.MM.yyyy HH:mm") : title, ReportWidth));
            builder.AppendLine(new string('-', ReportWidth));
            builder.AppendLine(
                Col("Sira", SiraWidth) +
                Col("Islem", IslemWidth) +
                Col("Durum", DurumWidth) +
                Col("Plaka", PlakaWidth) +
                Col("Firma", FirmaWidth) +
                Col("Giris", TarihWidth) +
                Col("Saat", SaatWidth) +
                Col("Cikis", TarihWidth) +
                Col("Saat", SaatWidth) +
                Col("1.Tartim", TartimWidth) +
                Col("2.Tartim", TartimWidth) +
                Col("Net", NetWidth) +
                Col("Tahsilat", TahsilatWidth) +
                Col("Odeme", OdemeWidth) +
                Col("Fis", FisWidth) +
                Col("Kullanici", KullaniciWidth) +
                Col("Toplam", ToplamWidth));
            builder.AppendLine(new string('-', ReportWidth));

            foreach (var row in list)
            {
                builder.AppendLine(
                    Col(row.SiraNo.ToString(), SiraWidth) +
                    Col(row.IslemNo, IslemWidth) +
                    Col(row.Durum, DurumWidth) +
                    Col(row.Plaka, PlakaWidth) +
                    Col(row.Firma, FirmaWidth) +
                    Col(row.GirisTarihi, TarihWidth) +
                    Col(row.GirisSaati, SaatWidth) +
                    Col(row.CikisTarihi, TarihWidth) +
                    Col(row.CikisSaati, SaatWidth) +
                    Col(row.BirinciTartim, TartimWidth) +
                    Col(row.IkinciTartim, TartimWidth) +
                    Col(row.Net, NetWidth) +
                    Col(row.TahsilatNo, TahsilatWidth) +
                    Col(row.OdemeTuru, OdemeWidth) +
                    Col(row.KantarFisNo, FisWidth) +
                    Col(row.Kullanici, KullaniciWidth) +
                    Col(row.ToplamUcret, ToplamWidth));
            }

            builder.AppendLine(new string('-', ReportWidth));
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
