using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
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

        private void LoadResults()
        {
            try
            {
                var plate = NormalizePlate(PlateTextBox.Text);
                var company = NormalizeText(CompanyTextBox.Text);
                var entryStart = EntryStartDatePicker.SelectedDate.HasValue ? (DateTime?)EntryStartDatePicker.SelectedDate.Value.Date : null;
                var entryEndExclusive = EntryEndDatePicker.SelectedDate.HasValue ? (DateTime?)EntryEndDatePicker.SelectedDate.Value.Date.AddDays(1) : null;
                var exitStart = ExitStartDatePicker.SelectedDate.HasValue ? (DateTime?)ExitStartDatePicker.SelectedDate.Value.Date : null;
                var exitEndExclusive = ExitEndDatePicker.SelectedDate.HasValue ? (DateTime?)ExitEndDatePicker.SelectedDate.Value.Date.AddDays(1) : null;

                ValidateRange(entryStart, entryEndExclusive, "Giriş");
                ValidateRange(exitStart, exitEndExclusive, "Çıkış");

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

                    if (entryStart.HasValue)
                    {
                        query = query.Where(x => x.GirisTarihi >= entryStart.Value);
                    }

                    if (entryEndExclusive.HasValue)
                    {
                        query = query.Where(x => x.GirisTarihi < entryEndExclusive.Value);
                    }

                    if (exitStart.HasValue)
                    {
                        query = query.Where(x => x.CikisTarihi.HasValue && x.CikisTarihi.Value >= exitStart.Value);
                    }

                    if (exitEndExclusive.HasValue)
                    {
                        query = query.Where(x => x.CikisTarihi.HasValue && x.CikisTarihi.Value < exitEndExclusive.Value);
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
            var fisNolari = islem.Tartimlar
                .Where(x => !string.IsNullOrWhiteSpace(x.KantarFisNo))
                .OrderBy(x => x.TartimTarihi)
                .Select(x => x.KantarFisNo.Trim())
                .Distinct()
                .ToList();

            return new SearchResultRow
            {
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
                return "Tum";
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

        private static void ValidateRange(DateTime? start, DateTime? endExclusive, string label)
        {
            if (start.HasValue && endExclusive.HasValue && endExclusive.Value <= start.Value)
            {
                throw new InvalidOperationException(label + " bitiş tarihi başlangıç tarihinden önce olamaz.");
            }
        }
    }
}
