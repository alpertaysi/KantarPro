# Gumruk Kantar Otomasyon Sistemi

Bu depo, Bursa Tasfiye Isletme Mudurlugu cift kantar akisi icin hazirlanan WPF tabanli masaustu uygulama iskeletini icerir.

## Teknoloji

- C# WPF
- .NET Framework 4.8
- Entity Framework 6
- SQL Server 2008 Express
- SQL semasi: ASCII uyumlu Turkce tablo ve kolon adlari

## Klasorler

- `database/000_create_migration_history.sql`: Veritabani yoksa olusturur ve migration takip tablosunu ekler.
- `database/001_create_schema.sql`: Yeni kurulum / bilerek sifirlama icin tablo, iliski ve indeks scripti. Mevcut tablolar varsa `@OnayliSil = 1` yapilmadan calismaz.
- `database/002_add_saha_ziyareti_kantar_dosyasi.sql`: Mevcut veritabanlari icin saha ziyareti ve kantar dosyasi guncellemesi.
- `database/003_seed_random_demo_data.sql`: Sadece Dev/Test/Local veritabanlari icin demo veri scripti. Production'da calismaz.
- `database/004_add_fatura_id.sql`: Mevcut veritabanlari icin fatura, tahsilat no ve odeme turu alanlari.
- `database/005_add_muaf_kolonlari.sql`: Mevcut veritabanlari icin muafiyet alanlari.
- `database/006_seed_first_admin.sql`: Ilk admin kullanicisini olusturur; calistirmadan once gercek parola hash degeri yazilmalidir.
- `database/007_add_check_constraints.sql`: Mevcut veritabanlarina veri butunlugu CHECK constraint'leri ekler.
- `database/010_fix_fatura_id_index.sql`: Ayni tahsilata ait birden fazla ucret satirinin ortak FaturaId kullanabilmesini saglar.
- `src/KantarPro.Domain`: Entity ve sabitler.
- `src/KantarPro.Application`: Is kurallari ve uygulama servisleri.
- `src/KantarPro.Infrastructure`: EF6 DbContext, Fluent API mapping ve repository.
- `src/KantarPro.Desktop`: WPF baslangic uygulamasi.
- `tests/KantarPro.Application.Tests`: Cekirdek is kurali testleri.

## Ilk Kurulum

1. SQL Server 2008 Express uzerinde calisacaginiz instance'i belirleyin ve once yedekleme politikasini netlestirin.
2. `database/000_create_migration_history.sql` dosyasini calistirin.
3. Yeni kurulum icin `database/001_create_schema.sql` dosyasini calistirin. Mevcut tablo varsa script durur; bilerek sifirlama yapilacaksa script basindaki `@OnayliSil` degeri `1` yapilmalidir.
4. `database/004_add_fatura_id.sql`, `database/005_add_muaf_kolonlari.sql` ve `database/010_fix_fatura_id_index.sql` dosyalarini calistirin. Fresh install sonrasi idempotent olarak tekrar calissalar da sorun olmamalidir.
5. Istege bagli olarak `database/007_add_check_constraints.sql` dosyasini calistirin. Mevcut veride negatif/0 tartim veya negatif ucret varsa bu script durur; once veri temizlenmelidir.
6. `database/006_seed_first_admin.sql` dosyasinda `@AdminParolaHash` degerini gercek hash ile degistirip ilk admin kullanicisini olusturun.
7. Programin Ayarlar ekranindan SQL, istasyon ve COM port secimini yapin. OKI 5720 yazici paylasim yolu da kurulum sonrasi gercek paylasim yolu ile guncellenmelidir.
8. Visual Studio ile `KantarPro.sln` dosyasini acip NuGet paketlerini geri yukleyin.

## Mevcut Veritabani Guncellemesi

1. Mevcut `KantarPro` veritabaninin tam yedegini alin.
2. Scriptleri sirasiyla calistirin: `000_create_migration_history.sql`, `002_add_saha_ziyareti_kantar_dosyasi.sql`, `004_add_fatura_id.sql`, `005_add_muaf_kolonlari.sql`, `010_fix_fatura_id_index.sql`.
3. Veri temizligi uygunsa `007_add_check_constraints.sql` dosyasini calistirin.
4. `001_create_schema.sql` mevcut veritabaninda guncelleme amaciyla kullanilmaz; bu dosya yeni kurulum veya bilerek sifirlama icindir.

## Demo Veri

`003_seed_random_demo_data.sql` yalnizca Dev/Test/Local adli veritabanlarinda calisir. Calistirmadan once script icindeki `@DemoSeedOnayla` degeri `1` yapilmalidir. Gercek saha veritabaninda demo seed calistirilmemelidir.
