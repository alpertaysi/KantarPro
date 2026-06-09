# Operasyon Ekranlari ve Raporlar Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Makbuz, kantar fisi, arastirma, gunluk tahsilat, manuel yedekleme, kullanici degistirme ve admin sunum kilosu akisini saha kullanimina hazir hale getirmek.

**Architecture:** Kantar fisi numarasi tartim kaydi olusurken verilecek; yazdirma sadece mevcut numarayi kullanacak. Arastirma ve rapor ciktilari UI'dan ayrilmis formatter/exporter siniflariyla uretilecek. Kullanici degistirme ve admin manuel kilo yetkisi ana pencerenin mevcut oturum durumuna baglanacak.

**Tech Stack:** WPF .NET Framework 4.8, Entity Framework 6, MSTest, SQL Server Express, OKI 5720 raw text printing, PowerShell backup scripts.

---

## File Structure

- Modify `src/KantarPro.Application/Services/SahaZiyaretiServisi.cs`
  - Tartim olustururken 5 haneli `KantarFisNo` atar.
  - Mevcut `TartimKaydet(...)` tek merkezi nokta oldugu icin numara uretimi burada yapilir.
- Modify `src/KantarPro.Application/Services/IslemServisi.cs`
  - Eski servis yolundan olusan tartimlarda da ayni 5 haneli fis no kurali korunur.
- Create `src/KantarPro.Application/Services/KantarFisNoUretici.cs`
  - Uygulama katmaninda test edilebilir, side-effect'i sadece verilen `Tartim` nesnesine numara yazmak olan yardimci.
- Modify `src/KantarPro.Desktop/MainWindow.xaml.cs`
  - Yazdirma sirasinda numara artirmayi durdurur; sadece eksik eski kayitlar icin garanti eder.
  - `Makbuz`, `Arastir`, `Yedekle`, `Kullanici Degistir`, admin manuel kilo davranislarini baglar.
- Modify `src/KantarPro.Desktop/MainWindow.xaml`
  - Alt `Menu` butonunu kaldirir.
  - `Yedekler` butonunu `Yedekle` yapar.
  - `Sifre Degistir` butonunu `Kullanici Degistir` yapar.
  - Admin haricinde kilo kutusunu salt okunur tutacak baglantiya hazirlar.
- Create `src/KantarPro.Desktop/SearchWindow.xaml`
- Create `src/KantarPro.Desktop/SearchWindow.xaml.cs`
  - Plaka, firma, giris tarihi araligi, cikis tarihi araligi filtreleriyle gecmis kayit arar.
  - Sonuclari yazdirma ve disa aktarma icin hazirlar.
- Create `src/KantarPro.Desktop/SearchResultRow.cs`
  - Arastirma ekraninin satir modeli.
- Create `src/KantarPro.Desktop/SearchResultsTextFormatter.cs`
  - Arastirma sonuclarini OKI ham metin cikti formatina cevirir.
- Create `src/KantarPro.Desktop/TextPreviewWindow.xaml`
- Create `src/KantarPro.Desktop/TextPreviewWindow.xaml.cs`
  - Gunluk tahsilat OKI dokumu ve arastirma OKI dokumu icin ham metin onizleme/yazdirma penceresi.
- Modify `src/KantarPro.Desktop/DailyRevenueExcelExporter.cs`
  - CSV yerine gercek `.xlsx` uretir.
- Modify `src/KantarPro.Desktop/MainWindow.DataAndFormatting.cs`
  - Gunluk tahsilat Excel filtre uzantisini `.xlsx` yapar.
  - OKI dokumunu direkt yazdirma yerine onizleme penceresine yollar.
- Modify `tools/BackupKantarPro.ps1`
  - `-IgnoreWeekend` parametresi ekler; otomatik gorev hafta sonu atlar, manuel buton hafta sonu da calisir.
- Create `src/KantarPro.Desktop/UserSwitchWindow.xaml`
- Create `src/KantarPro.Desktop/UserSwitchWindow.xaml.cs`
  - Aktif kullanicilari combobox'ta listeler, sifreyle oturum degistirir.
  - Ayni pencerede secili kullanicinin kendi sifresini degistirmesine izin verir.
- Create `src/KantarPro.Desktop/ScaleWeightSelector.cs`
  - Indikator kilosu ve admin manuel kilo onceligini test edilebilir hale getirir.
- Modify `tests/KantarPro.Application.Tests/SahaZiyaretiServisiTests.cs`
  - Tartimli giris ve sonradan tartim icin fis no testleri.
- Modify `tests/KantarPro.Application.Tests/IslemServisiTests.cs`
  - Eski servis tartimlarinda fis no testleri.
- Modify `tests/KantarPro.Application.Tests/DailyRevenueExcelExporterTests.cs`
  - `.xlsx` paket icerigini dogrular.
- Create `tests/KantarPro.Application.Tests/SearchResultsTextFormatterTests.cs`
  - Arastirma dokum metnini dogrular.
- Create `tests/KantarPro.Application.Tests/ScaleWeightSelectorTests.cs`
  - Admin manuel kilo ve memur indikator kilosu kurallarini dogrular.

## Task 1: Kantar Fis No Uretimini Tartim Kaydina Tasi

**Files:**
- Create: `src/KantarPro.Application/Services/KantarFisNoUretici.cs`
- Modify: `src/KantarPro.Application/Services/SahaZiyaretiServisi.cs`
- Modify: `src/KantarPro.Application/Services/IslemServisi.cs`
- Test: `tests/KantarPro.Application.Tests/SahaZiyaretiServisiTests.cs`
- Test: `tests/KantarPro.Application.Tests/IslemServisiTests.cs`

- [ ] **Step 1: Write failing tests for SahaZiyaretiServisi fis no creation**

Append these tests to `tests/KantarPro.Application.Tests/SahaZiyaretiServisiTests.cs`:

```csharp
[TestMethod]
public void GirisKaydet_TartimliKaydaBesHaneliKantarFisNoAtar()
{
    var uow = new InMemoryUnitOfWork();
    uow.Ucretler.Add(new Ucret { UcretKodu = KantarSabitleri.UcretKodu.GirisCikis, UcretAdi = "Giris", Tutar = 366m, AktifMi = true, Yil = 2026, GecerlilikBaslangic = new DateTime(2026, 1, 1) });
    uow.Ucretler.Add(new Ucret { UcretKodu = KantarSabitleri.UcretKodu.Tartim, UcretAdi = "Tartim", Tutar = 366m, AktifMi = true, Yil = 2026, GecerlilikBaslangic = new DateTime(2026, 1, 1) });
    var servis = new SahaZiyaretiServisi(uow);

    var islem = servis.GirisKaydet("16FIS001", "FIS TEST", KantarSabitleri.GelisTuru.Dolu, true, 34000m, 1, new DateTime(2026, 6, 9, 10, 0, 0));

    Assert.AreEqual("00001", islem.Tartimlar.Single().KantarFisNo);
}

[TestMethod]
public void SonradanTartimEkle_MevcutSonNumaradanDevamEder()
{
    var uow = new InMemoryUnitOfWork();
    uow.Ucretler.Add(new Ucret { UcretKodu = KantarSabitleri.UcretKodu.GirisCikis, UcretAdi = "Giris", Tutar = 366m, AktifMi = true, Yil = 2026, GecerlilikBaslangic = new DateTime(2026, 1, 1) });
    uow.Ucretler.Add(new Ucret { UcretKodu = KantarSabitleri.UcretKodu.Tartim, UcretAdi = "Tartim", Tutar = 366m, AktifMi = true, Yil = 2026, GecerlilikBaslangic = new DateTime(2026, 1, 1) });
    uow.Tartimlar.Add(new Tartim { TartimId = 99, KantarFisNo = "00041", AgirlikKg = 12000m, TartimTarihi = new DateTime(2026, 6, 8), YukDurumu = KantarSabitleri.YukDurumu.Dolu, TartimTipi = KantarSabitleri.TartimTipi.Giris, KullaniciId = 1 });
    var servis = new SahaZiyaretiServisi(uow);
    servis.GirisKaydet("16FIS002", "FIS TEST", KantarSabitleri.GelisTuru.Dolu, false, null, 1, new DateTime(2026, 6, 9, 10, 0, 0));

    var islem = servis.SonradanTartimEkle("16FIS002", KantarSabitleri.YukDurumu.Dolu, 33000m, 1, new DateTime(2026, 6, 9, 11, 0, 0));

    Assert.AreEqual("00042", islem.Tartimlar.Single().KantarFisNo);
}
```

- [ ] **Step 2: Run the failing tests**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Restore,Build /p:Configuration=Debug
& "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\TestPlatform\vstest.console.exe" tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll /Tests:GirisKaydet_TartimliKaydaBesHaneliKantarFisNoAtar,SonradanTartimEkle_MevcutSonNumaradanDevamEder
```

Expected: tests fail because `KantarFisNo` is null.

- [ ] **Step 3: Create KantarFisNoUretici**

Create `src/KantarPro.Application/Services/KantarFisNoUretici.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KantarPro.Domain.Entities;

namespace KantarPro.Application.Services
{
    internal static class KantarFisNoUretici
    {
        public static string GarantiEt(Tartim tartim, IEnumerable<Tartim> tumTartimlar)
        {
            if (tartim == null)
            {
                throw new ArgumentNullException(nameof(tartim));
            }

            if (!string.IsNullOrWhiteSpace(tartim.KantarFisNo))
            {
                tartim.KantarFisNo = Formatla(tartim.KantarFisNo);
                return tartim.KantarFisNo;
            }

            var sonNumara = (tumTartimlar ?? Enumerable.Empty<Tartim>())
                .Where(x => x != null && !ReferenceEquals(x, tartim))
                .Select(x => ParseFisNo(x.KantarFisNo))
                .DefaultIfEmpty(0)
                .Max();

            tartim.KantarFisNo = (sonNumara + 1).ToString("00000", CultureInfo.InvariantCulture);
            return tartim.KantarFisNo;
        }

        private static int ParseFisNo(string value)
        {
            int parsed;
            return int.TryParse((value ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                ? parsed
                : 0;
        }

        private static string Formatla(string value)
        {
            int parsed;
            return int.TryParse((value ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                ? parsed.ToString("00000", CultureInfo.InvariantCulture)
                : value.Trim();
        }
    }
}
```

- [ ] **Step 4: Use it in SahaZiyaretiServisi.TartimKaydet**

In `src/KantarPro.Application/Services/SahaZiyaretiServisi.cs`, inside private `TartimKaydet(...)`, immediately after creating `var tartim = new Tartim { ... };`, add:

```csharp
KantarFisNoUretici.GarantiEt(tartim, _unitOfWork.Tartimlar.Query());
```

- [ ] **Step 5: Use it in IslemServisi every time a Tartim is created**

In `src/KantarPro.Application/Services/IslemServisi.cs`, after each `new Tartim { ... }` block and before `_unitOfWork.Tartimlar.Add(...)`, add:

```csharp
KantarFisNoUretici.GarantiEt(tartim, _unitOfWork.Tartimlar.Query());
```

For variables named `ikinciTartim`, use:

```csharp
KantarFisNoUretici.GarantiEt(ikinciTartim, _unitOfWork.Tartimlar.Query());
```

- [ ] **Step 6: Run tests**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Debug
& "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\TestPlatform\vstest.console.exe" tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll
```

Expected: all tests pass.

- [ ] **Step 7: Commit**

```powershell
git add src/KantarPro.Application/Services/KantarFisNoUretici.cs src/KantarPro.Application/Services/SahaZiyaretiServisi.cs src/KantarPro.Application/Services/IslemServisi.cs tests/KantarPro.Application.Tests/SahaZiyaretiServisiTests.cs tests/KantarPro.Application.Tests/IslemServisiTests.cs
git commit -m "Kantar fis numarasini tartim kaydinda uret"
```

## Task 2: Makbuz Button Must Not Increment Fis No

**Files:**
- Modify: `src/KantarPro.Desktop/MainWindow.xaml.cs`
- Test: `tests/KantarPro.Application.Tests/KantarFisFormatterTests.cs`

- [ ] **Step 1: Write formatter regression test**

Append to `tests/KantarPro.Application.Tests/KantarFisFormatterTests.cs`:

```csharp
[TestMethod]
public void PreviewData_FromVehicleRow_MevcutBesHaneliFisNoyuAynenKullanir()
{
    var row = new VehicleMovementRow
    {
        Plaka = "16FIS009",
        IslemNo = "ZYR20260609101010000",
        KantarFisNo = "00077",
        GirisTarihi = "09.06.2026",
        GirisSaati = "10:10:10",
        TartimKg = "34000 kg",
        IlkTartimKg = "34000 kg"
    };

    var data = KantarFisPreviewData.FromVehicleRow(row);

    Assert.AreEqual("00077", data.FisNo);
}
```

- [ ] **Step 2: Run the test**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\TestPlatform\vstest.console.exe" tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll /Tests:PreviewData_FromVehicleRow_MevcutBesHaneliFisNoyuAynenKullanir
```

Expected: pass or fail depending on current formatter; keep it as regression guard.

- [ ] **Step 3: Change print flow wording without changing number**

In `src/KantarPro.Desktop/MainWindow.xaml.cs`, keep `EnsureKantarFisNoForVehicleRow(...)` and `EnsureKantarFisNoForPendingRow(...)` only as old-data repair. Do not call a method that creates a new number after row already has `KantarFisNo`.

Use this pattern in `PrintKantarFisi(VehicleMovementRow row)`:

```csharp
row.KantarFisNo = string.IsNullOrWhiteSpace(row.KantarFisNo)
    ? EnsureKantarFisNoForVehicleRow(row)
    : KantarFisPreviewData.FormatFisNo(row);

var prompt = new PrintReceiptPromptWindow("Kantar fisi yazdirilsin mi?", "Fis No: " + row.KantarFisNo)
{
    Owner = this
};
if (prompt.ShowDialog() != true)
{
    return;
}

PrintKantarFisiCore(row);
```

- [ ] **Step 4: Run tests**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Debug
& "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\TestPlatform\vstest.console.exe" tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/KantarPro.Desktop/MainWindow.xaml.cs tests/KantarPro.Application.Tests/KantarFisFormatterTests.cs
git commit -m "Makbuz yazdirmada fis numarasini sabit tut"
```

## Task 3: Arastirma Window With Historical Filters

**Files:**
- Create: `src/KantarPro.Desktop/SearchResultRow.cs`
- Create: `src/KantarPro.Desktop/SearchResultsTextFormatter.cs`
- Create: `src/KantarPro.Desktop/SearchWindow.xaml`
- Create: `src/KantarPro.Desktop/SearchWindow.xaml.cs`
- Modify: `src/KantarPro.Desktop/MainWindow.xaml`
- Modify: `src/KantarPro.Desktop/MainWindow.xaml.cs`
- Test: `tests/KantarPro.Application.Tests/SearchResultsTextFormatterTests.cs`

- [ ] **Step 1: Create SearchResultRow**

Create `src/KantarPro.Desktop/SearchResultRow.cs`:

```csharp
namespace KantarPro.Desktop
{
    public sealed class SearchResultRow
    {
        public int SiraNo { get; set; }
        public string IslemNo { get; set; }
        public string Durum { get; set; }
        public string Plaka { get; set; }
        public string Firma { get; set; }
        public string GirisTarihi { get; set; }
        public string GirisSaati { get; set; }
        public string CikisTarihi { get; set; }
        public string CikisSaati { get; set; }
        public string BirinciTartim { get; set; }
        public string IkinciTartim { get; set; }
        public string Net { get; set; }
        public string TahsilatNo { get; set; }
        public string OdemeTuru { get; set; }
        public string KantarFisNo { get; set; }
        public string Kullanici { get; set; }
        public string ToplamUcret { get; set; }
        public string Notlar { get; set; }
    }
}
```

- [ ] **Step 2: Create formatter and test**

Create `tests/KantarPro.Application.Tests/SearchResultsTextFormatterTests.cs`:

```csharp
using KantarPro.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class SearchResultsTextFormatterTests
    {
        [TestMethod]
        public void Build_PlakaFirmaTarihVeTahsilatiYazar()
        {
            var text = SearchResultsTextFormatter.Build(new[]
            {
                new SearchResultRow
                {
                    SiraNo = 1,
                    IslemNo = "00018",
                    Durum = "Dolu-bos tamamlandi",
                    Plaka = "16ARA001",
                    Firma = "DENEME",
                    GirisTarihi = "09.06.2026",
                    CikisTarihi = "09.06.2026",
                    BirinciTartim = "34000 kg",
                    IkinciTartim = "9000 kg",
                    Net = "25000 kg",
                    TahsilatNo = "00018",
                    OdemeTuru = "Nakit",
                    KantarFisNo = "00018",
                    Kullanici = "Admin",
                    ToplamUcret = "732,00 TL"
                }
            }, "Arastirma Sonuclari");

            StringAssert.Contains(text, "16ARA001");
            StringAssert.Contains(text, "DENEME");
            StringAssert.Contains(text, "00018");
            StringAssert.Contains(text, "25000 kg");
        }
    }
}
```

Create `src/KantarPro.Desktop/SearchResultsTextFormatter.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KantarPro.Desktop
{
    public static class SearchResultsTextFormatter
    {
        public static string Build(IEnumerable<SearchResultRow> rows, string title)
        {
            var list = (rows ?? Enumerable.Empty<SearchResultRow>()).ToList();
            var builder = new StringBuilder();
            builder.AppendLine(title ?? "Arastirma Sonuclari");
            builder.AppendLine(new string('-', 132));
            builder.AppendLine("No  Islem  Plaka       Firma                Giris       Cikis       1.Tartim     2.Tartim     Net         Tahsilat Odeme");
            builder.AppendLine(new string('-', 132));
            foreach (var row in list)
            {
                builder.Append(Pad(row.SiraNo.ToString(), 4));
                builder.Append(Pad(row.IslemNo, 7));
                builder.Append(Pad(row.Plaka, 12));
                builder.Append(Pad(row.Firma, 21));
                builder.Append(Pad(row.GirisTarihi, 12));
                builder.Append(Pad(row.CikisTarihi, 12));
                builder.Append(Pad(row.BirinciTartim, 13));
                builder.Append(Pad(row.IkinciTartim, 13));
                builder.Append(Pad(row.Net, 12));
                builder.Append(Pad(row.TahsilatNo, 9));
                builder.Append(Pad(row.OdemeTuru, 12));
                builder.AppendLine();
            }
            builder.AppendLine(new string('-', 132));
            builder.AppendLine("Kayit: " + list.Count);
            return builder.ToString();
        }

        private static string Pad(string value, int width)
        {
            value = value ?? string.Empty;
            return value.Length > width ? value.Substring(0, width) : value.PadRight(width);
        }
    }
}
```

- [ ] **Step 3: Create SearchWindow XAML**

Create `src/KantarPro.Desktop/SearchWindow.xaml`:

```xml
<Window x:Class="KantarPro.Desktop.SearchWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Araştır" Width="1180" Height="720" WindowStartupLocation="CenterOwner"
        Background="#F3F6FA">
    <DockPanel Margin="12">
        <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="0,0,0,10">
            <StackPanel Margin="0,0,12,0">
                <TextBlock Text="Plaka" />
                <TextBox x:Name="PlateTextBox" Width="140" />
            </StackPanel>
            <StackPanel Margin="0,0,12,0">
                <TextBlock Text="Firma" />
                <TextBox x:Name="CompanyTextBox" Width="180" />
            </StackPanel>
            <StackPanel Margin="0,0,12,0">
                <TextBlock Text="Giriş Başlangıç" />
                <DatePicker x:Name="EntryStartDatePicker" Width="140" />
            </StackPanel>
            <StackPanel Margin="0,0,12,0">
                <TextBlock Text="Giriş Bitiş" />
                <DatePicker x:Name="EntryEndDatePicker" Width="140" />
            </StackPanel>
            <StackPanel Margin="0,0,12,0">
                <TextBlock Text="Çıkış Başlangıç" />
                <DatePicker x:Name="ExitStartDatePicker" Width="140" />
            </StackPanel>
            <StackPanel Margin="0,0,12,0">
                <TextBlock Text="Çıkış Bitiş" />
                <DatePicker x:Name="ExitEndDatePicker" Width="140" />
            </StackPanel>
            <Button Content="Ara" Width="110" Height="34" Margin="0,18,8,0" Click="Search_Click" />
            <Button Content="OKI Önizle" Width="120" Height="34" Margin="0,18,8,0" Click="PreviewOki_Click" />
            <Button Content="Kapat" Width="90" Height="34" Margin="0,18,0,0" Click="Close_Click" />
        </StackPanel>
        <DataGrid x:Name="ResultsGrid" AutoGenerateColumns="False" IsReadOnly="True" SelectionMode="Single">
            <DataGrid.Columns>
                <DataGridTextColumn Header="Sıra" Binding="{Binding SiraNo}" Width="55" />
                <DataGridTextColumn Header="İşlem No" Binding="{Binding IslemNo}" Width="85" />
                <DataGridTextColumn Header="Durum" Binding="{Binding Durum}" Width="140" />
                <DataGridTextColumn Header="Plaka" Binding="{Binding Plaka}" Width="100" />
                <DataGridTextColumn Header="Firma" Binding="{Binding Firma}" Width="160" />
                <DataGridTextColumn Header="Giriş Tarihi" Binding="{Binding GirisTarihi}" Width="100" />
                <DataGridTextColumn Header="Giriş Saati" Binding="{Binding GirisSaati}" Width="90" />
                <DataGridTextColumn Header="Çıkış Tarihi" Binding="{Binding CikisTarihi}" Width="100" />
                <DataGridTextColumn Header="Çıkış Saati" Binding="{Binding CikisSaati}" Width="90" />
                <DataGridTextColumn Header="1. Tartım" Binding="{Binding BirinciTartim}" Width="110" />
                <DataGridTextColumn Header="2. Tartım" Binding="{Binding IkinciTartim}" Width="110" />
                <DataGridTextColumn Header="Net" Binding="{Binding Net}" Width="110" />
                <DataGridTextColumn Header="Tahsilat No" Binding="{Binding TahsilatNo}" Width="95" />
                <DataGridTextColumn Header="Ödeme Türü" Binding="{Binding OdemeTuru}" Width="105" />
                <DataGridTextColumn Header="Fiş No" Binding="{Binding KantarFisNo}" Width="85" />
                <DataGridTextColumn Header="Kullanıcı" Binding="{Binding Kullanici}" Width="130" />
                <DataGridTextColumn Header="Toplam" Binding="{Binding ToplamUcret}" Width="110" />
                <DataGridTextColumn Header="Notlar" Binding="{Binding Notlar}" Width="220" />
            </DataGrid.Columns>
        </DataGrid>
    </DockPanel>
</Window>
```

- [ ] **Step 4: Create SearchWindow code-behind**

Create `src/KantarPro.Desktop/SearchWindow.xaml.cs` with database query:

```csharp
using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Windows;
using KantarPro.Domain;
using KantarPro.Infrastructure.Data;

namespace KantarPro.Desktop
{
    public partial class SearchWindow : Window
    {
        private readonly ObservableCollection<SearchResultRow> _rows = new ObservableCollection<SearchResultRow>();

        public SearchWindow()
        {
            InitializeComponent();
            ResultsGrid.ItemsSource = _rows;
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            LoadRows();
        }

        private void PreviewOki_Click(object sender, RoutedEventArgs e)
        {
            if (_rows.Count == 0)
            {
                MessageBox.Show("Önizleme için önce arama yapın.", "Araştır", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var preview = new TextPreviewWindow("Araştırma Dökümü", SearchResultsTextFormatter.Build(_rows, "Araştırma Sonuçları"))
            {
                Owner = this
            };
            preview.ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LoadRows()
        {
            var plaka = (PlateTextBox.Text ?? string.Empty).Trim().ToUpperInvariant();
            var firma = (CompanyTextBox.Text ?? string.Empty).Trim().ToUpperInvariant();
            var girisBaslangic = EntryStartDatePicker.SelectedDate.HasValue ? EntryStartDatePicker.SelectedDate.Value.Date : (DateTime?)null;
            var girisBitisExclusive = EntryEndDatePicker.SelectedDate.HasValue ? EntryEndDatePicker.SelectedDate.Value.Date.AddDays(1) : (DateTime?)null;
            var cikisBaslangic = ExitStartDatePicker.SelectedDate.HasValue ? ExitStartDatePicker.SelectedDate.Value.Date : (DateTime?)null;
            var cikisBitisExclusive = ExitEndDatePicker.SelectedDate.HasValue ? ExitEndDatePicker.SelectedDate.Value.Date.AddDays(1) : (DateTime?)null;

            using (var context = KantarDbContextFactory.Create())
            {
                var query = context.Islemler
                    .Include(x => x.Arac)
                    .Include(x => x.Tartimlar)
                    .Include(x => x.Ucretler)
                    .Where(x => !x.SilindiMi);

                if (!string.IsNullOrWhiteSpace(plaka))
                {
                    query = query.Where(x => x.Arac.Plaka.Contains(plaka));
                }

                if (!string.IsNullOrWhiteSpace(firma))
                {
                    query = query.Where(x => x.Arac.FirmaAdi != null && x.Arac.FirmaAdi.ToUpper().Contains(firma));
                }

                if (girisBaslangic.HasValue)
                {
                    query = query.Where(x => x.GirisTarihi >= girisBaslangic.Value);
                }

                if (girisBitisExclusive.HasValue)
                {
                    query = query.Where(x => x.GirisTarihi < girisBitisExclusive.Value);
                }

                if (cikisBaslangic.HasValue)
                {
                    query = query.Where(x => x.CikisTarihi.HasValue && x.CikisTarihi.Value >= cikisBaslangic.Value);
                }

                if (cikisBitisExclusive.HasValue)
                {
                    query = query.Where(x => x.CikisTarihi.HasValue && x.CikisTarihi.Value < cikisBitisExclusive.Value);
                }

                var list = query.OrderByDescending(x => x.GirisTarihi).Take(500).ToList();
                _rows.Clear();
                var sira = 1;
                foreach (var islem in list)
                {
                    var tartimlar = islem.Tartimlar.OrderBy(x => x.TartimTarihi).ToList();
                    var ilk = tartimlar.FirstOrDefault();
                    var ikinci = tartimlar.Skip(1).FirstOrDefault();
                    var tahsilatlar = islem.Ucretler.Where(x => x.TahsilEdildiMi).ToList();
                    var tahsilat = tahsilatlar.FirstOrDefault();
                    _rows.Add(new SearchResultRow
                    {
                        SiraNo = sira++,
                        IslemNo = islem.CikisNo ?? islem.IslemNo,
                        Durum = islem.Durum,
                        Plaka = islem.Arac != null ? islem.Arac.Plaka : string.Empty,
                        Firma = islem.Arac != null ? islem.Arac.FirmaAdi : string.Empty,
                        GirisTarihi = islem.GirisTarihi.ToString("dd.MM.yyyy"),
                        GirisSaati = islem.GirisTarihi.ToString("HH:mm:ss"),
                        CikisTarihi = islem.CikisTarihi.HasValue ? islem.CikisTarihi.Value.ToString("dd.MM.yyyy") : string.Empty,
                        CikisSaati = islem.CikisTarihi.HasValue ? islem.CikisTarihi.Value.ToString("HH:mm:ss") : string.Empty,
                        BirinciTartim = ilk != null ? ilk.AgirlikKg.ToString("N0", CultureInfo.GetCultureInfo("tr-TR")) + " kg" : "Tartımsız",
                        IkinciTartim = ikinci != null ? ikinci.AgirlikKg.ToString("N0", CultureInfo.GetCultureInfo("tr-TR")) + " kg" : string.Empty,
                        Net = ilk != null && ikinci != null ? Math.Abs(ilk.AgirlikKg - ikinci.AgirlikKg).ToString("N0", CultureInfo.GetCultureInfo("tr-TR")) + " kg" : string.Empty,
                        TahsilatNo = tahsilat != null ? (tahsilat.TahsilatNo ?? tahsilat.FaturaId) : string.Empty,
                        OdemeTuru = tahsilat != null ? tahsilat.OdemeTuru : string.Empty,
                        KantarFisNo = ilk != null ? ilk.KantarFisNo : string.Empty,
                        Kullanici = islem.CikisKullaniciId.HasValue ? islem.CikisKullaniciId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                        ToplamUcret = islem.ToplamTahsilat.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")) + " TL",
                        Notlar = islem.Notlar
                    });
                }
            }
        }
    }
}
```

- [ ] **Step 5: Wire Araştır button**

In `src/KantarPro.Desktop/MainWindow.xaml`, change the lower `Araştır` button click from `MenuButton_Click` to:

```xml
Click="SearchButton_Click"
```

In `src/KantarPro.Desktop/MainWindow.xaml.cs`, add:

```csharp
private void SearchButton_Click(object sender, RoutedEventArgs e)
{
    var window = new SearchWindow { Owner = this };
    window.ShowDialog();
}
```

- [ ] **Step 6: Build and test**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Debug
& "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\TestPlatform\vstest.console.exe" tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll /Tests:Build_PlakaFirmaTarihVeTahsilatiYazar
```

Expected: build succeeds and formatter test passes.

- [ ] **Step 7: Commit**

```powershell
git add src/KantarPro.Desktop/SearchResultRow.cs src/KantarPro.Desktop/SearchResultsTextFormatter.cs src/KantarPro.Desktop/SearchWindow.xaml src/KantarPro.Desktop/SearchWindow.xaml.cs src/KantarPro.Desktop/MainWindow.xaml src/KantarPro.Desktop/MainWindow.xaml.cs tests/KantarPro.Application.Tests/SearchResultsTextFormatterTests.cs
git commit -m "Gecmis kayitlar icin arastirma ekranini ekle"
```

## Task 4: Daily Revenue XLSX Export

**Files:**
- Modify: `src/KantarPro.Desktop/DailyRevenueExcelExporter.cs`
- Modify: `src/KantarPro.Desktop/MainWindow.DataAndFormatting.cs`
- Test: `tests/KantarPro.Application.Tests/DailyRevenueExcelExporterTests.cs`

- [ ] **Step 1: Replace CSV test with XLSX test**

Replace `tests/KantarPro.Application.Tests/DailyRevenueExcelExporterTests.cs` content with:

```csharp
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using KantarPro.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class DailyRevenueExcelExporterTests
    {
        [TestMethod]
        public void Export_XlsxDosyasiOlustururVeToplamUcretiYazar()
        {
            var path = Path.Combine(Path.GetTempPath(), "kantarpro-gunluk-tahsilat-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                DailyRevenueExcelExporter.Export(path, new[]
                {
                    new DailyRevenueRow
                    {
                        SiraNo = 1,
                        IslemNo = "0017",
                        IslemTipi = "Tartımsız",
                        KantarFisNo = "-",
                        OdemeTuru = "Nakit",
                        FirmaAdi = "FARUK",
                        Plaka = "16DENEME1616",
                        CikisTarihi = "07.06.2026",
                        CikisSaati = "16:35:45",
                        GirisCikisUcreti = "366,00 TL",
                        TartimUcreti = "366,00 TL",
                        BeklemeUcreti = "0,00 TL",
                        ToplamUcret = "732,00 TL"
                    }
                });

                Assert.IsTrue(File.Exists(path));
                using (var archive = ZipFile.OpenRead(path))
                {
                    Assert.IsTrue(archive.Entries.Any(x => x.FullName == "xl/worksheets/sheet1.xml"));
                    var sheet = archive.GetEntry("xl/worksheets/sheet1.xml");
                    using (var reader = new StreamReader(sheet.Open()))
                    {
                        var xml = reader.ReadToEnd();
                        StringAssert.Contains(xml, "16DENEME1616");
                        StringAssert.Contains(xml, "Toplam Ücret");
                        StringAssert.Contains(xml, "732,00 TL");
                    }
                }
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}
```

- [ ] **Step 2: Add assembly reference if ZipFile is unavailable**

If build fails with `ZipFile` missing, add `System.IO.Compression` and `System.IO.Compression.FileSystem` references to test and desktop project files. In SDK-less `.csproj`, add:

```xml
<Reference Include="System.IO.Compression" />
<Reference Include="System.IO.Compression.FileSystem" />
```

- [ ] **Step 3: Replace exporter with minimal XLSX writer**

Replace `src/KantarPro.Desktop/DailyRevenueExcelExporter.cs` with a minimal OpenXML zip writer. The method must create:

```text
[Content_Types].xml
_rels/.rels
xl/workbook.xml
xl/_rels/workbook.xml.rels
xl/worksheets/sheet1.xml
```

Use inline strings in `sheet1.xml` (`t="inlineStr"`) so shared strings are not required. Rows are:

```csharp
var headers = new[]
{
    "Sıra", "İşlem No", "İşlem Tipi", "Kantar Fiş No", "Ödeme Türü", "Firma", "Plaka",
    "Çıkış Tarihi", "Çıkış Saati", "Giriş Ücreti", "Tartım Ücreti", "İşgaliye Ücreti", "Toplam Ücret"
};
```

Each cell XML must escape `&`, `<`, `>`, `"`, and `'`.

- [ ] **Step 4: Update SaveFileDialog**

In `src/KantarPro.Desktop/MainWindow.DataAndFormatting.cs`, inside `RevenueExportExcelButton_Click`, change:

```csharp
Filter = "Excel CSV dosyası (*.csv)|*.csv",
FileName = "GunlukTahsilat_" + DateTime.Today.ToString("yyyyMMdd") + ".csv",
DefaultExt = ".csv"
```

to:

```csharp
Filter = "Excel dosyası (*.xlsx)|*.xlsx",
FileName = "GunlukTahsilat_" + DateTime.Today.ToString("yyyyMMdd") + ".xlsx",
DefaultExt = ".xlsx"
```

- [ ] **Step 5: Run tests**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Debug
& "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\TestPlatform\vstest.console.exe" tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll /Tests:Export_XlsxDosyasiOlustururVeToplamUcretiYazar
```

Expected: pass.

- [ ] **Step 6: Commit**

```powershell
git add src/KantarPro.Desktop/DailyRevenueExcelExporter.cs src/KantarPro.Desktop/MainWindow.DataAndFormatting.cs tests/KantarPro.Application.Tests/DailyRevenueExcelExporterTests.cs
git commit -m "Gunluk tahsilati xlsx olarak aktar"
```

## Task 5: OKI Daily Revenue Preview

**Files:**
- Create: `src/KantarPro.Desktop/TextPreviewWindow.xaml`
- Create: `src/KantarPro.Desktop/TextPreviewWindow.xaml.cs`
- Modify: `src/KantarPro.Desktop/MainWindow.DataAndFormatting.cs`

- [ ] **Step 1: Create TextPreviewWindow XAML**

Create `src/KantarPro.Desktop/TextPreviewWindow.xaml`:

```xml
<Window x:Class="KantarPro.Desktop.TextPreviewWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Döküm Önizleme" Width="900" Height="680" WindowStartupLocation="CenterOwner">
    <DockPanel Margin="12">
        <TextBlock x:Name="TitleTextBlock" DockPanel.Dock="Top" FontSize="20" FontWeight="Bold" Margin="0,0,0,8" />
        <StackPanel DockPanel.Dock="Bottom" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,10,0,0">
            <Button Content="Yazdır" Width="110" Height="34" Margin="0,0,8,0" Click="Print_Click" />
            <Button Content="Kapat" Width="90" Height="34" Click="Close_Click" />
        </StackPanel>
        <TextBox x:Name="PreviewTextBox" FontFamily="Consolas" FontSize="13" IsReadOnly="True"
                 AcceptsReturn="True" AcceptsTab="True" VerticalScrollBarVisibility="Auto"
                 HorizontalScrollBarVisibility="Auto" TextWrapping="NoWrap" />
    </DockPanel>
</Window>
```

- [ ] **Step 2: Create TextPreviewWindow code**

Create `src/KantarPro.Desktop/TextPreviewWindow.xaml.cs`:

```csharp
using System;
using System.Windows;

namespace KantarPro.Desktop
{
    public partial class TextPreviewWindow : Window
    {
        private readonly string _text;

        public TextPreviewWindow(string title, string text)
        {
            InitializeComponent();
            Title = title;
            TitleTextBlock.Text = title;
            _text = text ?? string.Empty;
            PreviewTextBox.Text = _text;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            RawPrinterHelper.PrintTextWithDriver(
                RawPrinterHelper.GetPreferredPrinterName(),
                _text,
                Title + " " + DateTime.Now.ToString("yyyyMMddHHmmss"),
                topMarginLines: 0,
                leftMarginColumns: 0,
                fontSize: 9.0f);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
```

- [ ] **Step 3: Update RevenuePrintOkiButton_Click**

In `src/KantarPro.Desktop/MainWindow.DataAndFormatting.cs`, replace the direct `RawPrinterHelper.PrintTextWithDriver(...)` call in `RevenuePrintOkiButton_Click` with:

```csharp
var preview = new TextPreviewWindow("Günlük Tahsilat OKI Dökümü", rawText)
{
    Owner = this
};
preview.ShowDialog();
```

- [ ] **Step 4: Build**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Debug
```

Expected: build succeeds.

- [ ] **Step 5: Commit**

```powershell
git add src/KantarPro.Desktop/TextPreviewWindow.xaml src/KantarPro.Desktop/TextPreviewWindow.xaml.cs src/KantarPro.Desktop/MainWindow.DataAndFormatting.cs
git commit -m "OKI dokumu icin onizleme penceresi ekle"
```

## Task 6: Lower Buttons and Manual Backup

**Files:**
- Modify: `src/KantarPro.Desktop/MainWindow.xaml`
- Modify: `src/KantarPro.Desktop/MainWindow.xaml.cs`
- Modify: `tools/BackupKantarPro.ps1`

- [ ] **Step 1: Add IgnoreWeekend switch to backup script**

At the top of `tools/BackupKantarPro.ps1`, extend `param(...)` with:

```powershell
[switch]$IgnoreWeekend
```

Change weekend guard to:

```powershell
if (-not $IgnoreWeekend -and ((Get-Date).DayOfWeek -in @('Saturday', 'Sunday'))) {
    Write-Host "Weekend backup skipped."
    exit 0
}
```

- [ ] **Step 2: Update lower buttons**

In `src/KantarPro.Desktop/MainWindow.xaml`, remove lower `Menü` button. Keep:

```xml
<Button Content="Makbuz" MinHeight="34" Padding="16,4" FontSize="13" Margin="0,0,8,0" Click="KantarFisiYazdir_Click" />
<Button Content="Araştır" MinHeight="34" Padding="16,4" FontSize="13" Margin="0,0,8,0" Click="SearchButton_Click" />
<Button Content="Yedekle" MinHeight="34" Padding="16,4" FontSize="13" Click="BackupButton_Click" />
```

- [ ] **Step 3: Add BackupButton_Click**

In `src/KantarPro.Desktop/MainWindow.xaml.cs`, add:

```csharp
private void BackupButton_Click(object sender, RoutedEventArgs e)
{
    try
    {
        var script = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "BackupKantarPro.ps1");
        if (!System.IO.File.Exists(script))
        {
            script = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "tools", "BackupKantarPro.ps1"));
        }

        if (!System.IO.File.Exists(script))
        {
            throw new InvalidOperationException("Yedekleme scripti bulunamadı: " + script);
        }

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\" -IgnoreWeekend",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using (var process = System.Diagnostics.Process.Start(startInfo))
        {
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? output : error);
            }
        }

        MessageBox.Show("Yedekleme tamamlandı.", "Yedekle", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    catch (Exception ex)
    {
        MessageBox.Show("Yedekleme çalıştırılamadı: " + ex.Message, "Yedekle", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
```

- [ ] **Step 4: Build**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Debug
```

Expected: build succeeds.

- [ ] **Step 5: Commit**

```powershell
git add src/KantarPro.Desktop/MainWindow.xaml src/KantarPro.Desktop/MainWindow.xaml.cs tools/BackupKantarPro.ps1
git commit -m "Alt menu ve manuel yedekleme akislarini duzenle"
```

## Task 7: Kullanici Degistir Window

**Files:**
- Create: `src/KantarPro.Desktop/UserSwitchWindow.xaml`
- Create: `src/KantarPro.Desktop/UserSwitchWindow.xaml.cs`
- Modify: `src/KantarPro.Desktop/MainWindow.xaml`
- Modify: `src/KantarPro.Desktop/MainWindow.xaml.cs`

- [ ] **Step 1: Change top button**

In `src/KantarPro.Desktop/MainWindow.xaml`, replace:

```xml
<Button Content="Şifre Değiştir" MinHeight="28" Padding="12,4" FontSize="13" Margin="0,0,4,0" Click="ChangePasswordButton_Click" />
```

with:

```xml
<Button Content="Kullanıcı Değiştir" MinHeight="28" Padding="12,4" FontSize="13" Margin="0,0,4,0" Click="ChangeUserButton_Click" />
```

- [ ] **Step 2: Create UserSwitchWindow**

Create `src/KantarPro.Desktop/UserSwitchWindow.xaml`:

```xml
<Window x:Class="KantarPro.Desktop.UserSwitchWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Kullanıcı Değiştir" Width="420" Height="420" WindowStartupLocation="CenterOwner"
        ResizeMode="NoResize">
    <Grid Margin="22">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>
        <TextBlock Text="Kullanıcı Değiştir" FontSize="22" FontWeight="Bold" />
        <TextBlock Grid.Row="1" Text="Kullanıcıyı seçip şifresini girin." Margin="0,6,0,14" />
        <ComboBox x:Name="UserComboBox" Grid.Row="2" Height="34" DisplayMemberPath="DisplayName" />
        <PasswordBox x:Name="PasswordBox" Grid.Row="3" Height="34" Margin="0,10,0,0" />
        <Border x:Name="InfoBorder" Grid.Row="4" Background="#FFF1F2" BorderBrush="#FDA4AF" BorderThickness="1" Margin="0,10,0,0" Padding="8" Visibility="Collapsed">
            <TextBlock x:Name="InfoTextBlock" TextWrapping="Wrap" />
        </Border>
        <GroupBox Grid.Row="5" Header="Şifre Değiştir" Margin="0,14,0,0">
            <StackPanel Margin="10">
                <PasswordBox x:Name="CurrentPasswordBox" Height="30" Margin="0,0,0,8" />
                <PasswordBox x:Name="NewPasswordBox" Height="30" Margin="0,0,0,8" />
                <PasswordBox x:Name="RepeatPasswordBox" Height="30" />
            </StackPanel>
        </GroupBox>
        <StackPanel Grid.Row="6" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,14,0,0">
            <Button Content="Şifreyi Değiştir" Width="130" Height="34" Margin="0,0,8,0" Click="ChangePassword_Click" />
            <Button Content="Vazgeç" Width="90" Height="34" Margin="0,0,8,0" Click="Cancel_Click" />
            <Button Content="Giriş" Width="90" Height="34" Click="Login_Click" />
        </StackPanel>
    </Grid>
</Window>
```

- [ ] **Step 3: Create UserSwitchWindow code**

Create `src/KantarPro.Desktop/UserSwitchWindow.xaml.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using KantarPro.Application.Services;
using KantarPro.Domain.Entities;
using KantarPro.Infrastructure.Data;

namespace KantarPro.Desktop
{
    public partial class UserSwitchWindow : Window
    {
        private List<UserOption> _users = new List<UserOption>();

        public UserSwitchWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadUsers();
        }

        public KullaniciOturumu AuthenticatedUser { get; private set; }

        private void LoadUsers()
        {
            using (var context = KantarDbContextFactory.Create())
            {
                var servis = new KullaniciServisi(new KantarUnitOfWork(context));
                _users = servis.KullanicilariListele(true).Select(UserOption.From).ToList();
            }
            UserComboBox.ItemsSource = _users;
            UserComboBox.SelectedIndex = _users.Count > 0 ? 0 : -1;
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selected = UserComboBox.SelectedItem as UserOption;
                if (selected == null)
                {
                    throw new InvalidOperationException("Kullanıcı seçin.");
                }
                using (var context = KantarDbContextFactory.Create())
                {
                    var servis = new KullaniciServisi(new KantarUnitOfWork(context));
                    AuthenticatedUser = servis.GirisYap(selected.KullaniciAdi, PasswordBox.Password);
                }
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                PasswordBox.Clear();
                PasswordBox.Focus();
            }
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selected = UserComboBox.SelectedItem as UserOption;
                if (selected == null)
                {
                    throw new InvalidOperationException("Kullanıcı seçin.");
                }
                if (NewPasswordBox.Password != RepeatPasswordBox.Password)
                {
                    throw new InvalidOperationException("Yeni şifre tekrar alanı ile aynı olmalıdır.");
                }
                using (var context = KantarDbContextFactory.Create())
                {
                    var servis = new KullaniciServisi(new KantarUnitOfWork(context));
                    var oturum = servis.GirisYap(selected.KullaniciAdi, CurrentPasswordBox.Password);
                    servis.ParolaDegistir(oturum.KullaniciId, CurrentPasswordBox.Password, NewPasswordBox.Password);
                }
                ShowError("Şifre değiştirildi.");
                CurrentPasswordBox.Clear();
                NewPasswordBox.Clear();
                RepeatPasswordBox.Clear();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowError(string message)
        {
            InfoTextBlock.Text = message;
            InfoBorder.Visibility = Visibility.Visible;
        }

        private sealed class UserOption
        {
            public int KullaniciId { get; set; }
            public string KullaniciAdi { get; set; }
            public string DisplayName { get; set; }

            public static UserOption From(Kullanici kullanici)
            {
                return new UserOption
                {
                    KullaniciId = kullanici.KullaniciId,
                    KullaniciAdi = kullanici.KullaniciAdi,
                    DisplayName = string.Equals(kullanici.KullaniciAdi, "admin", StringComparison.OrdinalIgnoreCase)
                        ? "Admin"
                        : (string.IsNullOrWhiteSpace(kullanici.AdSoyad) ? kullanici.KullaniciAdi : kullanici.AdSoyad)
                };
            }
        }
    }
}
```

- [ ] **Step 4: Wire MainWindow session update**

In `src/KantarPro.Desktop/MainWindow.xaml.cs`, add:

```csharp
private void ChangeUserButton_Click(object sender, RoutedEventArgs e)
{
    var window = new UserSwitchWindow { Owner = this };
    if (window.ShowDialog() == true && window.AuthenticatedUser != null)
    {
        _currentUser = window.AuthenticatedUser;
        ApplyUserPermissions();
        App.LogOperation(_currentUser.KullaniciAdi, "Kullanici degistirildi", "Oturum program kapanmadan degistirildi.");
    }
}
```

If existing `ApplyUserPermissions()` only appends to `StationStatusText`, refactor it to rebuild the text instead of appending repeatedly:

```csharp
private void ApplyUserPermissions()
{
    SettingsNavButton.Visibility = _currentUser != null && _currentUser.AdminMi ? Visibility.Visible : Visibility.Collapsed;
    ApplyAdminOnlyMenuVisibility(_currentUser != null && _currentUser.AdminMi);
    UpdateManualWeightAccess();
    RefreshStationStatusText();
}
```

- [ ] **Step 5: Build**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Debug
```

Expected: build succeeds.

- [ ] **Step 6: Commit**

```powershell
git add src/KantarPro.Desktop/UserSwitchWindow.xaml src/KantarPro.Desktop/UserSwitchWindow.xaml.cs src/KantarPro.Desktop/MainWindow.xaml src/KantarPro.Desktop/MainWindow.xaml.cs
git commit -m "Program kapanmadan kullanici degistirme ekle"
```

## Task 8: Admin Manual Weight Fallback

**Files:**
- Create: `src/KantarPro.Desktop/ScaleWeightSelector.cs`
- Modify: `src/KantarPro.Desktop/MainWindow.xaml.cs`
- Modify: `src/KantarPro.Desktop/MainWindow.xaml`
- Test: `tests/KantarPro.Application.Tests/ScaleWeightSelectorTests.cs`

- [ ] **Step 1: Write tests**

Create `tests/KantarPro.Application.Tests/ScaleWeightSelectorTests.cs`:

```csharp
using System;
using KantarPro.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KantarPro.Application.Tests
{
    [TestClass]
    public class ScaleWeightSelectorTests
    {
        [TestMethod]
        public void GetWeight_MemurIcinIndikatorDegeriniKullanir()
        {
            Assert.AreEqual(25000m, ScaleWeightSelector.GetWeight(25000m, "34000", false));
        }

        [TestMethod]
        public void GetWeight_AdminIcinManuelDegeriKullanir()
        {
            Assert.AreEqual(34000m, ScaleWeightSelector.GetWeight(null, "34000", true));
        }

        [TestMethod]
        public void GetWeight_MemurIcinIndikatorYoksaHataVerir()
        {
            var ex = Assert.ThrowsException<InvalidOperationException>(() => ScaleWeightSelector.GetWeight(null, "34000", false));
            StringAssert.Contains(ex.Message, "İndikatörden geçerli kilo");
        }
    }
}
```

- [ ] **Step 2: Create ScaleWeightSelector**

Create `src/KantarPro.Desktop/ScaleWeightSelector.cs`:

```csharp
using System;
using System.Globalization;

namespace KantarPro.Desktop
{
    public static class ScaleWeightSelector
    {
        public static decimal GetWeight(decimal? indicatorWeightKg, string manualText, bool adminManualAllowed)
        {
            if (adminManualAllowed)
            {
                decimal manual;
                if (decimal.TryParse((manualText ?? string.Empty).Trim(), NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out manual) && manual > 0)
                {
                    return manual;
                }

                if (decimal.TryParse((manualText ?? string.Empty).Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out manual) && manual > 0)
                {
                    return manual;
                }
            }

            if (indicatorWeightKg.HasValue && indicatorWeightKg.Value > 0)
            {
                return indicatorWeightKg.Value;
            }

            throw new InvalidOperationException("İndikatörden geçerli kilo alınmadan tartımlı kayıt yapılamaz.");
        }
    }
}
```

- [ ] **Step 3: Update MainWindow.GetCurrentScaleWeightKg**

In `src/KantarPro.Desktop/MainWindow.xaml.cs`, replace `GetCurrentScaleWeightKg()` body with:

```csharp
private decimal GetCurrentScaleWeightKg()
{
    return ScaleWeightSelector.GetWeight(
        _lastScaleWeightKg,
        AgirlikTextBox.Text,
        _currentUser != null && _currentUser.AdminMi);
}
```

- [ ] **Step 4: Add manual access updater**

In `src/KantarPro.Desktop/MainWindow.xaml.cs`, add:

```csharp
private void UpdateManualWeightAccess()
{
    var admin = _currentUser != null && _currentUser.AdminMi;
    AgirlikTextBox.IsReadOnly = !admin;
    AgirlikTextBox.IsTabStop = admin;
}
```

Call `UpdateManualWeightAccess();` after login and after user switch.

- [ ] **Step 5: Build and tests**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Debug
& "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\TestPlatform\vstest.console.exe" tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll /Tests:GetWeight_MemurIcinIndikatorDegeriniKullanir,GetWeight_AdminIcinManuelDegeriKullanir,GetWeight_MemurIcinIndikatorYoksaHataVerir
```

Expected: pass.

- [ ] **Step 6: Commit**

```powershell
git add src/KantarPro.Desktop/ScaleWeightSelector.cs src/KantarPro.Desktop/MainWindow.xaml.cs src/KantarPro.Desktop/MainWindow.xaml tests/KantarPro.Application.Tests/ScaleWeightSelectorTests.cs
git commit -m "Admin icin manuel kilo girisini sinirli ac"
```

## Task 9: Full Verification and Push

**Files:**
- Verify all changed files.

- [ ] **Step 1: Build full solution**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Restore,Build /p:Configuration=Debug
```

Expected: `Build succeeded.`

- [ ] **Step 2: Run all tests**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\TestPlatform\vstest.console.exe" tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll
```

Expected: all tests pass.

- [ ] **Step 3: Manual smoke test**

Run desktop app from Visual Studio or `bin\Debug\KantarPro.Desktop.exe` and verify:

```text
1. Admin login works.
2. Admin can type kilo manually; memur cannot type kilo.
3. Tartimli kayit immediately has a 5 digit kantar fis no in list/preview.
4. Makbuz button asks for fis printing; cancel does not change fis no.
5. Arastir opens, searches by plaka/firma/giris/cikis dates.
6. Gunluk Tahsilat admin XLSX export creates an .xlsx file.
7. OKI Dokum Yazdir opens preview before printing.
8. Kullanici Degistir switches active user without closing app.
9. Yedekle runs the backup script with -IgnoreWeekend.
```

- [ ] **Step 4: Commit any final fixes**

```powershell
git status --short
git add <changed-files>
git commit -m "Operasyon ekranlari ve rapor akislarini tamamla"
```

- [ ] **Step 5: Push**

```powershell
git push origin codex/saha-ziyareti-model
```

Expected: remote branch updated.

## Self-Review

- Spec coverage:
  - Tartim yapilan her plaka icin fis no: Task 1 and Task 2.
  - Makbuz cancel/reprint no increment: Task 2.
  - Arastirma by plaka/firma/giris/cikis tarihleri: Task 3.
  - Admin XLSX export: Task 4.
  - OKI dokum preview: Task 5.
  - Alt menu: Task 6.
  - Manuel yedekleme: Task 6.
  - Kullanici Degistir and password change: Task 7.
  - Admin manual kilo: Task 8.
- Placeholder scan: No `TBD`, `TODO`, or unspecified test step remains.
- Type consistency:
  - `SearchResultRow`, `SearchResultsTextFormatter`, `TextPreviewWindow`, `UserSwitchWindow`, and `ScaleWeightSelector` are introduced before they are referenced.
  - Existing `KullaniciServisi`, `KantarDbContextFactory`, `RawPrinterHelper`, `DailyRevenueRow`, and `KantarFisPreviewData` names match current code search results.
