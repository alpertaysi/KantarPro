using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;

namespace KantarPro.Desktop
{
    public sealed class DailyRevenuePdfExporter
    {
        private readonly IList<DailyRevenueRow> _rows;
        private readonly string _title;
        private readonly string _entryTotal;
        private readonly string _weighingTotal;
        private readonly string _waitingTotal;
        private readonly string _grandTotal;
        private int _rowIndex;

        public DailyRevenuePdfExporter(
            IEnumerable<DailyRevenueRow> rows,
            string title,
            string entryTotal,
            string weighingTotal,
            string waitingTotal,
            string grandTotal)
        {
            _rows = (rows ?? Enumerable.Empty<DailyRevenueRow>()).ToList();
            _title = string.IsNullOrWhiteSpace(title) ? "Gunluk Tahsilat Dokumu" : title;
            _entryTotal = entryTotal ?? "0,00 TL";
            _weighingTotal = weighingTotal ?? "0,00 TL";
            _waitingTotal = waitingTotal ?? "0,00 TL";
            _grandTotal = grandTotal ?? "0,00 TL";
        }

        public void Export(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new InvalidOperationException("PDF dosya yolu secilmedi.");
            }

            using (var document = new PrintDocument())
            {
                document.DocumentName = "Gunluk Tahsilat Dokumu";
                document.PrinterSettings.PrinterName = "Microsoft Print to PDF";
                document.PrinterSettings.PrintToFile = true;
                document.PrinterSettings.PrintFileName = filePath;
                document.DefaultPageSettings.Landscape = true;
                document.DefaultPageSettings.Margins = new Margins(35, 35, 35, 35);
                document.PrintPage += PrintPage;
                _rowIndex = 0;
                document.Print();
            }
        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            var bounds = e.MarginBounds;
            using (var titleFont = new Font("Segoe UI", 14, FontStyle.Bold))
            using (var headerFont = new Font("Segoe UI", 8, FontStyle.Bold))
            using (var rowFont = new Font("Segoe UI", 8, FontStyle.Regular))
            using (var totalFont = new Font("Segoe UI", 9, FontStyle.Bold))
            using (var linePen = new Pen(Color.FromArgb(180, 190, 200)))
            using (var headerBrush = new SolidBrush(Color.FromArgb(235, 240, 248)))
            {
                var y = bounds.Top;
                e.Graphics.DrawString(_title, titleFont, Brushes.Black, bounds.Left, y);
                y += 28;

                var columns = BuildColumns(bounds.Width);
                DrawHeader(e.Graphics, headerFont, headerBrush, linePen, columns, bounds.Left, y);
                y += 24;

                while (_rowIndex < _rows.Count && y + 22 < bounds.Bottom - 42)
                {
                    DrawRow(e.Graphics, rowFont, linePen, columns, bounds.Left, y, _rows[_rowIndex]);
                    y += 22;
                    _rowIndex++;
                }

                if (_rowIndex >= _rows.Count)
                {
                    y = Math.Max(y + 12, bounds.Bottom - 34);
                    var totals = string.Format(
                        "Giris: {0}    Tartim: {1}    Isgaliye: {2}    Toplam: {3}",
                        _entryTotal,
                        _weighingTotal,
                        _waitingTotal,
                        _grandTotal);
                    e.Graphics.DrawString(totals, totalFont, Brushes.Black, bounds.Left, y);
                    e.HasMorePages = false;
                }
                else
                {
                    e.HasMorePages = true;
                }
            }
        }

        private static IList<ReportColumn> BuildColumns(int totalWidth)
        {
            var widths = new[] { 35, 55, 80, 70, 75, 120, 75, 70, 70, 70, 80, 80, 80, 85 };
            var sum = widths.Sum();
            var scale = totalWidth / (float)sum;
            return new[]
            {
                new ReportColumn("Sira", widths[0] * scale, x => x.SiraNo.ToString()),
                new ReportColumn("Islem", widths[1] * scale, x => x.IslemNo),
                new ReportColumn("Tip", widths[2] * scale, x => x.IslemTipi),
                new ReportColumn("Fis", widths[3] * scale, x => x.KantarFisNo),
                new ReportColumn("Odeme", widths[4] * scale, x => x.OdemeTuru),
                new ReportColumn("Firma", widths[5] * scale, x => x.FirmaAdi),
                new ReportColumn("Plaka", widths[6] * scale, x => x.Plaka),
                new ReportColumn("Cikis T.", widths[7] * scale, x => x.CikisTarihi),
                new ReportColumn("Cikis S.", widths[8] * scale, x => x.CikisSaati),
                new ReportColumn("1.Tartim", widths[9] * scale, x => x.IlkTartim),
                new ReportColumn("Giris", widths[10] * scale, x => x.GirisCikisUcreti),
                new ReportColumn("Tartim", widths[11] * scale, x => x.TartimUcreti),
                new ReportColumn("Isgaliye", widths[12] * scale, x => x.BeklemeUcreti),
                new ReportColumn("Toplam", widths[13] * scale, x => x.ToplamUcret)
            };
        }

        private static void DrawHeader(Graphics graphics, Font font, Brush headerBrush, Pen linePen, IList<ReportColumn> columns, int left, int y)
        {
            float x = left;
            foreach (var column in columns)
            {
                var rect = new RectangleF(x, y, column.Width, 24);
                graphics.FillRectangle(headerBrush, rect);
                graphics.DrawRectangle(linePen, rect.X, rect.Y, rect.Width, rect.Height);
                graphics.DrawString(column.Header, font, Brushes.Black, rect.X + 3, rect.Y + 5);
                x += column.Width;
            }
        }

        private static void DrawRow(Graphics graphics, Font font, Pen linePen, IList<ReportColumn> columns, int left, int y, DailyRevenueRow row)
        {
            float x = left;
            foreach (var column in columns)
            {
                var rect = new RectangleF(x, y, column.Width, 22);
                graphics.DrawRectangle(linePen, rect.X, rect.Y, rect.Width, rect.Height);
                graphics.DrawString(TrimToFit(column.Read(row), 24), font, Brushes.Black, rect.X + 3, rect.Y + 4);
                x += column.Width;
            }
        }

        private static string TrimToFit(string value, int max)
        {
            value = value ?? "";
            return value.Length <= max ? value : value.Substring(0, max - 1) + ".";
        }

        private sealed class ReportColumn
        {
            public ReportColumn(string header, float width, Func<DailyRevenueRow, string> read)
            {
                Header = header;
                Width = width;
                Read = read;
            }

            public string Header { get; private set; }
            public float Width { get; private set; }
            public Func<DailyRevenueRow, string> Read { get; private set; }
        }
    }
}
