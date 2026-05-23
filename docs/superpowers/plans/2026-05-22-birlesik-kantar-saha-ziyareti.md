# Birlesik Kantar ve Saha Ziyareti Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `KantarPro` model every saha gelisi as its own visit while a separate kantar dosyasi links the first and counter weighing across one or two visits.

**Architecture:** Keep the existing `Islem` entity as the first implementation slice of the `Saha Ziyareti` concept, add a separate `KantarDosyasi` aggregate for dolu-bos completion, and let that file reference the two weighing rows attached to their saha visits. Add a new visit-oriented application service before redirecting the WPF dashboard to the new service so old code paths remain readable during the transition.

**Tech Stack:** C# 7.3, WPF, .NET Framework 4.8, EF6 code-first mappings, SQL Server Express SQL scripts, MSTest.

---

## Scope Split

This plan covers the first useful implementation slice from the approved design:

1. New domain and database model for visit type, payment id, and kantar files.
2. Visit-oriented service rules and tests for the high-risk saha scenarios.
3. Minimal dashboard binding for dolu, bos, tartimsiz, and kesin cikis tracking.

This plan deliberately leaves official invoice/e-fatura, printable receipts, legacy database import, wide reporting, and final visual polish outside the tasks.

## File Map

**Domain**

- Modify `src/KantarPro.Domain/Entities/Islem.cs`: make visit type explicit.
- Modify `src/KantarPro.Domain/Entities/IslemUcreti.cs`: add `TahsilatId` while keeping existing `FaturaId` intact for compatibility.
- Modify `src/KantarPro.Domain/Entities/Tartim.cs`: identify dolu/bos load state.
- Modify `src/KantarPro.Domain/Entities/Arac.cs`: expose kantar files for plate history.
- Create `src/KantarPro.Domain/Entities/KantarDosyasi.cs`: hold first weighing, counter weighing, net, and status.
- Modify `src/KantarPro.Domain/KantarSabitleri.cs`: add visit types, load states, and kantar-file statuses.
- Modify `src/KantarPro.Domain/KantarPro.Domain.csproj`: compile the new entity.

**Application**

- Modify `src/KantarPro.Application/Abstractions/IUnitOfWork.cs`: expose kantar files.
- Create `src/KantarPro.Application/Services/SahaZiyaretiServisi.cs`: create visits, append weighings, complete counter weighings, close visits, collect charges, and expire stale kantar files.
- Modify `src/KantarPro.Application/KantarPro.Application.csproj`: compile the new service.

**Infrastructure**

- Modify `src/KantarPro.Infrastructure/Data/KantarDbContext.cs`: map `KantarDosyalari`, visit type, weighing load state, payment id, and relationships.
- Modify `src/KantarPro.Infrastructure/Data/KantarUnitOfWork.cs`: expose EF repository for kantar files.
- Modify `database/001_create_schema.sql`: fresh installations include the new tables and columns.
- Create `database/002_add_saha_ziyareti_kantar_dosyasi.sql`: upgrade the existing `KantarPro` database without dropping current data.

**Tests**

- Modify `tests/KantarPro.Application.Tests/Fakes/InMemoryUnitOfWork.cs`: keep kantar files in memory.
- Create `tests/KantarPro.Application.Tests/SahaZiyaretiServisiTests.cs`: approved scenario coverage for the new service.
- Modify `tests/KantarPro.Application.Tests/KantarPro.Application.Tests.csproj`: compile the test file.

**Desktop**

- Modify `src/KantarPro.Desktop/MainWindow.xaml`: add visit type controls and right-side list tabs for dolu, bos, tartimsiz, and kesin cikis.
- Modify `src/KantarPro.Desktop/MainWindow.xaml.cs`: route dashboard entry/exit actions and list loading through `SahaZiyaretiServisi`.

## Task 1: Add the visit and kantar file domain shape

**Files:**

- Modify: `src/KantarPro.Domain/Entities/Arac.cs`
- Modify: `src/KantarPro.Domain/Entities/Islem.cs`
- Modify: `src/KantarPro.Domain/Entities/IslemUcreti.cs`
- Modify: `src/KantarPro.Domain/Entities/Tartim.cs`
- Create: `src/KantarPro.Domain/Entities/KantarDosyasi.cs`
- Modify: `src/KantarPro.Domain/KantarSabitleri.cs`
- Modify: `src/KantarPro.Domain/KantarPro.Domain.csproj`

- [ ] **Step 1: Add the new domain entity and constants**

Create `src/KantarPro.Domain/Entities/KantarDosyasi.cs`:

```csharp
using System;

namespace KantarPro.Domain.Entities
{
    public class KantarDosyasi
    {
        public int KantarDosyasiId { get; set; }
        public int AracId { get; set; }
        public int IlkTartimId { get; set; }
        public int? KarsiTartimId { get; set; }
        public string Durum { get; set; }
        public decimal? NetAgirlikKg { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
        public DateTime? TamamlanmaTarihi { get; set; }

        public virtual Arac Arac { get; set; }
        public virtual Tartim IlkTartim { get; set; }
        public virtual Tartim KarsiTartim { get; set; }
    }
}
```

Add these constants to `src/KantarPro.Domain/KantarSabitleri.cs`:

```csharp
public static class GelisTuru
{
    public const string Dolu = "Dolu";
    public const string Bos = "Bos";
    public const string Tartimsiz = "Tartimsiz";
}

public static class YukDurumu
{
    public const string Dolu = "Dolu";
    public const string Bos = "Bos";
}

public static class KantarDosyasiDurumu
{
    public const string KarsiTartimBekleniyor = "KarsiTartimBekleniyor";
    public const string Tamamlandi = "Tamamlandi";
    public const string SuresiDoldu = "SuresiDoldu";
}
```

- [ ] **Step 2: Extend the existing entities without renaming `Islem` yet**

Update `src/KantarPro.Domain/Entities/Islem.cs` with the visit type:

```csharp
public string GelisTuru { get; set; }
```

Update `src/KantarPro.Domain/Entities/IslemUcreti.cs` with the new payment identifier:

```csharp
public string TahsilatId { get; set; }
```

Update `src/KantarPro.Domain/Entities/Tartim.cs`:

```csharp
public string YukDurumu { get; set; }
```

Update `src/KantarPro.Domain/Entities/Arac.cs` constructor and navigation:

```csharp
KantarDosyalari = new List<KantarDosyasi>();
```

```csharp
public virtual ICollection<KantarDosyasi> KantarDosyalari { get; set; }
```

- [ ] **Step 3: Add the new file to the old-style domain project**

Add this compile item to `src/KantarPro.Domain/KantarPro.Domain.csproj` beside the other entity includes:

```xml
<Compile Include="Entities\KantarDosyasi.cs" />
```

- [ ] **Step 4: Build the domain project**

Run:

```powershell
msbuild .\src\KantarPro.Domain\KantarPro.Domain.csproj /t:Build /p:Configuration=Debug
```

Expected: build succeeds and no missing `KantarDosyasi` compile include error remains.

- [ ] **Step 5: Commit the domain shape**

```powershell
git add .\src\KantarPro.Domain
git commit -m "feat: add saha visit and kantar file domain shape"
```

## Task 2: Map the new shape in EF and SQL

**Files:**

- Modify: `src/KantarPro.Infrastructure/Data/KantarDbContext.cs`
- Modify: `src/KantarPro.Infrastructure/Data/KantarUnitOfWork.cs`
- Modify: `src/KantarPro.Application/Abstractions/IUnitOfWork.cs`
- Modify: `database/001_create_schema.sql`
- Create: `database/002_add_saha_ziyareti_kantar_dosyasi.sql`

- [ ] **Step 1: Expose kantar files through unit of work**

Add this repository to `src/KantarPro.Application/Abstractions/IUnitOfWork.cs`:

```csharp
IRepository<KantarDosyasi> KantarDosyalari { get; }
```

Add this initialization and property to `src/KantarPro.Infrastructure/Data/KantarUnitOfWork.cs`:

```csharp
KantarDosyalari = new EfRepository<KantarDosyasi>(_context);
```

```csharp
public IRepository<KantarDosyasi> KantarDosyalari { get; private set; }
```

- [ ] **Step 2: Add EF DbSet and relationship mappings**

Add to `src/KantarPro.Infrastructure/Data/KantarDbContext.cs`:

```csharp
public DbSet<KantarDosyasi> KantarDosyalari { get; set; }
```

Extend the `Islem`, `Tartim`, and `IslemUcreti` mappings:

```csharp
modelBuilder.Entity<Islem>().Property(x => x.GelisTuru).HasMaxLength(20).IsRequired();
modelBuilder.Entity<Tartim>().Property(x => x.YukDurumu).HasMaxLength(20);
modelBuilder.Entity<IslemUcreti>().Property(x => x.TahsilatId).HasMaxLength(40);
```

Map the new aggregate:

```csharp
modelBuilder.Entity<KantarDosyasi>().ToTable("KantarDosyalari");
modelBuilder.Entity<KantarDosyasi>().HasKey(x => x.KantarDosyasiId);
modelBuilder.Entity<KantarDosyasi>().Property(x => x.Durum).HasMaxLength(30).IsRequired();
modelBuilder.Entity<KantarDosyasi>().Property(x => x.NetAgirlikKg).HasPrecision(18, 2);
modelBuilder.Entity<KantarDosyasi>().HasRequired(x => x.Arac).WithMany(x => x.KantarDosyalari).HasForeignKey(x => x.AracId).WillCascadeOnDelete(false);
modelBuilder.Entity<KantarDosyasi>().HasRequired(x => x.IlkTartim).WithMany().HasForeignKey(x => x.IlkTartimId).WillCascadeOnDelete(false);
modelBuilder.Entity<KantarDosyasi>().HasOptional(x => x.KarsiTartim).WithMany().HasForeignKey(x => x.KarsiTartimId).WillCascadeOnDelete(false);
```

- [ ] **Step 3: Update the fresh-install SQL**

In `database/001_create_schema.sql`, add `dbo.KantarDosyalari` to the drop order before `dbo.Tartimlar`, add `GelisTuru` to `dbo.Islemler`, add `YukDurumu` to `dbo.Tartimlar`, add `TahsilatId` to `dbo.IslemUcretleri`, and add the table after `dbo.Tartimlar`:

```sql
CREATE TABLE dbo.KantarDosyalari
(
    KantarDosyasiId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_KantarDosyalari PRIMARY KEY,
    AracId INT NOT NULL,
    IlkTartimId INT NOT NULL,
    KarsiTartimId INT NULL,
    Durum NVARCHAR(30) NOT NULL,
    NetAgirlikKg DECIMAL(18,2) NULL,
    OlusturmaTarihi DATETIME NOT NULL,
    TamamlanmaTarihi DATETIME NULL,
    CONSTRAINT FK_KantarDosyalari_Araclar FOREIGN KEY (AracId) REFERENCES dbo.Araclar(AracId),
    CONSTRAINT FK_KantarDosyalari_IlkTartim FOREIGN KEY (IlkTartimId) REFERENCES dbo.Tartimlar(TartimId),
    CONSTRAINT FK_KantarDosyalari_KarsiTartim FOREIGN KEY (KarsiTartimId) REFERENCES dbo.Tartimlar(TartimId)
);
GO

CREATE INDEX IX_KantarDosyalari_Arac_Durum ON dbo.KantarDosyalari(AracId, Durum);
GO
```

- [ ] **Step 4: Create the upgrade SQL**

Create `database/002_add_saha_ziyareti_kantar_dosyasi.sql`:

```sql
USE KantarPro;
GO

IF COL_LENGTH(N'dbo.Islemler', N'GelisTuru') IS NULL
BEGIN
    ALTER TABLE dbo.Islemler ADD GelisTuru NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Islemler_GelisTuru DEFAULT (N'Tartimsiz');
END
GO

IF COL_LENGTH(N'dbo.Tartimlar', N'YukDurumu') IS NULL
BEGIN
    ALTER TABLE dbo.Tartimlar ADD YukDurumu NVARCHAR(20) NULL;
END
GO

IF COL_LENGTH(N'dbo.IslemUcretleri', N'TahsilatId') IS NULL
BEGIN
    ALTER TABLE dbo.IslemUcretleri ADD TahsilatId NVARCHAR(40) NULL;
END
GO

IF OBJECT_ID(N'dbo.KantarDosyalari', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KantarDosyalari
    (
        KantarDosyasiId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_KantarDosyalari PRIMARY KEY,
        AracId INT NOT NULL,
        IlkTartimId INT NOT NULL,
        KarsiTartimId INT NULL,
        Durum NVARCHAR(30) NOT NULL,
        NetAgirlikKg DECIMAL(18,2) NULL,
        OlusturmaTarihi DATETIME NOT NULL,
        TamamlanmaTarihi DATETIME NULL,
        CONSTRAINT FK_KantarDosyalari_Araclar FOREIGN KEY (AracId) REFERENCES dbo.Araclar(AracId),
        CONSTRAINT FK_KantarDosyalari_IlkTartim FOREIGN KEY (IlkTartimId) REFERENCES dbo.Tartimlar(TartimId),
        CONSTRAINT FK_KantarDosyalari_KarsiTartim FOREIGN KEY (KarsiTartimId) REFERENCES dbo.Tartimlar(TartimId)
    );
    CREATE INDEX IX_KantarDosyalari_Arac_Durum ON dbo.KantarDosyalari(AracId, Durum);
END
GO
```

- [ ] **Step 5: Build the infrastructure project**

Run:

```powershell
msbuild .\src\KantarPro.Infrastructure\KantarPro.Infrastructure.csproj /t:Build /p:Configuration=Debug
```

Expected: EF mappings compile and `IUnitOfWork` implementers report only the test fake gap that Task 3 will fill if tests are built early.

- [ ] **Step 6: Commit the data mapping**

```powershell
git add .\src\KantarPro.Application\Abstractions\IUnitOfWork.cs .\src\KantarPro.Infrastructure .\database
git commit -m "feat: map kantar files and visit fields"
```

## Task 3: Add service tests for separate visits and kantar files

**Files:**

- Modify: `tests/KantarPro.Application.Tests/Fakes/InMemoryUnitOfWork.cs`
- Create: `tests/KantarPro.Application.Tests/SahaZiyaretiServisiTests.cs`
- Modify: `tests/KantarPro.Application.Tests/KantarPro.Application.Tests.csproj`

- [ ] **Step 1: Extend the in-memory unit of work**

Add to `tests/KantarPro.Application.Tests/Fakes/InMemoryUnitOfWork.cs`:

```csharp
public readonly List<KantarDosyasi> KantarDosyasiListesi = new List<KantarDosyasi>();
```

Initialize it:

```csharp
KantarDosyalari = new InMemoryRepository<KantarDosyasi>(KantarDosyasiListesi);
```

Expose it:

```csharp
public IRepository<KantarDosyasi> KantarDosyalari { get; private set; }
```

- [ ] **Step 2: Write failing visit service tests**

Create `tests/KantarPro.Application.Tests/SahaZiyaretiServisiTests.cs`:

```csharp
using System;
using System.Linq;
using KantarPro.Application.Services;
using KantarPro.Application.Tests.Fakes;
using KantarPro.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class SahaZiyaretiServisiTests
    {
        [TestMethod]
        public void DoluIlkTartimdanSonraBosZiyaretAcarVeAyniKantarDosyasiniTamamlar()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);

            var ilkZiyaret = servis.GirisKaydet("16 BKK 747", "Firma A", KantarSabitleri.GelisTuru.Dolu, true, 28000m, 1, new DateTime(2026, 5, 12, 11, 0, 0));
            servis.CikisYap(ilkZiyaret.Arac.Plaka, false, null, 1, new DateTime(2026, 5, 12, 19, 30, 0));
            var ikinciZiyaret = servis.GirisKaydet("16 BKK 747", "Firma A", KantarSabitleri.GelisTuru.Bos, true, 12000m, 1, new DateTime(2026, 5, 14, 10, 0, 0));

            Assert.AreEqual(2, uow.IslemListesi.Count);
            Assert.AreEqual(2, uow.IslemUcretiListesi.Count(x => x.Ucret.UcretKodu == KantarSabitleri.UcretKodu.GirisCikis));
            Assert.AreEqual(2, uow.IslemUcretiListesi.Count(x => x.Ucret.UcretKodu == KantarSabitleri.UcretKodu.Tartim));
            Assert.AreEqual(KantarSabitleri.KantarDosyasiDurumu.Tamamlandi, uow.KantarDosyasiListesi.Single().Durum);
            Assert.AreEqual(16000m, uow.KantarDosyasiListesi.Single().NetAgirlikKg);
            Assert.AreNotSame(ilkZiyaret, ikinciZiyaret);
        }

        [TestMethod]
        public void TartimsizZiyaretKantarDosyasiAcmaz()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);

            var ziyaret = servis.GirisKaydet("16 TST 001", "Firma B", KantarSabitleri.GelisTuru.Tartimsiz, false, null, 1, new DateTime(2026, 5, 12, 11, 0, 0));
            servis.CikisYap(ziyaret.Arac.Plaka, false, null, 1, new DateTime(2026, 5, 12, 17, 0, 0));

            Assert.AreEqual(0, uow.KantarDosyasiListesi.Count);
            Assert.AreEqual(1, uow.IslemUcretiListesi.Count);
            Assert.IsTrue(uow.IslemUcretiListesi.Single().TahsilEdildiMi);
        }

        [TestMethod]
        public void TahsilEdilenIlkZiyaretIkinciZiyaretToplaminaEklenmez()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);

            var ilk = servis.GirisKaydet("16 TST 002", "Firma C", KantarSabitleri.GelisTuru.Bos, true, 9000m, 1, new DateTime(2026, 5, 12, 8, 0, 0));
            servis.CikisYap(ilk.Arac.Plaka, false, null, 1, new DateTime(2026, 5, 12, 12, 0, 0));
            var ikinci = servis.GirisKaydet("16 TST 002", "Firma C", KantarSabitleri.GelisTuru.Dolu, true, 24000m, 1, new DateTime(2026, 5, 13, 9, 0, 0));

            Assert.AreEqual(732m, ilk.ToplamTahsilat);
            Assert.AreEqual(732m, ikinci.ToplamTahakkuk);
            Assert.AreEqual(0m, ikinci.ToplamTahsilat);
        }
    }
}
```

- [ ] **Step 3: Add the test file to the old-style test project**

Add to `tests/KantarPro.Application.Tests/KantarPro.Application.Tests.csproj`:

```xml
<Compile Include="SahaZiyaretiServisiTests.cs" />
```

- [ ] **Step 4: Run tests and confirm they fail for the missing service**

Run:

```powershell
msbuild .\tests\KantarPro.Application.Tests\KantarPro.Application.Tests.csproj /t:Build /p:Configuration=Debug
```

Expected: FAIL because `SahaZiyaretiServisi` does not exist yet.

- [ ] **Step 5: Commit the failing tests**

```powershell
git add .\tests\KantarPro.Application.Tests
git commit -m "test: cover saha visits and kantar files"
```

## Task 4: Implement visit-oriented service rules

**Files:**

- Create: `src/KantarPro.Application/Services/SahaZiyaretiServisi.cs`
- Modify: `src/KantarPro.Application/KantarPro.Application.csproj`
- Test: `tests/KantarPro.Application.Tests/SahaZiyaretiServisiTests.cs`

- [ ] **Step 1: Create the service skeleton and add it to the application project**

Create `src/KantarPro.Application/Services/SahaZiyaretiServisi.cs`:

```csharp
using System;
using System.Linq;
using KantarPro.Application.Abstractions;
using KantarPro.Domain;
using KantarPro.Domain.Entities;

namespace KantarPro.Application.Services
{
    public class SahaZiyaretiServisi
    {
        private readonly IUnitOfWork _unitOfWork;

        public SahaZiyaretiServisi(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }
    }
}
```

Add to `src/KantarPro.Application/KantarPro.Application.csproj`:

```xml
<Compile Include="Services\SahaZiyaretiServisi.cs" />
```

- [ ] **Step 2: Implement visit entry and first weighing**

Add `GirisKaydet` and the first weighing helpers:

```csharp
public string GelisTuruOner(string plaka)
{
    var temizPlaka = NormalizePlaka(plaka);
    var bekleyen = _unitOfWork.KantarDosyalari.Query()
        .Where(x => x.Arac.Plaka == temizPlaka && x.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor)
        .OrderByDescending(x => x.OlusturmaTarihi)
        .FirstOrDefault();

    if (bekleyen == null)
    {
        return KantarSabitleri.GelisTuru.Dolu;
    }

    return bekleyen.IlkTartim.YukDurumu == KantarSabitleri.YukDurumu.Dolu
        ? KantarSabitleri.GelisTuru.Bos
        : KantarSabitleri.GelisTuru.Dolu;
}

public Islem GirisKaydet(string plaka, string firmaAdi, string gelisTuru, bool tartimYap, decimal? agirlikKg, int kullaniciId, DateTime tarih)
{
    var arac = AracBulVeyaOlustur(plaka, firmaAdi, tarih);
    if (_unitOfWork.Islemler.Query().Any(x => x.Arac.Plaka == arac.Plaka && x.Durum == KantarSabitleri.IslemDurumu.Iceride))
    {
        throw new InvalidOperationException("Bu plaka icin sahada acik ziyaret var.");
    }

    var ziyaret = new Islem
    {
        Arac = arac,
        IslemNo = "ZYR" + tarih.ToString("yyyyMMddHHmmssfff"),
        GelisTuru = gelisTuru,
        GirisTarihi = tarih,
        Durum = KantarSabitleri.IslemDurumu.Iceride,
        GirisKullaniciId = kullaniciId
    };

    _unitOfWork.Islemler.Add(ziyaret);
    UcretEkle(ziyaret, KantarSabitleri.UcretKodu.GirisCikis, tarih);

    if (tartimYap)
    {
        TartimKaydet(ziyaret, YukDurumuGetir(gelisTuru), agirlikKg, kullaniciId, tarih);
    }

    _unitOfWork.SaveChanges();
    return ziyaret;
}
```

```csharp
private void TartimKaydet(Islem ziyaret, string yukDurumu, decimal? agirlikKg, int kullaniciId, DateTime tarih)
{
    if (!agirlikKg.HasValue || agirlikKg.Value <= 0)
    {
        throw new ArgumentException("Tartim icin gecerli agirlik zorunludur.", nameof(agirlikKg));
    }

    var tartim = new Tartim
    {
        Islem = ziyaret,
        Arac = ziyaret.Arac,
        YukDurumu = yukDurumu,
        TartimTipi = KantarSabitleri.TartimTipi.Giris,
        AgirlikKg = agirlikKg.Value,
        TartimTarihi = tarih,
        ComPorttanAlindiMi = true,
        KullaniciId = kullaniciId
    };

    _unitOfWork.Tartimlar.Add(tartim);
    ziyaret.Tartimlar.Add(tartim);
    UcretEkle(ziyaret, KantarSabitleri.UcretKodu.Tartim, tarih);
    KantarDosyasinaBagla(ziyaret.Arac, tartim, tarih);
}
```

- [ ] **Step 3: Implement kantar file matching and completion**

Add:

```csharp
private void KantarDosyasinaBagla(Arac arac, Tartim tartim, DateTime tarih)
{
    var bekleyen = _unitOfWork.KantarDosyalari.Query()
        .Where(x => x.Arac.Plaka == arac.Plaka && x.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor)
        .OrderByDescending(x => x.OlusturmaTarihi)
        .FirstOrDefault();

    if (bekleyen == null)
    {
        var dosya = new KantarDosyasi
        {
            Arac = arac,
            IlkTartim = tartim,
            Durum = KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor,
            OlusturmaTarihi = tarih
        };
        _unitOfWork.KantarDosyalari.Add(dosya);
        return;
    }

    if (bekleyen.IlkTartim.YukDurumu == tartim.YukDurumu)
    {
        throw new InvalidOperationException("Karsi tartim dolu-bos yonuyle uyusmuyor.");
    }

    bekleyen.KarsiTartim = tartim;
    bekleyen.KarsiTartimId = tartim.TartimId;
    bekleyen.NetAgirlikKg = Math.Abs(bekleyen.IlkTartim.AgirlikKg - tartim.AgirlikKg);
    bekleyen.Durum = KantarSabitleri.KantarDosyasiDurumu.Tamamlandi;
    bekleyen.TamamlanmaTarihi = tarih;
}
```

- [ ] **Step 4: Implement visit exit, wait charge, and tahsilat id**

Add:

```csharp
public Islem CikisYap(string plaka, bool cikistaTart, decimal? agirlikKg, int kullaniciId, DateTime cikisTarihi)
{
    var ziyaret = AcikZiyaretBul(plaka);
    if (cikistaTart)
    {
        TartimKaydet(ziyaret, KarsiYukDurumuGetir(ziyaret), agirlikKg, kullaniciId, cikisTarihi);
    }

    ziyaret.CikisTarihi = cikisTarihi;
    ziyaret.CikisKullaniciId = kullaniciId;
    ziyaret.Durum = KantarSabitleri.IslemDurumu.CikisYapti;

    var beklemeGun = HesaplaBeklemeGunSayisi(ziyaret.GirisTarihi, cikisTarihi);
    for (var i = 0; i < beklemeGun; i++)
    {
        UcretEkle(ziyaret, KantarSabitleri.UcretKodu.Bekleme, cikisTarihi);
    }

    TahsilEt(ziyaret, kullaniciId, cikisTarihi);
    _unitOfWork.SaveChanges();
    return ziyaret;
}
```

Add a visit-scoped late weighing method for the right-click `Tartim Ekle`
flow and for tartimsiz visits whose driver changes their mind:

```csharp
public Islem SonradanTartimEkle(string plaka, string yukDurumu, decimal agirlikKg, int kullaniciId, DateTime tartimTarihi)
{
    var ziyaret = AcikZiyaretBul(plaka);
    var tamamlanmisDosyaVarMi = _unitOfWork.KantarDosyalari.Query()
        .Any(x => x.Durum == KantarSabitleri.KantarDosyasiDurumu.Tamamlandi &&
            (x.IlkTartim.Islem == ziyaret || x.KarsiTartim.Islem == ziyaret));

    if (tamamlanmisDosyaVarMi)
    {
        throw new InvalidOperationException("Bu ziyaretin dolu-bos tartimi tamamlandi. Yeni tartim eklenemez.");
    }

    TartimKaydet(ziyaret, yukDurumu, agirlikKg, kullaniciId, tartimTarihi);
    _unitOfWork.SaveChanges();
    return ziyaret;
}
```

Add the approved expiry rule:

```csharp
public int SuresiDolanKantarDosyalariniKapat(DateTime kontrolTarihi, int gunSiniri)
{
    var sinir = kontrolTarihi.Date.AddDays(-gunSiniri);
    var kapanacaklar = _unitOfWork.KantarDosyalari.Query()
        .Where(x => x.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor && x.OlusturmaTarihi <= sinir)
        .ToList();

    foreach (var dosya in kapanacaklar)
    {
        dosya.Durum = KantarSabitleri.KantarDosyasiDurumu.SuresiDoldu;
    }

    if (kapanacaklar.Count > 0)
    {
        _unitOfWork.SaveChanges();
    }

    return kapanacaklar.Count;
}
```

```csharp
private static void TahsilEt(Islem ziyaret, int kullaniciId, DateTime tarih)
{
    var tahsilatId = "THS" + tarih.ToString("yyyyMMddHHmmssfff") + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
    var odenecekler = ziyaret.Ucretler.Where(x => !x.TahsilEdildiMi).ToList();
    foreach (var ucret in odenecekler)
    {
        ucret.TahsilEdildiMi = true;
        ucret.TahsilTarihi = tarih;
        ucret.TahsilEdenKullaniciId = kullaniciId;
        ucret.TahsilatId = tahsilatId;
    }

    ziyaret.ToplamTahsilat += odenecekler.Sum(x => x.Tutar);
}
```

Copy `UcretEkle`, plate normalization, vehicle creation, open visit lookup, and wait-day calculation from `IslemServisi` into focused private helpers so the service is self-contained in this slice.

- [ ] **Step 5: Add the remaining rule tests before refining helpers**

Append these tests to `SahaZiyaretiServisiTests.cs`:

```csharp
[TestMethod]
public void AyniKantarDosyasinaUcuncuTartimEklenmez()
{
    var uow = new InMemoryUnitOfWork();
    var servis = new SahaZiyaretiServisi(uow);
    var ziyaret = servis.GirisKaydet("16 TST 003", "Firma D", KantarSabitleri.GelisTuru.Dolu, true, 26000m, 1, new DateTime(2026, 5, 12, 10, 0, 0));
    servis.SonradanTartimEkle(ziyaret.Arac.Plaka, KantarSabitleri.YukDurumu.Bos, 11000m, 1, new DateTime(2026, 5, 12, 11, 0, 0));

    Assert.ThrowsException<InvalidOperationException>(() =>
        servis.SonradanTartimEkle(ziyaret.Arac.Plaka, KantarSabitleri.YukDurumu.Bos, 10900m, 1, new DateTime(2026, 5, 12, 11, 30, 0)));
}

[TestMethod]
public void TartimsizZiyareteSonradanIlkTartimEklenir()
{
    var uow = new InMemoryUnitOfWork();
    var servis = new SahaZiyaretiServisi(uow);
    var ziyaret = servis.GirisKaydet("16 TST 005", "Firma F", KantarSabitleri.GelisTuru.Tartimsiz, false, null, 1, new DateTime(2026, 5, 12, 10, 0, 0));

    servis.SonradanTartimEkle(ziyaret.Arac.Plaka, KantarSabitleri.YukDurumu.Dolu, 21500m, 1, new DateTime(2026, 5, 12, 10, 20, 0));

    Assert.AreEqual(1, ziyaret.Tartimlar.Count);
    Assert.AreEqual(1, uow.KantarDosyasiListesi.Count);
    Assert.AreEqual(732m, ziyaret.ToplamTahakkuk);
}

[TestMethod]
public void GeceYarisiGecilirseBeklemeUcretiTahsilEdilir()
{
    var uow = new InMemoryUnitOfWork();
    var servis = new SahaZiyaretiServisi(uow);
    var ziyaret = servis.GirisKaydet("16 TST 004", "Firma E", KantarSabitleri.GelisTuru.Tartimsiz, false, null, 1, new DateTime(2026, 5, 12, 23, 40, 0));

    servis.CikisYap(ziyaret.Arac.Plaka, false, null, 1, new DateTime(2026, 5, 13, 0, 5, 0));

    Assert.AreEqual(1, ziyaret.Ucretler.Count(x => x.Ucret.UcretKodu == KantarSabitleri.UcretKodu.Bekleme));
    Assert.IsTrue(ziyaret.Ucretler.All(x => !string.IsNullOrWhiteSpace(x.TahsilatId)));
}

[TestMethod]
public void BekleyenDoluDosyasiBosGelisOnerir()
{
    var uow = new InMemoryUnitOfWork();
    var servis = new SahaZiyaretiServisi(uow);
    servis.GirisKaydet("16 TST 006", "Firma G", KantarSabitleri.GelisTuru.Dolu, true, 20000m, 1, new DateTime(2026, 5, 12, 10, 0, 0));

    Assert.AreEqual(KantarSabitleri.GelisTuru.Bos, servis.GelisTuruOner("16 TST 006"));
}

[TestMethod]
public void OnGunuGecenKantarDosyasiSuresiDolduOlur()
{
    var uow = new InMemoryUnitOfWork();
    var servis = new SahaZiyaretiServisi(uow);
    servis.GirisKaydet("16 TST 007", "Firma H", KantarSabitleri.GelisTuru.Bos, true, 9000m, 1, new DateTime(2026, 5, 1, 10, 0, 0));

    var kapanan = servis.SuresiDolanKantarDosyalariniKapat(new DateTime(2026, 5, 12, 9, 0, 0), 10);

    Assert.AreEqual(1, kapanan);
    Assert.AreEqual(KantarSabitleri.KantarDosyasiDurumu.SuresiDoldu, uow.KantarDosyasiListesi.Single().Durum);
}
```

- [ ] **Step 6: Run service tests**

Run:

```powershell
msbuild .\KantarPro.sln /t:Build /p:Configuration=Debug
vstest.console.exe .\tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll
```

Expected: the new `SahaZiyaretiServisiTests` pass. If old `IslemServisiTests` fail because `GelisTuru` became required, update old service visit creation to use `KantarSabitleri.GelisTuru.Tartimsiz` until desktop routing is switched.

- [ ] **Step 7: Commit the service**

```powershell
git add .\src\KantarPro.Application .\tests\KantarPro.Application.Tests
git commit -m "feat: add saha visit service rules"
```

## Task 5: Move the dashboard lists to visit-oriented reads

**Files:**

- Modify: `src/KantarPro.Desktop/MainWindow.xaml`
- Modify: `src/KantarPro.Desktop/MainWindow.xaml.cs`

- [ ] **Step 1: Add visit type choice to the left operation panel**

Add the `GelisTuruComboBox` to the existing left panel near plaka/firma inputs in `MainWindow.xaml`:

```xml
<TextBlock Grid.Row="2" Text="Gelis Turu:" VerticalAlignment="Center" Margin="0,0,6,8" />
<ComboBox x:Name="GelisTuruComboBox"
          Grid.Row="2"
          Grid.Column="1"
          Height="26"
          SelectedIndex="0"
          Margin="0,0,0,8">
    <ComboBoxItem Content="Dolu" />
    <ComboBoxItem Content="Bos" />
    <ComboBoxItem Content="Tartimsiz" />
</ComboBox>
```

Move the existing firma and aciklama rows down one row in the same grid so field overlap does not occur.

- [ ] **Step 2: Replace the one active grid title with active visit tabs**

Wrap the active tracking area in a `TabControl`:

```xml
<TabControl Grid.Row="1">
    <TabItem Header="Dolu Gelenler">
        <DataGrid x:Name="DoluVisitsGrid" ItemsSource="{Binding DoluVisits}" />
    </TabItem>
    <TabItem Header="Bos Gelenler">
        <DataGrid x:Name="BosVisitsGrid" ItemsSource="{Binding BosVisits}" />
    </TabItem>
    <TabItem Header="Tartimsiz Girisler">
        <DataGrid x:Name="TartimsizVisitsGrid" ItemsSource="{Binding TartimsizVisits}" />
    </TabItem>
</TabControl>
```

Use the existing compact dashboard columns for the first pass:

```xml
<DataGridTextColumn Header="Durum" Binding="{Binding Durum}" Width="120" />
<DataGridTextColumn Header="Plaka" Binding="{Binding Plaka}" Width="90" />
<DataGridTextColumn Header="Firma" Binding="{Binding FirmaAdi}" Width="130" />
<DataGridTextColumn Header="Giris Tarihi" Binding="{Binding GirisTarihi}" Width="95" />
<DataGridTextColumn Header="Giris Saati" Binding="{Binding GirisSaati}" Width="85" />
<DataGridTextColumn Header="Tartim Durumu" Binding="{Binding TartimDurumu}" Width="145" />
<DataGridTextColumn Header="Toplam" Binding="{Binding Ucret}" Width="95" />
```

- [ ] **Step 3: Add collections for the three active lists**

In `MainWindow.xaml.cs`, add:

```csharp
public ObservableCollection<VehicleMovementRow> DoluVisits { get; private set; }
public ObservableCollection<VehicleMovementRow> BosVisits { get; private set; }
public ObservableCollection<VehicleMovementRow> TartimsizVisits { get; private set; }
```

Initialize them in the constructor:

```csharp
DoluVisits = new ObservableCollection<VehicleMovementRow>();
BosVisits = new ObservableCollection<VehicleMovementRow>();
TartimsizVisits = new ObservableCollection<VehicleMovementRow>();
```

Add `TartimDurumu` to the local `VehicleMovementRow` type:

```csharp
public string TartimDurumu { get; set; }
```

- [ ] **Step 4: Route dashboard entry and exit through `SahaZiyaretiServisi`**

Use the plate field to fill the suggested visit type, while keeping the combo box editable by the operator:

```xml
<TextBox x:Name="PlakaTextBox"
         CharacterCasing="Upper"
         LostFocus="PlakaTextBox_LostFocus" />
```

```csharp
private void PlakaTextBox_LostFocus(object sender, RoutedEventArgs e)
{
    if (string.IsNullOrWhiteSpace(PlakaTextBox.Text))
    {
        return;
    }

    using (var context = new KantarDbContext())
    {
        var servis = new SahaZiyaretiServisi(new KantarUnitOfWork(context));
        SelectGelisTuru(servis.GelisTuruOner(PlakaTextBox.Text));
    }
}
```

```csharp
private void SelectGelisTuru(string gelisTuru)
{
    foreach (ComboBoxItem item in GelisTuruComboBox.Items)
    {
        if (string.Equals(item.Content as string, gelisTuru, StringComparison.Ordinal))
        {
            GelisTuruComboBox.SelectedItem = item;
            return;
        }
    }
}
```

Replace dashboard `CreateEntry` calls with:

```csharp
var gelisTuru = ((ComboBoxItem)GelisTuruComboBox.SelectedItem).Content.ToString();
var servis = new SahaZiyaretiServisi(new KantarUnitOfWork(context));
servis.GirisKaydet(plaka, firmaAdi, gelisTuru, tartimIsteniyor, agirlik, kullaniciId, islemTarihi);
```

Replace dashboard exit confirmation completion path with:

```csharp
var servis = new SahaZiyaretiServisi(new KantarUnitOfWork(context));
servis.CikisYap(plaka, tartimIsteniyor, agirlik, kullaniciId, cikisTarihi);
```

- [ ] **Step 5: Split `LoadDashboardData` by visit type**

After fetching `Islemler`, populate the three active collections:

```csharp
DoluVisits.Clear();
BosVisits.Clear();
TartimsizVisits.Clear();

foreach (var ziyaret in girisler)
{
    var row = MapVisitRow(ziyaret, context, listeHesapTarihi);
    if (ziyaret.GelisTuru == KantarSabitleri.GelisTuru.Dolu)
    {
        DoluVisits.Add(row);
    }
    else if (ziyaret.GelisTuru == KantarSabitleri.GelisTuru.Bos)
    {
        BosVisits.Add(row);
    }
    else
    {
        TartimsizVisits.Add(row);
    }
}
```

Append first-visit rows whose saha visit closed but whose kantar file still
waits for the counter weighing:

```csharp
var karsiTartimBekleyenler = context.KantarDosyalari
    .Include(x => x.Arac)
    .Include(x => x.IlkTartim.Islem.Ucretler.Select(u => u.Ucret))
    .Where(x => x.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor)
    .OrderByDescending(x => x.OlusturmaTarihi)
    .Take(100)
    .ToList();

foreach (var dosya in karsiTartimBekleyenler)
{
    var row = MapPendingKantarRow(dosya);
    row.Durum = "Karsi tartim bekleniyor";
    if (dosya.IlkTartim.YukDurumu == KantarSabitleri.YukDurumu.Dolu)
    {
        DoluVisits.Add(row);
    }
    else
    {
        BosVisits.Add(row);
    }
}
```

`MapPendingKantarRow` should use the first weighing and first visit timestamps,
show the first visit's already-collected payment history in detail, and show
`0` as the currently unpaid visit total after that visit was closed.

Use a row helper that reads a linked kantar file when available:

```csharp
private static string GetTartimDurumu(KantarDbContext context, Islem ziyaret)
{
    var dosya = context.KantarDosyalari
        .FirstOrDefault(x => x.IlkTartim.IslemId == ziyaret.IslemId ||
            (x.KarsiTartimId.HasValue && x.KarsiTartim.IslemId == ziyaret.IslemId));
    return dosya == null ? "Tartim yok" : dosya.Durum;
}
```

- [ ] **Step 6: Keep `Kesin Cikislar` visit-based**

Load the exit list from closed visits, not completed kantar files:

```csharp
var cikislar = context.Islemler
    .Include(x => x.Arac)
    .Include(x => x.Tartimlar)
    .Include(x => x.Ucretler.Select(u => u.Ucret))
    .Where(x => x.Durum == KantarSabitleri.IslemDurumu.CikisYapti)
    .OrderByDescending(x => x.CikisTarihi)
    .Take(100)
    .ToList();
```

Show `KarsiTartimBekleniyor` in the row status when the visit's weighing belongs to an open kantar file.

- [ ] **Step 7: Build the desktop app**

Run:

```powershell
msbuild .\src\KantarPro.Desktop\KantarPro.Desktop.csproj /t:Build /p:Configuration=Debug
```

Expected: the dashboard compiles with the new visit-type controls and the new service references.

- [ ] **Step 8: Commit the minimal UI switch**

```powershell
git add .\src\KantarPro.Desktop
git commit -m "feat: show saha visit operation lists"
```

## Task 6: Verify the approved field scenarios end to end

**Files:**

- Verify: `tests/KantarPro.Application.Tests/SahaZiyaretiServisiTests.cs`
- Verify: `src/KantarPro.Desktop/MainWindow.xaml`
- Verify: `src/KantarPro.Desktop/MainWindow.xaml.cs`
- Verify: `database/002_add_saha_ziyareti_kantar_dosyasi.sql`

- [ ] **Step 1: Run full build and application tests**

Run:

```powershell
msbuild .\KantarPro.sln /t:Build /p:Configuration=Debug
vstest.console.exe .\tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll
```

Expected: solution build succeeds and all application tests pass.

- [ ] **Step 2: Apply the upgrade SQL to the local test database**

Run in SQL Server tooling against the local `KantarPro` database:

```sql
:r C:\Users\DELL\OneDrive\Masaüstü\KantarPro_Tasima_Paketi\KantarPro_Tasima_Paketi\project\Codex Kantar\database\002_add_saha_ziyareti_kantar_dosyasi.sql
```

Expected: `KantarDosyalari` exists, old `Islemler` rows keep `GelisTuru = Tartimsiz`, and no existing data is dropped.

- [ ] **Step 3: Manually verify the four dashboard flows**

Use the desktop app with test plates:

1. Enter a dolu visit with `Tart ve Kaydet`, close it without counter weighing, and confirm the row remains trackable as counter weighing pending while its visit appears in `Kesin Cikislar`.
2. Re-enter the same plate as bos with `Tart ve Kaydet`, confirm the same kantar file completes and only the second visit charges are currently unpaid before exit.
3. Enter a tartimsiz visit, close it, and confirm no kantar file opens.
4. Enter a bos visit first and a dolu visit second, confirm net uses absolute difference.

- [ ] **Step 4: Record any UI-only cleanup as follow-up tasks**

If the logic verifies but the screen still needs spacing, column, or search refinements, capture them as separate follow-ups instead of expanding this plan into visual polish.

- [ ] **Step 5: Commit verification fixes if needed**

```powershell
git add .
git commit -m "test: verify saha visit kantar flows"
```

## Final Acceptance Checklist

- Every saha entry opens a distinct `Islem` visit with a `GelisTuru`.
- A tartimsiz visit never creates `KantarDosyasi`.
- A first dolu or bos weighing creates one open `KantarDosyasi`.
- A later counter weighing on a new visit completes the same `KantarDosyasi`.
- A single visit can complete two weighings.
- Net is absolute difference between linked weighings.
- Entry/exit, tartim, and bekleme charges stay on the visit that caused them.
- Closing a visit assigns `TahsilatId` to unpaid charge rows collected together.
- Closed visits feed `Kesin Cikislar` even if their linked kantar file still waits for the counter weighing.
- The active dolu/bos tracking surface can still show open kantar work after the first visit exits.
