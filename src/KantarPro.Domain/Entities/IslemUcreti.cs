using System;

namespace KantarPro.Domain.Entities
{
    public class IslemUcreti
    {
        public int IslemUcretId { get; set; }
        public int IslemId { get; set; }
        public int UcretId { get; set; }
        public string UcretAdi { get; set; }
        public decimal Tutar { get; set; }
        public DateTime TahakkukTarihi { get; set; }
        public bool TahsilEdildiMi { get; set; }
        public DateTime? TahsilTarihi { get; set; }
        public int? TahsilEdenKullaniciId { get; set; }
        public string FaturaId { get; set; }

        public virtual Islem Islem { get; set; }
        public virtual Ucret Ucret { get; set; }
        public virtual Kullanici TahsilEdenKullanici { get; set; }
    }
}
