using KantarPro.Domain.Entities;

namespace KantarPro.Application.Abstractions
{
    public interface IUnitOfWork
    {
        IRepository<Arac> Araclar { get; }
        IRepository<Islem> Islemler { get; }
        IRepository<Tartim> Tartimlar { get; }
        IRepository<Ucret> Ucretler { get; }
        IRepository<IslemUcreti> IslemUcretleri { get; }
        IRepository<BekleyenTartim> BekleyenTartimlar { get; }
        IRepository<LogKaydi> Loglar { get; }
        int SaveChanges();
    }
}

