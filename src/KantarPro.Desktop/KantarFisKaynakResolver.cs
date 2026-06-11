using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KantarPro.Domain;
using KantarPro.Domain.Entities;

namespace KantarPro.Desktop
{
    public static class KantarFisKaynakResolver
    {
        private const string TartimsizFisUyarisi =
            "Bu kayıtta kantar tartımı yok. Tartımsız girişler için kantar fişi oluşturulmaz.";

        public static VehicleMovementRow Resolve(
            VehicleMovementRow selectedRow,
            IEnumerable<KantarDosyasi> kantarDosyalari,
            string canonicalPlaka = null)
        {
            if (selectedRow == null)
            {
                throw new ArgumentNullException(nameof(selectedRow));
            }

            if (SatirinGercekTartimiVar(selectedRow))
            {
                return selectedRow;
            }

            var normalizedPlaka = NormalizePlaka(
                string.IsNullOrWhiteSpace(canonicalPlaka)
                    ? selectedRow.Plaka
                    : canonicalPlaka);
            var adaylar = (kantarDosyalari ?? Enumerable.Empty<KantarDosyasi>())
                .Where(x => AdayUygunMu(x, normalizedPlaka))
                .ToList();

            if (adaylar.Count == 0)
            {
                throw new InvalidOperationException(TartimsizFisUyarisi);
            }

            if (adaylar.Count > 1)
            {
                throw new InvalidOperationException(
                    "Bu plaka için birden fazla aktif kantar tartımı bulundu. Kantar fişi kaynağı belirlenemedi.");
            }

            return IlkTartimSatiriOlustur(adaylar[0]);
        }

        public static IQueryable<KantarDosyasi> AktifAdaylariFiltrele(
            IQueryable<KantarDosyasi> source,
            string canonicalPlaka)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var normalizedPlaka = NormalizePlaka(canonicalPlaka);
            return source.Where(x =>
                x.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor &&
                x.Arac != null &&
                x.Arac.Plaka != null &&
                x.Arac.Plaka.Trim().Replace(" ", "").ToUpper() == normalizedPlaka);
        }

        private static bool AdayUygunMu(KantarDosyasi dosya, string normalizedPlaka)
        {
            if (dosya == null ||
                dosya.Durum != KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor ||
                dosya.IlkTartim == null ||
                dosya.IlkTartim.Islem == null ||
                dosya.IlkTartim.Islem.SilindiMi)
            {
                return false;
            }

            var arac = GetArac(dosya);
            return arac != null &&
                NormalizePlaka(arac.Plaka) == normalizedPlaka;
        }

        private static VehicleMovementRow IlkTartimSatiriOlustur(KantarDosyasi dosya)
        {
            var ilkTartim = dosya.IlkTartim;
            var islem = ilkTartim.Islem;
            var arac = GetArac(dosya);
            if (string.IsNullOrWhiteSpace(ilkTartim.KantarFisNo))
            {
                throw new InvalidOperationException(
                    "İlk tartımın kantar fiş numarası bulunamadı. Eski tartıma yeni numara verilmedi.");
            }

            return new VehicleMovementRow
            {
                IslemId = islem.IslemId,
                IslemNo = islem.IslemNo,
                KantarFisNo = ilkTartim.KantarFisNo,
                Plaka = arac.Plaka,
                FirmaAdi = arac.FirmaAdi,
                GirisTarihi = ilkTartim.TartimTarihi.ToString("dd.MM.yyyy"),
                GirisSaati = ilkTartim.TartimTarihi.ToString("HH:mm:ss"),
                IlkTartimTarihi = ilkTartim.TartimTarihi.ToString("dd.MM.yyyy"),
                IlkTartimSaati = ilkTartim.TartimTarihi.ToString("HH:mm:ss"),
                Tartim = ilkTartim.AgirlikKg.ToString(
                    "N0",
                    CultureInfo.GetCultureInfo("tr-TR")) + " kg"
            };
        }

        private static Arac GetArac(KantarDosyasi dosya)
        {
            return dosya.Arac ?? dosya.IlkTartim.Arac ?? dosya.IlkTartim.Islem.Arac;
        }

        public static bool SatirinGercekTartimiVar(VehicleMovementRow row)
        {
            return GercekTartimDegeri(row.Tartim) ||
                GercekTartimDegeri(row.IkinciTartim) ||
                GercekTartimDegeri(row.SonTartim);
        }

        private static bool GercekTartimDegeri(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var numericValue = value.Trim()
                .ToLowerInvariant()
                .Replace("kg", string.Empty)
                .Trim();
            decimal parsed;
            return
                (decimal.TryParse(
                    numericValue,
                    NumberStyles.Number,
                    CultureInfo.GetCultureInfo("tr-TR"),
                    out parsed) &&
                 parsed > 0m) ||
                (decimal.TryParse(
                    numericValue,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out parsed) &&
                 parsed > 0m);
        }

        private static string NormalizePlaka(string plaka)
        {
            return (plaka ?? string.Empty)
                .Trim()
                .ToUpperInvariant()
                .Replace(" ", string.Empty);
        }
    }
}
