SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE KantarPro;
GO

DECLARE @AdminId INT;
SELECT @AdminId = KullaniciId FROM dbo.Kullanicilar WHERE KullaniciAdi = N'admin';

IF @AdminId IS NULL
BEGIN
    INSERT INTO dbo.Kullanicilar (KullaniciAdi, ParolaHash, AdSoyad, Rol, AktifMi)
    VALUES (N'admin', N'DEVELOPMENT_PLACEHOLDER_HASH', N'Admin Kullanici', N'Admin', 1);
    SET @AdminId = SCOPE_IDENTITY();
END

DECLARE @Yil INT;
SET @Yil = YEAR(GETDATE());

DECLARE @GirisCikisUcretId INT;
DECLARE @TartimUcretId INT;
DECLARE @BeklemeUcretId INT;
DECLARE @GirisCikisTutar DECIMAL(18,2);
DECLARE @TartimTutar DECIMAL(18,2);
DECLARE @BeklemeTutar DECIMAL(18,2);

SELECT TOP 1 @GirisCikisUcretId = UcretId, @GirisCikisTutar = Tutar
FROM dbo.Ucretler
WHERE UcretKodu = N'GIRIS_CIKIS' AND Yil = @Yil AND AktifMi = 1
ORDER BY GecerlilikBaslangic DESC;

SELECT TOP 1 @TartimUcretId = UcretId, @TartimTutar = Tutar
FROM dbo.Ucretler
WHERE UcretKodu = N'TARTIM' AND Yil = @Yil AND AktifMi = 1
ORDER BY GecerlilikBaslangic DESC;

SELECT TOP 1 @BeklemeUcretId = UcretId, @BeklemeTutar = Tutar
FROM dbo.Ucretler
WHERE UcretKodu = N'BEKLEME' AND Yil = @Yil AND AktifMi = 1
ORDER BY GecerlilikBaslangic DESC;

IF @GirisCikisUcretId IS NULL OR @TartimUcretId IS NULL OR @BeklemeUcretId IS NULL
BEGIN
    RAISERROR('Varsayilan ucret kayitlari bulunamadi.', 16, 1);
    RETURN;
END

-- Demo verileri tekrar calistirmada cogalmasin diye yalniz demo kayitlari temizlenir.
DELETE bt
FROM dbo.BekleyenTartimlar bt
INNER JOIN dbo.Araclar a ON a.AracId = bt.AracId
WHERE a.Aciklama IN (N'Demo giris kaydi', N'Demo cikis kaydi');

DELETE iu
FROM dbo.IslemUcretleri iu
INNER JOIN dbo.Islemler i ON i.IslemId = iu.IslemId
INNER JOIN dbo.Araclar a ON a.AracId = i.AracId
WHERE a.Aciklama IN (N'Demo giris kaydi', N'Demo cikis kaydi') OR i.IslemNo LIKE N'DMG%' OR i.IslemNo LIKE N'DMC%';

DELETE t
FROM dbo.Tartimlar t
INNER JOIN dbo.Araclar a ON a.AracId = t.AracId
WHERE a.Aciklama IN (N'Demo giris kaydi', N'Demo cikis kaydi');

DELETE l
FROM dbo.Loglar l
INNER JOIN dbo.Islemler i ON i.IslemId = l.IslemId
INNER JOIN dbo.Araclar a ON a.AracId = i.AracId
WHERE a.Aciklama IN (N'Demo giris kaydi', N'Demo cikis kaydi') OR i.IslemNo LIKE N'DMG%' OR i.IslemNo LIKE N'DMC%';

DELETE i
FROM dbo.Islemler i
INNER JOIN dbo.Araclar a ON a.AracId = i.AracId
WHERE a.Aciklama IN (N'Demo giris kaydi', N'Demo cikis kaydi') OR i.IslemNo LIKE N'DMG%' OR i.IslemNo LIKE N'DMC%';

DELETE FROM dbo.Araclar
WHERE Aciklama IN (N'Demo giris kaydi', N'Demo cikis kaydi');
GO

DECLARE @AdminId2 INT;
SELECT @AdminId2 = KullaniciId FROM dbo.Kullanicilar WHERE KullaniciAdi = N'admin';

DECLARE @Yil2 INT;
SET @Yil2 = YEAR(GETDATE());

DECLARE @GirisCikisUcretId2 INT;
DECLARE @TartimUcretId2 INT;
DECLARE @BeklemeUcretId2 INT;
DECLARE @GirisCikisTutar2 DECIMAL(18,2);
DECLARE @TartimTutar2 DECIMAL(18,2);
DECLARE @BeklemeTutar2 DECIMAL(18,2);

SELECT TOP 1 @GirisCikisUcretId2 = UcretId, @GirisCikisTutar2 = Tutar FROM dbo.Ucretler WHERE UcretKodu = N'GIRIS_CIKIS' AND Yil = @Yil2 AND AktifMi = 1 ORDER BY GecerlilikBaslangic DESC;
SELECT TOP 1 @TartimUcretId2 = UcretId, @TartimTutar2 = Tutar FROM dbo.Ucretler WHERE UcretKodu = N'TARTIM' AND Yil = @Yil2 AND AktifMi = 1 ORDER BY GecerlilikBaslangic DESC;
SELECT TOP 1 @BeklemeUcretId2 = UcretId, @BeklemeTutar2 = Tutar FROM dbo.Ucretler WHERE UcretKodu = N'BEKLEME' AND Yil = @Yil2 AND AktifMi = 1 ORDER BY GecerlilikBaslangic DESC;

DECLARE @i INT;
DECLARE @Plaka NVARCHAR(20);
DECLARE @AracId INT;
DECLARE @IslemId INT;
DECLARE @GirisTarihi DATETIME;
DECLARE @CikisTarihi DATETIME;
DECLARE @HasTartim BIT;
DECLARE @HasBekleme BIT;
DECLARE @Toplam DECIMAL(18,2);
DECLARE @Agirlik DECIMAL(18,2);
DECLARE @TartimId INT;
DECLARE @TartimTipi NVARCHAR(30);
DECLARE @Seed INT;
DECLARE @IlKodu INT;
DECLARE @Harf1 NCHAR(1);
DECLARE @Harf2 NCHAR(1);
DECLARE @Harf3 NCHAR(1);
DECLARE @Rakam INT;

-- 100 adet acik giris kaydi.
SET @i = 1;
WHILE @i <= 100
BEGIN
    SET @Seed = @i;
    SET @IlKodu = ((@Seed * 37) % 81) + 1;
    SET @Harf1 = NCHAR(65 + ((@Seed * 5 + 1) % 26));
    SET @Harf2 = NCHAR(65 + ((@Seed * 11 + 7) % 26));
    SET @Harf3 = NCHAR(65 + ((@Seed * 17 + 13) % 26));
    SET @Rakam = 1000 + ((@Seed * 73 + 421) % 9000);
    SET @Plaka = RIGHT('00' + CAST(@IlKodu AS VARCHAR(2)), 2) + @Harf1 + @Harf2 + @Harf3 + RIGHT('0000' + CAST(@Rakam AS VARCHAR(4)), 4);
    WHILE EXISTS (SELECT 1 FROM dbo.Araclar WHERE Plaka = @Plaka)
    BEGIN
        SET @Seed = @Seed + 211;
        SET @IlKodu = ((@Seed * 37) % 81) + 1;
        SET @Harf1 = NCHAR(65 + ((@Seed * 5 + 1) % 26));
        SET @Harf2 = NCHAR(65 + ((@Seed * 11 + 7) % 26));
        SET @Harf3 = NCHAR(65 + ((@Seed * 17 + 13) % 26));
        SET @Rakam = 1000 + ((@Seed * 73 + 421) % 9000);
        SET @Plaka = RIGHT('00' + CAST(@IlKodu AS VARCHAR(2)), 2) + @Harf1 + @Harf2 + @Harf3 + RIGHT('0000' + CAST(@Rakam AS VARCHAR(4)), 4);
    END
    SET @HasTartim = CASE WHEN @i % 2 = 0 OR @i % 5 = 0 THEN 1 ELSE 0 END;
    SET @HasBekleme = CASE WHEN @i % 3 = 0 THEN 1 ELSE 0 END;
    SET @GirisTarihi = DATEADD(MINUTE, -(@i * 17), DATEADD(DAY, -CASE WHEN @HasBekleme = 1 THEN ((@i % 4) + 1) ELSE 0 END, GETDATE()));
    SET @Toplam = @GirisCikisTutar2 + CASE WHEN @HasTartim = 1 THEN @TartimTutar2 ELSE 0 END;
    SET @Agirlik = 9000 + (@i * 137);

    INSERT INTO dbo.Araclar (Plaka, FirmaAdi, AracTipi, Aciklama, AktifMi, OlusturmaTarihi)
    VALUES (@Plaka, N'Demo Firma ' + RIGHT('000' + CAST(@i AS VARCHAR(3)), 3), N'TIR', N'Demo giris kaydi', 1, @GirisTarihi);
    SET @AracId = SCOPE_IDENTITY();

    INSERT INTO dbo.Islemler (IslemNo, AracId, GirisTarihi, CikisTarihi, Durum, GirisKullaniciId, CikisKullaniciId, ToplamTahakkuk, ToplamTahsilat, Notlar)
    VALUES (N'DMG' + RIGHT('000000' + CAST(@i AS VARCHAR(6)), 6), @AracId, @GirisTarihi, NULL, N'Iceride', @AdminId2, NULL, @Toplam, 0, N'Demo acik giris');
    SET @IslemId = SCOPE_IDENTITY();

    INSERT INTO dbo.IslemUcretleri (IslemId, UcretId, UcretAdi, Tutar, TahakkukTarihi, TahsilEdildiMi)
    VALUES (@IslemId, @GirisCikisUcretId2, N'Giris-Cikis Ucreti', @GirisCikisTutar2, @GirisTarihi, 0);

    IF @HasTartim = 1
    BEGIN
        INSERT INTO dbo.Tartimlar (IslemId, AracId, TartimTipi, AgirlikKg, TartimTarihi, ComPorttanAlindiMi, ManuelMi, KullaniciId, FisYazdirildiMi)
        VALUES (@IslemId, @AracId, N'Giris', @Agirlik, @GirisTarihi, 0, 1, @AdminId2, 0);
        SET @TartimId = SCOPE_IDENTITY();

        INSERT INTO dbo.IslemUcretleri (IslemId, UcretId, UcretAdi, Tutar, TahakkukTarihi, TahsilEdildiMi)
        VALUES (@IslemId, @TartimUcretId2, N'Tartim Ucreti', @TartimTutar2, @GirisTarihi, 0);

        INSERT INTO dbo.BekleyenTartimlar (AracId, IlkTartimId, IlkAgirlikKg, IlkTartimTarihi, Durum)
        VALUES (@AracId, @TartimId, @Agirlik, @GirisTarihi, N'Bekliyor');
    END

    INSERT INTO dbo.Loglar (KullaniciId, IslemId, LogTipi, Mesaj, Tarih, BilgisayarAdi)
    VALUES (@AdminId2, @IslemId, N'Demo', N'Demo giris kaydi olusturuldu.', GETDATE(), HOST_NAME());

    SET @i = @i + 1;
END

-- 100 adet cikis yapilmis kayit.
SET @i = 1;
WHILE @i <= 100
BEGIN
    SET @Seed = @i + 100;
    SET @IlKodu = ((@Seed * 37) % 81) + 1;
    SET @Harf1 = NCHAR(65 + ((@Seed * 5 + 1) % 26));
    SET @Harf2 = NCHAR(65 + ((@Seed * 11 + 7) % 26));
    SET @Harf3 = NCHAR(65 + ((@Seed * 17 + 13) % 26));
    SET @Rakam = 1000 + ((@Seed * 73 + 421) % 9000);
    SET @Plaka = RIGHT('00' + CAST(@IlKodu AS VARCHAR(2)), 2) + @Harf1 + @Harf2 + @Harf3 + RIGHT('0000' + CAST(@Rakam AS VARCHAR(4)), 4);
    WHILE EXISTS (SELECT 1 FROM dbo.Araclar WHERE Plaka = @Plaka)
    BEGIN
        SET @Seed = @Seed + 211;
        SET @IlKodu = ((@Seed * 37) % 81) + 1;
        SET @Harf1 = NCHAR(65 + ((@Seed * 5 + 1) % 26));
        SET @Harf2 = NCHAR(65 + ((@Seed * 11 + 7) % 26));
        SET @Harf3 = NCHAR(65 + ((@Seed * 17 + 13) % 26));
        SET @Rakam = 1000 + ((@Seed * 73 + 421) % 9000);
        SET @Plaka = RIGHT('00' + CAST(@IlKodu AS VARCHAR(2)), 2) + @Harf1 + @Harf2 + @Harf3 + RIGHT('0000' + CAST(@Rakam AS VARCHAR(4)), 4);
    END
    SET @HasTartim = CASE WHEN @i % 2 = 1 OR @i % 7 = 0 THEN 1 ELSE 0 END;
    SET @HasBekleme = CASE WHEN @i % 4 = 0 THEN 1 ELSE 0 END;
    SET @GirisTarihi = DATEADD(MINUTE, -(@i * 23), DATEADD(DAY, -CASE WHEN @HasBekleme = 1 THEN ((@i % 5) + 1) ELSE 0 END, GETDATE()));
    SET @CikisTarihi = DATEADD(MINUTE, -(@i * 5), GETDATE());
    SET @Toplam = @GirisCikisTutar2 + CASE WHEN @HasTartim = 1 THEN @TartimTutar2 ELSE 0 END + CASE WHEN @HasBekleme = 1 THEN (DATEDIFF(DAY, @GirisTarihi, @CikisTarihi) * @BeklemeTutar2) ELSE 0 END;
    SET @Agirlik = 8500 + (@i * 149);

    INSERT INTO dbo.Araclar (Plaka, FirmaAdi, AracTipi, Aciklama, AktifMi, OlusturmaTarihi)
    VALUES (@Plaka, N'Demo Firma ' + RIGHT('000' + CAST((@i + 100) AS VARCHAR(3)), 3), N'TIR', N'Demo cikis kaydi', 1, @GirisTarihi);
    SET @AracId = SCOPE_IDENTITY();

    INSERT INTO dbo.Islemler (IslemNo, AracId, GirisTarihi, CikisTarihi, Durum, GirisKullaniciId, CikisKullaniciId, ToplamTahakkuk, ToplamTahsilat, Notlar)
    VALUES (N'DMC' + RIGHT('000000' + CAST(@i AS VARCHAR(6)), 6), @AracId, @GirisTarihi, @CikisTarihi, N'CikisYapti', @AdminId2, @AdminId2, @Toplam, 0, N'Demo tamamlanmis cikis');
    SET @IslemId = SCOPE_IDENTITY();

    INSERT INTO dbo.IslemUcretleri (IslemId, UcretId, UcretAdi, Tutar, TahakkukTarihi, TahsilEdildiMi)
    VALUES (@IslemId, @GirisCikisUcretId2, N'Giris-Cikis Ucreti', @GirisCikisTutar2, @GirisTarihi, 0);

    IF @HasTartim = 1
    BEGIN
        SET @TartimTipi = CASE WHEN @i % 3 = 0 THEN N'Cikis' ELSE N'Giris' END;
        INSERT INTO dbo.Tartimlar (IslemId, AracId, TartimTipi, AgirlikKg, TartimTarihi, ComPorttanAlindiMi, ManuelMi, KullaniciId, FisYazdirildiMi)
        VALUES (@IslemId, @AracId, @TartimTipi, @Agirlik, CASE WHEN @i % 3 = 0 THEN @CikisTarihi ELSE @GirisTarihi END, 0, 1, @AdminId2, 0);
        SET @TartimId = SCOPE_IDENTITY();

        INSERT INTO dbo.IslemUcretleri (IslemId, UcretId, UcretAdi, Tutar, TahakkukTarihi, TahsilEdildiMi)
        VALUES (@IslemId, @TartimUcretId2, N'Tartim Ucreti', @TartimTutar2, @GirisTarihi, 0);

        IF @TartimTipi = N'Giris'
        BEGIN
            INSERT INTO dbo.BekleyenTartimlar (AracId, IlkTartimId, IlkAgirlikKg, IlkTartimTarihi, Durum)
            VALUES (@AracId, @TartimId, @Agirlik, @GirisTarihi, N'Bekliyor');
        END
    END

    IF @HasBekleme = 1
    BEGIN
        DECLARE @BeklemeIndex INT;
        DECLARE @BeklemeGun INT;
        SET @BeklemeIndex = 0;
        SET @BeklemeGun = DATEDIFF(DAY, @GirisTarihi, @CikisTarihi);
        WHILE @BeklemeIndex < @BeklemeGun
        BEGIN
            INSERT INTO dbo.IslemUcretleri (IslemId, UcretId, UcretAdi, Tutar, TahakkukTarihi, TahsilEdildiMi)
            VALUES (@IslemId, @BeklemeUcretId2, N'Bekleme Ucreti', @BeklemeTutar2, @CikisTarihi, 0);
            SET @BeklemeIndex = @BeklemeIndex + 1;
        END
    END

    INSERT INTO dbo.Loglar (KullaniciId, IslemId, LogTipi, Mesaj, Tarih, BilgisayarAdi)
    VALUES (@AdminId2, @IslemId, N'Demo', N'Demo cikis kaydi olusturuldu.', GETDATE(), HOST_NAME());

    SET @i = @i + 1;
END
GO
