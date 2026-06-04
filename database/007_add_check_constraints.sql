SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'KantarPro') IS NULL
BEGIN
    RAISERROR('KantarPro veritabani bulunamadi. Once temel sema scriptlerini calistirin.', 16, 1);
    RETURN;
END
GO

USE KantarPro;
GO

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

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Ucretler_Tutar')
    BEGIN
        ALTER TABLE dbo.Ucretler WITH CHECK ADD CONSTRAINT CK_Ucretler_Tutar CHECK (Tutar >= 0);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Ucretler_Yil')
    BEGIN
        ALTER TABLE dbo.Ucretler WITH CHECK ADD CONSTRAINT CK_Ucretler_Yil CHECK (Yil BETWEEN 2000 AND 2100);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Islemler_ToplamTahakkuk')
    BEGIN
        ALTER TABLE dbo.Islemler WITH CHECK ADD CONSTRAINT CK_Islemler_ToplamTahakkuk CHECK (ToplamTahakkuk >= 0);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Islemler_ToplamTahsilat')
    BEGIN
        ALTER TABLE dbo.Islemler WITH CHECK ADD CONSTRAINT CK_Islemler_ToplamTahsilat CHECK (ToplamTahsilat >= 0);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Tartimlar_AgirlikKg')
    BEGIN
        ALTER TABLE dbo.Tartimlar WITH CHECK ADD CONSTRAINT CK_Tartimlar_AgirlikKg CHECK (AgirlikKg > 0 AND AgirlikKg < 100000);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_IslemUcretleri_Tutar')
    BEGIN
        ALTER TABLE dbo.IslemUcretleri WITH CHECK ADD CONSTRAINT CK_IslemUcretleri_Tutar CHECK (Tutar >= 0);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_BekleyenTartimlar_IlkAgirlikKg')
    BEGIN
        ALTER TABLE dbo.BekleyenTartimlar WITH CHECK ADD CONSTRAINT CK_BekleyenTartimlar_IlkAgirlikKg CHECK (IlkAgirlikKg > 0 AND IlkAgirlikKg < 100000);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_KantarDosyalari_NetAgirlikKg')
    BEGIN
        ALTER TABLE dbo.KantarDosyalari WITH CHECK ADD CONSTRAINT CK_KantarDosyalari_NetAgirlikKg CHECK (NetAgirlikKg IS NULL OR NetAgirlikKg >= 0);
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.__SchemaVersions WHERE Version = N'007')
    BEGIN
        INSERT INTO dbo.__SchemaVersions (Version, Aciklama)
        VALUES (N'007', N'Veri butunlugu check constraintleri');
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
    RAISERROR('007_add_check_constraints basarisiz: %s (Satir: %d)', 16, 1, @HataMesaj, @HataSatir);
END CATCH
GO
