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

    IF COL_LENGTH(N'dbo.Islemler', N'MuafMi') IS NULL
    BEGIN
        ALTER TABLE dbo.Islemler
        ADD MuafMi BIT NOT NULL CONSTRAINT DF_Islemler_MuafMi DEFAULT (0);
    END

    IF COL_LENGTH(N'dbo.Islemler', N'MuafiyetNedeni') IS NULL
    BEGIN
        ALTER TABLE dbo.Islemler
        ADD MuafiyetNedeni NVARCHAR(250) NULL;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.__SchemaVersions WHERE Version = N'005')
    BEGIN
        INSERT INTO dbo.__SchemaVersions (Version, Aciklama)
        VALUES (N'005', N'Muafiyet alanlari');
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
    RAISERROR('005_add_muaf_kolonlari basarisiz: %s (Satir: %d)', 16, 1, @HataMesaj, @HataSatir);
END CATCH
GO
