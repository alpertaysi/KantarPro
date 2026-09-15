using System;
using System.Linq;
using EntityFramework.Exceptions.Common;
using KantarPro.Application.Abstractions;
using KantarPro.Domain;
using KantarPro.Domain.Entities;

namespace KantarPro.Infrastructure.Data
{
    public class KantarUnitOfWork : IUnitOfWork
    {
        private readonly KantarDbContext _context;

        public KantarUnitOfWork(KantarDbContext context)
        {
            _context = context;
            Araclar = new EfRepository<Arac>(_context);
            Islemler = new EfRepository<Islem>(_context);
            Tartimlar = new EfRepository<Tartim>(_context);
            KantarDosyalari = new EfRepository<KantarDosyasi>(_context);
            Ucretler = new EfRepository<Ucret>(_context);
            IslemUcretleri = new EfRepository<IslemUcreti>(_context);
            BekleyenTartimlar = new EfRepository<BekleyenTartim>(_context);
            Kullanicilar = new EfRepository<Kullanici>(_context);
            Loglar = new EfRepository<LogKaydi>(_context);
            Ayarlar = new EfRepository<Ayar>(_context);
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
            const int maxRetries = 3;

            for (var attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    return _context.SaveChanges();
                }
                catch (UniqueConstraintException ex)
                {
                    if (!ex.Message.Contains("UX_Islemler_CikisNo"))
                    {
                        throw;
                    }

                    if (!CikisNumarasiniYenile())
                    {
                        throw;
                    }
                }
            }

            throw new InvalidOperationException("Cikis numarasi eszamanli kayitlar nedeniyle tekrar uretilemedi.");
        }

        private bool CikisNumarasiniYenile()
        {
            var eklenecekIslemler = _context.ChangeTracker.Entries<Islem>()
                .Where(x => x.State == System.Data.Entity.EntityState.Added && !string.IsNullOrWhiteSpace(x.Entity.CikisNo))
                .Select(x => x.Entity)
                .ToList();

            if (eklenecekIslemler.Count == 0)
            {
                return false;
            }

            var sonNo = _context.Set<Islem>()
                .Where(x => x.CikisNo != null && x.CikisNo != "")
                .Select(x => x.CikisNo)
                .ToList()
                .Select(ParseCikisNo)
                .DefaultIfEmpty(0)
                .Max();

            foreach (var islem in eklenecekIslemler)
            {
                sonNo++;
                islem.CikisNo = sonNo.ToString("00000");
            }

            return true;
        }

        private static int ParseCikisNo(string value)
        {
            int number;
            return int.TryParse(value, out number) ? number : 0;
        }
    }
}
