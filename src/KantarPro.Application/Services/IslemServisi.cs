using System;
using System.Linq;
using KantarPro.Application.Abstractions;
using KantarPro.Domain;
using KantarPro.Domain.Entities;

namespace KantarPro.Application.Services
{
    public class IslemServisi
    {
        private readonly IUnitOfWork _unitOfWork;

        public IslemServisi(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public Islem GirisYap(string plaka, bool tartimIsteniyor, decimal? agirlikKg, int kullaniciId, DateTime islemTarihi)
        {
            return GirisYap(plaka, null, null, tartimIsteniyor, agirlikKg, kullaniciId, islemTarihi);
        }

        public Islem GirisYap(string plaka, string firmaAdi, string aciklama, bool tartimIsteniyor, decimal? agirlikKg, int kullaniciId, DateTime islemTarihi)
        {
            return GirisYap(plaka, firmaAdi, aciklama, tartimIsteniyor, agirlikKg, kullaniciId, islemTarihi, true);
        }

        private Islem GirisYap(string plaka, string firmaAdi, string aciklama, bool tartimIsteniyor, decimal? agirlikKg, int kullaniciId, DateTime islemTarihi, bool bekleyenIlkTartimOlustur)
        {
            if (string.IsNullOrWhiteSpace(plaka))
            {
                throw new ArgumentException("Plaka bos olamaz.", nameof(plaka));
            }

            var temizPlaka = NormalizePlaka(plaka);
            var arac = _unitOfWork.Araclar.SingleOrDefault(x => x.Plaka == temizPlaka);
            if (arac == null)
            {
                arac = new Arac
                {
                    Plaka = temizPlaka,
                    FirmaAdi = NormalizeOptional(firmaAdi),
                    Aciklama = NormalizeOptional(aciklama),
                    AktifMi = true,
                    OlusturmaTarihi = islemTarihi
                };
                _unitOfWork.Araclar.Add(arac);
            }
            else
            {
                var temizFirma = NormalizeOptional(firmaAdi);
                var temizAciklama = NormalizeOptional(aciklama);
                if (!string.IsNullOrWhiteSpace(temizFirma))
                {
                    arac.FirmaAdi = temizFirma;
                }

                if (!string.IsNullOrWhiteSpace(temizAciklama))
                {
                    arac.Aciklama = temizAciklama;
                }
            }

            var icerideMi = _unitOfWork.Islemler.Query()
                .Any(x => x.Arac.Plaka == temizPlaka && x.Durum == KantarSabitleri.IslemDurumu.Iceride);

            if (icerideMi)
            {
                throw new InvalidOperationException("Bu plaka icin iceride acik islem var.");
            }

            var islem = new Islem
            {
                Arac = arac,
                IslemNo = UretIslemNo(islemTarihi),
                GirisTarihi = islemTarihi,
                GelisTuru = KantarSabitleri.GelisTuru.Tartimsiz,
                Durum = KantarSabitleri.IslemDurumu.Iceride,
                GirisKullaniciId = kullaniciId,
                Notlar = NormalizeOptional(aciklama)
            };

            _unitOfWork.Islemler.Add(islem);
            UcretEkle(islem, KantarSabitleri.UcretKodu.GirisCikis, islemTarihi);

            if (tartimIsteniyor)
            {
                if (!agirlikKg.HasValue || agirlikKg.Value <= 0)
                {
                    throw new ArgumentException("Tartim isteniyorsa gecerli agirlik zorunludur.", nameof(agirlikKg));
                }

                var tartim = new Tartim
                {
                    Islem = islem,
                    Arac = arac,
                    TartimTipi = KantarSabitleri.TartimTipi.Giris,
                    AgirlikKg = agirlikKg.Value,
                    TartimTarihi = islemTarihi,
                    ComPorttanAlindiMi = true,
                    ManuelMi = false,
                    KullaniciId = kullaniciId
                };
                _unitOfWork.Tartimlar.Add(tartim);
                islem.Tartimlar.Add(tartim);
                UcretEkle(islem, KantarSabitleri.UcretKodu.Tartim, islemTarihi);
                if (bekleyenIlkTartimOlustur)
                {
                    BekleyenIlkTartimEkle(arac, tartim);
                }
            }

            LogEkle(kullaniciId, islem, "Giris", "Arac giris islemi olusturuldu.");
            _unitOfWork.SaveChanges();
            return islem;
        }

        public Islem GirisYapVeBekleyenTartimiTamamla(string plaka, string firmaAdi, string aciklama, int bekleyenTartimId, decimal ikinciAgirlikKg, int kullaniciId, DateTime islemTarihi)
        {
            if (ikinciAgirlikKg <= 0)
            {
                throw new ArgumentException("Ikinci tartim icin gecerli agirlik zorunludur.", nameof(ikinciAgirlikKg));
            }

            var temizPlaka = NormalizePlaka(plaka);
            var bekleyenTartim = _unitOfWork.BekleyenTartimlar.Query()
                .FirstOrDefault(x => x.BekleyenTartimId == bekleyenTartimId && x.Durum == KantarSabitleri.BekleyenTartimDurumu.Bekliyor);

            if (bekleyenTartim == null)
            {
                throw new InvalidOperationException("Bekleyen ilk tartim bulunamadi veya daha once tamamlanmis.");
            }

            var islem = _unitOfWork.Islemler.Query()
                .FirstOrDefault(x => x.Arac.Plaka == temizPlaka && x.Tartimlar.Any(t => t.TartimId == bekleyenTartim.IlkTartimId));

            if (islem == null)
            {
                throw new InvalidOperationException("Bekleyen ilk tartimin bagli oldugu islem bulunamadi.");
            }

            var temizFirma = NormalizeOptional(firmaAdi);
            var temizAciklama = NormalizeOptional(aciklama);
            if (!string.IsNullOrWhiteSpace(temizFirma))
            {
                islem.Arac.FirmaAdi = temizFirma;
            }

            if (!string.IsNullOrWhiteSpace(temizAciklama))
            {
                islem.Notlar = temizAciklama;
                islem.Arac.Aciklama = temizAciklama;
            }

            var ikinciTartim = new Tartim
            {
                Islem = islem,
                AracId = islem.AracId,
                Arac = islem.Arac,
                TartimTipi = KantarSabitleri.TartimTipi.Sonradan,
                AgirlikKg = ikinciAgirlikKg,
                TartimTarihi = islemTarihi,
                ComPorttanAlindiMi = true,
                ManuelMi = false,
                KullaniciId = kullaniciId
            };

            _unitOfWork.Tartimlar.Add(ikinciTartim);
            islem.Tartimlar.Add(ikinciTartim);
            islem.GirisTarihi = islemTarihi;
            islem.CikisTarihi = null;
            islem.CikisKullaniciId = null;
            islem.Durum = KantarSabitleri.IslemDurumu.Iceride;
            UcretEkle(islem, KantarSabitleri.UcretKodu.GirisCikis, islemTarihi);
            UcretEkle(islem, KantarSabitleri.UcretKodu.Tartim, islemTarihi);

            bekleyenTartim.TamamlayanTartim = ikinciTartim;
            bekleyenTartim.Durum = KantarSabitleri.BekleyenTartimDurumu.Tamamlandi;
            LogEkle(kullaniciId, islem, "DoluBos", "Bekleyen ilk tartim ikinci gelis tartimiyla tamamlandi.");
            _unitOfWork.SaveChanges();
            return islem;
        }

        public Islem IceridekiBekleyenTartimiTamamla(string plaka, int bekleyenTartimId, decimal ikinciAgirlikKg, int kullaniciId, DateTime tartimTarihi)
        {
            if (ikinciAgirlikKg <= 0)
            {
                throw new ArgumentException("Ikinci tartim icin gecerli agirlik zorunludur.", nameof(ikinciAgirlikKg));
            }

            var temizPlaka = NormalizePlaka(plaka);
            var bekleyenTartim = _unitOfWork.BekleyenTartimlar.Query()
                .FirstOrDefault(x => x.BekleyenTartimId == bekleyenTartimId && x.Durum == KantarSabitleri.BekleyenTartimDurumu.Bekliyor);

            if (bekleyenTartim == null)
            {
                throw new InvalidOperationException("Bekleyen ilk tartim bulunamadi veya daha once tamamlanmis.");
            }

            var islem = _unitOfWork.Islemler.Query()
                .FirstOrDefault(x => x.Arac.Plaka == temizPlaka && x.Durum == KantarSabitleri.IslemDurumu.Iceride);

            if (islem == null)
            {
                throw new InvalidOperationException("Bu plaka icin iceride acik islem bulunamadi.");
            }

            var ikinciTartim = new Tartim
            {
                Islem = islem,
                AracId = islem.AracId,
                Arac = islem.Arac,
                TartimTipi = KantarSabitleri.TartimTipi.Sonradan,
                AgirlikKg = ikinciAgirlikKg,
                TartimTarihi = tartimTarihi,
                ComPorttanAlindiMi = true,
                ManuelMi = false,
                KullaniciId = kullaniciId
            };

            _unitOfWork.Tartimlar.Add(ikinciTartim);
            islem.Tartimlar.Add(ikinciTartim);
            UcretEkle(islem, KantarSabitleri.UcretKodu.Tartim, tartimTarihi);

            bekleyenTartim.TamamlayanTartim = ikinciTartim;
            bekleyenTartim.Durum = KantarSabitleri.BekleyenTartimDurumu.Tamamlandi;

            LogEkle(kullaniciId, islem, "DoluBos", "Icerideki aracin bekleyen ilk tartimi tamamlandi.");
            _unitOfWork.SaveChanges();
            return islem;
        }

        public Islem CikisYap(string plaka, bool tartimIsteniyor, decimal? agirlikKg, int kullaniciId, DateTime cikisTarihi)
        {
            var temizPlaka = NormalizePlaka(plaka);
            var islem = _unitOfWork.Islemler.Query()
                .FirstOrDefault(x => x.Arac.Plaka == temizPlaka && x.Durum == KantarSabitleri.IslemDurumu.Iceride);

            if (islem == null)
            {
                throw new InvalidOperationException("Bu plaka icin acik giris islemi bulunamadi.");
            }

            islem.CikisTarihi = cikisTarihi;
            islem.CikisKullaniciId = kullaniciId;
            islem.Durum = KantarSabitleri.IslemDurumu.CikisYapti;

            if (tartimIsteniyor)
            {
                if (!agirlikKg.HasValue || agirlikKg.Value <= 0)
                {
                    throw new ArgumentException("Cikis tartimi icin gecerli agirlik zorunludur.", nameof(agirlikKg));
                }

                var tartim = new Tartim
                {
                    Islem = islem,
                    AracId = islem.AracId,
                    TartimTipi = KantarSabitleri.TartimTipi.Cikis,
                    AgirlikKg = agirlikKg.Value,
                    TartimTarihi = cikisTarihi,
                    ComPorttanAlindiMi = true,
                    ManuelMi = false,
                    KullaniciId = kullaniciId
                };
                _unitOfWork.Tartimlar.Add(tartim);
                islem.Tartimlar.Add(tartim);
                UcretEkle(islem, KantarSabitleri.UcretKodu.Tartim, cikisTarihi);
                BekleyenIlkTartimTamamla(islem.AracId, tartim);
            }

            var beklemeGunSayisi = HesaplaBeklemeGunSayisi(islem.GirisTarihi, cikisTarihi);
            for (var i = 0; i < beklemeGunSayisi; i++)
            {
                UcretEkle(islem, KantarSabitleri.UcretKodu.Bekleme, cikisTarihi);
            }

            TahsilEt(islem, kullaniciId, cikisTarihi);
            LogEkle(kullaniciId, islem, "Cikis", "Arac cikis islemi tamamlandi.");
            _unitOfWork.SaveChanges();
            return islem;
        }

        public Islem SonradanTartimEkle(string plaka, decimal agirlikKg, int kullaniciId, DateTime tartimTarihi)
        {
            if (agirlikKg <= 0)
            {
                throw new ArgumentException("Sonradan tartim icin gecerli agirlik zorunludur.", nameof(agirlikKg));
            }

            var temizPlaka = NormalizePlaka(plaka);
            var islem = _unitOfWork.Islemler.Query()
                .FirstOrDefault(x => x.Arac.Plaka == temizPlaka && x.Durum == KantarSabitleri.IslemDurumu.Iceride);

            if (islem == null)
            {
                throw new InvalidOperationException("Bu plaka icin iceride acik islem bulunamadi.");
            }

            var ilkTartimVarMi = islem.Tartimlar.Any(x => x.TartimTipi == KantarSabitleri.TartimTipi.Giris);
            var ikinciTartimVarMi = islem.Tartimlar.Any(x =>
                x.TartimTipi == KantarSabitleri.TartimTipi.Sonradan ||
                x.TartimTipi == KantarSabitleri.TartimTipi.Cikis);

            if (ilkTartimVarMi && ikinciTartimVarMi)
            {
                throw new InvalidOperationException("Bu plaka icin dolu-bos tartim tamamlanmis. Kesin cikis yapilmadan tekrar tartim yapilamaz.");
            }

            var tartim = new Tartim
            {
                Islem = islem,
                AracId = islem.AracId,
                Arac = islem.Arac,
                TartimTipi = ilkTartimVarMi ? KantarSabitleri.TartimTipi.Sonradan : KantarSabitleri.TartimTipi.Giris,
                AgirlikKg = agirlikKg,
                TartimTarihi = tartimTarihi,
                ComPorttanAlindiMi = true,
                ManuelMi = false,
                KullaniciId = kullaniciId
            };

            _unitOfWork.Tartimlar.Add(tartim);
            islem.Tartimlar.Add(tartim);
            UcretEkle(islem, KantarSabitleri.UcretKodu.Tartim, tartimTarihi);

            if (ilkTartimVarMi)
            {
                BekleyenIlkTartimTamamla(islem.AracId, tartim);
            }
            else
            {
                BekleyenIlkTartimEkle(islem.Arac, tartim);
            }

            LogEkle(kullaniciId, islem, "SonradanTartim", "Icerideki araca sonradan tartim eklendi.");
            _unitOfWork.SaveChanges();
            return islem;
        }

        public int SuresiDolanBekleyenTartimlariKapat(DateTime kontrolTarihi, int gunSiniri)
        {
            if (gunSiniri <= 0)
            {
                throw new ArgumentException("Gun siniri pozitif olmalidir.", nameof(gunSiniri));
            }

            var sonTarihBitisi = kontrolTarihi.Date.AddDays(-gunSiniri).AddDays(1);
            var kapanacaklar = _unitOfWork.BekleyenTartimlar.Query()
                .Where(x => x.Durum == KantarSabitleri.BekleyenTartimDurumu.Bekliyor && x.IlkTartimTarihi < sonTarihBitisi)
                .ToList();

            foreach (var bekleyen in kapanacaklar)
            {
                bekleyen.Durum = KantarSabitleri.BekleyenTartimDurumu.SuresiDoldu;
            }

            if (kapanacaklar.Count > 0)
            {
                _unitOfWork.SaveChanges();
            }

            return kapanacaklar.Count;
        }

        private void UcretEkle(Islem islem, string ucretKodu, DateTime tarih)
        {
            var ucret = _unitOfWork.Ucretler.Query()
                .Where(x => x.UcretKodu == ucretKodu && x.AktifMi && x.Yil == tarih.Year)
                .OrderByDescending(x => x.GecerlilikBaslangic)
                .FirstOrDefault();

            if (ucret == null)
            {
                throw new InvalidOperationException("Aktif ucret bulunamadi: " + ucretKodu);
            }

            var islemUcreti = new IslemUcreti
            {
                Islem = islem,
                Ucret = ucret,
                UcretAdi = ucret.UcretAdi,
                Tutar = ucret.Tutar,
                TahakkukTarihi = tarih,
                TahsilEdildiMi = false
            };

            islem.Ucretler.Add(islemUcreti);
            _unitOfWork.IslemUcretleri.Add(islemUcreti);
            islem.ToplamTahakkuk += ucret.Tutar;
        }

        private void BekleyenIlkTartimEkle(Arac arac, Tartim tartim)
        {
            _unitOfWork.BekleyenTartimlar.Add(new BekleyenTartim
            {
                Arac = arac,
                IlkTartim = tartim,
                IlkAgirlikKg = tartim.AgirlikKg,
                IlkTartimTarihi = tartim.TartimTarihi,
                Durum = KantarSabitleri.BekleyenTartimDurumu.Bekliyor
            });
        }

        private void BekleyenIlkTartimTamamla(int aracId, Tartim tamamlayanTartim)
        {
            var bekleyenTartim = _unitOfWork.BekleyenTartimlar.Query()
                .Where(x => x.AracId == aracId && x.Durum == KantarSabitleri.BekleyenTartimDurumu.Bekliyor)
                .OrderByDescending(x => x.IlkTartimTarihi)
                .FirstOrDefault();

            if (bekleyenTartim == null)
            {
                return;
            }

            bekleyenTartim.TamamlayanTartim = tamamlayanTartim;
            bekleyenTartim.Durum = KantarSabitleri.BekleyenTartimDurumu.Tamamlandi;
        }

        private void TahsilEt(Islem islem, int kullaniciId, DateTime tahsilTarihi)
        {
            islem.ToplamTahsilat = islem.ToplamTahakkuk;
            var odenecekUcretler = islem.Ucretler.Where(x => !x.TahsilEdildiMi).ToList();
            var faturaId = UretFaturaId(tahsilTarihi);
            var tahsilatNo = odenecekUcretler.Count > 0 ? UretSiradakiTahsilatNo() : null;
            foreach (var ucret in odenecekUcretler)
            {
                ucret.TahsilEdildiMi = true;
                ucret.TahsilTarihi = tahsilTarihi;
                ucret.TahsilEdenKullaniciId = kullaniciId;
                ucret.FaturaId = faturaId;
                ucret.TahsilatNo = tahsilatNo;
            }
        }

        private string UretSiradakiTahsilatNo()
        {
            var sonNo = _unitOfWork.IslemUcretleri.Query()
                .Where(x => x.TahsilatNo != null && x.TahsilatNo != "")
                .Select(x => x.TahsilatNo)
                .ToList()
                .Select(ParseTahsilatNo)
                .DefaultIfEmpty(0)
                .Max();

            return (sonNo + 1).ToString("0000");
        }

        private static int ParseTahsilatNo(string tahsilatNo)
        {
            int value;
            return int.TryParse(tahsilatNo, out value) ? value : 0;
        }

        private void LogEkle(int kullaniciId, Islem islem, string logTipi, string mesaj)
        {
            _unitOfWork.Loglar.Add(new LogKaydi
            {
                KullaniciId = kullaniciId,
                Islem = islem,
                LogTipi = logTipi,
                Mesaj = mesaj,
                Tarih = DateTime.Now,
                BilgisayarAdi = Environment.MachineName
            });
        }

        private static string NormalizePlaka(string plaka)
        {
            return plaka.Trim().ToUpperInvariant().Replace(" ", string.Empty);
        }

        private static string NormalizeOptional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string UretIslemNo(DateTime tarih)
        {
            return "ISL" + tarih.ToString("yyyyMMddHHmmssfff");
        }

        private static string UretFaturaId(DateTime tarih)
        {
            return "FAT" + tarih.ToString("yyyyMMddHHmmssfff") + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
        }

        public static int HesaplaBeklemeGunSayisi(DateTime girisTarihi, DateTime cikisTarihi)
        {
            var gunSayisi = (cikisTarihi.Date - girisTarihi.Date).Days;
            return gunSayisi > 0 ? gunSayisi : 0;
        }
    }
}
