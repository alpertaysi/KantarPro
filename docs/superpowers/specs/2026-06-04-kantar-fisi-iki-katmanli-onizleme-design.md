# Kantar Fisi Iki Katmanli Onizleme Tasarimi

## Amac

Kantar fisi memur tarafindan ekranda rahat okunabilmeli, ancak OKI 5720 nokta vuruslu yaziciya giden gercek cikti sade, hizali ve surekli forma uygun metin olarak kalmalidir.

Bu nedenle kantar fisi iki katmanli tasarlanacaktir:

- Modern onizleme katmani: memurun ekranda gordugu okunakli pencere.
- Ham metin cikti katmani: OKI 5720'ye gidecek sabit genislikli metin.

## Yazici ve Surekli Form Karari

Kantar fisi OKI 5720 model nokta vuruslu yazicidan alinacaktir. Yaziciya surekli form takildigi icin cikti klasik A4 sayfa mantigiyla degil, satir satir ilerleyen form mantigiyla ele alinacaktir.

Ham metin cikti icin temel ilkeler:

- Monospace karakter duzeni kullanilir.
- Turkce karakterlerden kacinilir: `Fis`, `Giris`, `Tarti` gibi ASCII metinler kullanilir.
- Satir genisligi sabit tutulur.
- Fis sonunda imza alani bulunur.
- Fis sonunda formu gereksiz fazla ilerletmeyecek kontrollu bos satirlar kullanilir.
- Ileride ihtiyac olursa ayarlardan `Fis satir sayisi` veya `Fis sonu bos satir` degeri eklenebilir.

## Fis Tipleri

### Tek Tartim Fisi

Ilk tartimi olan araclar icin uretilir. Tartimsiz kayitlarda fis uretilmez.

Alanlar:

- Plaka
- Fis No
- Giris Tarihi
- Giris Saati
- 1. Tartim
- Memur Imza

Bu fisi cikis tarihi, cikis saati, mal cinsi, geldigi yer, gittigi yer, 2. tartim ve net bilgisi icermeyecektir.

### Dolu-Bos Fisi

Iki tartimi tamamlanmis araclar icin uretilir.

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

### Tartimsiz Kayitlar

Tartimsiz girip tartimsiz cikan araclar icin kantar fisi uretilmez.

Tartimsiz girip sonradan tartilan arac icin tek tartim fisi alinabilir.

## Modern Onizleme Penceresi

`Makbuz Goster` veya `Kantar Fisi Goster` secildiginde modern onizleme penceresi acilir.

Pencere yapisi:

- Ust baslik: `Kantar Fisi Onizleme`
- Fis tipi bilgisi: `Tek Tartim` veya `Dolu-Bos`
- Orta bolum: okunur kart gorunumu
- Alt butonlar:
  - `Yazdir`
  - `Ham Metni Goster`
  - `Kapat`

Modern onizleme sadece ekranda okunurluk icindir. Yaziciya bu modern kart gonderilmez.

## Ham Metin Gosterimi

`Ham Metni Goster` secildiginde OKI 5720'ye gidecek gercek metin goruntulenir.

Bu metin mevcut `KantarFisFormatter` tarafindan uretilen metindir ve yaziciya gonderilecek icerikle ayni olmalidir.

## Yazdirma Akisi

Yazdirma sonraki uygulama adiminda baglanacaktir.

Planlanan akisi:

1. Fis verisi secili satirdan uretilir.
2. Modern onizleme penceresi acilir.
3. Memur `Yazdir` der.
4. Ham metin OKI 5720 yazicisina gonderilir.
5. Yazdirma basariliysa ilgili tartim kaydinda `FisYazdirildiMi` isaretlenebilir.

## Hata Kurallari

- Secili satir yoksa uyari verilir.
- Tartimsiz kayitta fis istenirse uyari verilir.
- 2. tartimi olmayan kayitta dolu-bos fisi uretilmez; tek tartim fisi uretilir.
- Yazici bagli degilse veya cikti alinamazsa modern uyari penceresi gosterilir.

## Test Beklentileri

- Tek tartim fisi yalniz tek tartim alanlarini icerir.
- Dolu-bos fisi iki tartim ve net bilgilerini icerir.
- Tartimsiz kayit fis uretmez.
- Modern onizleme ham metinden bagimsiz gorunur ama ayni veri kaynagindan beslenir.
- Ham metin surekli form icin sabit genislikli ve ASCII karakterlidir.
