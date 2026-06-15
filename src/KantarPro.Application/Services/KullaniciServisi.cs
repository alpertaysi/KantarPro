using System;
using System.Collections.Generic;
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
            if (_unitOfWork.Kullanicilar.Query().Any())
            {
                return;
            }

            _unitOfWork.Kullanicilar.Add(new Kullanici
            {
                KullaniciAdi = "admin",
                ParolaHash = HashPassword("admin"),
                AdSoyad = "Admin Kullanıcı",
                Rol = KullaniciRolleri.Admin,
                AktifMi = true
            });

            _unitOfWork.Kullanicilar.Add(new Kullanici
            {
                KullaniciAdi = "memur",
                ParolaHash = HashPassword("memur"),
                AdSoyad = "Memur Kullanıcı",
                Rol = KullaniciRolleri.Memur,
                AktifMi = true
            });

            _unitOfWork.SaveChanges();
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

        public void ParolaDegistir(int kullaniciId, string mevcutParola, string yeniParola)
        {
            if (string.IsNullOrWhiteSpace(mevcutParola))
            {
                throw new InvalidOperationException("Mevcut sifre girilmelidir.");
            }

            if (string.IsNullOrWhiteSpace(yeniParola) || yeniParola.Trim().Length < 6)
            {
                throw new InvalidOperationException("Yeni sifre en az 6 karakter olmalidir.");
            }

            var kullanici = _unitOfWork.Kullanicilar.SingleOrDefault(x => x.KullaniciId == kullaniciId && x.AktifMi);
            if (kullanici == null)
            {
                throw new InvalidOperationException("Kullanici bulunamadi.");
            }

            if (!VerifyPassword(mevcutParola, kullanici.ParolaHash))
            {
                throw new InvalidOperationException("Mevcut sifre hatali.");
            }

            kullanici.ParolaHash = HashPassword(yeniParola.Trim());
            _unitOfWork.SaveChanges();
        }

        public Kullanici KullaniciEkle(string kullaniciAdi, string adSoyad, string rol, string parola)
        {
            kullaniciAdi = (kullaniciAdi ?? string.Empty).Trim();
            adSoyad = (adSoyad ?? string.Empty).Trim();
            rol = NormalizeRol(rol);

            if (string.IsNullOrWhiteSpace(kullaniciAdi))
            {
                throw new InvalidOperationException("Kullanici adi girilmelidir.");
            }

            if (string.IsNullOrWhiteSpace(adSoyad))
            {
                throw new InvalidOperationException("Ad soyad girilmelidir.");
            }

            if (string.IsNullOrWhiteSpace(parola) || parola.Trim().Length < 6)
            {
                throw new InvalidOperationException("Sifre en az 6 karakter olmalidir.");
            }

            var mevcutKullanici = _unitOfWork.Kullanicilar
                .SingleOrDefault(x => x.KullaniciAdi == kullaniciAdi);
            if (mevcutKullanici != null && mevcutKullanici.AktifMi)
            {
                throw new InvalidOperationException("Bu kullanici adi zaten kullaniliyor.");
            }

            if (mevcutKullanici != null)
            {
                mevcutKullanici.ParolaHash = HashPassword(parola.Trim());
                mevcutKullanici.AdSoyad = adSoyad;
                mevcutKullanici.Rol = rol;
                mevcutKullanici.AktifMi = true;
                mevcutKullanici.SonGirisTarihi = null;
                _unitOfWork.SaveChanges();
                return mevcutKullanici;
            }

            var kullanici = new Kullanici
            {
                KullaniciAdi = kullaniciAdi,
                ParolaHash = HashPassword(parola.Trim()),
                AdSoyad = adSoyad,
                Rol = rol,
                AktifMi = true
            };

            _unitOfWork.Kullanicilar.Add(kullanici);
            _unitOfWork.SaveChanges();
            return kullanici;
        }

        public Kullanici KullaniciGuncelle(int kullaniciId, string kullaniciAdi, string adSoyad, string rol, string yeniParola = null)
        {
            kullaniciAdi = (kullaniciAdi ?? string.Empty).Trim();
            adSoyad = (adSoyad ?? string.Empty).Trim();
            rol = NormalizeRol(rol);

            if (string.IsNullOrWhiteSpace(kullaniciAdi))
            {
                throw new InvalidOperationException("Kullanici adi girilmelidir.");
            }

            if (string.IsNullOrWhiteSpace(adSoyad))
            {
                throw new InvalidOperationException("Ad soyad girilmelidir.");
            }

            var mevcutKullanici = _unitOfWork.Kullanicilar
                .SingleOrDefault(x => x.KullaniciAdi == kullaniciAdi && x.KullaniciId != kullaniciId);
            if (mevcutKullanici != null && mevcutKullanici.AktifMi)
            {
                throw new InvalidOperationException("Bu kullanici adi baska bir hesap tarafindan kullaniliyor.");
            }

            var kullanici = _unitOfWork.Kullanicilar.SingleOrDefault(x => x.KullaniciId == kullaniciId && x.AktifMi);
            if (kullanici == null)
            {
                throw new InvalidOperationException("Guncellenecek kullanici bulunamadi veya pasif.");
            }

            kullanici.KullaniciAdi = kullaniciAdi;
            kullanici.AdSoyad = adSoyad;
            kullanici.Rol = rol;

            if (!string.IsNullOrWhiteSpace(yeniParola))
            {
                if (yeniParola.Trim().Length < 6)
                {
                    throw new InvalidOperationException("Sifre en az 6 karakter olmalidir.");
                }
                kullanici.ParolaHash = HashPassword(yeniParola.Trim());
            }

            _unitOfWork.SaveChanges();
            return kullanici;
        }

        public IList<Kullanici> KullanicilariListele(bool sadeceAktif)
        {
            var query = _unitOfWork.Kullanicilar.Query();
            if (sadeceAktif)
            {
                query = query.Where(x => x.AktifMi);
            }

            return query
                .OrderBy(x => x.KullaniciAdi)
                .ToList();
        }

        public void KullaniciSil(int silinecekKullaniciId, int yapanKullaniciId)
        {
            if (silinecekKullaniciId == yapanKullaniciId)
            {
                throw new InvalidOperationException("Kullanici kendi hesabini silemez.");
            }

            var kullanici = _unitOfWork.Kullanicilar.SingleOrDefault(x => x.KullaniciId == silinecekKullaniciId);
            if (kullanici == null)
            {
                throw new InvalidOperationException("Kullanici bulunamadi.");
            }

            if (!kullanici.AktifMi)
            {
                return;
            }

            if (string.Equals(kullanici.Rol, KullaniciRolleri.Admin, StringComparison.OrdinalIgnoreCase))
            {
                var aktifAdminSayisi = _unitOfWork.Kullanicilar.Query()
                    .Count(x => x.AktifMi && x.Rol == KullaniciRolleri.Admin);
                if (aktifAdminSayisi <= 1)
                {
                    throw new InvalidOperationException("Son aktif admin kullanicisi silinemez.");
                }
            }

            kullanici.AktifMi = false;
            _unitOfWork.SaveChanges();
        }

        private static string NormalizeRol(string rol)
        {
            if (string.Equals(rol, KullaniciRolleri.Admin, StringComparison.OrdinalIgnoreCase))
            {
                return KullaniciRolleri.Admin;
            }

            if (string.Equals(rol, KullaniciRolleri.Memur, StringComparison.OrdinalIgnoreCase))
            {
                return KullaniciRolleri.Memur;
            }

            throw new InvalidOperationException("Kullanici rolu Admin veya Memur olmalidir.");
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
