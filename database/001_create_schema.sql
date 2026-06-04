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

-- DIKKAT: Bu script yeni kurulum veya bilerek sifirlama icindir.
-- Mevcut core tablolar varsa @OnayliSil = 1 yapilmadan calismaz.
DECLARE @OnayliSil BIT;
SET @OnayliSil = 0;

IF @OnayliSil = 0
   AND EXISTS (
       SELECT 1
       FROM sys.objects
       WHERE type = 'U'
         AND schema_id = SCHEMA_ID(N'dbo')
         AND name IN (
             N'Araclar', N'Kullanicilar', N'Ucretler', N'Islemler',
             N'Tartimlar', N'IslemUcretleri', N'Loglar',
             N'BekleyenTartimlar', N'KantarDosyalari', N'Ayarlar'
         )
   )
BEGIN
    RAISERROR('KantarPro sema tablolari zaten mevcut. Bu script tablolari DROP eder. Devam etmek icin script basindaki @OnayliSil degerini 1 yapin.', 16, 1);
    RETURN;
END

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- FK bagimliligi olan tablolar once drop edilir.
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

    IF OBJECT_ID(N'dbo.__SchemaVersions', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.__SchemaVersions
        (
            Version NVARCHAR(20) NOT NULL CONSTRAINT PK___SchemaVersions PRIMARY KEY,
            Aciklama NVARCHAR(250) NULL,
            AppliedAt DATETIME NOT NULL CONSTRAINT DF___SchemaVersions_AppliedAt DEFAULT (GETDATE()),
            AppliedBy NVARCHAR(100) NOT NULL CONSTRAINT DF___SchemaVersions_AppliedBy DEFAULT (SUSER_SNAME())
        );
    END

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

    CREATE UNIQUE INDEX UX_Araclar_Plaka ON dbo.Araclar(Plaka);

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

    CREATE UNIQUE INDEX UX_Kullanicilar_KullaniciAdi ON dbo.Kullanicilar(KullaniciAdi);

    CREATE TABLE dbo.Ucretler
    (
        UcretId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ucretler PRIMARY KEY,
        UcretKodu NVARCHAR(30) NOT NULL,
        UcretAdi NVARCHAR(100) NOT NULL,
        Yil INT NOT NULL,
        Tutar DECIMAL(18,2) NOT NULL,
        AktifMi BIT NOT NULL CONSTRAINT DF_Ucretler_AktifMi DEFAULT (1),
        GecerlilikBaslangic DATETIME NOT NULL,
        GecerlilikBitis DATETIME NULL,
        CONSTRAINT CK_Ucretler_Tutar CHECK (Tutar >= 0),
        CONSTRAINT CK_Ucretler_Yil CHECK (Yil BETWEEN 2000 AND 2100)
    );

    CREATE UNIQUE INDEX UX_Ucretler_Kod_Yil ON dbo.Ucretler(UcretKodu, Yil);

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
        MuafMi BIT NOT NULL CONSTRAINT DF_Islemler_MuafMi DEFAULT (0),
        MuafiyetNedeni NVARCHAR(250) NULL,
        Notlar NVARCHAR(500) NULL,
        CONSTRAINT CK_Islemler_ToplamTahakkuk CHECK (ToplamTahakkuk >= 0),
        CONSTRAINT CK_Islemler_ToplamTahsilat CHECK (ToplamTahsilat >= 0),
        CONSTRAINT FK_Islemler_Araclar FOREIGN KEY (AracId) REFERENCES dbo.Araclar(AracId),
        CONSTRAINT FK_Islemler_GirisKullanicilar FOREIGN KEY (GirisKullaniciId) REFERENCES dbo.Kullanicilar(KullaniciId),
        CONSTRAINT FK_Islemler_CikisKullanicilar FOREIGN KEY (CikisKullaniciId) REFERENCES dbo.Kullanicilar(KullaniciId)
    );

    CREATE UNIQUE INDEX UX_Islemler_IslemNo ON dbo.Islemler(IslemNo);
    CREATE UNIQUE INDEX UX_Islemler_Iceride_Arac ON dbo.Islemler(AracId) WHERE Durum = N'Iceride';
    CREATE INDEX IX_Islemler_Durum_GirisTarihi ON dbo.Islemler(Durum, GirisTarihi);

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
        CONSTRAINT CK_Tartimlar_AgirlikKg CHECK (AgirlikKg > 0 AND AgirlikKg < 100000),
        CONSTRAINT FK_Tartimlar_Islemler FOREIGN KEY (IslemId) REFERENCES dbo.Islemler(IslemId),
        CONSTRAINT FK_Tartimlar_Araclar FOREIGN KEY (AracId) REFERENCES dbo.Araclar(AracId),
        CONSTRAINT FK_Tartimlar_Kullanicilar FOREIGN KEY (KullaniciId) REFERENCES dbo.Kullanicilar(KullaniciId)
    );

    CREATE INDEX IX_Tartimlar_Arac_Tarih ON dbo.Tartimlar(AracId, TartimTarihi);
    CREATE INDEX IX_Tartimlar_Islem_Tip ON dbo.Tartimlar(IslemId, TartimTipi);

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
        TahsilatNo NVARCHAR(20) NULL,
        OdemeTuru NVARCHAR(30) NULL,
        CONSTRAINT CK_IslemUcretleri_Tutar CHECK (Tutar >= 0),
        CONSTRAINT FK_IslemUcretleri_Islemler FOREIGN KEY (IslemId) REFERENCES dbo.Islemler(IslemId),
        CONSTRAINT FK_IslemUcretleri_Ucretler FOREIGN KEY (UcretId) REFERENCES dbo.Ucretler(UcretId),
        CONSTRAINT FK_IslemUcretleri_TahsilEden FOREIGN KEY (TahsilEdenKullaniciId) REFERENCES dbo.Kullanicilar(KullaniciId)
    );

    CREATE INDEX IX_IslemUcretleri_Islem ON dbo.IslemUcretleri(IslemId);
    CREATE UNIQUE INDEX UX_IslemUcretleri_FaturaId ON dbo.IslemUcretleri(FaturaId) WHERE FaturaId IS NOT NULL;
    CREATE INDEX IX_IslemUcretleri_TahsilatNo ON dbo.IslemUcretleri(TahsilatNo) WHERE TahsilatNo IS NOT NULL;

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

    CREATE INDEX IX_Loglar_Tarih ON dbo.Loglar(Tarih);

    CREATE TABLE dbo.BekleyenTartimlar
    (
        BekleyenTartimId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BekleyenTartimlar PRIMARY KEY,
        AracId INT NOT NULL,
        IlkTartimId INT NOT NULL,
        IlkAgirlikKg DECIMAL(18,2) NOT NULL,
        IlkTartimTarihi DATETIME NOT NULL,
        Durum NVARCHAR(30) NOT NULL,
        TamamlayanTartimId INT NULL,
        CONSTRAINT CK_BekleyenTartimlar_IlkAgirlikKg CHECK (IlkAgirlikKg > 0 AND IlkAgirlikKg < 100000),
        CONSTRAINT FK_BekleyenTartimlar_Araclar FOREIGN KEY (AracId) REFERENCES dbo.Araclar(AracId),
        CONSTRAINT FK_BekleyenTartimlar_IlkTartim FOREIGN KEY (IlkTartimId) REFERENCES dbo.Tartimlar(TartimId),
        CONSTRAINT FK_BekleyenTartimlar_TamamlayanTartim FOREIGN KEY (TamamlayanTartimId) REFERENCES dbo.Tartimlar(TartimId)
    );

    CREATE INDEX IX_BekleyenTartimlar_Arac_Durum ON dbo.BekleyenTartimlar(AracId, Durum);

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
        CONSTRAINT CK_KantarDosyalari_NetAgirlikKg CHECK (NetAgirlikKg IS NULL OR NetAgirlikKg >= 0),
        CONSTRAINT FK_KantarDosyalari_Araclar FOREIGN KEY (AracId) REFERENCES dbo.Araclar(AracId),
        CONSTRAINT FK_KantarDosyalari_IlkTartim FOREIGN KEY (IlkTartimId) REFERENCES dbo.Tartimlar(TartimId),
        CONSTRAINT FK_KantarDosyalari_KarsiTartim FOREIGN KEY (KarsiTartimId) REFERENCES dbo.Tartimlar(TartimId)
    );

    CREATE INDEX IX_KantarDosyalari_Arac_Durum ON dbo.KantarDosyalari(AracId, Durum);

    CREATE TABLE dbo.Ayarlar
    (
        AyarId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ayarlar PRIMARY KEY,
        AyarAnahtari NVARCHAR(80) NOT NULL,
        AyarDegeri NVARCHAR(500) NULL,
        AyarGrubu NVARCHAR(50) NOT NULL,
        Aciklama NVARCHAR(250) NULL,
        GuncellemeTarihi DATETIME NOT NULL CONSTRAINT DF_Ayarlar_GuncellemeTarihi DEFAULT (GETDATE())
    );

    CREATE UNIQUE INDEX UX_Ayarlar_AyarAnahtari ON dbo.Ayarlar(AyarAnahtari);

    DECLARE @Yil INT;
    SET @Yil = YEAR(GETDATE());

    INSERT INTO dbo.Ucretler (UcretKodu, UcretAdi, Yil, Tutar, GecerlilikBaslangic)
    VALUES
    (N'GIRIS_CIKIS', N'Giris-Cikis Ucreti', @Yil, 366.00, DATEADD(yy, DATEDIFF(yy, 0, GETDATE()), 0)),
    (N'TARTIM', N'Tartim Ucreti', @Yil, 366.00, DATEADD(yy, DATEDIFF(yy, 0, GETDATE()), 0)),
    (N'BEKLEME', N'Bekleme Ucreti', @Yil, 816.00, DATEADD(yy, DATEDIFF(yy, 0, GETDATE()), 0));

    INSERT INTO dbo.Ayarlar (AyarAnahtari, AyarDegeri, AyarGrubu, Aciklama)
    VALUES
    (N'ComPort', N'', N'COM', N'BAYKON indikator seri port adi; kurulumda program Ayarlar ekranindan secilmelidir'),
    (N'BaudRate', N'9600', N'COM', N'Seri port baud rate'),
    (N'Parity', N'None', N'COM', N'Seri port parity'),
    (N'StopBits', N'One', N'COM', N'Seri port stop bits'),
    (N'DataBits', N'8', N'COM', N'Seri port data bits'),
    (N'AgirlikRegex', N'[-+]?[0-9]+([,.][0-9]+)?', N'COM', N'Gelen frame icinden agirlik ayiklama deseni'),
    (N'YaziciPaylasimYolu', N'BURAYA_YAZICI_PAYLASIM_YOLU', N'Yazici', N'Kurulum sonrasi gercek OKI 5720 paylasim yolu ile guncellenmelidir'),
    (N'FisSatirSayisi', N'40', N'Yazici', N'Nokta vuruslu fis satir sayisi'),
    (N'KurumAdi', N'Bursa Tasfiye Isletme Mudurlugu', N'Yazici', N'Fis basligi'),
    (N'Tema', N'Light', N'Sistem', N'Varsayilan tema'),
    (N'OtomatikYedeklemeAktifMi', N'1', N'Sistem', N'Otomatik SQL yedekleme durumu'),
    (N'YedeklemeKlasoru', N'C:\KantarPro\Yedekler', N'Sistem', N'Veritabani yedek klasoru');

    IF NOT EXISTS (SELECT 1 FROM dbo.__SchemaVersions WHERE Version = N'001')
    BEGIN
        INSERT INTO dbo.__SchemaVersions (Version, Aciklama)
        VALUES (N'001', N'Yeni kurulum semasi');
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END

    DECLARE @HataMesaj NVARCHAR(4000);
    DECLARE @HataSatir INT;
    SET @HataMesaj = ERROR_MESSAGE();
    SET @HataSatir = ERROR_LINE();
    RAISERROR('001_create_schema basarisiz: %s (Satir: %d)', 16, 1, @HataMesaj, @HataSatir);
END CATCH
GO
