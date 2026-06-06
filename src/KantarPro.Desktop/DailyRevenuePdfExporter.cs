using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace KantarPro.Desktop
{
    public sealed class DailyRevenuePdfExporter
    {
        private const float PageWidth = 842f;
        private const float PageHeight = 595f;
        private const float LeftMargin = 28f;
        private const float RightMargin = 28f;
        private const float TopMargin = 34f;
        private const float RowHeight = 18f;

        private readonly IList<DailyRevenueRow> _rows;
        private readonly string _title;
        private readonly string _entryTotal;
        private readonly string _weighingTotal;
        private readonly string _waitingTotal;
        private readonly string _grandTotal;

        public DailyRevenuePdfExporter(
            IEnumerable<DailyRevenueRow> rows,
            string title,
            string entryTotal,
            string weighingTotal,
            string waitingTotal,
            string grandTotal)
        {
            _rows = (rows ?? Enumerable.Empty<DailyRevenueRow>()).ToList();
            _title = string.IsNullOrWhiteSpace(title) ? "Günlük Tahsilat Dökümü" : title;
            _entryTotal = entryTotal ?? "0,00 TL";
            _weighingTotal = weighingTotal ?? "0,00 TL";
            _waitingTotal = waitingTotal ?? "0,00 TL";
            _grandTotal = grandTotal ?? "0,00 TL";
        }

        public void Export(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new InvalidOperationException("PDF dosya yolu seçilmedi.");
            }

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var pages = BuildPages();
            WritePdf(filePath, pages);
        }

        private IList<string> BuildPages()
        {
            var pages = new List<string>();
            var columns = BuildColumns();
            var rowIndex = 0;

            do
            {
                var content = new StringBuilder();
                DrawText(content, _title, 16, LeftMargin, PageHeight - TopMargin, true);

                var y = PageHeight - TopMargin - 34;
                DrawTableHeader(content, columns, y);
                y -= RowHeight;

                while (rowIndex < _rows.Count && y > 64)
                {
                    DrawTableRow(content, columns, _rows[rowIndex], y);
                    y -= RowHeight;
                    rowIndex++;
                }

                if (rowIndex >= _rows.Count)
                {
                    var totals = string.Format(
                        CultureInfo.InvariantCulture,
                        "Giriş: {0}   Tartım: {1}   İşgaliye: {2}   Toplam: {3}",
                        _entryTotal,
                        _weighingTotal,
                        _waitingTotal,
                        _grandTotal);
                    DrawText(content, totals, 10, LeftMargin, 34, true);
                }

                pages.Add(content.ToString());
            }
            while (rowIndex < _rows.Count);

            return pages;
        }

        private static IList<ReportColumn> BuildColumns()
        {
            var baseWidths = new[] { 30, 48, 70, 54, 62, 120, 70, 68, 64, 75, 75, 82, 82 };
            var scale = (PageWidth - LeftMargin - RightMargin) / baseWidths.Sum();
            var widths = baseWidths.Select(x => (float)Math.Floor(x * scale)).ToArray();
            return new[]
            {
                new ReportColumn("Sıra", widths[0], x => x.SiraNo.ToString(CultureInfo.InvariantCulture)),
                new ReportColumn("İşlem", widths[1], x => x.IslemNo),
                new ReportColumn("Tip", widths[2], x => x.IslemTipi),
                new ReportColumn("Fiş", widths[3], x => x.KantarFisNo),
                new ReportColumn("Ödeme", widths[4], x => x.OdemeTuru),
                new ReportColumn("Firma", widths[5], x => x.FirmaAdi),
                new ReportColumn("Plaka", widths[6], x => x.Plaka),
                new ReportColumn("Çıkış T.", widths[7], x => x.CikisTarihi),
                new ReportColumn("Çıkış S.", widths[8], x => x.CikisSaati),
                new ReportColumn("Giriş", widths[9], x => x.GirisCikisUcreti),
                new ReportColumn("Tartım", widths[10], x => x.TartimUcreti),
                new ReportColumn("İşgaliye", widths[11], x => x.BeklemeUcreti),
                new ReportColumn("Toplam", widths[12], x => x.ToplamUcret)
            };
        }

        private static void DrawTableHeader(StringBuilder content, IList<ReportColumn> columns, float y)
        {
            var x = LeftMargin;
            foreach (var column in columns)
            {
                DrawRectangle(content, x, y - 3, column.Width, RowHeight);
                DrawText(content, column.Header, 7, x + 2, y + 2, true);
                x += column.Width;
            }
        }

        private static void DrawTableRow(StringBuilder content, IList<ReportColumn> columns, DailyRevenueRow row, float y)
        {
            var x = LeftMargin;
            foreach (var column in columns)
            {
                DrawRectangle(content, x, y - 3, column.Width, RowHeight);
                DrawText(content, TrimToFit(column.Read(row), column.MaxLength), 7, x + 2, y + 2, false);
                x += column.Width;
            }
        }

        private static void DrawRectangle(StringBuilder content, float x, float y, float width, float height)
        {
            content.AppendFormat(CultureInfo.InvariantCulture, "0.75 w {0:0.##} {1:0.##} {2:0.##} {3:0.##} re S\n", x, y, width, height);
        }

        private static void DrawText(StringBuilder content, string text, int size, float x, float y, bool bold)
        {
            content.AppendFormat(CultureInfo.InvariantCulture, "BT /{0} {1} Tf {2:0.##} {3:0.##} Td ({4}) Tj ET\n", bold ? "F2" : "F1", size, x, y, EscapePdfText(text));
        }

        private static void WritePdf(string filePath, IList<string> pages)
        {
            var objects = new List<string>();
            var pageObjectIds = new List<int>();

            objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
            objects.Add("");
            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding 5 0 R >>");
            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding 5 0 R >>");
            objects.Add("<< /Type /Encoding /BaseEncoding /WinAnsiEncoding /Differences [208 /Gbreve 221 /Idotaccent 222 /Scedilla 240 /gbreve 253 /dotlessi 254 /scedilla] >>");

            foreach (var page in pages)
            {
                var contentId = objects.Count + 2;
                var pageId = objects.Count + 1;
                pageObjectIds.Add(pageId);
                objects.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {0} {1}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {2} 0 R >>",
                    PageWidth,
                    PageHeight,
                    contentId));
                var bytes = Encoding.ASCII.GetBytes(page);
                objects.Add("<< /Length " + bytes.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n" + page + "endstream");
            }

            objects[1] = "<< /Type /Pages /Kids [" + string.Join(" ", pageObjectIds.Select(x => x.ToString(CultureInfo.InvariantCulture) + " 0 R")) + "] /Count " + pageObjectIds.Count.ToString(CultureInfo.InvariantCulture) + " >>";

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                WriteAscii(stream, "%PDF-1.4\n");
                var offsets = new List<long> { 0 };
                for (var i = 0; i < objects.Count; i++)
                {
                    offsets.Add(stream.Position);
                    WriteAscii(stream, (i + 1).ToString(CultureInfo.InvariantCulture) + " 0 obj\n");
                    WriteAscii(stream, objects[i]);
                    WriteAscii(stream, "\nendobj\n");
                }

                var xrefStart = stream.Position;
                WriteAscii(stream, "xref\n");
                WriteAscii(stream, "0 " + (objects.Count + 1).ToString(CultureInfo.InvariantCulture) + "\n");
                WriteAscii(stream, "0000000000 65535 f \n");
                for (var i = 1; i < offsets.Count; i++)
                {
                    WriteAscii(stream, offsets[i].ToString("0000000000", CultureInfo.InvariantCulture) + " 00000 n \n");
                }

                WriteAscii(stream, "trailer\n");
                WriteAscii(stream, "<< /Size " + (objects.Count + 1).ToString(CultureInfo.InvariantCulture) + " /Root 1 0 R >>\n");
                WriteAscii(stream, "startxref\n");
                WriteAscii(stream, xrefStart.ToString(CultureInfo.InvariantCulture) + "\n");
                WriteAscii(stream, "%%EOF");
            }
        }

        private static void WriteAscii(Stream stream, string text)
        {
            var bytes = Encoding.ASCII.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static string TrimToFit(string value, int max)
        {
            value = value ?? "";
            return value.Length <= max ? value : value.Substring(0, max - 1) + ".";
        }

        private static string EscapePdfText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "";
            }

            var bytes = Encoding.GetEncoding(1254).GetBytes(text.Replace("₺", "T"));
            var builder = new StringBuilder(bytes.Length);
            foreach (var b in bytes)
            {
                if (b == 40 || b == 41 || b == 92)
                {
                    builder.Append('\\').Append((char)b);
                }
                else if (b < 32 || b > 126)
                {
                    builder.Append('\\').Append(Convert.ToString(b, 8).PadLeft(3, '0'));
                }
                else
                {
                    builder.Append((char)b);
                }
            }

            return builder.ToString();
        }

        private sealed class ReportColumn
        {
            public ReportColumn(string header, float width, Func<DailyRevenueRow, string> read)
            {
                Header = header;
                Width = width;
                Read = read;
                MaxLength = Math.Max(4, (int)Math.Floor(width / 5));
            }

            public string Header { get; private set; }
            public float Width { get; private set; }
            public int MaxLength { get; private set; }
            public Func<DailyRevenueRow, string> Read { get; private set; }
        }
    }
}



