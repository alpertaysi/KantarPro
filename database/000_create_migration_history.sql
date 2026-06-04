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
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__SchemaVersions WHERE Version = N'000')
BEGIN
    INSERT INTO dbo.__SchemaVersions (Version, Aciklama)
    VALUES (N'000', N'Migration takip tablosu olusturuldu');
END
GO
