# KantarPro Performans Refactoring Raporu - V2

Tarih: 23.09.2026

## Kapsam ve baseline

Bu çalışma mevcut davranışı koruyan, küçük ve izole performans değişiklikleriyle sınırlandırıldı. Veritabanı şeması, iş kuralları, ücret/tahsilat hesapları, seri port kodu, yazdırma ve kullanıcı arayüzü değiştirilmedi.

Canlı SQL Server ve üretim verisi bu çalışma ortamına bağlı olmadığı için süre, CPU ve RAM kazancı ölçülmedi. Sorgu sayıları statik EF6 çağrı analiziyle karşılaştırıldı; bu nedenle ölçülmemiş bir yüzde performans artışı iddia edilmemektedir.

Baseline doğrulaması:

- `dotnet build`, eski tip .NET Framework proje yapısında EF6 referansını çözemedi.
- Visual Studio MSBuild ile restore/build başarılı oldu.
- Visual Studio VSTest ile 122 testin tamamı geçti.

## İlk analizde bulunan 10 öncelikli konu

| # | Konum | Sorun | N+1 | Etki | Risk | Sonuç |
|---|---|---|---|---|---|---|
| 1 | `MainWindow.DashboardData.LoadEntryVehicleRows` | Her giriş satırı için kantar dosyası sorgusu | Evet | Çok yüksek | Düşük | Toplu sorguya çevrildi |
| 2 | `DashboardRowBuilder.HesaplaBeklemeUcreti` | Aktif bekleme tarifesi her giriş satırında sorgulanıyor | Evet | Yüksek | Düşük | Yenileme başına tek sorguya çevrildi |
| 3 | `MainWindow.DashboardData.LoadPendingWeighingRows` | Her bekleyen dosya için açık dönüş kontrolü | Evet | Yüksek | Düşük | Tek toplu sorguya çevrildi |
| 4 | `MainWindow.DashboardData.LoadExitVehicleRows` | Her çıkış satırı için kantar dosyası sorgusu | Evet | Çok yüksek | Düşük | Toplu sorguya çevrildi |
| 5 | `MainWindow.DataAndFormatting.LoadDailyRevenueData` | Her tahsilat/muaf işlem için kantar dosyası sorgusu | Evet | Yüksek | Düşük-Orta | Toplu sorguya çevrildi |
| 6 | `SahaZiyaretiServisi.SuresiDolanKantarDosyalariniKapat` | Her dosya için bekleyen tartım sorgusu | Evet | Orta-Yüksek | Düşük | Toplu sorguya çevrildi |
| 7 | `SahaZiyaretiServisi.SenkronizeBekleyenTartimlar` | İki tam liste arasında iç içe doğrusal arama | Hayır | Yüksek CPU | Düşük | O(n²) yerine O(n) sözlük eşleştirmesi |
| 8 | `MainWindow.LoadLogsByDate` | Kullanıcı navigation alanı döngüde lazy-load ediliyor | Evet | Orta | Düşük | Eager-load ile tek sorgu |
| 9 | Dashboard, rapor ve arama sorguları | Salt okunur entity'ler ChangeTracker'a alınıyor | Hayır | Orta RAM/CPU | Düşük | Uygun sorgulara `AsNoTracking` eklendi |
| 10 | `KantarUnitOfWork` ve numara üretim metotları | String numaralar tamamen RAM'e alınıp parse ediliyor | Hayır | Veri büyüdükçe yüksek | Yüksek | Şema/numaralandırma riski nedeniyle değiştirilmedi |

## Uygulanan değişiklikler

### Dashboard N+1 sorguları

- Önce: giriş listesinde `1 + N + N`, bekleyen listede `1 + N`, çıkış listesinde `1 + N` sorgu adayı.
- Sonra: her liste için ana sorguya ek olarak en fazla 1-2 toplu sorgu.
- Davranış değişikliği: Yok.
- Etkilenen dosyalar: `MainWindow.DashboardData.cs`, `MainWindow.DataAndFormatting.cs`, `DashboardRowBuilder.cs`.
- Risk: Düşük.
- Test: Build başarılı, 122/122 test başarılı.

### Süresi dolan kantar dosyaları

- Önce: 1 ana sorgu + kapanan her dosya için 1 sorgu.
- Sonra: en fazla 2 sorgu.
- Davranış değişikliği: Yok; durum, not ve log üretimi korunuyor.
- Etkilenen dosya: `SahaZiyaretiServisi.cs`.
- Risk: Düşük.
- Test: Mevcut süre dolumu testi güçlendirildi; 122/122 test başarılı.

### Günlük tahsilat kantar dosyaları

- Önce: iki ana sorgu + her tahsilat/muaf işlem için ayrı sorgu.
- Sonra: iki ana sorgu + tek toplu kantar dosyası sorgusu.
- Davranış değişikliği: Yok; tahsilat gruplama, ödeme türü ve toplamlar aynı kaldı.
- Etkilenen dosya: `MainWindow.DataAndFormatting.cs`.
- Risk: Düşük-Orta.
- Test: Build başarılı, 122/122 test başarılı.

### Salt okunur sorgular

- Değişiklik: Dashboard, günlük tahsilat, detay ve arama sorgularına seçici olarak `AsNoTracking` eklendi.
- Davranış değişikliği: Yok.
- Güncelleme/silme/SaveChanges kullanan sorgular kapsam dışında bırakıldı.
- Risk: Düşük.
- Test: Build başarılı, 122/122 test başarılı.

### Bekleyen tartım senkronizasyonu

- Önce: O(kantar dosyası x bekleyen tartım) bellek içi arama.
- Sonra: O(kantar dosyası + bekleyen tartım) sözlük eşleştirmesi.
- SQL sorgu sayısı: Değişmedi.
- Davranış değişikliği: Yok; aynı ilk eşleşme korunuyor.
- Risk: Düşük.
- Test: Build başarılı, 122/122 test başarılı.

### Log ve Include optimizasyonları

- Log kullanıcıları eager-load edilerek lazy-load N+1 kaldırıldı.
- Bekleyen detayındaki kullanılmayan karşı tartım ücret graph'ı kaldırıldı.
- Plaka ödeme geçmişindeki kullanılmayan işlem/araç Include kaldırıldı.
- Davranış değişikliği: Yok.
- Risk: Düşük.
- Test: Build başarılı, 122/122 test başarılı.

## Bilerek uygulanmayan değişiklikler

- `CikisNo`, tahsilat no ve kantar fiş no alanlarının string yapısı değiştirilmedi.
- `ToList().GroupBy()` kullanılan ödeme grupları SQL'e zorla taşınmadı; null/fallback ve aynı tahsilatın birden fazla ücret satırı kuralları korundu.
- Dashboard bakım işlemleri kaldırılmadı veya background thread'e taşınmadı.
- UI genelinde async/await dönüşümü yapılmadı.
- RS232, buffer, timer, frame parsing ve timeout koduna dokunulmadı.
- Yeni SQL indeksi eklenmedi. Mevcut scriptlerde ilgili temel indeksler var; canlı execution plan ve ölçüm olmadan yeni indeks eklemek uygun görülmedi.
- `SenkronizeBekleyenTartimlar` sorgularının tüm tarihsel tabloları okuması değiştirilmedi; sorgu kapsamını daraltmak canlı veriyle manuel regresyon gerektirir.

## Manuel regresyon önerileri

Otomatik testlere ek olarak saha ortamında Dashboard açılışı, giriş/çıkış, iki tartım eşleştirmesi, bekleyen tartım, günlük tahsilat, log listesi, arama, kantar fişi ve yeniden başlatma senaryoları çalıştırılmalıdır. SQL Profiler ile Dashboard sorgu sayısının ve açılış süresinin önce/sonra ölçülmesi önerilir.
