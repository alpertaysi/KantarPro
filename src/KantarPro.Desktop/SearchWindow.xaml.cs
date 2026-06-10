using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using KantarPro.Application.Services;
using KantarPro.Domain;
using KantarPro.Domain.Entities;
using System.Data.Entity;

namespace KantarPro.Desktop
{
    public partial class SearchWindow : Window
    {
        public ObservableCollection<SearchResultRow> Results { get; private set; }

        public SearchWindow()
        {
            Results = new ObservableCollection<SearchResultRow>();
            DataContext = this;
            InitializeComponent();
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            LoadResults();
        }

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            if (Results.Count == 0)
            {
                MessageBox.Show("Önizleme için önce arama sonucu oluşturun.", "Araştır", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var title = BuildPreviewTitle();
            var text = SearchResultsTextFormatter.Build(Results, title);
            var preview = new TextPreviewWindow(title, text)
            {
                Owner = this
            };
            preview.ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ResultsGridRow_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var row = sender as DataGridRow;
            if (row != null)
            {
                row.IsSelected = true;
                ResultsGrid.SelectedItem = row.Item;
            }
        }

        private void SearchReceiptPreview_Click(object sender, RoutedEventArgs e)
        {
            var row = LoadSelectedReceiptRow();
            if (row == null)
            {
                return;
            }

            try
            {
                var rawText = KantarFisFormatter.BuildFromRow(row);
                var preview = new KantarFisPreviewWindow(KantarFisPreviewData.FromVehicleRow(row, rawText))
                {
                    Owner = this
                };
                preview.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Kantar fişi oluşturulamadı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SearchReceiptPrint_Click(object sender, RoutedEventArgs e)
        {
            var row = LoadSelectedReceiptRow();
            if (row == null)
            {
                return;
            }

            try
            {
                var rawText = KantarFisFormatter.BuildFromRow(row);
                MainWindow.PrintReceiptText(
                    rawText,
                    "Kantar Fisi " + KantarFisPreviewData.FormatFisNo(row));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Kantar fişi yazdırılamadı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private VehicleMovementRow LoadSelectedReceiptRow()
        {
            var selected = ResultsGrid.SelectedItem as SearchResultRow;
            if (selected == null)
            {
                MessageBox.Show("Kantar fişi için bir kayıt seçin.", "Araştır", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            using (var context = KantarDbContextFactory.Create())
            {
                var islem = context.Islemler
                    .Include(x => x.Arac)
                    .Include(x => x.Tartimlar)
                    .FirstOrDefault(x => x.IslemId == selected.IslemId && !x.SilindiMi);
                if (islem == null)
                {
                    MessageBox.Show("Seçilen işlem veritabanında bulunamadı.", "Araştır", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }

                var dosya = context.KantarDosyalari
                    .Include(x => x.IlkTartim)
                    .Include(x => x.KarsiTartim)
                    .FirstOrDefault(x =>
                        x.IlkTartim.IslemId == selected.IslemId ||
                        (x.KarsiTartimId.HasValue && x.KarsiTartim.IslemId == selected.IslemId));

                return SearchReceiptRowBuilder.Build(islem, dosya);
            }
        }

        private void LoadResults()
        {
            try
            {
                var plate = NormalizePlate(PlateTextBox.Text);
                var company = NormalizeText(CompanyTextBox.Text);
                var entryDates = SearchDateCriteria.Create(
                    EntryStartDatePicker.SelectedDate,
                    EntryEndDatePicker.SelectedDate,
                    "Giriş");
                var exitDates = SearchDateCriteria.Create(
                    ExitStartDatePicker.SelectedDate,
                    ExitEndDatePicker.SelectedDate,
                    "Çıkış");

                using (var context = KantarDbContextFactory.Create())
                {
                    var query = context.Islemler
                        .Include(x => x.Arac)
                        .Include(x => x.Tartimlar)
                        .Include(x => x.Ucretler.Select(u => u.Ucret))
                        .Include(x => x.GirisKullanici)
                        .Include(x => x.CikisKullanici)
                        .Where(x => !x.SilindiMi);

                    if (!string.IsNullOrWhiteSpace(plate))
                    {
                        query = query.Where(x => x.Arac.Plaka.Contains(plate));
                    }

                    if (!string.IsNullOrWhiteSpace(company))
                    {
                        query = query.Where(x => x.Arac.FirmaAdi != null && x.Arac.FirmaAdi.ToUpper().Contains(company));
                    }

                    if (entryDates.StartInclusive.HasValue)
                    {
                        query = query.Where(x => x.GirisTarihi >= entryDates.StartInclusive.Value);
                    }

                    if (entryDates.EndExclusive.HasValue)
                    {
                        query = query.Where(x => x.GirisTarihi < entryDates.EndExclusive.Value);
                    }

                    if (exitDates.StartInclusive.HasValue)
                    {
                        query = query.Where(x =>
                            x.CikisTarihi.HasValue &&
                            x.CikisTarihi.Value >= exitDates.StartInclusive.Value);
                    }

                    if (exitDates.EndExclusive.HasValue)
                    {
                        query = query.Where(x =>
                            x.CikisTarihi.HasValue &&
                            x.CikisTarihi.Value < exitDates.EndExclusive.Value);
                    }

                    var islemler = query
                        .OrderByDescending(x => x.GirisTarihi)
                        .Take(500)
                        .ToList();

                    Results.Clear();
                    var siraNo = 1;
                    foreach (var islem in islemler)
                    {
                        Results.Add(BuildRow(islem, siraNo++));
                    }

                    StatusTextBlock.Text = "Kayıt: " + Results.Count + " (en fazla 500 kayıt gösterilir)";
                    if (Results.Count == 0)
                    {
                        MessageBox.Show(
                            "Aradığınız kriterlerde veri bulunamamıştır.",
                            "Araştır",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Arama yapılamadı: " + ex.Message, "Araştır", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static SearchResultRow BuildRow(Islem islem, int siraNo)
        {
            var ilkTartim = DashboardVisitInfo.GetIlkTartim(islem);
            var ikinciTartim = DashboardVisitInfo.GetIkinciTartim(islem);
            var tahsilatlar = islem.Ucretler
                .Where(x => x.TahsilEdildiMi || !string.IsNullOrWhiteSpace(x.TahsilatNo) || !string.IsNullOrWhiteSpace(x.FaturaId))
                .ToList();
            var girisCikisUcreti = SumFee(tahsilatlar, KantarSabitleri.UcretKodu.GirisCikis);
            var tartimUcreti = SumFee(tahsilatlar, KantarSabitleri.UcretKodu.Tartim);
            var beklemeUcreti = SumFee(tahsilatlar, KantarSabitleri.UcretKodu.Bekleme);
            var fisNolari = islem.Tartimlar
                .Where(x => !string.IsNullOrWhiteSpace(x.KantarFisNo))
                .OrderBy(x => x.TartimTarihi)
                .Select(x => x.KantarFisNo.Trim())
                .Distinct()
                .ToList();

            return new SearchResultRow
            {
                IslemId = islem.IslemId,
                SiraNo = siraNo,
                IslemNo = ValueOrDash(!string.IsNullOrWhiteSpace(islem.CikisNo) ? islem.CikisNo : islem.IslemNo),
                Durum = FormatDurum(islem),
                Plaka = islem.Arac != null ? islem.Arac.Plaka : "",
                Firma = islem.Arac != null ? islem.Arac.FirmaAdi : "",
                GirisTarihi = islem.GirisTarihi.ToString("dd.MM.yyyy"),
                GirisSaati = islem.GirisTarihi.ToString("HH:mm:ss"),
                CikisTarihi = islem.CikisTarihi.HasValue ? islem.CikisTarihi.Value.ToString("dd.MM.yyyy") : "",
                CikisSaati = islem.CikisTarihi.HasValue ? islem.CikisTarihi.Value.ToString("HH:mm:ss") : "",
                BirinciTartim = DashboardFormat.TartimDegeri(ilkTartim),
                IkinciTartim = DashboardFormat.TartimDegeri(ikinciTartim),
                Net = KantarDisplayFormatter.FormatNetAgirlik(IsTartimsiz(islem, ilkTartim, ikinciTartim), Weight(ilkTartim), Weight(ikinciTartim)),
                TahsilatNo = JoinDistinct(tahsilatlar.Select(x => !string.IsNullOrWhiteSpace(x.TahsilatNo) ? x.TahsilatNo : x.FaturaId)),
                OdemeTuru = islem.MuafMi ? "Muaf" : ValueOrDash(JoinDistinct(tahsilatlar.Select(x => x.OdemeTuru))),
                KantarFisNo = fisNolari.Count == 0 ? "-" : string.Join(", ", fisNolari),
                Kullanici = FormatKullanici(islem),
                ToplamUcret = DashboardFormat.Para(islem.ToplamTahsilat > 0 ? islem.ToplamTahsilat : islem.Ucretler.Where(x => x.TahsilEdildiMi).Sum(x => x.Tutar)),
                GirisCikisUcreti = DashboardFormat.Para(girisCikisUcreti),
                TartimUcreti = DashboardFormat.Para(tartimUcreti),
                BeklemeUcreti = DashboardFormat.Para(beklemeUcreti),
                Notlar = islem.MuafMi && !string.IsNullOrWhiteSpace(islem.MuafiyetNedeni)
                    ? islem.MuafiyetNedeni
                    : islem.Notlar
            };
        }

        private string BuildPreviewTitle()
        {
            var entry = FormatRange(EntryStartDatePicker.SelectedDate, EntryEndDatePicker.SelectedDate);
            var exit = FormatRange(ExitStartDatePicker.SelectedDate, ExitEndDatePicker.SelectedDate);
            return "Araştırma Dökümü Giriş: " + entry + " Çıkış: " + exit;
        }

        private static string FormatRange(DateTime? start, DateTime? end)
        {
            if (!start.HasValue && !end.HasValue)
            {
                return "Tümü";
            }

            return (start.HasValue ? start.Value.ToString("dd.MM.yyyy") : "...") + "-" +
                (end.HasValue ? end.Value.ToString("dd.MM.yyyy") : "...");
        }

        private static string FormatDurum(Islem islem)
        {
            if (islem == null)
            {
                return "";
            }

            if (islem.MuafMi)
            {
                return "Muaf";
            }

            if (islem.Durum == KantarSabitleri.IslemDurumu.CikisYapti)
            {
                return "Çıkış Yapıldı";
            }

            if (islem.Durum == KantarSabitleri.IslemDurumu.Iceride)
            {
                return "İçeride";
            }

            return islem.Durum;
        }

        private static string FormatKullanici(Islem islem)
        {
            var user = islem.CikisKullanici ?? islem.GirisKullanici;
            if (user != null && !string.IsNullOrWhiteSpace(user.AdSoyad))
            {
                return user.AdSoyad;
            }

            if (islem.CikisKullaniciId.HasValue)
            {
                return islem.CikisKullaniciId.Value.ToString(CultureInfo.InvariantCulture);
            }

            return islem.GirisKullaniciId.ToString(CultureInfo.InvariantCulture);
        }

        private static string JoinDistinct(System.Collections.Generic.IEnumerable<string> values)
        {
            var list = values
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            return list.Count == 0 ? "-" : string.Join(", ", list);
        }

        private static decimal? Weight(Tartim tartim)
        {
            return tartim != null ? (decimal?)tartim.AgirlikKg : null;
        }

        private static decimal SumFee(System.Collections.Generic.IEnumerable<IslemUcreti> fees, string feeCode)
        {
            return fees
                .Where(x => x.Ucret != null && x.Ucret.UcretKodu == feeCode)
                .Sum(x => x.Tutar);
        }

        private static bool IsTartimsiz(Islem islem, Tartim ilkTartim, Tartim ikinciTartim)
        {
            return islem != null &&
                islem.GelisTuru == KantarSabitleri.GelisTuru.Tartimsiz &&
                ilkTartim == null &&
                ikinciTartim == null;
        }

        private static string ValueOrDash(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }

        private static string NormalizePlate(string value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", string.Empty);
        }

        private static string NormalizeText(string value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }
    }
}
