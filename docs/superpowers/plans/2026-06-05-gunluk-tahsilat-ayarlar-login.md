# Gunluk Tahsilat, Ayarlar ve Login Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Gunluk tahsilat ekranini kullanisli hale getirmek, PDF dokum eklemek, ayarlari ayirmak ve program acilisina rol bazli kullanici girisi eklemek.

**Architecture:** Masaustu arayuz degisiklikleri WPF `MainWindow` ve yeni kucuk pencerelerle yapilacak. Kimlik dogrulama mevcut `Kullanicilar` tablosuna baglanacak; sifre hash ve rol kontrolu uygulama tarafinda kucuk, test edilebilir yardimci siniflarla tutulacak. PDF dokum, ek harici kutuphane gerektirmeden `FlowDocument`/`PrintDialog` yerine dosyaya sabit ve okunakli XPS/PDF benzeri ihtiyac icin Windows yazdirma akisina degil, basit HTML->PDF olmayan local `PrintDocument`/dosya akisindan ayrilacak; ilk hedef kullanicinin secilen yere PDF dosyasi alabilmesidir.

**Tech Stack:** .NET Framework 4.8, WPF, Entity Framework 6, MSTest, SQL Server Express.

---

### Task 1: Yazdirma Sonrasi Mesaji Kaldir

**Files:**
- Modify: `src/KantarPro.Desktop/MainWindow.xaml.cs`
- Modify: `src/KantarPro.Desktop/KantarFisPreviewWindow.xaml.cs`

- [ ] `PrintKantarFisi` ve `PrintPendingKantarFisi` icindeki basarili yazdirma messagebox'larini kaldir.
- [ ] Onizleme penceresindeki `Yazdir` butonunda da basarili yazdirma mesaji gosterme; hata olursa mesaj kalacak.
- [ ] Kantar fisi yazdirma akisini elle derleyerek dogrula.

### Task 2: Gunluk Tahsilat Ekranini Sigdir ve PDF Butonu Ekle

**Files:**
- Modify: `src/KantarPro.Desktop/MainWindow.xaml`
- Modify: `src/KantarPro.Desktop/MainWindow.DataAndFormatting.cs`
- Modify: `src/KantarPro.Desktop/MainWindow.xaml.cs`
- Create: `src/KantarPro.Desktop/DailyRevenuePdfExporter.cs`
- Test: `tests/KantarPro.Application.Tests` altinda uygun formatter/exporter testi

- [ ] `Rapor Dokumu` butonunu kaldir.
- [ ] `Excel'e Gonder` butonunu `PDF Olarak Disa Aktar` yap ve click handler ekle.
- [ ] `DailyRevenueGrid` icin yatay scroll gorunur olsun; toplam bandi pencere disina tasmasin.
- [ ] PDF exporter, gunluk tahsilat satirlarini ve toplamlarini basit dokum olarak dosyaya yazsin.
- [ ] SaveFileDialog ile hedef PDF yolu alinsin.

### Task 3: Ayarlar Ekranini Ayir

**Files:**
- Modify: `src/KantarPro.Desktop/MainWindow.xaml`
- Modify: `src/KantarPro.Desktop/MainWindow.xaml.cs`

- [ ] Ayarlar sayfasi icine `TabControl` ekle.
- [ ] `Ucret Ayarlari` sekmesinde sadece ucret kutulari ve ucret kaydetme butonu olsun.
- [ ] `Baglanti Ayarlari` sekmesinde SQL, istasyon ve COM kutulari olsun.
- [ ] Mevcut `Kaydet` davranisini iki kaydetme davranisina ayir: ucret kaydet ve baglanti kaydet.

### Task 4: Login ve Rol Altyapisi

**Files:**
- Create: `src/KantarPro.Application/Services/KullaniciServisi.cs`
- Create: `src/KantarPro.Desktop/LoginWindow.xaml`
- Create: `src/KantarPro.Desktop/LoginWindow.xaml.cs`
- Modify: `src/KantarPro.Desktop/App.xaml.cs`
- Modify: `src/KantarPro.Desktop/MainWindow.xaml.cs`
- Modify: `src/KantarPro.Desktop/KantarPro.Desktop.csproj`
- Test: `tests/KantarPro.Application.Tests/KullaniciServisiTests.cs`

- [ ] Parola hash icin PBKDF2 tabanli helper ekle.
- [ ] Varsayilan `admin/admin` ve `memur/memur` ilk kurulum kullanicilarini sadece tablo bossa olustur.
- [ ] Login penceresi basarili giriste kullanici nesnesini MainWindow'a aktarir.
- [ ] MainWindow artik `EnsureAdminUser` yerine aktif kullanici id'sini kullanir.
- [ ] Memur rolunde Ayarlar sekmesi gizlenir veya pasif olur; Admin rolunde acik kalir.

### Task 5: Dogrulama

**Commands:**
- `MSBuild.exe KantarPro.sln /p:Configuration=Debug /v:minimal`
- `vstest.console.exe tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll`

- [ ] Derleme hatasiz olmali.
- [ ] Tum testler gecmeli.
- [ ] Program acilisinda login gorunmeli.
- [ ] Admin ile ayarlar acilmali, memur ile ayarlar kapali olmali.
- [ ] Gunluk Tahsilat listesi pencereye sigmali ve PDF dosyasi olusturabilmeli.
