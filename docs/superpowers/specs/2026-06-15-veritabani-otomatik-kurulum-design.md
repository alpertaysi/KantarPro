# Veritabani Otomatik Kurulum Tasarimi

## Amac

KantarPro'nun Ayarlar > Baglanti Ayarlari ekranina `Veritabanini Kur` dugmesi eklenecek. Dugme, kullanicinin formda girdigi SQL Server, veritabani adi ve kimlik dogrulama bilgilerini kullanarak yeni KantarPro veritabanini ve semasini otomatik kuracak.

Kurulum, `database` klasorundeki SQL dosyalarini ada gore sirayla calistiracak; rastgele prova verisi olusturan `003_seed_random_demo_data.sql` dosyasi kesinlikle calistirilmayacak.

## Guvenlik Kurallari

1. Islem yalnizca admin kullanicida gorunur ve kullanilabilir.
2. Kurulumdan once kullanicidan acik onay alinir.
3. Hedef SQL Server'a once `master` veritabani uzerinden baglanilir.
4. `KantarPro` veritabani yoksa SQL scriptleri tarafindan olusturulmasina izin verilir.
5. Hedef veritabaninda KantarPro'nun temel tablolarindan herhangi biri varsa kurulum durdurulur.
6. Mevcut tablolar silinmez, sifirlanmaz ve uzerine yazilmaz.
7. `001_create_schema.sql` icindeki koruma davranisi degistirilmez.
8. Herhangi bir script hata verirse sonraki scriptler calistirilmaz ve hata veren dosyanin adi kullaniciya gosterilir.
9. `003_seed_random_demo_data.sql` dosyasi hem dosya adiyla hem de `003` sira numarasiyla filtrelenerek atlanir.

## Mimari

SQL kurulum mantigi `MainWindow.xaml.cs` icine gomulmeyecek. Desktop projesinde ayri bir `DatabaseInstallationService` sinifi bulunacak.

Servisin sorumluluklari:

- Kurulum klasorunu bulmak.
- SQL dosyalarini siralamak.
- `003` dosyasini elemek.
- Sunucu baglantisini test etmek.
- Mevcut KantarPro semasini kontrol etmek.
- SQL Server `GO` ayiraclarini guvenli bicimde batch'lere bolmek.
- Scriptleri sirayla calistirmak.
- Calistirilan ve atlanan scriptleri sonuc nesnesinde bildirmek.

Arayuzun sorumluluklari:

- Formdaki baglanti ayarlarini okumak.
- Kullanici onayi almak.
- Dugmeyi kurulum boyunca pasif hale getirmek.
- Basari veya ayrintili hata sonucunu gostermek.
- Basarili kurulumdan sonra baglanti ayarlarini kaydetmek, sema kontrolunu calistirmak ve ekran verilerini yenilemek.

## Scriptlerin Bulunmasi

Uygulama once calisan EXE'nin yanindaki `database` klasorunu arayacak. Gelistirme ortaminda bu klasor bulunamazsa depo kokune dogru kontrollu bir arama yapilacak.

Kurulum paketine `database\*.sql` dosyalari icerik olarak kopyalanacak. Boylece gercek kantar bilgisayarinda kaynak kod deposuna ihtiyac olmayacak.

Calistirma sirasi:

1. `000_create_migration_history.sql`
2. `001_create_schema.sql`
3. `002_add_saha_ziyareti_kantar_dosyasi.sql`
4. `004_add_fatura_id.sql`
5. `005_add_muaf_kolonlari.sql`
6. `006_seed_first_admin.sql`
7. `007_add_check_constraints.sql`
8. `008_add_cikis_no.sql`
9. `009_add_cikis_no_guards.sql`
10. `010_fix_fatura_id_index.sql`

## Baglanti Akisi

Formdaki SQL ayarlari henuz kaydedilmemis olsa bile kurulum bu degerleri kullanacak. Ilk baglanti `master` veritabanina kurulacak; scriptlerdeki `USE KantarPro` ifadeleri kendi hedeflerine gececek.

SQL Server kimlik dogrulamasi:

- Windows baglantisi seciliyse mevcut Windows kullanicisi kullanilir.
- SQL baglantisi seciliyse formdaki SQL kullanici adi ve sifre kullanilir.

SQL hesabinda veritabani olusturma ve sema nesneleri olusturma yetkisi yoksa kurulum durur ve yetki hatasi acikca gosterilir.

## Arayuz

`Baglanti Ayarlari` sekmesindeki mevcut `Baglantiyi Test Et` dugmesinin yanina veya alt eylem satirina `Veritabanini Kur` dugmesi eklenecek.

Onay metni hedef sunucu ve veritabani adini gosterecek:

> Bu islem belirtilen SQL Server uzerinde yeni KantarPro veritabanini kuracaktir. Mevcut KantarPro tablolari bulunursa islem yapilmayacaktir.

Basarili sonuc, calistirilan script sayisini ve `003` dosyasinin atlandigini bildirecek.

## Hata Yonetimi ve Loglama

Asagidaki bilgiler uygulama loguna yazilacak:

- Kurulumu baslatan kullanici.
- Hedef SQL Server ve veritabani.
- Calistirilan her script.
- Bilerek atlanan `003` scripti.
- Hata veren script ve SQL hata mesaji.
- Kurulumun basarili tamamlanmasi.

SQL sifresi loglara yazilmayacak.

## Testler

Otomatik testlerde:

1. Scriptler ada gore siralanir.
2. `003` her durumda listeden cikarilir.
3. Yalnizca `.sql` dosyalari secilir.
4. `GO` ayiraclari tek basina satir oldugunda batch'lere ayrilir.
5. Script klasoru yoksa anlasilir hata uretilir.
6. Temel KantarPro tablosu varsa kurulum reddedilir.
7. Bos hedefte beklenen script listesi calistirilir.
8. Bir script hata verirse sonraki scriptler calistirilmaz.

Son dogrulamada tam cozum derlenecek ve mevcut testlerin tamami calistirilacak.

## Kabul Kriterleri

- Admin, Baglanti Ayarlari ekranindan tek dugmeyle veritabanini kurabilir.
- `003_seed_random_demo_data.sql` hicbir kosulda calismaz.
- Mevcut KantarPro verileri silinmez veya sifirlanmaz.
- Kurulum paketinde gerekli SQL scriptleri bulunur.
- Hata durumunda hangi scriptin basarisiz oldugu gorulur.
- Basarili kurulumdan sonra uygulama yeni veritabanina baglanabilir.
