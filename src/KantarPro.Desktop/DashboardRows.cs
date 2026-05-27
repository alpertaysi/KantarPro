namespace KantarPro.Desktop
{
    public class VehicleMovementRow
    {
        public string Plaka { get; set; }
        public string FirmaAdi { get; set; }
        public string GirisTarihi { get; set; }
        public string GirisSaati { get; set; }
        public string CikisTarihi { get; set; }
        public string CikisSaati { get; set; }
        public string DoluCikisTarihi { get; set; }
        public string DoluCikisSaati { get; set; }
        public string BosGelisTarihi { get; set; }
        public string BosGelisSaati { get; set; }
        public string IlkTartimTarihi { get; set; }
        public string IlkTartimSaati { get; set; }
        public string IkinciTartimTarihi { get; set; }
        public string IkinciTartimSaati { get; set; }
        public string SonTartimTarihi { get; set; }
        public string SonTartimSaati { get; set; }
        public string SonTartim { get; set; }
        public string Saat { get; set; }
        public string Tartim { get; set; }
        public string IkinciTartim { get; set; }
        public string NetAgirlik { get; set; }
        public string Ucret { get; set; }
        public string Tahsilat { get; set; }
        public string GirisCikisUcreti { get; set; }
        public string TartimUcreti { get; set; }
        public string BeklemeUcreti { get; set; }
        public string Durum { get; set; }
        public bool KesinCikisMi { get; set; }
    }

    public class DailyTransactionRow
    {
        public string IslemNo { get; set; }
        public string Plaka { get; set; }
        public string Tip { get; set; }
        public string Ucret { get; set; }
        public string Kullanici { get; set; }
    }

    public class DailyRevenueRow
    {
        public int SiraNo { get; set; }
        public string IslemNo { get; set; }
        public string OdemeTuru { get; set; }
        public string MuafiyetNedeni { get; set; }
        public string FirmaAdi { get; set; }
        public string Plaka { get; set; }
        public string GirisTarihi { get; set; }
        public string GirisSaati { get; set; }
        public string CikisTarihi { get; set; }
        public string CikisSaati { get; set; }
        public string IlkTartim { get; set; }
        public string IkinciTartim { get; set; }
        public string NetAgirlik { get; set; }
        public string GirisCikisUcreti { get; set; }
        public string TartimUcreti { get; set; }
        public string BeklemeUcreti { get; set; }
        public string ToplamUcret { get; set; }
    }

    public class PendingWeighingPrototypeRow
    {
        public string Plaka { get; set; }
        public string FirmaAdi { get; set; }
        public string IlkGirisTarihi { get; set; }
        public string IlkGirisSaati { get; set; }
        public string IlkCikisTarihi { get; set; }
        public string IlkCikisSaati { get; set; }
        public string IlkTartimTarihi { get; set; }
        public string IlkTartimSaati { get; set; }
        public string IlkAgirlik { get; set; }
        public string YukDurumu { get; set; }
        public string Aciklama { get; set; }
    }
}
