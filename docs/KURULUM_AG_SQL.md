# KantarPro iki bilgisayarli kurulum rehberi

Bu rehber, KantarPro'nun bir giris kantari bilgisayari ve bir cikis kantari bilgisayari ile ortak SQL Server uzerinden calismasi icindir.

## Mimari

- Giris bilgisayari sunucudur.
  - SQL Server Express bu bilgisayarda calisir.
  - Veritabani adi: `KantarPro`
  - Giris kantari COM portu bu bilgisayarda ayarlanir.
- Cikis bilgisayari istemcidir.
  - SQL Server yuklu olmak zorunda degildir.
  - Giris bilgisayarindaki SQL Server'a ag kablosu uzerinden baglanir.
  - Cikis kantari COM portu bu bilgisayarda ayarlanir.
- Ucretler SQL veritabaninda ortaktir.
- SQL, istasyon ve COM ayarlari her bilgisayarda ayrica saklanir.
  - Yerel ayar dosyasi: `%AppData%\KantarPro\station-settings.ini`

## Onemli not

Gercek sahadaki bilgisayarlarda eski kantar programi calisiyorsa kurulum mesai disinda ve yedek alindiktan sonra yapilmalidir. Ozellikle SQL Server ayarlari degistirildigi icin eski programla ayni SQL instance kullaniliyorsa once test edilmelidir.

## 1. Sunucu bilgisayar hazirligi

Sunucu bilgisayar giris kantarinin bagli oldugu bilgisayardir.

1. SQL Server Express kurulu ve `SQLEXPRESS` instance'i calisir durumda olmali.
2. `KantarPro` veritabani bu SQL Server uzerinde bulunmali.
3. Iki bilgisayar ag kablosu ile baglanmali.
4. `tools\SetupServerNetwork.ps1` dosyasi yonetici olarak calistirilmali.

Varsayilan sunucu ayarlari:

```text
Sunucu Ethernet IP: 192.168.50.1
SQL TCP Port: 1433
SQL Kullanici: kantar_app
SQL Sifre: KantarPro2026!
```

Script su islemleri yapar:

- Ethernet IP adresini `192.168.50.1` yapar.
- SQL Server TCP/IP ozelligini acar.
- SQL Server portunu `1433` yapar.
- Windows guvenlik duvarinda `1433` portuna izin verir.
- SQL karma kimlik dogrulamasini acar.
- `kantar_app` kullanicisini olusturur ve `KantarPro` veritabanina yetki verir.

Programda sunucu bilgisayar ayarlari:

```text
SQL Server/IP: .\SQLEXPRESS
Veritabani: KantarPro
Windows baglantisi: Isaretli
Istasyon tipi: Giris Kantari
COM Port: Giris kantarinin portu
```

## 2. Istemci bilgisayar hazirligi

Istemci bilgisayar cikis kantarinin bagli oldugu bilgisayardir.

1. `KantarPro_Istemci_Paketi` klasoru komple istemci bilgisayara kopyalanmali.
2. Sadece `.exe` dosyasi kopyalanmamalidir; yanindaki `.dll` ve `.config` dosyalari da gereklidir.
3. `tools\SetupClientNetwork.ps1` dosyasi yonetici olarak calistirilmelidir.

Varsayilan istemci ayari:

```text
Istemci Ethernet IP: 192.168.50.2
Alt ag maskesi: 255.255.255.0
Ag gecidi: Bos
DNS: Bos
```

Programda istemci bilgisayar ayarlari:

```text
SQL Server/IP: tcp:192.168.50.1,1433
Veritabani: KantarPro
Windows baglantisi: Isaretsiz
SQL Kullanici: kantar_app
SQL Sifre: KantarPro2026!
Istasyon tipi: Cikis Kantari
COM Port: Cikis kantarinin portu
```

## 3. Baglanti testi

Her iki bilgisayarda da programdaki `Ayarlar` sekmesinden `Baglantiyi Test Et` butonu kullanilir.

Beklenen sonuc:

```text
Baglanti basarili.
```

Eger istemci bilgisayarda bilgisayar adi ile baglanti basarisiz olursa IP ile baglanilmalidir:

```text
tcp:192.168.50.1,1433
```

## 4. Is akisi testi

Kurulumdan sonra asagidaki testler yapilmalidir.

### Test 1: Ortak liste testi

1. Giris bilgisayarindan `16TEST001` plakasi tartimsiz kaydedilir.
2. Cikis bilgisayarinda `Yenile` denir.
3. Plaka cikis bilgisayarinda gorunmelidir.
4. Cikis bilgisayarindan cikis yapilir.
5. Giris bilgisayarinda `Yenile` denir.
6. Cikis islemi giris bilgisayarinda gorunmelidir.

### Test 2: Tartimli dolu-bos testi

1. Giris bilgisayarindan `16TEST002` plakasi tartimli kaydedilir.
2. Cikis bilgisayarindan cikis yapilir.
3. Plaka `2. Tartim Bekleyenler` listesine dusmelidir.
4. Ayni plaka tekrar giris bilgisayarindan tartimli kaydedilir.
5. Dolu-bos formu acilmali ve net hesaplanmalidir.
6. Cikis bilgisayarindan cikis yapildiginda plaka `Kesin Cikis Yapilanlar` listesine gitmelidir.

### Test 3: Tartimsiz giris, sonradan tartim testi

1. Giris bilgisayarindan `16TEST003` plakasi tartimsiz kaydedilir.
2. Ust listede sag tik ile `Tart` secilir ve ilk tartim eklenir.
3. Cikis bilgisayarindan cikis yapilir.
4. Plaka `2. Tartim Bekleyenler` listesine dusmelidir.
5. Ayni plaka tekrar giris bilgisayarindan tartimli kaydedilir.
6. Dolu-bos formu acilmali, net hesaplanmali ve cikis sonrasi kesin cikisa gitmelidir.

## 5. Sorun giderme

### Program acilmiyor

- Program klasoru komple kopyalanmamis olabilir.
- `.dll` dosyalari `.exe` ile ayni klasorde olmalidir.
- .NET Framework 4.8 kurulu olmalidir.
- Veritabani baglantisi yoksa program Ayarlar ekranini acmalidir.

### Istemci SQL'e baglanamiyor

Sunucuda kontrol edilecekler:

```powershell
Get-Service 'MSSQL$SQLEXPRESS'
netstat -ano | Select-String ':1433'
Get-NetFirewallRule -DisplayName 'KantarPro SQL Server 1433'
```

Istemcide kontrol edilecekler:

```powershell
ping 192.168.50.1
Test-NetConnection 192.168.50.1 -Port 1433
```

### COM port yanlis

Her bilgisayarda kendi kantarinin COM portu ayarlanmalidir. Sunucudaki COM port istemciyi, istemcideki COM port sunucuyu etkilemez.
