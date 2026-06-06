using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using KantarPro.Application.Services;
using KantarPro.Domain;
using KantarPro.Domain.Entities;
using KantarPro.Infrastructure.Data;

namespace KantarPro.Desktop
{
    public partial class ExitConfirmationWindow : Window
    {
        private readonly string _plaka;
        private readonly bool _tartimIsteniyor;
        private readonly decimal? _agirlikKg;
        private readonly DateTime _cikisTarihi;
        private readonly int _kullaniciId;

        private Islem _islem;
        private decimal _previewToplam;

        public ExitConfirmationWindow(string plaka, bool tartimIsteniyor, decimal? agirlikKg, DateTime cikisTarihi, int kullaniciId)
        {
            _plaka = NormalizePlaka(plaka);
            _tartimIsteniyor = tartimIsteniyor;
            _agirlikKg = agirlikKg;
            _cikisTarihi = cikisTarihi;
            _kullaniciId = kullaniciId;

            InitializeComponent();
            LoadPreview();
        }

        private void LoadPreview()
        {
            using (var context = KantarDbContextFactory.Create())
            {
                var unitOfWork = new KantarUnitOfWork(context);
                var sahaServisi = new SahaZiyaretiServisi(unitOfWork);
                var ucretAyarlari = new UcretAyarlariServisi(unitOfWork).Getir(_cikisTarihi);
                _islem = sahaServisi.AcikZiyaretiGetir(_plaka);

                var girisCikisToplam = SumUnpaidFee(_islem, KantarSabitleri.UcretKodu.GirisCikis);
                var mevcutTartimAdedi = _islem.Ucretler.Count(x => x.Ucret != null && x.Ucret.UcretKodu == KantarSabitleri.UcretKodu.Tartim && !x.TahsilEdildiMi);
                var toplamTartimAdedi = mevcutTartimAdedi + (_tartimIsteniyor ? 1 : 0);
                var tartimToplam = SumUnpaidFee(_islem, KantarSabitleri.UcretKodu.Tartim) + (_tartimIsteniyor ? ucretAyarlari.TartimUcreti : 0m);
                var beklemeAdedi = SahaZiyaretiServisi.HesaplaBeklemeGunSayisi(_islem.GirisTarihi, _cikisTarihi);
                var beklemeToplam = SumUnpaidFee(_islem, KantarSabitleri.UcretKodu.Bekleme) + (beklemeAdedi * ucretAyarlari.BeklemeUcreti);

                _previewToplam = girisCikisToplam + tartimToplam + beklemeToplam;

                PlateText.Text = _islem.Arac.Plaka;
                EntryDateText.Text = _islem.GirisTarihi.ToString("dd.MM.yyyy");
                EntryTimeText.Text = _islem.GirisTarihi.ToString("HH:mm:ss");
                ExitDateText.Text = _cikisTarihi.ToString("dd.MM.yyyy");
                ExitTimeText.Text = _cikisTarihi.ToString("HH:mm:ss");
                EntryExitFeeText.Text = FormatMoney(girisCikisToplam);
                WeighingFeeLabel.Text = "Tahsil Edilecek Tartım (" + toplamTartimAdedi + " kez)";
                WeighingFeeText.Text = FormatMoney(tartimToplam);
                WaitingFeeLabel.Text = "İşgaliye Ücreti      (" + beklemeAdedi + " kez)";
                WaitingFeeText.Text = FormatMoney(beklemeToplam);
                TotalFeeText.Text = FormatMoney(_previewToplam);
                InfoText.Text = BuildInfoText();
            }
        }

        private void ConfirmExit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var odemeTuru = _islem != null && _islem.MuafMi
                    ? null
                    : SelectPaymentType();
                if (_islem != null && !_islem.MuafMi && string.IsNullOrWhiteSpace(odemeTuru))
                {
                    return;
                }

                using (var context = KantarDbContextFactory.Create())
                {
                    var servis = new SahaZiyaretiServisi(new KantarUnitOfWork(context));
                    servis.CikisYap(_plaka, _tartimIsteniyor, _agirlikKg, _kullaniciId, ParseExitDateTime(), odemeTuru);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Çıkış işlemi tamamlanamadı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private string SelectPaymentType()
        {
            var dialog = new PaymentTypeChoiceWindow
            {
                Owner = this
            };

            return dialog.ShowDialog() == true ? dialog.SelectedPaymentType : null;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private string BuildInfoText()
        {
            var tartimText = _tartimIsteniyor
                ? "Çıkış tartımı eklenecek: " + _agirlikKg.GetValueOrDefault().ToString("N0") + " kg"
                : "Çıkış tartımı eklenmeyecek.";

            return tartimText + "\nOnay sonrasi arac cikis listesine tasinir.";
        }

        private static decimal SumUnpaidFee(Islem islem, string ucretKodu)
        {
            return islem.Ucretler
                .Where(x => x.Ucret != null && x.Ucret.UcretKodu == ucretKodu && !x.TahsilEdildiMi)
                .Sum(x => x.Tutar);
        }

        private DateTime ParseExitDateTime()
        {
            DateTime tarih;
            if (!DateTime.TryParseExact((ExitDateText.Text ?? string.Empty).Trim(), "dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.None, out tarih))
            {
                throw new ArgumentException("Çıkış tarihi gg.aa.yyyy formatında olmalıdır.");
            }

            DateTime saat;
            if (!DateTime.TryParseExact((ExitTimeText.Text ?? string.Empty).Trim(), new[] { "HH:mm:ss", "H:mm:ss", "HH:mm", "H:mm" }, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.None, out saat))
            {
                throw new ArgumentException("Çıkış saati sa:dk veya sa:dk:sn formatında olmalıdır.");
            }

            return tarih.Date.Add(saat.TimeOfDay);
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

        private static string FormatMoney(decimal value)
        {
            return value.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"));
        }

        private static string NormalizePlaka(string plaka)
        {
            return (plaka ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", string.Empty);
        }
    }
}




