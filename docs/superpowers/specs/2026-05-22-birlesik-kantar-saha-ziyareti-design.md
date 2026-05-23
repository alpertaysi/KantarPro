# Birlesik Kantar ve Saha Ziyareti Tasarimi

## Amac

Bu tasarim, mevcut kantar programinin hizli tartim akisiyla Gumruk Tir Kontrol
programinin saha giris-cikis ve ucret tahsilat mantigini tek yeni programda
birlestirir. Yeni program tek yeni veritabani kullanir. Eski programlar ve
veritabanlari referans olarak kalir; ilk surumde surekli entegrasyon veya veri
aktarimi hedeflenmez.

Ilk kullanilabilir surumde oncelik veri modeli ve islem kurallarinin dogru
calismasidir. Arayuz bu kurallari saha senaryolariyla denemeye yetecek kadar
sade ve anlasilir kalir.

## Temel Kararlar

- Saha giris-cikis ve kantar tartim isi ana ekranda esit agirlikta ele alinir.
- Sahaya giren her arac icin bir saha ziyareti acilir.
- Ayni plakanin iki saha ziyareti tek dolu-bos kantar dosyasini
  tamamlayabilir.
- Her saha ziyareti kendi ucretlerini ve kendi tahsilatini tasir.
- Tahsil edilen eski ziyaret tutarlari yeni ziyaret toplamlarina eklenmez.
- Kantar tartim sirasi sabit degildir. Ilk tartim dolu veya bos olabilir.
- Program gelis turunu plaka gecmisine gore onerir; memur gerekirse degistirir.
- Ilk asamada resmi fatura, e-fatura ve makbuz tasarimi kapsam disidir.

## Referanslardan Alinan Dersler

### Kantar Programi

Kantar programi aktif giris kayitlarini ve cikis kayitlarini ayri tutar.
Aktif kayit ilk tartimi, cikis kaydi ikinci tartimi ve net agirligi tasir.
Operator ana ekranda once plaka, tartim ve cikis durumunu hizli okumak ister.

### Gumruk Tir Kontrol

Gumruk Tir Kontrol saha girisi, saha cikisi, kalinan gun, giris-cikis ucreti,
tartim ucreti ve isgaliye ucretini arac ziyareti etrafinda toplar. Ucret
kalemlerini cikis aninda gorunur yapar.

### Birlesik Program

Yeni program bu iki yaklasimi tek ana kayda zorlamaz. Saha ziyareti ile kantar
dosyasini ayirir ve ayni plaka uzerinden baglar.

## Veri Modeli

### Saha Ziyareti

Arac sahaya her geldiginde yeni bir saha ziyareti acilir.

Saha ziyareti sunlari tasir:

- Plaka
- Firma
- Gelis turu: `Dolu`, `Bos`, `Tartimsiz`
- Giris tarih ve saati
- Cikis tarih ve saati
- Bekleme gunu
- Bu ziyarete ait ucret kalemleri
- Bu ziyarete ait tahsilat kayitlari
- Durum: sahada, cikti, tahsil edildi gibi ziyaret odakli durumlar

Bir saha ziyareti tartim icermeyebilir. Tartim yapildiysa o tartim ziyaretle
baglanir.

### Kantar Dosyasi

Kantar dosyasi iki tartimdan olusan dolu-bos tartim isini tasir.

Kantar dosyasi sunlari tasir:

- Plaka
- Ilk tartimin bagli oldugu saha ziyareti
- Ilk tartim turu: `Dolu` veya `Bos`
- Ilk tartim tarih, saat ve kilo bilgisi
- Karsi tartimin bagli oldugu saha ziyareti
- Karsi tartim turu
- Karsi tartim tarih, saat ve kilo bilgisi
- Net agirlik
- Durum: `Karsi Tartim Bekleniyor`, `Tamamlandi`, `Suresi Doldu`

Bir kantar dosyasi tek saha ziyaretinde iki tartimla tamamlanabilir. Bir kantar
dosyasi iki farkli tarihteki iki saha ziyaretiyle de tamamlanabilir.

### Ucret ve Tahsilat

Ucret kantar dosyasina degil saha ziyaretine yazilir.

- Her saha ziyaretinde giris-cikis ucreti olusur.
- O ziyarette tartim yapildiysa tartim ucreti olusur.
- Ziyaret gece `00.00` sonrasina kaldiysa bekleme ucreti olusur.
- Cikista yalniz o ziyarete ait ucretler tahsil edilir.
- Her tahsilatin benzersiz bir Tahsilat ID degeri olur.

## Is Akisi

### Plaka ile Baslama

Memur plakayi girer. Program plakanin mevcut durumunu kontrol eder.

- Acik saha ziyareti yoksa yeni ziyaret acilabilir.
- Acik saha ziyareti varsa yeni giris engellenir.
- Karsi tartim bekleyen kantar dosyasi varsa program karsi gelis turunu onerir.
- Bekleyen kantar dosyasi yoksa program ilk dolu veya bos gelis icin oneride
  bulunur.
- Memur gelis turunu sahadaki gercek duruma gore degistirebilir.

### Giris Kaydi

Memur giriste su islemlerden birini yapar:

- `Tart ve Kaydet`
- `Tartmadan Kaydet`
- Kayit sonrasi islem menusunden `Tartim Ekle`

Tartimsiz giriste yalniz giris-cikis ucreti olusur. Tartimli giriste
giris-cikis ve tartim ucreti olusur. Sonradan tartim eklendiginde mevcut
ziyarete yalniz tartim ucreti eklenir.

### Ilk Tartim Sonrasi Cikis

Ilk tartim yapildiysa kantar dosyasi acilir. Arac o gun cikabilir. Cikista o
ziyaretin ucretleri tahsil edilir. Karsi tartim tamamlanmadiysa kantar dosyasi
`Karsi Tartim Bekleniyor` durumunda kalir.

### Gunler Sonra Karsi Tartim

Ayni plaka gunler sonra tekrar sahaya geldiginde yeni saha ziyareti acilir.
Program bekleyen kantar dosyasini bulur ve tartimi o dosyaya baglamayi onerir.
Karsi tartim alindiginda net agirlik hesaplanir. Ikinci ziyaret icin yeni
giris-cikis ve tartim ucreti olusur. Cikista yalniz ikinci ziyaretin ucretleri
tahsil edilir.

### Tek Ziyarette Iki Tartim

Arac ayni saha ziyaretinde giris ve cikis tartimini tamamlarsa kantar dosyasi
tek ziyarette tamamlanir. O ziyarette giris-cikis, iki tartim ve varsa bekleme
ucretleri tahsil edilir.

### Tartimsiz Ziyaret

Tartimsiz ziyaret kantar dosyasi acmaz. Arac cikarken giris-cikis ve varsa
bekleme ucretleri tahsil edilir.

### Sure Dolumu

Karsi tartim bekleyen kantar dosyasi ayarlanan sure sonunda `Suresi Doldu`
durumuna gecer. Ilk tartim, ziyaretler ve tahsilatlar silinmez. Suresi dolan
dosya yeni otomatik karsi tartim eslestirmesinde onerilmez.

## Ana Ekran

Ana ekran operasyon masasi duzeninde olur.

### Sol Taraf

Sol taraf hizli islem panelidir:

- Plaka
- Firma
- Programin onerdigi gelis turu
- Memurun degistirebildigi gelis turu secimi
- Giris tarih ve saati
- Tartim alani
- `Tart ve Kaydet`
- `Tartmadan Kaydet`
- `Cikis Yap`
- `Tahsilat Al`

Secili plaka icin sol detay bolumunde su bilgiler gorulur:

- Acik saha ziyareti
- Acik kantar dosyasi
- Ilk ve karsi tartim durumu
- Bu ziyaretin ucret kalemleri
- Tahsilat gecmisinin varligi

### Sag Taraf

Sag taraf takip alanidir.

Aktif listeler:

- `Dolu Gelenler`
- `Bos Gelenler`
- `Tartimsiz Girisler`

Cikis listesi:

- `Kesin Cikislar`

Listelerde ana ekran icin gerekli minimum alanlar gorulur:

- Durum
- Plaka
- Firma
- Giris tarih ve saati
- Tartim durumu
- Bu ziyaretin odeme toplami

Kesin cikis listesi saha cikislarini gosterir. Kantar isi bitmemisse satir
durumu bunu belirtir. Ilk tartimini yapip cikmis arac ayni zamanda ilk gelis
turune uygun aktif kantar listesinde `Karsi Tartim Bekleniyor` olarak
gorunmeye devam eder.

## Kurallar ve Uyarilar

- Acik saha ziyareti olan plaka icin yeni giris acilmaz.
- Iki tartimi tamamlanmis kantar dosyasina ucuncu tartim eklenmez.
- Karsi tartim turu ilk tartima gore onerilir.
- Birden fazla bekleyen kantar dosyasi varsa program otomatik baglama yapmaz.
- Cikis islemi acik saha ziyareti olmadan yapilmaz.
- Cikis penceresi tahsil edilmemis ziyaret ucretlerini kalem kalem gosterir.
- Bekleme ucreti ayni gun cikista olusmaz.
- Bekleme ucreti takvim gunu `00.00` gecisine gore hesaplanir.
- Tum tahsilatlar plaka gecmisinde Tahsilat ID ile bulunabilir.

## Ilk Surum Kapsami

### Dahil

- Saha ziyareti acma ve cikis yapma
- Dolu, bos ve tartimsiz gelis turleri
- Plaka gecmisine gore gelis turu onerisi
- Tartimli kayit
- Tartimsiz kayit
- Sonradan tartim ekleme
- Tek ziyarette iki tartimi tamamlama
- Gunler sonra karsi tartimi tamamlama
- Net agirlik hesaplama
- Giris-cikis, tartim ve bekleme ucreti
- Ziyaret bazli tahsilat
- Tahsilat ID
- Dolu gelenler, bos gelenler, tartimsiz girisler ve kesin cikis listeleri
- Plaka icin temel ziyaret, tartim ve tahsilat detayi

### Kapsam Disi

- Resmi fatura
- E-fatura
- Makbuz tasarimi ve yazdirma
- Eski iki veritabanindan otomatik aktarim
- Surekli eski sistem entegrasyonu
- Genis rapor ekranlari
- Son arayuz cilasi

## Dogrulama Senaryolari

Ilk uygulama plani ve testler en az su saha senaryolarini kapsar:

1. Dolu gelip tartilan, ayni gun cikis ve tahsilat yapan, gunler sonra bos
   karsi tartima gelen arac.
2. Ayni saha ziyaretinde iki tartimi tamamlayip net hesaplayan arac.
3. Tartimsiz girip tartimsiz cikan arac.
4. Tartimsiz girip sonradan tartim isteyen arac.
5. Ilk tartimdan sonra karsi tartima gelmeyip suresi dolan kantar dosyasi.
6. Once bos, sonra dolu tartilan arac.
7. Her saha ziyaretinde ayri tahsilat olusmasi.
8. Eski tahsilatin yeni ziyaret toplam tutarina eklenmemesi.
9. Iki tartimi tamamlanan dosyaya ucuncu tartim eklenememesi.
10. Bekleme ucretinin gun degisiminde dogru hesaplanmasi.

## Sonuc

Bu tasarim iki farkli soruyu iki ayri kayit turuyla cevaplar:

- `Saha ziyareti`: Arac ne zaman geldi, ne zaman cikti, bu ziyarette hangi
  ucretler tahsil edildi?
- `Kantar dosyasi`: Bu plakanin dolu-bos tartim isi hangi iki tartimla
  tamamlandi ve net agirlik nedir?

Bu ayrim programin iki eski sistemi birlestirmesini saglarken ayni plaka icin
gunlere yayilan tartim ve tahsilat senaryolarini karistirmadan tutar.
