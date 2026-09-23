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

    UPDATE I
    SET CikisNo = NULL
    FROM dbo.Islemler I
    WHERE I.Durum = N'Iceride'
      AND I.CikisTarihi IS NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.IslemUcretleri U
          WHERE U.IslemId = I.IslemId
            AND U.TahsilEdildiMi = 1
      );

    IF OBJECT_ID(N'dbo.__SchemaVersions', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.__SchemaVersions WHERE Version = N'013')
    BEGIN
        INSERT INTO dbo.__SchemaVersions (Version, Aciklama)
        VALUES (N'013', N'Tahsilat numarasi cikis ve tahsilat tamamlanana kadar ertelendi.');
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
    RAISERROR('013_defer_tahsilat_no_until_exit basarisiz: %s (Satir: %d)', 16, 1, @HataMesaj, @HataSatir);
END CATCH;
GO
