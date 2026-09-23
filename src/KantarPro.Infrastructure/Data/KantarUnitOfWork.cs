using System;
using System.Linq;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using KantarPro.Application.Abstractions;
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
                catch (DbUpdateException ex)
                {
                    if (IsActiveVehicleUniqueConstraint(ex))
                    {
                        throw new InvalidOperationException("Bu plaka icin baska bir bilgisayarda zaten acik saha ziyareti kaydedildi.", ex);
                    }

                    if (!IsNumberUniqueConstraint(ex) || !RegeneratePendingOperationNumbers())
                    {
                        throw;
                    }
                }
            }

            throw new InvalidOperationException("Eszamanli kayitlar nedeniyle islem numarasi tekrar uretilemedi.");
        }

        private bool IsNumberUniqueConstraint(DbUpdateException ex)
        {
            var message = ex.ToString();
            var cikisNoBekleyenIslem = _context.ChangeTracker.Entries<Islem>().Any(x =>
                (x.State == EntityState.Added || x.State == EntityState.Modified) &&
                !string.IsNullOrWhiteSpace(x.Entity.CikisNo) &&
                (x.State == EntityState.Added ||
                 !string.Equals((string)x.OriginalValues["CikisNo"], x.Entity.CikisNo, StringComparison.Ordinal)));
            var yeniIslem = _context.ChangeTracker.Entries<Islem>().Any(x => x.State == EntityState.Added);

            return (cikisNoBekleyenIslem && message.IndexOf("UX_Islemler_CikisNo", StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (yeniIslem && message.IndexOf("UX_Islemler_IslemNo", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private bool IsActiveVehicleUniqueConstraint(DbUpdateException ex)
        {
            return ex.ToString().IndexOf("UX_Islemler_Iceride_Arac", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool RegeneratePendingOperationNumbers()
        {
            var pendingOperationEntries = _context.ChangeTracker.Entries<Islem>()
                .Where(x => x.State == EntityState.Added ||
                            (x.State == EntityState.Modified &&
                             !string.Equals((string)x.OriginalValues["CikisNo"], x.Entity.CikisNo, StringComparison.Ordinal)))
                .ToList();

            if (pendingOperationEntries.Count == 0)
            {
                return false;
            }

            var sonCikisNo = _context.Set<Islem>()
                .Where(x => x.CikisNo != null && x.CikisNo != "")
                .Select(x => x.CikisNo)
                .ToList()
                .Select(ParseNumber)
                .DefaultIfEmpty(0)
                .Max();

            foreach (var entry in pendingOperationEntries)
            {
                var islem = entry.Entity;
                if (!string.IsNullOrWhiteSpace(islem.CikisNo))
                {
                    var eskiCikisNo = islem.CikisNo;
                    sonCikisNo++;
                    islem.CikisNo = sonCikisNo.ToString("00000");

                    foreach (var ucret in islem.Ucretler.Where(x =>
                        x.TahsilEdildiMi && string.Equals(x.TahsilatNo, eskiCikisNo, StringComparison.Ordinal)))
                    {
                        ucret.TahsilatNo = islem.CikisNo;
                    }
                }

                if (entry.State == EntityState.Added &&
                    !string.IsNullOrWhiteSpace(islem.IslemNo) &&
                    _context.Set<Islem>().Any(x => x.IslemNo == islem.IslemNo))
                {
                    islem.IslemNo = islem.IslemNo + "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
                }
            }

            return true;
        }

        private static int ParseNumber(string value)
        {
            int number;
            return int.TryParse(value, out number) ? number : 0;
        }
    }
}
