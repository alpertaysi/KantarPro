using System;
using System.Collections.Generic;

namespace KantarPro.Domain.Entities
{
    public class Arac
    {
        public Arac()
        {
            Islemler = new List<Islem>();
            Tartimlar = new List<Tartim>();
            KantarDosyalari = new List<KantarDosyasi>();
        }

        public int AracId { get; set; }
        public string Plaka { get; set; }
        public string FirmaAdi { get; set; }
        public string AracTipi { get; set; }
        public string Aciklama { get; set; }
        public bool AktifMi { get; set; }
        public DateTime OlusturmaTarihi { get; set; }

        public virtual ICollection<Islem> Islemler { get; set; }
        public virtual ICollection<Tartim> Tartimlar { get; set; }
        public virtual ICollection<KantarDosyasi> KantarDosyalari { get; set; }
    }
}
