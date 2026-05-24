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

namespace KantarPro.Desktop
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<VehicleMovementRow> EntryVehicles { get; private set; }
        public ObservableCollection<VehicleMovementRow> ExitVehicles { get; private set; }
        public ObservableCollection<DailyTransactionRow> DailyTransactions { get; private set; }
        public ObservableCollection<PendingWeighingPrototypeRow> PendingWeighings { get; private set; }
        public ICollectionView EntryVehiclesView { get; private set; }
        public ICollectionView ExitVehiclesView { get; private set; }

        private string _entryPlakaFilter = string.Empty;
        private string _entryFirmaFilter = string.Empty;
        private string _exitPlakaFilter = string.Empty;
        private string _exitFirmaFilter = string.Empty;

        public MainWindow()
        {
            EntryVehicles = new ObservableCollection<VehicleMovementRow>();
            ExitVehicles = new ObservableCollection<VehicleMovementRow>();
            DailyTransactions = new ObservableCollection<DailyTransactionRow>();
            PendingWeighings = new ObservableCollection<PendingWeighingPrototypeRow>();
            EntryVehiclesView = CollectionViewSource.GetDefaultView(EntryVehicles);
            ExitVehiclesView = CollectionViewSource.GetDefaultView(ExitVehicles);
            EntryVehiclesView.Filter = FilterEntryVehicle;
            ExitVehiclesView.Filter = FilterExitVehicle;

            DataContext = this;
            InitializeComponent();
            EnsureDatabaseSchema();
            LoadPendingWeighingPrototypeData();
            ShowEntryPage();

            var timer = new DispatcherTimer { Interval = System.TimeSpan.FromSeconds(1) };
            timer.Tick += (sender, args) => ClockText.Text = System.DateTime.Now.ToString("HH:mm:ss");
            timer.Start();
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            MessageBox.Show((button != null ? button.Content : "Menu") + " ekrani bir sonraki adimda baglanacak.", "Kantar Pro");
        }

        private void AracGirisButton_Click(object sender, RoutedEventArgs e)
        {
            ShowEntryPage();
        }

        private void AracCikisButton_Click(object sender, RoutedEventArgs e)
        {
            ShowExitPage();
        }

        private void DoluBosTartimButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDoluBosPage();
        }

        private void TartVeKaydet_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EntrySaveChoiceWindow
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            DashboardGirisKaydiOlustur(dialog.Choice == EntrySaveChoice.WeighAndSave);
        }

        private void DashboardGirisKaydiOlustur(bool tartimIsteniyor)
        {
            try
            {
                var plaka = PlakaTextBox.Text;
                var firmaAdi = FirmaTextBox.Text;
                var aciklama = AciklamaTextBox.Text;
                var agirlik = tartimIsteniyor ? ParseAgirlik(AgirlikTextBox.Text) : (decimal?)null;
                var islemTarihi = ParseIslemTarihi(GirisTarihiTextBox.Text, "Giris tarihi");

                if (tartimIsteniyor && TryCompletePendingDoluBosFromDashboard(plaka, agirlik.GetValueOrDefault(), islemTarihi))
                {
                    PlakaTextBox.Clear();
                    FirmaTextBox.Clear();
                    AciklamaTextBox.Clear();
                    AgirlikTextBox.Text = "0";
                    return;
                }

                CreateEntry(plaka, firmaAdi, aciklama, tartimIsteniyor, agirlik, islemTarihi, tartimIsteniyor ? null : KantarSabitleri.GelisTuru.Tartimsiz);

                LoadDashboardData();
                MessageBox.Show("Giris kaydi olusturuldu.", "Kantar Pro");
                PlakaTextBox.Clear();
                FirmaTextBox.Clear();
                AciklamaTextBox.Clear();
                AgirlikTextBox.Text = "0";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Giris kaydi olusturulamadi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EntryPageGirisKaydiOlustur_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var plaka = EntryPlakaTextBox.Text;
                var firmaAdi = EntryFirmaTextBox.Text;
                var aciklama = EntryAciklamaTextBox.Text;
                var tartimIsteniyor = EntryTartimIsteniyorCheckBox.IsChecked != true;
                var agirlik = tartimIsteniyor ? ParseAgirlik(EntryAgirlikTextBox.Text) : (decimal?)null;

                CreateEntry(plaka, firmaAdi, aciklama, tartimIsteniyor, agirlik, DateTime.Now);

                LoadDashboardData();
                MessageBox.Show("Giris kaydi olusturuldu.", "Kantar Pro");
                ClearEntryPageForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Giris kaydi olusturulamadi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EntryFormTemizle_Click(object sender, RoutedEventArgs e)
        {
            ClearEntryPageForm();
        }

        private void ExitFindVehicle_Click(object sender, RoutedEventArgs e)
        {
            ShowOpenEntryInfo(ExitPlakaTextBox.Text);
        }

        private void ExitPageCikisTamamla_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var plaka = ExitPlakaTextBox.Text;
                var tartimIsteniyor = ExitManuelTartimCheckBox.IsChecked == true;
                var agirlik = tartimIsteniyor ? ParseAgirlik(ExitAgirlikTextBox.Text) : (decimal?)null;
                var cikisTarihi = ParseIslemTarihi(ExitCikisTarihiTextBox.Text, "Cikis tarihi");

                if (ShowExitConfirmation(plaka, tartimIsteniyor, agirlik, cikisTarihi))
                {
                    LoadDashboardData();
                    MessageBox.Show("Cikis islemi tamamlandi.", "Kantar Pro");
                    ClearExitPageForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Cikis islemi tamamlanamadi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExitFormTemizle_Click(object sender, RoutedEventArgs e)
        {
            ClearExitPageForm();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDashboardData();
        }

        private void CikisIsleminiTamamla_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var plaka = GetPlakaForOperation();
                var tartimIsteniyor = false;
                var agirlik = (decimal?)null;
                var cikisTarihi = ParseIslemTarihi(CikisTarihiTextBox.Text, "Cikis tarihi");

                if (ShowExitConfirmation(plaka, tartimIsteniyor, agirlik, cikisTarihi))
                {
                    LoadDashboardData();
                    MessageBox.Show("Cikis islemi tamamlandi.", "Kantar Pro");
                    PlakaTextBox.Clear();
                    AgirlikTextBox.Text = "0";
                    CikisTarihiTextBox.Text = DateTime.Today.ToString("dd.MM.yyyy");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Cikis islemi tamamlanamadi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void KantarFisiYazdir_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Kantar fisi yazdirma sonraki adimda OKI 5720 ayarlariyla baglanacak.", "Kantar Pro");
        }

        private void PlakaDuzelt_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Plaka duzeltme ekrani sonraki adimda acilacak.", "Kantar Pro");
        }

        private void EntryVehiclesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var grid = sender as DataGrid;
            var row = grid != null ? grid.SelectedItem as VehicleMovementRow : null;
            if (row != null)
            {
                ShowVehicleMovementDetail(row);
            }
        }

        private void EntryVehiclesGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = FindParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if (row == null)
            {
                return;
            }

            row.IsSelected = true;
            EntryVehiclesGrid.SelectedItem = row.Item;
            EntryVehiclesGrid.Focus();
        }

        private void EntryGridTartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var row = EntryVehiclesGrid.SelectedItem as VehicleMovementRow;
            if (row == null)
            {
                MessageBox.Show("Tartim eklenecek satiri secin.", "Kantar Pro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var agirlik = ParseAgirlik(AgirlikTextBox.Text);
                var tartimTarihi = ParseIslemTarihi(GirisTarihiTextBox.Text, "Islem tarihi");

                using (var context = new KantarDbContext())
                {
                    var kullaniciId = EnsureAdminUser(context);
                    var servis = new SahaZiyaretiServisi(new KantarUnitOfWork(context));
                    servis.SonradanTartimEkle(row.Plaka, YukDurumuFromRow(row), agirlik.Value, kullaniciId, tartimTarihi);
                }

                LoadDashboardData();
                MessageBox.Show("Tartim eklendi.", "Kantar Pro");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Tartim eklenemedi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EntryGridGelisTarihiniGuncelleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var row = EntryVehiclesGrid.SelectedItem as VehicleMovementRow;
            if (row == null)
            {
                MessageBox.Show("Tarihi degistirilecek satiri secin.", "Kantar Pro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var tarihPenceresi = new DateInputWindow(GetVehicleRowDate(row))
                {
                    Owner = this
                };

                if (tarihPenceresi.ShowDialog() != true)
                {
                    return;
                }

                var yeniTarih = tarihPenceresi.SelectedDate;
                using (var context = new KantarDbContext())
                {
                    var normalized = NormalizePlaka(row.Plaka);
                    var islem = context.Islemler
                        .Include(x => x.Arac)
                        .Include(x => x.Tartimlar)
                        .FirstOrDefault(x => x.Arac.Plaka == normalized && x.Durum == KantarSabitleri.IslemDurumu.Iceride);

                    if (islem == null)
                    {
                        islem = context.Islemler
                            .Include(x => x.Arac)
                            .Include(x => x.Tartimlar)
                            .Where(x => x.Arac.Plaka == normalized && x.Durum == KantarSabitleri.IslemDurumu.CikisYapti)
                            .OrderByDescending(x => x.CikisTarihi)
                            .FirstOrDefault();
                    }

                    if (islem == null)
                    {
                        throw new InvalidOperationException("Bu plaka icin guncellenecek islem bulunamadi.");
                    }

                    var ilkTartim = islem.Tartimlar
                        .Where(x => x.TartimTipi == KantarSabitleri.TartimTipi.Giris)
                        .OrderBy(x => x.TartimTarihi)
                        .FirstOrDefault();

                    var ikinciTartim = islem.Tartimlar
                        .Where(x => x.TartimTipi == KantarSabitleri.TartimTipi.Sonradan || x.TartimTipi == KantarSabitleri.TartimTipi.Cikis)
                        .OrderByDescending(x => x.TartimTarihi)
                        .FirstOrDefault();

                    if (ikinciTartim != null)
                    {
                        islem.GirisTarihi = yeniTarih;
                        ikinciTartim.TartimTarihi = yeniTarih;
                    }
                    else
                    {
                        islem.GirisTarihi = yeniTarih;
                        if (ilkTartim != null)
                        {
                            ilkTartim.TartimTarihi = yeniTarih;
                        }
                    }

                    context.SaveChanges();
                }

                LoadDashboardData();
                MessageBox.Show("Gelis tarihi guncellendi.", "Kantar Pro");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Gelis tarihi guncellenemedi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExitVehiclesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var grid = sender as DataGrid;
            var row = grid != null ? grid.SelectedItem as VehicleMovementRow : null;
            if (row != null)
            {
                ShowVehicleMovementDetail(row);
            }
        }

        private void ExitPageEntryVehiclesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var row = ExitPageEntryVehiclesGrid.SelectedItem as VehicleMovementRow;
            if (row != null)
            {
                ExitPlakaTextBox.Text = row.Plaka;
                ShowOpenEntryInfo(row.Plaka);
            }
        }

        private void CreateEntry(string plaka, string firmaAdi, string aciklama, bool tartimIsteniyor, decimal? agirlik, DateTime islemTarihi, string gelisTuruOverride = null)
        {
            using (var context = new KantarDbContext())
            {
                var kullaniciId = EnsureAdminUser(context);
                var servis = new SahaZiyaretiServisi(new KantarUnitOfWork(context));
                var gelisTuru = gelisTuruOverride ?? servis.GelisTuruOner(plaka);

                var normalizedPlaka = NormalizePlaka(plaka);
                var acikIslemVarMi = context.Islemler.Any(x => x.Arac.Plaka == normalizedPlaka && x.Durum == KantarSabitleri.IslemDurumu.Iceride);
                if (acikIslemVarMi)
                {
                    if (!tartimIsteniyor)
                    {
                        throw new InvalidOperationException("Bu plaka icin iceride acik islem var. Yeni giris yerine cikis yapin veya tartim ekleyin.");
                    }

                    servis.SonradanTartimEkle(plaka, YukDurumuFromGelisTuru(gelisTuru), agirlik.Value, kullaniciId, islemTarihi);
                    return;
                }

                servis.GirisKaydet(plaka, firmaAdi, gelisTuru, tartimIsteniyor, agirlik, kullaniciId, islemTarihi);
            }
        }

        private bool TryCompletePendingDoluBosFromDashboard(string plaka, decimal scaleWeightKg, DateTime secondWeighingDate)
        {
            using (var context = new KantarDbContext())
            {
                var pendingRow = FindPendingDoluBosRow(context, plaka);
                if (pendingRow == null)
                {
                    return false;
                }

                var dialog = new DoluBosSecondWeighingWindow(pendingRow, scaleWeightKg, secondWeighingDate)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() != true)
                {
                    return true;
                }

                var gelisTuru = pendingRow.YukDurumu == KantarSabitleri.YukDurumu.Bos
                    ? KantarSabitleri.GelisTuru.Dolu
                    : KantarSabitleri.GelisTuru.Bos;

                CreateEntry(pendingRow.Plaka, pendingRow.FirmaAdi, pendingRow.Aciklama, true, dialog.SecondWeightKg, secondWeighingDate, gelisTuru);
            }

            LoadDashboardData();
            ShowEntryPage();
            MessageBox.Show("Ikinci tartim kaydedildi. Arac ust listedeki dolu-bos kaydina alindi.", "Dolu-Bos Tartim");
            return true;
        }

        private void TryOpenPendingDoluBosForPlate(KantarDbContext context, string plaka)
        {
            var pendingRow = FindPendingDoluBosRow(context, plaka);
            if (pendingRow == null)
            {
                return;
            }

            LoadDashboardData();
            ShowDoluBosPage();
            DoluBosPlakaTextBox.Text = pendingRow.Plaka;
            SelectPendingWeighingRow(pendingRow.Plaka);
        }

        private static PendingWeighingPrototypeRow FindPendingDoluBosRow(KantarDbContext context, string plaka)
        {
            var normalized = NormalizePlaka(plaka);
            var bekleyen = context.KantarDosyalari
                .Include(x => x.Arac)
                .Include(x => x.IlkTartim)
                .Include(x => x.IlkTartim.Islem)
                .Where(x => x.Arac.Plaka == normalized && x.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor)
                .OrderByDescending(x => x.OlusturmaTarihi)
                .FirstOrDefault();

            if (bekleyen == null || bekleyen.IlkTartim == null || bekleyen.IlkTartim.Islem == null ||
                bekleyen.IlkTartim.Islem.Durum != KantarSabitleri.IslemDurumu.CikisYapti)
            {
                return null;
            }

            return new PendingWeighingPrototypeRow
            {
                Plaka = bekleyen.Arac.Plaka,
                FirmaAdi = bekleyen.Arac.FirmaAdi,
                IlkGirisTarihi = bekleyen.IlkTartim.Islem.GirisTarihi.ToString("dd.MM.yyyy"),
                IlkGirisSaati = bekleyen.IlkTartim.Islem.GirisTarihi.ToString("HH:mm:ss"),
                IlkCikisTarihi = bekleyen.IlkTartim.Islem.CikisTarihi.HasValue ? bekleyen.IlkTartim.Islem.CikisTarihi.Value.ToString("dd.MM.yyyy") : "",
                IlkCikisSaati = bekleyen.IlkTartim.Islem.CikisTarihi.HasValue ? bekleyen.IlkTartim.Islem.CikisTarihi.Value.ToString("HH:mm:ss") : "",
                IlkTartimTarihi = bekleyen.IlkTartim.TartimTarihi.ToString("dd.MM.yyyy"),
                IlkTartimSaati = bekleyen.IlkTartim.TartimTarihi.ToString("HH:mm:ss"),
                IlkAgirlik = bekleyen.IlkTartim.AgirlikKg.ToString("N0"),
                YukDurumu = bekleyen.IlkTartim.YukDurumu,
                Aciklama = GetBeklenenTartimDurumu(bekleyen.IlkTartim)
            };
        }

        private void ShowEntryPage()
        {
            DashboardContent.Visibility = Visibility.Visible;
            EntryPageContent.Visibility = Visibility.Collapsed;
            ExitPageContent.Visibility = Visibility.Collapsed;
            DoluBosPageContent.Visibility = Visibility.Collapsed;
            LoadDashboardData();
            PlakaTextBox.Focus();
        }

        private void ShowExitPage()
        {
            DashboardContent.Visibility = Visibility.Visible;
            EntryPageContent.Visibility = Visibility.Collapsed;
            ExitPageContent.Visibility = Visibility.Collapsed;
            DoluBosPageContent.Visibility = Visibility.Collapsed;
            LoadDashboardData();
            PlakaTextBox.Focus();
        }

        private void ShowDoluBosPage()
        {
            DashboardContent.Visibility = Visibility.Collapsed;
            EntryPageContent.Visibility = Visibility.Collapsed;
            ExitPageContent.Visibility = Visibility.Collapsed;
            DoluBosPageContent.Visibility = Visibility.Visible;
            DoluBosPlakaTextBox.Focus();
        }

        private void ClearEntryPageForm()
        {
            EntryPlakaTextBox.Clear();
            EntryFirmaTextBox.Clear();
            EntryAciklamaTextBox.Clear();
            EntryAgirlikTextBox.Text = "0";
            EntryTartimIsteniyorCheckBox.IsChecked = false;
            EntryManuelTartimCheckBox.IsChecked = false;
            EntryPlakaTextBox.Focus();
        }

        private void ClearExitPageForm()
        {
            ExitPlakaTextBox.Clear();
            ExitCikisTarihiTextBox.Text = DateTime.Today.ToString("dd.MM.yyyy");
            ExitAgirlikTextBox.Text = "0";
            ExitManuelTartimCheckBox.IsChecked = false;
            ExitOpenEntryInfoText.Text = "Liste secimi yap veya plaka girip acik girisi bul.";
            ExitPlakaTextBox.Focus();
        }

        private void ShowOpenEntryInfo(string plaka)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(plaka))
                {
                    ExitOpenEntryInfoText.Text = "Plaka girin veya listeden arac secin.";
                    return;
                }

                var normalized = NormalizePlaka(plaka);
                using (var context = new KantarDbContext())
                {
                    var islem = context.Islemler
                        .Include(x => x.Arac)
                        .Include(x => x.Tartimlar)
                        .Include(x => x.Ucretler.Select(u => u.Ucret))
                        .FirstOrDefault(x => x.Arac.Plaka == normalized && x.Durum == KantarSabitleri.IslemDurumu.Iceride);

                    if (islem == null)
                    {
                        ExitOpenEntryInfoText.Text = "Bu plaka icin acik giris bulunamadi.";
                        return;
                    }

                    var bekleme = DateTime.Now.Date > islem.GirisTarihi.Date ? "Bekleme ucreti olusacak" : "Bekleme ucreti yok";
                    ExitOpenEntryInfoText.Text =
                        "Plaka: " + islem.Arac.Plaka +
                        "\nGiris: " + islem.GirisTarihi.ToString("dd.MM.yyyy HH:mm:ss") +
                        "\nGiris tartimi: " + FormatTartim(islem) +
                        "\nTahsil edilecek: " + FormatKalanBorc(islem) +
                        "\n" + bekleme;
                }
            }
            catch (Exception ex)
            {
                ExitOpenEntryInfoText.Text = "Acik giris bilgisi okunamadi: " + ex.Message;
            }
        }

        private bool ShowExitConfirmation(string plaka, bool tartimIsteniyor, decimal? agirlik, DateTime cikisTarihi)
        {
            var dialog = new ExitConfirmationWindow(plaka, tartimIsteniyor, agirlik, cikisTarihi)
            {
                Owner = this
            };

            return dialog.ShowDialog() == true;
        }

        private int? SelectPendingWeighingIfNeeded(KantarDbContext context, string plaka)
        {
            var normalized = NormalizePlaka(plaka);
            EnsurePendingWeighingsForPlate(context, normalized);

            var pendingRows = context.BekleyenTartimlar
                .Include(x => x.Arac)
                .Where(x => x.Arac.Plaka == normalized && x.Durum == KantarSabitleri.BekleyenTartimDurumu.Bekliyor)
                .OrderByDescending(x => x.IlkTartimTarihi)
                .ToList();

            if (pendingRows.Count == 0)
            {
                return null;
            }

            var rows = new ObservableCollection<PendingWeighingChoiceRow>(
                pendingRows.Select(x => new PendingWeighingChoiceRow
                {
                    BekleyenTartimId = x.BekleyenTartimId,
                    Plaka = x.Arac.Plaka,
                    FirmaAdi = x.Arac.FirmaAdi,
                    IlkTartimTarihi = x.IlkTartimTarihi.ToString("dd.MM.yyyy"),
                    IlkTartimSaati = x.IlkTartimTarihi.ToString("HH:mm:ss"),
                    IlkAgirlik = x.IlkAgirlikKg.ToString("N0"),
                    Aciklama = x.Arac.Aciklama
                }));

            var dialog = new PendingWeighingChoiceWindow(normalized, rows)
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true)
            {
                throw new OperationCanceledException("Giris islemi iptal edildi.");
            }

            if (dialog.Result == PendingWeighingChoiceResult.UseExisting)
            {
                return dialog.SelectedBekleyenTartimId;
            }

            return null;
        }

        private static void EnsurePendingWeighingsForPlate(KantarDbContext context, string normalizedPlaka)
        {
            var girisTartimlari = context.Tartimlar
                .Include(x => x.Arac)
                .Where(x => x.Arac.Plaka == normalizedPlaka && x.TartimTipi == KantarSabitleri.TartimTipi.Giris)
                .ToList();

            var eklendiMi = false;
            foreach (var tartim in girisTartimlari)
            {
                var bekleyenKayitVarMi = context.BekleyenTartimlar.Any(x => x.IlkTartimId == tartim.TartimId);
                if (bekleyenKayitVarMi)
                {
                    continue;
                }

                context.BekleyenTartimlar.Add(new BekleyenTartim
                {
                    AracId = tartim.AracId,
                    IlkTartimId = tartim.TartimId,
                    IlkAgirlikKg = tartim.AgirlikKg,
                    IlkTartimTarihi = tartim.TartimTarihi,
                    Durum = KantarSabitleri.BekleyenTartimDurumu.Bekliyor
                });
                eklendiMi = true;
            }

            if (eklendiMi)
            {
                context.SaveChanges();
            }
        }

        private string GetPlakaForOperation()
        {
            if (!string.IsNullOrWhiteSpace(PlakaTextBox.Text))
            {
                return PlakaTextBox.Text;
            }

            var selectedEntry = EntryVehiclesGrid.SelectedItem as VehicleMovementRow;
            if (selectedEntry != null)
            {
                return selectedEntry.Plaka;
            }

            throw new ArgumentException("Plaka girin veya giris listesinden bir arac secin.");
        }

        private static decimal? ParseAgirlik(string text)
        {
            decimal value;
            var normalized = (text ?? string.Empty).Trim().Replace(',', '.');
            if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            {
                throw new ArgumentException("Agirlik sayisal olmalidir.");
            }

            return value;
        }

        private static DateTime ParseIslemTarihi(string text, string alanAdi)
        {
            DateTime tarih;
            if (!DateTime.TryParseExact((text ?? string.Empty).Trim(), "dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.None, out tarih))
            {
                throw new ArgumentException(alanAdi + " gg.aa.yyyy formatinda olmalidir.");
            }

            return tarih.Date.Add(DateTime.Now.TimeOfDay);
        }

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
                        var dosya = GetKantarDosyasiForIslem(context, islem);
                        var ilkTartim = dosya != null ? dosya.IlkTartim : GetIlkTartim(islem);
                        var ikinciTartim = dosya != null ? dosya.KarsiTartim : GetIkinciTartim(islem);
                        var sonTartim = GetSonTartim(islem);
                        var beklemeUcreti = HesaplaBeklemeUcreti(context, islem, listeHesapTarihi);
                        var kayitliBeklemeUcreti = SumTahsilEdilmemisUcret(islem, KantarSabitleri.UcretKodu.Bekleme);
                        EntryVehicles.Add(new VehicleMovementRow
                        {
                            Plaka = islem.Arac.Plaka,
                            FirmaAdi = islem.Arac.FirmaAdi,
                            GirisTarihi = FormatDoluGelisTarihi(islem, ilkTartim),
                            GirisSaati = FormatDoluGelisSaati(islem, ilkTartim),
                            DoluCikisTarihi = FormatDoluCikisTarihi(islem, ilkTartim, ikinciTartim),
                            DoluCikisSaati = FormatDoluCikisSaati(islem, ilkTartim, ikinciTartim),
                            BosGelisTarihi = FormatBosGelisTarihi(ilkTartim, ikinciTartim),
                            BosGelisSaati = FormatBosGelisSaati(ilkTartim, ikinciTartim),
                            Saat = FormatSaatSaniyeli(islem.GirisTarihi),
                            CikisTarihi = "",
                            CikisSaati = "",
                            IlkTartimTarihi = FormatTartimTarihi(ilkTartim),
                            IlkTartimSaati = FormatTartimSaati(ilkTartim),
                            IkinciTartimTarihi = FormatTartimTarihi(ikinciTartim),
                            IkinciTartimSaati = FormatTartimSaati(ikinciTartim),
                            SonTartimTarihi = FormatTartimTarihi(sonTartim),
                            SonTartimSaati = FormatTartimSaati(sonTartim),
                            SonTartim = FormatSonTartim(sonTartim),
                            Tartim = FormatTartimDegeri(ilkTartim),
                            IkinciTartim = FormatTartimDegeri(ikinciTartim),
                            NetAgirlik = FormatNetAgirlik(ilkTartim, ikinciTartim),
                            Ucret = FormatKalanBorc(islem, beklemeUcreti - kayitliBeklemeUcreti),
                            Tahsilat = FormatPara(islem.ToplamTahsilat),
                            GirisCikisUcreti = FormatUcretKalemi(islem, KantarSabitleri.UcretKodu.GirisCikis, true),
                            TartimUcreti = FormatUcretKalemi(islem, KantarSabitleri.UcretKodu.Tartim, true),
                            BeklemeUcreti = FormatPara(beklemeUcreti),
                            Durum = GetVisitRowDurum(islem, dosya),
                            KesinCikisMi = false
                        });
                    }

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
                        var islem = dosya.IlkTartim != null ? dosya.IlkTartim.Islem : null;
                        if (dosya.IlkTartim == null ||
                            islem == null ||
                            islem.Durum != KantarSabitleri.IslemDurumu.CikisYapti)
                        {
                            continue;
                        }

                        PendingWeighings.Add(new PendingWeighingPrototypeRow
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
                        });
                    }

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
                        var dosya = GetKantarDosyasiForIslem(context, islem);
                        var ilkTartim = dosya != null ? dosya.IlkTartim : GetIlkTartim(islem);
                        var ikinciTartim = dosya != null ? dosya.KarsiTartim : GetIkinciTartim(islem);
                        if (!AltCikisListesindeGoster(islem, dosya, ilkTartim, ikinciTartim))
                        {
                            continue;
                        }

                        var cikisSatiri = new VehicleMovementRow
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
                            IlkTartimTarihi = FormatTartimTarihi(ilkTartim),
                            IlkTartimSaati = FormatTartimSaati(ilkTartim),
                            IkinciTartimTarihi = FormatTartimTarihi(ikinciTartim),
                            IkinciTartimSaati = FormatTartimSaati(ikinciTartim),
                            Tartim = FormatTartimDegeri(ilkTartim),
                            IkinciTartim = FormatTartimDegeri(ikinciTartim),
                            NetAgirlik = FormatNetAgirlik(ilkTartim, ikinciTartim),
                            Ucret = FormatPara(islem.ToplamTahakkuk),
                            Tahsilat = FormatPara(islem.ToplamTahsilat),
                            GirisCikisUcreti = FormatUcretKalemi(islem, KantarSabitleri.UcretKodu.GirisCikis),
                            TartimUcreti = FormatUcretKalemi(islem, KantarSabitleri.UcretKodu.Tartim),
                            BeklemeUcreti = FormatUcretKalemi(islem, KantarSabitleri.UcretKodu.Bekleme),
                            Durum = dosya != null ? GetVisitRowDurum(islem, dosya) : "Kesin cikis",
                            KesinCikisMi = true
                        };
                        finalExitRows.Add(Tuple.Create(islem.CikisTarihi ?? islem.GirisTarihi, cikisSatiri));
                    }

                    foreach (var row in finalExitRows.OrderByDescending(x => x.Item1).Select(x => x.Item2))
                    {
                        ExitVehicles.Add(row);
                    }

                    EntryVehiclesView.Refresh();
                    ExitVehiclesView.Refresh();

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
                            Ucret = FormatPara(islem.ToplamTahakkuk),
                            Kullanici = "Admin"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ana ekran verileri okunamadi: " + ex.Message, "Kantar Pro", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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

            return
                "Plaka: " + row.Plaka + Environment.NewLine +
                "Firma: " + (string.IsNullOrWhiteSpace(row.FirmaAdi) ? "-" : row.FirmaAdi) + Environment.NewLine +
                "Durum: " + row.Durum + Environment.NewLine + Environment.NewLine +
                "Dolu Hareket" + Environment.NewLine +
                "Gelis: " + FormatBosDeger(row.GirisTarihi + " " + row.GirisSaati) + Environment.NewLine +
                "Tartim: " + FormatBosDeger(row.IlkTartimTarihi + " " + row.IlkTartimSaati) + " | " + FormatBosDeger(row.Tartim) + Environment.NewLine +
                "Cikis: " + FormatBosDeger(row.DoluCikisTarihi + " " + row.DoluCikisSaati) + Environment.NewLine + Environment.NewLine +
                "Bos Hareket" + Environment.NewLine +
                "Gelis: " + FormatBosDeger(row.IkinciTartimTarihi + " " + row.IkinciTartimSaati) + Environment.NewLine +
                "Tartim: " + FormatBosDeger(row.IkinciTartim) + Environment.NewLine +
                "Cikis: " + FormatBosDeger(row.CikisTarihi + " " + row.CikisSaati) + Environment.NewLine +
                "Net: " + FormatBosDeger(row.NetAgirlik) + Environment.NewLine + Environment.NewLine +
                "Ucret Dokumu" + Environment.NewLine +
                "Giris-Cikis: " + FormatBosDeger(row.GirisCikisUcreti) + Environment.NewLine +
                "Tartim: " + FormatBosDeger(row.TartimUcreti) + Environment.NewLine +
                "Bekleme: " + FormatBosDeger(row.BeklemeUcreti) + Environment.NewLine +
                "Toplam Tahakkuk: " + FormatBosDeger(row.Ucret) + Environment.NewLine +
                "Tahsilat: " + FormatBosDeger(row.Tahsilat) +
                GetPaymentHistoryText(row.Plaka);
        }

        private static string TryBuildKantarDosyasiDetail(VehicleMovementRow row)
        {
            var normalized = NormalizePlaka(row.Plaka);
            using (var context = new KantarDbContext())
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

                return
                    "Plaka: " + dosya.Arac.Plaka + Environment.NewLine +
                    "Firma: " + FormatBosDeger(dosya.Arac.FirmaAdi) + Environment.NewLine +
                    "Durum: " + GetVisitRowDurum(ikinciIslem ?? ilkIslem, dosya) + Environment.NewLine +
                    "Net: " + FormatBosDeger(dosya.NetAgirlikKg.HasValue ? dosya.NetAgirlikKg.Value.ToString("N0") + " kg" : "") + Environment.NewLine + Environment.NewLine +
                    "Ilk Ziyaret" + Environment.NewLine +
                    FormatVisitBlock(ilkIslem, dosya.IlkTartim) + Environment.NewLine + Environment.NewLine +
                    "Ikinci Ziyaret" + Environment.NewLine +
                    FormatVisitBlock(ikinciIslem, dosya.KarsiTartim) + Environment.NewLine + Environment.NewLine +
                    "Odeme Gecmisi (Fatura ID | Tarih | Tutar)" + Environment.NewLine +
                    FormatKantarDosyasiPaymentHistory(ilkIslem, ikinciIslem);
            }
        }

        private static string GetPaymentHistoryText(string plaka)
        {
            var normalized = NormalizePlaka(plaka);
            using (var context = new KantarDbContext())
            {
                var tahsilatlar = context.IslemUcretleri
                    .Include(x => x.Islem.Arac)
                    .Where(x => x.Islem.Arac.Plaka == normalized && x.TahsilEdildiMi && x.TahsilTarihi.HasValue)
                    .ToList()
                    .GroupBy(x => new
                    {
                        FaturaId = string.IsNullOrWhiteSpace(x.FaturaId) ? "Eski kayit" : x.FaturaId,
                        TahsilTarihi = x.TahsilTarihi.Value
                    })
                    .OrderByDescending(x => x.Key.TahsilTarihi)
                    .Take(10)
                    .ToList();

                if (tahsilatlar.Count == 0)
                {
                    return Environment.NewLine + Environment.NewLine + "Odeme Gecmisi" + Environment.NewLine + "-";
                }

                var satirlar = tahsilatlar.Select(x =>
                    x.Key.FaturaId + " | " +
                    x.Key.TahsilTarihi.ToString("dd.MM.yyyy HH:mm:ss") + " | " +
                    FormatPara(x.Sum(u => u.Tutar)));

                return Environment.NewLine + Environment.NewLine +
                    "Odeme Gecmisi (Fatura ID | Tarih | Tutar)" + Environment.NewLine +
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
                "Gelis: " + FormatBosDeger(islem != null ? islem.GirisTarihi.ToString("dd.MM.yyyy HH:mm:ss") : "") + Environment.NewLine +
                "Cikis: " + FormatBosDeger(islem != null && islem.CikisTarihi.HasValue ? islem.CikisTarihi.Value.ToString("dd.MM.yyyy HH:mm:ss") : "") + Environment.NewLine +
                "Tartim: " + FormatBosDeger(tartim != null ? tartim.TartimTarihi.ToString("dd.MM.yyyy HH:mm:ss") + " | " + tartim.AgirlikKg.ToString("N0") + " kg" : "") + Environment.NewLine +
                "Giris-Cikis: " + FormatBosDeger(islem != null ? FormatUcretKalemi(islem, KantarSabitleri.UcretKodu.GirisCikis) : "") + Environment.NewLine +
                "Tartim: " + FormatBosDeger(islem != null ? FormatUcretKalemi(islem, KantarSabitleri.UcretKodu.Tartim) : "") + Environment.NewLine +
                "Bekleme: " + FormatBosDeger(islem != null ? FormatUcretKalemi(islem, KantarSabitleri.UcretKodu.Bekleme) : "") + Environment.NewLine +
                "Tahakkuk: " + FormatBosDeger(islem != null ? FormatPara(islem.ToplamTahakkuk) : "") + Environment.NewLine +
                "Tahsilat: " + FormatBosDeger(islem != null ? FormatPara(islem.ToplamTahsilat) : "") + Environment.NewLine +
                "Fatura ID: " + FormatBosDeger(GetLastInvoiceId(islem));
        }

        private static string FormatKantarDosyasiPaymentHistory(Islem ilkIslem, Islem ikinciIslem)
        {
            var ucretler = new[] { ilkIslem, ikinciIslem }
                .Where(x => x != null)
                .SelectMany(x => x.Ucretler)
                .Where(x => x.TahsilEdildiMi && x.TahsilTarihi.HasValue)
                .GroupBy(x => new
                {
                    FaturaId = string.IsNullOrWhiteSpace(x.FaturaId) ? "Eski kayit" : x.FaturaId,
                    TahsilTarihi = x.TahsilTarihi.Value
                })
                .OrderBy(x => x.Key.TahsilTarihi)
                .ToList();

            if (ucretler.Count == 0)
            {
                return "-";
            }

            return string.Join(Environment.NewLine, ucretler.Select(x =>
                x.Key.FaturaId + " | " +
                x.Key.TahsilTarihi.ToString("dd.MM.yyyy HH:mm:ss") + " | " +
                FormatPara(x.Sum(u => u.Tutar))));
        }

        private static string GetLastInvoiceId(Islem islem)
        {
            if (islem == null)
            {
                return "";
            }

            var fatura = islem.Ucretler
                .Where(x => x.TahsilEdildiMi && !string.IsNullOrWhiteSpace(x.FaturaId))
                .OrderByDescending(x => x.TahsilTarihi)
                .Select(x => x.FaturaId)
                .FirstOrDefault();

            return fatura ?? "";
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
                string.Equals(FormatBosDeger(agirlik), tartimWeight, StringComparison.Ordinal);
        }

        private static bool AltCikisListesindeGoster(Islem islem, KantarDosyasi dosya, Tartim ilkTartim, Tartim ikinciTartim)
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

        private static string FormatSaat(DateTime tarih)
        {
            if (tarih.Date == DateTime.Today)
            {
                return tarih.ToString("HH:mm");
            }

            return tarih.ToString("dd.MM HH:mm");
        }

        private static string FormatSaatSaniyeli(DateTime tarih)
        {
            return tarih.ToString("HH:mm:ss");
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
                return "Giris: " + girisTartimi.AgirlikKg.ToString("N0") + " kg";
            }

            var sonradanTartim = islem.Tartimlar
                .Where(x => x.TartimTipi == KantarSabitleri.TartimTipi.Sonradan)
                .OrderByDescending(x => x.TartimTarihi)
                .FirstOrDefault();

            if (sonradanTartim != null)
            {
                return "Dolu-Bos: " + sonradanTartim.AgirlikKg.ToString("N0") + " kg";
            }

            return "Tartimi Yok";
        }

        private static string FormatIkinciTartim(Islem islem)
        {
            var ikinciTartim = GetIkinciTartim(islem);
            if (ikinciTartim == null)
            {
                return "";
            }

            return ikinciTartim.AgirlikKg.ToString("N0") + " kg";
        }

        private static string FormatTartimTarihi(Tartim tartim)
        {
            return tartim == null ? "" : tartim.TartimTarihi.ToString("dd.MM.yyyy");
        }

        private static string FormatTartimSaati(Tartim tartim)
        {
            return tartim == null ? "" : tartim.TartimTarihi.ToString("HH:mm:ss");
        }

        private static string FormatSonTartim(Tartim tartim)
        {
            return tartim == null ? "Tartim Yok" : tartim.AgirlikKg.ToString("N0") + " kg";
        }

        private static string FormatTartimDegeri(Tartim tartim)
        {
            return tartim == null ? "" : tartim.AgirlikKg.ToString("N0") + " kg";
        }

        private static string FormatDoluGelisTarihi(Islem islem, Tartim ilkTartim)
        {
            return (ilkTartim != null ? ilkTartim.TartimTarihi : islem.GirisTarihi).ToString("dd.MM.yyyy");
        }

        private static string FormatDoluGelisSaati(Islem islem, Tartim ilkTartim)
        {
            return FormatSaatSaniyeli(ilkTartim != null ? ilkTartim.TartimTarihi : islem.GirisTarihi);
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
                return null;
            }

            var ilkTahsilat = islem.Ucretler
                .Where(x => x.TahsilTarihi.HasValue)
                .Select(x => x.TahsilTarihi)
                .OrderBy(x => x)
                .FirstOrDefault();

            return ilkTahsilat ?? islem.CikisTarihi;
        }

        private static string FormatBosDeger(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }

        private static string FormatNetAgirlik(Islem islem)
        {
            var ilkTartim = GetIlkTartim(islem);
            var ikinciTartim = GetIkinciTartim(islem);
            return FormatNetAgirlik(ilkTartim, ikinciTartim);
        }

        private static string FormatNetAgirlik(Tartim ilkTartim, Tartim ikinciTartim)
        {
            if (ilkTartim == null || ikinciTartim == null)
            {
                return "";
            }

            return Math.Abs(ilkTartim.AgirlikKg - ikinciTartim.AgirlikKg).ToString("N0") + " kg";
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

        private static string GetVisitRowDurum(Islem islem, KantarDosyasi dosya)
        {
            if (dosya == null)
            {
                return islem.GelisTuru == KantarSabitleri.GelisTuru.Tartimsiz ? "Tartimsiz cikis bekliyor" : "Cikis bekliyor";
            }

            if (dosya.Durum == KantarSabitleri.KantarDosyasiDurumu.Tamamlandi)
            {
                return "Dolu-bos tamamlandi";
            }

            return GetBeklenenTartimDurumu(dosya.IlkTartim);
        }

        private static string GetBeklenenTartimDurumu(Tartim ilkTartim)
        {
            return ilkTartim != null && ilkTartim.YukDurumu == KantarSabitleri.YukDurumu.Bos
                ? "Dolu bekleniyor"
                : "Bos bekleniyor";
        }

        private static Tartim GetIlkTartim(Islem islem)
        {
            return islem.Tartimlar
                .Where(x => x.TartimTipi == KantarSabitleri.TartimTipi.Giris)
                .OrderBy(x => x.TartimTarihi)
                .FirstOrDefault();
        }

        private static Tartim GetIkinciTartim(Islem islem)
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

        private static Tartim GetSonTartim(Islem islem)
        {
            return islem.Tartimlar
                .OrderByDescending(x => x.TartimTarihi)
                .FirstOrDefault();
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

            return FormatPara(toplam);
        }

        private DateTime GetListeHesapTarihi()
        {
            try
            {
                return ParseIslemTarihi(GirisTarihiTextBox.Text, "Islem tarihi");
            }
            catch
            {
                return DateTime.Now;
            }
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

        private static string FormatKalanBorc(Islem islem)
        {
            return FormatKalanBorc(islem, 0m);
        }

        private static string FormatKalanBorc(Islem islem, decimal ekBeklemeUcreti)
        {
            var kalan = islem.ToplamTahakkuk - islem.ToplamTahsilat;
            kalan += ekBeklemeUcreti;
            return FormatPara(kalan > 0 ? kalan : 0m);
        }

        private static string FormatPara(decimal tutar)
        {
            return tutar.ToString("N2") + " TL";
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
            using (var context = new KantarDbContext())
            {
                context.Database.ExecuteSqlCommand(
                    "IF COL_LENGTH('dbo.IslemUcretleri', 'FaturaId') IS NULL " +
                    "ALTER TABLE dbo.IslemUcretleri ADD FaturaId NVARCHAR(40) NULL");
                context.Database.ExecuteSqlCommand(
                    "IF COL_LENGTH('dbo.Islemler', 'GelisTuru') IS NULL " +
                    "ALTER TABLE dbo.Islemler ADD GelisTuru NVARCHAR(20) NOT NULL CONSTRAINT DF_Islemler_GelisTuru DEFAULT (N'Tartimsiz')");
                context.Database.ExecuteSqlCommand(
                    "IF COL_LENGTH('dbo.Tartimlar', 'YukDurumu') IS NULL " +
                    "ALTER TABLE dbo.Tartimlar ADD YukDurumu NVARCHAR(20) NULL");
                context.Database.ExecuteSqlCommand(
                    "IF COL_LENGTH('dbo.IslemUcretleri', 'TahsilatId') IS NULL " +
                    "ALTER TABLE dbo.IslemUcretleri ADD TahsilatId NVARCHAR(40) NULL");
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
                Aciklama = "X firma dolu cikis sonrasi bekleyen tartim"
            });
            PendingWeighings.Add(new PendingWeighingPrototypeRow
            {
                Plaka = "06DMS294",
                FirmaAdi = "YENI Nakliye",
                IlkTartimTarihi = "02.04.2026",
                IlkTartimSaati = "09:15:21",
                IlkAgirlik = "18200",
                YukDurumu = KantarSabitleri.YukDurumu.Dolu,
                Aciklama = "Ayni plaka farkli firma ornegi"
            });
            PendingWeighings.Add(new PendingWeighingPrototypeRow
            {
                Plaka = "16TLS4821",
                FirmaAdi = "Bursa Gumruk Depo",
                IlkTartimTarihi = "08.05.2026",
                IlkTartimSaati = "14:08:33",
                IlkAgirlik = "21480",
                YukDurumu = KantarSabitleri.YukDurumu.Dolu,
                Aciklama = "Tek bekleyen tartim ornegi"
            });
        }

        private void DoluBosBekleyenBul_Click(object sender, RoutedEventArgs e)
        {
            var plaka = NormalizePlaka(DoluBosPlakaTextBox.Text);
            if (!SelectPendingWeighingRow(plaka))
            {
                MessageBox.Show("Bu plaka icin bekleyen ilk tartim bulunamadi. Yeni ilk tartim olarak acilabilir.", "Dolu-Bos Tartim");
                DoluBosYeniIlkRadio.IsChecked = true;
                return;
            }
        }

        private bool SelectPendingWeighingRow(string plaka)
        {
            var normalizedPlaka = NormalizePlaka(plaka);
            var match = PendingWeighings.FirstOrDefault(x => NormalizePlaka(x.Plaka) == normalizedPlaka);
            if (match == null)
            {
                return false;
            }

            DoluBosBekleyenGrid.SelectedItem = match;
            DoluBosBekleyenGrid.ScrollIntoView(match);
            ApplyPendingWeighingSelection(match);
            return true;
        }

        private void DoluBosBekleyenGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var row = DoluBosBekleyenGrid.SelectedItem as PendingWeighingPrototypeRow;
            if (row != null)
            {
                ApplyPendingWeighingSelection(row);
            }
        }

        private void ApplyPendingWeighingSelection(PendingWeighingPrototypeRow row)
        {
            DoluBosEskiTartimRadio.IsChecked = true;
            DoluBosSeciliPlakaTextBox.Text = row.Plaka;
            DoluBosSeciliFirmaTextBox.Text = row.FirmaAdi;
            DoluBosIlkTartimTextBox.Text = row.IlkTartimTarihi + " " + row.IlkTartimSaati;
            DoluBosIlkKgTextBox.Text = row.IlkAgirlik;
            CalculateDoluBosNet();
        }

        private void DoluBosKiloAl_Click(object sender, RoutedEventArgs e)
        {
            DoluBosIkinciKgTextBox.Text = AgirlikTextBox != null && !string.IsNullOrWhiteSpace(AgirlikTextBox.Text)
                ? AgirlikTextBox.Text
                : "0";
            CalculateDoluBosNet();
        }

        private void DoluBosNetHesapla_Click(object sender, RoutedEventArgs e)
        {
            CalculateDoluBosNet();
        }

        private void DoluBosPrototypeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var row = DoluBosBekleyenGrid.SelectedItem as PendingWeighingPrototypeRow;
                if (DoluBosEskiTartimRadio.IsChecked == true && row == null)
                {
                    throw new InvalidOperationException("Once bekleyen ilk tartim satirini secin.");
                }

                var plaka = DoluBosEskiTartimRadio.IsChecked == true ? row.Plaka : DoluBosPlakaTextBox.Text;
                var firma = DoluBosEskiTartimRadio.IsChecked == true ? row.FirmaAdi : DoluBosFirmaTextBox.Text;
                var gelisTuru = row != null && row.YukDurumu == KantarSabitleri.YukDurumu.Bos
                    ? KantarSabitleri.GelisTuru.Dolu
                    : KantarSabitleri.GelisTuru.Bos;
                var ikinciKg = ParseAgirlik(DoluBosIkinciKgTextBox.Text);

                CreateEntry(plaka, firma, DoluBosAciklamaTextBox.Text, true, ikinciKg, DateTime.Now, gelisTuru);
                LoadDashboardData();
                ShowEntryPage();
                MessageBox.Show("Dolu-bos tartim kaydi acildi. Arac cikis yaptiginda tahsilati tamamlanip kesin cikisa aktarilacak.", "Dolu-Bos Tartim");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Dolu-bos tartim kaydedilemedi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CalculateDoluBosNet()
        {
            decimal ilkKg;
            decimal ikinciKg;
            if (!decimal.TryParse((DoluBosIlkKgTextBox.Text ?? string.Empty).Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out ilkKg) ||
                !decimal.TryParse((DoluBosIkinciKgTextBox.Text ?? string.Empty).Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out ikinciKg))
            {
                DoluBosNetKgTextBox.Text = "0";
                return;
            }

            DoluBosNetKgTextBox.Text = Math.Abs(ilkKg - ikinciKg).ToString("N0");
            DoluBosTahakkukTextBox.Text = FormatPara(732m);
        }

        private void ColumnFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            var tag = textBox != null ? textBox.Tag as string : null;
            var value = textBox != null ? textBox.Text : string.Empty;

            switch (tag)
            {
                case "EntryPlaka":
                    _entryPlakaFilter = NormalizePlaka(value);
                    EntryVehiclesView.Refresh();
                    SelectFirstVisibleVehicle(EntryVehiclesView, EntryVehiclesGrid);
                    SelectFirstVisibleVehicle(EntryVehiclesView, ExitPageEntryVehiclesGrid);
                    break;
                case "EntryFirma":
                    _entryFirmaFilter = NormalizeText(value);
                    EntryVehiclesView.Refresh();
                    SelectFirstVisibleVehicle(EntryVehiclesView, EntryVehiclesGrid);
                    SelectFirstVisibleVehicle(EntryVehiclesView, ExitPageEntryVehiclesGrid);
                    break;
                case "ExitPlaka":
                    _exitPlakaFilter = NormalizePlaka(value);
                    ExitVehiclesView.Refresh();
                    SelectFirstVisibleVehicle(ExitVehiclesView, ExitVehiclesGrid);
                    break;
                case "ExitFirma":
                    _exitFirmaFilter = NormalizeText(value);
                    ExitVehiclesView.Refresh();
                    SelectFirstVisibleVehicle(ExitVehiclesView, ExitVehiclesGrid);
                    break;
            }
        }

        private void EntryVehiclesGrid_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.OriginalSource is TextBox)
            {
                return;
            }

            if (AppendEntryGridSearch(e.Text, sender as DataGrid))
            {
                e.Handled = true;
            }
        }

        private void ExitVehiclesGrid_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.OriginalSource is TextBox)
            {
                return;
            }

            if (AppendExitGridSearch(e.Text, sender as DataGrid))
            {
                e.Handled = true;
            }
        }

        private void EntryVehiclesGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var keyText = GetSearchTextFromKey(e.Key);
            if (!string.IsNullOrEmpty(keyText) && AppendEntryGridSearch(keyText, sender as DataGrid))
            {
                e.Handled = true;
                return;
            }

            if (HandleEntryGridSearchKey(e.Key, sender as DataGrid))
            {
                e.Handled = true;
            }
        }

        private void ExitVehiclesGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var keyText = GetSearchTextFromKey(e.Key);
            if (!string.IsNullOrEmpty(keyText) && AppendExitGridSearch(keyText, sender as DataGrid))
            {
                e.Handled = true;
                return;
            }

            if (HandleExitGridSearchKey(e.Key, sender as DataGrid))
            {
                e.Handled = true;
            }
        }

        private bool AppendEntryGridSearch(string text, DataGrid sourceGrid)
        {
            return false;
        }

        private bool AppendExitGridSearch(string text, DataGrid sourceGrid)
        {
            return false;
        }

        private bool HandleEntryGridSearchKey(Key key, DataGrid sourceGrid)
        {
            if (key == Key.Escape)
            {
                _entryPlakaFilter = string.Empty;
                _entryFirmaFilter = string.Empty;
                EntryVehiclesView.Refresh();
                SelectFirstVisibleVehicle(EntryVehiclesView, sourceGrid);
                return true;
            }

            return false;
        }

        private bool HandleExitGridSearchKey(Key key, DataGrid sourceGrid)
        {
            if (key == Key.Escape)
            {
                _exitPlakaFilter = string.Empty;
                _exitFirmaFilter = string.Empty;
                ExitVehiclesView.Refresh();
                SelectFirstVisibleVehicle(ExitVehiclesView, sourceGrid);
                return true;
            }

            return false;
        }

        private bool FilterEntryVehicle(object item)
        {
            return FilterVehicle(item, _entryPlakaFilter, _entryFirmaFilter);
        }

        private bool FilterExitVehicle(object item)
        {
            return FilterVehicle(item, _exitPlakaFilter, _exitFirmaFilter);
        }

        private static bool FilterVehicle(object item, string plakaFilter, string firmaFilter)
        {
            var row = item as VehicleMovementRow;
            if (row == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(plakaFilter) &&
                !NormalizePlaka(row.Plaka).StartsWith(plakaFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(firmaFilter) &&
                !NormalizeText(row.FirmaAdi).Contains(firmaFilter))
            {
                return false;
            }

            return true;
        }

        private static string GetSearchTextFromKey(Key key)
        {
            if (key >= Key.A && key <= Key.Z)
            {
                return key.ToString();
            }

            if (key >= Key.D0 && key <= Key.D9)
            {
                return ((int)(key - Key.D0)).ToString(CultureInfo.InvariantCulture);
            }

            if (key >= Key.NumPad0 && key <= Key.NumPad9)
            {
                return ((int)(key - Key.NumPad0)).ToString(CultureInfo.InvariantCulture);
            }

            return null;
        }

        private static void SelectFirstVisibleVehicle(ICollectionView view, DataGrid grid)
        {
            if (view == null || grid == null)
            {
                return;
            }

            var first = view.Cast<object>().FirstOrDefault();
            if (first == null)
            {
                grid.SelectedItem = null;
                return;
            }

            grid.SelectedItem = first;
            grid.ScrollIntoView(first);
            if (grid.Columns.Count > 0)
            {
                grid.CurrentCell = new DataGridCellInfo(first, grid.Columns[0]);
            }
            grid.Focus();
        }

        private void ClearVehicleFilters()
        {
            _entryPlakaFilter = string.Empty;
            _entryFirmaFilter = string.Empty;
            _exitPlakaFilter = string.Empty;
            _exitFirmaFilter = string.Empty;
        }
    }

    public class VehicleMovementRow
    {
        public string Plaka { get; set; }
        public string FirmaAdi { get; set; }
        public string GirisTarihi { get; set; }
        public string GirisSaati { get; set; }
        public string CikisTarihi { get; set; }
        public string CikisSaati { get; set; }
        public string DoluCikisTarihi { get; set; }
        public string DoluCikisSaati { get; set; }
        public string BosGelisTarihi { get; set; }
        public string BosGelisSaati { get; set; }
        public string IlkTartimTarihi { get; set; }
        public string IlkTartimSaati { get; set; }
        public string IkinciTartimTarihi { get; set; }
        public string IkinciTartimSaati { get; set; }
        public string SonTartimTarihi { get; set; }
        public string SonTartimSaati { get; set; }
        public string SonTartim { get; set; }
        public string Saat { get; set; }
        public string Tartim { get; set; }
        public string IkinciTartim { get; set; }
        public string NetAgirlik { get; set; }
        public string Ucret { get; set; }
        public string Tahsilat { get; set; }
        public string GirisCikisUcreti { get; set; }
        public string TartimUcreti { get; set; }
        public string BeklemeUcreti { get; set; }
        public string Durum { get; set; }
        public bool KesinCikisMi { get; set; }
    }

    public class DailyTransactionRow
    {
        public string IslemNo { get; set; }
        public string Plaka { get; set; }
        public string Tip { get; set; }
        public string Ucret { get; set; }
        public string Kullanici { get; set; }
    }

    public class PendingWeighingPrototypeRow
    {
        public string Plaka { get; set; }
        public string FirmaAdi { get; set; }
        public string IlkGirisTarihi { get; set; }
        public string IlkGirisSaati { get; set; }
        public string IlkCikisTarihi { get; set; }
        public string IlkCikisSaati { get; set; }
        public string IlkTartimTarihi { get; set; }
        public string IlkTartimSaati { get; set; }
        public string IlkAgirlik { get; set; }
        public string YukDurumu { get; set; }
        public string Aciklama { get; set; }
    }
}
