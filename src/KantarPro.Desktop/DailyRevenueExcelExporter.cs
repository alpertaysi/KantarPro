using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KantarPro.Desktop
{
    public static class DailyRevenueExcelExporter
    {
        public static void Export(string path, IEnumerable<DailyRevenueRow> rows)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Excel dosya yolu boş olamaz.", nameof(path));
            }

            var list = (rows ?? Enumerable.Empty<DailyRevenueRow>()).ToList();
            var builder = new StringBuilder();
            builder.AppendLine("Sıra;İşlem No;İşlem Tipi;Kantar Fiş No;Ödeme Türü;Firma;Plaka;Çıkış Tarihi;Çıkış Saati;Giriş Ücreti;Tartım Ücreti;İşgaliye Ücreti;Toplam Ücret");

            foreach (var row in list)
            {
                builder.Append(row.SiraNo).Append(';')
                    .Append(Escape(row.IslemNo)).Append(';')
                    .Append(Escape(row.IslemTipi)).Append(';')
                    .Append(Escape(row.KantarFisNo)).Append(';')
                    .Append(Escape(row.OdemeTuru)).Append(';')
                    .Append(Escape(row.FirmaAdi)).Append(';')
                    .Append(Escape(row.Plaka)).Append(';')
                    .Append(Escape(row.CikisTarihi)).Append(';')
                    .Append(Escape(row.CikisSaati)).Append(';')
                    .Append(Escape(row.GirisCikisUcreti)).Append(';')
                    .Append(Escape(row.TartimUcreti)).Append(';')
                    .Append(Escape(row.BeklemeUcreti)).Append(';')
                    .Append(Escape(row.ToplamUcret))
                    .AppendLine();
            }

            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(true));
        }

        private static string Escape(string value)
        {
            value = value ?? string.Empty;
            if (value.IndexOfAny(new[] { ';', '"', '\r', '\n' }) < 0)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
