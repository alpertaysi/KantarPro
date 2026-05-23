using System;

namespace KantarPro.Domain.Entities
{
    public class LogKaydi
    {
        public int LogId { get; set; }
        public int? KullaniciId { get; set; }
        public int? IslemId { get; set; }
        public string LogTipi { get; set; }
        public string Mesaj { get; set; }
        public string Detay { get; set; }
        public DateTime Tarih { get; set; }
        public string BilgisayarAdi { get; set; }

        public virtual Kullanici Kullanici { get; set; }
        public virtual Islem Islem { get; set; }
    }
}

