# KantarPro – Güvenli Performans Refactoring Talimatı

Bu proje aktif olarak çalışan bir saha uygulamasıdır. Programın mevcut işlevleri şu anda beklediğimiz şekilde çalışmaktadır.

Bu nedenle önceliğin yeni özellik eklemek veya mimariyi yeniden tasarlamak değil, **mevcut davranışı %100 koruyarak performans darboğazlarını azaltmak** olmalıdır.

## 1. EN ÖNEMLİ KURAL

Mevcut çalışan davranışı bozma.

Aşağıdaki alanlarda davranış değişikliği yapılmayacak:

* Kantar giriş işlemleri
* Kantar çıkış işlemleri
* Tartım hesaplamaları
* Net ağırlık hesaplamaları
* Ücret hesaplamaları
* Tahakkuk
* Tahsilat
* Bekleyen tartım işlemleri
* Kantar dosyaları
* Saha ziyareti işlemleri
* Plaka işlemleri
* Yazdırma
* Excel/PDF raporları
* Seri port / RS232 haberleşmesi
* Kullanıcı yetkilendirme
* SQL kayıt mantığı
* Veri silme/güncelleme davranışları
* Mevcut ekranların kullanıcı davranışı

Bir refactoring işlemi mevcut davranışı değiştirme riski taşıyorsa bunu otomatik olarak uygulama. Önce problemi ve riski raporla.

---

# 2. ÇALIŞMA YÖNTEMİ

İşlemleri tek seferde büyük bir refactoring olarak yapma.

Her değişiklik şu sırayla yapılmalı:

1. Mevcut kodu incele.
2. Performans problemini kesin olarak tespit et.
3. Problemin nedenini açıkla.
4. Değişikliğin mevcut davranışı neden bozmayacağını açıkla.
5. Küçük ve izole bir değişiklik yap.
6. Build/test çalıştır.
7. Değişiklikten sonra oluşabilecek davranış farklarını kontrol et.
8. Sonucu raporla.
9. Sonraki performans problemine geç.

Birden fazla bağımsız optimizasyonu tek devasa değişiklik halinde yapma.

---

# 3. ÖNCELİKLİ PERFORMANS PROBLEMLERİ

Öncelik sırası aşağıdaki olmalıdır.

## P0 – N+1 SQL sorguları

Özellikle Dashboard ve listeleme ekranlarını incele.

Şu tür kodları ara:

```csharp
foreach (...)
{
    context.SomeTable
        .Where(...)
        .FirstOrDefault();
}
```

veya:

```csharp
var rows = query.ToList();

foreach (var row in rows)
{
    var detail = GetSomething(row.Id);
}
```

Bir ana sorgudan sonra her kayıt için ayrı SQL sorgusu çalışıyorsa bunu tespit et.

Örneğin:

```text
1 ana sorgu
+
100 kayıt için 100 ayrı sorgu
=
101 SQL sorgusu
```

Bu tür yapıları batch sorguya dönüştür.

Tercih edilen yaklaşım:

```text
Ana kayıtları getir
↓
İlgili ID'leri topla
↓
Tek/az sayıda SQL sorgusuyla ilişkili kayıtları getir
↓
Dictionary / Lookup oluştur
↓
Bellekte eşleştir
```

Ama sonuçların mevcut kodla aynı olduğundan emin ol.

---

# 4. DASHBOARD ÖNCELİKLİ İNCELEME

Özellikle:

```text
MainWindow
MainWindow.DataAndFormatting.cs
MainWindow.DashboardData.cs
```

dosyalarını incele.

Dashboard açılışında kaç SQL sorgusu çalıştığını analiz et.

Özellikle aşağıdakileri tespit et:

* Döngü içinde SQL sorguları
* Her satır için tekrar tekrar sorgu
* Gereksiz Include
* Gereksiz ToList()
* Gereksiz FirstOrDefault()
* Gereksiz Count()
* Aynı verinin birden fazla kez sorgulanması
* Aynı entity'nin tekrar tekrar yüklenmesi

Dashboard'ın sadece ihtiyaç duyduğu alanları çek.

Entity'nin tamamını yüklemek yerine gerektiğinde projection kullan:

```csharp
.Select(x => new SomeDto
{
    Id = x.Id,
    Plaka = x.Arac.Plaka,
    ...
})
```

Ancak DTO/projection değişikliğinin mevcut UI davranışını bozmadığından emin ol.

---

# 5. AsNoTracking

Salt okunur sorguları tespit et.

Örneğin:

* Dashboard
* Arama sonuçları
* Rapor ekranları
* Geçmiş kayıtları
* Salt okunur listeler

Bu sorgularda uygun olduğu yerlerde:

```csharp
.AsNoTracking()
```

kullan.

Ancak aşağıdaki işlemlerde körlemesine AsNoTracking kullanma:

* Güncelleme
* Silme
* Entity üzerinde değişiklik yapılıp SaveChanges çağrılan işlemler
* Mevcut UnitOfWork davranışına bağlı işlemler

Amaç ChangeTracker yükünü azaltmaktır; iş mantığını değiştirmek değildir.

---

# 6. GEREKSİZ Include'LARI AZALT

Projede tüm:

```csharp
.Include(...)
```

kullanımlarını incele.

Özellikle bir sorguda:

```csharp
.Include(...)
.Include(...)
.Include(...)
```

şeklinde büyük entity graph oluşturuluyorsa bunun gerçekten gerekli olup olmadığını kontrol et.

Dikkat:

Include'ları sadece performans amacıyla rastgele kaldırma.

Önce ilgili navigation property'nin gerçekten kullanılıp kullanılmadığını tespit et.

Kullanılmayan ilişkiler kaldırılabilir.

Gerektiğinde projection kullan.

---

# 7. ToList() SONRASI YAPILAN İŞLEMLER

Aşağıdaki kalıpları özellikle ara:

```csharp
.ToList()
.GroupBy(...)
```

```csharp
.ToList()
.Where(...)
```

```csharp
.ToList()
.OrderBy(...)
```

```csharp
.ToList()
.Select(...)
```

```csharp
.ToList()
.FirstOrDefault(...)
```

```csharp
.ToList()
.Count()
```

Eğer işlem SQL Server tarafından güvenli şekilde yapılabiliyorsa işlemi SQL tarafına taşı.

Özellikle:

```csharp
ToList().GroupBy()
```

kullanımlarını incele.

Ancak EF6'nın SQL'e çeviremediği karmaşık ifadelerde zorla SQL'e taşımaya çalışma.

---

# 8. KANTAR DOSYASI SORGULARI

Özellikle:

```text
TryBuildKantarDosyasiDetail
```

ve benzeri metotları incele.

Büyük Include graph'ları ve sonrasında bellekte yapılan:

```csharp
FirstOrDefault(...)
```

eşleştirmelerini analiz et.

Özellikle aynı araç/plaka için gereğinden fazla kayıt çekiliyorsa sorgunun sınırlandırılıp sınırlandırılamayacağını değerlendir.

Mevcut tarih/saat/tartım eşleştirme mantığını değiştirme.

Bu eşleştirme iş kuralları açısından kritik olabilir.

---

# 9. ÖDEME / TAHSİLAT SORGULARI

Özellikle ödeme geçmişi ve günlük tahsilat sorgularını incele.

Şu tür bir yapı varsa:

```csharp
.ToList()
.GroupBy(...)
.OrderBy(...)
.Take(...)
```

öncelikle bunun SQL tarafında yapılabilirliğini değerlendir.

Ama ödeme kayıtlarının anlamını veya gruplama mantığını değiştirme.

Özellikle:

* Tahsilat numarası
* Tahsilat tarihi
* Ödeme türü
* Tahsil edilen ücret
* Aynı tahsilatın birden fazla ücret satırı olması

gibi mevcut davranışları koru.

---

# 10. CIKIS NO / NUMARALANDIRMA

Şu tür yapıları ara:

```csharp
.ToList()
.Select(ParseNumber)
.Max()
```

veya CikisNo gibi string olarak tutulan sayısal değerlerin tamamının RAM'e çekilmesi.

Bunun performans etkisini değerlendir.

Ancak mevcut veritabanı şemasını değiştirmek gerekiyorsa bunu otomatik olarak yapma.

Özellikle:

```text
CikisNo
```

gibi alanları `string -> int` dönüştürmek bu çalışma kapsamında otomatik yapılmamalıdır.

Bu ancak ayrı bir migration/refactoring planı olarak raporlanmalıdır.

---

# 11. UI THREAD

WPF UI thread üzerinde çalışan ağır SQL işlemlerini tespit et.

Özellikle Dashboard açılışında:

```text
SQL sorgusu
↓
ToList
↓
GroupBy
↓
DTO oluşturma
↓
hesaplamalar
↓
UI
```

gibi uzun süren işlemler varsa incele.

Ancak async/await dönüşümünü bütün projeye yayma.

Öncelikle gerçekten UI'ı bloke eden sorguları tespit et.

Threading değişikliği yapılacaksa:

* UI kontrollerine background thread'den erişme
* DbContext'i thread'ler arasında paylaşma
* Aynı DbContext'i paralel kullanma
* Race condition oluşturma
* Mevcut transaction davranışını bozma

---

# 12. DASHBOARD MAINTENANCE İŞLEMLERİ

Dashboard açılırken aşağıdaki gibi bakım/senkronizasyon işlemleri yapılıyorsa tespit et:

```text
SuresiDolanKantarDosyalariniKapat
SenkronizeBekleyenTartimlar
```

Dashboard'ın görüntülenmesi ile bakım işlemlerini birbirinden ayırmanın mümkün olup olmadığını değerlendir.

Ancak bu işlemleri kaldırma.

Önce hangi iş kuralını gerçekleştirdiklerini tespit et.

Amaç:

```text
Maintenance
    ↓
ayrı

Dashboard Read
    ↓
ayrı
```

hale getirmektir.

---

# 13. SQL INDEXLER

Mevcut SQL indexlerini incele.

Önce mevcut indexleri listele.

Sonra uygulamadaki gerçek sorgularla karşılaştır.

Örneğin:

```text
WHERE
JOIN
ORDER BY
GROUP BY
```

kullanılan alanları analiz et.

Yeni index eklemek için:

1. Önce sorgunun gerçekten yavaş olduğunu göster.
2. Mevcut indexleri kontrol et.
3. Execution plan varsa incele.
4. Yeni index'in faydasını açıkla.
5. Gereksiz veya duplicate index oluşturma.

Sırf teorik olarak faydalı olabilir diye çok sayıda index ekleme.

---

# 14. SERIAL / RS232 KODUNA DOKUNMA

Kantarın RS232 haberleşmesi kritik saha fonksiyonudur.

Özellikle:

```text
KantarSerialReader
DataReceived
Timer
Buffer
Frame parsing
```

kodlarını performans gerekçesiyle değiştirmeden önce çok dikkatli ol.

COM haberleşmesi şu anda çalışıyorsa:

* event mekanizmasını değiştirme
* polling mekanizmasını kaldırma
* buffer davranışını değiştirme
* frame parsing mantığını değiştirme
* timeout değerlerini değiştirme

Bunlar ancak ayrı bir performans problemi kanıtlanırsa ele alınmalıdır.

CPU kullanımı düşükse RS232 optimizasyonunu önceliklendirme.

---

# 15. EF DbContext

DbContext kullanımını incele.

Şunları kontrol et:

* Gereğinden uzun yaşayan DbContext
* Aynı context'in çok fazla entity takip etmesi
* Aynı context ile gereksiz sorgular
* Dispose problemleri
* Thread'ler arasında Context paylaşılması

Ancak mevcut DbContext mimarisini komple değiştirme.

---

# 16. SaveChanges

Aşağıdaki noktaları incele:

```csharp
SaveChanges()
```

kaç kez çağrılıyor?

Tek bir kullanıcı işlemi içinde gereksiz şekilde:

```text
SaveChanges
SaveChanges
SaveChanges
```

yapılıyorsa bunun transaction ve performans etkisini analiz et.

Ancak SaveChanges sayılarını azaltmak için transaction/business logic davranışını bozma.

---

# 17. MEMORY

Özellikle büyük:

```csharp
ToList()
Include()
GroupBy()
```

işlemlerinde RAM kullanımını değerlendir.

500 / 1000 / 5000 kayıt gibi büyük sonuçların belleğe alınmasını engellemek mümkünse değerlendir.

Ama kullanıcı ekranının beklediği kayıt sayısını değiştirme.

---

# 18. YASAKLAR

Bu performans refactoring sırasında aşağıdakileri yapma:

* Büyük mimari yeniden yazım
* MVVM'e komple geçiş
* EF6 -> EF Core geçişi
* WPF -> başka UI teknolojisi
* SQL Server değişikliği
* Database schema redesign
* Yeni API katmanı
* Yeni dependency ekleme
* Gereksiz NuGet paketi ekleme
* Seri haberleşme protokolünü değiştirme
* İş kurallarını değiştirme
* Kullanıcı arayüzünü yeniden tasarlama
* Dosya/namespace yapısını gereksiz değiştirme
* Çalışan kodu sırf "daha temiz" olduğu için yeniden yazma

Bu çalışma bir "rewrite" değildir.

Bu çalışma:

> **behavior-preserving performance refactoring**

çalışmasıdır.

---

# 19. HER DEĞİŞİKLİK İÇİN RAPOR

Her yaptığın optimizasyondan sonra şu formatta raporla:

### Değişiklik

Örneğin:

```text
Dashboard'daki N+1 sorgu problemi giderildi.
```

### Önce

```text
1 + N SQL sorgusu
```

### Sonra

```text
2-3 SQL sorgusu
```

### Davranış değişikliği

```text
Yok
```

### Etkilenen dosyalar

```text
...
```

### Risk

```text
Düşük / Orta / Yüksek
```

### Test

```text
Build: başarılı
Unit test: başarılı
...
```

---

# 20. DEĞİŞİKLİK BOYUTU

Her commit küçük ve anlaşılır olmalı.

Örneğin:

```text
perf: remove dashboard N+1 query
```

sonra:

```text
perf: add AsNoTracking to read-only dashboard queries
```

sonra:

```text
perf: optimize payment history query
```

gibi.

Bir commit içinde:

```text
20 dosya
+
mimari değişiklik
+
database migration
+
UI değişikliği
+
performans optimizasyonu
```

yapma.

---

# 21. TEST KURALI

Her optimizasyondan sonra:

```text
dotnet build
```

ve mevcut test altyapısı neyi destekliyorsa ilgili testleri çalıştır.

Ayrıca mümkün olduğunca aşağıdaki manuel regresyon senaryolarını koru:

1. Kantar açılışı
2. Ağırlık okuma
3. Giriş tartımı
4. Çıkış tartımı
5. Net hesaplama
6. Ücret hesaplama
7. Tahakkuk
8. Tahsilat
9. Bekleyen tartım
10. Kantar dosyası
11. Saha ziyareti
12. Arama
13. Rapor
14. Yazdırma
15. Program yeniden başlatma

Performans değişikliği bu davranışlardan herhangi birini etkiliyorsa dur ve raporla.

---

# 22. ÖNCE BASELINE ÇIKAR

Kod değiştirmeden önce mümkün olduğunca mevcut durumu ölç.

Örneğin:

```text
Dashboard açılış süresi
SQL sorgu sayısı
En fazla çalışan sorgular
Büyük Include sorguları
CPU
RAM
```

Ölçüm mümkün değilse statik kod analizine dayandığını belirt.

"Performans arttı" demek için ölçüm yoksa bunu iddia etme.

---

# 23. EN ÖNEMLİ ÖNCELİK SIRASI

Şu sırayla çalış:

### 1.

N+1 SQL sorguları

### 2.

Dashboard sorguları

### 3.

Gereksiz Include

### 4.

AsNoTracking

### 5.

ToList() sonrası GroupBy/Where/OrderBy gibi işlemler

### 6.

Ödeme/tahsilat sorguları

### 7.

UI thread üzerinde ağır DB işlemleri

### 8.

KantarDosyasi sorguları

### 9.

Gereksiz SaveChanges

### 10.

SQL indexleri

### 11.

CikisNo gibi ikincil optimizasyonlar

### 12.

RS232/serial optimizasyonu

RS232'yi ancak gerçek bir performans problemi tespit edilirse ele al.

---

# 24. ÇALIŞMAYA BAŞLARKEN

İlk aşamada hiçbir dosyayı değiştirme.

Önce repo genelinde analiz yap.

Özellikle şu dosyaları incele:

```text
MainWindow.DataAndFormatting.cs
MainWindow.DashboardData.cs
MainWindow.xaml.cs
IslemServisi.cs
SahaZiyaretiServisi.cs
KantarUnitOfWork.cs
KantarDbContext
KantarSerialReader
```

Ayrıca tüm repo içinde:

```text
.Include(
.ToList(
.SaveChanges(
.FirstOrDefault(
.Count(
.GroupBy(
foreach
```

kullanımlarını tara.

Bana önce şu raporu ver:

1. En kritik 10 performans problemi
2. Her problemin bulunduğu dosya/metot
3. Tahmini etkisi
4. N+1 olup olmadığı
5. Değişiklik riski
6. Önerilen çözüm
7. Hangi değişikliklerin güvenli olduğu
8. Hangilerinin manuel test gerektirdiği

**İlk rapor tamamlanmadan kod değiştirme.**

---

# 25. SON KURAL

Bu proje şu anda çalışıyor.

Bu nedenle:

> **"Daha temiz kod" uğruna çalışan kodu değiştirme.**

Öncelik sırası:

```text
Mevcut davranış
      ↓
Veri doğruluğu
      ↓
Saha güvenilirliği
      ↓
Performans
      ↓
Kod temizliği
```

Performans kazancı küçük, regresyon riski yüksek olan değişiklikleri yapma.

Performans refactoring'in amacı mevcut sistemi yeniden tasarlamak değil:

> **Aynı sonucu daha az SQL sorgusu, daha az CPU, daha az RAM ve daha kısa sürede üretmesini sağlamaktır.**
