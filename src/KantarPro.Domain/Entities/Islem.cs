using System;
using System.Collections.Generic;

namespace KantarPro.Domain.Entities
{
    public class Islem
    {
        public Islem()
        {
            Tartimlar = new List<Tartim>();
            Ucretler = new List<IslemUcreti>();
        }

        public int IslemId { get; set; }
        public string IslemNo { get; set; }
        public int AracId { get; set; }
        public DateTime GirisTarihi { get; set; }
        public DateTime? CikisTarihi { get; set; }
        public string GelisTuru { get; set; }
        public string Durum { get; set; }
        public int GirisKullaniciId { get; set; }
        public int? CikisKullaniciId { get; set; }
        public decimal ToplamTahakkuk { get; set; }
        public decimal ToplamTahsilat { get; set; }
        public bool MuafMi { get; set; }
        public string MuafiyetNedeni { get; set; }
        public string Notlar { get; set; }

        public virtual Arac Arac { get; set; }
        public virtual Kullanici GirisKullanici { get; set; }
        public virtual Kullanici CikisKullanici { get; set; }
        public virtual ICollection<Tartim> Tartimlar { get; set; }
        public virtual ICollection<IslemUcreti> Ucretler { get; set; }
    }
}
