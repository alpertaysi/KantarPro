SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE KantarPro;
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.IslemUcretleri', N'U') IS NULL
    BEGIN
        RAISERROR('IslemUcretleri tablosu bulunamadi.', 16, 1);
    END

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UX_IslemUcretleri_FaturaId'
          AND object_id = OBJECT_ID(N'dbo.IslemUcretleri')
    )
    BEGIN
        DROP INDEX UX_IslemUcretleri_FaturaId ON dbo.IslemUcretleri;
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_IslemUcretleri_FaturaId'
          AND object_id = OBJECT_ID(N'dbo.IslemUcretleri')
    )
    BEGIN
        CREATE INDEX IX_IslemUcretleri_FaturaId
        ON dbo.IslemUcretleri(FaturaId)
        WHERE FaturaId IS NOT NULL;
    END

    IF OBJECT_ID(N'dbo.__SchemaVersions', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.__SchemaVersions WHERE Version = N'010')
    BEGIN
        INSERT INTO dbo.__SchemaVersions (Version, Aciklama)
        VALUES (N'010', N'FaturaId indeksi tahsilat gruplarini destekleyecek sekilde duzeltildi.');
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrorMessage NVARCHAR(4000);
    SET @ErrorMessage = ERROR_MESSAGE();
    RAISERROR(@ErrorMessage, 16, 1);
END CATCH;
GO
