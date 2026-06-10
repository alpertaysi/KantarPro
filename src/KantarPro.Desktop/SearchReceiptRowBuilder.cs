using System;
using KantarPro.Application.Services;
using KantarPro.Domain.Entities;

namespace KantarPro.Desktop
{
    public static class SearchReceiptRowBuilder
    {
        public static VehicleMovementRow Build(Islem islem, KantarDosyasi dosya)
        {
            if (islem == null)
            {
                throw new ArgumentNullException(nameof(islem));
            }

            var ilkTartim = dosya != null && dosya.IlkTartim != null
                ? dosya.IlkTartim
                : DashboardVisitInfo.GetIlkTartim(islem);
            var ikinciTartim = dosya != null && dosya.KarsiTartim != null
                ? dosya.KarsiTartim
                : DashboardVisitInfo.GetIkinciTartim(islem);

            return new VehicleMovementRow
            {
                IslemId = islem.IslemId,
                IslemNo = string.IsNullOrWhiteSpace(islem.CikisNo) ? islem.IslemNo : islem.CikisNo,
                KantarFisNo = GetFisNo(ilkTartim, ikinciTartim),
                Plaka = islem.Arac != null ? islem.Arac.Plaka : "",
                FirmaAdi = islem.Arac != null ? islem.Arac.FirmaAdi : "",
                GirisTarihi = FormatDate(ilkTartim != null ? ilkTartim.TartimTarihi : islem.GirisTarihi),
                GirisSaati = FormatTime(ilkTartim != null ? ilkTartim.TartimTarihi : islem.GirisTarihi),
                CikisTarihi = islem.CikisTarihi.HasValue ? FormatDate(islem.CikisTarihi.Value) : "",
                CikisSaati = islem.CikisTarihi.HasValue ? FormatTime(islem.CikisTarihi.Value) : "",
                BosGelisTarihi = ikinciTartim != null ? FormatDate(ikinciTartim.TartimTarihi) : "",
                BosGelisSaati = ikinciTartim != null ? FormatTime(ikinciTartim.TartimTarihi) : "",
                IkinciTartimTarihi = ikinciTartim != null ? FormatDate(ikinciTartim.TartimTarihi) : "",
                IkinciTartimSaati = ikinciTartim != null ? FormatTime(ikinciTartim.TartimTarihi) : "",
                Tartim = DashboardFormat.TartimDegeri(ilkTartim),
                IkinciTartim = DashboardFormat.TartimDegeri(ikinciTartim),
                NetAgirlik = KantarDisplayFormatter.FormatNetAgirlik(
                    false,
                    ilkTartim != null ? (decimal?)ilkTartim.AgirlikKg : null,
                    ikinciTartim != null ? (decimal?)ikinciTartim.AgirlikKg : null)
            };
        }

        private static string GetFisNo(Tartim ilkTartim, Tartim ikinciTartim)
        {
            var hedef = ikinciTartim ?? ilkTartim;
            return hedef != null && !string.IsNullOrWhiteSpace(hedef.KantarFisNo)
                ? hedef.KantarFisNo.Trim()
                : "";
        }

        private static string FormatDate(DateTime value)
        {
            return value.ToString("dd.MM.yyyy");
        }

        private static string FormatTime(DateTime value)
        {
            return value.ToString("HH:mm:ss");
        }
    }
}
