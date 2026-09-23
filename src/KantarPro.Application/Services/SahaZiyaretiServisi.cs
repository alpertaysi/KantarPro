using System;
using System.Collections.Generic;
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

        public Islem GirisKaydet(string plaka, string firmaAdi, string gelisTuru, bool tartimYap, decimal? agirlikKg, int kullaniciId, DateTime tarih, bool muafMi = false, string muafiyetNedeni = null, string notlar = null)
        {
            var arac = AracBulVeyaOlustur(plaka, firmaAdi, tarih);
            if (_unitOfWork.Islemler.Query().Any(x => !x.SilindiMi && x.Arac.Plaka == arac.Plaka && x.Durum == KantarSabitleri.IslemDurumu.Iceride))
            {
                throw new InvalidOperationException("Bu plaka icin sahada acik ziyaret var.");
            }

            var bekleyenDosya = BekleyenKantarDosyasiBul(arac.Plaka);
            if (!muafMi && bekleyenDosya != null && bekleyenDosya.IlkTartim != null && bekleyenDosya.IlkTartim.Islem != null && bekleyenDosya.IlkTartim.Islem.MuafMi)
            {
                muafMi = true;
                muafiyetNedeni = bekleyenDosya.IlkTartim.Islem.MuafiyetNedeni;
            }

            var temizMuafiyetNedeni = NormalizeMuafiyetNedeni(muafMi, muafiyetNedeni);
            var ziyaret = new Islem
            {
                Arac = arac,
                AracId = arac.AracId,
                IslemNo = UretIslemNo(tarih),
                CikisNo = UretSiradakiCikisNo(),
                GelisTuru = NormalizeGelisTuru(gelisTuru),
                GirisTarihi = tarih,
                Durum = KantarSabitleri.IslemDurumu.Iceride,
                GirisKullaniciId = kullaniciId,
                MuafMi = muafMi,
                MuafiyetNedeni = temizMuafiyetNedeni,
                Notlar = NormalizeOptional(notlar)
            };

            arac.Islemler.Add(ziyaret);
            _unitOfWork.Islemler.Add(ziyaret);
            UcretEkle(ziyaret, KantarSabitleri.UcretKodu.GirisCikis, tarih);

            if (tartimYap)
            {
                TartimKaydet(ziyaret, YukDurumuGetir(ziyaret.GelisTuru), agirlikKg, KantarSabitleri.TartimTipi.Giris, kullaniciId, tarih);
            }

            LogEkle(kullaniciId, ziyaret, "SahaGiris", "Saha girisi kaydedildi: " + ziyaret.Arac.Plaka + ", GelisTuru=" + ziyaret.GelisTuru);
            _unitOfWork.SaveChanges();
            return ziyaret;
        }

        public Islem SonradanTartimEkle(string plaka, string yukDurumu, decimal agirlikKg, int kullaniciId, DateTime tartimTarihi, string notlar = null)
        {
            var ziyaret = AcikZiyaretBul(plaka);
            if (TamamlanmisDosyayaBagliMi(ziyaret))
            {
                throw new InvalidOperationException("Bu ziyaretin dolu-bos tartimi tamamlandi. Yeni tartim eklenemez.");
            }

            var temizNotlar = NormalizeOptional(notlar);
            if (!string.IsNullOrWhiteSpace(temizNotlar))
            {
                ziyaret.Notlar = temizNotlar;
            }

            TartimKaydet(ziyaret, NormalizeYukDurumu(yukDurumu), agirlikKg, KantarSabitleri.TartimTipi.Sonradan, kullaniciId, tartimTarihi);
            LogEkle(kullaniciId, ziyaret, "SahaSonradanTartim", "Sonradan tartim eklendi: " + ziyaret.Arac.Plaka + ", " + agirlikKg.ToString("N0") + " kg");
            _unitOfWork.SaveChanges();
            return ziyaret;
        }

        public Islem CikisYap(string plaka, bool cikistaTart, decimal? agirlikKg, int kullaniciId, DateTime cikisTarihi, string odemeTuru = KantarSabitleri.OdemeTuru.Nakit)
        {
            var ziyaret = AcikZiyaretBul(plaka);
            var temizOdemeTuru = ziyaret.MuafMi ? null : NormalizeOdemeTuru(odemeTuru);
            if (!ziyaret.MuafMi && temizOdemeTuru == null)
            {
                throw new ArgumentException("Gecerli odeme turu zorunludur.", nameof(odemeTuru));
            }

            if (cikistaTart)
            {
                TartimKaydet(ziyaret, KarsiYukDurumuGetir(ziyaret), agirlikKg, KantarSabitleri.TartimTipi.Cikis, kullaniciId, cikisTarihi);
            }

            ziyaret.CikisTarihi = cikisTarihi;
            ziyaret.CikisKullaniciId = kullaniciId;
            ziyaret.Durum = KantarSabitleri.IslemDurumu.CikisYapti;
            if (string.IsNullOrWhiteSpace(ziyaret.CikisNo))
            {
                ziyaret.CikisNo = UretSiradakiCikisNo();
            }

            var beklemeGunSayisi = HesaplaBeklemeGunSayisi(ziyaret.GirisTarihi, cikisTarihi);
            for (var i = 0; i < beklemeGunSayisi; i++)
            {
                UcretEkle(ziyaret, KantarSabitleri.UcretKodu.Bekleme, cikisTarihi);
            }

            TahsilEt(ziyaret, kullaniciId, cikisTarihi, temizOdemeTuru, ziyaret.CikisNo);
            LogEkle(kullaniciId, ziyaret, "SahaCikis", "Saha cikisi yapildi: " + ziyaret.Arac.Plaka + ", Tahakkuk=" + ziyaret.ToplamTahakkuk.ToString("N2"));
            _unitOfWork.SaveChanges();
            return ziyaret;
        }

        public int SuresiDolanKantarDosyalariniKapat(DateTime kontrolTarihi, int gunSiniri)
        {
            if (gunSiniri <= 0)
            {
                throw new ArgumentException("Gun siniri pozitif olmalidir.", nameof(gunSiniri));
            }

            var sinirSonu = kontrolTarihi.Date.AddDays(-gunSiniri).AddDays(1);
            var kapanacaklar = _unitOfWork.KantarDosyalari.Query()
                .Where(x => x.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor &&
                            x.OlusturmaTarihi < sinirSonu &&
                            x.IlkTartim != null &&
                            x.IlkTartim.Islem != null &&
                            x.IlkTartim.Islem.Durum == KantarSabitleri.IslemDurumu.CikisYapti &&
                            !x.IlkTartim.Islem.SilindiMi)
                .ToList();

            var ilkTartimIds = kapanacaklar
                .Select(x => x.IlkTartimId)
                .Distinct()
                .ToList();
            var bekleyenByIlkTartimId = ilkTartimIds.Count == 0
                ? new Dictionary<int, BekleyenTartim>()
                : _unitOfWork.BekleyenTartimlar.Query()
                    .Where(x => ilkTartimIds.Contains(x.IlkTartimId) &&
                                x.Durum == KantarSabitleri.BekleyenTartimDurumu.Bekliyor)
                    .ToList()
                    .GroupBy(x => x.IlkTartimId)
                    .ToDictionary(x => x.Key, x => x.First());

            foreach (var dosya in kapanacaklar)
            {
                dosya.Durum = KantarSabitleri.KantarDosyasiDurumu.SuresiDoldu;
                BekleyenTartim eskiBekleyen;
                bekleyenByIlkTartimId.TryGetValue(dosya.IlkTartimId, out eskiBekleyen);
                if (eskiBekleyen != null)
                {
                    eskiBekleyen.Durum = KantarSabitleri.BekleyenTartimDurumu.SuresiDoldu;
                }
                var ilkIslem = dosya.IlkTartim != null ? dosya.IlkTartim.Islem : null;
                var not = gunSiniri.ToString() + " gun icinde ikinci tartima gelmedigi icin kesin cikisa alindi.";
                if (ilkIslem != null)
                {
                    ilkIslem.Notlar = string.IsNullOrWhiteSpace(ilkIslem.Notlar)
                        ? not
                        : ilkIslem.Notlar + Environment.NewLine + not;
                }

                LogEkle(null, ilkIslem, "KantarDosyasiSuresiDoldu", "Karsi tartim suresi doldu: " + dosya.Arac.Plaka + ". " + not);
            }

            if (kapanacaklar.Count > 0)
            {
                _unitOfWork.SaveChanges();
            }

            return kapanacaklar.Count;
        }

        public int SenkronizeBekleyenTartimlar()
        {
            var dosyalar = _unitOfWork.KantarDosyalari.Query().ToList();
            var bekleyenler = _unitOfWork.BekleyenTartimlar.Query().ToList();
            var eklenen = 0;

            foreach (var dosya in dosyalar)
            {
                var eskiBekleyen = bekleyenler.FirstOrDefault(x => x.IlkTartimId == dosya.IlkTartimId);
                if (eskiBekleyen == null && dosya.IlkTartim != null)
                {
                    eskiBekleyen = new BekleyenTartim
                    {
                        Arac = dosya.Arac,
                        AracId = dosya.AracId,
                        IlkTartim = dosya.IlkTartim,
                        IlkTartimId = dosya.IlkTartimId,
                        IlkAgirlikKg = dosya.IlkTartim.AgirlikKg,
                        IlkTartimTarihi = dosya.IlkTartim.TartimTarihi,
                        Durum = dosya.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor
                            ? KantarSabitleri.BekleyenTartimDurumu.Bekliyor
                            : dosya.Durum == KantarPro.Domain.KantarSabitleri.KantarDosyasiDurumu.SuresiDoldu
                                ? KantarSabitleri.BekleyenTartimDurumu.SuresiDoldu
                                : KantarSabitleri.BekleyenTartimDurumu.Tamamlandi,
                        TamamlayanTartim = dosya.KarsiTartim,
                        TamamlayanTartimId = dosya.KarsiTartimId
                    };
                    _unitOfWork.BekleyenTartimlar.Add(eskiBekleyen);
                    bekleyenler.Add(eskiBekleyen);
                    eklenen++;
                }
                else if (eskiBekleyen != null)
                {
                    eskiBekleyen.Durum = dosya.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor
                        ? KantarSabitleri.BekleyenTartimDurumu.Bekliyor
                        : dosya.Durum == KantarSabitleri.KantarDosyasiDurumu.SuresiDoldu
                            ? KantarSabitleri.BekleyenTartimDurumu.SuresiDoldu
                            : KantarSabitleri.BekleyenTartimDurumu.Tamamlandi;
                    eskiBekleyen.TamamlayanTartim = dosya.KarsiTartim;
                    eskiBekleyen.TamamlayanTartimId = dosya.KarsiTartimId;
                }
            }

            if (eklenen > 0 || bekleyenler.Any(x => x.TamamlayanTartimId.HasValue))
            {
                _unitOfWork.SaveChanges();
            }
            return eklenen;
        }

        public Islem AcikZiyaretiGetir(string plaka)
        {
            return AcikZiyaretBul(plaka);
        }

        public IList<Islem> SuresiGecmisAcikZiyaretleriGetir(DateTime kontrolTarihi, TimeSpan esikSure)
        {
            if (esikSure <= TimeSpan.Zero)
            {
                throw new ArgumentException("Esik sure pozitif olmalidir.", nameof(esikSure));
            }

            var sinir = kontrolTarihi.Subtract(esikSure);
            return _unitOfWork.Islemler.Query()
                .Where(x => !x.SilindiMi && x.Durum == KantarSabitleri.IslemDurumu.Iceride && x.GirisTarihi <= sinir)
                .OrderBy(x => x.GirisTarihi)
                .ToList();
        }

        public void IslemGizle(int islemId, int kullaniciId, string neden)
        {
            var islem = _unitOfWork.Islemler.SingleOrDefault(x => x.IslemId == islemId);
            if (islem == null)
            {
                throw new InvalidOperationException("Silinecek işlem bulunamadı.");
            }

            if (islem.SilindiMi)
            {
                return;
            }

            islem.SilindiMi = true;
            islem.Durum = KantarSabitleri.IslemDurumu.Silindi;

            var temizNeden = NormalizeOptional(neden);
            if (!string.IsNullOrWhiteSpace(temizNeden)
            )
            {
                islem.Notlar = string.IsNullOrWhiteSpace(islem.Notlar)
                    ? "Silme nedeni: " + temizNeden
                    : islem.Notlar + Environment.NewLine + "Silme nedeni: " + temizNeden;
            }

            LogEkle(kullaniciId, islem, "IslemSilindi", "İşlem listelerden gizlendi: " + (islem.Arac != null ? islem.Arac.Plaka : islemId.ToString()));
            _unitOfWork.SaveChanges();
        }

        public Arac FirmaAdiniGuncelle(string plaka, string yeniFirmaAdi, int? kullaniciId = null)
        {
            var temizPlaka = NormalizePlakaZorunlu(plaka, nameof(plaka));
            var arac = _unitOfWork.Araclar.SingleOrDefault(x => x.Plaka == temizPlaka);
            if (arac == null)
            {
                throw new InvalidOperationException("Arac kaydi bulunamadi.");
            }

            var eskiFirmaAdi = NormalizeOptional(arac.FirmaAdi);
            arac.FirmaAdi = NormalizeOptional(yeniFirmaAdi);
            if (eskiFirmaAdi != arac.FirmaAdi)
            {
                var acikZiyaret = _unitOfWork.Islemler.Query()
                    .FirstOrDefault(x => !x.SilindiMi && x.Arac.Plaka == temizPlaka && x.Durum == KantarSabitleri.IslemDurumu.Iceride);
                LogEkle(kullaniciId, acikZiyaret, "FirmaGuncelle", "Firma guncellendi: " + temizPlaka + ", " + (eskiFirmaAdi ?? "-") + " -> " + (arac.FirmaAdi ?? "-"));
            }

            _unitOfWork.SaveChanges();
            return arac;
        }

        public Islem IslemMuafYap(int islemId, string muafiyetNedeni, int kullaniciId)
        {
            var ziyaret = _unitOfWork.Islemler.Query().FirstOrDefault(x => x.IslemId == islemId);
            if (ziyaret == null)
            {
                throw new InvalidOperationException("Saha ziyareti bulunamadi.");
            }

            if (ziyaret.Durum != KantarSabitleri.IslemDurumu.Iceride)
            {
                throw new InvalidOperationException("Sadece sahada acik ziyaretler muaf yapilabilir.");
            }

            ziyaret.MuafMi = true;
            ziyaret.MuafiyetNedeni = NormalizeMuafiyetNedeni(true, muafiyetNedeni);

            var ucretler = ziyaret.Ucretler.ToList();
            _unitOfWork.IslemUcretleri.RemoveRange(ucretler);
            ziyaret.Ucretler.Clear();
            ziyaret.ToplamTahakkuk = 0m;
            ziyaret.ToplamTahsilat = 0m;

            LogEkle(kullaniciId, ziyaret, "IslemMuafYap", "Islem ucretten muaf yapildi: " + ziyaret.Arac.Plaka);
            _unitOfWork.SaveChanges();
            return ziyaret;
        }

        public Islem PlakaHatasiniDuzelt(string hataliPlaka, string dogruPlaka, decimal? onaylananIkinciAgirlikKg, int kullaniciId)
        {
            var temizHataliPlaka = NormalizePlakaZorunlu(hataliPlaka, nameof(hataliPlaka));
            var temizDogruPlaka = NormalizePlakaZorunlu(dogruPlaka, nameof(dogruPlaka));
            if (temizHataliPlaka == temizDogruPlaka)
            {
                return AcikZiyaretBul(temizHataliPlaka);
            }

            var ziyaret = _unitOfWork.Islemler.Query()
                .FirstOrDefault(x => !x.SilindiMi && x.Arac.Plaka == temizHataliPlaka && x.Durum == KantarSabitleri.IslemDurumu.Iceride);
            if (ziyaret == null)
            {
                throw new InvalidOperationException("Hatali plaka icin acik saha ziyareti bulunamadi.");
            }

            var eskiArac = ziyaret.Arac;
            var eskiAracId = ziyaret.AracId;
            var hedefArac = _unitOfWork.Araclar.SingleOrDefault(x => x.Plaka == temizDogruPlaka);
            if (hedefArac == null)
            {
                hedefArac = new Arac
                {
                    Plaka = temizDogruPlaka,
                    FirmaAdi = eskiArac != null ? eskiArac.FirmaAdi : null,
                    AktifMi = true,
                    OlusturmaTarihi = ziyaret.GirisTarihi
                };
                _unitOfWork.Araclar.Add(hedefArac);
            }
            else if (hedefArac.AracId != eskiAracId)
            {
                var hedefPlakadaAcikIslemVarMi = _unitOfWork.Islemler.Query().Any(x =>
                    x.AracId == hedefArac.AracId &&
                    !x.SilindiMi &&
                    x.Durum == KantarSabitleri.IslemDurumu.Iceride &&
                    x.IslemId != ziyaret.IslemId);
                if (hedefPlakadaAcikIslemVarMi)
                {
                    throw new InvalidOperationException("Yeni plaka ile iceride acik kayit var. Kayit duzeltilemez.");
                }
            }

            var tartimlar = ziyaret.Tartimlar.OrderBy(x => x.TartimTarihi).ToList();
            var hedefBekleyenDosyalar = _unitOfWork.KantarDosyalari.Query()
                .Where(x => x.Arac.Plaka == temizDogruPlaka &&
                            x.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor &&
                            (x.IlkTartim == null || x.IlkTartim.Islem == null || !x.IlkTartim.Islem.SilindiMi))
                .OrderByDescending(x => x.OlusturmaTarihi)
                .ToList();

            if (hedefBekleyenDosyalar.Count > 1)
            {
                throw new InvalidOperationException("Yeni plaka icin birden fazla bekleyen dolu-bos dosyasi var. Once dogru bekleyen kaydi netlestirin.");
            }

            var islemTartimIdleri = tartimlar.Select(x => x.TartimId).ToList();
            var hataliPlakaDosyalari = _unitOfWork.KantarDosyalari.Query()
                .Where(x => x.Arac.Plaka == temizHataliPlaka && islemTartimIdleri.Contains(x.IlkTartimId))
                .ToList();

            ziyaret.AracId = hedefArac.AracId;
            ziyaret.Arac = hedefArac;
            if (!hedefArac.Islemler.Contains(ziyaret))
            {
                hedefArac.Islemler.Add(ziyaret);
            }

            foreach (var tartim in tartimlar)
            {
                tartim.AracId = hedefArac.AracId;
                tartim.Arac = hedefArac;
                if (!hedefArac.Tartimlar.Contains(tartim))
                {
                    hedefArac.Tartimlar.Add(tartim);
                }
            }

            if (hedefBekleyenDosyalar.Count == 1 && tartimlar.Count > 0)
            {
                _unitOfWork.KantarDosyalari.RemoveRange(hataliPlakaDosyalari);
                DoluBosDosyasiniKarsiTartimlaTamamla(ziyaret, hedefBekleyenDosyalar.Single(), tartimlar.Last(), onaylananIkinciAgirlikKg);
            }
            else
            {
                foreach (var dosya in hataliPlakaDosyalari)
                {
                    dosya.AracId = hedefArac.AracId;
                    dosya.Arac = hedefArac;
                    if (!hedefArac.KantarDosyalari.Contains(dosya))
                    {
                        hedefArac.KantarDosyalari.Add(dosya);
                    }
                }
            }

            LogEkle(kullaniciId, ziyaret, "PlakaDuzeltme", "Plaka duzeltildi: " + temizHataliPlaka + " -> " + temizDogruPlaka);
            _unitOfWork.SaveChanges();
            return ziyaret;
        }

        private void LogEkle(int? kullaniciId, Islem islem, string logTipi, string mesaj)
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

        private static void DoluBosDosyasiniKarsiTartimlaTamamla(Islem ziyaret, KantarDosyasi bekleyen, Tartim karsiTartim, decimal? onaylananIkinciAgirlikKg)
        {
            if (ReferenceEquals(bekleyen.IlkTartim, karsiTartim) ||
                (bekleyen.IlkTartimId > 0 && bekleyen.IlkTartimId == karsiTartim.TartimId))
            {
                return;
            }

            if (onaylananIkinciAgirlikKg.HasValue)
            {
                karsiTartim.AgirlikKg = onaylananIkinciAgirlikKg.Value;
            }

            karsiTartim.YukDurumu = bekleyen.IlkTartim.YukDurumu == KantarSabitleri.YukDurumu.Dolu
                ? KantarSabitleri.YukDurumu.Bos
                : KantarSabitleri.YukDurumu.Dolu;
            ziyaret.GelisTuru = karsiTartim.YukDurumu == KantarSabitleri.YukDurumu.Dolu
                ? KantarSabitleri.GelisTuru.Dolu
                : KantarSabitleri.GelisTuru.Bos;

            bekleyen.KarsiTartim = karsiTartim;
            bekleyen.KarsiTartimId = karsiTartim.TartimId;
            bekleyen.NetAgirlikKg = Math.Abs(bekleyen.IlkTartim.AgirlikKg - karsiTartim.AgirlikKg);
            bekleyen.Durum = KantarSabitleri.KantarDosyasiDurumu.Tamamlandi;
            bekleyen.TamamlanmaTarihi = karsiTartim.TartimTarihi;
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

            KantarFisNoUretici.GarantiEt(tartim, _unitOfWork.Tartimlar.Query());
            _unitOfWork.Tartimlar.Add(tartim);
            ziyaret.Tartimlar.Add(tartim);
            ziyaret.Arac.Tartimlar.Add(tartim);
            UcretEkle(ziyaret, KantarSabitleri.UcretKodu.Tartim, tarih);
            KantarDosyasinaBagla(ziyaret.Arac, tartim, tarih);
        }

        private void KantarDosyasinaBagla(Arac arac, Tartim tartim, DateTime tarih)
        {
            var bekleyenler = _unitOfWork.KantarDosyalari.Query()
                .Where(x => x.Arac.Plaka == arac.Plaka &&
                            x.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor &&
                            (x.IlkTartim == null || x.IlkTartim.Islem == null || !x.IlkTartim.Islem.SilindiMi))
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
                _unitOfWork.BekleyenTartimlar.Add(new BekleyenTartim
                {
                    Arac = arac,
                    AracId = arac.AracId,
                    IlkTartim = tartim,
                    IlkTartimId = tartim.TartimId,
                    IlkAgirlikKg = tartim.AgirlikKg,
                    IlkTartimTarihi = tartim.TartimTarihi,
                    Durum = KantarSabitleri.BekleyenTartimDurumu.Bekliyor
                });
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
                throw new InvalidOperationException("Beklenen dolu-bos tartimi yuk yonuyle uyusmuyor.");
            }

            bekleyen.KarsiTartim = tartim;
            bekleyen.KarsiTartimId = tartim.TartimId;
            bekleyen.NetAgirlikKg = Math.Abs(bekleyen.IlkTartim.AgirlikKg - tartim.AgirlikKg);
            bekleyen.Durum = KantarSabitleri.KantarDosyasiDurumu.Tamamlandi;
            bekleyen.TamamlanmaTarihi = tarih;

            var eskiBekleyenTartim = _unitOfWork.BekleyenTartimlar.Query()
                .FirstOrDefault(x => x.IlkTartimId == bekleyen.IlkTartimId &&
                                     x.Durum == KantarSabitleri.BekleyenTartimDurumu.Bekliyor);
            if (eskiBekleyenTartim != null)
            {
                eskiBekleyenTartim.TamamlayanTartim = tartim;
                eskiBekleyenTartim.TamamlayanTartimId = tartim.TartimId;
                eskiBekleyenTartim.Durum = KantarSabitleri.BekleyenTartimDurumu.Tamamlandi;
            }
        }

        private void UcretEkle(Islem ziyaret, string ucretKodu, DateTime tarih)
        {
            if (ziyaret.MuafMi)
            {
                return;
            }

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

        private void TahsilEt(Islem ziyaret, int kullaniciId, DateTime tarih, string odemeTuru, string tahsilatNo)
        {
            var tahsilatId = "THS" + tarih.ToString("yyyyMMddHHmmssfff") + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            var odenecekler = ziyaret.Ucretler.Where(x => !x.TahsilEdildiMi).ToList();
            var temizOdemeTuru = NormalizeOdemeTuru(odemeTuru);
            foreach (var ucret in odenecekler)
            {
                ucret.TahsilEdildiMi = true;
                ucret.TahsilTarihi = tarih;
                ucret.TahsilEdenKullaniciId = kullaniciId;
                ucret.TahsilatId = tahsilatId;
                ucret.FaturaId = tahsilatId;
                ucret.TahsilatNo = tahsilatNo;
                ucret.OdemeTuru = temizOdemeTuru;
            }

            ziyaret.ToplamTahsilat += odenecekler.Sum(x => x.Tutar);
        }

        private static string NormalizeOdemeTuru(string odemeTuru)
        {
            if (string.IsNullOrWhiteSpace(odemeTuru))
            {
                return null;
            }

            if (odemeTuru == KantarSabitleri.OdemeTuru.Nakit ||
                odemeTuru == KantarSabitleri.OdemeTuru.KrediKarti)
            {
                return odemeTuru;
            }

            throw new ArgumentException("Gecersiz odeme turu.", nameof(odemeTuru));
        }

        private string UretSiradakiCikisNo()
        {
            var ucretNolari = _unitOfWork.IslemUcretleri.Query()
                .Where(x => x.TahsilatNo != null && x.TahsilatNo != "")
                .Select(x => x.TahsilatNo)
                .ToList();
            var cikisNolari = _unitOfWork.Islemler.Query()
                .Where(x => x.CikisNo != null && x.CikisNo != "")
                .Select(x => x.CikisNo)
                .ToList();
            var sonNo = ucretNolari
                .Concat(cikisNolari)
                .Select(ParseTahsilatNo)
                .DefaultIfEmpty(0)
                .Max();

            return (sonNo + 1).ToString("00000");
        }

        private static int ParseTahsilatNo(string tahsilatNo)
        {
            int value;
            return int.TryParse(tahsilatNo, out value) ? value : 0;
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
                .FirstOrDefault(x => !x.SilindiMi && x.Arac.Plaka == temizPlaka && x.Durum == KantarSabitleri.IslemDurumu.Iceride);

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
                .Where(x => x.Arac.Plaka == temizPlaka &&
                            x.Durum == KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor &&
                            (x.IlkTartim == null || x.IlkTartim.Islem == null || !x.IlkTartim.Islem.SilindiMi))
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
            var ziyaretId = ziyaret.IslemId;
            return _unitOfWork.KantarDosyalari.Query()
                .Any(x => x.Durum == KantarSabitleri.KantarDosyasiDurumu.Tamamlandi &&
                    (x.IlkTartim.IslemId == ziyaretId || (x.KarsiTartimId.HasValue && x.KarsiTartim.IslemId == ziyaretId)));
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

        private static string NormalizePlakaZorunlu(string plaka, string parametreAdi)
        {
            if (string.IsNullOrWhiteSpace(plaka))
            {
                throw new ArgumentException("Plaka bos olamaz.", parametreAdi);
            }

            return NormalizePlaka(plaka);
        }

        private static string NormalizeOptional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string NormalizeMuafiyetNedeni(bool muafMi, string muafiyetNedeni)
        {
            var temizNeden = NormalizeOptional(muafiyetNedeni);
            if (muafMi && string.IsNullOrWhiteSpace(temizNeden))
            {
                throw new InvalidOperationException("Ucretten muaf islem icin muafiyet nedeni zorunludur.");
            }

            return muafMi ? temizNeden : null;
        }

        private string UretIslemNo(DateTime tarih)
        {
            var temelNo = "ZYR" + tarih.ToString("yyyyMMddHHmmssfff");
            if (!_unitOfWork.Islemler.Query().Any(x => x.IslemNo == temelNo))
            {
                return temelNo;
            }

            for (var sira = 1; sira <= 999; sira++)
            {
                var adayNo = temelNo + "-" + sira.ToString("000");
                if (!_unitOfWork.Islemler.Query().Any(x => x.IslemNo == adayNo))
                {
                    return adayNo;
                }
            }

            return temelNo + "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
        }

        public static int HesaplaBeklemeGunSayisi(DateTime girisTarihi, DateTime cikisTarihi)
        {
            var gunSayisi = (cikisTarihi.Date - girisTarihi.Date).Days;
            return gunSayisi > 0 ? gunSayisi : 0;
        }
    }
}
