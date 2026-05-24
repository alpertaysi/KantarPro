using System.Data.Entity;
using KantarPro.Domain.Entities;

namespace KantarPro.Infrastructure.Data
{
    public class KantarDbContext : DbContext
    {
        public KantarDbContext()
            : base("name=KantarDb")
        {
            System.Data.Entity.Database.SetInitializer<KantarDbContext>(null);
        }

        public KantarDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            System.Data.Entity.Database.SetInitializer<KantarDbContext>(null);
        }

        public DbSet<Arac> Araclar { get; set; }
        public DbSet<Islem> Islemler { get; set; }
        public DbSet<Tartim> Tartimlar { get; set; }
        public DbSet<KantarDosyasi> KantarDosyalari { get; set; }
        public DbSet<Ucret> Ucretler { get; set; }
        public DbSet<IslemUcreti> IslemUcretleri { get; set; }
        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<LogKaydi> Loglar { get; set; }
        public DbSet<BekleyenTartim> BekleyenTartimlar { get; set; }
        public DbSet<Ayar> Ayarlar { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Arac>().ToTable("Araclar");
            modelBuilder.Entity<Arac>().HasKey(x => x.AracId);
            modelBuilder.Entity<Arac>().Property(x => x.Plaka).HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Arac>().Property(x => x.FirmaAdi).HasMaxLength(150);
            modelBuilder.Entity<Arac>().Property(x => x.AracTipi).HasMaxLength(50);
            modelBuilder.Entity<Arac>().Property(x => x.Aciklama).HasMaxLength(250);

            modelBuilder.Entity<Kullanici>().ToTable("Kullanicilar");
            modelBuilder.Entity<Kullanici>().HasKey(x => x.KullaniciId);
            modelBuilder.Entity<Kullanici>().Property(x => x.KullaniciAdi).HasMaxLength(50).IsRequired();
            modelBuilder.Entity<Kullanici>().Property(x => x.ParolaHash).HasMaxLength(256).IsRequired();
            modelBuilder.Entity<Kullanici>().Property(x => x.AdSoyad).HasMaxLength(100).IsRequired();
            modelBuilder.Entity<Kullanici>().Property(x => x.Rol).HasMaxLength(30).IsRequired();

            modelBuilder.Entity<Ucret>().ToTable("Ucretler");
            modelBuilder.Entity<Ucret>().HasKey(x => x.UcretId);
            modelBuilder.Entity<Ucret>().Property(x => x.UcretKodu).HasMaxLength(30).IsRequired();
            modelBuilder.Entity<Ucret>().Property(x => x.UcretAdi).HasMaxLength(100).IsRequired();
            modelBuilder.Entity<Ucret>().Property(x => x.Tutar).HasPrecision(18, 2);

            modelBuilder.Entity<Islem>().ToTable("Islemler");
            modelBuilder.Entity<Islem>().HasKey(x => x.IslemId);
            modelBuilder.Entity<Islem>().Property(x => x.IslemNo).HasMaxLength(30).IsRequired();
            modelBuilder.Entity<Islem>().Property(x => x.GelisTuru).HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Islem>().Property(x => x.Durum).HasMaxLength(30).IsRequired();
            modelBuilder.Entity<Islem>().Property(x => x.ToplamTahakkuk).HasPrecision(18, 2);
            modelBuilder.Entity<Islem>().Property(x => x.ToplamTahsilat).HasPrecision(18, 2);
            modelBuilder.Entity<Islem>().Property(x => x.Notlar).HasMaxLength(500);
            modelBuilder.Entity<Islem>().HasRequired(x => x.Arac).WithMany(x => x.Islemler).HasForeignKey(x => x.AracId);
            modelBuilder.Entity<Islem>().HasRequired(x => x.GirisKullanici).WithMany(x => x.GirisIslemleri).HasForeignKey(x => x.GirisKullaniciId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Islem>().HasOptional(x => x.CikisKullanici).WithMany(x => x.CikisIslemleri).HasForeignKey(x => x.CikisKullaniciId).WillCascadeOnDelete(false);

            modelBuilder.Entity<Tartim>().ToTable("Tartimlar");
            modelBuilder.Entity<Tartim>().HasKey(x => x.TartimId);
            modelBuilder.Entity<Tartim>().Property(x => x.TartimTipi).HasMaxLength(30).IsRequired();
            modelBuilder.Entity<Tartim>().Property(x => x.YukDurumu).HasMaxLength(20);
            modelBuilder.Entity<Tartim>().Property(x => x.AgirlikKg).HasPrecision(18, 2);
            modelBuilder.Entity<Tartim>().HasOptional(x => x.Islem).WithMany(x => x.Tartimlar).HasForeignKey(x => x.IslemId);
            modelBuilder.Entity<Tartim>().HasRequired(x => x.Arac).WithMany(x => x.Tartimlar).HasForeignKey(x => x.AracId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Tartim>().HasRequired(x => x.Kullanici).WithMany(x => x.Tartimlar).HasForeignKey(x => x.KullaniciId).WillCascadeOnDelete(false);

            modelBuilder.Entity<IslemUcreti>().ToTable("IslemUcretleri");
            modelBuilder.Entity<IslemUcreti>().HasKey(x => x.IslemUcretId);
            modelBuilder.Entity<IslemUcreti>().Property(x => x.UcretAdi).HasMaxLength(100).IsRequired();
            modelBuilder.Entity<IslemUcreti>().Property(x => x.Tutar).HasPrecision(18, 2);
            modelBuilder.Entity<IslemUcreti>().Property(x => x.FaturaId).HasMaxLength(40);
            modelBuilder.Entity<IslemUcreti>().Property(x => x.TahsilatId).HasMaxLength(40);
            modelBuilder.Entity<IslemUcreti>().Property(x => x.TahsilatNo).HasMaxLength(20);
            modelBuilder.Entity<IslemUcreti>().HasRequired(x => x.Islem).WithMany(x => x.Ucretler).HasForeignKey(x => x.IslemId);
            modelBuilder.Entity<IslemUcreti>().HasRequired(x => x.Ucret).WithMany(x => x.IslemUcretleri).HasForeignKey(x => x.UcretId);
            modelBuilder.Entity<IslemUcreti>().HasOptional(x => x.TahsilEdenKullanici).WithMany().HasForeignKey(x => x.TahsilEdenKullaniciId).WillCascadeOnDelete(false);

            modelBuilder.Entity<LogKaydi>().ToTable("Loglar");
            modelBuilder.Entity<LogKaydi>().HasKey(x => x.LogId);
            modelBuilder.Entity<LogKaydi>().Property(x => x.LogTipi).HasMaxLength(30).IsRequired();
            modelBuilder.Entity<LogKaydi>().Property(x => x.Mesaj).HasMaxLength(250).IsRequired();
            modelBuilder.Entity<LogKaydi>().Property(x => x.BilgisayarAdi).HasMaxLength(100);
            modelBuilder.Entity<LogKaydi>().HasOptional(x => x.Kullanici).WithMany().HasForeignKey(x => x.KullaniciId).WillCascadeOnDelete(false);
            modelBuilder.Entity<LogKaydi>().HasOptional(x => x.Islem).WithMany().HasForeignKey(x => x.IslemId).WillCascadeOnDelete(false);

            modelBuilder.Entity<BekleyenTartim>().ToTable("BekleyenTartimlar");
            modelBuilder.Entity<BekleyenTartim>().HasKey(x => x.BekleyenTartimId);
            modelBuilder.Entity<BekleyenTartim>().Property(x => x.IlkAgirlikKg).HasPrecision(18, 2);
            modelBuilder.Entity<BekleyenTartim>().Property(x => x.Durum).HasMaxLength(30).IsRequired();
            modelBuilder.Entity<BekleyenTartim>().HasRequired(x => x.Arac).WithMany().HasForeignKey(x => x.AracId).WillCascadeOnDelete(false);
            modelBuilder.Entity<BekleyenTartim>().HasRequired(x => x.IlkTartim).WithMany().HasForeignKey(x => x.IlkTartimId).WillCascadeOnDelete(false);
            modelBuilder.Entity<BekleyenTartim>().HasOptional(x => x.TamamlayanTartim).WithMany().HasForeignKey(x => x.TamamlayanTartimId).WillCascadeOnDelete(false);

            modelBuilder.Entity<KantarDosyasi>().ToTable("KantarDosyalari");
            modelBuilder.Entity<KantarDosyasi>().HasKey(x => x.KantarDosyasiId);
            modelBuilder.Entity<KantarDosyasi>().Property(x => x.Durum).HasMaxLength(30).IsRequired();
            modelBuilder.Entity<KantarDosyasi>().Property(x => x.NetAgirlikKg).HasPrecision(18, 2);
            modelBuilder.Entity<KantarDosyasi>().HasRequired(x => x.Arac).WithMany(x => x.KantarDosyalari).HasForeignKey(x => x.AracId).WillCascadeOnDelete(false);
            modelBuilder.Entity<KantarDosyasi>().HasRequired(x => x.IlkTartim).WithMany().HasForeignKey(x => x.IlkTartimId).WillCascadeOnDelete(false);
            modelBuilder.Entity<KantarDosyasi>().HasOptional(x => x.KarsiTartim).WithMany().HasForeignKey(x => x.KarsiTartimId).WillCascadeOnDelete(false);

            modelBuilder.Entity<Ayar>().ToTable("Ayarlar");
            modelBuilder.Entity<Ayar>().HasKey(x => x.AyarId);
            modelBuilder.Entity<Ayar>().Property(x => x.AyarAnahtari).HasMaxLength(80).IsRequired();
            modelBuilder.Entity<Ayar>().Property(x => x.AyarDegeri).HasMaxLength(500);
            modelBuilder.Entity<Ayar>().Property(x => x.AyarGrubu).HasMaxLength(50).IsRequired();
            modelBuilder.Entity<Ayar>().Property(x => x.Aciklama).HasMaxLength(250);
        }
    }
}
