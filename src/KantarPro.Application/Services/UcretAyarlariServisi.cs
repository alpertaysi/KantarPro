using System;
using System.Linq;
using KantarPro.Application.Abstractions;
using KantarPro.Domain;
using KantarPro.Domain.Entities;

namespace KantarPro.Application.Services
{
    public class UcretAyarlari
    {
        public decimal GirisCikisUcreti { get; set; }
        public decimal TartimUcreti { get; set; }
        public decimal BeklemeUcreti { get; set; }
    }

    public class UcretAyarlariServisi
    {
        private readonly IUnitOfWork _unitOfWork;

        public UcretAyarlariServisi(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public UcretAyarlari Getir(DateTime tarih)
        {
            return new UcretAyarlari
            {
                GirisCikisUcreti = AktifTutar(KantarSabitleri.UcretKodu.GirisCikis, tarih),
                TartimUcreti = AktifTutar(KantarSabitleri.UcretKodu.Tartim, tarih),
                BeklemeUcreti = AktifTutar(KantarSabitleri.UcretKodu.Bekleme, tarih)
            };
        }

        public void Guncelle(DateTime tarih, decimal girisCikisUcreti, decimal tartimUcreti, decimal beklemeUcreti)
        {
            Validate(girisCikisUcreti, nameof(girisCikisUcreti));
            Validate(tartimUcreti, nameof(tartimUcreti));
            Validate(beklemeUcreti, nameof(beklemeUcreti));

            GuncelleUcret(KantarSabitleri.UcretKodu.GirisCikis, "Giris-Cikis Ucreti", girisCikisUcreti, tarih);
            GuncelleUcret(KantarSabitleri.UcretKodu.Tartim, "Tartim Ucreti", tartimUcreti, tarih);
            GuncelleUcret(KantarSabitleri.UcretKodu.Bekleme, "Bekleme Ucreti", beklemeUcreti, tarih);
            _unitOfWork.SaveChanges();
        }

        private decimal AktifTutar(string kod, DateTime tarih)
        {
            var ucret = _unitOfWork.Ucretler.Query()
                .Where(x => x.UcretKodu == kod && x.AktifMi && x.Yil == tarih.Year)
                .OrderByDescending(x => x.GecerlilikBaslangic)
                .FirstOrDefault();

            return ucret != null ? ucret.Tutar : 0m;
        }

        private void GuncelleUcret(string kod, string ad, decimal tutar, DateTime tarih)
        {
            var mevcutYilUcreti = _unitOfWork.Ucretler.Query()
                .FirstOrDefault(x => x.UcretKodu == kod && x.Yil == tarih.Year);

            if (mevcutYilUcreti != null)
            {
                mevcutYilUcreti.UcretAdi = ad;
                mevcutYilUcreti.Tutar = tutar;
                mevcutYilUcreti.AktifMi = true;
                mevcutYilUcreti.GecerlilikBaslangic = tarih;
                mevcutYilUcreti.GecerlilikBitis = null;
                return;
            }

            _unitOfWork.Ucretler.Add(new Ucret
            {
                UcretKodu = kod,
                UcretAdi = ad,
                Yil = tarih.Year,
                Tutar = tutar,
                AktifMi = true,
                GecerlilikBaslangic = tarih
            });
        }

        private static void Validate(decimal tutar, string paramName)
        {
            if (tutar < 0)
            {
                throw new ArgumentException("Ucret negatif olamaz.", paramName);
            }
        }
    }
}
