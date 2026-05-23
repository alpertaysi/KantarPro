using System;
using System.Collections.Generic;

namespace KantarPro.Domain.Entities
{
    public class Kullanici
    {
        public Kullanici()
        {
            GirisIslemleri = new List<Islem>();
            CikisIslemleri = new List<Islem>();
            Tartimlar = new List<Tartim>();
        }

        public int KullaniciId { get; set; }
        public string KullaniciAdi { get; set; }
        public string ParolaHash { get; set; }
        public string AdSoyad { get; set; }
        public string Rol { get; set; }
        public bool AktifMi { get; set; }
        public DateTime? SonGirisTarihi { get; set; }

        public virtual ICollection<Islem> GirisIslemleri { get; set; }
        public virtual ICollection<Islem> CikisIslemleri { get; set; }
        public virtual ICollection<Tartim> Tartimlar { get; set; }
    }
}

