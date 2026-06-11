# Tartimsiz Ikinci Ziyarette Ilk Tartim Fisi Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tartimsiz ikinci ziyarette bulunan bir arac icin ust listedeki satirdan, ilk ziyaretteki tartimin kantar fisini ayni fis numarasiyla yeniden gostermek ve yazdirmak.

**Architecture:** Fis kaynagi secimi yeni ve saf bir `KantarFisKaynakResolver` sinifinda toplanacak. `MainWindow` yalnizca secili islemin aracina ait bekleyen kantar dosyalarini veritabanindan yukleyip bu sinifa verecek; onizleme ve yazdirma ayni cozulmus `VehicleMovementRow` nesnesini kullanacak.

**Tech Stack:** .NET Framework 4.8, C# 7.3, WPF, Entity Framework 6, MSTest, SQL Server.

---

## File Map

- Create: `src/KantarPro.Desktop/KantarFisKaynakResolver.cs`
  - Secili satirin kendi tartimini veya bekleyen ilk tartimini tek ve test edilebilir bir kuralla secer.
- Modify: `src/KantarPro.Desktop/KantarPro.Desktop.csproj`
  - Yeni resolver dosyasini eski tip proje dosyasinin derleme listesine ekler.
- Create: `tests/KantarPro.Application.Tests/KantarFisKaynakResolverTests.cs`
  - Kendi tartimi, bekleyen ilk tartim, kayit yoklugu ve belirsiz kayit senaryolarini dogrular.
- Modify: `tests/KantarPro.Application.Tests/KantarPro.Application.Tests.csproj`
  - Yeni test dosyasini derleme listesine ekler.
- Modify: `src/KantarPro.Desktop/MainWindow.xaml.cs:740-790`
  - Makbuz yazdirma akisini cozulmus fis satirina yonlendirir.
- Modify: `src/KantarPro.Desktop/MainWindow.xaml.cs:934-960`
  - Makbuz onizleme akisini ayni cozulmus fis satirina yonlendirir.

### Task 1: Resolver Davranisini Testlerle Sabitle

**Files:**
- Create: `tests/KantarPro.Application.Tests/KantarFisKaynakResolverTests.cs`
- Modify: `tests/KantarPro.Application.Tests/KantarPro.Application.Tests.csproj`

- [ ] **Step 1: Yeni test dosyasini proje dosyasina ekle**

`KantarPro.Application.Tests.csproj` dosyasindaki test `Compile` kayitlarina sunu ekle:

```xml
<Compile Include="KantarFisKaynakResolverTests.cs" />
```

- [ ] **Step 2: Kendi tartimi bulunan satirin degismedigini test et**

```csharp
using System;
using System.Collections.Generic;
using KantarPro.Desktop;
using KantarPro.Domain;
using KantarPro.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class KantarFisKaynakResolverTests
    {
        [TestMethod]
        public void Resolve_SeciliIsleminTartimiVarsaAyniSatiriDondurur()
        {
            var selected = new VehicleMovementRow
            {
                IslemId = 2,
                Plaka = "16BKK767",
                Tartim = "8.900 kg"
            };

            var result = KantarFisKaynakResolver.Resolve(
                selected,
                new List<KantarDosyasi>());

            Assert.AreSame(selected, result);
        }
    }
}
```

- [ ] **Step 3: Tartimsiz ikinci ziyarette ilk tartimin kullanilmasini test et**

Ayni test sinifina su testi ekle:

```csharp
[TestMethod]
public void Resolve_TartimsizIkinciZiyaretteBekleyenIlkTartimSatiriniDondurur()
{
    var firstVisit = new Islem
    {
        IslemId = 1,
        IslemNo = "00001",
        GirisTarihi = new DateTime(2026, 6, 11, 14, 7, 30)
    };
    var vehicle = new Arac
    {
        AracId = 1,
        Plaka = "16BKK767",
        FirmaAdi = "ORNEK FIRMA"
    };
    var firstWeighing = new Tartim
    {
        TartimId = 1,
        IslemId = 1,
        Islem = firstVisit,
        AracId = 1,
        AgirlikKg = 34000m,
        TartimTarihi = new DateTime(2026, 6, 11, 14, 8, 0),
        KantarFisNo = "00001"
    };
    var file = new KantarDosyasi
    {
        KantarDosyasiId = 1,
        AracId = 1,
        Arac = vehicle,
        IlkTartimId = 1,
        IlkTartim = firstWeighing,
        Durum = KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor
    };
    var selected = new VehicleMovementRow
    {
        IslemId = 2,
        Plaka = "16BKK767",
        Tartim = "Tartım Yok"
    };

    var result = KantarFisKaynakResolver.Resolve(
        selected,
        new List<KantarDosyasi> { file });

    Assert.AreEqual(1, result.IslemId);
    Assert.AreEqual("00001", result.IslemNo);
    Assert.AreEqual("00001", result.KantarFisNo);
    Assert.AreEqual("16BKK767", result.Plaka);
    Assert.AreEqual("ORNEK FIRMA", result.FirmaAdi);
    Assert.AreEqual("11.06.2026", result.GirisTarihi);
    Assert.AreEqual("14:07:30", result.GirisSaati);
    Assert.AreEqual("34.000 kg", result.Tartim);
}
```

- [ ] **Step 4: Kayit bulunamamasi ve belirsizlik testlerini ekle**

```csharp
[TestMethod]
public void Resolve_TartimsizKayitIcinBekleyenTartimYoksaHataVerir()
{
    var selected = new VehicleMovementRow
    {
        IslemId = 2,
        Plaka = "16BKK767",
        Tartim = "Tartım Yok"
    };

    var error = Assert.ThrowsException<InvalidOperationException>(() =>
        KantarFisKaynakResolver.Resolve(selected, new List<KantarDosyasi>()));

    StringAssert.Contains(error.Message, "Tartımsız girişler");
}

[TestMethod]
public void Resolve_BirdenFazlaBekleyenDosyaVarsaTahminYurutmez()
{
    var selected = new VehicleMovementRow
    {
        IslemId = 3,
        Plaka = "16BKK767",
        Tartim = "Tartım Yok"
    };
    var candidates = new List<KantarDosyasi>
    {
        CreatePendingFile(1, "16BKK767", 34000m),
        CreatePendingFile(2, "16BKK767", 33500m)
    };

    var error = Assert.ThrowsException<InvalidOperationException>(() =>
        KantarFisKaynakResolver.Resolve(selected, candidates));

    StringAssert.Contains(error.Message, "birden fazla");
}

private static KantarDosyasi CreatePendingFile(int id, string plate, decimal weight)
{
    var visit = new Islem
    {
        IslemId = id,
        IslemNo = id.ToString("00000"),
        GirisTarihi = new DateTime(2026, 6, 11, 10, 0, 0)
    };
    return new KantarDosyasi
    {
        KantarDosyasiId = id,
        AracId = id,
        Arac = new Arac { AracId = id, Plaka = plate, FirmaAdi = "FIRMA" },
        IlkTartimId = id,
        IlkTartim = new Tartim
        {
            TartimId = id,
            IslemId = id,
            Islem = visit,
            AracId = id,
            AgirlikKg = weight,
            TartimTarihi = visit.GirisTarihi,
            KantarFisNo = id.ToString("00000")
        },
        Durum = KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor
    };
}
```

- [ ] **Step 5: Testleri derle ve beklenen kirmizi sonucu dogrula**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Debug /v:minimal
```

Expected: `KantarFisKaynakResolver` henuz bulunmadigi icin derleme basarisiz olur.

- [ ] **Step 6: Test iskeletini commit et**

```powershell
git add tests/KantarPro.Application.Tests/KantarFisKaynakResolverTests.cs tests/KantarPro.Application.Tests/KantarPro.Application.Tests.csproj
git commit -m "test: ilk tartim fisi kaynak secimini tanimla"
```

### Task 2: Saf Fis Kaynagi Resolverini Uygula

**Files:**
- Create: `src/KantarPro.Desktop/KantarFisKaynakResolver.cs`
- Modify: `src/KantarPro.Desktop/KantarPro.Desktop.csproj`
- Test: `tests/KantarPro.Application.Tests/KantarFisKaynakResolverTests.cs`

- [ ] **Step 1: Resolver dosyasini Desktop projesine ekle**

`KantarPro.Desktop.csproj` dosyasinda `KantarFisFormatter.cs` kaydinin yanina sunu ekle:

```xml
<Compile Include="KantarFisKaynakResolver.cs" />
```

- [ ] **Step 2: Minimal resolver uygulamasini yaz**

`KantarFisKaynakResolver.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KantarPro.Domain;
using KantarPro.Domain.Entities;

namespace KantarPro.Desktop
{
    public static class KantarFisKaynakResolver
    {
        public static VehicleMovementRow Resolve(
            VehicleMovementRow selectedRow,
            IEnumerable<KantarDosyasi> pendingFiles)
        {
            if (selectedRow == null)
            {
                throw new ArgumentNullException(nameof(selectedRow));
            }

            if (HasWeighing(selectedRow.Tartim))
            {
                return selectedRow;
            }

            var normalizedPlate = NormalizePlate(selectedRow.Plaka);
            var matches = (pendingFiles ?? Enumerable.Empty<KantarDosyasi>())
                .Where(x =>
                    x != null &&
                    x.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor &&
                    x.Arac != null &&
                    NormalizePlate(x.Arac.Plaka) == normalizedPlate &&
                    x.IlkTartim != null &&
                    x.IlkTartim.Islem != null &&
                    !x.IlkTartim.Islem.SilindiMi)
                .ToList();

            if (matches.Count == 0)
            {
                throw new InvalidOperationException(
                    "Bu kayıtta kantar tartımı yok. Tartımsız girişler için kantar fişi oluşturulmaz.");
            }

            if (matches.Count > 1)
            {
                throw new InvalidOperationException(
                    "Bu plaka için birden fazla bekleyen ilk tartım bulundu. Yanlış fiş basılmaması için kayıtları kontrol edin.");
            }

            return BuildFirstWeighingRow(matches[0]);
        }

        private static VehicleMovementRow BuildFirstWeighingRow(KantarDosyasi file)
        {
            var weighing = file.IlkTartim;
            var visit = weighing.Islem;
            return new VehicleMovementRow
            {
                IslemId = visit.IslemId,
                IslemNo = FirstNonEmpty(visit.IslemNo, visit.CikisNo),
                KantarFisNo = weighing.KantarFisNo,
                Plaka = file.Arac.Plaka,
                FirmaAdi = file.Arac.FirmaAdi,
                GirisTarihi = visit.GirisTarihi.ToString("dd.MM.yyyy"),
                GirisSaati = visit.GirisTarihi.ToString("HH:mm:ss"),
                IlkTartimTarihi = weighing.TartimTarihi.ToString("dd.MM.yyyy"),
                IlkTartimSaati = weighing.TartimTarihi.ToString("HH:mm:ss"),
                Tartim = weighing.AgirlikKg.ToString("N0", CultureInfo.GetCultureInfo("tr-TR")) + " kg"
            };
        }

        private static bool HasWeighing(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var normalized = value.Trim();
            return normalized.IndexOf("tartımsız", StringComparison.OrdinalIgnoreCase) < 0 &&
                   normalized.IndexOf("tartimsiz", StringComparison.OrdinalIgnoreCase) < 0 &&
                   normalized.IndexOf("tartım yok", StringComparison.OrdinalIgnoreCase) < 0 &&
                   normalized.IndexOf("tartim yok", StringComparison.OrdinalIgnoreCase) < 0 &&
                   normalized != "0" &&
                   normalized != "0,00" &&
                   normalized != "-";
        }

        private static string NormalizePlate(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? ""
                : value.Replace(" ", "").Trim().ToUpperInvariant();
        }

        private static string FirstNonEmpty(string first, string second)
        {
            return !string.IsNullOrWhiteSpace(first) ? first.Trim() : (second ?? "").Trim();
        }
    }
}
```

- [ ] **Step 3: Resolver testlerini calistir**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Debug /v:minimal
& "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll /Tests:KantarFisKaynakResolverTests
```

Expected: Build succeeds and all four resolver tests pass.

- [ ] **Step 4: Resolver uygulamasini commit et**

```powershell
git add src/KantarPro.Desktop/KantarFisKaynakResolver.cs src/KantarPro.Desktop/KantarPro.Desktop.csproj
git commit -m "feat: tartimsiz ziyarette ilk tartim fisini coz"
```

### Task 3: Makbuz Goster ve Yazdir Akislarini Resolvera Bagla

**Files:**
- Modify: `src/KantarPro.Desktop/MainWindow.xaml.cs:740-790`
- Modify: `src/KantarPro.Desktop/MainWindow.xaml.cs:934-960`
- Test: `tests/KantarPro.Application.Tests/KantarFisKaynakResolverTests.cs`

- [ ] **Step 1: Veritabanindan aday dosyalari yukleyen yardimci metodu ekle**

`MainWindow.xaml.cs` icinde fis metotlarinin yanina ekle:

```csharp
private static VehicleMovementRow ResolveKantarFisRow(VehicleMovementRow selectedRow)
{
    if (selectedRow == null)
    {
        throw new ArgumentNullException(nameof(selectedRow));
    }

    using (var context = KantarDbContextFactory.Create())
    {
        var selectedVisit = context.Islemler
            .Include(x => x.Arac)
            .FirstOrDefault(x => x.IslemId == selectedRow.IslemId);
        if (selectedVisit == null)
        {
            throw new InvalidOperationException("Kantar fişi için işlem kaydı bulunamadı.");
        }

        var normalizedPlate = NormalizePlaka(selectedRow.Plaka);
        var pendingFiles = context.KantarDosyalari
            .Include(x => x.Arac)
            .Include(x => x.IlkTartim)
            .Include(x => x.IlkTartim.Islem)
            .Where(x =>
                x.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor &&
                (x.AracId == selectedVisit.AracId || x.Arac.Plaka == normalizedPlate))
            .ToList();

        return KantarFisKaynakResolver.Resolve(selectedRow, pendingFiles);
    }
}
```

- [ ] **Step 2: Yazdirma akisini cozulmus satira yonlendir**

`PrintKantarFisi` metodunun `try` blogunu su hale getir:

```csharp
try
{
    var receiptRow = ResolveKantarFisRow(row);
    PrintKantarFisiCore(receiptRow);
}
catch (Exception ex)
{
    MessageBox.Show(ex.Message, "Kantar fişi yazdırılamadı", MessageBoxButton.OK, MessageBoxImage.Warning);
}
```

- [ ] **Step 3: Onizleme akisini ayni cozulmus satira yonlendir**

`ShowKantarFisiPreview` metodunun `try` blogunu su hale getir:

```csharp
try
{
    var receiptRow = ResolveKantarFisRow(row);
    if (string.IsNullOrWhiteSpace(receiptRow.KantarFisNo))
    {
        receiptRow.KantarFisNo = EnsureKantarFisNoForVehicleRow(receiptRow);
    }

    var rawText = KantarFisFormatter.BuildFromRow(receiptRow);
    var preview = new KantarFisPreviewWindow(
        KantarFisPreviewData.FromVehicleRow(receiptRow, rawText))
    {
        Owner = this
    };
    preview.ShowDialog();
}
catch (Exception ex)
{
    MessageBox.Show(ex.Message, "Kantar fişi oluşturulamadı", MessageBoxButton.OK, MessageBoxImage.Warning);
}
```

- [ ] **Step 4: Resolver ve formatter birlikteligini test et**

`KantarFisKaynakResolverTests.cs` dosyasina ekle:

```csharp
[TestMethod]
public void Resolve_BekleyenIlkTartimFormatterdaEskiFisNumarasiniKorur()
{
    var selected = new VehicleMovementRow
    {
        IslemId = 2,
        Plaka = "16BKK767",
        Tartim = "Tartım Yok"
    };
    var source = KantarFisKaynakResolver.Resolve(
        selected,
        new List<KantarDosyasi>
        {
            CreatePendingFile(1, "16BKK767", 34000m)
        });

    var rawText = KantarFisFormatter.BuildFromRow(source);
    var preview = KantarFisPreviewData.FromVehicleRow(source, rawText);

    Assert.AreEqual("00001", preview.FisNo);
    Assert.AreEqual("34.000 kg", preview.BirinciTartim);
    StringAssert.Contains(rawText, "00001");
    StringAssert.Contains(rawText, "34.000 Kg");
}
```

- [ ] **Step 5: Tum testleri calistir**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Debug /v:minimal
& "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll
```

Expected: Build succeeds; existing 95 tests plus the new resolver tests all pass.

- [ ] **Step 6: UI baglantisini commit et**

```powershell
git add src/KantarPro.Desktop/MainWindow.xaml.cs tests/KantarPro.Application.Tests/KantarFisKaynakResolverTests.cs
git commit -m "fix: tartimsiz ikinci ziyarette ilk tartim fisini goster"
```

### Task 4: Release ve Gercek Senaryo Dogrulamasi

**Files:**
- Verify: `src/KantarPro.Desktop/bin/Release/KantarPro.Desktop.exe`
- Verify: live SQL database, no schema change

- [ ] **Step 1: Release derlemesi al**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Release /v:minimal
```

Expected: `Build succeeded`, zero errors.

- [ ] **Step 2: Mevcut 16BKK767 senaryosunu onizleme ile dogrula**

1. Programi ac.
2. Ust listede tartimsiz ikinci ziyarette bulunan `16BKK767` satirini sec.
3. Sag tik `Makbuz Goster` sec.
4. Fiste su degerleri dogrula:
   - Fis No: `00001`
   - Islem No: ilk ziyarete ait numara
   - Giris: `11.06.2026 14:07:30`
   - 1. Tartim: `34.000 kg`
5. Pencereyi kapatip yeniden ac; fis numarasinin degismedigini dogrula.

- [ ] **Step 3: Yazdirma yolunu dogrula**

1. Ayni satirda sag tik `Makbuz Yazdir` sec.
2. Yazici/onizleme cikisinda ilk tartim fisinin kullanildigini dogrula.
3. Islemi tekrar et ve yeni fis numarasi tuketilmedigini SQL sorgusuyla dogrula:

```sql
SELECT TartimId, IslemId, AgirlikKg, KantarFisNo
FROM Tartimlar
WHERE AracId = (SELECT TOP 1 AracId FROM Araclar WHERE Plaka = '16BKK767')
ORDER BY TartimId;
```

Expected: Ilk tartimin `KantarFisNo` degeri ayni kalir; tartimsiz ikinci ziyaret icin yeni `Tartimlar` satiri olusmaz.

- [ ] **Step 4: Regresyon kontrolu yap**

Su uc kayitta `Makbuz Goster` davranisini kontrol et:

1. Kendi tek tartimi bulunan acik islem: kendi tek tartim fisi acilir.
2. Iki tartimi tamamlanmis islem: dolu-bos fisi acilir.
3. Hic tartimi ve bekleyen dosyasi olmayan tartimsiz islem: mevcut uyari gorunur.

- [ ] **Step 5: Son durumu kaydet**

Kod degisikligi kalmadiysa:

```powershell
git status --short
git log -3 --oneline
```

Expected: Bu ozellige ait test, resolver ve UI commitleri gorunur; beklenmeyen yeni dosya yoktur.
