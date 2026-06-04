SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- DIKKAT: Bu script yalniz DEV/TEST/LOCAL veritabanlarinda demo veri uretmek icindir.
-- Production veritabaninda ASLA calistirilmemelidir.
DECLARE @DemoSeedOnayla BIT;
DECLARE @MevcutDb NVARCHAR(128);
DECLARE @OrtamUygunMu BIT;

SET @DemoSeedOnayla = 0;
SET @MevcutDb = DB_NAME();
SET @OrtamUygunMu = 0;

IF @MevcutDb LIKE N'%Dev%' OR @MevcutDb LIKE N'%Test%' OR @MevcutDb LIKE N'%Local%'
BEGIN
    SET @OrtamUygunMu = 1;
END

IF @DemoSeedOnayla = 0 OR @OrtamUygunMu = 0
BEGIN
    RAISERROR('003_seed_random_demo_data sadece Dev/Test/Local veritabaninda ve @DemoSeedOnayla = 1 iken calisir. Mevcut DB: %s', 16, 1, @MevcutDb);
    RETURN;
END

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Araclar', N'U') IS NULL
       OR OBJECT_ID(N'dbo.Islemler', N'U') IS NULL
       OR OBJECT_ID(N'dbo.Tartimlar', N'U') IS NULL
       OR OBJECT_ID(N'dbo.IslemUcretleri', N'U') IS NULL
       OR OBJECT_ID(N'dbo.Kullanicilar', N'U') IS NULL
    BEGIN
        RAISERROR('Demo seed icin gerekli KantarPro tablolari bulunamadi. Once sema scriptlerini calistirin.', 16, 1);
    END

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

    DECLARE @AdminId INT;
    SELECT @AdminId = KullaniciId FROM dbo.Kullanicilar WHERE KullaniciAdi = N'admin';

    IF @AdminId IS NULL
    BEGIN
        RAISERROR('Demo seed icin admin kullanicisi gereklidir. Once 006_seed_first_admin.sql ile admin kullanicisini olusturun.', 16, 1);
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
    END

    -- Demo verileri tekrar calistirmada cogalmasin diye yalniz demo isaretli kayitlar temizlenir.
    IF OBJECT_ID(N'dbo.KantarDosyalari', N'U') IS NOT NULL
    BEGIN
        DELETE kd
        FROM dbo.KantarDosyalari kd
        INNER JOIN dbo.Araclar a ON a.AracId = kd.AracId
        WHERE a.Aciklama IN (N'Demo giris kaydi', N'Demo cikis kaydi');
    END

    IF OBJECT_ID(N'dbo.BekleyenTartimlar', N'U') IS NOT NULL
    BEGIN
        DELETE bt
        FROM dbo.BekleyenTartimlar bt
        INNER JOIN dbo.Araclar a ON a.AracId = bt.AracId
        WHERE a.Aciklama IN (N'Demo giris kaydi', N'Demo cikis kaydi');
    END

    DELETE iu
    FROM dbo.IslemUcretleri iu
    INNER JOIN dbo.Islemler i ON i.IslemId = iu.IslemId
    INNER JOIN dbo.Araclar a ON a.AracId = i.AracId
    WHERE a.Aciklama IN (N'Demo giris kaydi', N'Demo cikis kaydi');

    DELETE t
    FROM dbo.Tartimlar t
    INNER JOIN dbo.Araclar a ON a.AracId = t.AracId
    WHERE a.Aciklama IN (N'Demo giris kaydi', N'Demo cikis kaydi');

    DELETE l
    FROM dbo.Loglar l
    INNER JOIN dbo.Islemler i ON i.IslemId = l.IslemId
    INNER JOIN dbo.Araclar a ON a.AracId = i.AracId
    WHERE a.Aciklama IN (N'Demo giris kaydi', N'Demo cikis kaydi');

    DELETE i
    FROM dbo.Islemler i
    INNER JOIN dbo.Araclar a ON a.AracId = i.AracId
    WHERE a.Aciklama IN (N'Demo giris kaydi', N'Demo cikis kaydi');

    DELETE FROM dbo.Araclar
    WHERE Aciklama IN (N'Demo giris kaydi', N'Demo cikis kaydi');

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
    DECLARE @BeklemeIndex INT;
    DECLARE @BeklemeGun INT;

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
        SET @Toplam = @GirisCikisTutar + CASE WHEN @HasTartim = 1 THEN @TartimTutar ELSE 0 END;
        SET @Agirlik = 9000 + (@i * 137);

        INSERT INTO dbo.Araclar (Plaka, FirmaAdi, AracTipi, Aciklama, AktifMi, OlusturmaTarihi)
        VALUES (@Plaka, N'Demo Firma ' + RIGHT('000' + CAST(@i AS VARCHAR(3)), 3), N'TIR', N'Demo giris kaydi', 1, @GirisTarihi);
        SET @AracId = SCOPE_IDENTITY();

        INSERT INTO dbo.Islemler (IslemNo, AracId, GirisTarihi, CikisTarihi, Durum, GirisKullaniciId, CikisKullaniciId, ToplamTahakkuk, ToplamTahsilat, Notlar)
        VALUES (N'DMG' + RIGHT('000000' + CAST(@i AS VARCHAR(6)), 6), @AracId, @GirisTarihi, NULL, N'Iceride', @AdminId, NULL, @Toplam, 0, N'Demo acik giris');
        SET @IslemId = SCOPE_IDENTITY();

        INSERT INTO dbo.IslemUcretleri (IslemId, UcretId, UcretAdi, Tutar, TahakkukTarihi, TahsilEdildiMi)
        VALUES (@IslemId, @GirisCikisUcretId, N'Giris-Cikis Ucreti', @GirisCikisTutar, @GirisTarihi, 0);

        IF @HasTartim = 1
        BEGIN
            INSERT INTO dbo.Tartimlar (IslemId, AracId, TartimTipi, AgirlikKg, TartimTarihi, ComPorttanAlindiMi, ManuelMi, KullaniciId, FisYazdirildiMi)
            VALUES (@IslemId, @AracId, N'Giris', @Agirlik, @GirisTarihi, 0, 1, @AdminId, 0);
            SET @TartimId = SCOPE_IDENTITY();

            INSERT INTO dbo.IslemUcretleri (IslemId, UcretId, UcretAdi, Tutar, TahakkukTarihi, TahsilEdildiMi)
            VALUES (@IslemId, @TartimUcretId, N'Tartim Ucreti', @TartimTutar, @GirisTarihi, 0);

            IF OBJECT_ID(N'dbo.BekleyenTartimlar', N'U') IS NOT NULL
            BEGIN
                INSERT INTO dbo.BekleyenTartimlar (AracId, IlkTartimId, IlkAgirlikKg, IlkTartimTarihi, Durum)
                VALUES (@AracId, @TartimId, @Agirlik, @GirisTarihi, N'Bekliyor');
            END
        END

        INSERT INTO dbo.Loglar (KullaniciId, IslemId, LogTipi, Mesaj, Tarih, BilgisayarAdi)
        VALUES (@AdminId, @IslemId, N'Demo', N'Demo giris kaydi olusturuldu.', GETDATE(), ISNULL(HOST_NAME(), N'BILINMIYOR'));

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
        SET @Toplam = @GirisCikisTutar + CASE WHEN @HasTartim = 1 THEN @TartimTutar ELSE 0 END + CASE WHEN @HasBekleme = 1 THEN (DATEDIFF(DAY, @GirisTarihi, @CikisTarihi) * @BeklemeTutar) ELSE 0 END;
        SET @Agirlik = 8500 + (@i * 149);

        INSERT INTO dbo.Araclar (Plaka, FirmaAdi, AracTipi, Aciklama, AktifMi, OlusturmaTarihi)
        VALUES (@Plaka, N'Demo Firma ' + RIGHT('000' + CAST((@i + 100) AS VARCHAR(3)), 3), N'TIR', N'Demo cikis kaydi', 1, @GirisTarihi);
        SET @AracId = SCOPE_IDENTITY();

        INSERT INTO dbo.Islemler (IslemNo, AracId, GirisTarihi, CikisTarihi, Durum, GirisKullaniciId, CikisKullaniciId, ToplamTahakkuk, ToplamTahsilat, Notlar)
        VALUES (N'DMC' + RIGHT('000000' + CAST(@i AS VARCHAR(6)), 6), @AracId, @GirisTarihi, @CikisTarihi, N'CikisYapti', @AdminId, @AdminId, @Toplam, 0, N'Demo tamamlanmis cikis');
        SET @IslemId = SCOPE_IDENTITY();

        INSERT INTO dbo.IslemUcretleri (IslemId, UcretId, UcretAdi, Tutar, TahakkukTarihi, TahsilEdildiMi)
        VALUES (@IslemId, @GirisCikisUcretId, N'Giris-Cikis Ucreti', @GirisCikisTutar, @GirisTarihi, 0);

        IF @HasTartim = 1
        BEGIN
            SET @TartimTipi = CASE WHEN @i % 3 = 0 THEN N'Cikis' ELSE N'Giris' END;
            INSERT INTO dbo.Tartimlar (IslemId, AracId, TartimTipi, AgirlikKg, TartimTarihi, ComPorttanAlindiMi, ManuelMi, KullaniciId, FisYazdirildiMi)
            VALUES (@IslemId, @AracId, @TartimTipi, @Agirlik, CASE WHEN @i % 3 = 0 THEN @CikisTarihi ELSE @GirisTarihi END, 0, 1, @AdminId, 0);
            SET @TartimId = SCOPE_IDENTITY();

            INSERT INTO dbo.IslemUcretleri (IslemId, UcretId, UcretAdi, Tutar, TahakkukTarihi, TahsilEdildiMi)
            VALUES (@IslemId, @TartimUcretId, N'Tartim Ucreti', @TartimTutar, @GirisTarihi, 0);

            IF @TartimTipi = N'Giris' AND OBJECT_ID(N'dbo.BekleyenTartimlar', N'U') IS NOT NULL
            BEGIN
                INSERT INTO dbo.BekleyenTartimlar (AracId, IlkTartimId, IlkAgirlikKg, IlkTartimTarihi, Durum)
                VALUES (@AracId, @TartimId, @Agirlik, @GirisTarihi, N'Bekliyor');
            END
        END

        IF @HasBekleme = 1
        BEGIN
            SET @BeklemeIndex = 0;
            SET @BeklemeGun = DATEDIFF(DAY, @GirisTarihi, @CikisTarihi);
            WHILE @BeklemeIndex < @BeklemeGun
            BEGIN
                INSERT INTO dbo.IslemUcretleri (IslemId, UcretId, UcretAdi, Tutar, TahakkukTarihi, TahsilEdildiMi)
                VALUES (@IslemId, @BeklemeUcretId, N'Bekleme Ucreti', @BeklemeTutar, @CikisTarihi, 0);
                SET @BeklemeIndex = @BeklemeIndex + 1;
            END
        END

        INSERT INTO dbo.Loglar (KullaniciId, IslemId, LogTipi, Mesaj, Tarih, BilgisayarAdi)
        VALUES (@AdminId, @IslemId, N'Demo', N'Demo cikis kaydi olusturuldu.', GETDATE(), ISNULL(HOST_NAME(), N'BILINMIYOR'));

        SET @i = @i + 1;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.__SchemaVersions WHERE Version = N'003')
    BEGIN
        INSERT INTO dbo.__SchemaVersions (Version, Aciklama)
        VALUES (N'003', N'Dev/Test demo verileri');
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
    RAISERROR('003_seed_random_demo_data basarisiz: %s (Satir: %d)', 16, 1, @HataMesaj, @HataSatir);
END CATCH
GO
