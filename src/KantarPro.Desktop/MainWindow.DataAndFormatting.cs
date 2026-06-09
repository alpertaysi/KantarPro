using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Windows.Controls;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using KantarPro.Application.Services;
using KantarPro.Domain;
using KantarPro.Domain.Entities;
using KantarPro.Infrastructure.Data;
using System.Data.Entity;
using Microsoft.Win32;

namespace KantarPro.Desktop
{
    public partial class MainWindow : Window
    {
        private static int EnsureAdminUser(KantarDbContext context)
        {
            var admin = context.Kullanicilar.FirstOrDefault(x => x.KullaniciAdi == "admin");
            if (admin != null)
            {
                return admin.KullaniciId;
            }

            admin = new Kullanici
            {
                KullaniciAdi = "admin",
                ParolaHash = "DEVELOPMENT_PLACEHOLDER_HASH",
                AdSoyad = "Admin Kullanici",
                Rol = "Admin",
                AktifMi = true
            };
            context.Kullanicilar.Add(admin);
            context.SaveChanges();
            return admin.KullaniciId;
        }


        private void RevenueListButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDailyRevenueData();
        }

        private void RevenueExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var rows = DailyRevenueView.Cast<DailyRevenueRow>().ToList();
                if (rows.Count == 0)
                {
                    MessageBox.Show("PDF oluşturmak için önce tahsilat listesini doldurun.", "Günlük Tahsilat", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var baslangic = RevenueStartDatePicker.SelectedDate.GetValueOrDefault(DateTime.Today).ToString("dd.MM.yyyy");
                var bitis = RevenueEndDatePicker.SelectedDate.GetValueOrDefault(DateTime.Today).ToString("dd.MM.yyyy");
                var dialog = new SaveFileDialog
                {
                    Title = "Gunluk tahsilat PDF dosyasi",
                    Filter = "PDF dosyasi (*.pdf)|*.pdf",
                    FileName = "GunlukTahsilat_" + DateTime.Today.ToString("yyyyMMdd") + ".pdf",
                    AddExtension = true,
                    DefaultExt = ".pdf"
                };

                if (dialog.ShowDialog(this) != true)
                {
                    return;
                }

                var exporter = new DailyRevenuePdfExporter(
                    rows,
                    "Günlük Tahsilat Dokumu (" + baslangic + " - " + bitis + ")",
                    RevenueEntryExitTotalText.Text,
                    RevenueWeighingTotalText.Text,
                    RevenueWaitingTotalText.Text,
                    RevenueGrandTotalText.Text);

                exporter.Export(dialog.FileName);
                MessageBox.Show("Günlük tahsilat PDF dosyası oluşturuldu.", "Günlük Tahsilat");
            }
            catch (Exception ex)
            {
                MessageBox.Show("PDF oluşturulamadı: " + ex.Message, "Günlük Tahsilat", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RevenueExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentUser == null || !_currentUser.AdminMi)
                {
                    MessageBox.Show("Excel dışa aktarma yetkisi sadece admin kullanıcılara açıktır.", "Günlük Tahsilat", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var rows = DailyRevenueView.Cast<DailyRevenueRow>().ToList();
                if (rows.Count == 0)
                {
                    MessageBox.Show("Excel oluşturmak için önce tahsilat listesini doldurun.", "Günlük Tahsilat", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Title = "Günlük tahsilat Excel dosyası",
                    Filter = "Excel CSV dosyası (*.csv)|*.csv",
                    FileName = "GunlukTahsilat_" + DateTime.Today.ToString("yyyyMMdd") + ".csv",
                    AddExtension = true,
                    DefaultExt = ".csv"
                };

                if (dialog.ShowDialog(this) != true)
                {
                    return;
                }

                DailyRevenueExcelExporter.Export(dialog.FileName, rows);
                MessageBox.Show("Günlük tahsilat Excel dosyası oluşturuldu.", "Günlük Tahsilat");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Excel oluşturulamadı: " + ex.Message, "Günlük Tahsilat", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RevenuePrintOkiButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var rows = DailyRevenueView.Cast<DailyRevenueRow>().ToList();
                if (rows.Count == 0)
                {
                    MessageBox.Show("Yazdırmak için önce tahsilat listesini doldurun.", "Günlük Tahsilat", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var baslangic = RevenueStartDatePicker.SelectedDate.GetValueOrDefault(DateTime.Today).ToString("dd.MM.yyyy");
                var bitis = RevenueEndDatePicker.SelectedDate.GetValueOrDefault(DateTime.Today).ToString("dd.MM.yyyy");
                var rawText = DailyRevenueTextFormatter.Build(
                    rows,
                    baslangic + " - " + bitis,
                    RevenueEntryExitTotalText.Text,
                    RevenueWeighingTotalText.Text,
                    RevenueWaitingTotalText.Text,
                    RevenueGrandTotalText.Text);

                RawPrinterHelper.PrintTextWithDriver(
                    RawPrinterHelper.GetPreferredPrinterName(),
                    rawText,
                    "Gunluk Tahsilat " + DateTime.Today.ToString("yyyyMMdd"),
                    topMarginLines: 0,
                    leftMarginColumns: 0,
                    fontSize: 9.0f);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Günlük tahsilat dökümü yazdırılamadı: " + ex.Message, "Günlük Tahsilat", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadDailyRevenueData()
        {
            try
            {
                var baslangic = RevenueStartDatePicker.SelectedDate.GetValueOrDefault(DateTime.Today).Date;
                var bitis = RevenueEndDatePicker.SelectedDate.GetValueOrDefault(baslangic).Date;
                if (bitis < baslangic)
                {
                    throw new InvalidOperationException("Bitiş tarihi başlangıç tarihinden önce olamaz.");
                }

                var bitisExclusive = bitis.AddDays(1);
                using (var context = KantarDbContextFactory.Create())
                {
                    var tahsilatlar = context.IslemUcretleri
                        .Include(x => x.Ucret)
                        .Include(x => x.Islem.Arac)
                        .Include(x => x.Islem.Tartimlar)
                        .Where(x =>
                            x.TahsilEdildiMi &&
                            !x.Islem.SilindiMi &&
                            x.Islem.CikisTarihi.HasValue &&
                            x.Islem.CikisTarihi.Value >= baslangic &&
                            x.Islem.CikisTarihi.Value < bitisExclusive)
                        .ToList()
                        .GroupBy(x => new
                        {
                            x.IslemId,
                            TahsilatNo = string.IsNullOrWhiteSpace(x.TahsilatNo) ? x.FaturaId : x.TahsilatNo,
                            TahsilTarihi = x.TahsilTarihi.Value,
                            OdemeTuru = string.IsNullOrWhiteSpace(x.OdemeTuru) ? KantarSabitleri.OdemeTuru.Nakit : x.OdemeTuru
                        })
                        .OrderBy(x => x.Key.TahsilTarihi)
                        .ThenBy(x => x.Key.TahsilatNo)
                        .ToList();

                    DailyRevenueRows.Clear();
                    decimal girisToplam = 0m;
                    decimal tartimToplam = 0m;
                    decimal beklemeToplam = 0m;
                    decimal genelToplam = 0m;

                    var siraNo = 1;
                    foreach (var tahsilat in tahsilatlar)
                    {
                        var islem = tahsilat.First().Islem;
                        var dosya = GetKantarDosyasiForIslem(context, islem);
                        var ilkTartim = dosya != null ? dosya.IlkTartim : DashboardVisitInfo.GetIlkTartim(islem);
                        var ikinciTartim = DashboardVisitInfo.GetRevenueSecondWeighingForVisit(islem, dosya);
                        var girisCikis = SumFee(tahsilat, KantarSabitleri.UcretKodu.GirisCikis);
                        var tartim = SumFee(tahsilat, KantarSabitleri.UcretKodu.Tartim);
                        var bekleme = SumFee(tahsilat, KantarSabitleri.UcretKodu.Bekleme);
                        var toplam = tahsilat.Sum(x => x.Tutar);

                        girisToplam += girisCikis;
                        tartimToplam += tartim;
                        beklemeToplam += bekleme;
                        genelToplam += toplam;

                        DailyRevenueRows.Add(new DailyRevenueRow
                        {
                            IslemId = islem.IslemId,
                            SiraNo = siraNo++,
                            IslemNo = !string.IsNullOrWhiteSpace(islem.CikisNo) ? islem.CikisNo : (string.IsNullOrWhiteSpace(tahsilat.Key.TahsilatNo) ? islem.IslemNo : tahsilat.Key.TahsilatNo),
                            IslemTipi = FormatRevenueIslemTipi(islem, ilkTartim, ikinciTartim),
                            KantarFisNo = FormatRevenueKantarFisNo(ilkTartim, ikinciTartim),
                            OdemeTuru = islem.MuafMi ? "Muaf" : tahsilat.Key.OdemeTuru,
                            MuafiyetNedeni = islem.MuafMi ? islem.MuafiyetNedeni : "",
                            FirmaAdi = islem.MuafMi ? islem.MuafiyetNedeni : islem.Arac.FirmaAdi,
                            Plaka = islem.Arac.Plaka,
                            GirisTarihi = islem.GirisTarihi.ToString("dd.MM.yyyy"),
                            GirisSaati = islem.GirisTarihi.ToString("HH:mm:ss"),
                            CikisTarihi = islem.CikisTarihi.HasValue ? islem.CikisTarihi.Value.ToString("dd.MM.yyyy") : "",
                            CikisSaati = islem.CikisTarihi.HasValue ? islem.CikisTarihi.Value.ToString("HH:mm:ss") : "",
                            IlkTartim = DashboardFormat.TartimDegeri(ilkTartim),
                            IkinciTartim = DashboardFormat.TartimDegeri(ikinciTartim),
                            NetAgirlik = FormatNetAgirlik(ilkTartim, ikinciTartim),
                            GirisCikisUcreti = DashboardFormat.Para(girisCikis),
                            TartimUcreti = DashboardFormat.Para(tartim),
                            BeklemeUcreti = DashboardFormat.Para(bekleme),
                            ToplamUcret = DashboardFormat.Para(toplam)
                        });
                    }
 
                    var muafIslemler = context.Islemler
                        .Include(x => x.Arac)
                        .Include(x => x.Tartimlar)
                        .Where(x =>
                            x.MuafMi &&
                            !x.SilindiMi &&
                            x.CikisTarihi.HasValue &&
                            x.CikisTarihi.Value >= baslangic &&
                            x.CikisTarihi.Value < bitisExclusive)
                        .OrderBy(x => x.CikisTarihi)
                        .ToList();
 
                    foreach (var islem in muafIslemler)
                    {
                        var dosya = GetKantarDosyasiForIslem(context, islem);
                        var ilkTartim = dosya != null ? dosya.IlkTartim : DashboardVisitInfo.GetIlkTartim(islem);
                        var ikinciTartim = DashboardVisitInfo.GetRevenueSecondWeighingForVisit(islem, dosya);
 
                        DailyRevenueRows.Add(new DailyRevenueRow
                        {
                            IslemId = islem.IslemId,
                            SiraNo = siraNo++,
                            IslemNo = string.IsNullOrWhiteSpace(islem.CikisNo) ? islem.IslemNo : islem.CikisNo,
                            IslemTipi = FormatRevenueIslemTipi(islem, ilkTartim, ikinciTartim),
                            KantarFisNo = FormatRevenueKantarFisNo(ilkTartim, ikinciTartim),
                            OdemeTuru = "Muaf",
                            MuafiyetNedeni = islem.MuafiyetNedeni,
                            FirmaAdi = islem.MuafiyetNedeni,
                            Plaka = islem.Arac.Plaka,
                            GirisTarihi = islem.GirisTarihi.ToString("dd.MM.yyyy"),
                            GirisSaati = islem.GirisTarihi.ToString("HH:mm:ss"),
                            CikisTarihi = islem.CikisTarihi.HasValue ? islem.CikisTarihi.Value.ToString("dd.MM.yyyy") : "",
                            CikisSaati = islem.CikisTarihi.HasValue ? islem.CikisTarihi.Value.ToString("HH:mm:ss") : "",
                            IlkTartim = DashboardFormat.TartimDegeri(ilkTartim),
                            IkinciTartim = DashboardFormat.TartimDegeri(ikinciTartim),
                            NetAgirlik = FormatNetAgirlik(ilkTartim, ikinciTartim),
                            GirisCikisUcreti = DashboardFormat.Para(0m),
                            TartimUcreti = DashboardFormat.Para(0m),
                            BeklemeUcreti = DashboardFormat.Para(0m),
                            ToplamUcret = DashboardFormat.Para(0m)
                        });
                    }

                    RevenueRowCountText.Text = "Kayıt: " + DailyRevenueRows.Count;
                    RevenueEntryExitTotalText.Text = DashboardFormat.Para(girisToplam);
                    RevenueWeighingTotalText.Text = DashboardFormat.Para(tartimToplam);
                    RevenueWaitingTotalText.Text = DashboardFormat.Para(beklemeToplam);
                    RevenueGrandTotalText.Text = DashboardFormat.Para(genelToplam);
                    DailyRevenueView.Refresh();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Günlük hasılat okunamadı: " + ex.Message, "Kantar Pro", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static string FormatRevenueIslemTipi(Islem islem, Tartim ilkTartim, Tartim ikinciTartim)
        {
            if (ilkTartim == null && ikinciTartim == null)
            {
                return "Tartimsiz";
            }

            if (islem != null && islem.MuafMi)
            {
                return "Muaf Tartim";
            }

            return ikinciTartim != null ? "Dolu-Boş" : "Tek Tartım";
        }

        private static string FormatRevenueKantarFisNo(Tartim ilkTartim, Tartim ikinciTartim)
        {
            var fisNolari = new[] { ilkTartim, ikinciTartim }
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.KantarFisNo))
                .Select(x => x.KantarFisNo.Trim())
                .Distinct()
                .ToList();

            return fisNolari.Count == 0 ? "-" : string.Join(", ", fisNolari);
        }

        private void ShowVehicleMovementDetail(VehicleMovementRow row)
        {
            if (row == null || SelectedVehicleDetailTextBox == null)
            {
                return;
            }

            SelectedVehicleDetailTextBox.Text = BuildVehicleMovementDetail(row);
        }

        private static string BuildVehicleMovementDetail(VehicleMovementRow row)
        {
            var detay = TryBuildKantarDosyasiDetail(row);
            if (!string.IsNullOrWhiteSpace(detay))
            {
                return detay;
            }

            var tartimsiz = KantarDisplayFormatter.IsTartimsizMovement(row.Durum, row.Tartim, row.NetAgirlik);
            if (tartimsiz)
            {
                return
                    "Plaka: " + row.Plaka + Environment.NewLine +
                    "Firma: " + (string.IsNullOrWhiteSpace(row.FirmaAdi) ? "-" : row.FirmaAdi) + Environment.NewLine +
                    "Durum: " + row.Durum + Environment.NewLine +
                    "Açıklama: " + DashboardFormat.BosDeger(GetIslemNotlari(row.IslemId)) + Environment.NewLine + Environment.NewLine +
                    "Saha Hareketleri" + Environment.NewLine +
                    "Giriş: " + DashboardFormat.BosDeger(row.GirisTarihi + " " + row.GirisSaati) + Environment.NewLine +
                    "Cikis: " + DashboardFormat.BosDeger(row.CikisTarihi + " " + row.CikisSaati) + Environment.NewLine + Environment.NewLine +
                    "Ücret Dökümü" + Environment.NewLine +
                    "Giris-Cikis: " + DashboardFormat.BosDeger(row.GirisCikisUcreti) + Environment.NewLine +
                    "Tartim: " + DashboardFormat.BosDeger(row.TartimUcreti) + Environment.NewLine +
                    "Bekleme: " + DashboardFormat.BosDeger(row.BeklemeUcreti) + Environment.NewLine +
                    "Toplam: " + DashboardFormat.BosDeger(row.Ucret) + Environment.NewLine +
                    "Tahsilat: " + DashboardFormat.BosDeger(row.Tahsilat) + Environment.NewLine +
                    "Tahsilat No: " + DashboardFormat.BosDeger(GetIslemCikisNo(row.IslemId)) +
                    GetPaymentHistoryText(row.Plaka);
            }

            return
                "Plaka: " + row.Plaka + Environment.NewLine +
                "Firma: " + (string.IsNullOrWhiteSpace(row.FirmaAdi) ? "-" : row.FirmaAdi) + Environment.NewLine +
                "Durum: " + row.Durum + Environment.NewLine + Environment.NewLine +
                "Dolu Hareket" + Environment.NewLine +
                "Gelis: " + DashboardFormat.BosDeger(row.GirisTarihi + " " + row.GirisSaati) + Environment.NewLine +
                "Açıklama: " + DashboardFormat.BosDeger(GetIslemNotlari(row.IslemId)) + Environment.NewLine +
                "Tartim: " + DashboardFormat.BosDeger(row.IlkTartimTarihi + " " + row.IlkTartimSaati) + " | " + DashboardFormat.BosDeger(row.Tartim) + Environment.NewLine +
                "Cikis: " + DashboardFormat.BosDeger(row.DoluCikisTarihi + " " + row.DoluCikisSaati) + Environment.NewLine + Environment.NewLine +
                "Bos Hareket" + Environment.NewLine +
                "Gelis: " + DashboardFormat.BosDeger(row.IkinciTartimTarihi + " " + row.IkinciTartimSaati) + Environment.NewLine +
                "Tartim: " + DashboardFormat.BosDeger(row.IkinciTartim) + Environment.NewLine +
                "Cikis: " + DashboardFormat.BosDeger(row.CikisTarihi + " " + row.CikisSaati) + Environment.NewLine +
                "Net: " + DashboardFormat.BosDeger(row.NetAgirlik) + Environment.NewLine + Environment.NewLine +
                "Ücret Dökümü" + Environment.NewLine +
                "Giris-Cikis: " + DashboardFormat.BosDeger(row.GirisCikisUcreti) + Environment.NewLine +
                "Tartim: " + DashboardFormat.BosDeger(row.TartimUcreti) + Environment.NewLine +
                "Bekleme: " + DashboardFormat.BosDeger(row.BeklemeUcreti) + Environment.NewLine +
                "Toplam: " + DashboardFormat.BosDeger(row.Ucret) + Environment.NewLine +
                "Tahsilat: " + DashboardFormat.BosDeger(row.Tahsilat) +
                GetPaymentHistoryText(row.Plaka);
        }

        private static string BuildPendingWeighingDetail(PendingWeighingPrototypeRow row)
        {
            var normalized = NormalizePlaka(row.Plaka);
            using (var context = KantarDbContextFactory.Create())
            {
                var dosya = context.KantarDosyalari
                    .Include(x => x.Arac)
                    .Include(x => x.IlkTartim.Islem.Ucretler.Select(u => u.Ucret))
                    .Include(x => x.KarsiTartim.Islem.Ucretler.Select(u => u.Ucret))
                    .Where(x => x.Arac.Plaka == normalized && x.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor)
                    .ToList()
                    .FirstOrDefault(x =>
                        MatchesRowTartim(x.IlkTartim, row.IlkTartimTarihi, row.IlkTartimSaati, row.IlkAgirlik) ||
                        (x.IlkTartim != null && x.IlkTartim.Islem != null &&
                            string.Equals(x.IlkTartim.Islem.GirisTarihi.ToString("dd.MM.yyyy"), row.IlkGirisTarihi, StringComparison.Ordinal) &&
                            string.Equals(x.IlkTartim.Islem.GirisTarihi.ToString("HH:mm:ss"), row.IlkGirisSaati, StringComparison.Ordinal)));

                if (dosya == null)
                {
                    return
                        "Plaka: " + row.Plaka + Environment.NewLine +
                        "Firma: " + DashboardFormat.BosDeger(row.FirmaAdi) + Environment.NewLine +
                        "Durum: " + DashboardFormat.BosDeger(row.Aciklama) + Environment.NewLine +
                        "Ilk Tartim: " + DashboardFormat.BosDeger(row.IlkTartimTarihi + " " + row.IlkTartimSaati) + " | " + DashboardFormat.BosDeger(row.IlkAgirlik + " kg");
                }

                var ilkIslem = dosya.IlkTartim != null ? dosya.IlkTartim.Islem : null;
                return
                    "Plaka: " + dosya.Arac.Plaka + Environment.NewLine +
                    "Firma: " + DashboardFormat.BosDeger(dosya.Arac.FirmaAdi) + Environment.NewLine +
                    "Durum: " + DashboardVisitInfo.GetBeklenenTartimDurumu(dosya.IlkTartim) + Environment.NewLine +
                    "Net: " + DashboardFormat.BosDeger(dosya.NetAgirlikKg.HasValue ? dosya.NetAgirlikKg.Value.ToString("N0") + " kg" : "") + Environment.NewLine + Environment.NewLine +
                    "Ilk Ziyaret" + Environment.NewLine +
                    FormatVisitBlock(ilkIslem, dosya.IlkTartim) + Environment.NewLine + Environment.NewLine +
                    "Ödeme Geçmişi (Tahsilat No | Tarih | Tutar | Ödeme Türü)" + Environment.NewLine +
                    FormatKantarDosyasiPaymentHistory(ilkIslem, null);
            }
        }

        private static string TryBuildKantarDosyasiDetail(VehicleMovementRow row)
        {
            var normalized = NormalizePlaka(row.Plaka);
            using (var context = KantarDbContextFactory.Create())
            {
                var dosyalar = context.KantarDosyalari
                    .Include(x => x.Arac)
                    .Include(x => x.IlkTartim.Islem.Ucretler.Select(u => u.Ucret))
                    .Include(x => x.KarsiTartim.Islem.Ucretler.Select(u => u.Ucret))
                    .Where(x => x.Arac.Plaka == normalized)
                    .OrderByDescending(x => x.TamamlanmaTarihi ?? x.OlusturmaTarihi)
                    .Take(20)
                    .ToList();

                var dosya = dosyalar.FirstOrDefault(x =>
                    MatchesRowTartim(x.IlkTartim, row.IlkTartimTarihi, row.IlkTartimSaati, row.Tartim) ||
                    (x.IlkTartim != null && x.IlkTartim.Islem != null &&
                        x.IlkTartim.Islem.CikisTarihi.HasValue &&
                        string.Equals(x.IlkTartim.Islem.CikisTarihi.Value.ToString("dd.MM.yyyy"), row.CikisTarihi, StringComparison.Ordinal) &&
                        string.Equals(x.IlkTartim.Islem.CikisTarihi.Value.ToString("HH:mm:ss"), row.CikisSaati, StringComparison.Ordinal)) ||
                    MatchesRowTartim(x.KarsiTartim, row.IkinciTartimTarihi, row.IkinciTartimSaati, row.IkinciTartim) ||
                    MatchesRowTartim(x.KarsiTartim, row.SonTartimTarihi, row.SonTartimSaati, row.SonTartim));

                if (dosya == null)
                {
                    dosya = dosyalar.FirstOrDefault();
                }

                if (dosya == null)
                {
                    return null;
                }

                var ilkIslem = dosya.IlkTartim != null ? dosya.IlkTartim.Islem : null;
                var ikinciIslem = dosya.KarsiTartim != null ? dosya.KarsiTartim.Islem : null;
                var tekZiyaretteTamamlandi =
                    ilkIslem != null &&
                    ikinciIslem != null &&
                    ilkIslem.IslemId == ikinciIslem.IslemId;

                if (tekZiyaretteTamamlandi)
                {
                    return
                        "Plaka: " + dosya.Arac.Plaka + Environment.NewLine +
                        "Firma: " + DashboardFormat.BosDeger(dosya.Arac.FirmaAdi) + Environment.NewLine +
                        "Durum: " + DashboardVisitInfo.GetVisitRowDurum(ilkIslem, dosya) + Environment.NewLine +
                        "Net: " + DashboardFormat.BosDeger(dosya.NetAgirlikKg.HasValue ? dosya.NetAgirlikKg.Value.ToString("N0") + " kg" : "") + Environment.NewLine + Environment.NewLine +
                        "Saha Ziyareti" + Environment.NewLine +
                        FormatSingleVisitDoluBosBlock(ilkIslem, dosya.IlkTartim, dosya.KarsiTartim) + Environment.NewLine + Environment.NewLine +
                        "Ödeme Geçmişi (Tahsilat No | Tarih | Tutar | Ödeme Türü)" + Environment.NewLine +
                        FormatKantarDosyasiPaymentHistory(ilkIslem, null);
                }

                return
                    "Plaka: " + dosya.Arac.Plaka + Environment.NewLine +
                    "Firma: " + DashboardFormat.BosDeger(dosya.Arac.FirmaAdi) + Environment.NewLine +
                    "Durum: " + DashboardVisitInfo.GetVisitRowDurum(ikinciIslem ?? ilkIslem, dosya) + Environment.NewLine +
                    "Net: " + DashboardFormat.BosDeger(dosya.NetAgirlikKg.HasValue ? dosya.NetAgirlikKg.Value.ToString("N0") + " kg" : "") + Environment.NewLine + Environment.NewLine +
                    "Ilk Ziyaret" + Environment.NewLine +
                    FormatVisitBlock(ilkIslem, dosya.IlkTartim) + Environment.NewLine + Environment.NewLine +
                    "Ikinci Ziyaret" + Environment.NewLine +
                    FormatVisitBlock(ikinciIslem, dosya.KarsiTartim) + Environment.NewLine + Environment.NewLine +
                    "Ödeme Geçmişi (Tahsilat No | Tarih | Tutar | Ödeme Türü)" + Environment.NewLine +
                    FormatKantarDosyasiPaymentHistory(ilkIslem, ikinciIslem);
            }
        }

        private static string GetPaymentHistoryText(string plaka)
        {
            var normalized = NormalizePlaka(plaka);
            using (var context = KantarDbContextFactory.Create())
            {
                var tahsilatlar = context.IslemUcretleri
                    .Include(x => x.Islem.Arac)
                    .Where(x => x.Islem.Arac.Plaka == normalized && x.TahsilEdildiMi && x.TahsilTarihi.HasValue)
                    .ToList()
                    .GroupBy(x => new
                    {
                        TahsilatNo = GetDisplayTahsilatNo(x),
                        TahsilTarihi = x.TahsilTarihi.Value,
                        OdemeTuru = string.IsNullOrWhiteSpace(x.OdemeTuru) ? KantarSabitleri.OdemeTuru.Nakit : x.OdemeTuru
                    })
                    .OrderByDescending(x => x.Key.TahsilTarihi)
                    .Take(10)
                    .ToList();

                if (tahsilatlar.Count == 0)
                {
                    return Environment.NewLine + Environment.NewLine + "Ödeme Geçmişi" + Environment.NewLine + "-";
                }

                var satirlar = tahsilatlar.Select(x =>
                    x.Key.TahsilatNo + " | " +
                    x.Key.TahsilTarihi.ToString("dd.MM.yyyy HH:mm:ss") + " | " +
                    DashboardFormat.Para(x.Sum(u => u.Tutar)) + " | " +
                    x.Key.OdemeTuru);

                return Environment.NewLine + Environment.NewLine +
                    "Ödeme Geçmişi (Tahsilat No | Tarih | Tutar | Ödeme Türü)" + Environment.NewLine +
                    string.Join(Environment.NewLine, satirlar);
            }
        }

        private static string FormatVisitBlock(Islem islem, Tartim tartim)
        {
            if (islem == null && tartim == null)
            {
                return "-";
            }

            return
                "Gelis: " + DashboardFormat.BosDeger(islem != null ? islem.GirisTarihi.ToString("dd.MM.yyyy HH:mm:ss") : "") + Environment.NewLine +
                "Cikis: " + DashboardFormat.BosDeger(islem != null && islem.CikisTarihi.HasValue ? islem.CikisTarihi.Value.ToString("dd.MM.yyyy HH:mm:ss") : "") + Environment.NewLine +
                "Tartim: " + DashboardFormat.BosDeger(tartim != null ? tartim.TartimTarihi.ToString("dd.MM.yyyy HH:mm:ss") + " | " + tartim.AgirlikKg.ToString("N0") + " kg" : "") + Environment.NewLine +
                "Açıklama: " + DashboardFormat.BosDeger(islem != null ? islem.Notlar : "") + Environment.NewLine +
                "Muafiyet: " + DashboardFormat.BosDeger(islem != null && islem.MuafMi ? islem.MuafiyetNedeni : "") + Environment.NewLine +
                "Giris-Cikis: " + DashboardFormat.BosDeger(islem != null ? FormatUcretKalemi(islem, KantarSabitleri.UcretKodu.GirisCikis) : "") + Environment.NewLine +
                "Tartim: " + DashboardFormat.BosDeger(islem != null ? FormatUcretKalemi(islem, KantarSabitleri.UcretKodu.Tartim) : "") + Environment.NewLine +
                "Bekleme: " + DashboardFormat.BosDeger(islem != null ? FormatUcretKalemi(islem, KantarSabitleri.UcretKodu.Bekleme) : "") + Environment.NewLine +
                "Tahsilat: " + DashboardFormat.BosDeger(islem != null ? DashboardFormat.Para(islem.ToplamTahsilat) : "") + Environment.NewLine +
                "Tahsilat No: " + DashboardFormat.BosDeger(GetLastTahsilatNo(islem));
        }

        private static string FormatSingleVisitDoluBosBlock(Islem islem, Tartim ilkTartim, Tartim ikinciTartim)
        {
            if (islem == null)
            {
                return "-";
            }

            return
                "Gelis: " + islem.GirisTarihi.ToString("dd.MM.yyyy HH:mm:ss") + Environment.NewLine +
                "Cikis: " + DashboardFormat.BosDeger(islem.CikisTarihi.HasValue ? islem.CikisTarihi.Value.ToString("dd.MM.yyyy HH:mm:ss") : "") + Environment.NewLine +
                "1. Tartım: " + DashboardFormat.BosDeger(ilkTartim != null ? ilkTartim.TartimTarihi.ToString("dd.MM.yyyy HH:mm:ss") + " | " + ilkTartim.AgirlikKg.ToString("N0") + " kg" : "") + Environment.NewLine +
                "2. Tartım: " + DashboardFormat.BosDeger(ikinciTartim != null ? ikinciTartim.TartimTarihi.ToString("dd.MM.yyyy HH:mm:ss") + " | " + ikinciTartim.AgirlikKg.ToString("N0") + " kg" : "") + Environment.NewLine +
                "Açıklama: " + DashboardFormat.BosDeger(islem.Notlar) + Environment.NewLine +
                "Muafiyet: " + DashboardFormat.BosDeger(islem.MuafMi ? islem.MuafiyetNedeni : "") + Environment.NewLine +
                "Giris-Cikis: " + DashboardFormat.BosDeger(FormatUcretKalemi(islem, KantarSabitleri.UcretKodu.GirisCikis)) + Environment.NewLine +
                "Tartim: " + DashboardFormat.BosDeger(FormatUcretKalemi(islem, KantarSabitleri.UcretKodu.Tartim)) + Environment.NewLine +
                "Bekleme: " + DashboardFormat.BosDeger(FormatUcretKalemi(islem, KantarSabitleri.UcretKodu.Bekleme)) + Environment.NewLine +
                "Tahsilat: " + DashboardFormat.BosDeger(DashboardFormat.Para(islem.ToplamTahsilat)) + Environment.NewLine +
                "Tahsilat No: " + DashboardFormat.BosDeger(GetLastTahsilatNo(islem));
        }

        private static string FormatKantarDosyasiPaymentHistory(Islem ilkIslem, Islem ikinciIslem)
        {
            var ucretler = new[] { ilkIslem, ikinciIslem }
                .Where(x => x != null)
                .GroupBy(x => x.IslemId)
                .Select(x => x.First())
                .SelectMany(x => x.Ucretler)
                .Where(x => x.TahsilEdildiMi && x.TahsilTarihi.HasValue)
                .GroupBy(x => new
                {
                    TahsilatNo = GetDisplayTahsilatNo(x),
                    TahsilTarihi = x.TahsilTarihi.Value,
                    OdemeTuru = string.IsNullOrWhiteSpace(x.OdemeTuru) ? KantarSabitleri.OdemeTuru.Nakit : x.OdemeTuru
                })
                .OrderBy(x => x.Key.TahsilTarihi)
                .ToList();

            if (ucretler.Count == 0)
            {
                return "-";
            }

            return string.Join(Environment.NewLine, ucretler.Select(x =>
                x.Key.TahsilatNo + " | " +
                x.Key.TahsilTarihi.ToString("dd.MM.yyyy HH:mm:ss") + " | " +
                DashboardFormat.Para(x.Sum(u => u.Tutar)) + " | " +
                x.Key.OdemeTuru));
        }

        private static string GetLastTahsilatNo(Islem islem)
        {
            if (islem == null)
            {
                return "";
            }

            if (!string.IsNullOrWhiteSpace(islem.CikisNo))
            {
                return islem.CikisNo;
            }

            var tahsilatNo = islem.Ucretler
                .Where(x => x.TahsilEdildiMi && !string.IsNullOrWhiteSpace(x.TahsilatNo))
                .OrderByDescending(x => x.TahsilTarihi)
                .Select(x => x.TahsilatNo)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(tahsilatNo))
            {
                return tahsilatNo;
            }

            var faturaId = islem.Ucretler
                .Where(x => x.TahsilEdildiMi && !string.IsNullOrWhiteSpace(x.FaturaId))
                .OrderByDescending(x => x.TahsilTarihi)
                .Select(x => x.FaturaId)
                .FirstOrDefault();

            return faturaId ?? "";
        }

        private static string GetIslemNotlari(int islemId)
        {
            if (islemId <= 0)
            {
                return "";
            }

            using (var context = KantarDbContextFactory.Create())
            {
                return context.Islemler
                    .Where(x => x.IslemId == islemId)
                    .Select(x => x.Notlar)
                    .FirstOrDefault() ?? "";
            }
        }

        private static string GetIslemCikisNo(int islemId)
        {
            if (islemId <= 0)
            {
                return "";
            }

            using (var context = KantarDbContextFactory.Create())
            {
                return context.Islemler
                    .Where(x => x.IslemId == islemId)
                    .Select(x => x.CikisNo)
                    .FirstOrDefault() ?? "";
            }
        }

        private static string GetDisplayTahsilatNo(IslemUcreti ucret)
        {
            if (!string.IsNullOrWhiteSpace(ucret.TahsilatNo))
            {
                return ucret.TahsilatNo;
            }

            if (!string.IsNullOrWhiteSpace(ucret.FaturaId))
            {
                return ucret.FaturaId;
            }

            return "Eski kayit";
        }

        private static bool MatchesRowTartim(Tartim tartim, string tarih, string saat, string agirlik)
        {
            if (tartim == null)
            {
                return false;
            }

            var rowDate = (tarih + " " + saat).Trim();
            var tartimDate = tartim.TartimTarihi.ToString("dd.MM.yyyy HH:mm:ss");
            var tartimWeight = tartim.AgirlikKg.ToString("N0") + " kg";

            return string.Equals(rowDate, tartimDate, StringComparison.Ordinal) ||
                string.Equals(DashboardFormat.BosDeger(agirlik), tartimWeight, StringComparison.Ordinal);
        }

        private static string FormatTartim(Islem islem)
        {
            var tartim = islem.Tartimlar
                .Where(x => x.TartimTipi == KantarSabitleri.TartimTipi.Giris)
                .OrderByDescending(x => x.TartimTarihi)
                .FirstOrDefault();

            if (tartim == null)
            {
                tartim = islem.Tartimlar
                    .Where(x => x.TartimTipi == KantarSabitleri.TartimTipi.Sonradan)
                    .OrderByDescending(x => x.TartimTarihi)
                    .FirstOrDefault();
            }

            if (tartim == null)
            {
                return "Tartimi Yok";
            }

            return tartim.AgirlikKg.ToString("N0") + " kg";
        }

        private static string FormatCikisListesiTartim(Islem islem)
        {
            var girisTartimi = islem.Tartimlar
                .Where(x => x.TartimTipi == KantarSabitleri.TartimTipi.Giris)
                .OrderByDescending(x => x.TartimTarihi)
                .FirstOrDefault();

            if (girisTartimi != null)
            {
                return "Giriş: " + girisTartimi.AgirlikKg.ToString("N0") + " kg";
            }

            var sonradanTartim = islem.Tartimlar
                .Where(x => x.TartimTipi == KantarSabitleri.TartimTipi.Sonradan)
                .OrderByDescending(x => x.TartimTarihi)
                .FirstOrDefault();

            if (sonradanTartim != null)
            {
                return "Dolu-Boş: " + sonradanTartim.AgirlikKg.ToString("N0") + " kg";
            }

            return "Tartimi Yok";
        }

        private static string FormatIkinciTartim(Islem islem)
        {
            var ikinciTartim = DashboardVisitInfo.GetIkinciTartim(islem);
            if (ikinciTartim == null)
            {
                return "";
            }

            return ikinciTartim.AgirlikKg.ToString("N0") + " kg";
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

        private static string FormatNetAgirlik(Islem islem)
        {
            var ilkTartim = DashboardVisitInfo.GetIlkTartim(islem);
            var ikinciTartim = DashboardVisitInfo.GetIkinciTartim(islem);
            return FormatNetAgirlik(islem, ilkTartim, ikinciTartim);
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

        private static KantarDosyasi GetKantarDosyasiForIslem(KantarDbContext context, Islem islem)
        {
            return context.KantarDosyalari
                .Include(x => x.IlkTartim)
                .Include(x => x.KarsiTartim)
                .FirstOrDefault(x =>
                    x.IlkTartim.IslemId == islem.IslemId ||
                    (x.KarsiTartimId.HasValue && x.KarsiTartim.IslemId == islem.IslemId));
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

            var toplam = ucretler.Sum(x => x.Tutar);

            return DashboardFormat.Para(toplam);
        }

        private DateTime GetListeHesapTarihi()
        {
            return DateTime.Now;
        }

        private static decimal HesaplaBeklemeUcreti(KantarDbContext context, Islem islem, DateTime hesapTarihi)
        {
            var kayitliBekleme = SumTahsilEdilmemisUcret(islem, KantarSabitleri.UcretKodu.Bekleme);

            var beklemeGunSayisi = SahaZiyaretiServisi.HesaplaBeklemeGunSayisi(islem.GirisTarihi, hesapTarihi);
            if (beklemeGunSayisi <= 0)
            {
                return kayitliBekleme;
            }

            var aktifBekleme = context.Ucretler
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

        private static decimal SumFee(System.Collections.Generic.IEnumerable<IslemUcreti> ucretler, string ucretKodu)
        {
            return ucretler
                .Where(x => x.Ucret != null && x.Ucret.UcretKodu == ucretKodu)
                .Sum(x => x.Tutar);
        }

        private static string FormatKalanBorc(Islem islem)
        {
            return FormatKalanBorc(islem, 0m);
        }

        private static string FormatKalanBorc(Islem islem, decimal ekBeklemeUcreti)
        {
            var kalan = islem.ToplamTahakkuk - islem.ToplamTahsilat;
            kalan += ekBeklemeUcreti;
            return DashboardFormat.Para(kalan > 0 ? kalan : 0m);
        }

        private static string NormalizePlaka(string plaka)
        {
            return (plaka ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", string.Empty);
        }

        private static string NormalizeText(string text)
        {
            return (text ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string YukDurumuFromGelisTuru(string gelisTuru)
        {
            return gelisTuru == KantarSabitleri.GelisTuru.Bos
                ? KantarSabitleri.YukDurumu.Bos
                : KantarSabitleri.YukDurumu.Dolu;
        }

        private static string YukDurumuFromRow(VehicleMovementRow row)
        {
            if (row != null && row.Durum != null && row.Durum.IndexOf("Bos", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return KantarSabitleri.YukDurumu.Bos;
            }

            return KantarSabitleri.YukDurumu.Dolu;
        }

        private static string GetExpectedYukDurumuForOpenVisit(Islem islem)
        {
            var ilkTartim = islem != null
                ? islem.Tartimlar
                    .OrderBy(x => x.TartimTarihi)
                    .FirstOrDefault()
                : null;

            if (ilkTartim != null)
            {
                return GetOppositeYukDurumu(ilkTartim.YukDurumu);
            }

            return YukDurumuFromGelisTuru(islem != null ? islem.GelisTuru : KantarSabitleri.GelisTuru.Dolu);
        }

        private static string GetExpectedYukDurumuForPending(PendingWeighingPrototypeRow row)
        {
            return GetOppositeYukDurumu(row != null ? row.YukDurumu : KantarSabitleri.YukDurumu.Dolu);
        }

        private static string GetOppositeYukDurumu(string yukDurumu)
        {
            return yukDurumu == KantarSabitleri.YukDurumu.Bos
                ? KantarSabitleri.YukDurumu.Dolu
                : KantarSabitleri.YukDurumu.Bos;
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                var typed = child as T;
                if (typed != null)
                {
                    return typed;
                }

                child = VisualTreeHelper.GetParent(child);
            }

            return null;
        }

        private static DateTime GetVehicleRowDate(VehicleMovementRow row)
        {
            DateTime value;
            var ikinciGelis = (row.IkinciTartimTarihi + " " + row.IkinciTartimSaati).Trim();
            if (DateTime.TryParse(ikinciGelis, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.AllowWhiteSpaces, out value))
            {
                return value;
            }

            var ilkGelis = (row.GirisTarihi + " " + row.GirisSaati).Trim();
            if (DateTime.TryParse(ilkGelis, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.AllowWhiteSpaces, out value))
            {
                return value;
            }

            return DateTime.Now;
        }

        private static void EnsureDatabaseSchema()
        {
            using (var context = KantarDbContextFactory.Create())
            {
                context.Database.ExecuteSqlCommand(
                    "IF COL_LENGTH('dbo.IslemUcretleri', 'FaturaId') IS NULL " +
                    "ALTER TABLE dbo.IslemUcretleri ADD FaturaId NVARCHAR(40) NULL");
                context.Database.ExecuteSqlCommand(
                    "IF COL_LENGTH('dbo.Islemler', 'GelisTuru') IS NULL " +
                    "ALTER TABLE dbo.Islemler ADD GelisTuru NVARCHAR(20) NOT NULL CONSTRAINT DF_Islemler_GelisTuru DEFAULT (N'Tartimsiz')");
                context.Database.ExecuteSqlCommand(
                    "IF COL_LENGTH('dbo.Islemler', 'CikisNo') IS NULL " +
                    "ALTER TABLE dbo.Islemler ADD CikisNo NVARCHAR(20) NULL");
                context.Database.ExecuteSqlCommand(
                    "IF OBJECT_ID(N'dbo.CK_Islemler_CikisNo_Format', N'C') IS NULL " +
                    "AND NOT EXISTS (SELECT 1 FROM dbo.Islemler WHERE CikisNo IS NOT NULL AND CikisNo <> N'' AND CikisNo NOT LIKE N'[0-9][0-9][0-9][0-9]') " +
                    "ALTER TABLE dbo.Islemler WITH CHECK ADD CONSTRAINT CK_Islemler_CikisNo_Format " +
                    "CHECK (CikisNo IS NULL OR CikisNo = N'' OR CikisNo LIKE N'[0-9][0-9][0-9][0-9]')");
                context.Database.ExecuteSqlCommand(
                    "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Islemler_CikisNo' AND object_id = OBJECT_ID(N'dbo.Islemler')) " +
                    "AND NOT EXISTS (SELECT CikisNo FROM dbo.Islemler WHERE CikisNo IS NOT NULL AND CikisNo <> N'' GROUP BY CikisNo HAVING COUNT(*) > 1) " +
                    "CREATE UNIQUE INDEX UX_Islemler_CikisNo ON dbo.Islemler(CikisNo) WHERE CikisNo IS NOT NULL AND CikisNo <> N''");
                context.Database.ExecuteSqlCommand(
                    "IF COL_LENGTH('dbo.Islemler', 'MuafMi') IS NULL " +
                    "ALTER TABLE dbo.Islemler ADD MuafMi BIT NOT NULL CONSTRAINT DF_Islemler_MuafMi DEFAULT (0)");
                context.Database.ExecuteSqlCommand(
                    "IF COL_LENGTH('dbo.Islemler', 'MuafiyetNedeni') IS NULL " +
                    "ALTER TABLE dbo.Islemler ADD MuafiyetNedeni NVARCHAR(250) NULL");
                context.Database.ExecuteSqlCommand(
                    "IF COL_LENGTH('dbo.Islemler', 'Notlar') IS NULL " +
                    "ALTER TABLE dbo.Islemler ADD Notlar NVARCHAR(500) NULL");
                context.Database.ExecuteSqlCommand(
                    "IF COL_LENGTH('dbo.Islemler', 'SilindiMi') IS NULL " +
                    "ALTER TABLE dbo.Islemler ADD SilindiMi BIT NOT NULL CONSTRAINT DF_Islemler_SilindiMi DEFAULT (0)");
                context.Database.ExecuteSqlCommand(
                    "IF COL_LENGTH('dbo.Tartimlar', 'YukDurumu') IS NULL " +
                    "ALTER TABLE dbo.Tartimlar ADD YukDurumu NVARCHAR(20) NULL");
                context.Database.ExecuteSqlCommand(
                    "IF COL_LENGTH('dbo.Tartimlar', 'KantarFisNo') IS NULL " +
                    "ALTER TABLE dbo.Tartimlar ADD KantarFisNo NVARCHAR(20) NULL");
                context.Database.ExecuteSqlCommand(
                    "IF COL_LENGTH('dbo.IslemUcretleri', 'TahsilatId') IS NULL " +
                    "ALTER TABLE dbo.IslemUcretleri ADD TahsilatId NVARCHAR(40) NULL");
                context.Database.ExecuteSqlCommand(
                    "IF COL_LENGTH('dbo.IslemUcretleri', 'TahsilatNo') IS NULL " +
                    "ALTER TABLE dbo.IslemUcretleri ADD TahsilatNo NVARCHAR(20) NULL");
                context.Database.ExecuteSqlCommand(
                    "IF COL_LENGTH('dbo.IslemUcretleri', 'OdemeTuru') IS NULL " +
                    "ALTER TABLE dbo.IslemUcretleri ADD OdemeTuru NVARCHAR(30) NULL");
                context.Database.ExecuteSqlCommand(
                    "IF OBJECT_ID(N'dbo.KantarDosyalari', N'U') IS NULL " +
                    "CREATE TABLE dbo.KantarDosyalari (" +
                    "KantarDosyasiId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_KantarDosyalari PRIMARY KEY, " +
                    "AracId INT NOT NULL, IlkTartimId INT NOT NULL, KarsiTartimId INT NULL, Durum NVARCHAR(30) NOT NULL, " +
                    "NetAgirlikKg DECIMAL(18,2) NULL, OlusturmaTarihi DATETIME NOT NULL, TamamlanmaTarihi DATETIME NULL, " +
                    "CONSTRAINT FK_KantarDosyalari_Araclar FOREIGN KEY (AracId) REFERENCES dbo.Araclar(AracId), " +
                    "CONSTRAINT FK_KantarDosyalari_IlkTartim FOREIGN KEY (IlkTartimId) REFERENCES dbo.Tartimlar(TartimId), " +
                    "CONSTRAINT FK_KantarDosyalari_KarsiTartim FOREIGN KEY (KarsiTartimId) REFERENCES dbo.Tartimlar(TartimId))");
                context.Database.ExecuteSqlCommand(
                    "UPDATE dbo.IslemUcretleri " +
                    "SET FaturaId = 'FATESKI' + CAST(IslemId AS NVARCHAR(12)) + CONVERT(NVARCHAR(8), TahsilTarihi, 112) + REPLACE(CONVERT(NVARCHAR(8), TahsilTarihi, 108), ':', '') " +
                    "WHERE TahsilEdildiMi = 1 AND TahsilTarihi IS NOT NULL AND FaturaId IS NULL");
                context.Database.ExecuteSqlCommand(
                    "UPDATE dbo.IslemUcretleri " +
                    "SET TahsilatNo = RIGHT('00000' + CAST(IslemId AS NVARCHAR(12)), 5) " +
                    "WHERE TahsilEdildiMi = 1 AND TahsilTarihi IS NOT NULL AND (TahsilatNo IS NULL OR TahsilatNo = '')");
                context.Database.ExecuteSqlCommand(
                    "UPDATE dbo.IslemUcretleri " +
                    "SET OdemeTuru = N'Nakit' " +
                    "WHERE TahsilEdildiMi = 1 AND TahsilTarihi IS NOT NULL AND (OdemeTuru IS NULL OR OdemeTuru = '')");
            }
        }

        private void LoadPendingWeighingPrototypeData()
        {
            PendingWeighings.Clear();
            PendingWeighings.Add(new PendingWeighingPrototypeRow
            {
                Plaka = "06DMS294",
                FirmaAdi = "UZAY Lojistik",
                IlkTartimTarihi = "24.03.2026",
                IlkTartimSaati = "10:42:58",
                IlkAgirlik = "16500",
                YukDurumu = KantarSabitleri.YukDurumu.Dolu,
                Aciklama = "X firma dolu çıkış sonrası bekleyen tartım"
            });
            PendingWeighings.Add(new PendingWeighingPrototypeRow
            {
                Plaka = "06DMS294",
                FirmaAdi = "YENI Nakliye",
                IlkTartimTarihi = "02.04.2026",
                IlkTartimSaati = "09:15:21",
                IlkAgirlik = "18200",
                YukDurumu = KantarSabitleri.YukDurumu.Dolu,
                Aciklama = "Aynı plaka farklı firma örneği"
            });
            PendingWeighings.Add(new PendingWeighingPrototypeRow
            {
                Plaka = "16TLS4821",
                FirmaAdi = "Bursa Gumruk Depo",
                IlkTartimTarihi = "08.05.2026",
                IlkTartimSaati = "14:08:33",
                IlkAgirlik = "21480",
                YukDurumu = KantarSabitleri.YukDurumu.Dolu,
                Aciklama = "Tek bekleyen tartım örneği"
            });
        }
    }
}




