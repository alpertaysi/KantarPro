IF COL_LENGTH('dbo.Islemler', 'MuafMi') IS NULL
BEGIN
    ALTER TABLE dbo.Islemler
    ADD MuafMi BIT NOT NULL CONSTRAINT DF_Islemler_MuafMi DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.Islemler', 'MuafiyetNedeni') IS NULL
BEGIN
    ALTER TABLE dbo.Islemler
    ADD MuafiyetNedeni NVARCHAR(250) NULL;
END
GO
