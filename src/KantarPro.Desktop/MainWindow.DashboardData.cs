using System.Windows;
using System;
using System.Linq;
using KantarPro.Application.Services;
using KantarPro.Domain;
using KantarPro.Domain.Entities;
using KantarPro.Infrastructure.Data;
using System.Data.Entity;

namespace KantarPro.Desktop
{
    public partial class MainWindow : Window
    {
        private void LoadDashboardData()
        {
            try
            {
                using (var context = new KantarDbContext())
                {
                    var sahaServisi = new SahaZiyaretiServisi(new KantarUnitOfWork(context));
                    sahaServisi.SuresiDolanKantarDosyalariniKapat(DateTime.Today, 10);

                    var bugun = DateTime.Today;
                    var yarin = bugun.AddDays(1);
                    var listeHesapTarihi = GetListeHesapTarihi();

                    LoadEntryVehicleRows(context, listeHesapTarihi);
                    LoadPendingWeighingRows(context);
                    LoadExitVehicleRows(context);
                    EntryVehiclesView.Refresh();
                    ExitVehiclesView.Refresh();
                    LoadDailyTransactionRows(context, bugun, yarin);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ana ekran verileri okunamadi: " + ex.Message, "Kantar Pro", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadEntryVehicleRows(KantarDbContext context, DateTime listeHesapTarihi)
        {
            var girisler = context.Islemler
                .Include(x => x.Arac)
                .Include(x => x.Tartimlar)
                .Include(x => x.Ucretler.Select(u => u.Ucret))
                .Where(x => x.Durum == KantarSabitleri.IslemDurumu.Iceride)
                .OrderByDescending(x => x.GirisTarihi)
                .Take(100)
                .ToList();

            EntryVehicles.Clear();
            foreach (var islem in girisler)
            {
                EntryVehicles.Add(BuildEntryVehicleRow(context, islem, listeHesapTarihi));
            }
        }

        private VehicleMovementRow BuildEntryVehicleRow(KantarDbContext context, Islem islem, DateTime listeHesapTarihi)
        {
            var dosya = GetKantarDosyasiForIslem(context, islem);
            var ilkTartim = dosya != null ? dosya.IlkTartim : GetIlkTartim(islem);
            var ikinciTartim = dosya != null ? dosya.KarsiTartim : GetIkinciTartim(islem);
            var sonTartim = GetSonTartim(islem);
            var beklemeUcreti = HesaplaBeklemeUcreti(context, islem, listeHesapTarihi);
            var kayitliBeklemeUcreti = SumTahsilEdilmemisUcret(islem, KantarSabitleri.UcretKodu.Bekleme);

            return new VehicleMovementRow
            {
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
                Durum = GetVisitRowDurum(islem, dosya),
                KesinCikisMi = false
            };
        }

        private void LoadPendingWeighingRows(KantarDbContext context)
        {
            var bekleyenKantarDosyalari = context.KantarDosyalari
                .Include(x => x.Arac)
                .Include(x => x.IlkTartim)
                .Include(x => x.IlkTartim.Islem)
                .Where(x => x.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor)
                .OrderByDescending(x => x.OlusturmaTarihi)
                .Take(100)
                .ToList();

            PendingWeighings.Clear();
            foreach (var dosya in bekleyenKantarDosyalari)
            {
                var row = TryBuildPendingWeighingRow(context, dosya);
                if (row != null)
                {
                    PendingWeighings.Add(row);
                }
            }
        }

        private PendingWeighingPrototypeRow TryBuildPendingWeighingRow(KantarDbContext context, KantarDosyasi dosya)
        {
            var islem = dosya.IlkTartim != null ? dosya.IlkTartim.Islem : null;
            if (dosya.IlkTartim == null ||
                islem == null ||
                islem.Durum != KantarSabitleri.IslemDurumu.CikisYapti)
            {
                return null;
            }

            var acikDonusVarMi = context.Islemler.Any(x =>
                x.AracId == dosya.AracId &&
                x.Durum == KantarSabitleri.IslemDurumu.Iceride);
            if (acikDonusVarMi)
            {
                return null;
            }

            return new PendingWeighingPrototypeRow
            {
                Plaka = dosya.Arac.Plaka,
                FirmaAdi = dosya.Arac.FirmaAdi,
                IlkGirisTarihi = islem.GirisTarihi.ToString("dd.MM.yyyy"),
                IlkGirisSaati = islem.GirisTarihi.ToString("HH:mm:ss"),
                IlkCikisTarihi = islem.CikisTarihi.HasValue ? islem.CikisTarihi.Value.ToString("dd.MM.yyyy") : "",
                IlkCikisSaati = islem.CikisTarihi.HasValue ? islem.CikisTarihi.Value.ToString("HH:mm:ss") : "",
                IlkTartimTarihi = dosya.IlkTartim.TartimTarihi.ToString("dd.MM.yyyy"),
                IlkTartimSaati = dosya.IlkTartim.TartimTarihi.ToString("HH:mm:ss"),
                IlkAgirlik = dosya.IlkTartim.AgirlikKg.ToString("N0"),
                YukDurumu = dosya.IlkTartim.YukDurumu,
                Aciklama = GetBeklenenTartimDurumu(dosya.IlkTartim)
            };
        }

        private void LoadExitVehicleRows(KantarDbContext context)
        {
            var cikislar = context.Islemler
                .Include(x => x.Arac)
                .Include(x => x.Tartimlar)
                .Include(x => x.Ucretler.Select(u => u.Ucret))
                .Where(x => x.Durum == KantarSabitleri.IslemDurumu.CikisYapti)
                .OrderByDescending(x => x.Tartimlar
                    .Where(t => t.TartimTipi == KantarSabitleri.TartimTipi.Sonradan || t.TartimTipi == KantarSabitleri.TartimTipi.Cikis)
                    .Select(t => (DateTime?)t.TartimTarihi)
                    .Max() ?? x.CikisTarihi)
                .Take(100)
                .ToList();

            ExitVehicles.Clear();
            var finalExitRows = new System.Collections.Generic.List<Tuple<DateTime, VehicleMovementRow>>();
            foreach (var islem in cikislar)
            {
                var cikisSatiri = TryBuildExitVehicleRow(context, islem);
                if (cikisSatiri != null)
                {
                    finalExitRows.Add(Tuple.Create(islem.CikisTarihi ?? islem.GirisTarihi, cikisSatiri));
                }
            }

            foreach (var row in finalExitRows.OrderByDescending(x => x.Item1).Select(x => x.Item2))
            {
                ExitVehicles.Add(row);
            }
        }

        private VehicleMovementRow TryBuildExitVehicleRow(KantarDbContext context, Islem islem)
        {
            var dosya = GetKantarDosyasiForIslem(context, islem);
            var ilkTartim = dosya != null ? dosya.IlkTartim : GetIlkTartim(islem);
            var ikinciTartim = dosya != null ? dosya.KarsiTartim : GetIkinciTartim(islem);
            if (!AltCikisListesindeGoster(islem, dosya, ilkTartim, ikinciTartim))
            {
                return null;
            }

            return new VehicleMovementRow
            {
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
                Durum = dosya != null ? GetVisitRowDurum(islem, dosya) : "Kesin cikis",
                KesinCikisMi = true
            };
        }

        private void LoadDailyTransactionRows(KantarDbContext context, DateTime bugun, DateTime yarin)
        {
            var gunluk = context.Islemler
                .Include(x => x.Arac)
                .Where(x => (x.GirisTarihi >= bugun && x.GirisTarihi < yarin) || (x.CikisTarihi >= bugun && x.CikisTarihi < yarin))
                .OrderByDescending(x => x.GirisTarihi)
                .Take(100)
                .ToList();

            DailyTransactions.Clear();
            foreach (var islem in gunluk)
            {
                DailyTransactions.Add(new DailyTransactionRow
                {
                    IslemNo = islem.IslemNo,
                    Plaka = islem.Arac.Plaka,
                    Tip = islem.Durum == KantarSabitleri.IslemDurumu.Iceride ? "Giris" : "Cikis",
                    Ucret = DashboardFormat.Para(islem.ToplamTahakkuk),
                    Kullanici = "Admin"
                });
            }
        }
    }
}
