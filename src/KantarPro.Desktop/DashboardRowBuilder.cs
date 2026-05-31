using System;
using System.Linq;
using KantarPro.Application.Services;
using KantarPro.Domain;
using KantarPro.Domain.Entities;
using KantarPro.Infrastructure.Data;

namespace KantarPro.Desktop
{
    internal sealed class DashboardRowBuilder
    {
        private readonly KantarDbContext _context;

        public DashboardRowBuilder(KantarDbContext context)
        {
            _context = context;
        }

        public VehicleMovementRow BuildEntryVehicleRow(Islem islem, KantarDosyasi dosya, DateTime listeHesapTarihi)
        {
            var ilkTartim = dosya != null ? dosya.IlkTartim : DashboardVisitInfo.GetIlkTartim(islem);
            var ikinciTartim = dosya != null ? dosya.KarsiTartim : DashboardVisitInfo.GetIkinciTartim(islem);
            var sonTartim = DashboardVisitInfo.GetSonTartim(islem);
            var beklemeUcreti = HesaplaBeklemeUcreti(islem, listeHesapTarihi);
            var kayitliBeklemeUcreti = SumTahsilEdilmemisUcret(islem, KantarSabitleri.UcretKodu.Bekleme);

            return new VehicleMovementRow
            {
                IslemNo = islem.IslemNo,
                Plaka = islem.Arac.Plaka,
                FirmaAdi = islem.Arac.FirmaAdi,
                GirisTarihi = FormatDoluGelisTarihi(islem, ilkTartim),
                GirisSaati = FormatDoluGelisSaati(islem, ilkTartim),
                DoluCikisTarihi = FormatDoluCikisTarihi(islem, ilkTartim, ikinciTartim),
                DoluCikisSaati = FormatDoluCikisSaati(islem, ilkTartim, ikinciTartim),
                BosGelisTarihi = FormatBosGelisTarihi(ilkTartim, ikinciTartim),
                BosGelisSaati = FormatBosGelisSaati(ilkTartim, ikinciTartim),
                Saat = DashboardFormat.SaatSaniyeli(islem.GirisTarihi),
                CikisTarihi = "",
                CikisSaati = "",
                IlkTartimTarihi = DashboardFormat.TartimTarihi(ilkTartim),
                IlkTartimSaati = DashboardFormat.TartimSaati(ilkTartim),
                IkinciTartimTarihi = DashboardFormat.TartimTarihi(ikinciTartim),
                IkinciTartimSaati = DashboardFormat.TartimSaati(ikinciTartim),
                SonTartimTarihi = DashboardFormat.TartimTarihi(sonTartim),
                SonTartimSaati = DashboardFormat.TartimSaati(sonTartim),
                SonTartim = DashboardFormat.SonTartim(sonTartim),
                Tartim = DashboardFormat.TartimDegeri(ilkTartim),
                IkinciTartim = DashboardFormat.TartimDegeri(ikinciTartim),
                NetAgirlik = FormatNetAgirlik(ilkTartim, ikinciTartim),
                Ucret = FormatKalanBorc(islem, beklemeUcreti - kayitliBeklemeUcreti),
                Tahsilat = DashboardFormat.Para(islem.ToplamTahsilat),
                GirisCikisUcreti = FormatUcretKalemi(islem, KantarSabitleri.UcretKodu.GirisCikis, true),
                TartimUcreti = FormatUcretKalemi(islem, KantarSabitleri.UcretKodu.Tartim, true),
                BeklemeUcreti = DashboardFormat.Para(beklemeUcreti),
                Durum = DashboardVisitInfo.GetVisitRowDurum(islem, dosya),
                KesinCikisMi = false
            };
        }

        public VehicleMovementRow TryBuildExitVehicleRow(Islem islem, KantarDosyasi dosya)
        {
            var ilkTartim = dosya != null ? dosya.IlkTartim : DashboardVisitInfo.GetIlkTartim(islem);
            var ikinciTartim = dosya != null ? dosya.KarsiTartim : DashboardVisitInfo.GetIkinciTartim(islem);
            if (!DashboardVisitInfo.ShouldShowInFinalExitList(islem, dosya, ilkTartim, ikinciTartim))
            {
                return null;
            }

            return new VehicleMovementRow
            {
                IslemNo = islem.IslemNo,
                Plaka = islem.Arac.Plaka,
                FirmaAdi = islem.Arac.FirmaAdi,
                GirisTarihi = FormatDoluGelisTarihi(islem, ilkTartim),
                GirisSaati = FormatDoluGelisSaati(islem, ilkTartim),
                DoluCikisTarihi = FormatDoluCikisTarihi(islem, ilkTartim, ikinciTartim),
                DoluCikisSaati = FormatDoluCikisSaati(islem, ilkTartim, ikinciTartim),
                BosGelisTarihi = FormatBosGelisTarihi(ilkTartim, ikinciTartim),
                BosGelisSaati = FormatBosGelisSaati(ilkTartim, ikinciTartim),
                CikisTarihi = islem.CikisTarihi.HasValue ? islem.CikisTarihi.Value.ToString("dd.MM.yyyy") : "",
                CikisSaati = islem.CikisTarihi.HasValue ? islem.CikisTarihi.Value.ToString("HH:mm:ss") : "",
                Saat = islem.CikisTarihi.HasValue ? islem.CikisTarihi.Value.ToString("HH:mm:ss") : "",
                IlkTartimTarihi = DashboardFormat.TartimTarihi(ilkTartim),
                IlkTartimSaati = DashboardFormat.TartimSaati(ilkTartim),
                IkinciTartimTarihi = DashboardFormat.TartimTarihi(ikinciTartim),
                IkinciTartimSaati = DashboardFormat.TartimSaati(ikinciTartim),
                Tartim = DashboardFormat.TartimDegeri(ilkTartim),
                IkinciTartim = DashboardFormat.TartimDegeri(ikinciTartim),
                NetAgirlik = FormatNetAgirlik(islem, ilkTartim, ikinciTartim),
                Ucret = DashboardFormat.Para(islem.ToplamTahakkuk),
                Tahsilat = DashboardFormat.Para(islem.ToplamTahsilat),
                GirisCikisUcreti = FormatUcretKalemi(islem, KantarSabitleri.UcretKodu.GirisCikis),
                TartimUcreti = FormatUcretKalemi(islem, KantarSabitleri.UcretKodu.Tartim),
                BeklemeUcreti = FormatUcretKalemi(islem, KantarSabitleri.UcretKodu.Bekleme),
                Durum = dosya != null ? DashboardVisitInfo.GetVisitRowDurum(islem, dosya) : "Kesin cikis",
                KesinCikisMi = true
            };
        }

        private static string FormatDoluGelisTarihi(Islem islem, Tartim ilkTartim)
        {
            return (ilkTartim != null ? ilkTartim.TartimTarihi : islem.GirisTarihi).ToString("dd.MM.yyyy");
        }

        private static string FormatDoluGelisSaati(Islem islem, Tartim ilkTartim)
        {
            return DashboardFormat.SaatSaniyeli(ilkTartim != null ? ilkTartim.TartimTarihi : islem.GirisTarihi);
        }

        private static string FormatDoluCikisTarihi(Islem islem, Tartim ilkTartim, Tartim ikinciTartim)
        {
            var tarih = GetDoluCikisTarihi(islem, ilkTartim, ikinciTartim);
            return tarih.HasValue ? tarih.Value.ToString("dd.MM.yyyy") : "";
        }

        private static string FormatDoluCikisSaati(Islem islem, Tartim ilkTartim, Tartim ikinciTartim)
        {
            var tarih = GetDoluCikisTarihi(islem, ilkTartim, ikinciTartim);
            return tarih.HasValue ? tarih.Value.ToString("HH:mm:ss") : "";
        }

        private static string FormatBosGelisTarihi(Tartim ilkTartim, Tartim ikinciTartim)
        {
            var islem = GetIkinciZiyaretIslemi(ilkTartim, ikinciTartim);
            return islem != null ? islem.GirisTarihi.ToString("dd.MM.yyyy") : "";
        }

        private static string FormatBosGelisSaati(Tartim ilkTartim, Tartim ikinciTartim)
        {
            var islem = GetIkinciZiyaretIslemi(ilkTartim, ikinciTartim);
            return islem != null ? islem.GirisTarihi.ToString("HH:mm:ss") : "";
        }

        private static Islem GetIkinciZiyaretIslemi(Tartim ilkTartim, Tartim ikinciTartim)
        {
            if (ilkTartim == null || ikinciTartim == null || ikinciTartim.Islem == null)
            {
                return null;
            }

            return ilkTartim.IslemId != ikinciTartim.IslemId ? ikinciTartim.Islem : null;
        }

        private static DateTime? GetDoluCikisTarihi(Islem islem, Tartim ilkTartim, Tartim ikinciTartim)
        {
            if (ilkTartim != null &&
                ikinciTartim != null &&
                ilkTartim.IslemId == ikinciTartim.IslemId)
            {
                return islem.CikisTarihi;
            }

            var ilkTahsilat = islem.Ucretler
                .Where(x => x.TahsilTarihi.HasValue)
                .Select(x => x.TahsilTarihi)
                .OrderBy(x => x)
                .FirstOrDefault();

            return ilkTahsilat ?? islem.CikisTarihi;
        }

        private static string FormatNetAgirlik(Tartim ilkTartim, Tartim ikinciTartim)
        {
            return KantarDisplayFormatter.FormatNetAgirlik(false, ilkTartim != null ? (decimal?)ilkTartim.AgirlikKg : null, ikinciTartim != null ? (decimal?)ikinciTartim.AgirlikKg : null);
        }

        private static string FormatNetAgirlik(Islem islem, Tartim ilkTartim, Tartim ikinciTartim)
        {
            var tartimsizCikis = islem != null &&
                islem.GelisTuru == KantarSabitleri.GelisTuru.Tartimsiz &&
                ilkTartim == null &&
                ikinciTartim == null;

            return KantarDisplayFormatter.FormatNetAgirlik(tartimsizCikis, ilkTartim != null ? (decimal?)ilkTartim.AgirlikKg : null, ikinciTartim != null ? (decimal?)ikinciTartim.AgirlikKg : null);
        }

        private static string FormatUcretKalemi(Islem islem, string ucretKodu)
        {
            return FormatUcretKalemi(islem, ucretKodu, false);
        }

        private static string FormatUcretKalemi(Islem islem, string ucretKodu, bool sadeceTahsilEdilmemis)
        {
            var ucretler = islem.Ucretler
                .Where(x => x.Ucret != null && x.Ucret.UcretKodu == ucretKodu);

            if (sadeceTahsilEdilmemis)
            {
                ucretler = ucretler.Where(x => !x.TahsilEdildiMi);
            }

            return DashboardFormat.Para(ucretler.Sum(x => x.Tutar));
        }

        private decimal HesaplaBeklemeUcreti(Islem islem, DateTime hesapTarihi)
        {
            var kayitliBekleme = SumTahsilEdilmemisUcret(islem, KantarSabitleri.UcretKodu.Bekleme);

            var beklemeGunSayisi = SahaZiyaretiServisi.HesaplaBeklemeGunSayisi(islem.GirisTarihi, hesapTarihi);
            if (beklemeGunSayisi <= 0)
            {
                return kayitliBekleme;
            }

            var aktifBekleme = _context.Ucretler
                .Where(x => x.UcretKodu == KantarSabitleri.UcretKodu.Bekleme && x.AktifMi && x.Yil == hesapTarihi.Year)
                .OrderByDescending(x => x.GecerlilikBaslangic)
                .FirstOrDefault();

            return kayitliBekleme + (beklemeGunSayisi * (aktifBekleme != null ? aktifBekleme.Tutar : 0m));
        }

        private static decimal SumTahsilEdilmemisUcret(Islem islem, string ucretKodu)
        {
            return islem.Ucretler
                .Where(x => x.Ucret != null && x.Ucret.UcretKodu == ucretKodu && !x.TahsilEdildiMi)
                .Sum(x => x.Tutar);
        }

        private static string FormatKalanBorc(Islem islem, decimal ekBeklemeUcreti)
        {
            var kalan = islem.ToplamTahakkuk - islem.ToplamTahsilat;
            kalan += ekBeklemeUcreti;
            return DashboardFormat.Para(kalan > 0 ? kalan : 0m);
        }
    }
}
