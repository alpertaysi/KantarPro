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
        public ObservableCollection<DailyRevenueRow> DailyRevenueRows { get; private set; }
        public ObservableCollection<PendingWeighingPrototypeRow> PendingWeighings { get; private set; }
        public ICollectionView EntryVehiclesView { get; private set; }
        public ICollectionView ExitVehiclesView { get; private set; }
        public ICollectionView DailyRevenueView { get; private set; }

        private string _entryPlakaFilter = string.Empty;
        private string _entryFirmaFilter = string.Empty;
        private string _exitPlakaFilter = string.Empty;
        private string _exitFirmaFilter = string.Empty;
        private string _revenuePlakaFilter = string.Empty;
        private string _revenueFirmaFilter = string.Empty;
        private string _plateCorrectionOriginalPlate;
        private bool _manualGirisSaati;
        private bool _manualCikisSaati;
        private bool _isAutoRefreshing;
        private KantarSerialReader _scaleReader;
        private decimal? _lastScaleWeightKg;

        public MainWindow()
        {
            EntryVehicles = new ObservableCollection<VehicleMovementRow>();
            ExitVehicles = new ObservableCollection<VehicleMovementRow>();
            DailyTransactions = new ObservableCollection<DailyTransactionRow>();
            DailyRevenueRows = new ObservableCollection<DailyRevenueRow>();
            PendingWeighings = new ObservableCollection<PendingWeighingPrototypeRow>();
            EntryVehiclesView = CollectionViewSource.GetDefaultView(EntryVehicles);
            ExitVehiclesView = CollectionViewSource.GetDefaultView(ExitVehicles);
            DailyRevenueView = CollectionViewSource.GetDefaultView(DailyRevenueRows);
            EntryVehiclesView.Filter = FilterEntryVehicle;
            ExitVehiclesView.Filter = FilterExitVehicle;
            DailyRevenueView.Filter = FilterDailyRevenue;

            DataContext = this;
            InitializeComponent();
            LoadStationSettings();
            UpdateStationStatus();
            StartScaleReader();
            Closing += MainWindow_Closing;

            try
            {
                EnsureDatabaseSchema();
                LoadPendingWeighingPrototypeData();
                ShowEntryPage();
            }
            catch (Exception ex)
            {
                ShowSettingsPage(false);
                MessageBox.Show(
                    "Veritabani baglantisi kurulamadigi icin Ayarlar ekrani acildi. " +
                    "Bu bilgisayarin SQL ve istasyon bilgilerini girip Baglantiyi Test Et butonunu kullanin.\n\n" +
                    ex.Message,
                    "Baglanti ayari gerekli",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            var timer = new DispatcherTimer { Interval = System.TimeSpan.FromSeconds(1) };
            GirisSaatiTextBox.Text = DateTime.Now.ToString("HH:mm:ss");
            CikisSaatiTextBox.Text = DateTime.Now.ToString("HH:mm:ss");
            timer.Tick += (sender, args) =>
            {
                var saat = DateTime.Now.ToString("HH:mm:ss");
                ClockText.Text = saat;
                if (!_manualGirisSaati)
                {
                    GirisSaatiTextBox.Text = saat;
                }

                if (!_manualCikisSaati)
                {
                    CikisSaatiTextBox.Text = saat;
                }
            };
            timer.Start();

            var autoRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            autoRefreshTimer.Tick += (sender, args) => TryAutoRefreshDashboard();
            autoRefreshTimer.Start();
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

        private void GunlukHasilatButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDailyRevenuePage();
        }

        private void AyarlarButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSettingsPage();
        }

        private void TartVeKaydet_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_plateCorrectionOriginalPlate))
            {
                DashboardPlakaDegistir();
                return;
            }

            if (string.IsNullOrWhiteSpace(PlakaTextBox.Text))
            {
                MessageBox.Show("Plaka girin.", "Kantar Pro", MessageBoxButton.OK, MessageBoxImage.Warning);
                PlakaTextBox.Focus();
                return;
            }

            if (IsMuafPendingDoluBosPlate(PlakaTextBox.Text))
            {
                DashboardGirisKaydiOlustur(true);
                return;
            }

            var dialog = new EntrySaveChoiceWindow
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            if (dialog.Choice == EntrySaveChoice.Exempt)
            {
                var exemptionDialog = new ExemptionReasonWindow(PlakaTextBox.Text)
                {
                    Owner = this
                };

                if (exemptionDialog.ShowDialog() != true)
                {
                    return;
                }

                DashboardGirisKaydiOlustur(true, true, exemptionDialog.ExemptionReason);
                return;
            }

            DashboardGirisKaydiOlustur(dialog.Choice == EntrySaveChoice.WeighAndSave);
        }

        private void DashboardGirisKaydiOlustur(bool tartimIsteniyor, bool muafMi = false, string muafiyetNedeni = null)
        {
            try
            {
                var plaka = PlakaTextBox.Text;
                var firmaAdi = FirmaTextBox.Text;
                var aciklama = AciklamaTextBox.Text;
                var agirlik = tartimIsteniyor ? ParseAgirlik(AgirlikTextBox.Text) : (decimal?)null;
                var islemTarihi = ParseIslemTarihi(GirisTarihiTextBox.Text, GirisSaatiTextBox.Text, "Giris tarihi");

                if (tartimIsteniyor && TryCompletePendingDoluBosFromDashboard(plaka, agirlik.GetValueOrDefault(), islemTarihi, muafMi, muafiyetNedeni))
                {
                    ClearDashboardEntryForm();
                    return;
                }

                if (!muafMi && !tartimIsteniyor && TryOpenPendingDoluBosWithoutWeighingFromDashboard(plaka, islemTarihi))
                {
                    ClearDashboardEntryForm();
                    return;
                }

                CreateEntry(plaka, firmaAdi, aciklama, tartimIsteniyor, agirlik, islemTarihi, tartimIsteniyor ? null : KantarSabitleri.GelisTuru.Tartimsiz, muafMi, muafiyetNedeni);

                LoadDashboardData();
                MessageBox.Show("Giris kaydi olusturuldu.", "Kantar Pro");
                ClearDashboardEntryForm();
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
            AutoRefreshStatusText.Text = "Son guncelleme: " + DateTime.Now.ToString("HH:mm:ss");
        }

        private void TryAutoRefreshDashboard()
        {
            if (_isAutoRefreshing || DashboardContent.Visibility != Visibility.Visible)
            {
                return;
            }

            if (!AutoRefreshPolicy.ShouldRefresh(IsEditingInput(), IsAnyChildWindowOpen()))
            {
                AutoRefreshStatusText.Text = "Oto yenileme: bekliyor";
                return;
            }

            var selectedEntryPlate = GetSelectedPlate(EntryVehiclesGrid);
            var selectedExitPlate = GetSelectedPlate(ExitVehiclesGrid);
            _isAutoRefreshing = true;
            try
            {
                LoadDashboardData(false);
                RestoreSelectedPlate(EntryVehiclesGrid, selectedEntryPlate);
                RestoreSelectedPlate(ExitVehiclesGrid, selectedExitPlate);
                AutoRefreshStatusText.Text = "Son guncelleme: " + DateTime.Now.ToString("HH:mm:ss");
            }
            catch
            {
                AutoRefreshStatusText.Text = "Oto yenileme basarisiz";
            }
            finally
            {
                _isAutoRefreshing = false;
            }
        }

        private bool IsEditingInput()
        {
            var focused = Keyboard.FocusedElement as DependencyObject;
            while (focused != null)
            {
                if (focused is TextBox || focused is PasswordBox || focused is ComboBox)
                {
                    return true;
                }

                focused = VisualTreeHelper.GetParent(focused);
            }

            return false;
        }

        private bool IsAnyChildWindowOpen()
        {
            return System.Windows.Application.Current.Windows.OfType<Window>().Any(x => x != this && x.IsVisible);
        }

        private static string GetSelectedPlate(DataGrid grid)
        {
            var row = grid != null ? grid.SelectedItem as VehicleMovementRow : null;
            return row != null ? row.Plaka : null;
        }

        private static void RestoreSelectedPlate(DataGrid grid, string plate)
        {
            if (grid == null || string.IsNullOrWhiteSpace(plate))
            {
                return;
            }

            var row = grid.Items
                .OfType<VehicleMovementRow>()
                .FirstOrDefault(x => string.Equals(NormalizePlaka(x.Plaka), NormalizePlaka(plate), StringComparison.OrdinalIgnoreCase));
            if (row != null)
            {
                grid.SelectedItem = row;
                grid.ScrollIntoView(row);
            }
        }

        private void CikisIsleminiTamamla_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var plaka = GetPlakaForOperation();
                var tartimIsteniyor = false;
                var agirlik = (decimal?)null;
                var cikisTarihi = ParseIslemTarihi(CikisTarihiTextBox.Text, CikisSaatiTextBox.Text, "Cikis tarihi");

                if (ShowExitConfirmation(plaka, tartimIsteniyor, agirlik, cikisTarihi))
                {
                    LoadDashboardData();
                    MessageBox.Show("Cikis islemi tamamlandi.", "Kantar Pro");
                    PlakaTextBox.Clear();
                    AgirlikTextBox.Text = "0";
                    CikisTarihiTextBox.Text = DateTime.Today.ToString("dd.MM.yyyy");
                    _manualCikisSaati = false;
                    CikisSaatiTextBox.Text = DateTime.Now.ToString("HH:mm:ss");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Cikis islemi tamamlanamadi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void KantarFisiYazdir_Click(object sender, RoutedEventArgs e)
        {
            var row = EntryVehiclesGrid.SelectedItem as VehicleMovementRow
                ?? ExitVehiclesGrid.SelectedItem as VehicleMovementRow;
            ShowKantarFisiPreview(row);
        }

        private void EntryGridMakbuzYazdirMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var row = EntryVehiclesGrid.SelectedItem as VehicleMovementRow;
            if (row == null)
            {
                MessageBox.Show("Makbuz yazdirilacak satiri secin.", "Kantar Pro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ShowKantarFisiPreview(row);
        }

        private void EntryGridMakbuzGosterMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var row = EntryVehiclesGrid.SelectedItem as VehicleMovementRow;
            if (row == null)
            {
                MessageBox.Show("Makbuzu gosterilecek satiri secin.", "Kantar Pro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ShowKantarFisiPreview(row);
        }

        private void ShowKantarFisiPreview(VehicleMovementRow row)
        {
            if (row == null)
            {
                MessageBox.Show("Kantar fisi gosterilecek satiri secin.", "Kantar Pro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var preview = new KantarFisPreviewWindow(KantarFisFormatter.BuildFromRow(row))
                {
                    Owner = this
                };
                preview.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Kantar fisi olusturulamadi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ShowPendingKantarFisiPreview(PendingWeighingPrototypeRow row)
        {
            if (row == null)
            {
                MessageBox.Show("Kantar fisi gosterilecek satiri secin.", "Kantar Pro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var preview = new KantarFisPreviewWindow(KantarFisFormatter.BuildFromPendingRow(row))
                {
                    Owner = this
                };
                preview.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Kantar fisi olusturulamadi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EntryGridPlakaDuzeltMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var row = EntryVehiclesGrid.SelectedItem as VehicleMovementRow;
            if (row == null)
            {
                MessageBox.Show("Duzeltilecek kayit satirini secin.", "Kantar Pro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _plateCorrectionOriginalPlate = NormalizePlaka(row.Plaka);
            PlakaTextBox.Text = row.Plaka;
            FirmaTextBox.Text = row.FirmaAdi;
            EntrySaveButton.Content = "Değiştir";
            EntryCorrectionCancelButton.Visibility = Visibility.Visible;
            PlakaTextBox.Focus();
            PlakaTextBox.SelectAll();
        }

        private void EntryGridFirmaGuncelleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var row = EntryVehiclesGrid.SelectedItem as VehicleMovementRow;
            if (row == null)
            {
                MessageBox.Show("Firma bilgisi guncellenecek satiri secin.", "Kantar Pro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new FirmaUpdateWindow(row.Plaka, row.FirmaAdi)
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                using (var context = KantarDbContextFactory.Create())
                {
                    var servis = new SahaZiyaretiServisi(new KantarUnitOfWork(context));
                    servis.FirmaAdiniGuncelle(row.Plaka, dialog.FirmaAdi);
                }

                LoadDashboardData();
                MessageBox.Show("Firma bilgisi guncellendi.", "Kantar Pro");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Firma guncellenemedi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EntryCorrectionCancelButton_Click(object sender, RoutedEventArgs e)
        {
            ClearDashboardEntryForm();
            ClearPlateCorrectionMode();
            PlakaTextBox.Focus();
        }

        private void EntryGridMuafMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var row = EntryVehiclesGrid.SelectedItem as VehicleMovementRow;
            if (row == null)
            {
                MessageBox.Show("Ücretten muaf yapılacak satırı seçin.", "Kantar Pro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new ExemptionReasonWindow(row.Plaka)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var context = KantarDbContextFactory.Create())
                    {
                        var kullaniciId = EnsureAdminUser(context);
                        var servis = new SahaZiyaretiServisi(new KantarUnitOfWork(context));
                        servis.IslemMuafYap(row.IslemId, dialog.ExemptionReason, kullaniciId);
                    }

                    LoadDashboardData();
                    MessageBox.Show($"{row.Plaka} plakalı araç ücretten muaf olarak güncellendi.", "Ücretten Muafiyet");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Muafiyet tanımlanamadı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
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

        private void ExitVehiclesGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = FindParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if (row == null)
            {
                return;
            }

            row.IsSelected = true;
            ExitVehiclesGrid.SelectedItem = row.Item;
            ExitVehiclesGrid.Focus();
        }

        private void PendingWeighingsGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = FindParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if (row == null)
            {
                return;
            }

            row.IsSelected = true;
            PendingWeighingsGrid.SelectedItem = row.Item;
            PendingWeighingsGrid.Focus();
        }

        private void ExitGridMakbuzYazdirMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowKantarFisiPreview(ExitVehiclesGrid.SelectedItem as VehicleMovementRow);
        }

        private void ExitGridMakbuzGosterMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowKantarFisiPreview(ExitVehiclesGrid.SelectedItem as VehicleMovementRow);
        }

        private void PendingGridMakbuzYazdirMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowPendingKantarFisiPreview(PendingWeighingsGrid.SelectedItem as PendingWeighingPrototypeRow);
        }

        private void PendingGridMakbuzGosterMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowPendingKantarFisiPreview(PendingWeighingsGrid.SelectedItem as PendingWeighingPrototypeRow);
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
                var tartimTarihi = ParseIslemTarihi(CikisTarihiTextBox.Text, CikisSaatiTextBox.Text, "Cikis tarihi");

                using (var context = KantarDbContextFactory.Create())
                {
                    var normalized = NormalizePlaka(row.Plaka);
                    var openIslem = context.Islemler
                        .Include(x => x.Arac)
                        .Include(x => x.Tartimlar)
                        .FirstOrDefault(x => x.Arac.Plaka == normalized && x.Durum == KantarSabitleri.IslemDurumu.Iceride);
                    if (openIslem != null && IsOpenVisitLinkedToCompletedKantarDosyasi(context, openIslem.IslemId))
                    {
                        MessageBox.Show("Bu aracin dolu-bos tartimi tamamlandi. Kesin cikis yapilmadan yeni tartim eklenemez.", "Tartim eklenemedi", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var pendingRow = FindPendingDoluBosRow(context, row.Plaka);
                    var openVisitPendingRow = openIslem != null ? BuildOpenVisitSecondWeighingRow(openIslem, tartimTarihi) : null;
                    if (openVisitPendingRow != null)
                    {
                        var dialog = new DoluBosSecondWeighingWindow(openVisitPendingRow, agirlik.Value, tartimTarihi)
                        {
                            Owner = this
                        };

                        if (dialog.ShowDialog() != true)
                        {
                            return;
                        }

                        var doluBosKullaniciId = EnsureAdminUser(context);
                        var doluBosServis = new SahaZiyaretiServisi(new KantarUnitOfWork(context));
                        doluBosServis.SonradanTartimEkle(row.Plaka, GetExpectedYukDurumuForOpenVisit(openIslem), dialog.SecondWeightKg, doluBosKullaniciId, tartimTarihi);

                        LoadDashboardData();
                        MessageBox.Show("Dolu-bos ikinci tartim eklendi.", "Kantar Pro");
                        return;
                    }

                    if (openIslem != null && pendingRow != null)
                    {
                        var dialog = new DoluBosSecondWeighingWindow(pendingRow, agirlik.Value, tartimTarihi)
                        {
                            Owner = this
                        };

                        if (dialog.ShowDialog() != true)
                        {
                            return;
                        }

                        var doluBosKullaniciId = EnsureAdminUser(context);
                        var doluBosServis = new SahaZiyaretiServisi(new KantarUnitOfWork(context));
                        doluBosServis.SonradanTartimEkle(row.Plaka, GetExpectedYukDurumuForPending(pendingRow), dialog.SecondWeightKg, doluBosKullaniciId, tartimTarihi);

                        LoadDashboardData();
                        MessageBox.Show("Dolu-bos ikinci tartim eklendi.", "Kantar Pro");
                        return;
                    }

                    var yukDurumu = openIslem != null
                        ? GetExpectedYukDurumuForOpenVisit(openIslem)
                        : YukDurumuFromRow(row);
                    var kullaniciId = EnsureAdminUser(context);
                    var servis = new SahaZiyaretiServisi(new KantarUnitOfWork(context));
                    servis.SonradanTartimEkle(row.Plaka, yukDurumu, agirlik.Value, kullaniciId, tartimTarihi);
                }

                LoadDashboardData();
                MessageBox.Show("Tartim eklendi.", "Kantar Pro");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Tartim eklenemedi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static bool IsOpenVisitLinkedToCompletedKantarDosyasi(KantarDbContext context, int islemId)
        {
            return context.KantarDosyalari.Any(x =>
                x.Durum == KantarSabitleri.KantarDosyasiDurumu.Tamamlandi &&
                (x.IlkTartim.IslemId == islemId || (x.KarsiTartimId.HasValue && x.KarsiTartim.IslemId == islemId)));
        }

        private static PendingWeighingPrototypeRow BuildOpenVisitSecondWeighingRow(Islem islem, DateTime cikisTarihi)
        {
            if (islem == null)
            {
                return null;
            }

            var tartimlar = islem.Tartimlar
                .OrderBy(x => x.TartimTarihi)
                .ToList();
            if (tartimlar.Count != 1)
            {
                return null;
            }

            var ilkTartim = tartimlar[0];
            return new PendingWeighingPrototypeRow
            {
                Plaka = islem.Arac.Plaka,
                FirmaAdi = islem.Arac.FirmaAdi,
                IlkGirisTarihi = islem.GirisTarihi.ToString("dd.MM.yyyy"),
                IlkGirisSaati = islem.GirisTarihi.ToString("HH:mm:ss"),
                IlkCikisTarihi = cikisTarihi.ToString("dd.MM.yyyy"),
                IlkCikisSaati = cikisTarihi.ToString("HH:mm:ss"),
                IlkTartimTarihi = ilkTartim.TartimTarihi.ToString("dd.MM.yyyy"),
                IlkTartimSaati = ilkTartim.TartimTarihi.ToString("HH:mm:ss"),
                IlkAgirlik = ilkTartim.AgirlikKg.ToString("N0"),
                YukDurumu = ilkTartim.YukDurumu,
                Aciklama = DashboardVisitInfo.GetBeklenenTartimDurumu(ilkTartim)
            };
        }

        private void DashboardPlakaDegistir()
        {
            try
            {
                var eskiPlaka = NormalizePlaka(_plateCorrectionOriginalPlate);
                var yeniPlaka = NormalizePlaka(PlakaTextBox.Text);
                var yeniFirma = (FirmaTextBox.Text ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(yeniPlaka))
                {
                    throw new InvalidOperationException("Yeni plaka bos olamaz.");
                }

                using (var context = KantarDbContextFactory.Create())
                {
                    var islem = context.Islemler
                        .Include(x => x.Arac)
                        .Include(x => x.Tartimlar)
                        .FirstOrDefault(x => x.Arac.Plaka == eskiPlaka && x.Durum == KantarSabitleri.IslemDurumu.Iceride);

                    if (islem == null)
                    {
                        throw new InvalidOperationException("Bu plaka icin acik saha ziyareti bulunamadi.");
                    }

                    if (eskiPlaka != yeniPlaka)
                    {
                        var hedefArac = context.Araclar.FirstOrDefault(x => x.Plaka == yeniPlaka);
                        if (hedefArac != null && hedefArac.AracId != islem.AracId)
                        {
                            var hedefPlakadaAcikIslemVarMi = context.Islemler.Any(x =>
                                x.AracId == hedefArac.AracId &&
                                x.Durum == KantarSabitleri.IslemDurumu.Iceride &&
                                x.IslemId != islem.IslemId);
                            if (hedefPlakadaAcikIslemVarMi)
                            {
                                throw new InvalidOperationException("Yeni plaka ile iceride acik kayit var. Kayit duzeltilemez.");
                            }

                            decimal? onaylananIkinciTartim = null;
                            var sonTartim = islem.Tartimlar.OrderBy(x => x.TartimTarihi).LastOrDefault();
                            var pendingRow = FindPendingDoluBosRow(context, yeniPlaka);
                            if (pendingRow != null && sonTartim != null)
                            {
                                var dialog = new DoluBosSecondWeighingWindow(pendingRow, sonTartim.AgirlikKg, sonTartim.TartimTarihi)
                                {
                                    Owner = this
                                };

                                if (dialog.ShowDialog() != true)
                                {
                                    return;
                                }

                                onaylananIkinciTartim = dialog.SecondWeightKg;
                            }

                            var kullaniciId = EnsureAdminUser(context);
                            var servis = new SahaZiyaretiServisi(new KantarUnitOfWork(context));
                            servis.PlakaHatasiniDuzelt(eskiPlaka, yeniPlaka, onaylananIkinciTartim, kullaniciId);
                        }
                        else if (hedefArac == null)
                        {
                            var acikIslemVarMi = context.Islemler.Any(x =>
                                x.Arac.Plaka == yeniPlaka &&
                                x.Durum == KantarSabitleri.IslemDurumu.Iceride &&
                                x.IslemId != islem.IslemId);
                            if (acikIslemVarMi)
                            {
                                throw new InvalidOperationException("Yeni plaka ile iceride acik kayit var. Kayit duzeltilemez.");
                            }

                            islem.Arac.Plaka = yeniPlaka;
                        }
                    }

                    islem.Arac.FirmaAdi = yeniFirma;
                    context.SaveChanges();
                }

                LoadDashboardData();
                MessageBox.Show("Kayit duzeltildi.", "Kantar Pro");
                ClearDashboardEntryForm();
                ClearPlateCorrectionMode();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Kayit duzeltilemedi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ClearPlateCorrectionMode()
        {
            _plateCorrectionOriginalPlate = null;
            EntrySaveButton.Content = "Kaydet";
            EntryCorrectionCancelButton.Visibility = Visibility.Collapsed;
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
                using (var context = KantarDbContextFactory.Create())
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

        private void SettingsRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadStationSettings();
            LoadFeeSettings();
        }

        private void SettingsRefreshComPortsButton_Click(object sender, RoutedEventArgs e)
        {
            PopulateComPortOptions(GetSelectedComboBoxText(SettingsComPortComboBox));
        }

        private void SettingsTestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = ReadStationSettingsFromForm();
                using (var context = new KantarDbContext(settings.BuildConnectionString()))
                {
                    context.Database.Connection.Open();
                    context.Database.Connection.Close();
                }

                SettingsConnectionInfoText.Text = "Baglanti basarili: " + settings.SqlServerAddress + " / " + settings.DatabaseName;
            }
            catch (Exception ex)
            {
                SettingsConnectionInfoText.Text = "Baglanti basarisiz: " + ex.Message;
            }
        }

        private void SettingsSaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var stationSettings = ReadStationSettingsFromForm();
                StationSettingsStore.Save(stationSettings);

                var girisCikis = ParseFee(SettingsEntryExitFeeTextBox.Text, "Giris-Cikis ucreti");
                var tartim = ParseFee(SettingsWeighingFeeTextBox.Text, "Tartim ucreti");
                var bekleme = ParseFee(SettingsWaitingFeeTextBox.Text, "Bekleme ucreti");

                using (var context = new KantarDbContext(stationSettings.BuildConnectionString()))
                {
                    var servis = new UcretAyarlariServisi(new KantarUnitOfWork(context));
                    servis.Guncelle(DateTime.Now, girisCikis, tartim, bekleme);
                }

                UpdateStationStatus();
                StartScaleReader();
                LoadFeeSettings();
                LoadDashboardData();
                MessageBox.Show("Ayarlar kaydedildi. SQL, istasyon, COM ve ucret bilgileri guncellendi.", "Ayarlar");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ayarlar kaydedilemedi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static bool IsMuafPendingDoluBosPlate(string plaka)
        {
            var normalized = NormalizePlaka(plaka);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            using (var context = KantarDbContextFactory.Create())
            {
                return context.KantarDosyalari
                    .Include(x => x.Arac)
                    .Include(x => x.IlkTartim)
                    .Include(x => x.IlkTartim.Islem)
                    .Any(x =>
                        x.Arac.Plaka == normalized &&
                        x.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor &&
                        x.IlkTartim != null &&
                        x.IlkTartim.Islem != null &&
                        x.IlkTartim.Islem.Durum == KantarSabitleri.IslemDurumu.CikisYapti &&
                        x.IlkTartim.Islem.MuafMi);
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

        private void PendingWeighingsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var grid = sender as DataGrid;
            var row = grid != null ? grid.SelectedItem as PendingWeighingPrototypeRow : null;
            if (row != null && SelectedVehicleDetailTextBox != null)
            {
                SelectedVehicleDetailTextBox.Text = BuildPendingWeighingDetail(row);
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

        private void CreateEntry(string plaka, string firmaAdi, string aciklama, bool tartimIsteniyor, decimal? agirlik, DateTime islemTarihi, string gelisTuruOverride = null, bool muafMi = false, string muafiyetNedeni = null)
        {
            using (var context = KantarDbContextFactory.Create())
            {
                var kullaniciId = EnsureAdminUser(context);
                var servis = new SahaZiyaretiServisi(new KantarUnitOfWork(context));
                var gelisTuru = gelisTuruOverride ?? servis.GelisTuruOner(plaka);

                var normalizedPlaka = NormalizePlaka(plaka);
                var acikIslemVarMi = context.Islemler.Any(x => x.Arac.Plaka == normalizedPlaka && x.Durum == KantarSabitleri.IslemDurumu.Iceride);
                if (acikIslemVarMi)
                {
                    throw new InvalidOperationException("Bu plaka icin iceride acik islem var. Yeni giris yapilamaz. Ikinci tartim icin listedeki satira sag tiklayip Tart secenegini kullanin veya Cikis Yap islemini tamamlayin.");
                }

                servis.GirisKaydet(plaka, firmaAdi, gelisTuru, tartimIsteniyor, agirlik, kullaniciId, islemTarihi, muafMi, muafiyetNedeni);
            }
        }

        private bool TryCompletePendingDoluBosFromDashboard(string plaka, decimal scaleWeightKg, DateTime secondWeighingDate, bool muafMi = false, string muafiyetNedeni = null)
        {
            using (var context = KantarDbContextFactory.Create())
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

                CreateEntry(pendingRow.Plaka, pendingRow.FirmaAdi, pendingRow.Aciklama, true, dialog.SecondWeightKg, secondWeighingDate, gelisTuru, muafMi, muafiyetNedeni);
            }

            LoadDashboardData();
            ShowEntryPage();
            MessageBox.Show("Ikinci tartim kaydedildi. Arac ust listedeki dolu-bos kaydina alindi.", "Dolu-Bos Tartim");
            return true;
        }

        private bool TryOpenPendingDoluBosWithoutWeighingFromDashboard(string plaka, DateTime entryDate)
        {
            PendingWeighingPrototypeRow pendingRow;
            using (var context = KantarDbContextFactory.Create())
            {
                pendingRow = FindPendingDoluBosRow(context, plaka);
            }

            if (pendingRow == null)
            {
                return false;
            }

            var gelisTuru = pendingRow.YukDurumu == KantarSabitleri.YukDurumu.Bos
                ? KantarSabitleri.GelisTuru.Dolu
                : KantarSabitleri.GelisTuru.Bos;

            CreateEntry(pendingRow.Plaka, pendingRow.FirmaAdi, pendingRow.Aciklama, false, null, entryDate, gelisTuru);
            LoadDashboardData();
            ShowEntryPage();
            MessageBox.Show("Bekleyen dolu-bos kaydi icin tartimsiz giris acildi. Arac cikis yaptiginda tahsilat ust listedeki kayittan alinacak.", "Dolu-Bos Tartim");
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
                Aciklama = DashboardVisitInfo.GetBeklenenTartimDurumu(bekleyen.IlkTartim)
            };
        }

        private void ShowEntryPage()
        {
            DashboardContent.Visibility = Visibility.Visible;
            EntryPageContent.Visibility = Visibility.Collapsed;
            ExitPageContent.Visibility = Visibility.Collapsed;
            DoluBosPageContent.Visibility = Visibility.Collapsed;
            DailyRevenueContent.Visibility = Visibility.Collapsed;
            SettingsContent.Visibility = Visibility.Collapsed;
            LoadDashboardData();
            PlakaTextBox.Focus();
        }

        private void ShowExitPage()
        {
            DashboardContent.Visibility = Visibility.Visible;
            EntryPageContent.Visibility = Visibility.Collapsed;
            ExitPageContent.Visibility = Visibility.Collapsed;
            DoluBosPageContent.Visibility = Visibility.Collapsed;
            DailyRevenueContent.Visibility = Visibility.Collapsed;
            SettingsContent.Visibility = Visibility.Collapsed;
            LoadDashboardData();
            PlakaTextBox.Focus();
        }

        private void ShowDoluBosPage()
        {
            DashboardContent.Visibility = Visibility.Collapsed;
            EntryPageContent.Visibility = Visibility.Collapsed;
            ExitPageContent.Visibility = Visibility.Collapsed;
            DoluBosPageContent.Visibility = Visibility.Visible;
            DailyRevenueContent.Visibility = Visibility.Collapsed;
            SettingsContent.Visibility = Visibility.Collapsed;
            DoluBosPlakaTextBox.Focus();
        }

        private void ShowDailyRevenuePage()
        {
            DashboardContent.Visibility = Visibility.Collapsed;
            EntryPageContent.Visibility = Visibility.Collapsed;
            ExitPageContent.Visibility = Visibility.Collapsed;
            DoluBosPageContent.Visibility = Visibility.Collapsed;
            DailyRevenueContent.Visibility = Visibility.Visible;
            SettingsContent.Visibility = Visibility.Collapsed;
            LoadDailyRevenueData();
        }

        private void ShowSettingsPage(bool loadFees = true)
        {
            DashboardContent.Visibility = Visibility.Collapsed;
            EntryPageContent.Visibility = Visibility.Collapsed;
            ExitPageContent.Visibility = Visibility.Collapsed;
            DoluBosPageContent.Visibility = Visibility.Collapsed;
            DailyRevenueContent.Visibility = Visibility.Collapsed;
            SettingsContent.Visibility = Visibility.Visible;
            if (loadFees)
            {
                LoadFeeSettings();
            }
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

        private void ClearDashboardEntryForm()
        {
            PlakaTextBox.Clear();
            FirmaTextBox.Clear();
            AciklamaTextBox.Clear();
            AgirlikTextBox.Text = "0";
            ClearPlateCorrectionMode();
        }

        private void LoadFeeSettings()
        {
            try
            {
                using (var context = KantarDbContextFactory.Create())
                {
                    var servis = new UcretAyarlariServisi(new KantarUnitOfWork(context));
                    var ayarlar = servis.Getir(DateTime.Now);
                    SettingsEntryExitFeeTextBox.Text = ayarlar.GirisCikisUcreti.ToString("N2");
                    SettingsWeighingFeeTextBox.Text = ayarlar.TartimUcreti.ToString("N2");
                    SettingsWaitingFeeTextBox.Text = ayarlar.BeklemeUcreti.ToString("N2");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ucret ayarlari okunamadi: " + ex.Message, "Ayarlar", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadStationSettings()
        {
            var settings = StationSettingsStore.Load();
            SettingsSqlServerTextBox.Text = settings.SqlServerAddress;
            SettingsDatabaseTextBox.Text = settings.DatabaseName;
            SettingsWindowsAuthCheckBox.IsChecked = settings.UseWindowsAuthentication;
            SettingsSqlUserTextBox.Text = settings.SqlUsername;
            SettingsSqlPasswordTextBox.Text = settings.SqlPassword;
            PopulateComPortOptions(settings.ComPort);
            SelectComboBoxItem(SettingsStationTypeComboBox, settings.StationType);
            SettingsConnectionInfoText.Text = "Yerel ayar dosyasi: " + StationSettingsStore.GetSettingsPath();
        }

        private void PopulateComPortOptions(string selectedPort)
        {
            var normalizedSelection = (selectedPort ?? string.Empty).Trim().ToUpperInvariant();
            var portNames = KantarSerialReader.GetAvailablePortNames().ToList();
            if (!string.IsNullOrWhiteSpace(normalizedSelection) &&
                !portNames.Any(x => string.Equals(x, normalizedSelection, StringComparison.OrdinalIgnoreCase)))
            {
                portNames.Insert(0, normalizedSelection);
            }

            SettingsComPortComboBox.ItemsSource = portNames;
            SettingsComPortComboBox.SelectedItem = portNames
                .FirstOrDefault(x => string.Equals(x, normalizedSelection, StringComparison.OrdinalIgnoreCase));

            if (SettingsComPortComboBox.SelectedItem == null && portNames.Count == 1)
            {
                SettingsComPortComboBox.SelectedIndex = 0;
            }

            SettingsConnectionInfoText.Text = portNames.Count == 0
                ? "Bu bilgisayarda COM port bulunamadi. USB-RS232 ceviriciyi taktikdan sonra Portlari Yenile'ye basin."
                : "Bulunan COM portlar: " + string.Join(", ", portNames);
        }

        private StationSettings ReadStationSettingsFromForm()
        {
            var settings = new StationSettings
            {
                SqlServerAddress = (SettingsSqlServerTextBox.Text ?? string.Empty).Trim(),
                DatabaseName = (SettingsDatabaseTextBox.Text ?? string.Empty).Trim(),
                UseWindowsAuthentication = SettingsWindowsAuthCheckBox.IsChecked == true,
                SqlUsername = (SettingsSqlUserTextBox.Text ?? string.Empty).Trim(),
                SqlPassword = SettingsSqlPasswordTextBox.Text ?? string.Empty,
                StationType = GetSelectedComboBoxText(SettingsStationTypeComboBox),
                ComPort = GetSelectedComboBoxText(SettingsComPortComboBox).Trim()
            };

            if (string.IsNullOrWhiteSpace(settings.SqlServerAddress))
            {
                throw new InvalidOperationException("SQL Server / IP bos olamaz.");
            }

            if (string.IsNullOrWhiteSpace(settings.DatabaseName))
            {
                throw new InvalidOperationException("Veritabani adi bos olamaz.");
            }

            if (!settings.UseWindowsAuthentication && string.IsNullOrWhiteSpace(settings.SqlUsername))
            {
                throw new InvalidOperationException("SQL kullanici adi bos olamaz.");
            }

            if (settings.StationType != StationSettings.ExitStation)
            {
                settings.StationType = StationSettings.EntryStation;
            }

            return settings;
        }

        private void UpdateStationStatus()
        {
            var settings = StationSettingsStore.Load();
            SqlStatusText.Text = "SQL: " + settings.SqlServerAddress;
            ComStatusText.Text = "COM: " + (string.IsNullOrWhiteSpace(settings.ComPort) ? "Beklemede" : settings.ComPort);
            StationStatusText.Text = settings.StationType;
        }

        private void StartScaleReader()
        {
            StopScaleReader();

            var settings = StationSettingsStore.Load();
            if (string.IsNullOrWhiteSpace(settings.ComPort))
            {
                ComStatusText.Text = "COM: Beklemede";
                return;
            }

            try
            {
                _scaleReader = new KantarSerialReader();
                _scaleReader.WeightReceived += ScaleReader_WeightReceived;
                _scaleReader.ReadError += ScaleReader_ReadError;
                _scaleReader.Start(settings.ComPort);
                ComStatusText.Text = _scaleReader.IsOpen
                    ? "COM: " + settings.ComPort + " dinleniyor"
                    : "COM: Beklemede";
            }
            catch (Exception ex)
            {
                StopScaleReader();
                ComStatusText.Text = "COM: " + settings.ComPort + " hata";
                SettingsConnectionInfoText.Text = "COM baglantisi acilamadi: " + ex.Message;
            }
        }

        private void StopScaleReader()
        {
            if (_scaleReader == null)
            {
                return;
            }

            _scaleReader.WeightReceived -= ScaleReader_WeightReceived;
            _scaleReader.ReadError -= ScaleReader_ReadError;
            _scaleReader.Dispose();
            _scaleReader = null;
        }

        private void ScaleReader_WeightReceived(object sender, ScaleWeightReceivedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _lastScaleWeightKg = e.WeightKg;
                var text = e.WeightKg.ToString("0.##", CultureInfo.InvariantCulture);
                SetScaleTextBoxValue(AgirlikTextBox, text);
                SetScaleTextBoxValue(EntryAgirlikTextBox, text);
                SetScaleTextBoxValue(ExitAgirlikTextBox, text);

                var settings = StationSettingsStore.Load();
                ComStatusText.Text = "COM: " + settings.ComPort + " - " + text + " kg";
            }));
        }

        private void ScaleReader_ReadError(object sender, string message)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var settings = StationSettingsStore.Load();
                ComStatusText.Text = "COM: " + settings.ComPort + " okuma hatasi";
                SettingsConnectionInfoText.Text = "COM okuma hatasi: " + message;
            }));
        }

        private static void SetScaleTextBoxValue(TextBox textBox, string value)
        {
            if (textBox == null)
            {
                return;
            }

            textBox.Text = value;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            StopScaleReader();
        }

        private static void SelectComboBoxItem(ComboBox comboBox, string text)
        {
            foreach (var item in comboBox.Items)
            {
                var comboBoxItem = item as ComboBoxItem;
                if (comboBoxItem != null && string.Equals(comboBoxItem.Content as string, text, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = comboBoxItem;
                    return;
                }
            }

            comboBox.SelectedIndex = 0;
        }

        private static string GetSelectedComboBoxText(ComboBox comboBox)
        {
            var comboBoxItem = comboBox.SelectedItem as ComboBoxItem;
            if (comboBoxItem != null)
            {
                return comboBoxItem.Content as string;
            }

            var selectedText = comboBox.SelectedItem as string;
            if (selectedText != null)
            {
                return selectedText;
            }

            return comboBox.Text;
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
                using (var context = KantarDbContextFactory.Create())
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

        private static decimal ParseFee(string text, string alanAdi)
        {
            decimal value;
            var normalized = (text ?? string.Empty).Trim().Replace("TL", "").Replace("tl", "").Replace(".", "").Replace(',', '.');
            if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            {
                throw new ArgumentException(alanAdi + " sayisal olmalidir.");
            }

            if (value < 0)
            {
                throw new ArgumentException(alanAdi + " negatif olamaz.");
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

        private static DateTime ParseIslemTarihi(string tarihText, string saatText, string alanAdi)
        {
            DateTime tarih;
            if (!DateTime.TryParseExact((tarihText ?? string.Empty).Trim(), "dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.None, out tarih))
            {
                throw new ArgumentException(alanAdi + " gg.aa.yyyy formatinda olmalidir.");
            }

            DateTime saat;
            var normalizedSaat = (saatText ?? string.Empty).Trim();
            if (!DateTime.TryParseExact(normalizedSaat, new[] { "HH:mm:ss", "H:mm:ss", "HH:mm", "H:mm" }, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.None, out saat))
            {
                throw new ArgumentException("Giris saati sa:dk veya sa:dk:sn formatinda olmalidir.");
            }

            return tarih.Date.Add(saat.TimeOfDay);
        }

        private void GirisSaatiTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && textBox.IsKeyboardFocusWithin)
            {
                _manualGirisSaati = true;
            }
        }

        private void CikisSaatiTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && textBox.IsKeyboardFocusWithin)
            {
                _manualCikisSaati = true;
            }

            RefreshDashboardForManualExitDate(textBox);
        }

        private void CikisTarihiTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshDashboardForManualExitDate(sender as TextBox);
        }

        private void RefreshDashboardForManualExitDate(TextBox textBox)
        {
            if (textBox == null || !textBox.IsKeyboardFocusWithin)
            {
                return;
            }

            try
            {
                ParseIslemTarihi(CikisTarihiTextBox.Text, CikisSaatiTextBox.Text, "Cikis tarihi");
            }
            catch
            {
                return;
            }

            LoadDashboardData();
        }


    }
}

