using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using KantarPro.Application.Services;
using KantarPro.Domain.Entities;
using KantarPro.Infrastructure.Data;

namespace KantarPro.Desktop
{
    public partial class UserSwitchWindow : Window
    {
        private List<UserOption> _users = new List<UserOption>();

        public UserSwitchWindow()
        {
            InitializeComponent();
            Loaded += UserSwitchWindow_Loaded;
        }

        public KullaniciOturumu AuthenticatedUser { get; private set; }

        private void UserSwitchWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUsers();
            PasswordBox.Focus();
        }

        private void LoadUsers()
        {
            try
            {
                using (var context = KantarDbContextFactory.Create())
                {
                    var servis = new KullaniciServisi(new KantarUnitOfWork(context));
                    _users = servis.KullanicilariListele(true)
                        .Select(UserOption.From)
                        .ToList();
                }

                UserComboBox.ItemsSource = _users;
                UserComboBox.SelectedIndex = _users.Count > 0 ? 0 : -1;
                HideInfo();
            }
            catch (Exception ex)
            {
                ShowInfo("Kullanıcı listesi okunamadı: " + ex.Message, true);
            }
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            TryLogin();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TryLogin();
            }
        }

        private void TryLogin()
        {
            try
            {
                var selected = GetSelectedUser();
                using (var context = KantarDbContextFactory.Create())
                {
                    var servis = new KullaniciServisi(new KantarUnitOfWork(context));
                    AuthenticatedUser = servis.GirisYap(selected.KullaniciAdi, PasswordBox.Password);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowInfo(ex.Message, true);
                PasswordBox.Clear();
                PasswordBox.Focus();
            }
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selected = GetSelectedUser();
                if (NewPasswordBox.Password != RepeatPasswordBox.Password)
                {
                    throw new InvalidOperationException("Yeni şifre ve tekrar alanı aynı olmalıdır.");
                }

                using (var context = KantarDbContextFactory.Create())
                {
                    var servis = new KullaniciServisi(new KantarUnitOfWork(context));
                    var session = servis.GirisYap(selected.KullaniciAdi, CurrentPasswordBox.Password);
                    servis.ParolaDegistir(
                        session.KullaniciId,
                        CurrentPasswordBox.Password,
                        NewPasswordBox.Password);
                }

                App.LogOperation(
                    selected.KullaniciAdi,
                    "Şifre değiştirildi",
                    "Kullanıcı, kullanıcı değiştirme penceresinden kendi şifresini değiştirdi.");
                ShowInfo("Şifre başarıyla değiştirildi.", false);
                CurrentPasswordBox.Clear();
                NewPasswordBox.Clear();
                RepeatPasswordBox.Clear();
                PasswordBox.Focus();
            }
            catch (Exception ex)
            {
                App.LogError("Kullanıcı değiştirme penceresinde şifre değiştirme", ex);
                ShowInfo(ex.Message, true);
                CurrentPasswordBox.Clear();
                NewPasswordBox.Clear();
                RepeatPasswordBox.Clear();
                CurrentPasswordBox.Focus();
            }
        }

        private UserOption GetSelectedUser()
        {
            var selected = UserComboBox.SelectedItem as UserOption;
            if (selected == null)
            {
                throw new InvalidOperationException("Kullanıcı seçin.");
            }

            return selected;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowInfo(string message, bool error)
        {
            InfoTextBlock.Text = message;
            InfoTextBlock.Foreground = error
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(153, 27, 27))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(6, 95, 70));
            InfoBorder.Background = error
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 242, 242))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(236, 253, 245));
            InfoBorder.BorderBrush = error
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(252, 165, 165))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(110, 231, 183));
            InfoBorder.Visibility = Visibility.Visible;
        }

        private void HideInfo()
        {
            InfoTextBlock.Text = string.Empty;
            InfoBorder.Visibility = Visibility.Collapsed;
        }

        private sealed class UserOption
        {
            public string KullaniciAdi { get; set; }
            public string DisplayName { get; set; }

            public static UserOption From(Kullanici kullanici)
            {
                return new UserOption
                {
                    KullaniciAdi = kullanici.KullaniciAdi,
                    DisplayName = string.Equals(
                        kullanici.KullaniciAdi,
                        "admin",
                        StringComparison.OrdinalIgnoreCase)
                        ? "Admin"
                        : (string.IsNullOrWhiteSpace(kullanici.AdSoyad)
                            ? kullanici.KullaniciAdi
                            : kullanici.AdSoyad)
                };
            }
        }
    }
}
