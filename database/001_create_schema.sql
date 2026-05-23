SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'KantarPro') IS NULL
BEGIN
    CREATE DATABASE KantarPro;
END
GO

USE KantarPro;
GO

IF OBJECT_ID(N'dbo.BekleyenTartimlar', N'U') IS NOT NULL DROP TABLE dbo.BekleyenTartimlar;
IF OBJECT_ID(N'dbo.KantarDosyalari', N'U') IS NOT NULL DROP TABLE dbo.KantarDosyalari;
IF OBJECT_ID(N'dbo.Loglar', N'U') IS NOT NULL DROP TABLE dbo.Loglar;
IF OBJECT_ID(N'dbo.IslemUcretleri', N'U') IS NOT NULL DROP TABLE dbo.IslemUcretleri;
IF OBJECT_ID(N'dbo.Tartimlar', N'U') IS NOT NULL DROP TABLE dbo.Tartimlar;
IF OBJECT_ID(N'dbo.Islemler', N'U') IS NOT NULL DROP TABLE dbo.Islemler;
IF OBJECT_ID(N'dbo.Ucretler', N'U') IS NOT NULL DROP TABLE dbo.Ucretler;
IF OBJECT_ID(N'dbo.Ayarlar', N'U') IS NOT NULL DROP TABLE dbo.Ayarlar;
IF OBJECT_ID(N'dbo.Kullanicilar', N'U') IS NOT NULL DROP TABLE dbo.Kullanicilar;
IF OBJECT_ID(N'dbo.Araclar', N'U') IS NOT NULL DROP TABLE dbo.Araclar;
GO

CREATE TABLE dbo.Araclar
(
    AracId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Araclar PRIMARY KEY,
    Plaka NVARCHAR(20) NOT NULL,
    FirmaAdi NVARCHAR(150) NULL,
    AracTipi NVARCHAR(50) NULL,
    Aciklama NVARCHAR(250) NULL,
    AktifMi BIT NOT NULL CONSTRAINT DF_Araclar_AktifMi DEFAULT (1),
    OlusturmaTarihi DATETIME NOT NULL CONSTRAINT DF_Araclar_OlusturmaTarihi DEFAULT (GETDATE())
);
GO

CREATE UNIQUE INDEX UX_Araclar_Plaka ON dbo.Araclar(Plaka);
GO

CREATE TABLE dbo.Kullanicilar
(
    KullaniciId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Kullanicilar PRIMARY KEY,
    KullaniciAdi NVARCHAR(50) NOT NULL,
    ParolaHash NVARCHAR(256) NOT NULL,
    AdSoyad NVARCHAR(100) NOT NULL,
    Rol NVARCHAR(30) NOT NULL,
    AktifMi BIT NOT NULL CONSTRAINT DF_Kullanicilar_AktifMi DEFAULT (1),
    SonGirisTarihi DATETIME NULL
);
GO

CREATE UNIQUE INDEX UX_Kullanicilar_KullaniciAdi ON dbo.Kullanicilar(KullaniciAdi);
GO

CREATE TABLE dbo.Ucretler
(
    UcretId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ucretler PRIMARY KEY,
    UcretKodu NVARCHAR(30) NOT NULL,
    UcretAdi NVARCHAR(100) NOT NULL,
    Yil INT NOT NULL,
    Tutar DECIMAL(18,2) NOT NULL,
    AktifMi BIT NOT NULL CONSTRAINT DF_Ucretler_AktifMi DEFAULT (1),
    GecerlilikBaslangic DATETIME NOT NULL,
    GecerlilikBitis DATETIME NULL
);
GO

CREATE UNIQUE INDEX UX_Ucretler_Kod_Yil ON dbo.Ucretler(UcretKodu, Yil);
GO

CREATE TABLE dbo.Islemler
(
    IslemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Islemler PRIMARY KEY,
    IslemNo NVARCHAR(30) NOT NULL,
    AracId INT NOT NULL,
    GirisTarihi DATETIME NOT NULL,
    CikisTarihi DATETIME NULL,
    GelisTuru NVARCHAR(20) NOT NULL CONSTRAINT DF_Islemler_GelisTuru DEFAULT (N'Tartimsiz'),
    Durum NVARCHAR(30) NOT NULL,
    GirisKullaniciId INT NOT NULL,
    CikisKullaniciId INT NULL,
    ToplamTahakkuk DECIMAL(18,2) NOT NULL CONSTRAINT DF_Islemler_ToplamTahakkuk DEFAULT (0),
    ToplamTahsilat DECIMAL(18,2) NOT NULL CONSTRAINT DF_Islemler_ToplamTahsilat DEFAULT (0),
    Notlar NVARCHAR(500) NULL,
    CONSTRAINT FK_Islemler_Araclar FOREIGN KEY (AracId) REFERENCES dbo.Araclar(AracId),
    CONSTRAINT FK_Islemler_GirisKullanicilar FOREIGN KEY (GirisKullaniciId) REFERENCES dbo.Kullanicilar(KullaniciId),
    CONSTRAINT FK_Islemler_CikisKullanicilar FOREIGN KEY (CikisKullaniciId) REFERENCES dbo.Kullanicilar(KullaniciId)
);
GO

CREATE UNIQUE INDEX UX_Islemler_IslemNo ON dbo.Islemler(IslemNo);
CREATE UNIQUE INDEX UX_Islemler_Iceride_Arac ON dbo.Islemler(AracId) WHERE Durum = N'Iceride';
CREATE INDEX IX_Islemler_Durum_GirisTarihi ON dbo.Islemler(Durum, GirisTarihi);
GO

CREATE TABLE dbo.Tartimlar
(
    TartimId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tartimlar PRIMARY KEY,
    IslemId INT NULL,
    AracId INT NOT NULL,
    TartimTipi NVARCHAR(30) NOT NULL,
    YukDurumu NVARCHAR(20) NULL,
    AgirlikKg DECIMAL(18,2) NOT NULL,
    TartimTarihi DATETIME NOT NULL,
    ComPorttanAlindiMi BIT NOT NULL CONSTRAINT DF_Tartimlar_ComPorttanAlindiMi DEFAULT (0),
    ManuelMi BIT NOT NULL CONSTRAINT DF_Tartimlar_ManuelMi DEFAULT (0),
    KullaniciId INT NOT NULL,
    FisYazdirildiMi BIT NOT NULL CONSTRAINT DF_Tartimlar_FisYazdirildiMi DEFAULT (0),
    CONSTRAINT FK_Tartimlar_Islemler FOREIGN KEY (IslemId) REFERENCES dbo.Islemler(IslemId),
    CONSTRAINT FK_Tartimlar_Araclar FOREIGN KEY (AracId) REFERENCES dbo.Araclar(AracId),
    CONSTRAINT FK_Tartimlar_Kullanicilar FOREIGN KEY (KullaniciId) REFERENCES dbo.Kullanicilar(KullaniciId)
);
GO

CREATE INDEX IX_Tartimlar_Arac_Tarih ON dbo.Tartimlar(AracId, TartimTarihi);
CREATE INDEX IX_Tartimlar_Islem_Tip ON dbo.Tartimlar(IslemId, TartimTipi);
GO

CREATE TABLE dbo.IslemUcretleri
(
    IslemUcretId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IslemUcretleri PRIMARY KEY,
    IslemId INT NOT NULL,
    UcretId INT NOT NULL,
    UcretAdi NVARCHAR(100) NOT NULL,
    Tutar DECIMAL(18,2) NOT NULL,
    TahakkukTarihi DATETIME NOT NULL,
    TahsilEdildiMi BIT NOT NULL CONSTRAINT DF_IslemUcretleri_TahsilEdildiMi DEFAULT (0),
    TahsilTarihi DATETIME NULL,
    TahsilEdenKullaniciId INT NULL,
    FaturaId NVARCHAR(40) NULL,
    TahsilatId NVARCHAR(40) NULL,
    CONSTRAINT FK_IslemUcretleri_Islemler FOREIGN KEY (IslemId) REFERENCES dbo.Islemler(IslemId),
    CONSTRAINT FK_IslemUcretleri_Ucretler FOREIGN KEY (UcretId) REFERENCES dbo.Ucretler(UcretId),
    CONSTRAINT FK_IslemUcretleri_TahsilEden FOREIGN KEY (TahsilEdenKullaniciId) REFERENCES dbo.Kullanicilar(KullaniciId)
);
GO

CREATE INDEX IX_IslemUcretleri_Islem ON dbo.IslemUcretleri(IslemId);
GO

CREATE TABLE dbo.Loglar
(
    LogId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Loglar PRIMARY KEY,
    KullaniciId INT NULL,
    IslemId INT NULL,
    LogTipi NVARCHAR(30) NOT NULL,
    Mesaj NVARCHAR(250) NOT NULL,
    Detay NVARCHAR(MAX) NULL,
    Tarih DATETIME NOT NULL CONSTRAINT DF_Loglar_Tarih DEFAULT (GETDATE()),
    BilgisayarAdi NVARCHAR(100) NULL,
    CONSTRAINT FK_Loglar_Kullanicilar FOREIGN KEY (KullaniciId) REFERENCES dbo.Kullanicilar(KullaniciId),
    CONSTRAINT FK_Loglar_Islemler FOREIGN KEY (IslemId) REFERENCES dbo.Islemler(IslemId)
);
GO

CREATE INDEX IX_Loglar_Tarih ON dbo.Loglar(Tarih);
GO

CREATE TABLE dbo.BekleyenTartimlar
(
    BekleyenTartimId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BekleyenTartimlar PRIMARY KEY,
    AracId INT NOT NULL,
    IlkTartimId INT NOT NULL,
    IlkAgirlikKg DECIMAL(18,2) NOT NULL,
    IlkTartimTarihi DATETIME NOT NULL,
    Durum NVARCHAR(30) NOT NULL,
    TamamlayanTartimId INT NULL,
    CONSTRAINT FK_BekleyenTartimlar_Araclar FOREIGN KEY (AracId) REFERENCES dbo.Araclar(AracId),
    CONSTRAINT FK_BekleyenTartimlar_IlkTartim FOREIGN KEY (IlkTartimId) REFERENCES dbo.Tartimlar(TartimId),
    CONSTRAINT FK_BekleyenTartimlar_TamamlayanTartim FOREIGN KEY (TamamlayanTartimId) REFERENCES dbo.Tartimlar(TartimId)
);
GO

CREATE INDEX IX_BekleyenTartimlar_Arac_Durum ON dbo.BekleyenTartimlar(AracId, Durum);
GO

CREATE TABLE dbo.KantarDosyalari
(
    KantarDosyasiId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_KantarDosyalari PRIMARY KEY,
    AracId INT NOT NULL,
    IlkTartimId INT NOT NULL,
    KarsiTartimId INT NULL,
    Durum NVARCHAR(30) NOT NULL,
    NetAgirlikKg DECIMAL(18,2) NULL,
    OlusturmaTarihi DATETIME NOT NULL,
    TamamlanmaTarihi DATETIME NULL,
    CONSTRAINT FK_KantarDosyalari_Araclar FOREIGN KEY (AracId) REFERENCES dbo.Araclar(AracId),
    CONSTRAINT FK_KantarDosyalari_IlkTartim FOREIGN KEY (IlkTartimId) REFERENCES dbo.Tartimlar(TartimId),
    CONSTRAINT FK_KantarDosyalari_KarsiTartim FOREIGN KEY (KarsiTartimId) REFERENCES dbo.Tartimlar(TartimId)
);
GO

CREATE INDEX IX_KantarDosyalari_Arac_Durum ON dbo.KantarDosyalari(AracId, Durum);
GO

CREATE TABLE dbo.Ayarlar
(
    AyarId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ayarlar PRIMARY KEY,
    AyarAnahtari NVARCHAR(80) NOT NULL,
    AyarDegeri NVARCHAR(500) NULL,
    AyarGrubu NVARCHAR(50) NOT NULL,
    Aciklama NVARCHAR(250) NULL,
    GuncellemeTarihi DATETIME NOT NULL CONSTRAINT DF_Ayarlar_GuncellemeTarihi DEFAULT (GETDATE())
);
GO

CREATE UNIQUE INDEX UX_Ayarlar_AyarAnahtari ON dbo.Ayarlar(AyarAnahtari);
GO

DECLARE @Yil INT;
SET @Yil = YEAR(GETDATE());

INSERT INTO dbo.Ucretler (UcretKodu, UcretAdi, Yil, Tutar, GecerlilikBaslangic)
VALUES
(N'GIRIS_CIKIS', N'Giris-Cikis Ucreti', @Yil, 366.00, DATEADD(yy, DATEDIFF(yy, 0, GETDATE()), 0)),
(N'TARTIM', N'Tartim Ucreti', @Yil, 366.00, DATEADD(yy, DATEDIFF(yy, 0, GETDATE()), 0)),
(N'BEKLEME', N'Bekleme Ucreti', @Yil, 816.00, DATEADD(yy, DATEDIFF(yy, 0, GETDATE()), 0));

INSERT INTO dbo.Ayarlar (AyarAnahtari, AyarDegeri, AyarGrubu, Aciklama)
VALUES
(N'ComPort', N'COM1', N'COM', N'BAYKON indikator seri port adi'),
(N'BaudRate', N'9600', N'COM', N'Seri port baud rate'),
(N'Parity', N'None', N'COM', N'Seri port parity'),
(N'StopBits', N'One', N'COM', N'Seri port stop bits'),
(N'DataBits', N'8', N'COM', N'Seri port data bits'),
(N'AgirlikRegex', N'[-+]?[0-9]+([,.][0-9]+)?', N'COM', N'Gelen frame icinden agirlik ayiklama deseni'),
(N'YaziciPaylasimYolu', N'\\SERVER\OKI5720', N'Yazici', N'OKI 5720 ag paylasim yolu'),
(N'FisSatirSayisi', N'40', N'Yazici', N'Nokta vuruslu fis satir sayisi'),
(N'KurumAdi', N'Bursa Tasfiye Isletme Mudurlugu', N'Yazici', N'Fis basligi'),
(N'Tema', N'Light', N'Sistem', N'Varsayilan tema'),
(N'OtomatikYedeklemeAktifMi', N'1', N'Sistem', N'Otomatik SQL yedekleme durumu'),
(N'YedeklemeKlasoru', N'C:\KantarPro\Yedekler', N'Sistem', N'Veritabani yedek klasoru');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Kullanicilar WHERE KullaniciAdi = N'admin')
BEGIN
    INSERT INTO dbo.Kullanicilar (KullaniciAdi, ParolaHash, AdSoyad, Rol, AktifMi)
    VALUES (N'admin', N'DEVELOPMENT_PLACEHOLDER_HASH', N'Admin Kullanici', N'Admin', 1);
END
GO
