using System;
using System.Linq;
using KantarPro.Application.Services;
using KantarPro.Application.Tests.Fakes;
using KantarPro.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class SahaZiyaretiServisiTests
    {
        [TestMethod]
        public void DoluIlkTartimdanSonraBosZiyaretAcarVeAyniKantarDosyasiniTamamlar()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);

            var ilkZiyaret = servis.GirisKaydet("16 BKK 747", "Firma A", KantarSabitleri.GelisTuru.Dolu, true, 28000m, 1, new DateTime(2026, 5, 12, 11, 0, 0));
            servis.CikisYap(ilkZiyaret.Arac.Plaka, false, null, 1, new DateTime(2026, 5, 12, 19, 30, 0));
            var ikinciZiyaret = servis.GirisKaydet("16 BKK 747", "Firma A", KantarSabitleri.GelisTuru.Bos, true, 12000m, 1, new DateTime(2026, 5, 14, 10, 0, 0));

            Assert.AreEqual(2, uow.IslemListesi.Count);
            Assert.IsTrue(uow.LogListesi.Any(x => x.LogTipi == "SahaGiris"));
            Assert.IsTrue(uow.LogListesi.Any(x => x.LogTipi == "SahaCikis"));
            Assert.AreEqual(2, uow.IslemUcretiListesi.Count(x => x.Ucret.UcretKodu == KantarSabitleri.UcretKodu.GirisCikis));
            Assert.AreEqual(2, uow.IslemUcretiListesi.Count(x => x.Ucret.UcretKodu == KantarSabitleri.UcretKodu.Tartim));
            Assert.AreEqual(KantarSabitleri.KantarDosyasiDurumu.Tamamlandi, uow.KantarDosyasiListesi.Single().Durum);
            Assert.AreEqual(16000m, uow.KantarDosyasiListesi.Single().NetAgirlikKg);
            Assert.AreNotSame(ilkZiyaret, ikinciZiyaret);
        }

        [TestMethod]
        public void TartimsizZiyaretKantarDosyasiAcmaz()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);

            var ziyaret = servis.GirisKaydet("16 TST 001", "Firma B", KantarSabitleri.GelisTuru.Tartimsiz, false, null, 1, new DateTime(2026, 5, 12, 11, 0, 0));
            servis.CikisYap(ziyaret.Arac.Plaka, false, null, 1, new DateTime(2026, 5, 12, 17, 0, 0));

            Assert.AreEqual(0, uow.KantarDosyasiListesi.Count);
            Assert.AreEqual(1, uow.IslemUcretiListesi.Count);
            Assert.IsTrue(uow.IslemUcretiListesi.Single().TahsilEdildiMi);
        }

        [TestMethod]
        public void IslemGizle_IslemiSilindiIsaretlerVeLogYazar()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);
            var ziyaret = servis.GirisKaydet("16 SIL 001", "Silme Test", KantarSabitleri.GelisTuru.Tartimsiz, false, null, 1, new DateTime(2026, 6, 7, 9, 0, 0));

            servis.IslemGizle(ziyaret.IslemId, 1, "Prova kaydi silindi");

            Assert.IsTrue(ziyaret.SilindiMi);
            Assert.AreEqual(KantarSabitleri.IslemDurumu.Silindi, ziyaret.Durum);
            Assert.IsTrue(uow.LogListesi.Any(x => x.LogTipi == "IslemSilindi" && ReferenceEquals(x.Islem, ziyaret)));
        }

        [TestMethod]
        public void TahsilEdilenIlkZiyaretIkinciZiyaretToplaminaEklenmez()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);

            var ilk = servis.GirisKaydet("16 TST 002", "Firma C", KantarSabitleri.GelisTuru.Bos, true, 9000m, 1, new DateTime(2026, 5, 12, 8, 0, 0));
            servis.CikisYap(ilk.Arac.Plaka, false, null, 1, new DateTime(2026, 5, 12, 12, 0, 0));
            var ikinci = servis.GirisKaydet("16 TST 002", "Firma C", KantarSabitleri.GelisTuru.Dolu, true, 24000m, 1, new DateTime(2026, 5, 13, 9, 0, 0));

            Assert.AreEqual(732m, ilk.ToplamTahsilat);
            Assert.AreEqual(732m, ikinci.ToplamTahakkuk);
            Assert.AreEqual(0m, ikinci.ToplamTahsilat);
        }

        [TestMethod]
        public void AyniKantarDosyasinaUcuncuTartimEklenmez()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);
            var ziyaret = servis.GirisKaydet("16 TST 003", "Firma D", KantarSabitleri.GelisTuru.Dolu, true, 26000m, 1, new DateTime(2026, 5, 12, 10, 0, 0));
            servis.SonradanTartimEkle(ziyaret.Arac.Plaka, KantarSabitleri.YukDurumu.Bos, 11000m, 1, new DateTime(2026, 5, 12, 11, 0, 0));

            AssertInvalidOperation(() =>
                servis.SonradanTartimEkle(ziyaret.Arac.Plaka, KantarSabitleri.YukDurumu.Bos, 10900m, 1, new DateTime(2026, 5, 12, 11, 30, 0)));
        }

        [TestMethod]
        public void SonradanIkinciTartimYapilipCikarsaKantarDosyasiTamamlanir()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);
            var ziyaret = servis.GirisKaydet("16 TST 008", "Firma I", KantarSabitleri.GelisTuru.Dolu, true, 26000m, 1, new DateTime(2026, 5, 12, 10, 0, 0));

            servis.SonradanTartimEkle(ziyaret.Arac.Plaka, KantarSabitleri.YukDurumu.Bos, 11000m, 1, new DateTime(2026, 5, 12, 11, 0, 0));
            servis.CikisYap(ziyaret.Arac.Plaka, false, null, 1, new DateTime(2026, 5, 12, 11, 30, 0));

            Assert.AreEqual(KantarSabitleri.IslemDurumu.CikisYapti, ziyaret.Durum);
            Assert.AreEqual(KantarSabitleri.KantarDosyasiDurumu.Tamamlandi, uow.KantarDosyasiListesi.Single().Durum);
            Assert.AreEqual(15000m, uow.KantarDosyasiListesi.Single().NetAgirlikKg);
        }

        [TestMethod]
        public void TartimsizZiyareteSonradanTartimEklenirseKarsiTartimBekler()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);
            var ziyaret = servis.GirisKaydet("16 TST 005", "Firma F", KantarSabitleri.GelisTuru.Tartimsiz, false, null, 1, new DateTime(2026, 5, 12, 10, 0, 0));

            servis.SonradanTartimEkle(ziyaret.Arac.Plaka, KantarSabitleri.YukDurumu.Dolu, 21500m, 1, new DateTime(2026, 5, 12, 10, 20, 0));

            Assert.AreEqual(1, ziyaret.Tartimlar.Count);
            Assert.IsTrue(uow.LogListesi.Any(x => x.LogTipi == "SahaSonradanTartim"));
            Assert.AreEqual(1, uow.KantarDosyasiListesi.Count);
            Assert.AreEqual(KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor, uow.KantarDosyasiListesi.Single().Durum);
            Assert.AreEqual(732m, ziyaret.ToplamTahakkuk);
        }

        [TestMethod]
        public void TartimsizZiyareteSonradanIkiTartimEklenirseDoluBosTamamlanir()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);
            var ziyaret = servis.GirisKaydet("16 TST 009", "Firma J", KantarSabitleri.GelisTuru.Tartimsiz, false, null, 1, new DateTime(2026, 5, 12, 10, 0, 0));

            servis.SonradanTartimEkle(ziyaret.Arac.Plaka, KantarSabitleri.YukDurumu.Dolu, 21500m, 1, new DateTime(2026, 5, 12, 10, 20, 0));
            servis.SonradanTartimEkle(ziyaret.Arac.Plaka, KantarSabitleri.YukDurumu.Bos, 8500m, 1, new DateTime(2026, 5, 12, 11, 20, 0));

            Assert.AreEqual(2, ziyaret.Tartimlar.Count);
            Assert.AreEqual(KantarSabitleri.KantarDosyasiDurumu.Tamamlandi, uow.KantarDosyasiListesi.Single().Durum);
            Assert.AreEqual(13000m, uow.KantarDosyasiListesi.Single().NetAgirlikKg);
        }

        [TestMethod]
        public void SonradanTartimEkle_NotlariGunceller()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);
            var ziyaret = servis.GirisKaydet("16 NOT 002", "Firma", KantarSabitleri.GelisTuru.Tartimsiz, false, null, 1, new DateTime(2026, 5, 12, 10, 0, 0));

            servis.SonradanTartimEkle(ziyaret.Arac.Plaka, KantarSabitleri.YukDurumu.Dolu, 21500m, 1, new DateTime(2026, 5, 12, 10, 20, 0), "Sonradan tartim aciklamasi");

            Assert.AreEqual("Sonradan tartim aciklamasi", ziyaret.Notlar);
        }

        [TestMethod]
        public void GeceYarisiGecilirseBeklemeUcretiTahsilEdilir()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);
            var ziyaret = servis.GirisKaydet("16 TST 004", "Firma E", KantarSabitleri.GelisTuru.Tartimsiz, false, null, 1, new DateTime(2026, 5, 12, 23, 40, 0));

            servis.CikisYap(ziyaret.Arac.Plaka, false, null, 1, new DateTime(2026, 5, 13, 0, 5, 0));

            Assert.AreEqual(1, ziyaret.Ucretler.Count(x => x.Ucret.UcretKodu == KantarSabitleri.UcretKodu.Bekleme));
            Assert.IsTrue(ziyaret.Ucretler.All(x => !string.IsNullOrWhiteSpace(x.TahsilatId)));
        }

        [TestMethod]
        public void CikisYap_TahsilatlaraSurekliArtanTahsilatNoVerir()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);

            var ilk = servis.GirisKaydet("16 TNO 001", "Firma T", KantarSabitleri.GelisTuru.Tartimsiz, false, null, 1, new DateTime(2026, 5, 12, 9, 0, 0));
            servis.CikisYap(ilk.Arac.Plaka, false, null, 1, new DateTime(2026, 5, 12, 10, 0, 0));
            var ikinci = servis.GirisKaydet("16 TNO 002", "Firma T", KantarSabitleri.GelisTuru.Dolu, true, 22000m, 1, new DateTime(2026, 5, 13, 9, 0, 0));
            servis.CikisYap(ikinci.Arac.Plaka, false, null, 1, new DateTime(2026, 5, 13, 10, 0, 0));

            Assert.AreEqual("00001", ilk.Ucretler.Single().TahsilatNo);
            Assert.IsTrue(ikinci.Ucretler.All(x => x.TahsilatNo == "00002"));
        }

        [TestMethod]
        public void CikisYap_MuafIslemlereDeSiraliCikisNoVerir()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);

            var muaf = servis.GirisKaydet("16 MNO 001", "Resmi Kurum", KantarSabitleri.GelisTuru.Dolu, true, 18000m, 1, new DateTime(2026, 5, 12, 9, 0, 0), true, "Resmi gorev");
            servis.CikisYap(muaf.Arac.Plaka, false, null, 1, new DateTime(2026, 5, 12, 10, 0, 0));
            var ucretli = servis.GirisKaydet("16 MNO 002", "Firma", KantarSabitleri.GelisTuru.Tartimsiz, false, null, 1, new DateTime(2026, 5, 12, 11, 0, 0));
            servis.CikisYap(ucretli.Arac.Plaka, false, null, 1, new DateTime(2026, 5, 12, 12, 0, 0));

            Assert.AreEqual("00001", muaf.CikisNo);
            Assert.AreEqual(0, muaf.Ucretler.Count);
            Assert.AreEqual("00002", ucretli.CikisNo);
            Assert.IsTrue(ucretli.Ucretler.All(x => x.TahsilatNo == "00002"));
        }

        [TestMethod]
        public void GirisKaydet_NotlariIslemeYazar()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);

            var ziyaret = servis.GirisKaydet("16 NOT 001", "Firma", KantarSabitleri.GelisTuru.Tartimsiz, false, null, 1, new DateTime(2026, 5, 12, 9, 0, 0), false, null, "Kapıdaki aciklama");

            Assert.AreEqual("Kapıdaki aciklama", ziyaret.Notlar);
        }

        [TestMethod]
        public void GirisKaydet_AyniTarihSaatteFarkliAraclaraBenzersizIslemNoVerir()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);
            var tarih = new DateTime(2026, 6, 1, 10, 0, 45);

            var ilk = servis.GirisKaydet("16 DUP 001", "Firma", KantarSabitleri.GelisTuru.Dolu, true, 22000m, 1, tarih);
            var ikinci = servis.GirisKaydet("16 DUP 002", "Firma", KantarSabitleri.GelisTuru.Dolu, true, 23000m, 1, tarih);

            Assert.AreNotEqual(ilk.IslemNo, ikinci.IslemNo);
        }

        [TestMethod]
        public void SuresiGecmisAcikZiyaretleriGetir_SadeceEskiIceridekileriDondurur()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);
            var kontrolTarihi = new DateTime(2026, 6, 7, 16, 0, 0);

            var eski = servis.GirisKaydet("16 OLD 001", "Firma Eski", KantarSabitleri.GelisTuru.Tartimsiz, false, null, 1, kontrolTarihi.AddHours(-3));
            servis.GirisKaydet("16 NEW 001", "Firma Yeni", KantarSabitleri.GelisTuru.Tartimsiz, false, null, 1, kontrolTarihi.AddMinutes(-30));
            var cikmis = servis.GirisKaydet("16 OUT 001", "Firma Cikti", KantarSabitleri.GelisTuru.Tartimsiz, false, null, 1, kontrolTarihi.AddHours(-4));
            servis.CikisYap(cikmis.Arac.Plaka, false, null, 1, kontrolTarihi.AddHours(-2));

            var sonuc = servis.SuresiGecmisAcikZiyaretleriGetir(kontrolTarihi, TimeSpan.FromHours(2));

            Assert.AreEqual(1, sonuc.Count);
            Assert.AreSame(eski, sonuc.Single());
        }

        [TestMethod]
        public void CikisYap_OdemeTuruSecilirseTahsilatlaraYazar()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);

            var ziyaret = servis.GirisKaydet("16 PAY 001", "Firma P", KantarSabitleri.GelisTuru.Dolu, true, 22000m, 1, new DateTime(2026, 5, 12, 9, 0, 0));

            servis.CikisYap(ziyaret.Arac.Plaka, false, null, 1, new DateTime(2026, 5, 12, 10, 0, 0), KantarSabitleri.OdemeTuru.KrediKarti);

            Assert.IsTrue(ziyaret.Ucretler.All(x => x.OdemeTuru == KantarSabitleri.OdemeTuru.KrediKarti));
        }

        [TestMethod]
        public void MuafGiris_TartimliVeCikisliIslemdeUcretTahakkukEtmez()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);

            var ziyaret = servis.GirisKaydet("16 MUF 001", "Resmi Kurum", KantarSabitleri.GelisTuru.Dolu, true, 18000m, 1, new DateTime(2026, 5, 12, 9, 0, 0), true, "Polis kaçak eşya teslimi");
            servis.CikisYap(ziyaret.Arac.Plaka, true, 9000m, 1, new DateTime(2026, 5, 13, 10, 0, 0));

            Assert.IsTrue(ziyaret.MuafMi);
            Assert.AreEqual("Polis kaçak eşya teslimi", ziyaret.MuafiyetNedeni);
            Assert.AreEqual(0, ziyaret.Ucretler.Count);
            Assert.AreEqual(0m, ziyaret.ToplamTahakkuk);
            Assert.AreEqual(0m, ziyaret.ToplamTahsilat);
            Assert.AreEqual(2, ziyaret.Tartimlar.Count);
        }

        [TestMethod]
        public void MuafGiris_NedenYoksaReddedilir()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);

            AssertInvalidOperation(() =>
                servis.GirisKaydet("16 MUF 002", "Resmi Kurum", KantarSabitleri.GelisTuru.Tartimsiz, false, null, 1, new DateTime(2026, 5, 12, 9, 0, 0), true, ""));
        }

        [TestMethod]
        public void MuafIlkTartim_IkinciZiyareteMuafiyetiDevreder()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);

            var ilk = servis.GirisKaydet("16 MUF 003", "Resmi Kurum", KantarSabitleri.GelisTuru.Dolu, true, 18000m, 1, new DateTime(2026, 5, 12, 9, 0, 0), true, "Polis kaçak eşya teslimi");
            servis.CikisYap(ilk.Arac.Plaka, false, null, 1, new DateTime(2026, 5, 12, 10, 0, 0));

            var ikinci = servis.GirisKaydet(ilk.Arac.Plaka, "Resmi Kurum", KantarSabitleri.GelisTuru.Bos, true, 9000m, 1, new DateTime(2026, 5, 13, 9, 0, 0));

            Assert.IsTrue(ikinci.MuafMi);
            Assert.AreEqual("Polis kaçak eşya teslimi", ikinci.MuafiyetNedeni);
            Assert.AreEqual(0, ikinci.Ucretler.Count);
            Assert.AreEqual(0m, ikinci.ToplamTahakkuk);
        }

        [TestMethod]
        public void BekleyenDoluDosyasiBosGelisOnerir()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);
            servis.GirisKaydet("16 TST 006", "Firma G", KantarSabitleri.GelisTuru.Dolu, true, 20000m, 1, new DateTime(2026, 5, 12, 10, 0, 0));

            Assert.AreEqual(KantarSabitleri.GelisTuru.Bos, servis.GelisTuruOner("16 TST 006"));
        }

        [TestMethod]
        public void OnGunuGecenKantarDosyasiSuresiDolduOlur()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);
            servis.GirisKaydet("16 TST 007", "Firma H", KantarSabitleri.GelisTuru.Bos, true, 9000m, 1, new DateTime(2026, 5, 1, 10, 0, 0));

            var kapanan = servis.SuresiDolanKantarDosyalariniKapat(new DateTime(2026, 5, 12, 9, 0, 0), 10);

            Assert.AreEqual(1, kapanan);
            Assert.IsTrue(uow.LogListesi.Any(x => x.LogTipi == "KantarDosyasiSuresiDoldu"));
            Assert.AreEqual(KantarSabitleri.KantarDosyasiDurumu.SuresiDoldu, uow.KantarDosyasiListesi.Single().Durum);
        }

        [TestMethod]
        public void PlakaHatasiniDuzelt_YanlisIkinciGelisDogruBekleyenDosyayiTamamlar()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);

            var ilk = servis.GirisKaydet("23 FHE 956", "Firma P", KantarSabitleri.GelisTuru.Dolu, true, 28000m, 1, new DateTime(2026, 5, 12, 9, 0, 0));
            servis.CikisYap(ilk.Arac.Plaka, false, null, 1, new DateTime(2026, 5, 12, 10, 0, 0));
            var yanlis = servis.GirisKaydet("23 FEH 956", "Firma P", KantarSabitleri.GelisTuru.Bos, true, 12500m, 1, new DateTime(2026, 5, 14, 9, 0, 0));

            var dogruArac = ilk.Arac;
            servis.PlakaHatasiniDuzelt("23FEH956", "23FHE956", 12000m, 1);

            var dosya = uow.KantarDosyasiListesi.Single(x => x.AracId == dogruArac.AracId);
            var karsiTartim = yanlis.Tartimlar.OrderBy(x => x.TartimTarihi).Last();
            Assert.AreEqual(dogruArac.AracId, yanlis.AracId);
            Assert.IsTrue(yanlis.Tartimlar.All(x => x.AracId == dogruArac.AracId));
            Assert.AreEqual(KantarSabitleri.KantarDosyasiDurumu.Tamamlandi, dosya.Durum);
            Assert.AreEqual(karsiTartim, dosya.KarsiTartim);
            Assert.AreEqual(12000m, karsiTartim.AgirlikKg);
            Assert.AreEqual(16000m, dosya.NetAgirlikKg);
            Assert.AreEqual(KantarSabitleri.YukDurumu.Bos, karsiTartim.YukDurumu);
            Assert.AreEqual(KantarSabitleri.GelisTuru.Bos, yanlis.GelisTuru);
            Assert.AreEqual(0, uow.KantarDosyasiListesi.Count(x => x.Arac.Plaka == "23FEH956"));
            Assert.IsTrue(uow.LogListesi.Any(x => x.LogTipi == "PlakaDuzeltme"));
        }

        [TestMethod]
        public void PlakaHatasiniDuzelt_DogruPlakadaBirdenFazlaBekleyenDosyaVarsaReddeder()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);

            var ilk = servis.GirisKaydet("23 FHE 956", "Firma P", KantarSabitleri.GelisTuru.Dolu, true, 28000m, 1, new DateTime(2026, 5, 12, 9, 0, 0));
            servis.CikisYap(ilk.Arac.Plaka, false, null, 1, new DateTime(2026, 5, 12, 10, 0, 0));
            uow.KantarDosyasiListesi.Add(new KantarPro.Domain.Entities.KantarDosyasi
            {
                KantarDosyasiId = 999,
                Arac = ilk.Arac,
                AracId = ilk.AracId,
                IlkTartim = ilk.Tartimlar.Single(),
                IlkTartimId = ilk.Tartimlar.Single().TartimId,
                Durum = KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor,
                OlusturmaTarihi = new DateTime(2026, 5, 13, 9, 0, 0)
            });
            servis.GirisKaydet("23 FEH 956", "Firma P", KantarSabitleri.GelisTuru.Bos, true, 12500m, 1, new DateTime(2026, 5, 14, 9, 0, 0));

            AssertInvalidOperation(() =>
                servis.PlakaHatasiniDuzelt("23FEH956", "23FHE956", null, 1));
        }

        [TestMethod]
        public void PlakaHatasiniDuzelt_HedefPlakaYoksa_YeniAracOlusturur()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);
            servis.GirisKaydet("23 FEH 956", "Firma P", KantarSabitleri.GelisTuru.Dolu, true, 28000m, 1, new DateTime(2026, 5, 12, 9, 0, 0));

            servis.PlakaHatasiniDuzelt("23FEH956", "23FHE956", null, 1);

            var duzeltilenIslem = uow.IslemListesi.Single();
            var duzeltilenTartim = duzeltilenIslem.Tartimlar.Single();
            var dosya = uow.KantarDosyasiListesi.Single();
            Assert.AreEqual(1, uow.AracListesi.Count(x => x.Plaka == "23FHE956"));
            Assert.AreEqual(0, uow.IslemListesi.Count(x => x.Arac.Plaka == "23FEH956" && x.Durum == KantarSabitleri.IslemDurumu.Iceride));
            Assert.AreEqual("23FHE956", duzeltilenIslem.Arac.Plaka);
            Assert.AreEqual(duzeltilenIslem.AracId, duzeltilenTartim.AracId);
            Assert.AreEqual(duzeltilenIslem.AracId, dosya.AracId);
            Assert.AreEqual(duzeltilenTartim.TartimId, dosya.IlkTartimId);
            Assert.AreEqual(KantarSabitleri.KantarDosyasiDurumu.KarsiTartimBekleniyor, dosya.Durum);
        }

        [TestMethod]
        public void FirmaAdiniGuncelle_AracFirmasiniGuncellerVeLogYazar()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);
            servis.GirisKaydet("16 ABC 123", "", KantarSabitleri.GelisTuru.Tartimsiz, false, null, 1, new DateTime(2026, 5, 12, 9, 0, 0));

            servis.FirmaAdiniGuncelle("16ABC123", "Yeni Firma", 1);

            Assert.AreEqual("Yeni Firma", uow.AracListesi.Single().FirmaAdi);
            Assert.IsTrue(uow.LogListesi.Any(x => x.LogTipi == "FirmaGuncelle" && x.Mesaj.Contains("Yeni Firma")));
        }

        [TestMethod]
        public void IslemMuafYap_UcretleriSilerVeLogYazar()
        {
            var uow = new InMemoryUnitOfWork();
            var servis = new SahaZiyaretiServisi(uow);
            var ziyaret = servis.GirisKaydet("16 ABC 123", "Firma", KantarSabitleri.GelisTuru.Dolu, true, 20000m, 1, new DateTime(2026, 5, 12, 9, 0, 0));
            ziyaret.IslemId = 123;

            servis.IslemMuafYap(123, "Resmi kurum talebi", 1);

            Assert.IsTrue(ziyaret.MuafMi);
            Assert.AreEqual("Resmi kurum talebi", ziyaret.MuafiyetNedeni);
            Assert.AreEqual(0, ziyaret.Ucretler.Count);
            Assert.AreEqual(0m, ziyaret.ToplamTahakkuk);
            Assert.AreEqual(0m, ziyaret.ToplamTahsilat);
            Assert.AreEqual(0, uow.IslemUcretiListesi.Count);
            Assert.IsTrue(uow.LogListesi.Any(x => x.LogTipi == "IslemMuafYap"));
        }

        private static void AssertInvalidOperation(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            Assert.Fail("InvalidOperationException bekleniyordu.");
        }
    }
}
