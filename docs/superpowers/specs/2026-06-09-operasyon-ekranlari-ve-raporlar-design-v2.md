# KantarPro Operasyon Ekranlari ve Raporlar Tasarimi

## Amac
Bu calisma, gercek saha kullaniminda memurun programi kapatmadan islem yapabilmesini, gecmis kayitlari arastirabilmesini, fis/rapor ciktilarini daha kontrollu alabilmesini ve sunum sirasinda admin kullanicisinin gecici manuel kilo girebilmesini saglar.

## Kapsam
- Makbuz butonu ve kantar fisi numarasi davranisi.
- Plaka, firma, giris tarihi ve cikis tarihi bazli gecmis arastirma ekrani ve cikti alma.
- Gunluk tahsilat icin admin XLSX disa aktarma.
- OKI dokum yazdirma onizleme akisi.
- Alt menu sadelestirme ve manuel yedekleme butonu.
- Program acikken kullanici degistirme ve ayni pencerede sifre degistirme.
- Sadece admin icin gecici manuel kilo girisi.

## Makbuz ve Kantar Fisi
Tartim yapilan her plaka icin, kantar fisi yazdirilsin veya yazdirilmasin, kantar fisi numarasi verilir. Bu numara yazdirma anina bagli degildir; tartim kaydi olustugunda garanti edilir ve veritabaninda saklanir.

Makbuz butonuna basildiginda secili satirin mevcut kantar fisi numarasi kullanilir. Kullaniciyla `Kantar fisi yazdirilsin mi?` sorusu paylasilir. Kullanici vazgecerse fis basilmaz ancak daha once verilen fis numarasi veritabaninda kalir.

Ayni islem icin tekrar fis yazdirilirsa yeni numara uretilmez. Mevcut `Tartim.KantarFisNo` degeri kullanilir. Boylece fiziksel fis ile sistem kaydi tekrar baskilarda da ayni kalir.

## Arastir Ekrani
Arastir butonu yeni bir pencere acar. Pencere plaka, firma, giris tarihi araligi ve cikis tarihi araligi alanlariyla gecmis kayitlari arar. Arama sadece aktif listelerle sinirli olmaz; veritabanindaki gecmis giris, cikis, tartim, tahsilat, odeme turu, fis no, kullanici ve durum bilgilerini getirir.

Ayni plaka farkli tarihlerde ve farkli firmalarla gelmisse her kayit ayri satir olarak gosterilir. Sonuclar tarih sirasina gore listelenir ve kullanici isterse cikti alabilir. Cikti icin OKI ham metin dokumu ve dosyaya aktarma secenekleri desteklenir.

## Gunluk Tahsilat Ciktilari
Admin kullanicisi gunluk tahsilati XLSX olarak disa aktarabilir. XLSX uretimi programi zorlamayacak sekilde hafif tutulur: bellekte basit tablo olusturulur, formul veya agir bicimlendirme yapilmaz. Mevcut PDF/OKI rapor akislari korunur.

OKI Dokum Yazdir butonuna basildiginda dogrudan yaziciya gitmek yerine baski onizleme acilir. Onizleme uzerinden `Yazdir` secilirse OKI yaziciya ham metin dokumu gonderilir.

## Alt Menu ve Yedekleme
Alt menude `Menu` butonu kaldirilir. `Makbuz`, `Arastir` ve `Yedekle` kalir.

Yedekle butonu otomatik yedekleme scriptinin yaptigi isi manuel baslatir. Otomatik script hafta sonu calismaz; ancak kullanici cumartesi veya pazar gunu bu butona basarsa manuel yedek alinir. Bu nedenle UI cagrisi scriptteki hafta sonu kontrolunu zorunlu olarak atlayabilmelidir.

## Kullanici Degistir
Ustteki `Sifre Degistir` butonu `Kullanici Degistir` olur. Program kapanmadan kucuk bir pencere acilir. Pencerede sistemdeki aktif kullanicilar combobox icinde listelenir. Kullanici secilir, sifre girilir ve oturum aktif kullaniciya gecer.

Ayni pencerede sifre degistirme alani da bulunur. Kullanici mevcut sifresini ve yeni sifresini girerek kendi sifresini degistirebilir. Admin de bu pencereden kendi kullanicisina gecebilir; kullanici yetkileri oturum degisince ana ekrana yansir.

## Admin Manuel Kilo
Gercek kullanimda kilo indikatorunden gelir. Ancak sunum ve egitim icin sadece admin kullanicisina gecici manuel kilo girisi acilir. Memur kullanicilari kilo textboxina elle deger giremez.

Admin modunda elle girilen kilo yine mevcut tartim kaynagi metodundan okunur. Boylece is akisi tek kalir; sadece veri kaynagi admin icin gecici olarak textbox olabilir.

## Veri ve Servis Etkisi
- Kantar fisi numarasi uretimi yazdirma yollarinda degil, tartim kaydi olustugunda garanti edilir.
- Yazdirma yollari mevcut numarayi kullanir; numara yoksa ayni garanti metodu ile tamamlar.
- Arastir ekrani dogrudan veritabanindan genis kapsamli salt-okunur veri ceker.
- XLSX disa aktarma yeni bir hafif exporter sinifiyla yapilir.
- Manuel yedekleme icin mevcut `tools/BackupKantarPro.ps1` scriptine hafta sonu kontrolunu atlayan parametre eklenir veya UI cagrisi ayri manuel modla yapilir.
- Kullanici degistirme mevcut `KullaniciServisi` dogrulama ve parola degistirme akislarini kullanir.

## Test Kabul Kriterleri
- Tartim yapilan her plakaya, fis basilsin veya basilmasin, 5 haneli kantar fisi numarasi yazilir.
- Makbuz butonunda kullanici vazgecse bile mevcut fis numarasi korunur.
- Ayni satira tekrar makbuz basilirse fis numarasi degismez.
- Arastir ekrani plaka, firma, giris tarihi araligi ve cikis tarihi araligi ile eski kayitlari getirir.
- Arastir sonuclari yazdirilabilir veya disa aktarilabilir.
- Admin XLSX disa aktarabilir; memur bu islemi yapamaz.
- OKI dokum once onizleme acar, yazdir denince ham metin yaziciya gonderilir.
- Alt menude Menu butonu yoktur; Makbuz, Arastir, Yedekle kalir.
- Yedekle butonu hafta sonu dahil manuel yedek alabilir.
- Kullanici degistir penceresi programi kapatmadan oturum degistirir.
- Ayni pencereden sifre degistirilebilir.
- Sadece admin manuel kilo girebilir; memur indikator kilosu olmadan tartimli kayit yapamaz.
