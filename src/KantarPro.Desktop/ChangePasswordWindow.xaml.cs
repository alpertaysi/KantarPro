using System;
using System.Windows;
using KantarPro.Application.Services;
using KantarPro.Infrastructure.Data;

namespace KantarPro.Desktop
{
    public partial class ChangePasswordWindow : Window
    {
        private readonly KullaniciOturumu _currentUser;

        public ChangePasswordWindow(KullaniciOturumu currentUser)
        {
            if (currentUser == null)
            {
                throw new ArgumentNullException("currentUser");
            }

            _currentUser = currentUser;
            InitializeComponent();
            UserInfoTextBlock.Text = _currentUser.KullaniciAdi + " - " + _currentUser.AdSoyad;
            CurrentPasswordBox.Focus();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show(this, "Yeni şifre ve tekrar alanı aynı olmalıdır.", "Şifre değiştirilemedi", MessageBoxButton.OK, MessageBoxImage.Warning);
                ClearNewPasswordFields();
                return;
            }

            try
            {
                using (var context = KantarDbContextFactory.Create())
                {
                    var servis = new KullaniciServisi(new KantarUnitOfWork(context));
                    servis.ParolaDegistir(_currentUser.KullaniciId, CurrentPasswordBox.Password, NewPasswordBox.Password);
                }

                App.LogOperation(_currentUser.KullaniciAdi, "Sifre degistirildi", "Kullanici kendi sifresini degistirdi.");
                MessageBox.Show(this, "Şifre başarıyla değiştirildi.", "Şifre Değiştir", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                App.LogError("Sifre degistirme", ex);
                MessageBox.Show(this, ex.Message, "Şifre değiştirilemedi", MessageBoxButton.OK, MessageBoxImage.Warning);
                CurrentPasswordBox.Clear();
                ClearNewPasswordFields();
                CurrentPasswordBox.Focus();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ClearNewPasswordFields()
        {
            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
            NewPasswordBox.Focus();
        }
    }
}
