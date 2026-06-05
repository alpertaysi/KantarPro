using System;
using System.Linq;
using System.Security.Cryptography;
using KantarPro.Application.Abstractions;
using KantarPro.Domain.Entities;

namespace KantarPro.Application.Services
{
    public sealed class KullaniciOturumu
    {
        public int KullaniciId { get; set; }
        public string KullaniciAdi { get; set; }
        public string AdSoyad { get; set; }
        public string Rol { get; set; }

        public bool AdminMi
        {
            get { return string.Equals(Rol, KullaniciRolleri.Admin, StringComparison.OrdinalIgnoreCase); }
        }
    }

    public static class KullaniciRolleri
    {
        public const string Admin = "Admin";
        public const string Memur = "Memur";
    }

    public sealed class KullaniciServisi
    {
        private const int IterationCount = 10000;
        private readonly IUnitOfWork _unitOfWork;

        public KullaniciServisi(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public void VarsayilanKullanicilariOlustur()
        {
            var degisti = false;
            if (!_unitOfWork.Kullanicilar.Query().Any(x => x.KullaniciAdi == "admin"))
            {
                _unitOfWork.Kullanicilar.Add(new Kullanici
                {
                    KullaniciAdi = "admin",
                    ParolaHash = HashPassword("admin"),
                    AdSoyad = "Admin Kullanici",
                    Rol = KullaniciRolleri.Admin,
                    AktifMi = true
                });
                degisti = true;
            }

            if (!_unitOfWork.Kullanicilar.Query().Any(x => x.KullaniciAdi == "memur"))
            {
                _unitOfWork.Kullanicilar.Add(new Kullanici
                {
                    KullaniciAdi = "memur",
                    ParolaHash = HashPassword("memur"),
                    AdSoyad = "Memur Kullanici",
                    Rol = KullaniciRolleri.Memur,
                    AktifMi = true
                });
                degisti = true;
            }

            if (degisti)
            {
                _unitOfWork.SaveChanges();
            }
        }

        public KullaniciOturumu GirisYap(string kullaniciAdi, string parola)
        {
            kullaniciAdi = (kullaniciAdi ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrWhiteSpace(parola))
            {
                throw new InvalidOperationException("Kullanici adi ve sifre girilmelidir.");
            }

            var kullanici = _unitOfWork.Kullanicilar
                .SingleOrDefault(x => x.KullaniciAdi == kullaniciAdi && x.AktifMi);
            if (kullanici == null || !VerifyPassword(parola, kullanici.ParolaHash))
            {
                throw new InvalidOperationException("Kullanici adi veya sifre hatali.");
            }

            kullanici.SonGirisTarihi = DateTime.Now;
            _unitOfWork.SaveChanges();

            return new KullaniciOturumu
            {
                KullaniciId = kullanici.KullaniciId,
                KullaniciAdi = kullanici.KullaniciAdi,
                AdSoyad = kullanici.AdSoyad,
                Rol = kullanici.Rol
            };
        }

        public static string HashPassword(string password)
        {
            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, IterationCount))
            {
                return "PBKDF2$" + IterationCount + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(deriveBytes.GetBytes(32));
            }
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            if (storedHash == "DEVELOPMENT_PLACEHOLDER_HASH")
            {
                return password == "admin";
            }

            var parts = storedHash.Split('$');
            if (parts.Length != 4 || parts[0] != "PBKDF2")
            {
                return false;
            }

            int iterations;
            if (!int.TryParse(parts[1], out iterations))
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                var actual = deriveBytes.GetBytes(expected.Length);
                return FixedTimeEquals(actual, expected);
            }
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            var diff = 0;
            for (var i = 0; i < left.Length; i++)
            {
                diff |= left[i] ^ right[i];
            }

            return diff == 0;
        }
    }
}
