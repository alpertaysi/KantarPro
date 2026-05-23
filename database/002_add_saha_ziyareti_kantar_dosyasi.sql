USE KantarPro;
GO

IF COL_LENGTH(N'dbo.Islemler', N'GelisTuru') IS NULL
BEGIN
    ALTER TABLE dbo.Islemler ADD GelisTuru NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Islemler_GelisTuru DEFAULT (N'Tartimsiz');
END
GO

IF COL_LENGTH(N'dbo.Tartimlar', N'YukDurumu') IS NULL
BEGIN
    ALTER TABLE dbo.Tartimlar ADD YukDurumu NVARCHAR(20) NULL;
END
GO

IF COL_LENGTH(N'dbo.IslemUcretleri', N'TahsilatId') IS NULL
BEGIN
    ALTER TABLE dbo.IslemUcretleri ADD TahsilatId NVARCHAR(40) NULL;
END
GO

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

    CREATE INDEX IX_KantarDosyalari_Arac_Durum ON dbo.KantarDosyalari(AracId, Durum);
END
GO
