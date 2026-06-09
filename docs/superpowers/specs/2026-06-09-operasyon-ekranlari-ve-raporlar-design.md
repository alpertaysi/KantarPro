# KantarPro Operasyon Ekranları ve Raporlar Tasarımı

## Amaç
Bu çalışma, gerçek saha kullanımında memurun programı kapatmadan işlem yapabilmesini, geçmiş kayıtları araştırabilmesini, fiş/rapor çıktılarını daha kontrollü alabilmesini ve sunum sırasında admin kullanıcısının geçici manuel kilo girebilmesini sağlar.

## Kapsam
- Makbuz butonu ve kantar fişi numarası davranışı.
- Plaka/firma bazlı geçmiş araştırma ekranı ve çıktı alma.
- Günlük tahsilat için admin XLSX dışa aktarma.
- OKI döküm yazdırma önizleme akışı.
- Alt menü sadeleştirme ve manuel yedekleme butonu.
- Program açıkken kullanıcı değiştirme ve aynı pencerede şifre değiştirme.
- Sadece admin için geçici manuel kilo girişi.

## Makbuz ve Kantar Fişi
Makbuz butonuna basıldığında seçili satır için önce kantar fişi numarası garanti edilir. Kullanıcıya `Kantar fişi yazdırılsın mı?` sorulur. Kullanıcı vazgeçerse fiş basılmaz ancak verilen fiş numarası veritabanında kalır.

Aynı işlem için tekrar fiş yazdırılırsa yeni numara üretilmez. Mevcut `Tartim.KantarFisNo` değeri kullanılır. Böylece fiziksel fiş ile sistem kaydı tekrar baskılarda da aynı kalır.

## Araştır Ekranı
Araştır butonu yeni bir pencere açar. Pencere plaka ve firma alanlarıyla geçmiş kayıtları arar. Arama sadece aktif listelerle sınırlı olmaz; veritabanındaki geçmiş giriş, çıkış, tartım, tahsilat, ödeme türü, fiş no, kullanıcı ve durum bilgilerini getirir.

Aynı plaka farklı tarihlerde ve farklı firmalarla gelmişse her kayıt ayrı satır olarak gösterilir. Sonuçlar tarih sırasına göre listelenir ve kullanıcı isterse çıktı alabilir. Çıktı için OKI ham metin dökümü ve dosyaya aktarma seçenekleri desteklenir.

## Günlük Tahsilat Çıktıları
Admin kullanıcısı günlük tahsilatı XLSX olarak dışa aktarabilir. XLSX üretimi programı zorlamayacak şekilde hafif tutulur: bellekte basit tablo oluşturulur, formül veya ağır biçimlendirme yapılmaz. Mevcut PDF/OKI rapor akışları korunur.

OKI Döküm Yazdır butonuna basıldığında doğrudan yazıcıya gitmek yerine baskı önizleme açılır. Önizleme üzerinden `Yazdır` seçilirse OKI yazıcıya ham metin dökümü gönderilir.

## Alt Menü ve Yedekleme
Alt menüde `Menü` butonu kaldırılır. `Makbuz`, `Araştır` ve `Yedekle` kalır.

Yedekle butonu otomatik yedekleme scriptinin yaptığı işi manuel başlatır. Otomatik script hafta sonu çalışmaz; ancak kullanıcı cumartesi veya pazar günü bu butona basarsa manuel yedek alınır. Bu nedenle UI çağrısı scriptteki hafta sonu kontrolünü zorunlu olarak atlayabilmelidir.

## Kullanıcı Değiştir
Üstteki `Şifre Değiştir` butonu `Kullanıcı Değiştir` olur. Program kapanmadan küçük bir pencere açılır. Pencerede sistemdeki aktif kullanıcılar combobox içinde listelenir. Kullanıcı seçilir, şifre girilir ve oturum aktif kullanıcıya geçer.

Aynı pencerede şifre değiştirme alanı da bulunur. Kullanıcı mevcut şifresini ve yeni şifresini girerek kendi şifresini değiştirebilir. Admin de bu pencereden kendi kullanıcısına geçebilir; kullanıcı yetkileri oturum değişince ana ekrana yansır.

## Admin Manuel Kilo
Gerçek kullanımda kilo indikatörden gelir. Ancak sunum ve eğitim için sadece admin kullanıcısına geçici manuel kilo girişi açılır. Memur kullanıcıları kilo textboxına elle değer giremez.

Admin modunda elle girilen kilo yine mevcut tartım kaynağı metodundan okunur. Böylece iş akışı tek kalır; sadece veri kaynağı admin için geçici olarak textbox olabilir.

## Veri ve Servis Etkisi
- Kantar fişi numarası üretimi mevcut `EnsureKantarFisNo` davranışını korur ve tüm yazdırma yollarında önce bu metot çağrılır.
- Araştır ekranı doğrudan veritabanından geniş kapsamlı salt-okunur veri çeker.
- XLSX dışa aktarma yeni bir hafif exporter sınıfıyla yapılır.
- Manuel yedekleme için mevcut `tools/BackupKantarPro.ps1` scriptine hafta sonu kontrolünü atlayan parametre eklenir veya UI çağrısı ayrı manuel modla yapılır.
- Kullanıcı değiştirme mevcut `KullaniciServisi` doğrulama ve parola değiştirme akışını kullanır.

## Test Kabul Kriterleri
- Makbuz butonunda kullanıcı vazgeçse bile seçili tartıma 5 haneli fiş numarası yazılır.
- Aynı satıra tekrar makbuz basılırsa fiş numarası değişmez.
- Araştır ekranı plaka/firma ile eski kayıtları tarih bağımsız getirir.
- Araştır sonuçları yazdırılabilir veya dışa aktarılabilir.
- Admin XLSX dışa aktarabilir; memur bu işlemi yapamaz.
- OKI döküm önce önizleme açar, yazdır denince ham metin yazıcıya gönderilir.
- Alt menüde Menü butonu yoktur; Makbuz, Araştır, Yedekle kalır.
- Yedekle butonu hafta sonu dahil manuel yedek alabilir.
- Kullanıcı değiştir penceresi programı kapatmadan oturum değiştirir.
- Aynı pencereden şifre değiştirilebilir.
- Sadece admin manuel kilo girebilir; memur indikatör kilosu olmadan tartımlı kayıt yapamaz.

