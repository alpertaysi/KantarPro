using System;
using System.Linq;
using KantarPro.Application.Abstractions;
using KantarPro.Domain;
using KantarPro.Domain.Entities;

namespace KantarPro.Application.Services
{
    public class SahaZiyaretiServisi
    {
        private readonly IUnitOfWork _unitOfWork;

        public SahaZiyaretiServisi(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public string GelisTuruOner(string plaka)
        {
            var bekleyen = BekleyenKantarDosyasiBul(plaka);
            if (bekleyen == null)
            {
                return KantarSabitleri.GelisTuru.Dolu;
            }

            return bekleyen.IlkTartim.YukDurumu == KantarSabitleri.YukDurumu.Dolu
                ? KantarSabitleri.GelisTuru.Bos
                : KantarSabitleri.GelisTuru.Dolu;
        }

        public Islem GirisKaydet(string plaka, string firmaAdi, string gelisTuru, bool tartimYap, decimal? agirlikKg, int kullaniciId, DateTime tarih)
        {
            var arac = AracBulVeyaOlustur(plaka, firmaAdi, tarih);
            if (_unitOfWork.Islemler.Query().Any(x => x.Arac.Plaka == arac.Plaka && x.Durum == KantarSabitleri.IslemDurumu.Iceride))
            {
                throw new InvalidOperationException("Bu plaka icin sahada acik ziyaret var.");
            }

            var ziyaret = new Islem
            {
                Arac = arac,
                AracId = arac.AracId,
                IslemNo = UretIslemNo(tarih),
                GelisTuru = NormalizeGelisTuru(gelisTuru),
                GirisTarihi = tarih,
                Durum = KantarSabitleri.IslemDurumu.Iceride,
                GirisKullaniciId = kullaniciId
            };

            arac.Islemler.Add(ziyaret);
            _unitOfWork.Islemler.Add(ziyaret);
            UcretEkle(ziyaret, KantarSabitleri.UcretKodu.GirisCikis, tarih);

            if (tartimYap)
            {
                TartimKaydet(ziyaret, YukDurumuGetir(ziyaret.GelisTuru), agirlikKg, KantarSabitleri.TartimTipi.Giris, kullaniciId, tarih);
            }

            _unitOfWork.SaveChanges();
            return ziyaret;
        }

        public Islem SonradanTartimEkle(string plaka, string yukDurumu, decimal agirlikKg, int kullaniciId, DateTime tartimTarihi)
        {
            var ziyaret = AcikZiyaretBul(plaka);
            if (TamamlanmisDosyayaBagliMi(ziyaret))
            {
                throw new InvalidOperationException("Bu ziyaretin dolu-bos tartimi tamamlandi. Yeni tartim eklenemez.");
            }

            TartimKaydet(ziyaret, NormalizeYukDurumu(yukDurumu), agirlikKg, KantarSabitleri.TartimTipi.Sonradan, kullaniciId, tartimTarihi);
            _unitOfWork.SaveChanges();
            return ziyaret;
        }

        public Islem CikisYap(string plaka, bool cikistaTart, decimal? agirlikKg, int kullaniciId, DateTime cikisTarihi)
        {
            var ziyaret = AcikZiyaretBul(plaka);
            if (cikistaTart)
            {
                TartimKaydet(ziyaret, KarsiYukDurumuGetir(ziyaret), agirlikKg, KantarSabitleri.TartimTipi.Cikis, kullaniciId, cikisTarihi);
            }

            ziyaret.CikisTarihi = cikisTarihi;
            ziyaret.CikisKullaniciId = kullaniciId;
            ziyaret.Durum = KantarSabitleri.IslemDurumu.CikisYapti;

            var beklemeGunSayisi = HesaplaBeklemeGunSayisi(ziyaret.GirisTarihi, cikisTarihi);
            for (var i = 0; i < beklemeGunSayisi; i++)
            {
                UcretEkle(ziyaret, KantarSabitleri.UcretKodu.Bekleme, cikisTarihi);
            }

            TahsilEt(ziyaret, kullaniciId, cikisTarihi);
            _unitOfWork.SaveChanges();
            return ziyaret;
        }

        public int SuresiDolanKantarDosyalariniKapat(DateTime kontrolTarihi, int gunSiniri)
        {
            if (gunSiniri <= 0)
            {
                throw new ArgumentException("Gun siniri pozitif olmalidir.", nameof(gunSiniri));
            }

            var sinir = kontrolTarihi.Date.AddDays(-gunSiniri);
            var kapanacaklar = _unitOfWork.KantarDosyalari.Query()
                .Where(x => x.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor && x.OlusturmaTarihi.Date <= sinir)
                .ToList();

            foreach (var dosya in kapanacaklar)
            {
                dosya.Durum = KantarSabitleri.KantarDosyasiDurumu.SuresiDoldu;
            }

            if (kapanacaklar.Count > 0)
            {
                _unitOfWork.SaveChanges();
            }

            return kapanacaklar.Count;
        }

        private void TartimKaydet(Islem ziyaret, string yukDurumu, decimal? agirlikKg, string tartimTipi, int kullaniciId, DateTime tarih)
        {
            if (!agirlikKg.HasValue || agirlikKg.Value <= 0)
            {
                throw new ArgumentException("Tartim icin gecerli agirlik zorunludur.", nameof(agirlikKg));
            }

            var tartim = new Tartim
            {
                Islem = ziyaret,
                IslemId = ziyaret.IslemId,
                Arac = ziyaret.Arac,
                AracId = ziyaret.AracId,
                YukDurumu = yukDurumu,
                TartimTipi = tartimTipi,
                AgirlikKg = agirlikKg.Value,
                TartimTarihi = tarih,
                ComPorttanAlindiMi = true,
                ManuelMi = false,
                KullaniciId = kullaniciId
            };

            _unitOfWork.Tartimlar.Add(tartim);
            ziyaret.Tartimlar.Add(tartim);
            ziyaret.Arac.Tartimlar.Add(tartim);
            UcretEkle(ziyaret, KantarSabitleri.UcretKodu.Tartim, tarih);
            KantarDosyasinaBagla(ziyaret.Arac, tartim, tarih);
        }

        private void KantarDosyasinaBagla(Arac arac, Tartim tartim, DateTime tarih)
        {
            var bekleyenler = _unitOfWork.KantarDosyalari.Query()
                .Where(x => x.Arac.Plaka == arac.Plaka && x.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor)
                .OrderByDescending(x => x.OlusturmaTarihi)
                .ToList();

            if (bekleyenler.Count == 0)
            {
                var dosya = new KantarDosyasi
                {
                    Arac = arac,
                    AracId = arac.AracId,
                    IlkTartim = tartim,
                    IlkTartimId = tartim.TartimId,
                    Durum = KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor,
                    OlusturmaTarihi = tarih
                };

                arac.KantarDosyalari.Add(dosya);
                _unitOfWork.KantarDosyalari.Add(dosya);
                return;
            }

            if (bekleyenler.Count > 1)
            {
                throw new InvalidOperationException("Bu plaka icin birden fazla bekleyen kantar dosyasi var.");
            }

            var bekleyen = bekleyenler.Single();
            if (bekleyen.IlkTartim == tartim)
            {
                return;
            }

            if (bekleyen.IlkTartim.YukDurumu == tartim.YukDurumu)
            {
                throw new InvalidOperationException("Karsi tartim dolu-bos yonuyle uyusmuyor.");
            }

            bekleyen.KarsiTartim = tartim;
            bekleyen.KarsiTartimId = tartim.TartimId;
            bekleyen.NetAgirlikKg = Math.Abs(bekleyen.IlkTartim.AgirlikKg - tartim.AgirlikKg);
            bekleyen.Durum = KantarSabitleri.KantarDosyasiDurumu.Tamamlandi;
            bekleyen.TamamlanmaTarihi = tarih;
        }

        private void UcretEkle(Islem ziyaret, string ucretKodu, DateTime tarih)
        {
            var ucret = _unitOfWork.Ucretler.Query()
                .Where(x => x.UcretKodu == ucretKodu && x.AktifMi && x.Yil == tarih.Year)
                .OrderByDescending(x => x.GecerlilikBaslangic)
                .FirstOrDefault();

            if (ucret == null)
            {
                throw new InvalidOperationException("Aktif ucret bulunamadi: " + ucretKodu);
            }

            var ziyaretUcreti = new IslemUcreti
            {
                Islem = ziyaret,
                Ucret = ucret,
                UcretAdi = ucret.UcretAdi,
                Tutar = ucret.Tutar,
                TahakkukTarihi = tarih,
                TahsilEdildiMi = false
            };

            ziyaret.Ucretler.Add(ziyaretUcreti);
            _unitOfWork.IslemUcretleri.Add(ziyaretUcreti);
            ziyaret.ToplamTahakkuk += ucret.Tutar;
        }

        private static void TahsilEt(Islem ziyaret, int kullaniciId, DateTime tarih)
        {
            var tahsilatId = "THS" + tarih.ToString("yyyyMMddHHmmssfff") + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            var odenecekler = ziyaret.Ucretler.Where(x => !x.TahsilEdildiMi).ToList();
            foreach (var ucret in odenecekler)
            {
                ucret.TahsilEdildiMi = true;
                ucret.TahsilTarihi = tarih;
                ucret.TahsilEdenKullaniciId = kullaniciId;
                ucret.TahsilatId = tahsilatId;
                ucret.FaturaId = tahsilatId;
            }

            ziyaret.ToplamTahsilat += odenecekler.Sum(x => x.Tutar);
        }

        private Arac AracBulVeyaOlustur(string plaka, string firmaAdi, DateTime tarih)
        {
            if (string.IsNullOrWhiteSpace(plaka))
            {
                throw new ArgumentException("Plaka bos olamaz.", nameof(plaka));
            }

            var temizPlaka = NormalizePlaka(plaka);
            var arac = _unitOfWork.Araclar.SingleOrDefault(x => x.Plaka == temizPlaka);
            if (arac != null)
            {
                var temizFirma = NormalizeOptional(firmaAdi);
                if (!string.IsNullOrWhiteSpace(temizFirma))
                {
                    arac.FirmaAdi = temizFirma;
                }

                return arac;
            }

            arac = new Arac
            {
                Plaka = temizPlaka,
                FirmaAdi = NormalizeOptional(firmaAdi),
                AktifMi = true,
                OlusturmaTarihi = tarih
            };

            _unitOfWork.Araclar.Add(arac);
            return arac;
        }

        private Islem AcikZiyaretBul(string plaka)
        {
            var temizPlaka = NormalizePlaka(plaka);
            var ziyaret = _unitOfWork.Islemler.Query()
                .FirstOrDefault(x => x.Arac.Plaka == temizPlaka && x.Durum == KantarSabitleri.IslemDurumu.Iceride);

            if (ziyaret == null)
            {
                throw new InvalidOperationException("Bu plaka icin acik saha ziyareti bulunamadi.");
            }

            return ziyaret;
        }

        private KantarDosyasi BekleyenKantarDosyasiBul(string plaka)
        {
            var temizPlaka = NormalizePlaka(plaka);
            var bekleyenler = _unitOfWork.KantarDosyalari.Query()
                .Where(x => x.Arac.Plaka == temizPlaka && x.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor)
                .OrderByDescending(x => x.OlusturmaTarihi)
                .ToList();

            if (bekleyenler.Count > 1)
            {
                throw new InvalidOperationException("Bu plaka icin birden fazla bekleyen kantar dosyasi var.");
            }

            return bekleyenler.FirstOrDefault();
        }

        private bool TamamlanmisDosyayaBagliMi(Islem ziyaret)
        {
            return _unitOfWork.KantarDosyalari.Query()
                .Any(x => x.Durum == KantarSabitleri.KantarDosyasiDurumu.Tamamlandi &&
                    (x.IlkTartim.Islem == ziyaret || (x.KarsiTartim != null && x.KarsiTartim.Islem == ziyaret)));
        }

        private string KarsiYukDurumuGetir(Islem ziyaret)
        {
            var bekleyen = BekleyenKantarDosyasiBul(ziyaret.Arac.Plaka);
            if (bekleyen != null)
            {
                return bekleyen.IlkTartim.YukDurumu == KantarSabitleri.YukDurumu.Dolu
                    ? KantarSabitleri.YukDurumu.Bos
                    : KantarSabitleri.YukDurumu.Dolu;
            }

            return ziyaret.GelisTuru == KantarSabitleri.GelisTuru.Dolu
                ? KantarSabitleri.YukDurumu.Bos
                : KantarSabitleri.YukDurumu.Dolu;
        }

        private static string YukDurumuGetir(string gelisTuru)
        {
            if (gelisTuru == KantarSabitleri.GelisTuru.Dolu)
            {
                return KantarSabitleri.YukDurumu.Dolu;
            }

            if (gelisTuru == KantarSabitleri.GelisTuru.Bos)
            {
                return KantarSabitleri.YukDurumu.Bos;
            }

            return null;
        }

        private static string NormalizeGelisTuru(string gelisTuru)
        {
            if (gelisTuru == KantarSabitleri.GelisTuru.Dolu ||
                gelisTuru == KantarSabitleri.GelisTuru.Bos ||
                gelisTuru == KantarSabitleri.GelisTuru.Tartimsiz)
            {
                return gelisTuru;
            }

            throw new ArgumentException("Gecersiz gelis turu.", nameof(gelisTuru));
        }

        private static string NormalizeYukDurumu(string yukDurumu)
        {
            if (yukDurumu == KantarSabitleri.YukDurumu.Dolu ||
                yukDurumu == KantarSabitleri.YukDurumu.Bos)
            {
                return yukDurumu;
            }

            throw new ArgumentException("Gecersiz yuk durumu.", nameof(yukDurumu));
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
            return "ZYR" + tarih.ToString("yyyyMMddHHmmssfff");
        }

        public static int HesaplaBeklemeGunSayisi(DateTime girisTarihi, DateTime cikisTarihi)
        {
            var gunSayisi = (cikisTarihi.Date - girisTarihi.Date).Days;
            return gunSayisi > 0 ? gunSayisi : 0;
        }
    }
}
