# KantarPro Proje Devam Rehberi

Son guncelleme: 05.06.2026

Bu dosya, sohbet gecmisi olmayan yeni bir Codex oturumunda KantarPro projesini yeniden tanitmak icin hazirlanmistir. Yeni oturumda once bu dosya okunmali, sonra `git status`, derleme ve test komutlari calistirilmalidir. Amac, bu projeyi hic bilmeyen bir yardimcinin is mantigini, teknik yapiyi, sahadaki gercek kullanim senaryolarini ve hassas noktalari tek dosyadan anlamasidir.

## 1. Yeni Oturumda Ilk Yapilacaklar

Yeni hesapta veya yeni bilgisayarda projeye devam ederken su sirayi izle:

```powershell
cd "C:\Users\DELL\OneDrive\Masaustu\KantarPro_Tasima_Paketi\KantarPro_Tasima_Paketi\project\Codex Kantar"
git status -sb
```

Derleme:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" "KantarPro.sln" /p:Configuration=Debug /v:minimal
```

Test:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" "tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll"
```

Programi acma:

```powershell
Start-Process -FilePath "src\KantarPro.Desktop\bin\Debug\KantarPro.Desktop.exe"
```

Varsayilan ilk kullanicilar:

- Admin: `admin` / `admin`
- Memur: `memur` / `memur`

Admin rolunde Ayarlar sekmesi gorunur. Memur rolunde Ayarlar sekmesi gizlenir.

## 2. Projenin Amaci

KantarPro, Bursa Tasfiye Isletme Mudurlugu sahasinda kullanilacak gumruk kantar otomasyon programidir. Programin temel hedefi, aktif kullanilan eski kantar programindaki giris-cikis ve tartim islerini daha anlasilir, daha izlenebilir ve iki bilgisayarli ortak SQL yapisina uygun sekilde yeniden kurmaktir.

Program su isleri yapar:

- Arac sahaya giris kaydi.
- Tartimli, tartimsiz ve muaf giris.
- Dolu-bos kantar takip dosyasi.
- Ilk tartim, ikinci tartim ve net agirlik hesabi.
- Cikis ve tahsilat.
- Bekleme/isgaliye ucreti hesabi.
- Nakit veya kredi karti odeme turu.
- Gunluk tahsilat dokumu.
- PDF olarak gunluk tahsilat disari aktarma.
- OKI ML5720 nokta vuruslu yazici icin kantar fisi basma.
- SQL Server uzerinden iki bilgisayarli ortak calisma.
- Kullanici adi/sifre girisi ve Admin/Memur rol ayrimi.

## 3. Calisma Klasoru ve Git

Ana proje klasoru:

```text
C:\Users\DELL\OneDrive\Masaustu\KantarPro_Tasima_Paketi\KantarPro_Tasima_Paketi\project\Codex Kantar
```

Solution:

```text
KantarPro.sln
```

GitHub remote:

```text
https://github.com/alpertaysi/KantarPro.git
```

Aktif gelistirme branch'i:

```text
codex/saha-ziyareti-model
```

## 4. Teknoloji Yigini

- Dil: C#
- UI: WPF
- Framework: .NET Framework 4.8
- ORM: Entity Framework 6
- Veritabani: SQL Server Express
- Test: MSTest / Visual Studio Test Platform
- Yazici: OKI ML5720 / OKI Dot-Matrix 9Pin ESC/P Class Driver
- Kantar haberlesmesi: RS232 / USB-to-Serial COM port

## 5. Katmanlar

### 5.1 Domain

Klasor:

```text
src\KantarPro.Domain
```

Temel entity ve sabitler buradadir:

- `Entities\Arac.cs`
- `Entities\Islem.cs`
- `Entities\Tartim.cs`
- `Entities\KantarDosyasi.cs`
- `Entities\IslemUcreti.cs`
- `Entities\Kullanici.cs`
- `Entities\LogKaydi.cs`
- `KantarSabitleri.cs`

### 5.2 Application

Klasor:

```text
src\KantarPro.Application
```

Is kurallari burada tutulur. Yeni is mantigi mumkunse bu katmana eklenmelidir.

Onemli servisler:

- `Services\SahaZiyaretiServisi.cs`: yeni ana is akisi. Giris, cikis, sonradan tartim, muafiyet, plaka/firma duzeltme, kantar dosyasi islemleri.
- `Services\UcretAyarlariServisi.cs`: giris-cikis, tartim ve bekleme ucretlerini okur/gunceller.
- `Services\KullaniciServisi.cs`: login, parola hash, varsayilan admin/memur kullanicilari.
- `Services\KantarDisplayFormatter.cs`: durum ve liste gorunum metinleri.
- `Services\IslemServisi.cs`: eski modelden kalan servis. Bazi testler hala bunu kapsar ama yeni akislarda asil servis `SahaZiyaretiServisi`dir.

### 5.3 Infrastructure

Klasor:

```text
src\KantarPro.Infrastructure
```

EF6 veritabani altyapisi:

- `Data\KantarDbContext.cs`
- `Data\KantarUnitOfWork.cs`
- `Data\EfRepository.cs`

### 5.4 Desktop

Klasor:

```text
src\KantarPro.Desktop
```

WPF ekranlari ve masaustu davranislari:

- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- `MainWindow.DashboardData.cs`
- `MainWindow.DataAndFormatting.cs`
- `MainWindow.DoluBosAndFilters.cs`
- `DashboardRowBuilder.cs`
- `DashboardVisitInfo.cs`
- `DashboardRows.cs`
- `KantarSerialReader.cs`
- `KantarFisFormatter.cs`
- `RawPrinterHelper.cs`
- `DailyRevenuePdfExporter.cs`
- `LoginWindow.xaml`
- `ExitConfirmationWindow.xaml`
- `DoluBosSecondWeighingWindow.xaml`
- `PaymentTypeChoiceWindow.xaml`
- `PrintReceiptPromptWindow.xaml`
- `KantarFisPreviewWindow.xaml`

## 6. Veritabani Modeli

Temel tablolar:

- `Araclar`: plaka, firma, arac bilgisi.
- `Islemler`: sahaya giris-cikis ziyareti.
- `Tartimlar`: her tartim kaydi.
- `KantarDosyalari`: dolu-bos eslestirme dosyasi.
- `IslemUcretleri`: tahakkuk ve tahsilat kalemleri.
- `Ucretler`: yillik ucret tarifesi.
- `Kullanicilar`: admin/memur kullanici kayitlari.
- `Loglar`: kritik islemlerin log kaydi.
- `Ayarlar`: uygulama ayarlari.

`EnsureDatabaseSchema()` uygulama acilisinda eksik kolon/tablo eklemeleri icin calisir. Mevcut SQL yedeklerinden veya eski kurulumdan gelen veritabanini bozmadan gerekli kolonlari eklemek icin kullanilir.

Onemli kolonlar:

- `Islemler.CikisNo`: her cikis icin surekli artan 4 haneli cikis/tahsilat no.
- `Islemler.MuafMi`, `Islemler.MuafiyetNedeni`: ucretten muaf araclar.
- `Islemler.Notlar`: giris veya dolu-bos form aciklamalari.
- `Tartimlar.KantarFisNo`: kantar fisi numarasi.
- `IslemUcretleri.TahsilatNo`: ucretli tahsilat numarasi.
- `IslemUcretleri.OdemeTuru`: Nakit veya Kredi Karti.

## 7. Kullanici ve Yetki

Program acilirken login penceresi gelir.

Varsayilan kullanicilar tablo eksikse otomatik olusturulur:

- `admin/admin`, rol: Admin
- `memur/memur`, rol: Memur

Parolalar `KullaniciServisi` icinde PBKDF2 hash ile saklanir. Eski gelistirme placeholder hash'i sadece admin icin geriye uyumluluk amaciyla taninir.

Rol davranisi:

- Admin: tum ekranlari kullanir, Ayarlar sekmesini gorur.
- Memur: giris-cikis, tartim, tahsilat, makbuz/fis islemlerini yapar; Ayarlar sekmesini gormez.

Yeni kullanici yonetimi ekrani henuz yoktur. Gerekirse sonraki fazda Admin icin kullanici ekleme/sifre degistirme ekrani eklenebilir.

## 8. Ana Is Kavramlari

### 8.1 Arac

Plaka ve firma bilgisi ile takip edilir. Firma bilgisi giriste unutulursa sag tik `Firma Guncelle` ile sonradan degistirilebilir. Plaka hatasi icin `Kayit Duzelt` kullanilir.

### 8.2 Islem / Saha Ziyareti

Bir aracin sahaya girisinden cikisina kadar olan kayittir. Her ziyaret bir `Islem`dir.

Durumlar:

- `Iceride`: arac sahada, cikis yapmamis.
- `CikisYapti`: arac cikis yapmis.
- `Iptal`: iptal edilmis.

Islem tartimli, tartimsiz veya muaf olabilir.

### 8.3 Tartim

Bir kantar okumasidir.

Tartim tipleri:

- `Giris`
- `Cikis`
- `Sonradan`

Yuk durumu:

- `Dolu`
- `Bos`

### 8.4 Kantar Dosyasi

Dolu-bos tartimlarin eslestirildigi dosyadir.

Mantik:

1. Ilk tartim yapilir.
2. `KantarDosyasi` acilir.
3. Karsi tartim beklenir.
4. Ikinci tartim geldiginde net = iki agirligin mutlak farki.
5. Dosya tamamlanir.

Durumlar:

- `KarsiTartimBekleniyor`
- `Tamamlandi`
- `SuresiDoldu`

10 gun icinde ikinci tartim gelmeyen dosyalar suresi doldu olarak kapatilabilir.

## 9. Ana Ekran

Ust sekmeler:

- `Giris-Cikis Islemleri`
- `Gunluk Tahsilat`
- `Ayarlar` (sadece Admin)

Giris-Cikis ekraninda:

- Sol ust: plaka, tarih, saat, firma, aciklama, kilo ve kaydet alani.
- Sag ust: `Dolu-Bos Kantar Hareketleri`.
- Alt bolum: `Kesin Cikis Yapilanlar`, `2. Tartim Bekleyenler`, `Son Islem` gibi takip alanlari.

## 10. Kaydet Akisi

Sol ustteki `Kaydet` butonu dogrudan kayit yapmaz. Once secim penceresi acar:

- Tart ve Kaydet
- Tartmadan Kaydet
- Muaf

### 10.1 Tart ve Kaydet

Plaka yeni ise:

1. Arac kaydi acilir.
2. Kantar kilosu alinir.
3. Ilk tartim yazilir.
4. Giris-cikis + tartim ucreti tahakkuk eder.
5. Kantar fisi yazdirilsin mi sorulur.

Plaka daha once ilk tartim yapip ciktiysa:

1. Sistem alt listede bekleyen dolu-bos dosyasini bulur.
2. Dolu-bos ikinci tartim formu acilir.
3. `Kilo Al` ile ikinci tartim alinip net hesaplanir.
4. Kaydedilince arac tekrar ust listeye alinir.
5. Cikis yapinca kesin cikisa gider.

Plaka zaten icerideyse:

- Yeni kayit acmamali.
- Kullaniciya mevcut acik islem oldugu bildirilmeli.
- Icerideki arac icin sag tik `Tart` veya `Cikis Yap` kullanilir.

### 10.2 Tartmadan Kaydet

Arac tartimsiz girer.

- Sadece giris-cikis ucreti tahakkuk eder.
- Tartim yoksa kantar fisi uretilmez.
- Sonradan sag tik `Tart` ile tartim eklenebilir.

### 10.3 Muaf

Resmi/istisnai araclar icin kullanilir.

- Muafiyet nedeni zorunludur.
- Ucret tahakkuk etmez.
- Cikis yaparken odeme turu sorulmaz.
- Gunluk tahsilatta odeme turu `Muaf` gorunur.
- Muafiyet nedeni firma alaninda gosterilir.

## 11. Cikis Akisi

`Cikis Yap`:

1. Acik islem bulunur.
2. Cikis tarihi/saatine gore bekleme ucreti hesaplanir.
3. Odeme ozet penceresi acilir.
4. Muaf degilse odeme turu sorulur:
   - Nakit
   - Kredi Karti
5. Cikis tamamlanir.
6. Tahsilat no / cikis no uretilir.
7. Ucretler tahsil edildi isaretlenir.
8. Liste yenilenir.

Test asamasinda cikis tarihi ve saati manuel degistirilebilir. Gercek sahaya gecmeden once bu alanlar kaldirilabilir veya sadece Admin rolune acilabilir.

## 12. Ucret ve Bekleme Mantigi

Ucret kalemleri:

- Giris-Cikis Ucreti
- Tartim Ucreti
- Bekleme / Isgaliye Ucreti

Bekleme hesabi:

- Ayni gun girip cikan araca bekleme ucreti yoktur.
- Gece 00:00 sonrasi her gun icin bekleme ucreti uygulanir.
- Test icin tarih/saat elle degistirildiginde listedeki bekleme/toplam alanlari da guncellenmelidir.

Ucretler `Ayarlar > Ucret Ayarlari` altindan Admin tarafindan degistirilir.

## 13. Senaryolar

### Senaryo 1: En yaygin akisi

1. Arac dolu gelir.
2. Tartilir.
3. Giris-cikis + tartim ucreti tahakkuk eder.
4. Cikis yaparken odeme alinir.
5. Arac ikinci tartim icin daha sonra gelir.
6. Sistem eski ilk tartimi yakalar.
7. Dolu-bos formu acilir.
8. Ikinci tartim alinir.
9. Net hesaplanir.
10. Ikinci ziyaret icin yeni giris-cikis + tartim ucreti tahakkuk eder.
11. Cikis yapinca kesin cikisa gider.

### Senaryo 2: Arac cikmadan ikinci tartimi ister

1. Arac tartimli girer.
2. Cikis yapmadan tekrar tartilmak ister.
3. Sag tik `Tart` ile dolu-bos formu acilir.
4. Ikinci tartim alinir.
5. Net hesaplanir.
6. Cikis yapilinca kesin cikisa gider.

### Senaryo 3: Tartimsiz giris

1. Arac tartilmak istemez.
2. Tartmadan kaydedilir.
3. Sadece giris-cikis ucreti tahakkuk eder.
4. Cikis yaparsa tartimsiz kesin cikis olur.
5. Kantar fisi yoktur.

### Senaryo 4: Tartimsiz girip sonradan tartim

1. Arac tartimsiz girer.
2. Icerideyken tartilmak ister.
3. Sag tik `Tart` ile ilk tartim eklenir.
4. Tartim ucreti eklenir.
5. Karsi tartim bekler.
6. Cikis yaparsa ikinci tartim bekleyen akisa girer.

### Senaryo 5: Ikinci tartima gelmeyen arac

1. Arac ilk tartimini yapar.
2. Cikis yapar ve odemesini yapar.
3. 2. Tartim Bekleyenler listesine duser.
4. 10 gun icinde gelmezse suresi doldu olarak kapatilir.

### Senaryo 6: Muaf arac

1. Arac gelir.
2. Muaf secilir.
3. Muafiyet nedeni yazilir.
4. Tartim varsa kayit tutulur.
5. Ucret yoktur.
6. Cikis yaparken odeme turu sorulmaz.

### Senaryo 7: Plaka hatasi

1. Ilk tartimli arac cikmis ve ikinci tartim bekliyordur.
2. Arac ikinci geliste plaka yanlis yazilabilir.
3. `Kayit Duzelt` ile dogru plaka yazilir.
4. Sistem dogru plakadaki bekleyen dosyayi bulup eslestirmelidir.
5. Birden fazla aday varsa otomatik eslestirme yapmamalidir.

## 14. Sag Tik Menuleri

`Dolu-Bos Kantar Hareketleri`:

- Tart
- Kayit Duzelt
- Firma Guncelle
- Ucretten Muaf
- Makbuz Yazdir
- Makbuz Goster

`2. Tartim Bekleyenler`:

- Makbuz Yazdir
- Makbuz Goster

`Kesin Cikis Yapilanlar`:

- Makbuz Yazdir
- Makbuz Goster

Tamamlanmis dolu-bos kaydinda tekrar `Tart` yapmaya izin verilmemelidir.

## 15. Kantar Fisi / Makbuz

Kantar fisi OKI ML5720 nokta vuruslu yazici icin ham metin olarak uretilir. Modern kart onizlemesi kaldirildi; asil cikti sabit genislikli metindir.

Ilgili dosyalar:

- `KantarFisFormatter.cs`
- `KantarFisPreviewData.cs`
- `KantarFisPreviewWindow.xaml`
- `RawPrinterHelper.cs`
- `PrintReceiptPromptWindow.xaml`

Baslik:

```text
TURKIYE CUMHURIYETI
TICARET BAKANLIGI
ULUDAG GUMRUK VE TICARET BOLGE MUDURLUGU
BURSA TASFIYE ISLETME MUDURLUGU
```

Tek tartim fisi:

- Plaka
- Fis No
- Firma
- Giris Tarihi / Saati
- 1. Tarti
- Memur Imza

Dolu-bos fisi:

- Plaka
- Fis No
- Firma
- 1. Giris Tarihi / Saati
- 2. Giris Tarihi / Saati
- 1. Tartim
- 2. Tartim
- Net
- Memur Imza

Tartimsiz araclara fis verilmez. Tartimsiz girip sonradan tartilan arac icin tek tartim fisi alinabilir.

Yazdirma davranisi:

- Tartimdan sonra `Kantar fisi yazdirilsin mi?` penceresi gelir.
- `Evet` denirse direkt yazdirir.
- Basarili yazdirma sonrasi ekstra messagebox yoktur.
- Hata olursa hata mesaji gosterilir.
- Sag tik `Makbuz Goster` onizleme acar.
- `Makbuz Yazdir` direkt yazdirir.

OKI ML5720 icin calisan yaklasim:

- Driver: OKI Dot-Matrix 9Pin ESC/P Class Driver veya OKI ML5720 uyumlu driver.
- Yontem: Windows driver uzerinden `PrintDocument`.
- Ham metin fontu: Courier New.
- Surekli form uzunlugu: sahada 14 cm / 5.5 inch olarak test edildi.
- Yazici ayarlarinda `Rear Feed`, `Page Length 139.7 mm (5.5")`, `Form Tear-Off` ve `Initial Position` cok onemlidir.

## 16. Kantar COM Okuma

Gercek kantar indikatorden veri COM porttan okunur.

Ilgili dosya:

```text
src\KantarPro.Desktop\KantarSerialReader.cs
```

Test edilen aktif ayarlar:

- Port: sahaya gore degisir. Test bilgisayarinda COM5 calisti.
- Baudrate: 9600
- Parity: None
- DataBits: 8
- StopBits: 1
- Okuma sikligi: anlik degisimi gosterecek sekilde hizlandirildi.

COM port otomatik listeleme:

- Ayarlar > Baglanti Ayarlari > Portlari Yenile
- Her bilgisayarda kendi kantarinin COM portu secilir.

Gercek sahada COM numarasi test bilgisayarindan farkli olabilir. Bu normaldir.

## 17. Iki Bilgisayarli Mimari

Hedef saha yapisi:

- Giris kantari bilgisayari SQL Server sunucusu.
- Cikis kantari bilgisayari istemci.
- Iki bilgisayar ortak SQL veritabanini kullanir.
- Her bilgisayarin COM port ayari yereldir.

Detayli kurulum:

```text
docs\KURULUM_AG_SQL.md
```

Yerel ayar dosyasi:

```text
%AppData%\KantarPro\station-settings.ini
```

Sunucu ornek ayarlari:

- SQL Server/IP: `.\SQLEXPRESS`
- Veritabani: `KantarPro`
- Windows baglantisi: isaretli
- Istasyon tipi: Giris Kantari
- COM Port: giris kantari portu

Istemci ornek ayarlari:

- SQL Server/IP: `tcp:192.168.50.1,1433`
- Veritabani: `KantarPro`
- Windows baglantisi: isaretsiz
- SQL kullanici: `kantar_app`
- Istasyon tipi: Cikis Kantari
- COM Port: cikis kantari portu

Oto yenileme vardir; memur surekli Yenile basmak zorunda kalmamali.

## 18. Gunluk Tahsilat

Tarih araligi cikis/tahsilat tarihine gore calisir.

Listelenenler:

- Ucretli cikislar.
- Muaf cikislar.
- Tek tartim, dolu-bos, tartimsiz cikislar.

Kolonlar:

- Sira
- Islem No
- Islem Tipi
- Kantar Fis No
- Odeme Turu
- Firma
- Plaka
- Cikis Tarihi
- Cikis Saati
- Giris Ucreti
- Tartim Ucreti
- Isgaliye Ucreti
- Toplam Ucret

Plaka ve Firma kolonlarinda filtre kutulari vardir.

Butonlar:

- Listele
- PDF Olarak Disa Aktar

PDF exporter:

```text
src\KantarPro.Desktop\DailyRevenuePdfExporter.cs
```

PDF icin `Microsoft Print to PDF` yazicisi kullanilir.

## 19. Ayarlar

Ayarlar sadece Admin rolunde gorunur.

Iki alt sekme vardir:

### 19.1 Ucret Ayarlari

- Giris-Cikis Ucreti
- Tartim Ucreti
- Bekleme Ucreti
- Ucretleri Kaydet

Ucretler SQL veritabaninda ortaktir.

### 19.2 Baglanti Ayarlari

- SQL Server / IP
- Veritabani
- Windows baglantisi
- SQL kullanici/sifre
- Istasyon tipi
- Kantar COM Port
- Portlari Yenile
- Baglantiyi Test Et
- Baglantiyi Kaydet

Baglanti ve COM ayarlari bu bilgisayara ozeldir.

## 20. Log ve Duzeltmeler

Loglanmasi gereken kritik islemler:

- Plaka duzeltme.
- Firma guncelleme.
- Muaf yapma.
- Cikis/tahsilat.
- Tartim ekleme.

Mevcut servislerde log mantigi kismen uygulanmistir. Yeni kritik islem eklenecekse `SahaZiyaretiServisi` icinde log eklenmelidir.

## 21. Test Durumu

Son dogrulama:

- Derleme: basarili.
- Test: 59 test, 59 gecti.

Bilinen derleme uyarilari:

- `IslemServisi` icin obsolete uyarilari var. Bunlar beklenen uyarilardir; eski model testleri halen korunuyor.

Onemli test dosyalari:

- `SahaZiyaretiServisiTests.cs`
- `IslemServisiTests.cs`
- `KantarFisFormatterTests.cs`
- `KantarSerialReaderTests.cs`
- `KullaniciServisiTests.cs`
- `UcretAyarlariServisiTests.cs`
- `AutoRefreshPolicyTests.cs`

## 22. Bilinen Hassas Noktalar

### 22.1 MainWindow halen buyuk

`MainWindow.xaml.cs` parcalara ayrildi ama hala buyuk. Yeni buyuk davranis eklenirken mumkunse:

- servis Application katmanina,
- tablo satiri olusturma DashboardRowBuilder'a,
- formatlama DashboardFormat veya formatter'a,
- ekran yardimcilari partial class'a

tasinsin.

### 22.2 Cikis tarihi/saat alanlari test amacli

Gercek sahada bu alanlar kaldirilacak veya yetkiliye acilacak. Simdilik bekleme ucreti testleri icin duruyor.

### 22.3 Kantar fisi numarasi

Fis no ve islem no konusunda idare karari gerekebilir. Su an fislerde 4 haneli numara kullanilir. Tartimsiz islemler de islem no aldigi icin idare isterse sadece tartimli fislere ozel ayri sayac yapilabilir.

### 22.4 PDF

PDF disari aktarim `Microsoft Print to PDF` yazicisini kullanir. Gercek bilgisayarda bu yazici devre disiysa Windows ozelliklerinden acilmasi gerekebilir.

### 22.5 SQL 2008 Express

Gercek eski kantar bilgisayarlarinda SQL Server 2008 Express olabilir. EF6 ve uygulama .NET 4.8 ile calisir; ancak schema scriptleri ve SQL uyumlulugu sahada dikkatle test edilmelidir.

### 22.6 Eski programlar

Gercek sahada eski kantar programi halen kullaniliyorsa yeni program kurulumu mesai disi ve yedek alinarak yapilmalidir.

## 23. Manuel Prova Listesi

Yeni oturumda veya yeni build sonrasinda su akislari elle denenmeli:

1. Admin login.
2. Memur login; Ayarlar gizli mi?
3. Tartimli giris ve ayni gun cikis.
4. Tartimli giris, cikis, ikinci tartim icin tekrar gelis.
5. Icerideyken ikinci tartim.
6. Tartimsiz giris ve tartimsiz cikis.
7. Tartimsiz giris, sonradan tartim, sonra ikinci tartim.
8. Muaf giris ve muaf cikis.
9. Bekleme ucreti icin manuel eski tarih/saat denemesi.
10. Firma guncelle.
11. Kayit duzelt / plaka duzelt.
12. Gunluk tahsilat listeleme.
13. PDF olarak disa aktar.
14. Kantar fisi goster.
15. Kantar fisi yazdir.
16. Iki bilgisayar ortak SQL liste yenileme.
17. COM port kilo okuma.

## 24. Yeni Oturumda Bana Projeyi Nasil Tanitirsin

Yeni Codex oturumunda su metni yazmak yeterli olur:

```text
Bu repo KantarPro gumruk kantar otomasyon projesi. Once docs/PROJE_DEVAM_REHBERI.md dosyasini oku. Sonra git status, derleme ve testleri calistir. Projenin is mantigini bu dosyaya gore devam ettirecegiz.
```

Sonra yeni istegi yazabilirsin.

## 25. Degistirme Yaparken Kurallar

- Once mevcut akisi oku.
- Is mantigini UI icine gommemeye calis.
- Kritik is kurali Application servisinde olsun.
- Her yeni davranisa test ekle.
- Veritabani degisikligi gerekiyorsa `EnsureDatabaseSchema()` ve EF mapping kontrol edilsin.
- Kullaniciya ait mevcut degisiklikleri geri alma.
- Derleme ve test almadan tamamlandi deme.
- Gercek sahaya gecmeden once iki bilgisayarli prova tekrar edilmeli.

## 26. Kisa Hafiza Ozeti

KantarPro artik su noktadadir:

- Calisan WPF masaustu uygulamasi var.
- SQL Server Express ile calisiyor.
- Iki bilgisayarli sunucu/istemci mimarisi test edildi.
- Gercek kantar indikatorunden COM5 ile kilo okuma test edildi.
- OKI ML5720 yazici icin ham metin kantar fisi basimi test edildi.
- Admin/Memur login eklendi.
- Gunluk tahsilat ve PDF disari aktarim eklendi.
- Dolu-bos, tartimsiz, muaf, bekleme, tahsilat ve fis akislari testlerle korunuyor.

Bu dosya yeni oturum icin ana baslangic noktasi olarak kabul edilmelidir.
