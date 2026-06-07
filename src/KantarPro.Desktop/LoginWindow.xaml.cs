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
    public partial class LoginWindow : Window
    {
        private List<Kullanici> _activeUsers = new List<Kullanici>();

        public LoginWindow()
        {
            InitializeComponent();
            Loaded += LoginWindow_Loaded;
        }

        public KullaniciOturumu AuthenticatedUser { get; private set; }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
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
                    servis.VarsayilanKullanicilariOlustur();
                    _activeUsers = servis.KullanicilariListele(true).ToList();
                }

                BindUsers();
                InfoBorder.Visibility = Visibility.Collapsed;
                InfoTextBlock.Text = string.Empty;
            }
            catch (Exception ex)
            {
                _activeUsers = new List<Kullanici>();
                UserComboBox.ItemsSource = null;
                InfoTextBlock.Text = "Kullanıcı listesi okunamadı: " + ex.Message;
                InfoBorder.Visibility = Visibility.Visible;
            }
        }

        private void BindUsers()
        {
            var users = _activeUsers
                .Select(x => new LoginUserOption
                {
                    KullaniciAdi = x.KullaniciAdi,
                    DisplayName = GetDisplayName(x)
                })
                .ToList();

            UserComboBox.ItemsSource = users;
            UserComboBox.SelectedIndex = users.Count > 0 ? 0 : -1;
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
                InfoBorder.Visibility = Visibility.Collapsed;
                InfoTextBlock.Text = string.Empty;

                var selectedUser = UserComboBox.SelectedItem as LoginUserOption;
                if (selectedUser == null)
                {
                    throw new InvalidOperationException("Oturum açacak kullanıcıyı seçin.");
                }

                using (var context = KantarDbContextFactory.Create())
                {
                    var servis = new KullaniciServisi(new KantarUnitOfWork(context));
                    AuthenticatedUser = servis.GirisYap(selectedUser.KullaniciAdi, PasswordBox.Password);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                InfoTextBlock.Text = ex.Message;
                InfoBorder.Visibility = Visibility.Visible;
                PasswordBox.Clear();
                PasswordBox.Focus();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private static string GetDisplayName(Kullanici kullanici)
        {
            if (string.Equals(kullanici.KullaniciAdi, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return "Admin";
            }

            return string.IsNullOrWhiteSpace(kullanici.AdSoyad)
                ? kullanici.KullaniciAdi
                : kullanici.AdSoyad;
        }

        private sealed class LoginUserOption
        {
            public string KullaniciAdi { get; set; }
            public string DisplayName { get; set; }

            public override string ToString()
            {
                return DisplayName ?? string.Empty;
            }
        }
    }
}
