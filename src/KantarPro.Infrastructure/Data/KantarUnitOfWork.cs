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
            return _context.SaveChanges();
        }
    }
}
