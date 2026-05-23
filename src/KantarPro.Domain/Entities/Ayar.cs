using System;

namespace KantarPro.Domain.Entities
{
    public class Ayar
    {
        public int AyarId { get; set; }
        public string AyarAnahtari { get; set; }
        public string AyarDegeri { get; set; }
        public string AyarGrubu { get; set; }
        public string Aciklama { get; set; }
        public DateTime GuncellemeTarihi { get; set; }
    }
}

