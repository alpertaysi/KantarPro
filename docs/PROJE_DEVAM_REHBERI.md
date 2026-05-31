# KantarPro Proje Devam Rehberi

Son guncelleme tarihi: 31.05.2026

Bu dosya, proje baska bir Codex oturumunda veya baska bir yapay zeka aracinda devam ettirilecekse okunmasi gereken ana hafiza dosyasidir. Amac, sohbet gecmisi kaybolsa bile is mantigi, ekran akislari, teknik yapi, kararlar, yarim kalan noktalar ve test/derleme komutlarinin tek yerden anlasilmasidir.

## 1. Projenin Amaci

Bu proje, Bursa Tasfiye Isletme Mudurlugu sahasinda kullanilacak gumruk kantar otomasyon programidir. Hedef, mevcut iki ayrik ihtiyaci tek programda birlestirmektir:

- Saha giris-cikis takibi.
- Dolu-bos kantar tartim sureci.
- Tahakkuk/tahsilat takibi.
- Gunluk hasilat dokumu.
- Kantar fisi/makbuz onizleme ve yazdirma.
- Ucretten muaf resmi/istisnai araclarin takip edilmesi.

Program, mevcut aktif kantar programindaki ana operasyon mantigini korumaya calisir ancak daha sade, daha takip edilebilir ve ileriye donuk bakimi kolay bir kod yapisina tasinir.

## 2. Calisma Klasoru

Ana proje klasoru:

```text
C:\Users\DELL\OneDrive\Masaüstü\KantarPro_Tasima_Paketi\KantarPro_Tasima_Paketi\project\Codex Kantar
```

Solution dosyasi:

```text
KantarPro.sln
```

Calisan exe derleme cikisi:

```text
src\KantarPro.Desktop\bin\Debug\KantarPro.Desktop.exe
```

## 3. Teknoloji Yigini

- Dil: C#
- Arayuz: WPF
- Framework: .NET Framework 4.8
- ORM: Entity Framework 6
- Veritabani: SQL Server Express
- Test: MSTest / Visual Studio Test Platform
- Mimari hedef: Domain, Application, Infrastructure, Desktop katmanlari ayrilmis sekilde ilerlemek.

## 4. Proje Katmanlari

### 4.1 Domain

Klasor:

```text
src\KantarPro.Domain
```

Icerik:

- Entity siniflari.
- Sabitler.
- Islem, arac, tartim, ucret, kantar dosyasi gibi temel veri nesneleri.

Onemli dosyalar:

- `Entities\Arac.cs`
- `Entities\Islem.cs`
- `Entities\Tartim.cs`
- `Entities\IslemUcreti.cs`
- `Entities\KantarDosyasi.cs`
- `KantarSabitleri.cs`

### 4.2 Application

Klasor:

```text
src\KantarPro.Application
```

Icerik:

- Is kurallari.
- Saha ziyareti ve cikis mantigi.
- Ucret ayarlari.
- Formatlama yardimcilari.

Onemli servisler:

- `Services\SahaZiyaretiServisi.cs`
- `Services\IslemServisi.cs` eski/onceki akistan kalan servis; bazi testler halen bunu kapsar.
- `Services\UcretAyarlariServisi.cs`
- `Services\KantarDisplayFormatter.cs`

### 4.3 Infrastructure

Klasor:

```text
src\KantarPro.Infrastructure
```

Icerik:

- EF6 `DbContext`
- Mapping
- Repository/UnitOfWork

Onemli dosyalar:

- `Data\KantarDbContext.cs`
- `Data\KantarUnitOfWork.cs`

### 4.4 Desktop

Klasor:

```text
src\KantarPro.Desktop
```

Icerik:

- WPF ekranlari.
- Ana ekran.
- Kantar fisi onizleme.
- Odeme turu secimi.
- Ucretten muafiyet formu.
- Dolu-bos ikinci tartim formu.
- Dashboard satir olusturucular.

Onemli dosyalar:

- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- `MainWindow.DashboardData.cs`
- `MainWindow.DataAndFormatting.cs`
- `MainWindow.DoluBosAndFilters.cs`
- `DashboardRowBuilder.cs`
- `DashboardVisitInfo.cs`
- `DashboardRows.cs`
- `KantarFisFormatter.cs`
- `KantarFisPreviewWindow.xaml`
- `DoluBosSecondWeighingWindow.xaml`
- `PaymentTypeChoiceWindow.xaml`
- `ExemptionReasonWindow.xaml`
- `FirmaUpdateWindow.xaml`

## 5. Veritabani ve Eski Sistem Kaynaklari

Kullanici tarafindan bildirilen eski/aktif sistem kaynaklari:

```text
C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup\GumrukTirKontrol_24012024_2028.bak
C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup\ForWin1_15052026_000000.bak
C:\Users\DELL\OneDrive\Masaüstü\GumrukTirKontrol_Programi
C:\Users\DELL\OneDrive\Masaüstü\Fatura Prog
C:\Users\DELL\OneDrive\Masaüstü\Kantar Ekran Görüntüleri
```

Bu yedekler ve ekran goruntuleri yeni programin is akisini anlamak icin referans olarak kullanildi.

## 6. Ana Is Kavramlari

### 6.1 Arac

Plaka ve firma bilgisi ile temsil edilir. Firma bazen giriste unutulabilir; bunun icin sag tik menusunde `Firma Guncelle` eklendi.

### 6.2 Islem / Saha Ziyareti

Bir aracin sahaya girisinden cikisina kadar olan ziyarettir.

Durumlar:

- `Iceride`: Arac sahada.
- `CikisYapti`: Arac cikis yapti.

Her giris icin bir islem kaydi olusur. Tartimli, tartimsiz veya muaf olabilir.

### 6.3 Tartim

Bir aracin kantar uzerinde tartilmasidir.

Tartim tipleri:

- Giris tartimi.
- Sonradan tartim.
- Cikis tartimi.

Uygulamada dolu-bos mantigi icin asil onemli olan agirlik ve yuk durumudur:

- Dolu
- Bos

### 6.4 Kantar Dosyasi

Dolu-bos tartim eslestirme dosyasidir.

Mantik:

- Ilk tartim yapilinca bir `KantarDosyasi` acilir.
- Karsi tartim yapilinca dosya tamamlanir.
- Net agirlik iki tartim arasindaki mutlak farktir.
- Ilk tartimdan sonra arac cikabilir ve ikinci tartim icin daha sonra gelebilir.
- 10 gun icinde ikinci tartim gelmezse dosya suresi doldu olarak kapatilabilir.

Durumlar:

- `KarsiTartimBekleniyor`
- `Tamamlandi`
- `SuresiDoldu`

### 6.5 Ucretler

Ucret kalemleri:

- Giris-cikis ucreti.
- Tartim ucreti.
- Bekleme/isgaliye ucreti.

Bekleme ucreti gece 00:00 sonrasi gun farkina gore hesaplanir. Test icin cikis tarihi ve saati su an manuel degistirilebilir. Program nihai hale geldiginde bu manuel alan kaldirilabilir.

### 6.6 Tahsilat

Cikis yapilirken o isleme ait tahsil edilmemis ucretler tahsil edilir.

Odeme turu:

- Nakit
- Kredi Karti

Cikis onayinda odeme turu sorulur. Muaf araclarda odeme turu sorulmaz.

Tahsilat numarasi surekli artar, gunluk sifirlanmaz.

### 6.7 Muafiyet

Bazi resmi/istisnai araclar ucretten muaftir. Ornek: polis veya kamu kurumu tarafindan kacirilan/emanet esya getirilmesi.

Muaf kayit:

- Ilk kayitta `Muaf` secenegi ile yapilabilir.
- Muafiyet nedeni zorunludur.
- Muaf araclar genellikle tartilir ama ucret tahakkuk etmez.
- Muafiyet nedeni gunluk hasilatta firma alaninda gosterilecek sekilde tasarlanmistir.
- Muaf arac cikisinda odeme turu sorulmaz.

## 7. Ana Ekran Mantigi

Ana ekran tek operasyon ekrani olarak tasarlanmistir.

Bolumler:

- Sol ust: arac bilgileri ve kayit alani.
- Sag ust: `Dolu-Bos Kantar Hareketleri` tablosu.
- Alt sekmeler:
  - `2. Tartim Bekleyenler`
  - `Kesin Cikis Yapanlar`
  - `Son Islem`
- Ust sekmeler:
  - `Giris-Cikis Islemleri`
  - `Gunluk Hasilat`
  - `Ayarlar`

## 8. Kayit Butonu Akisi

Sol ustte tek `Kaydet` butonu vardir.

`Kaydet` basildiginda secenek penceresi acilir:

- Tart ve Kaydet
- Tartmadan Kaydet
- Muaf

### 8.1 Tart ve Kaydet

Duruma gore:

- Plaka icin acik islem yoksa yeni giris acilir.
- Plaka icin bekleyen dolu-bos dosyasi varsa ikinci tartim formu acilir.
- Plaka zaten icerideyse yeni giris yapilmaz; kullaniciya listeden sag tik `Tart` veya `Cikis Yap` kullanmasi soylenir.

### 8.2 Tartmadan Kaydet

Yeni arac tartimsiz girer.

Sonradan sag tik `Tart` ile tartim eklenirse:

- Arac icin ilk tartim olusur.
- Durum karsi tartim bekler hale gelir.
- Bu arac cikinca hemen kesin cikisa atilmaz; ikinci tartimi bekleyebilir.

### 8.3 Muaf

Muafiyet nedeni zorunlu form acilir.

Muaf arac:

- Ucret tahakkuk etmez.
- Cikis sirasinda odeme turu sorulmaz.
- Dolu-bos tartimi varsa normal tartim mantigi isler ancak ucret uretilmez.

## 9. Sag Tik Menuleri

### 9.1 Dolu-Bos Kantar Hareketleri

Sag tik secenekleri:

- Tart
- Kayit Duzelt
- Firma Guncelle
- Ucretten Muaf
- Makbuz Yazdir
- Makbuz Goster

`Tart`:

- Acik islemde tek tartim varsa dolu-bos ikinci tartim formunu acar.
- Kilo alindiktan sonra net hesaplanir.
- Dolu-bos tamamlandiysa tekrar tartima izin verilmez.

`Kayit Duzelt`:

- Plaka ve firma gibi temel bilgiler duzeltilebilir.

`Firma Guncelle`:

- Sadece firma bilgisini sonradan girme/duzeltme icindir.

`Ucretten Muaf`:

- Acik islem sonradan muaf hale getirilebilir.

`Makbuz Yazdir/Goster`:

- Kantar fisi onizleme/yazdirma akisini acar.

### 9.2 2. Tartim Bekleyenler

Sag tik secenekleri:

- Kantar Fisi Yazdir
- Kantar Fisi Goster

Bu listede sadece ilk tartim oldugu icin tek tartim fisi uretilir.

### 9.3 Kesin Cikis Yapanlar

Sag tik secenekleri:

- Kantar Fisi Yazdir
- Kantar Fisi Goster

Kayit:

- Tek tartimliysa tek tartim fisi.
- Dolu-bos tamamlandiysa dolu-bos fisi.
- Tartimsiz ciktiysa fisi yok uyarisi.

## 10. Temel Senaryolar

### Senaryo 1: En yaygin akisi

1. Arac dolu gelir.
2. Tartilir.
3. Giris-cikis + dolu kantar ucreti tahakkuk eder.
4. Ayni gun cikarsa bekleme ucreti yoktur.
5. Cikis yaparken odemesini yapar.
6. Arac daha sonra bos/ikinci tartim icin gelir.
7. Sistem eski ilk tartimi bulur.
8. Dolu-bos formu acilir.
9. Ikinci tartim alinir.
10. Net agirlik hesaplanir.
11. Bu ikinci gelis icin yeni giris-cikis + tartim ucreti tahakkuk eder.
12. Cikis yapinca kesin cikisa alinir.

### Senaryo 2: Arac cikarken ikinci tartimi da yapmak ister

1. Arac dolu gelir ve tartilir.
2. Ayni gun veya sonra cikarken ikinci tartimi yapmak ister.
3. Sag tik `Tart` ile dolu-bos formu acilir.
4. Ikinci tartim alinir.
5. Net hesaplanir.
6. Cikis yapilir.
7. Odeme alinir.
8. Dolu-bos tamamlandigi icin kesin cikis listesine gider.

### Senaryo 3: Arac tartilmak istemeden girer

1. Arac gelir ama tartilmaz.
2. Tartmadan kaydedilir.
3. Sadece giris-cikis ucreti tahakkuk eder.
4. Cikis yaparsa tartimsiz kesin cikis olabilir.
5. Kantar fisi uretilmez.

### Senaryo 4: Tartimsiz giren arac sonradan tartilmak ister

1. Arac tartimsiz girer.
2. Daha sonra tartilmak ister.
3. Sag tik `Tart` ile tartim eklenir.
4. Tartim ucreti eklenir.
5. Bu artik ilk tartim sayilir.
6. Karsi tartim bekler.
7. Cikis yaparsa ikinci tartim bekleyenler mantigina gore takip edilir.

### Senaryo 5: Ilk tartimdan sonra ikinci tartima gelmez

1. Arac ilk tartimini yapar.
2. Cikis yapar ve odemesini yapar.
3. Ikinci tartim icin bekleyenler listesine duser.
4. 10 gun icinde ikinci tartima gelmezse suresi dolan kantar dosyasi kapatilabilir.
5. Ilk tartim kaydi sistemde kalir; dolu-bos tamamlanmamis olur.

### Senaryo 6: Muaf arac

1. Arac gelir.
2. Muaf secilir.
3. Muafiyet nedeni yazilir.
4. Tartim varsa tartim kaydi tutulur.
5. Ucret tahakkuk etmez.
6. Cikis yaparken odeme turu sorulmaz.

## 11. Cikis Akisi

`Cikis Yap` butonu:

1. Secili plaka veya girilen plaka icin acik islem bulunur.
2. Cikis tarihi ve saati okunur.
3. Bekleme ucreti hesaplanir.
4. Odeme ozeti gosterilir.
5. Muaf degilse odeme turu sorulur:
   - Nakit
   - Kredi Karti
6. Cikis tamamlanir.
7. Tahsilat no ve tahsilat bilgileri yazilir.
8. Liste yenilenir.

Not: Test icin cikis tarihi/saatini manuel degistirme ozelligi var. Nihai surumde bu alan kaldirilabilir.

## 12. Gunluk Hasilat

Gunluk hasilat sekmesi cikis/tahsilat tarihine gore listeleme yapar.

Alanlar:

- Sira
- Islem No / Tahsilat No
- Odeme Turu
- Firma
- Plaka
- Cikis Tarihi
- Cikis Saati
- 1. Tartim
- 2. Tartim
- Net
- Giris Ucreti
- Tartim Ucreti
- Isgaliye/Bekleme Ucreti
- Toplam Ucret

Arama:

- Plaka filtresi.
- Firma filtresi.

Gunluk hasilat sadece kesin cikislari degil, odemesi alinan cikis islemlerini listeler.

## 13. Kantar Fisi Mantigi

Kantar fisi su an OKI 5720 icin metin tabanli form olarak uretilir. Onizleme penceresi vardir.

Formatter:

```text
src\KantarPro.Desktop\KantarFisFormatter.cs
```

Onizleme:

```text
src\KantarPro.Desktop\KantarFisPreviewWindow.xaml
```

### 13.1 Tek tartim fisi

Ilk tartimi olan ama dolu-bos tamamlanmamis araclar icin.

Alanlar:

- Plaka
- Fis No
- Giris Tarihi
- Giris Saati
- 1. Tarti
- Memur Imza

### 13.2 Dolu-bos fisi

Iki tartimi tamamlanmis araclar icin.

Alanlar:

- Plaka
- Fis No
- 1. Giris Tarihi
- 1. Giris Saati
- 2. Giris Tarihi
- 2. Giris Saati
- 1. Tartim
- 2. Tartim
- Net
- Memur Imza

### 13.3 Tartimsiz araclar

Tartimsiz giren ve tartilmeden cikan araclar icin kantar fisi olusturulmaz.

Tartimsiz girip sonradan tartilan arac icin tek tartim fisi alinabilir.

### 13.4 Fis No Karari

Su an fis no olarak `IslemNo` kullaniliyor.

Kullanici idareye soracak:

- Fis No = Islem No olarak mi kalsin?
- Yoksa sadece kantar fisi basilan islemler icin ayri `KantarFisNo` mu uretilsin?

Bu karar henuz kesinlesmedi. Ileride idareden cevap gelince uygulanacak.

## 14. Bilinen Kararlar ve Gerekceler

- Tek liste fikrinden vazgecildi. Cikis yapan/tahsilati alinan ve ikinci tartim bekleyenler alt listede ayrildi.
- Kesin cikis yapanlar ayri listede tutuluyor.
- Dolu-bos hareketleri ana takip listesi olarak korunuyor.
- Tartimsiz araclara kantar fisi verilmez.
- Muaf araclarda odeme turu sorulmaz.
- Tahsilat numarasi gunluk sifirlanmaz, surekli artar.
- Cikis tarihi/saatini manuel degistirme sadece test amaclidir.
- Sag tik menuleri operasyonel islemler icin ana yoldur.

## 15. Kodda Yapilan Refaktorler

`MainWindow.xaml.cs` cok buyudugu icin bir kisim sorumluluklar ayrildi:

- `MainWindow.DashboardData.cs`: listeleri yukleme.
- `MainWindow.DataAndFormatting.cs`: detay metinleri, formatlama, ortak yardimcilar.
- `MainWindow.DoluBosAndFilters.cs`: filtre ve dolu-bos ekran yardimcilari.
- `DashboardRowBuilder.cs`: tablo satirlarini olusturma.
- `DashboardVisitInfo.cs`: ziyaret/tartim durum bilgileri.
- `DashboardFormat.cs`: para, tarih, tartim formatlari.
- `KantarFisFormatter.cs`: kantar fisi metni.

Ama `MainWindow.xaml.cs` halen buyuk. Ileri asamada daha da bolunebilir:

- Entry operations partial class.
- Exit operations partial class.
- Context menu handlers partial class.
- Receipt/printing partial class.

## 16. Testler

Test projesi:

```text
tests\KantarPro.Application.Tests
```

Son bilinen test sayisi:

```text
32
```

Onemli test dosyalari:

- `SahaZiyaretiServisiTests.cs`
- `IslemServisiTests.cs`
- `UcretAyarlariServisiTests.cs`
- `KantarDisplayFormatterTests.cs`
- `KantarFisFormatterTests.cs`

Testlerin kapsadigi ana konular:

- Tartimli giris.
- Tartimsiz giris.
- Dolu-bos eslestirme.
- Cikis ve bekleme ucreti.
- Tahsilat no.
- Odeme turu.
- Muafiyet.
- Kantar fisi formatlari.

## 17. Derleme ve Test Komutlari

Ana klasorde calistir:

```powershell
cd "C:\Users\DELL\OneDrive\Masaüstü\KantarPro_Tasima_Paketi\KantarPro_Tasima_Paketi\project\Codex Kantar"
```

Derleme:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' .\KantarPro.sln /t:Build /p:Configuration=Debug
```

Test:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe' .\tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll
```

Son dogrulama:

- Derleme: 0 hata, 0 uyari.
- Test: 32/32 gecti.

Not: Derleme sirasinda `KantarPro.Desktop.exe` aciksa dosya kilitlenebilir. Bu durumda program kapatilmali veya su komut calistirilmalidir:

```powershell
Get-Process KantarPro.Desktop -ErrorAction SilentlyContinue | Stop-Process -Force
```

## 18. Git Durumu

Son commitler:

```text
60cef33 feat: add exempt flow and scale ticket preview
1eb0413 feat: support fee-exempt weighings
be22de9 feat: record payment type on exit
f6633f2 refactor: build exit rows with dashboard builder
76351f8 refactor: build entry rows with dashboard builder
382a856 refactor: extract dashboard visit helpers
8bf9531 refactor: extract dashboard formatting helpers
81abad7 refactor: split dashboard row loading steps
```

Bu dosya yazilirken calisma agacinda commitlenmemis degisiklikler vardi. Bunlar son oturumdaki su isleri kapsar:

- Cikis tarihi/saatine gore bekleme kolonunun anlik yenilenmesi.
- Acik islem varken sol giris formundan ayni plakanin tekrar yeni kayit gibi islenmemesi.
- Firma guncelle penceresi.
- Kantar fisi formatinin tek tartim ve dolu-bos diye ayrilmasi.
- Fis No alanina `IslemNo` basilmaya baslanmasi.
- 2. Tartim Bekleyenler ve Kesin Cikis Yapanlar listelerine sag tik kantar fisi alma secenekleri.
- Kantar fisi testleri.

Baska oturuma gecmeden once tavsiye:

```powershell
git status --short
git add .
git commit -m "feat: improve receipt access and open-visit safeguards"
```

Push icin henuz remote olmadigi daha once gorulmustu. Remote eklenirse push yapilabilir.

## 19. Son Eklenen/Kritik Dosyalar

Son donemde eklenen veya onemli hale gelen dosyalar:

```text
src\KantarPro.Desktop\FirmaUpdateWindow.xaml
src\KantarPro.Desktop\FirmaUpdateWindow.xaml.cs
tests\KantarPro.Application.Tests\KantarFisFormatterTests.cs
docs\PROJE_DEVAM_REHBERI.md
```

## 20. Bilinen Hassas Noktalar

### 20.1 Kantar fisi numarasi

Su an `IslemNo` fis numarasi olarak kullaniliyor. Tartimsiz islemler de `IslemNo` aldigi icin fislerde numara atlamasi gibi gorunebilir. Idareye sorulacak.

Alternatif:

- `IslemNo`: tum islemler icin.
- `KantarFisNo`: sadece fis basilan tartimli islemler icin.

### 20.2 Manuel tarih/saat alanlari

Test icin giris ve cikis tarih/saatleri elle degistirilebiliyor.

Nihai surumde:

- Cikis tarihi/saat alanlari kaldirilabilir veya sadece yetkili kullaniciya acilabilir.
- Saat bilgisinin COM/kantar cihazindan veya sistem saatinden gelmesi netlestirilmeli.

### 20.3 Yazdirma

Kantar fisi su an onizleme metni uretir. Gercek OKI 5720 yazdirma baglantisi henuz tam entegre degil.

Ileride:

- Yazici secimi.
- Direkt yazdirma.
- Kopya sayisi.
- Yazdirildi bilgisi.
- Tekrar yazdirma logu.

eklenebilir.

### 20.4 SQL Server versiyonu

README eski SQL Server 2008 Express uyumundan bahsediyor. Kullanici bilgisayarinda SQL Server 2022 Express pathleri de goruldu. Connection string ve script uyumlulugu kontrol edilmeli.

### 20.5 MainWindow buyuklugu

Refaktor basladi ancak `MainWindow.xaml.cs` halen buyuk. Yeni buyuk ozellik eklenirken once ilgili sorumluluk ayri partial class veya servis haline getirilmeli.

## 21. Devam Ederken Oncelikli Kontrol Listesi

Yeni oturumda once su adimlar uygulanmali:

1. Bu dosyayi oku.
2. `git status --short` calistir.
3. Commitlenmemis degisiklikleri incele.
4. Derleme calistir.
5. Testleri calistir.
6. Uygulamayi acip temel senaryolari manuel dene.
7. Yeni istek gelirse once mevcut is mantigini bozmadan kucuk ve testli ilerle.

## 22. Manuel Test Senaryo Listesi

Yeni oturumda elle denenmesi iyi olacak akislari:

### 22.1 Tartimli giris ve ayni gun cikis

- Plaka gir.
- Tart ve Kaydet.
- Cikis Yap.
- Odeme turu sec.
- Kesin cikis veya ikinci tartim bekleyen mantigini kontrol et.

### 22.2 Ilk tartim, cikis, sonra ikinci tartim

- Arac tartimli girsin.
- Cikis yapsin.
- Alt listede ikinci tartim bekleyenlere dussun.
- Ayni plaka tekrar girilince dolu-bos formu acilsin.
- Kilo al, kaydet.
- Cikis yap.
- Kesin cikisa dussun.
- Kantar fisi dolu-bos formatinda olsun.

### 22.3 Icerideyken ikinci tartim

- Arac tartimli girsin.
- Cikis yapmadan sag tik Tart.
- Dolu-bos formu acilsin.
- Ikinci tartim kaydedilsin.
- Cikis yapinca kesin cikis olsun.

### 22.4 Tartimsiz giris

- Tartmadan Kaydet.
- Cikis Yap.
- Kantar fisi almaya calisinca uyarisi gelsin.

### 22.5 Tartimsiz girip sonradan tartim

- Tartmadan Kaydet.
- Sag tik Tart.
- Tek tartim fisi alinabilsin.
- Cikis sonrasi ikinci tartim bekleme mantigi dogru calissin.

### 22.6 Muaf arac

- Muaf sec.
- Neden yazmadan kaydetmeyi dene, izin vermemeli.
- Neden yazip kaydet.
- Cikis yap.
- Odeme turu sormamali.
- Gunluk hasilatta uygun gosterilmeli.

### 22.7 Bekleme ucreti

- Giris tarihi eski tarih yap.
- Cikis tarihi daha ileri tarih yap.
- Bekleme ve toplam kolonlari anlik degissin.
- Cikis yapinca ayni tutar tahsil edilsin.

### 22.8 Firma guncelle

- Firma bos kayit yap.
- Sag tik Firma Guncelle.
- Firma yaz.
- Liste ve veritabani guncellensin.

### 22.9 Ayni plaka icerideyken tekrar kayit

- Plaka giris yapsin.
- Cikis yapmadan sol formdan ayni plakayi tekrar kaydetmeyi dene.
- Yeni kayit acmamali.
- Uyari vermeli.

## 23. Uygulama Icindeki Onemli Ekranlar

### 23.1 Giris-Cikis Islemleri

Ana operasyon ekranidir.

Kullanici gunluk olarak en cok bu ekranda calisir.

### 23.2 Gunluk Hasilat

Idare/muhasebe icin tahsilat dokumudur.

Tarih araligina gore cikis/tahsilat kayitlarini listeler.

### 23.3 Ayarlar

Ucretleri guncellemek icindir:

- Giris-cikis ucreti.
- Tartim ucreti.
- Bekleme ucreti.

Yeni fiyatlar sonraki tahsilatlarda kullanilir.

## 24. Kodlama Ilkeleri

Projeye devam ederken:

- Is kurali mumkunse `Application` katmaninda olmali.
- WPF code-behind sadece ekran baglama ve kullanici etkilesimi icin kullanilmali.
- Yeni is mantigi testle desteklenmeli.
- MainWindow daha fazla sismezse iyi olur; partial class veya servis cikarmak tercih edilmeli.
- Mevcut testler bozulmadan ilerlenmeli.
- Veritabani degisikligi gerekiyorsa migration/script dokumani eklenmeli.

## 25. Kisa Ozet

Bu proje su anda calisan bir WPF prototipinden, sahada kullanilabilecek daha ciddi bir kantar otomasyonuna evriliyor.

En kritik is mantigi:

- Arac sahaya girer.
- Tartimli/tartimsiz/muaf olabilir.
- Ucretler tahakkuk eder.
- Cikis sirasinda tahsilat alinir.
- Dolu-bos tartimlar eslestirilir.
- Ikinci tartim bekleyenler ve kesin cikis yapanlar ayrilir.
- Gunluk hasilat tahsilata gore izlenir.
- Kantar fisi sadece tartimli kayitlara verilir.

Yeni oturumda bu dosya okunursa, sohbet gecmisine ihtiyac olmadan proje mantigi devam ettirilebilir.
