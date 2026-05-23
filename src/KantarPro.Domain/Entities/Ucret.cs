using System;
using System.Collections.Generic;

namespace KantarPro.Domain.Entities
{
    public class Ucret
    {
        public Ucret()
        {
            IslemUcretleri = new List<IslemUcreti>();
        }

        public int UcretId { get; set; }
        public string UcretKodu { get; set; }
        public string UcretAdi { get; set; }
        public int Yil { get; set; }
        public decimal Tutar { get; set; }
        public bool AktifMi { get; set; }
        public DateTime GecerlilikBaslangic { get; set; }
        public DateTime? GecerlilikBitis { get; set; }

        public virtual ICollection<IslemUcreti> IslemUcretleri { get; set; }
    }
}

