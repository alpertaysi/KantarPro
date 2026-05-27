using System.Linq;
using KantarPro.Domain;
using KantarPro.Domain.Entities;

namespace KantarPro.Desktop
{
    internal static class DashboardVisitInfo
    {
        public static string GetVisitRowDurum(Islem islem, KantarDosyasi dosya)
        {
            if (dosya == null)
            {
                return islem.GelisTuru == KantarSabitleri.GelisTuru.Tartimsiz ? "Tartimsiz cikis bekliyor" : "Cikis bekliyor";
            }

            if (dosya.Durum == KantarSabitleri.KantarDosyasiDurumu.Tamamlandi)
            {
                return "Dolu-bos tamamlandi";
            }

            if (islem != null &&
                islem.Durum == KantarSabitleri.IslemDurumu.Iceride &&
                dosya.IlkTartim != null &&
                dosya.IlkTartim.IslemId == islem.IslemId &&
                dosya.KarsiTartim == null)
            {
                return dosya.IlkTartim.YukDurumu == KantarSabitleri.YukDurumu.Bos
                    ? "Bos Tartim Yapildi"
                    : "Dolu Tartim Yapildi";
            }

            return GetBeklenenTartimDurumu(dosya.IlkTartim);
        }

        public static string GetBeklenenTartimDurumu(Tartim ilkTartim)
        {
            return ilkTartim != null && ilkTartim.YukDurumu == KantarSabitleri.YukDurumu.Bos
                ? "Dolu bekleniyor"
                : "Bos bekleniyor";
        }

        public static Tartim GetIlkTartim(Islem islem)
        {
            return islem.Tartimlar
                .Where(x => x.TartimTipi == KantarSabitleri.TartimTipi.Giris)
                .OrderBy(x => x.TartimTarihi)
                .FirstOrDefault();
        }

        public static Tartim GetIkinciTartim(Islem islem)
        {
            var sonradanTartim = islem.Tartimlar
                .Where(x => x.TartimTipi == KantarSabitleri.TartimTipi.Sonradan)
                .OrderByDescending(x => x.TartimTarihi)
                .FirstOrDefault();

            if (sonradanTartim != null)
            {
                return sonradanTartim;
            }

            return islem.Tartimlar
                .Where(x => x.TartimTipi == KantarSabitleri.TartimTipi.Cikis)
                .OrderByDescending(x => x.TartimTarihi)
                .FirstOrDefault();
        }

        public static Tartim GetRevenueSecondWeighingForVisit(Islem islem, KantarDosyasi dosya)
        {
            if (islem == null)
            {
                return null;
            }

            if (dosya != null && dosya.KarsiTartim != null && dosya.KarsiTartim.IslemId == islem.IslemId)
            {
                return dosya.KarsiTartim;
            }

            if (dosya != null && dosya.IlkTartim != null && dosya.IlkTartim.IslemId == islem.IslemId)
            {
                return null;
            }

            return GetIkinciTartim(islem);
        }

        public static Tartim GetSonTartim(Islem islem)
        {
            return islem.Tartimlar
                .OrderByDescending(x => x.TartimTarihi)
                .FirstOrDefault();
        }

        public static bool ShouldShowInFinalExitList(Islem islem, KantarDosyasi dosya, Tartim ilkTartim, Tartim ikinciTartim)
        {
            if (islem != null &&
                dosya != null &&
                dosya.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor &&
                dosya.IlkTartim != null &&
                dosya.IlkTartim.IslemId == islem.IslemId)
            {
                return false;
            }

            if (islem != null &&
                dosya != null &&
                dosya.Durum == KantarSabitleri.KantarDosyasiDurumu.SuresiDoldu &&
                dosya.IlkTartim != null &&
                dosya.IlkTartim.IslemId == islem.IslemId)
            {
                return true;
            }

            if (islem != null && islem.GelisTuru == KantarSabitleri.GelisTuru.Tartimsiz && dosya == null)
            {
                return true;
            }

            if (dosya != null && dosya.Durum == KantarSabitleri.KantarDosyasiDurumu.Tamamlandi)
            {
                return dosya.KarsiTartim != null && dosya.KarsiTartim.IslemId == islem.IslemId;
            }

            return ilkTartim == null && ikinciTartim == null;
        }
    }
}
