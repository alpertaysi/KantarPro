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
        private void LoadDashboardData(bool showErrorMessage = true)
        {
            try
            {
                using (var context = KantarDbContextFactory.Create())
                {
                    var sahaServisi = new SahaZiyaretiServisi(new KantarUnitOfWork(context));
                    sahaServisi.SuresiDolanKantarDosyalariniKapat(DateTime.Today, 7);

                    var bugun = DateTime.Today;
                    var yarin = bugun.AddDays(1);
                    var listeHesapTarihi = GetListeHesapTarihi();

                    LoadEntryVehicleRows(context, listeHesapTarihi);
                    LoadPendingWeighingRows(context);
                    LoadExitVehicleRows(context);
                    EntryVehiclesView.Refresh();
                    PendingWeighingsView.Refresh();
                    ExitVehiclesView.Refresh();
                    LoadDailyTransactionRows(context, bugun, yarin);
                }
            }
            catch (Exception ex)
            {
                if (showErrorMessage)
                {
                    MessageBox.Show("Ana ekran verileri okunamadı: " + ex.Message, "Kantar Pro", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                throw;
            }
        }

        private void LoadEntryVehicleRows(KantarDbContext context, DateTime listeHesapTarihi)
        {
            var girisler = context.Islemler
                .Include(x => x.Arac)
                .Include(x => x.Tartimlar)
                .Include(x => x.Ucretler.Select(u => u.Ucret))
                .Where(x => !x.SilindiMi && x.Durum == KantarSabitleri.IslemDurumu.Iceride)
                .OrderByDescending(x => x.GirisTarihi)
                .Take(100)
                .ToList();

            EntryVehicles.Clear();
            var rowBuilder = new DashboardRowBuilder(context);
            foreach (var islem in girisler)
            {
                var dosya = GetKantarDosyasiForIslem(context, islem);
                EntryVehicles.Add(rowBuilder.BuildEntryVehicleRow(islem, dosya, listeHesapTarihi));
            }
        }

        private void LoadPendingWeighingRows(KantarDbContext context)
        {
            var bekleyenKantarDosyalari = context.KantarDosyalari
                .Include(x => x.Arac)
                .Include(x => x.IlkTartim)
                .Include(x => x.IlkTartim.Islem)
                .Where(x => x.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor && x.IlkTartim.Islem != null && !x.IlkTartim.Islem.SilindiMi)
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

            var dosyaPlaka = NormalizePlaka(dosya.Arac != null ? dosya.Arac.Plaka : null);
            var acikDonusVarMi = context.Islemler.Any(x =>
                x.Durum == KantarSabitleri.IslemDurumu.Iceride &&
                !x.SilindiMi &&
                (x.AracId == dosya.AracId || x.Arac.Plaka == dosyaPlaka));
            if (acikDonusVarMi)
            {
                return null;
            }

            return new PendingWeighingPrototypeRow
            {
                IslemNo = string.IsNullOrWhiteSpace(islem.CikisNo) ? islem.IslemNo : islem.CikisNo,
                KantarFisNo = dosya.IlkTartim.KantarFisNo,
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
                Aciklama = DashboardVisitInfo.GetBeklenenTartimDurumu(dosya.IlkTartim)
            };
        }

        private void LoadExitVehicleRows(KantarDbContext context)
        {
            var cikislar = context.Islemler
                .Include(x => x.Arac)
                .Include(x => x.Tartimlar)
                .Include(x => x.Ucretler.Select(u => u.Ucret))
                .Where(x => !x.SilindiMi && x.Durum == KantarSabitleri.IslemDurumu.CikisYapti)
                .OrderByDescending(x => x.Tartimlar
                    .Where(t => t.TartimTipi == KantarSabitleri.TartimTipi.Sonradan || t.TartimTipi == KantarSabitleri.TartimTipi.Cikis)
                    .Select(t => (DateTime?)t.TartimTarihi)
                    .Max() ?? x.CikisTarihi)
                .Take(100)
                .ToList();

            ExitVehicles.Clear();
            var rowBuilder = new DashboardRowBuilder(context);
            var finalExitRows = new System.Collections.Generic.List<Tuple<DateTime, VehicleMovementRow>>();
            foreach (var islem in cikislar)
            {
                var dosya = GetKantarDosyasiForIslem(context, islem);
                var cikisSatiri = rowBuilder.TryBuildExitVehicleRow(islem, dosya);
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

        private void LoadDailyTransactionRows(KantarDbContext context, DateTime bugun, DateTime yarin)
        {
            var gunluk = context.Islemler
                .Include(x => x.Arac)
                .Where(x => !x.SilindiMi && ((x.GirisTarihi >= bugun && x.GirisTarihi < yarin) || (x.CikisTarihi >= bugun && x.CikisTarihi < yarin)))
                .OrderByDescending(x => x.GirisTarihi)
                .Take(100)
                .ToList();

            DailyTransactions.Clear();
            foreach (var islem in gunluk)
            {
                DailyTransactions.Add(new DailyTransactionRow
                {
                    IslemNo = string.IsNullOrWhiteSpace(islem.CikisNo) ? islem.IslemNo : islem.CikisNo,
                    Plaka = islem.Arac.Plaka,
                    Tip = islem.Durum == KantarSabitleri.IslemDurumu.Iceride ? "Giris" : "Cikis",
                    Ucret = DashboardFormat.Para(islem.ToplamTahakkuk),
                    Kullanici = "Admin"
                });
            }
        }
    }
}




