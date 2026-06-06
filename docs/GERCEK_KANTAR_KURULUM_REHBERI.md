# KantarPro Gercek Kantar Kurulum Rehberi

Bu rehber, programin gercek kantar sahasinda iki bilgisayara kurulmasi icindir.

## Bilgisayar yapisi

- Server bilgisayar: Windows 10, giris kantari bilgisayari.
- SQL Server: Server bilgisayarda SQL Server 2008 Express olabilir.
- Client bilgisayar: Windows 7, cikis kantari bilgisayari.
- Client bilgisayara SQL Server kurulmasi gerekmez.
- Iki bilgisayar ortak SQL veritabanini kullanir.

## Paket mantigi

Kurulum paketi su klasorlerden olusur:

```text
KantarPro_Kurulum_Paketi
|-- Program
|-- Kurulum
|-- Veritabani
|-- Dokumanlar
|-- PAKET_BILGISI.txt
```

Gercek bilgisayara `Program` klasoru komple kopyalanir. Sadece `.exe` dosyasini kopyalamak dogru degildir.

Yerel ayarlar her bilgisayarda ayri saklanir:

```text
%AppData%\KantarPro\station-settings.ini
```

Bu dosyada SQL server adresi, veritabani adi, istasyon tipi ve COM port bilgisi tutulur. Program guncellendiginde bu dosya silinmemelidir.

## Kurulum oncesi kontrol

Server bilgisayarda:

- SQL Server Express calisiyor olmali.
- SQL instance adi genellikle `SQLEXPRESS` olur.
- Eski kantar programi kullaniliyorsa kurulum mesai disinda yapilmalidir.
- Eski veritabanindan veya bilgisayardan yedek alinmalidir.

Client bilgisayarda:

- Windows 7 icin `.NET Framework 4.8` kurulu olmalidir.
- Kantar COM port surucusu kurulu olmalidir.
- OKI ML5720 yazici surucusu kurulu olmalidir.

## Paket nasil uretilir?

Paket gelistirme bilgisayarinda uretilir. Gercek kantar bilgisayarinda Visual Studio gerekmez.

PowerShell:

```powershell
.\tools\BuildReleasePackage.ps1
```

Olusan paket:

```text
dist\KantarPro_Kurulum_Paketi_YYYYAAGG_SSDD
dist\KantarPro_Kurulum_Paketi_YYYYAAGG_SSDD.zip
```

## Server bilgisayar kurulumu

1. Paketi server bilgisayara kopyalayin.
2. `Program` klasorunu uygun bir yere kopyalayin:

```text
C:\KantarPro\Program
```

3. SQL/IP ayarlari otomatik yapilacaksa PowerShell'i yonetici olarak acin ve calistirin:

```powershell
cd C:\KantarPro\Kurulum
.\Server_Ag_SQL_Ayarla.ps1 -EthernetAlias "Ethernet" -ServerIp "GERCEK_SERVER_IP" -SqlInstanceName "SQLEXPRESS"
```

Ornek:

```powershell
.\Server_Ag_SQL_Ayarla.ps1 -EthernetAlias "Ethernet" -ServerIp "192.168.1.25" -SqlInstanceName "SQLEXPRESS"
```

SQL Server 2008 Express icin bu script SQL surum numarasina bagli degildir. Instance adini registry uzerinden bulmaya calisir.

4. Programi acin:

```text
C:\KantarPro\Program\KantarPro.Desktop.exe
```

5. Ayarlar ekraninda server bilgisayar icin:

```text
SQL Server/IP       : .\SQLEXPRESS
Veritabani          : KantarPro
Windows baglantisi  : Isaretli
Istasyon tipi       : Giris Kantari
COM Port            : Giris kantarinin COM portu
```

6. `Baglantiyi Test Et` ile kontrol edin.

## Client bilgisayar kurulumu

1. Ayni paketi client bilgisayara kopyalayin.
2. `Program` klasorunu uygun bir yere kopyalayin:

```text
C:\KantarPro\Program
```

3. Ag ayari otomatik yapilacaksa PowerShell'i yonetici olarak acin:

```powershell
cd C:\KantarPro\Kurulum
.\Client_Ag_Ayarla.ps1 -EthernetAlias "Ethernet" -ClientIp "GERCEK_CLIENT_IP"
```

4. Programi acin.
5. Ayarlar ekraninda client bilgisayar icin:

```text
SQL Server/IP       : tcp:GERCEK_SERVER_IP,1433
Veritabani          : KantarPro
Windows baglantisi  : Isaretsiz
SQL Kullanici       : kantar_app
SQL Sifre           : Server scriptinde verilen sifre
Istasyon tipi       : Cikis Kantari
COM Port            : Cikis kantarinin COM portu
```

Ornek:

```text
SQL Server/IP: tcp:192.168.1.25,1433
```

## IP adresleri sabit degilse

Paket IP adresine bagli degildir. Gercek sahada server IP adresi neyse client tarafinda o IP yazilir.

Ornek:

```text
Server IP: 10.0.0.12
Client SQL Server/IP: tcp:10.0.0.12,1433
```

## Veritabani kurulumu

Yeni kurulumda `Veritabani` klasorundeki SQL dosyalari sira ile calistirilir.

Temel sira:

```text
000_create_migration_history.sql
001_create_schema.sql
002_add_saha_ziyareti_kantar_dosyasi.sql
004_add_fatura_id.sql
005_add_muaf_kolonlari.sql
007_add_check_constraints.sql
008_add_cikis_no.sql
```

`003_seed_random_demo_data.sql` gercek sahada calistirilmez. Bu dosya sadece demo/veri denemeleri icindir.

`006_seed_first_admin.sql` hash degeri el ile hazirlanmadan calistirilmez. Program bos kullanici tablosunda varsayilan admin/memur kullanicilarini olusturacak sekilde tasarlanmistir.

## Guncelleme nasil yapilir?

Programda degisiklik gerekirse:

1. Degisiklik gelistirme bilgisayarinda yapilir.
2. Testler gecirilir.
3. Yeni paket uretilir.
4. Gercek bilgisayarda program kapatilir.
5. Eski `C:\KantarPro\Program` klasoru yedeklenir.
6. Yeni paketteki `Program` klasoru onun yerine kopyalanir.
7. `%AppData%\KantarPro\station-settings.ini` silinmez.

Boylece SQL/IP/COM/istasyon ayarlari korunur.

## Kurulum sonrasi prova

1. Server bilgisayardan tartimsiz giris yapin.
2. Client bilgisayarda kaydin gorundugunu kontrol edin.
3. Client bilgisayardan cikis yapin.
4. Server bilgisayarda cikisin gorundugunu kontrol edin.
5. Tartimli giris yapin ve kantar kilosunun anlik geldigini kontrol edin.
6. Kantar fisi yazdirin.
7. Gunluk tahsilat listesini ve PDF cikisini kontrol edin.

## Sorun giderme

Client SQL'e baglanamazsa:

```powershell
ping GERCEK_SERVER_IP
Test-NetConnection GERCEK_SERVER_IP -Port 1433
```

Server uzerinde SQL portu dinliyor mu:

```powershell
netstat -ano | Select-String ":1433"
```

COM port gorunmuyorsa:

- Aygit Yoneticisi acilir.
- Baglanti noktalari (COM ve LPT) altinda COM numarasi kontrol edilir.
- Program Ayarlar ekraninda ayni COM port secilir.

Yazici basmiyorsa:

- OKI ML5720 surucusu kontrol edilir.
- Windows'tan sinama sayfasi basilabilir.
- Programda Makbuz/Goster ve Yazdir akisi test edilir.
