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

    IF COL_LENGTH(N'dbo.IslemUcretleri', N'FaturaId') IS NULL
    BEGIN
        ALTER TABLE dbo.IslemUcretleri ADD FaturaId NVARCHAR(40) NULL;
    END

    IF COL_LENGTH(N'dbo.IslemUcretleri', N'TahsilatNo') IS NULL
    BEGIN
        ALTER TABLE dbo.IslemUcretleri ADD TahsilatNo NVARCHAR(20) NULL;
    END

    IF COL_LENGTH(N'dbo.IslemUcretleri', N'OdemeTuru') IS NULL
    BEGIN
        ALTER TABLE dbo.IslemUcretleri ADD OdemeTuru NVARCHAR(30) NULL;
    END

    ;WITH TahsilEdilenler AS
    (
        SELECT
            IslemUcretId,
            FaturaId,
            'FATESKI' +
                RIGHT('0000000000' + CAST(IslemId AS VARCHAR(12)), 10) +
                CONVERT(NVARCHAR(8), TahsilTarihi, 112) +
                REPLACE(CONVERT(NVARCHAR(8), TahsilTarihi, 108), ':', '') +
                RIGHT('00000000' + CAST(IslemUcretId AS VARCHAR(12)), 8) AS YeniFaturaId
        FROM dbo.IslemUcretleri
        WHERE TahsilEdildiMi = 1
          AND TahsilTarihi IS NOT NULL
          AND (FaturaId IS NULL OR FaturaId LIKE N'FATESKI%')
    )
    UPDATE iu
    SET FaturaId = t.YeniFaturaId
    FROM dbo.IslemUcretleri iu
    INNER JOIN TahsilEdilenler t ON t.IslemUcretId = iu.IslemUcretId;

    UPDATE dbo.IslemUcretleri
    SET TahsilatNo = RIGHT('0000' + CAST(IslemId AS NVARCHAR(12)), 4)
    WHERE TahsilEdildiMi = 1
      AND TahsilTarihi IS NOT NULL
      AND (TahsilatNo IS NULL OR TahsilatNo = N'');

    UPDATE dbo.IslemUcretleri
    SET OdemeTuru = N'Nakit'
    WHERE TahsilEdildiMi = 1
      AND TahsilTarihi IS NOT NULL
      AND (OdemeTuru IS NULL OR OdemeTuru = N'');

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_IslemUcretleri_FaturaId' AND object_id = OBJECT_ID(N'dbo.IslemUcretleri'))
    BEGIN
        DROP INDEX IX_IslemUcretleri_FaturaId ON dbo.IslemUcretleri;
    END

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_IslemUcretleri_FaturaId' AND object_id = OBJECT_ID(N'dbo.IslemUcretleri'))
    BEGIN
        DROP INDEX UX_IslemUcretleri_FaturaId ON dbo.IslemUcretleri;
    END

    CREATE INDEX IX_IslemUcretleri_FaturaId
    ON dbo.IslemUcretleri(FaturaId)
    WHERE FaturaId IS NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_IslemUcretleri_TahsilatNo' AND object_id = OBJECT_ID(N'dbo.IslemUcretleri'))
    BEGIN
        CREATE INDEX IX_IslemUcretleri_TahsilatNo
        ON dbo.IslemUcretleri(TahsilatNo)
        WHERE TahsilatNo IS NOT NULL;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.__SchemaVersions WHERE Version = N'004')
    BEGIN
        INSERT INTO dbo.__SchemaVersions (Version, Aciklama)
        VALUES (N'004', N'Fatura, tahsilat no ve odeme turu alanlari');
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
    RAISERROR('004_add_fatura_id basarisiz: %s (Satir: %d)', 16, 1, @HataMesaj, @HataSatir);
END CATCH
GO
