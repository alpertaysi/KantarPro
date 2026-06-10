using System;

namespace KantarPro.Desktop
{
    public static class DotMatrixReportLayout
    {
        public static float FontPointSizeForCpi(float cpi)
        {
            cpi = Math.Max(8.0f, Math.Min(20.0f, cpi));
            return 120.0f / cpi;
        }

        public static int PageLengthHundredthsForLines(int lineCount)
        {
            lineCount = Math.Max(1, lineCount);
            return Math.Max(1100, (int)Math.Ceiling(lineCount / 6.0 * 100.0) + 50);
        }

        public static float ReceiptTopOffset(float lineHeight, int topMarginLines)
        {
            lineHeight = Math.Max(0.0f, lineHeight);
            topMarginLines = Math.Max(-1, Math.Min(20, topMarginLines));
            return topMarginLines * lineHeight;
        }
    }
}
