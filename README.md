# Gumruk Kantar Otomasyon Sistemi

Bu depo, Bursa Tasfiye Isletme Mudurlugu cift kantar akisi icin hazirlanan WPF tabanli masaustu uygulama iskeletini icerir.

## Teknoloji

- C# WPF
- .NET Framework 4.8
- Entity Framework 6
- SQL Server 2008 Express
- SQL semasi: ASCII uyumlu Turkce tablo ve kolon adlari

## Klasorler

- `database/001_create_schema.sql`: SQL Server 2008 uyumlu tablo, iliski, indeks ve seed scripti.
- `src/KantarPro.Domain`: Entity ve sabitler.
- `src/KantarPro.Application`: Is kurallari ve uygulama servisleri.
- `src/KantarPro.Infrastructure`: EF6 DbContext, Fluent API mapping ve repository.
- `src/KantarPro.Desktop`: WPF baslangic uygulamasi.
- `tests/KantarPro.Application.Tests`: Cekirdek is kurali testleri.

## Ilk Kurulum

1. SQL Server 2008 Express uzerinde `KantarPro` adli veritabani olusturun.
2. `database/001_create_schema.sql` dosyasini calistirin.
3. `src/KantarPro.Desktop/App.config` icindeki connection string sunucu adini saha ortamindaki SQL instance ile guncelleyin.
4. Visual Studio ile `KantarPro.sln` dosyasini acip NuGet paketlerini geri yukleyin.

