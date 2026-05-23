using System;

namespace KantarPro.Domain.Entities
{
    public class BekleyenTartim
    {
        public int BekleyenTartimId { get; set; }
        public int AracId { get; set; }
        public int IlkTartimId { get; set; }
        public decimal IlkAgirlikKg { get; set; }
        public DateTime IlkTartimTarihi { get; set; }
        public string Durum { get; set; }
        public int? TamamlayanTartimId { get; set; }

        public virtual Arac Arac { get; set; }
        public virtual Tartim IlkTartim { get; set; }
        public virtual Tartim TamamlayanTartim { get; set; }
    }
}

