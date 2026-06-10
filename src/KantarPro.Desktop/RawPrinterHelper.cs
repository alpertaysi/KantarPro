using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace KantarPro.Desktop
{
    internal static class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDocName;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pOutputFile;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pDataType;
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern bool OpenPrinter(string szPrinter, out IntPtr phPrinter, IntPtr pd);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern int StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFOA pDocInfo);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, byte[] pBytes, int dwCount, out int dwWritten);

        public static string GetPreferredPrinterName()
        {
            var installed = PrinterSettings.InstalledPrinters
                .Cast<string>()
                .ToList();

            var okiPrinter = installed.FirstOrDefault(x => x.IndexOf("OKI", StringComparison.OrdinalIgnoreCase) >= 0);
            if (!string.IsNullOrWhiteSpace(okiPrinter))
            {
                return okiPrinter;
            }

            var defaultPrinter = new PrinterSettings().PrinterName;
            return string.IsNullOrWhiteSpace(defaultPrinter) ? installed.FirstOrDefault() : defaultPrinter;
        }

        public static void PrintText(string printerName, string text, string documentName, int reverseFeedLines, int topMarginLines, int leftMarginColumns, int tearOffLines, bool formFeed)
        {
            if (string.IsNullOrWhiteSpace(printerName))
            {
                throw new InvalidOperationException("Yazici bulunamadi. Windows'ta OKI yazicinin kurulu oldugunu kontrol edin.");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Yazdırılacak kantar fişi metni boş.");
            }

            IntPtr printerHandle;
            if (!OpenPrinter(printerName, out printerHandle, IntPtr.Zero))
            {
                ThrowWin32("Yazici acilamadi");
            }

            try
            {
                var docInfo = new DOCINFOA
                {
                    pDocName = string.IsNullOrWhiteSpace(documentName) ? "Kantar Fisi" : documentName,
                    pDataType = "RAW"
                };

                if (StartDocPrinter(printerHandle, 1, docInfo) == 0)
                {
                    ThrowWin32("Yazdırma işi başlatılamadı");
                }

                try
                {
                    if (!StartPagePrinter(printerHandle))
                    {
                        ThrowWin32("Yazdırma sayfası başlatılamadı");
                    }

                    try
                    {
                        var bytes = Encoding.GetEncoding(1254).GetBytes(BuildEscpPayload(text, reverseFeedLines, topMarginLines, leftMarginColumns, tearOffLines, formFeed));
                        int written;
                        if (!WritePrinter(printerHandle, bytes, bytes.Length, out written) || written != bytes.Length)
                        {
                            ThrowWin32("Kantar fisi yaziciya gonderilemedi");
                        }
                    }
                    finally
                    {
                        EndPagePrinter(printerHandle);
                    }
                }
                finally
                {
                    EndDocPrinter(printerHandle);
                }
            }
            finally
            {
                ClosePrinter(printerHandle);
            }
        }

        public static void PrintTextWithDriver(string printerName, string text, string documentName, int topMarginLines, int leftMarginColumns, float fontSize)
        {
            if (string.IsNullOrWhiteSpace(printerName))
            {
                throw new InvalidOperationException("Yazici bulunamadi. Windows'ta OKI yazicinin kurulu oldugunu kontrol edin.");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Yazdırılacak kantar fişi metni boş.");
            }

            fontSize = Math.Max(8.0f, Math.Min(16.0f, fontSize));
            using (var document = new PrintDocument())
            using (var font = new Font("Courier New", fontSize, FontStyle.Regular, GraphicsUnit.Point))
            {
                document.DocumentName = string.IsNullOrWhiteSpace(documentName) ? "Kantar Fisi" : documentName;
                document.PrinterSettings.PrinterName = printerName;

                // Continuous form is 5.5 inch high. The width is kept at standard
                // tractor paper width so the driver owns page start/end handling.
                document.DefaultPageSettings.PaperSize = new PaperSize("Kantar Fisi 5.5", 850, 550);
                document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                document.OriginAtMargins = false;

                document.PrintPage += (sender, args) =>
                {
                    var x = Math.Max(0, Math.Min(40, leftMarginColumns)) * 8;
                    var lineHeight = font.GetHeight(args.Graphics);
                    var y = DotMatrixReportLayout.ReceiptTopOffset(lineHeight, topMarginLines);
                    args.Graphics.DrawString(NormalizeLineEndings(text), font, Brushes.Black, (float)x, (float)y);
                    args.HasMorePages = false;
                };

                document.Print();
            }
        }

        public static void PrintContinuousLandscapeTextWithDriver(
            string printerName,
            string text,
            string documentName,
            int topMarginLines,
            int leftMarginColumns,
            float cpi)
        {
            if (string.IsNullOrWhiteSpace(printerName))
            {
                throw new InvalidOperationException("Yazıcı bulunamadı. Windows'ta OKI yazıcının kurulu olduğunu kontrol edin.");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Yazdırılacak döküm metni boş.");
            }

            var lines = NormalizeLineEndings(text)
                .Replace("\r\n", "\n")
                .Split('\n');
            var fontSize = DotMatrixReportLayout.FontPointSizeForCpi(cpi);
            var pageLength = DotMatrixReportLayout.PageLengthHundredthsForLines(lines.Length + topMarginLines);

            using (var document = new PrintDocument())
            using (var font = new Font("Courier New", fontSize, FontStyle.Regular, GraphicsUnit.Point))
            {
                document.DocumentName = string.IsNullOrWhiteSpace(documentName) ? "Kantar Dökümü" : documentName;
                document.PrinterSettings.PrinterName = printerName;

                // Landscape output is 11 inches wide (132 columns at 12 CPI).
                // The feed direction is one long page so 14 cm perforations do not
                // introduce form feeds or blank gaps in report output.
                document.DefaultPageSettings.PaperSize = new PaperSize("Sürekli Yatay Döküm", pageLength, 1100);
                document.DefaultPageSettings.Landscape = true;
                document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                document.OriginAtMargins = false;

                document.PrintPage += (sender, args) =>
                {
                    var x = Math.Max(0, Math.Min(40, leftMarginColumns)) * 8.0f;
                    var lineHeight = font.GetHeight(args.Graphics);
                    var y = Math.Max(0, Math.Min(20, topMarginLines)) * lineHeight;

                    foreach (var line in lines)
                    {
                        args.Graphics.DrawString(line, font, Brushes.Black, x, y);
                        y += lineHeight;
                    }

                    args.HasMorePages = false;
                };

                document.Print();
            }
        }

        public static void PrintA5TextWithDriver(string printerName, string text, string documentName, float fontSize)
        {
            if (string.IsNullOrWhiteSpace(printerName))
            {
                throw new InvalidOperationException("Yazıcı bulunamadı. Windows'ta lazer yazıcının kurulu olduğunu kontrol edin.");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Yazdırılacak kantar fişi metni boş.");
            }

            fontSize = Math.Max(8.0f, Math.Min(14.0f, fontSize));
            using (var document = new PrintDocument())
            using (var font = new Font("Courier New", fontSize, FontStyle.Regular, GraphicsUnit.Point))
            {
                document.DocumentName = string.IsNullOrWhiteSpace(documentName) ? "Kantar Fişi A5" : documentName;
                document.PrinterSettings.PrinterName = printerName;
                document.DefaultPageSettings.PaperSize = new PaperSize("A5", 583, 827);
                document.DefaultPageSettings.Margins = new Margins(35, 35, 35, 35);
                document.OriginAtMargins = true;

                document.PrintPage += (sender, args) =>
                {
                    args.Graphics.DrawString(NormalizeLineEndings(text), font, Brushes.Black, 0, 0);
                    args.HasMorePages = false;
                };

                document.Print();
            }
        }

        private static string NormalizeLineEndings(string text)
        {
            return text
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\n", "\r\n");
        }

        private static string BuildEscpPayload(string text, int reverseFeedLines, int topMarginLines, int leftMarginColumns, int tearOffLines, bool formFeed)
        {
            var normalized = ApplyMargins(NormalizeLineEndings(text), topMarginLines, leftMarginColumns);
            tearOffLines = Math.Max(0, Math.Min(40, tearOffLines));
            if (tearOffLines > 0)
            {
                normalized += new string('\n', tearOffLines);
            }

            if (formFeed && !normalized.EndsWith("\f", StringComparison.Ordinal))
            {
                normalized += "\f";
            }

            return "\x1B@" + BuildReverseFeed(reverseFeedLines) + normalized;
        }

        private static string BuildReverseFeed(int reverseFeedLines)
        {
            reverseFeedLines = Math.Max(0, Math.Min(40, reverseFeedLines));
            if (reverseFeedLines == 0)
            {
                return string.Empty;
            }

            // ESC j n is the ESC/P reverse paper feed command. At 6 LPI one text line
            // is 36/216 inch, so repeating ESC j 36 mirrors one normal line feed.
            var builder = new StringBuilder(reverseFeedLines * 3);
            for (var i = 0; i < reverseFeedLines; i++)
            {
                builder.Append('\x1B');
                builder.Append('j');
                builder.Append((char)36);
            }

            return builder.ToString();
        }

        private static string ApplyMargins(string text, int topMarginLines, int leftMarginColumns)
        {
            topMarginLines = Math.Max(0, Math.Min(20, topMarginLines));
            leftMarginColumns = Math.Max(0, Math.Min(40, leftMarginColumns));

            var topMargin = new string('\n', topMarginLines);
            if (leftMarginColumns == 0)
            {
                return topMargin + text;
            }

            var leftMargin = new string(' ', leftMarginColumns);
            var lines = text
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Split('\n')
                .Select(x => x.Length == 0 ? x : leftMargin + x);

            return NormalizeLineEndings(topMargin + string.Join("\n", lines));
        }

        private static void ThrowWin32(string message)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(message + ". Windows hata kodu: " + error);
        }
    }
}



