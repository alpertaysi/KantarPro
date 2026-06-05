# Kantar Fisi Iki Katmanli Onizleme Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Makbuz Goster/Kantar Fisi Goster penceresini modern okunur onizleme + OKI 5720'ye gidecek ham metin gorunumu olarak iki katmanli hale getirmek.

**Architecture:** Mevcut `KantarFisFormatter` ham metin uretmeye devam eder. Yeni bir `KantarFisPreviewData` modeli secili satirdan modern onizleme alanlarini hazirlar. `KantarFisPreviewWindow` hem modern karti hem de ham metin sekmesini gosterir; yazdirma simdilik ham metin uzerinden uyarili kalir.

**Tech Stack:** WPF .NET Framework 4.8, MSTest, mevcut Desktop/Application test projesi.

---

### Task 1: Preview Data Model

**Files:**
- Create: `src/KantarPro.Desktop/KantarFisPreviewData.cs`
- Test: `tests/KantarPro.Application.Tests/KantarFisFormatterTests.cs`
- Modify: `src/KantarPro.Desktop/KantarPro.Desktop.csproj`

- [ ] **Step 1: Write failing tests**

Add tests proving tek tartim and dolu-bos preview data are produced from existing row objects.

- [ ] **Step 2: Run tests and verify failure**

Run: `MSBuild tests\KantarPro.Application.Tests\KantarPro.Application.Tests.csproj /p:Configuration=Debug /v:minimal`

Expected: compile fails because `KantarFisPreviewData` does not exist.

- [ ] **Step 3: Implement `KantarFisPreviewData`**

Create a small immutable-style model with:

- `FisTipi`
- `Plaka`
- `FisNo`
- `GirisTarihi`
- `GirisSaati`
- `IkinciGirisTarihi`
- `IkinciGirisSaati`
- `BirinciTartim`
- `IkinciTartim`
- `Net`
- `RawText`
- `SurekliFormNotu`

Factory methods:

- `FromVehicleRow(VehicleMovementRow row, string rawText)`
- `FromPendingRow(PendingWeighingPrototypeRow row, string rawText)`

- [ ] **Step 4: Run tests and verify pass**

Run: `vstest.console.exe tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll`

Expected: all tests pass.

### Task 2: Modern Preview Window

**Files:**
- Modify: `src/KantarPro.Desktop/KantarFisPreviewWindow.xaml`
- Modify: `src/KantarPro.Desktop/KantarFisPreviewWindow.xaml.cs`
- Modify: `src/KantarPro.Desktop/MainWindow.xaml.cs`

- [ ] **Step 1: Change window constructor**

Replace `KantarFisPreviewWindow(string receiptText)` with `KantarFisPreviewWindow(KantarFisPreviewData data)`.

- [ ] **Step 2: Build modern card UI**

Use a two-tab or two-section layout:

- `Onizleme`: modern readable fields
- `Ham Metin`: exact `RawText` in Courier New

Keep buttons:

- `Yazdir`
- `Kapat`

Add button:

- `Ham Metni Goster`

- [ ] **Step 3: Wire existing preview calls**

In `MainWindow.xaml.cs`, create raw text with existing formatter, create `KantarFisPreviewData`, and pass it to the window.

- [ ] **Step 4: Build solution**

Run: `MSBuild KantarPro.sln /p:Configuration=Debug /v:minimal`

Expected: build succeeds.

### Task 3: Verification

**Files:**
- No new files expected.

- [ ] **Step 1: Run full tests**

Run: `vstest.console.exe tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll`

Expected: all tests pass.

- [ ] **Step 2: Start desktop app**

Run: `Start-Process src\KantarPro.Desktop\bin\Debug\KantarPro.Desktop.exe`

Expected: app opens.

- [ ] **Step 3: Manual visual check**

Use `Makbuz Goster` on:

- one tek tartim row
- one dolu-bos completed row
- one tartimsiz row

Expected:

- tek tartim shows only first weighing data
- dolu-bos shows two weighings and net
- tartimsiz row warns that receipt cannot be created
- raw text remains available and matches OKI 5720 continuous-form style
