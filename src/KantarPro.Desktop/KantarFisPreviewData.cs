using System;

namespace KantarPro.Desktop
{
    public class KantarFisPreviewData
    {
        public string FisTipi { get; private set; }
        public string Plaka { get; private set; }
        public string Firma { get; private set; }
        public string FisNo { get; private set; }
        public string GirisTarihi { get; private set; }
        public string GirisSaati { get; private set; }
        public string IkinciGirisTarihi { get; private set; }
        public string IkinciGirisSaati { get; private set; }
        public string BirinciTartim { get; private set; }
        public string IkinciTartim { get; private set; }
        public string Net { get; private set; }
        public string RawText { get; private set; }
        public string SurekliFormNotu { get; private set; }

        public bool DoluBosMu
        {
            get { return string.Equals(FisTipi, "Dolu-Bos", StringComparison.OrdinalIgnoreCase); }
        }

        public static KantarFisPreviewData FromVehicleRow(VehicleMovementRow row, string rawText)
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            var doluBos = HasValue(row.IkinciTartim) && HasValue(row.NetAgirlik);
            return new KantarFisPreviewData
            {
                FisTipi = doluBos ? "Dolu-Bos" : "Tek Tartim",
                Plaka = Safe(row.Plaka),
                Firma = Safe(row.FirmaAdi),
                FisNo = FormatFisNo(row),
                GirisTarihi = Safe(row.GirisTarihi),
                GirisSaati = Safe(row.GirisSaati),
                IkinciGirisTarihi = FirstNonEmpty(row.BosGelisTarihi, row.IkinciTartimTarihi),
                IkinciGirisSaati = FirstNonEmpty(row.BosGelisSaati, row.IkinciTartimSaati),
                BirinciTartim = Safe(row.Tartim),
                IkinciTartim = Safe(row.IkinciTartim),
                Net = Safe(row.NetAgirlik),
                RawText = rawText ?? "",
                SurekliFormNotu = "OKI 5720 surekli form icin ham metin sabit genislikli uretilir."
            };
        }

        public static KantarFisPreviewData FromPendingRow(PendingWeighingPrototypeRow row, string rawText)
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            return new KantarFisPreviewData
            {
                FisTipi = "Tek Tartim",
                Plaka = Safe(row.Plaka),
                Firma = Safe(row.FirmaAdi),
                FisNo = FirstNonEmpty(row.KantarFisNo, row.IslemNo),
                GirisTarihi = FirstNonEmpty(row.IlkTartimTarihi, row.IlkGirisTarihi),
                GirisSaati = FirstNonEmpty(row.IlkTartimSaati, row.IlkGirisSaati),
                IkinciGirisTarihi = "",
                IkinciGirisSaati = "",
                BirinciTartim = Safe(row.IlkAgirlik),
                IkinciTartim = "",
                Net = "",
                RawText = rawText ?? "",
                SurekliFormNotu = "OKI 5720 surekli form icin ham metin sabit genislikli uretilir."
            };
        }

        public static string FormatFisNo(VehicleMovementRow row)
        {
            if (row == null)
            {
                return "-";
            }

            if (!string.IsNullOrWhiteSpace(row.KantarFisNo))
            {
                return row.KantarFisNo.Trim();
            }

            if (IsFourDigitNumber(row.IslemNo))
            {
                return row.IslemNo.Trim();
            }

            if (row.IslemId > 0)
            {
                return row.IslemId.ToString("0000");
            }

            return string.IsNullOrWhiteSpace(row.IslemNo) ? "-" : row.IslemNo.Trim();
        }

        private static string FirstNonEmpty(string first, string second)
        {
            return HasValue(first) ? first.Trim() : Safe(second);
        }

        private static bool HasValue(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                value.IndexOf("tartimsiz", StringComparison.OrdinalIgnoreCase) < 0 &&
                value.IndexOf("tartim yok", StringComparison.OrdinalIgnoreCase) < 0 &&
                value.Trim() != "-";
        }

        private static bool IsFourDigitNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            value = value.Trim();
            if (value.Length != 4)
            {
                return false;
            }

            int parsed;
            return int.TryParse(value, out parsed);
        }

        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
        }
    }
}
