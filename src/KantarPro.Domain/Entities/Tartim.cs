using System;

namespace KantarPro.Domain.Entities
{
    public class Tartim
    {
        public int TartimId { get; set; }
        public int? IslemId { get; set; }
        public int AracId { get; set; }
        public string TartimTipi { get; set; }
        public decimal AgirlikKg { get; set; }
        public DateTime TartimTarihi { get; set; }
        public bool ComPorttanAlindiMi { get; set; }
        public bool ManuelMi { get; set; }
        public int KullaniciId { get; set; }
        public bool FisYazdirildiMi { get; set; }

        public virtual Islem Islem { get; set; }
        public virtual Arac Arac { get; set; }
        public virtual Kullanici Kullanici { get; set; }
    }
}

