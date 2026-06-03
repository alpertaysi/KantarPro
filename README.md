# Gumruk Kantar Otomasyon Sistemi

Bu depo, Bursa Tasfiye Isletme Mudurlugu cift kantar akisi icin hazirlanan WPF tabanli masaustu uygulama iskeletini icerir.

## Teknoloji

- C# WPF
- .NET Framework 4.8
- Entity Framework 6
- SQL Server 2008 Express
- SQL semasi: ASCII uyumlu Turkce tablo ve kolon adlari

## Klasorler

- `database/001_create_schema.sql`: SQL Server 2008 uyumlu temiz kurulum tablo, iliski ve indeks scripti.
- `database/002_add_saha_ziyareti_kantar_dosyasi.sql`: Mevcut veritabanlari icin saha ziyareti ve kantar dosyasi guncellemesi.
- `database/003_seed_random_demo_data.sql`: Demo veri scripti.
- `database/004_add_fatura_id.sql`: Mevcut veritabanlari icin fatura/tahsilat alanlari.
- `database/005_add_muaf_kolonlari.sql`: Mevcut veritabanlari icin muafiyet alanlari.
- `src/KantarPro.Domain`: Entity ve sabitler.
- `src/KantarPro.Application`: Is kurallari ve uygulama servisleri.
- `src/KantarPro.Infrastructure`: EF6 DbContext, Fluent API mapping ve repository.
- `src/KantarPro.Desktop`: WPF baslangic uygulamasi.
- `tests/KantarPro.Application.Tests`: Cekirdek is kurali testleri.

## Ilk Kurulum

1. SQL Server 2008 Express uzerinde `KantarPro` adli veritabani olusturun.
2. Yeni kurulum icin `database/001_create_schema.sql` dosyasini calistirin.
3. Demo veri istenirse `database/003_seed_random_demo_data.sql` dosyasini calistirin.
4. Mevcut eski veritabani guncellenecekse scriptleri sirasiyla calistirin: `002_add_saha_ziyareti_kantar_dosyasi.sql`, `004_add_fatura_id.sql`, `005_add_muaf_kolonlari.sql`.
5. `src/KantarPro.Desktop/App.config` icindeki connection string sunucu adini saha ortamindaki SQL instance ile guncelleyin.
6. Visual Studio ile `KantarPro.sln` dosyasini acip NuGet paketlerini geri yukleyin.
