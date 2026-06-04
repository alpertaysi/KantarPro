SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'KantarPro') IS NULL
BEGIN
    RAISERROR('KantarPro veritabani bulunamadi. Once 000_create_migration_history.sql ve 001_create_schema.sql dosyalarini calistirin.', 16, 1);
    RETURN;
END
GO

USE KantarPro;
GO

DECLARE @AdminKullaniciAdi NVARCHAR(50);
DECLARE @AdminParolaHash NVARCHAR(256);

SET @AdminKullaniciAdi = N'admin';
SET @AdminParolaHash = N'BURAYA_GERCEK_PAROLA_HASHI_YAZIN';

IF @AdminParolaHash = N'BURAYA_GERCEK_PAROLA_HASHI_YAZIN'
BEGIN
    RAISERROR('Admin kullanicisi olusturulmadan once @AdminParolaHash degeri gercek parola hash degeriyle degistirilmelidir.', 16, 1);
    RETURN;
END

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

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

    IF NOT EXISTS (SELECT 1 FROM dbo.Kullanicilar WHERE KullaniciAdi = @AdminKullaniciAdi)
    BEGIN
        INSERT INTO dbo.Kullanicilar (KullaniciAdi, ParolaHash, AdSoyad, Rol, AktifMi)
        VALUES (@AdminKullaniciAdi, @AdminParolaHash, N'Sistem Yoneticisi', N'Admin', 1);
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.__SchemaVersions WHERE Version = N'006')
    BEGIN
        INSERT INTO dbo.__SchemaVersions (Version, Aciklama)
        VALUES (N'006', N'Ilk admin kullanicisi seed scripti');
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
    RAISERROR('006_seed_first_admin basarisiz: %s (Satir: %d)', 16, 1, @HataMesaj, @HataSatir);
END CATCH
GO
