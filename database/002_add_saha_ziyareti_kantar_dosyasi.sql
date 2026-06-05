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

    IF COL_LENGTH(N'dbo.Islemler', N'GelisTuru') IS NULL
    BEGIN
        ALTER TABLE dbo.Islemler ADD GelisTuru NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Islemler_GelisTuru DEFAULT (N'Tartimsiz');
    END

    IF COL_LENGTH(N'dbo.Tartimlar', N'YukDurumu') IS NULL
    BEGIN
        ALTER TABLE dbo.Tartimlar ADD YukDurumu NVARCHAR(20) NULL;
    END

    IF COL_LENGTH(N'dbo.Tartimlar', N'KantarFisNo') IS NULL
    BEGIN
        ALTER TABLE dbo.Tartimlar ADD KantarFisNo NVARCHAR(20) NULL;
    END

    IF COL_LENGTH(N'dbo.IslemUcretleri', N'TahsilatId') IS NULL
    BEGIN
        ALTER TABLE dbo.IslemUcretleri ADD TahsilatId NVARCHAR(40) NULL;
    END

    IF OBJECT_ID(N'dbo.KantarDosyalari', N'U') IS NULL
    BEGIN
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
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_KantarDosyalari_Arac_Durum' AND object_id = OBJECT_ID(N'dbo.KantarDosyalari'))
    BEGIN
        CREATE INDEX IX_KantarDosyalari_Arac_Durum ON dbo.KantarDosyalari(AracId, Durum);
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.__SchemaVersions WHERE Version = N'002')
    BEGIN
        INSERT INTO dbo.__SchemaVersions (Version, Aciklama)
        VALUES (N'002', N'Saha ziyareti ve kantar dosyasi alanlari');
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
    RAISERROR('002_add_saha_ziyareti_kantar_dosyasi basarisiz: %s (Satir: %d)', 16, 1, @HataMesaj, @HataSatir);
END CATCH
GO
