using System;
using System.Collections.Generic;
using KantarPro.Application.Abstractions;
using KantarPro.Domain;
using KantarPro.Domain.Entities;

namespace KantarPro.Application.Tests.Fakes
{
    public class InMemoryUnitOfWork : IUnitOfWork
    {
        public readonly List<Arac> AracListesi = new List<Arac>();
        public readonly List<Islem> IslemListesi = new List<Islem>();
        public readonly List<Tartim> TartimListesi = new List<Tartim>();
        public readonly List<KantarDosyasi> KantarDosyasiListesi = new List<KantarDosyasi>();
        public readonly List<Ucret> UcretListesi = new List<Ucret>();
        public readonly List<IslemUcreti> IslemUcretiListesi = new List<IslemUcreti>();
        public readonly List<BekleyenTartim> BekleyenTartimListesi = new List<BekleyenTartim>();
        public readonly List<Kullanici> KullaniciListesi = new List<Kullanici>();
        public readonly List<LogKaydi> LogListesi = new List<LogKaydi>();
        public readonly List<Ayar> AyarListesi = new List<Ayar>();

        public InMemoryUnitOfWork()
        {
            var yil = 2026;
            UcretListesi.Add(new Ucret { UcretId = 1, UcretKodu = KantarSabitleri.UcretKodu.GirisCikis, UcretAdi = "Giris-Cikis Ucreti", Yil = yil, Tutar = 366m, AktifMi = true, GecerlilikBaslangic = new DateTime(yil, 1, 1) });
            UcretListesi.Add(new Ucret { UcretId = 2, UcretKodu = KantarSabitleri.UcretKodu.Tartim, UcretAdi = "Tartim Ucreti", Yil = yil, Tutar = 366m, AktifMi = true, GecerlilikBaslangic = new DateTime(yil, 1, 1) });
            UcretListesi.Add(new Ucret { UcretId = 3, UcretKodu = KantarSabitleri.UcretKodu.Bekleme, UcretAdi = "Bekleme Ucreti", Yil = yil, Tutar = 816m, AktifMi = true, GecerlilikBaslangic = new DateTime(yil, 1, 1) });

            Araclar = new InMemoryRepository<Arac>(AracListesi);
            Islemler = new InMemoryRepository<Islem>(IslemListesi);
            Tartimlar = new InMemoryRepository<Tartim>(TartimListesi);
            KantarDosyalari = new InMemoryRepository<KantarDosyasi>(KantarDosyasiListesi);
            Ucretler = new InMemoryRepository<Ucret>(UcretListesi);
            IslemUcretleri = new InMemoryRepository<IslemUcreti>(IslemUcretiListesi);
            BekleyenTartimlar = new InMemoryRepository<BekleyenTartim>(BekleyenTartimListesi);
            Kullanicilar = new InMemoryRepository<Kullanici>(KullaniciListesi);
            Loglar = new InMemoryRepository<LogKaydi>(LogListesi);
            Ayarlar = new InMemoryRepository<Ayar>(AyarListesi);
        }

        public IRepository<Arac> Araclar { get; private set; }
        public IRepository<Islem> Islemler { get; private set; }
        public IRepository<Tartim> Tartimlar { get; private set; }
        public IRepository<KantarDosyasi> KantarDosyalari { get; private set; }
        public IRepository<Ucret> Ucretler { get; private set; }
        public IRepository<IslemUcreti> IslemUcretleri { get; private set; }
        public IRepository<BekleyenTartim> BekleyenTartimlar { get; private set; }
        public IRepository<Kullanici> Kullanicilar { get; private set; }
        public IRepository<LogKaydi> Loglar { get; private set; }
        public IRepository<Ayar> Ayarlar { get; private set; }

        public int SaveChanges()
        {
            return 1;
        }
    }
}
