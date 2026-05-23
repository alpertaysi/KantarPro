IF COL_LENGTH('dbo.IslemUcretleri', 'FaturaId') IS NULL
BEGIN
    ALTER TABLE dbo.IslemUcretleri ADD FaturaId NVARCHAR(40) NULL;
END
GO

UPDATE dbo.IslemUcretleri
SET FaturaId = 'FATESKI' + CAST(IslemId AS NVARCHAR(12)) + CONVERT(NVARCHAR(8), TahsilTarihi, 112) + REPLACE(CONVERT(NVARCHAR(8), TahsilTarihi, 108), ':', '')
WHERE TahsilEdildiMi = 1
  AND TahsilTarihi IS NOT NULL
  AND FaturaId IS NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IslemUcretleri_FaturaId' AND object_id = OBJECT_ID('dbo.IslemUcretleri'))
BEGIN
    CREATE INDEX IX_IslemUcretleri_FaturaId
    ON dbo.IslemUcretleri(FaturaId)
    WHERE FaturaId IS NOT NULL;
END
GO
