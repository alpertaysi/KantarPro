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

    IF OBJECT_ID(N'dbo.IslemUcretleri', N'U') IS NULL
    BEGIN
        RAISERROR('dbo.IslemUcretleri tablosu bulunamadi.', 16, 1);
    END

    IF COL_LENGTH(N'dbo.IslemUcretleri', N'OdemeTuru') IS NULL
    BEGIN
        ALTER TABLE dbo.IslemUcretleri ADD OdemeTuru NVARCHAR(30) NULL;
    END

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = N'CK_IslemUcretleri_OdemeTuru'
          AND parent_object_id = OBJECT_ID(N'dbo.IslemUcretleri')
    )
    BEGIN
        ALTER TABLE dbo.IslemUcretleri WITH NOCHECK
        ADD CONSTRAINT CK_IslemUcretleri_OdemeTuru
        CHECK
        (
            OdemeTuru IS NULL OR
            OdemeTuru = N'Nakit' OR
            OdemeTuru = N'Kredi Kartı'
        );
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
    RAISERROR('012_add_odeme_turu_guard basarisiz: %s (Satir: %d)', 16, 1, @HataMesaj, @HataSatir);
END CATCH
GO
