# Tartimsiz Ikinci Ziyarette Ilk Tartim Fisi Tasarimi

## Amac

Bir arac ilk ziyaretinde tartilip cikis yaptiktan sonra ikinci ziyaretinde tartilmadan
giris yapabilir. Bu durumda ust listedeki acik islem tartimsizdir; ancak plakaya ait
ilk tartim ve ilk kantar fisi halen `KantarDosyalari` kaydinda korunur.

Kullanici bu tartimsiz ikinci ziyaret satirinda `Makbuz Goster` veya `Makbuz Yazdir`
dediginde, sistem ilk ziyaretteki tartimin fisini ayni fis numarasiyla yeniden
gosterebilmeli veya yazdirabilmelidir.

## Kapsam

- Dolu-Bos Kantar Hareketleri listesindeki `Makbuz Goster` ve `Makbuz Yazdir`
  islemleri kapsanir.
- Mevcut tartimli kayitlarin davranisi degismez.
- Tartimsiz kayit yeni bir tartim veya yeni bir kantar fisi olusturmaz.
- Veritabanindaki islem, ucret ve tartim akislari degismez.

## Fis Kaynagi Cozumleme Kurali

Secili `VehicleMovementRow` icin fis kaynagi asagidaki sirayla belirlenir:

1. Secili islemin kendi tartimi varsa mevcut davranis korunur ve o tartim kullanilir.
2. Secili islem tartimsizsa plaka normalize edilerek ayni araca ait aktif
   `KarsiTartimBekleniyor` durumundaki `KantarDosyasi` aranir.
3. Bulunan dosyanin `IlkTartim` kaydi, ilk tartimin bagli oldugu asil `Islem` kaydi
   ve arac bilgileri birlikte okunur.
4. Onizleme/yazdirma satiri bu eski kayittan uretilir:
   - ilk ziyaretteki plaka ve firma,
   - ilk giris tarih ve saati,
   - ilk tartim agirligi,
   - ilk islemin islem numarasi,
   - ilk tartima daha once verilmis kantar fis numarasi.
5. Ilk tartimin fis numarasi yoksa mevcut merkezi fis numarasi uretimi ayni tartim
   kaydi icin bir kez calisir. Sonraki baskilar ayni numarayi kullanir.
6. Uygun ilk tartim bulunamazsa mevcut uyari korunur:
   `Bu kayitta kantar tartimi yok. Tartimsiz girisler icin kantar fisi olusturulmaz.`

## Belirsiz Kayit Guvenligi

Normal is akisinda bir plaka icin tek aktif `KarsiTartimBekleniyor` dosyasi bulunur.
Veri bozuklugu nedeniyle birden fazla aday bulunursa sistem en yeni kaydi sessizce
secmez. Kullaniciya birden fazla bekleyen tartim bulundugu bildirilir ve fis
olusturulmaz. Boylece yanlis araca veya yanlis ziyarete ait fis basilmaz.

## Teknik Yapi

Fis kaynagi secme mantigi tiklama olaylarinin icine dagitilmayacak, tek bir
cozumleyicide toplanacaktir. Cozumleyici:

- secili `VehicleMovementRow` bilgisini alir,
- once secili islemin tartimini kontrol eder,
- gerekirse bekleyen `KantarDosyasi.IlkTartim` kaydina geri doner,
- `KantarFisFormatter` ve `KantarFisPreviewData` tarafindan kullanilabilecek bir
  `VehicleMovementRow` fis modeli dondurur.

`Makbuz Goster` ve `Makbuz Yazdir` ayni cozumleyiciyi kullanir. Boylece ekranda
gosterilen fis ile yazicidan cikan fis ayni kaynaktan uretilir.

## Veri Degisikligi Kurallari

- Onizleme acmak tartim, islem veya ucret kaydi olusturmaz.
- Gecmis tartimin fis numarasi varsa degistirilmez.
- Fis numarasi yoksa numara sadece o mevcut tartim kaydina atanir.
- Tartimsiz ikinci ziyaretin `IslemNo`, `CikisNo`, ucretleri ve durumu degistirilmez.
- Yeniden baski yeni bir fis numarasi tuketmez.

## Ornek Akis

1. `16BKK767` ilk ziyarette `34.000 kg` tartilir ve `00001` numarali kantar fisi alir.
2. Arac cikis yapar ve ikinci tartim bekleyen duruma gecer.
3. Ayni plaka ikinci ziyaretinde `Tartmadan Kaydet` ile yeniden iceri alinir.
4. Ust listedeki yeni satirin kendi tartimi yoktur.
5. Bu satirda `Makbuz Goster` secilince sistem aktif bekleyen kantar dosyasini bulur.
6. Ekranda `34.000 kg`, ilk giris tarihi ve `00001` fis numarali ilk tartim fisi
   gosterilir.
7. Ayni fis tekrar yazdirilirsa numara yine `00001` olur.

## Testler

1. Secili islemin kendi tartimi varsa kendi fisi kullanilir.
2. Secili islem tartimsiz ve ayni plakada bekleyen ilk tartim varsa eski ilk tartim
   fisi gosterilir.
3. Geri donulen fis; eski fis numarasi, eski islem numarasi, ilk giris tarihi,
   firma ve agirligi dogru tasir.
4. Tekrar onizleme ve yazdirma yeni fis numarasi uretmez.
5. Tartimsiz islem icin bekleyen ilk tartim yoksa mevcut uyari gosterilir.
6. Birden fazla aktif bekleyen dosya varsa fis basilmaz ve acik bir uyari gosterilir.
7. Mevcut tek tartim ve dolu-bos fis testleri degismeden gecer.

## Kabul Kriterleri

- Tartimsiz ikinci ziyarette ilk tartim fisi ust listedeki satirdan yeniden
  goruntulenebilir ve yazdirilabilir.
- Fiste ilk ziyarete ait bilgiler ve ilk tartimin asil fis numarasi bulunur.
- Yeni fis numarasi veya yeni tartim kaydi olusmaz.
- Mevcut tartimli kayitlarin fis davranisi bozulmaz.
- Belirsiz veri durumunda sistem yanlis kaydi otomatik secmez.
