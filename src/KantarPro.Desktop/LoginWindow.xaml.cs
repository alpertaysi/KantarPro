using System;
using System.Windows;
using System.Windows.Input;
using KantarPro.Application.Services;
using KantarPro.Infrastructure.Data;

namespace KantarPro.Desktop
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            Loaded += (sender, args) => UsernameTextBox.Focus();
        }

        public KullaniciOturumu AuthenticatedUser { get; private set; }

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
                using (var context = KantarDbContextFactory.Create())
                {
                    var servis = new KullaniciServisi(new KantarUnitOfWork(context));
                    servis.VarsayilanKullanicilariOlustur();
                    AuthenticatedUser = servis.GirisYap(UsernameTextBox.Text, PasswordBox.Password);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                InfoTextBlock.Text = ex.Message;
                PasswordBox.Clear();
                PasswordBox.Focus();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
