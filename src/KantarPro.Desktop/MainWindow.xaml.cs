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
            EnsureDatabaseSchema();
            LoadPendingWeighingPrototypeData();
            ShowEntryPage();

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
                using (var context = new KantarDbContext())
                {
                    var normalized = NormalizePlaka(row.Plaka);
                    var arac = context.Araclar.FirstOrDefault(x => x.Plaka == normalized);
                    if (arac == null)
                    {
                        throw new InvalidOperationException("Arac kaydi bulunamadi.");
                    }

                    arac.FirmaAdi = dialog.FirmaAdi;
                    context.SaveChanges();
                }

                LoadDashboardData();
                MessageBox.Show("Firma bilgisi guncellendi.", "Kantar Pro");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Firma guncellenemedi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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
                    using (var context = new KantarDbContext())
                    {
                        var normalized = NormalizePlaka(row.Plaka);
                        var islem = context.Islemler
                            .Include(x => x.Arac)
                            .Include(x => x.Ucretler)
                            .FirstOrDefault(x => x.Arac.Plaka == normalized && x.Durum == KantarSabitleri.IslemDurumu.Iceride);

                        if (islem == null)
                        {
                            throw new InvalidOperationException("Bu araç için sahada açık ziyaret kaydı bulunamadı.");
                        }

                        islem.MuafMi = true;
                        islem.MuafiyetNedeni = dialog.ExemptionReason;

                        // Remove any existing accrued fees for this visit
                        var ucretler = islem.Ucretler.ToList();
                        foreach (var ucret in ucretler)
                        {
                            context.IslemUcretleri.Remove(ucret);
                        }
                        islem.Ucretler.Clear();
                        islem.ToplamTahakkuk = 0m;
                        islem.ToplamTahsilat = 0m;

                        context.SaveChanges();
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

                using (var context = new KantarDbContext())
                {
                    var normalized = NormalizePlaka(row.Plaka);
                    var openIslem = context.Islemler
                        .Include(x => x.Arac)
                        .Include(x => x.Tartimlar)
                        .FirstOrDefault(x => x.Arac.Plaka == normalized && x.Durum == KantarSabitleri.IslemDurumu.Iceride);
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

                using (var context = new KantarDbContext())
                {
                    var islem = context.Islemler
                        .Include(x => x.Arac)
                        .FirstOrDefault(x => x.Arac.Plaka == eskiPlaka && x.Durum == KantarSabitleri.IslemDurumu.Iceride);

                    if (islem == null)
                    {
                        throw new InvalidOperationException("Bu plaka icin acik saha ziyareti bulunamadi.");
                    }

                    if (eskiPlaka != yeniPlaka)
                    {
                        var plakaBaskaAractaVarMi = context.Araclar.Any(x => x.Plaka == yeniPlaka && x.AracId != islem.AracId);
                        if (plakaBaskaAractaVarMi)
                        {
                            throw new InvalidOperationException("Yeni plaka sistemde baska bir arac kaydinda var. Bu duzeltme icin once kayitlari kontrol edin.");
                        }

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

        private void SettingsRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadFeeSettings();
        }

        private void SettingsSaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var girisCikis = ParseFee(SettingsEntryExitFeeTextBox.Text, "Giris-Cikis ucreti");
                var tartim = ParseFee(SettingsWeighingFeeTextBox.Text, "Tartim ucreti");
                var bekleme = ParseFee(SettingsWaitingFeeTextBox.Text, "Bekleme ucreti");

                using (var context = new KantarDbContext())
                {
                    var servis = new UcretAyarlariServisi(new KantarUnitOfWork(context));
                    servis.Guncelle(DateTime.Now, girisCikis, tartim, bekleme);
                }

                LoadFeeSettings();
                LoadDashboardData();
                MessageBox.Show("Ucret ayarlari guncellendi. Yeni tahsilatlar bu fiyatlarla yapilacak.", "Ayarlar");
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

            using (var context = new KantarDbContext())
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
            using (var context = new KantarDbContext())
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
            using (var context = new KantarDbContext())
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

        private void ShowSettingsPage()
        {
            DashboardContent.Visibility = Visibility.Collapsed;
            EntryPageContent.Visibility = Visibility.Collapsed;
            ExitPageContent.Visibility = Visibility.Collapsed;
            DoluBosPageContent.Visibility = Visibility.Collapsed;
            DailyRevenueContent.Visibility = Visibility.Collapsed;
            SettingsContent.Visibility = Visibility.Visible;
            LoadFeeSettings();
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
            EntrySaveButton.Content = "Kaydet";
        }

        private void LoadFeeSettings()
        {
            try
            {
                using (var context = new KantarDbContext())
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
