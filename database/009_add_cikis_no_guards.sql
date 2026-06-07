SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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

    IF COL_LENGTH(N'dbo.Islemler', N'CikisNo') IS NULL
    BEGIN
        ALTER TABLE dbo.Islemler ADD CikisNo NVARCHAR(20) NULL;
    END

    IF OBJECT_ID(N'dbo.CK_Islemler_CikisNo_Format', N'C') IS NULL
       AND NOT EXISTS (
           SELECT 1
           FROM dbo.Islemler
           WHERE CikisNo IS NOT NULL
             AND CikisNo <> N''
             AND CikisNo NOT LIKE N'[0-9][0-9][0-9][0-9]'
       )
    BEGIN
        ALTER TABLE dbo.Islemler WITH CHECK
        ADD CONSTRAINT CK_Islemler_CikisNo_Format
        CHECK (CikisNo IS NULL OR CikisNo = N'' OR CikisNo LIKE N'[0-9][0-9][0-9][0-9]');
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Islemler_CikisNo' AND object_id = OBJECT_ID(N'dbo.Islemler'))
       AND NOT EXISTS (
           SELECT CikisNo
           FROM dbo.Islemler
           WHERE CikisNo IS NOT NULL AND CikisNo <> N''
           GROUP BY CikisNo
           HAVING COUNT(*) > 1
       )
    BEGIN
        CREATE UNIQUE INDEX UX_Islemler_CikisNo
        ON dbo.Islemler(CikisNo)
        WHERE CikisNo IS NOT NULL AND CikisNo <> N'';
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.__SchemaVersions WHERE Version = N'009')
    BEGIN
        INSERT INTO dbo.__SchemaVersions (Version, Aciklama)
        VALUES (N'009', N'CikisNo format ve benzersizlik korumalari eklendi.');
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
