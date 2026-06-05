# Kantar Fisi Kalici Surucu Baski Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Kantar fisi baskisini OKI ML5720 icin Windows suruculu 5.5 inch surekli form baskisi olarak kalici hale getirmek.

**Architecture:** `KantarFisFormatter` yalnizca sabit genislikli ham metin uretir. `KantarFisPreviewWindow` sadece ham metni gosterir ve suruculu yazdirma yapar. `MainWindow` Makbuz butonunda dogrudan yazdirir, sag tik Makbuz Goster ile onizleme acar, tartimli kayitlardan sonra kullaniciya yazdirma onayi sorar.

**Tech Stack:** WPF, .NET Framework 4.8, `System.Drawing.Printing`, MSTest.

---

### Task 1: Fis metnini sahaya uygun hale getir

**Files:**
- Modify: `src/KantarPro.Desktop/KantarFisFormatter.cs`
- Modify: `src/KantarPro.Desktop/KantarFisPreviewData.cs`
- Test: `tests/KantarPro.Application.Tests/KantarFisFormatterTests.cs`

- [ ] **Step 1: Formatlayiciya kurum ve firma satirlarini ekle**

`AppendHeader` sirasi `TURKIYE CUMHURIYETI`, `TICARET BAKANLIGI`, `ULUDAG GUMRUK VE TICARET BOLGE MUDURLUGU`, `BURSA TASFIYE ISLETME MUDURLUGU` olacak.

Tek tartim ve dolu-bos fislerinde `FIRMA` satiri plaka/fis satirindan sonra yazilacak.

- [ ] **Step 2: Preview data'ya firma bilgisini tasit**

`KantarFisPreviewData` icine `Firma` ozelligi eklenecek ve `VehicleMovementRow.Firma`, `PendingWeighingPrototypeRow.Firma` degerleri atanacak.

- [ ] **Step 3: Formatter testlerini guncelle**

Mevcut testlerde yeni kurum satiri, `BURSA TASFIYE` boslugu ve `FIRMA` satiri beklenir.

### Task 2: Onizlemeyi ham metin odakli yap

**Files:**
- Replace: `src/KantarPro.Desktop/KantarFisPreviewWindow.xaml`
- Modify: `src/KantarPro.Desktop/KantarFisPreviewWindow.xaml.cs`

- [ ] **Step 1: Modern kart onizlemeyi kaldir**

Pencere sadece baslik, aciklama, ham metin textbox, `Font`, `Yazdir`, `Kapat` icerecek.

- [ ] **Step 2: Yazdir butonunu suruculu baskiya bagla**

`Yazdir` butonu `RawPrinterHelper.PrintTextWithDriver(...)` cagirir. Varsayilan font 12 kalir.

### Task 3: Makbuz butonlarini dogrudan yazdir/goster olarak ayir

**Files:**
- Modify: `src/KantarPro.Desktop/MainWindow.xaml.cs`

- [ ] **Step 1: Dogrudan yazdirma yardimcilari ekle**

`PrintKantarFisi(row)` ve `PrintPendingKantarFisi(row)` fis no uretir, raw metin uretir, `RawPrinterHelper.PrintTextWithDriver(...)` cagirir.

- [ ] **Step 2: Makbuz butonlarini dogrudan yazdir**

Ana `Makbuz` ve sag tik `Makbuz Yazdir` olaylari onizleme acmadan dogrudan yazdirir. `Makbuz Goster` olaylari onizleme acmaya devam eder.

### Task 4: Tartim sonrasi yazdirma onayi ekle

**Files:**
- Modify: `src/KantarPro.Desktop/MainWindow.xaml.cs`

- [ ] **Step 1: Tartimli giris sonrasi sor**

`DashboardGirisKaydiOlustur` tartimli kayit olusturduktan sonra ilgili plaka icin yeni satiri bulur ve `Kantar fisi yazdirilsin mi?` diye sorar.

- [ ] **Step 2: Sonradan tartim ve ikinci tartim sonrasi sor**

Sag tik tartim ve dolu-bos formundan gelen tartimli kayitlardan sonra ilgili satir icin ayni onay sorulur.

### Task 5: Dogrulama

**Files:**
- Test: `tests/KantarPro.Application.Tests/bin/Debug/KantarPro.Application.Tests.dll`

- [ ] **Step 1: Build**

Run: `MSBuild.exe KantarPro.sln /p:Configuration=Debug /v:minimal`

- [ ] **Step 2: Tests**

Run: `vstest.console.exe tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll`

- [ ] **Step 3: App restart**

Run rebuilt desktop app for manual printer test.
