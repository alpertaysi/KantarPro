using System;

namespace KantarPro.Domain.Entities
{
    public class KantarDosyasi
    {
        public int KantarDosyasiId { get; set; }
        public int AracId { get; set; }
        public int IlkTartimId { get; set; }
        public int? KarsiTartimId { get; set; }
        public string Durum { get; set; }
        public decimal? NetAgirlikKg { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
        public DateTime? TamamlanmaTarihi { get; set; }

        public virtual Arac Arac { get; set; }
        public virtual Tartim IlkTartim { get; set; }
        public virtual Tartim KarsiTartim { get; set; }
    }
}
