# Plaka Duzeltme Servis Refaktor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the high-risk wrong-plate transfer logic from the WPF `MainWindow` into `SahaZiyaretiServisi` without changing the saha/kantar workflow behavior.

**Architecture:** Keep UI responsibility limited to collecting user input and optional second-weight confirmation. Put the database/business mutation into `SahaZiyaretiServisi.PlakaHatasiniDuzelt`, backed by MSTest coverage against the in-memory unit of work. Add repository delete support only because the existing UI method already removes temporary wrong-plate `KantarDosyasi` rows.

**Tech Stack:** C# 7.3, WPF, .NET Framework 4.8, Entity Framework 6, MSTest.

---

## File Map

- Modify `src/KantarPro.Application/Abstractions/IRepository.cs`: add `Remove` and `RemoveRange`.
- Modify `src/KantarPro.Infrastructure/Data/EfRepository.cs`: implement delete operations for EF.
- Modify `tests/KantarPro.Application.Tests/Fakes/InMemoryRepository.cs`: implement delete operations for tests.
- Modify `src/KantarPro.Application/Services/SahaZiyaretiServisi.cs`: add `PlakaHatasiniDuzelt` and helpers.
- Modify `tests/KantarPro.Application.Tests/SahaZiyaretiServisiTests.cs`: add regression tests for wrong-plate transfer and conflict cases.
- Modify `src/KantarPro.Desktop/MainWindow.xaml.cs`: replace `EskiPlakaKaydiniMevcutAracaTasi` with service call, then remove the old UI method.

---

### Task 1: Repository Delete Support

**Files:**
- Modify: `src/KantarPro.Application/Abstractions/IRepository.cs`
- Modify: `src/KantarPro.Infrastructure/Data/EfRepository.cs`
- Modify: `tests/KantarPro.Application.Tests/Fakes/InMemoryRepository.cs`

- [ ] **Step 1: Add delete signatures**

In `IRepository<T>`, add:

```csharp
void Remove(T entity);
void RemoveRange(IEnumerable<T> entities);
```

Also add `using System.Collections.Generic;`.

- [ ] **Step 2: Implement EF delete**

In `EfRepository<T>`, add:

```csharp
public void Remove(T entity)
{
    _set.Remove(entity);
}

public void RemoveRange(IEnumerable<T> entities)
{
    _set.RemoveRange(entities);
}
```

Also add `using System.Collections.Generic;`.

- [ ] **Step 3: Implement in-memory delete**

In `InMemoryRepository<T>`, add:

```csharp
public void Remove(T entity)
{
    _items.Remove(entity);
}

public void RemoveRange(IEnumerable<T> entities)
{
    foreach (var entity in entities.ToList())
    {
        _items.Remove(entity);
    }
}
```

- [ ] **Step 4: Build**

Run:

```powershell
MSBuild.exe .\KantarPro.sln /t:Build /p:Configuration=Debug
```

Expected: build succeeds. Existing obsolete warnings from `IslemServisiTests` are acceptable.

---

### Task 2: Service Regression Tests

**Files:**
- Modify: `tests/KantarPro.Application.Tests/SahaZiyaretiServisiTests.cs`

- [ ] **Step 1: Add wrong-plate transfer test**

Add a test that builds this scenario:

1. Correct plate `23FHE956` has a first loaded weighing and exits.
2. Wrong plate `23FEH956` is later entered with a second weighing.
3. `PlakaHatasiniDuzelt("23FEH956", "23FHE956", 12000m, 1)` is called.

Expected assertions:

```csharp
Assert.AreEqual(correctArac.AracId, wrongVisit.AracId);
Assert.IsTrue(wrongVisit.Tartimlar.All(x => x.AracId == correctArac.AracId));
Assert.AreEqual(KantarSabitleri.KantarDosyasiDurumu.Tamamlandi, dosya.Durum);
Assert.AreEqual(16000m, dosya.NetAgirlikKg);
Assert.AreEqual(KantarSabitleri.YukDurumu.Bos, wrongVisit.Tartimlar.Last().YukDurumu);
Assert.AreEqual(0, uow.KantarDosyasiListesi.Count(x => x.AracId == wrongAracId));
Assert.IsTrue(uow.LogListesi.Any(x => x.LogTipi == "PlakaDuzeltme"));
```

- [ ] **Step 2: Add duplicate pending file test**

Add a test with two waiting `KantarDosyasi` records for the correct plate and verify:

```csharp
AssertInvalidOperation(() =>
    servis.PlakaHatasiniDuzelt("23FEH956", "23FHE956", null, 1));
```

Expected message behavior: service throws before completing the transfer because the correct pending file is ambiguous.

- [ ] **Step 3: Run tests and verify failure**

Run:

```powershell
vstest.console.exe .\tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll
```

Expected before implementation: compile or test failure because `PlakaHatasiniDuzelt` does not exist.

---

### Task 3: Move Business Logic Into SahaZiyaretiServisi

**Files:**
- Modify: `src/KantarPro.Application/Services/SahaZiyaretiServisi.cs`

- [ ] **Step 1: Add public method**

Add:

```csharp
public void PlakaHatasiniDuzelt(string hataliPlaka, string dogruPlaka, decimal? onaylananIkinciAgirlikKg, int kullaniciId)
```

This method must:

1. Normalize both plates.
2. Find the open visit for `hataliPlaka`.
3. Reject if `dogruPlaka` has another open visit.
4. Find or create the target `Arac` for `dogruPlaka`.
5. Remove temporary wrong-plate `KantarDosyasi` rows whose `IlkTartimId` belongs to the open visit.
6. Move `Islem` and all its `Tartimlar` to the target `Arac`.
7. If the target plate has exactly one waiting `KantarDosyasi`, bind the open visit's last weighing as `KarsiTartim`, update load direction, net, status, and completion date.
8. Log `PlakaDuzeltme`.
9. Save changes.

- [ ] **Step 2: Preserve no-pending-file behavior**

If there is no waiting target `KantarDosyasi`, the method should still move the plate and weights but should not force dolu-bos completion. This matches the existing UI method.

- [ ] **Step 3: Run focused tests**

Run:

```powershell
vstest.console.exe .\tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll
```

Expected: new tests pass, existing tests pass.

---

### Task 4: Replace MainWindow Direct Database Mutation

**Files:**
- Modify: `src/KantarPro.Desktop/MainWindow.xaml.cs`

- [ ] **Step 1: Replace old method call**

In `DashboardPlakaDegistir`, replace:

```csharp
EskiPlakaKaydiniMevcutAracaTasi(context, islem, hedefArac, onaylananIkinciTartim);
```

with:

```csharp
var kullaniciId = EnsureAdminUser(context);
var servis = new SahaZiyaretiServisi(new KantarUnitOfWork(context));
servis.PlakaHatasiniDuzelt(eskiPlaka, yeniPlaka, onaylananIkinciTartim, kullaniciId);
```

- [ ] **Step 2: Keep simple new-plate rename path**

If `hedefArac == null`, keep the current direct `islem.Arac.Plaka = yeniPlaka;` path because it is a simple plate correction and does not merge into an existing plate's waiting file.

- [ ] **Step 3: Remove old UI method**

Delete:

```csharp
private static void EskiPlakaKaydiniMevcutAracaTasi(...)
```

- [ ] **Step 4: Build**

Run:

```powershell
MSBuild.exe .\KantarPro.sln /t:Build /p:Configuration=Debug
```

Expected: build succeeds.

---

### Task 5: Full Verification

**Files:**
- No code edits.

- [ ] **Step 1: Run all tests**

Run:

```powershell
vstest.console.exe .\tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll
```

Expected: all tests pass.

- [ ] **Step 2: Search for old method**

Run:

```powershell
rg "EskiPlakaKaydiniMevcutAracaTasi|PlakaHatasiniDuzelt" -n src tests
```

Expected: old method name is absent; new method appears in service, tests, and UI call.

- [ ] **Step 3: Review git diff**

Run:

```powershell
git diff --stat
git diff -- src/KantarPro.Application/Services/SahaZiyaretiServisi.cs src/KantarPro.Desktop/MainWindow.xaml.cs
```

Expected: UI code is smaller; service owns the data mutation.

---

## Self-Review

- Spec coverage: the plan protects the wrong-plate transfer workflow before moving it.
- Placeholder scan: no TBD/TODO steps.
- Type consistency: method name is consistently `PlakaHatasiniDuzelt`; entity names match the current codebase.
