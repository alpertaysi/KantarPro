# Veritabani Otomatik Kurulum Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ayarlar > Baglanti Ayarlari ekranindan, mevcut verileri tehlikeye atmadan ve `003_seed_random_demo_data.sql` dosyasini atlayarak KantarPro veritabanini tek dugmeyle kurmak.

**Architecture:** SQL dosyalarini bulma, siralama, `GO` batch'lerine ayirma ve kurulum akisini yonetme sorumlulugu yeni `DatabaseInstallationService` sinifinda tutulacak. SQL Server'a gercek erisim `SqlDatabaseInstallationExecutor` sinifina ayrilacak; servis bu arayuz sayesinde sahte executor ile test edilecek. `MainWindow` yalnizca baglanti formunu okuyacak, admin onayi alacak, servisi calistiracak ve sonucu gosterecek.

**Tech Stack:** C# 7.3, WPF, .NET Framework 4.8, `System.Data.SqlClient`, SQL Server, MSTest, klasik MSBuild proje dosyalari.

---

## File Structure

- Create: `src/KantarPro.Desktop/DatabaseInstallationService.cs`
  - Script katalogu, `003` eleme, `GO` ayirma, mevcut sema korumasi ve kurulum sonucunu yonetir.
  - `IDatabaseInstallationExecutor` ve `DatabaseInstallationResult` tiplerini barindirir.
- Create: `src/KantarPro.Desktop/SqlDatabaseInstallationExecutor.cs`
  - `master` baglantisini acar, mevcut KantarPro tablolarini sorgular ve SQL batch'lerini calistirir.
- Create: `tests/KantarPro.Application.Tests/DatabaseInstallationServiceTests.cs`
  - Dosya sirasi, `003` eleme, batch ayirma, mevcut sema korumasi ve hata halinde durma davranislarini test eder.
- Modify: `src/KantarPro.Desktop/MainWindow.xaml`
  - Baglanti Ayarlari sekmesine admin kurulum dugmesi ekler.
- Modify: `src/KantarPro.Desktop/MainWindow.xaml.cs`
  - Dugme olayini, onay akisini, loglamayi ve ekran yenilemeyi baglar.
- Modify: `src/KantarPro.Desktop/KantarPro.Desktop.csproj`
  - Yeni C# dosyalarini ve `database\*.sql` cikti icerigini projeye ekler.
- Modify: `tests/KantarPro.Application.Tests/KantarPro.Application.Tests.csproj`
  - Yeni test dosyasini derlemeye ekler.
- Modify: `tools/BuildReleasePackage.ps1`
  - Derleme ciktisindaki `database` klasorunu `Program\database` altina kopyalar.

### Task 1: Script katalogu ve GO ayiricisi

**Files:**
- Create: `tests/KantarPro.Application.Tests/DatabaseInstallationServiceTests.cs`
- Create: `src/KantarPro.Desktop/DatabaseInstallationService.cs`
- Modify: `tests/KantarPro.Application.Tests/KantarPro.Application.Tests.csproj`
- Modify: `src/KantarPro.Desktop/KantarPro.Desktop.csproj`

- [ ] **Step 1: Script sirasi ve 003 eleme testini yaz**

`DatabaseInstallationServiceTests.cs` dosyasina gecici klasor olusturan test ekle:

```csharp
[TestMethod]
public void GetInstallationScripts_Siralayip003DemoScriptiniAtlar()
{
    var directory = CreateTempDirectory(
        "010_fix.sql",
        "003_seed_random_demo_data.sql",
        "001_create_schema.sql",
        "000_create_migration_history.sql",
        "not-a-script.txt");

    var scripts = DatabaseInstallationService.GetInstallationScripts(directory)
        .Select(Path.GetFileName)
        .ToArray();

    CollectionAssert.AreEqual(
        new[]
        {
            "000_create_migration_history.sql",
            "001_create_schema.sql",
            "010_fix.sql"
        },
        scripts);
}
```

Test yardimcisinda her dosyayi bos SQL metniyle olustur; `TestCleanup` icinde gecici klasorleri sil.

- [ ] **Step 2: GO batch ayirma testlerini yaz**

```csharp
[TestMethod]
public void SplitSqlBatches_TekBasinaGoSatirlariniAyirir()
{
    var sql = "SELECT 1;\r\nGO\r\nSELECT 'GO metin icinde';\r\ngo -- batch\r\nSELECT 3;";

    var batches = DatabaseInstallationService.SplitSqlBatches(sql).ToArray();

    Assert.AreEqual(3, batches.Length);
    StringAssert.Contains(batches[0], "SELECT 1");
    StringAssert.Contains(batches[1], "'GO metin icinde'");
    StringAssert.Contains(batches[2], "SELECT 3");
}

[TestMethod]
[ExpectedException(typeof(DirectoryNotFoundException))]
public void GetInstallationScripts_KlasorYoksaAnlasilirHataVerir()
{
    DatabaseInstallationService.GetInstallationScripts(
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
}
```

- [ ] **Step 3: Test dosyasini eski tip test projesine ekle**

`KantarPro.Application.Tests.csproj` icindeki `Compile` listesine ekle:

```xml
<Compile Include="DatabaseInstallationServiceTests.cs" />
```

- [ ] **Step 4: Testleri derleyip beklenen kirmizi sonucu dogrula**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Debug /v:minimal
```

Expected: FAIL; `DatabaseInstallationService` bulunamadigi icin test projesi derlenemez.

- [ ] **Step 5: Script katalogu ve batch ayiricisini uygula**

`DatabaseInstallationService.cs` baslangic uygulamasi:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace KantarPro.Desktop
{
    public sealed class DatabaseInstallationService
    {
        private static readonly Regex GoLineRegex = new Regex(
            @"^\s*GO\s*(?:--.*)?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static IList<string> GetInstallationScripts(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException(
                    "Veritabani kurulum scriptleri bulunamadi: " + directory);
            }

            return Directory.GetFiles(directory, "*.sql", SearchOption.TopDirectoryOnly)
                .Where(path =>
                {
                    var name = Path.GetFileName(path);
                    return !name.StartsWith("003", StringComparison.OrdinalIgnoreCase) &&
                           !name.Equals("003_seed_random_demo_data.sql", StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static IList<string> SplitSqlBatches(string sql)
        {
            var batches = new List<string>();
            var current = new StringBuilder();

            using (var reader = new StringReader(sql ?? string.Empty))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (GoLineRegex.IsMatch(line))
                    {
                        AddBatch(batches, current);
                        continue;
                    }

                    current.AppendLine(line);
                }
            }

            AddBatch(batches, current);
            return batches;
        }

        private static void AddBatch(ICollection<string> batches, StringBuilder current)
        {
            var batch = current.ToString().Trim();
            current.Clear();
            if (batch.Length > 0)
            {
                batches.Add(batch);
            }
        }
    }
}
```

`KantarPro.Desktop.csproj` dosyasina ekle:

```xml
<Compile Include="DatabaseInstallationService.cs" />
```

- [ ] **Step 6: Hedef testleri calistir**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Debug /v:minimal
& "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" `
  "tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll" `
  /Tests:DatabaseInstallationServiceTests
```

Expected: PASS.

- [ ] **Step 7: Ilk parcayi commit et**

```powershell
git add src/KantarPro.Desktop/DatabaseInstallationService.cs `
        src/KantarPro.Desktop/KantarPro.Desktop.csproj `
        tests/KantarPro.Application.Tests/DatabaseInstallationServiceTests.cs `
        tests/KantarPro.Application.Tests/KantarPro.Application.Tests.csproj
git commit -m "Veritabani kurulum script katalogunu ekle"
```

### Task 2: Guvenli kurulum orkestrasyonu

**Files:**
- Modify: `tests/KantarPro.Application.Tests/DatabaseInstallationServiceTests.cs`
- Modify: `src/KantarPro.Desktop/DatabaseInstallationService.cs`

- [ ] **Step 1: Sahte SQL executor ve mevcut sema koruma testini yaz**

Test sinifina sahte executor ekle:

```csharp
private sealed class FakeInstallationExecutor : IDatabaseInstallationExecutor
{
    public bool HasExistingSchema { get; set; }
    public string FailWhenBatchContains { get; set; }
    public List<string> ExecutedBatches { get; } = new List<string>();

    public bool HasKantarProCoreSchema(string databaseName)
    {
        return HasExistingSchema;
    }

    public void ExecuteBatch(string batch)
    {
        ExecutedBatches.Add(batch);
        if (!string.IsNullOrWhiteSpace(FailWhenBatchContains) &&
            batch.Contains(FailWhenBatchContains))
        {
            throw new InvalidOperationException("Planli SQL hatasi");
        }
    }
}
```

Koruma testi:

```csharp
[TestMethod]
public void Install_MevcutKantarProSemasiVarsaHicbirScriptCalistirmaz()
{
    var directory = CreateTempDirectory("000_create.sql", "001_schema.sql");
    File.WriteAllText(Path.Combine(directory, "000_create.sql"), "SELECT 1;");
    var executor = new FakeInstallationExecutor { HasExistingSchema = true };

    try
    {
        new DatabaseInstallationService().Install(
            directory, "KantarPro", executor, null);
        Assert.Fail("Mevcut sema icin kurulum reddedilmeliydi.");
    }
    catch (InvalidOperationException ex)
    {
        StringAssert.Contains(ex.Message, "zaten");
    }

    Assert.AreEqual(0, executor.ExecutedBatches.Count);
}
```

- [ ] **Step 2: Hata halinde sonraki scripte gecmeme ve 003 atlama testlerini yaz**

```csharp
[TestMethod]
public void Install_BirScriptHataVerirseSonrakiScriptiCalistirmaz()
{
    var directory = CreateTempDirectory();
    File.WriteAllText(Path.Combine(directory, "000_create.sql"), "SELECT 'ilk';");
    File.WriteAllText(Path.Combine(directory, "001_schema.sql"), "SELECT 'hata';");
    File.WriteAllText(Path.Combine(directory, "002_after.sql"), "SELECT 'sonraki';");
    File.WriteAllText(Path.Combine(directory, "003_seed_random_demo_data.sql"), "SELECT 'demo';");
    var executor = new FakeInstallationExecutor { FailWhenBatchContains = "hata" };

    try
    {
        new DatabaseInstallationService().Install(
            directory, "KantarPro", executor, null);
        Assert.Fail("SQL hatasi disariya aktarilmaliydi.");
    }
    catch (DatabaseInstallationException ex)
    {
        Assert.AreEqual("001_schema.sql", ex.ScriptName);
    }

    Assert.AreEqual(2, executor.ExecutedBatches.Count);
    Assert.IsFalse(executor.ExecutedBatches.Any(x => x.Contains("sonraki")));
    Assert.IsFalse(executor.ExecutedBatches.Any(x => x.Contains("demo")));
}
```

- [ ] **Step 3: Yeni testlerin kirmizi oldugunu dogrula**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Debug /v:minimal
```

Expected: FAIL; `IDatabaseInstallationExecutor`, `Install` ve sonuc/hata tipleri henuz yoktur.

- [ ] **Step 4: Kurulum arayuzu, sonuc ve hata tiplerini uygula**

`DatabaseInstallationService.cs` dosyasina ekle:

```csharp
public interface IDatabaseInstallationExecutor
{
    bool HasKantarProCoreSchema(string databaseName);
    void ExecuteBatch(string batch);
}

public sealed class DatabaseInstallationResult
{
    public IList<string> ExecutedScripts { get; set; }
    public IList<string> SkippedScripts { get; set; }
}

public sealed class DatabaseInstallationException : Exception
{
    public DatabaseInstallationException(string scriptName, Exception innerException)
        : base(scriptName + " calistirilirken hata olustu: " + innerException.Message, innerException)
    {
        ScriptName = scriptName;
    }

    public string ScriptName { get; private set; }
}
```

Servise su kurulum metodunu ekle:

```csharp
public DatabaseInstallationResult Install(
    string scriptDirectory,
    string databaseName,
    IDatabaseInstallationExecutor executor,
    Action<string> progress)
{
    if (!string.Equals(databaseName, "KantarPro", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "Kurulum scriptleri yalnizca KantarPro veritabani adi icin hazirlanmistir.");
    }

    if (executor == null)
    {
        throw new ArgumentNullException(nameof(executor));
    }

    if (executor.HasKantarProCoreSchema(databaseName))
    {
        throw new InvalidOperationException(
            "KantarPro sema tablolari zaten mevcut. Guvenlik nedeniyle kurulum yapilmadi.");
    }

    var scripts = GetInstallationScripts(scriptDirectory);
    var result = new DatabaseInstallationResult
    {
        ExecutedScripts = new List<string>(),
        SkippedScripts = new List<string> { "003_seed_random_demo_data.sql" }
    };

    foreach (var path in scripts)
    {
        var name = Path.GetFileName(path);
        progress?.Invoke(name);
        try
        {
            foreach (var batch in SplitSqlBatches(File.ReadAllText(path, Encoding.UTF8)))
            {
                executor.ExecuteBatch(batch);
            }
            result.ExecutedScripts.Add(name);
        }
        catch (Exception ex)
        {
            throw new DatabaseInstallationException(name, ex);
        }
    }

    return result;
}
```

- [ ] **Step 5: Kurulum servis testlerini calistir**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Debug /v:minimal
& "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" `
  "tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll" `
  /Tests:DatabaseInstallationServiceTests
```

Expected: Build succeeds; all `DatabaseInstallationServiceTests` pass.

- [ ] **Step 6: Orkestrasyonu commit et**

```powershell
git add src/KantarPro.Desktop/DatabaseInstallationService.cs `
        tests/KantarPro.Application.Tests/DatabaseInstallationServiceTests.cs
git commit -m "Guvenli veritabani kurulum akisini ekle"
```

### Task 3: SQL Server executor

**Files:**
- Create: `src/KantarPro.Desktop/SqlDatabaseInstallationExecutor.cs`
- Modify: `src/KantarPro.Desktop/KantarPro.Desktop.csproj`
- Modify: `tests/KantarPro.Application.Tests/DatabaseInstallationServiceTests.cs`

- [ ] **Step 1: Master connection string testini yaz**

Gercek SQL Server gerektirmeyen baglanti metni testi ekle:

```csharp
[TestMethod]
public void BuildMasterConnectionString_HedefVeritabaniYerineMasterKullanir()
{
    var settings = new StationSettings
    {
        SqlServerAddress = @".\SQLEXPRESS",
        DatabaseName = "KantarPro",
        UseWindowsAuthentication = false,
        SqlUsername = "kantar_app",
        SqlPassword = "secret"
    };

    var connectionString =
        SqlDatabaseInstallationExecutor.BuildMasterConnectionString(settings, 15);
    var builder = new SqlConnectionStringBuilder(connectionString);

    Assert.AreEqual("master", builder.InitialCatalog);
    Assert.AreEqual(@".\SQLEXPRESS", builder.DataSource);
    Assert.AreEqual("kantar_app", builder.UserID);
    Assert.AreEqual("secret", builder.Password);
    Assert.AreEqual(15, builder.ConnectTimeout);
}
```

- [ ] **Step 2: Testi derleyip kirmizi sonucu dogrula**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Debug /v:minimal
```

Expected: FAIL; `SqlDatabaseInstallationExecutor` bulunamaz.

- [ ] **Step 3: Gercek SQL executor'u uygula**

`SqlDatabaseInstallationExecutor.cs`:

```csharp
using System;
using System.Data;
using System.Data.SqlClient;

namespace KantarPro.Desktop
{
    public sealed class SqlDatabaseInstallationExecutor :
        IDatabaseInstallationExecutor, IDisposable
    {
        private readonly SqlConnection _connection;

        public SqlDatabaseInstallationExecutor(StationSettings settings)
        {
            _connection = new SqlConnection(BuildMasterConnectionString(settings, 15));
            _connection.Open();
        }

        public static string BuildMasterConnectionString(
            StationSettings settings,
            int connectTimeoutSeconds)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var builder = new SqlConnectionStringBuilder(
                settings.BuildConnectionString(connectTimeoutSeconds))
            {
                InitialCatalog = "master",
                MultipleActiveResultSets = false
            };
            return builder.ConnectionString;
        }

        public bool HasKantarProCoreSchema(string databaseName)
        {
            const string sql = @"
IF DB_ID(@databaseName) IS NULL
    SELECT CAST(0 AS BIT);
ELSE
BEGIN
    DECLARE @statement NVARCHAR(MAX) =
        N'SELECT CAST(CASE WHEN EXISTS (
            SELECT 1 FROM ' + QUOTENAME(@databaseName) + N'.sys.tables
            WHERE name IN (
                N''Araclar'', N''Kullanicilar'', N''Ucretler'', N''Islemler'',
                N''Tartimlar'', N''IslemUcretleri'', N''Loglar'',
                N''BekleyenTartimlar'', N''KantarDosyalari'', N''Ayarlar''
            )
        ) THEN 1 ELSE 0 END AS BIT)';
    EXEC sp_executesql @statement;
END";
            using (var command = new SqlCommand(sql, _connection))
            {
                command.Parameters.Add("@databaseName", SqlDbType.NVarChar, 128)
                    .Value = databaseName;
                return Convert.ToBoolean(command.ExecuteScalar());
            }
        }

        public void ExecuteBatch(string batch)
        {
            using (var command = new SqlCommand(batch, _connection))
            {
                command.CommandTimeout = 120;
                command.ExecuteNonQuery();
            }
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
```

Projeye ekle:

```xml
<Compile Include="SqlDatabaseInstallationExecutor.cs" />
```

- [ ] **Step 4: Hedef testleri calistir**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Debug /v:minimal
& "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" `
  "tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll" `
  /Tests:DatabaseInstallationServiceTests
```

Expected: PASS without requiring a live SQL Server.

- [ ] **Step 5: SQL executor'u commit et**

```powershell
git add src/KantarPro.Desktop/SqlDatabaseInstallationExecutor.cs `
        src/KantarPro.Desktop/KantarPro.Desktop.csproj `
        tests/KantarPro.Application.Tests/DatabaseInstallationServiceTests.cs
git commit -m "SQL veritabani kurulum executorunu ekle"
```

### Task 4: Ayarlar ekranina Veritabanini Kur dugmesi

**Files:**
- Modify: `src/KantarPro.Desktop/MainWindow.xaml`
- Modify: `src/KantarPro.Desktop/MainWindow.xaml.cs`

- [ ] **Step 1: Baglanti Ayarlari arayuzune dugmeyi ekle**

Alt eylem satirini su sekilde genislet:

```xml
<StackPanel Orientation="Horizontal" Margin="0,18,0,0">
    <Button Content="Bağlantıyı Kaydet"
            Style="{StaticResource PrimaryButton}"
            Width="150"
            MinHeight="36"
            Padding="12,5"
            FontSize="13"
            Click="SettingsSaveConnectionButton_Click" />
    <Button Content="Yenile"
            Width="100"
            MinHeight="36"
            Padding="12,5"
            FontSize="13"
            Margin="8,0,0,0"
            Click="SettingsRefreshButton_Click" />
    <Button x:Name="InstallDatabaseButton"
            Content="Veritabanını Kur"
            Width="160"
            MinHeight="36"
            Padding="12,5"
            FontSize="13"
            Margin="8,0,0,0"
            Click="InstallDatabaseButton_Click" />
</StackPanel>
```

Bu sekme zaten yalnizca adminin erisebildigi Ayarlar sayfasindadir. Ek olarak click metodunda admin kontrolu yinelenecek.

- [ ] **Step 2: Script klasoru bulucusunu ekle**

`MainWindow.xaml.cs` icinde UI olayinin yanina su yardimciyi ekle:

```csharp
private static string FindDatabaseScriptDirectory()
{
    var current = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
    while (current != null)
    {
        var candidate = Path.Combine(current.FullName, "database");
        if (Directory.Exists(candidate))
        {
            return candidate;
        }
        current = current.Parent;
    }

    throw new DirectoryNotFoundException(
        "database klasoru bulunamadi. Program paketinin eksiksiz kopyalandigini kontrol edin.");
}
```

- [ ] **Step 3: Kurulum click olayini ekle**

```csharp
private async void InstallDatabaseButton_Click(object sender, RoutedEventArgs e)
{
    if (_currentUser == null || !_currentUser.AdminMi)
    {
        MessageBox.Show(
            "Veritabanı kurma yetkisi yalnızca admin kullanıcısına aittir.",
            "Veritabanı Kurulumu",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
    }

    try
    {
        var settings = ReadStationSettingsFromForm();
        if (!string.Equals(settings.DatabaseName, "KantarPro", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Otomatik kurulum için veritabanı adı KantarPro olmalıdır.");
        }

        var confirmation = MessageBox.Show(
            "SQL Server: " + settings.SqlServerAddress + "\n" +
            "Veritabanı: " + settings.DatabaseName + "\n\n" +
            "Yeni KantarPro veritabanı kurulacaktır. Mevcut KantarPro tabloları bulunursa hiçbir işlem yapılmayacaktır. Devam edilsin mi?",
            "Veritabanını Kur",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        InstallDatabaseButton.IsEnabled = false;
        SettingsConnectionInfoText.Text = "Veritabanı kurulumu hazırlanıyor...";
        var scriptDirectory = FindDatabaseScriptDirectory();
        App.LogOperation(
            _currentUser.KullaniciAdi,
            "Veritabani kurulumu baslatildi",
            settings.SqlServerAddress + " / " + settings.DatabaseName);

        var result = await Task.Run(() =>
        {
            using (var executor = new SqlDatabaseInstallationExecutor(settings))
            {
                return new DatabaseInstallationService().Install(
                    scriptDirectory,
                    settings.DatabaseName,
                    executor,
                    scriptName => App.LogInfo("Veritabani scripti calistiriliyor: " + scriptName));
            }
        });

        StationSettingsStore.Save(settings);
        EnsureDatabaseSchema();
        LoadFeeSettings();
        LoadUsers();
        LoadDashboardData();
        UpdateStationStatus();
        SettingsConnectionInfoText.Text =
            "Kurulum tamamlandı. Çalıştırılan script: " + result.ExecutedScripts.Count +
            ". 003 demo verisi atlandı.";
        App.LogOperation(
            _currentUser.KullaniciAdi,
            "Veritabani kurulumu tamamlandi",
            "Calistirilan script: " + result.ExecutedScripts.Count + "; 003 atlandi.");
        MessageBox.Show(
            SettingsConnectionInfoText.Text,
            "Veritabanı Kurulumu",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
    catch (Exception ex)
    {
        App.LogError("Veritabani kurulumu", ex);
        SettingsConnectionInfoText.Text = "Kurulum başarısız: " + ex.Message;
        MessageBox.Show(
            ex.Message,
            "Veritabanı kurulamadı",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
    finally
    {
        InstallDatabaseButton.IsEnabled = true;
    }
}
```

Gerekli `using` bildirimleri yoksa ekle:

```csharp
using System.IO;
using System.Threading.Tasks;
```

- [ ] **Step 4: Uygulamayi derle**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Debug /v:minimal
```

Expected: Build succeeds.

- [ ] **Step 5: UI baglantisini commit et**

```powershell
git add src/KantarPro.Desktop/MainWindow.xaml `
        src/KantarPro.Desktop/MainWindow.xaml.cs
git commit -m "Ayarlar ekranina veritabani kurulumunu bagla"
```

### Task 5: SQL scriptlerini calisan programa ve kurulum paketine dahil et

**Files:**
- Modify: `src/KantarPro.Desktop/KantarPro.Desktop.csproj`
- Modify: `tools/BuildReleasePackage.ps1`
- Modify: `tests/KantarPro.Application.Tests/DatabaseInstallationServiceTests.cs`

- [ ] **Step 1: Cikti klasorunde scriptlerin bulunmasini isteyen proje testi ekle**

Test dosyasina repo proje tanimini kontrol eden test ekle:

```csharp
[TestMethod]
public void DesktopProject_DatabaseScriptleriniCiktiyaKopyalar()
{
    var root = FindRepositoryRoot();
    var projectText = File.ReadAllText(
        Path.Combine(root, "src", "KantarPro.Desktop", "KantarPro.Desktop.csproj"));

    StringAssert.Contains(projectText, @"database\%(Filename)%(Extension)");
    StringAssert.Contains(projectText, "CopyToOutputDirectory");
}
```

Test sinifina Task 1'deki yaklasimla `FindRepositoryRoot` yardimcisini ekle.

- [ ] **Step 2: Testi calistirip kirmizi sonucu dogrula**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Debug /v:minimal
& "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" `
  "tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll" `
  /Tests:DesktopProject_DatabaseScriptleriniCiktiyaKopyalar
```

Expected: FAIL; Desktop proje dosyasi henuz database icerigini kopyalamaz.

- [ ] **Step 3: SQL dosyalarini Desktop cikti klasorune ekle**

`KantarPro.Desktop.csproj` icindeki Content grubuna ekle:

```xml
<Content Include="..\..\database\*.sql">
  <Link>database\%(Filename)%(Extension)</Link>
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

- [ ] **Step 4: Release paketinin Program klasorune database alt klasorunu kopyala**

`BuildReleasePackage.ps1` icinde program DLL/EXE kopyalamasindan sonra ekle:

```powershell
Copy-IfExists `
    -Path (Join-Path $desktopOutput "database") `
    -Destination (Join-Path $programPath "database")
```

Mevcut `Veritabani` klasorune kaynak scriptlerin kopyalanmasi korunacak; bu klasor manuel inceleme icin kalir. Programin otomatik kurucusu `Program\database` klasorunu kullanir.

- [ ] **Step 5: Testi ve Debug ciktiyi dogrula**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" KantarPro.sln /t:Build /p:Configuration=Debug /v:minimal
& "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" `
  "tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll" `
  /Tests:DesktopProject_DatabaseScriptleriniCiktiyaKopyalar
Get-ChildItem "src\KantarPro.Desktop\bin\Debug\database\*.sql" |
    Sort-Object Name |
    Select-Object -ExpandProperty Name
```

Expected: Test passes; output lists `000` through `010`, including `003` as a packaged file. `003` paketlenir ancak kurulum servisi tarafindan calistirilmaz.

- [ ] **Step 6: Paketleme degisikliklerini commit et**

```powershell
git add src/KantarPro.Desktop/KantarPro.Desktop.csproj `
        tools/BuildReleasePackage.ps1 `
        tests/KantarPro.Application.Tests/DatabaseInstallationServiceTests.cs
git commit -m "Veritabani scriptlerini program paketine ekle"
```

### Task 6: Tam dogrulama ve kontrollu manuel prova

**Files:**
- Verify only; production database kullanma.

- [ ] **Step 1: Tum cozumu temiz derle**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" `
  KantarPro.sln /t:Restore,Rebuild /p:Configuration=Debug /v:minimal
```

Expected: Build succeeds. Mevcut obsolete test uyarilari kabul edilebilir; yeni hata olmamalidir.

- [ ] **Step 2: Tum otomatik testleri calistir**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" `
  "tests\KantarPro.Application.Tests\bin\Debug\KantarPro.Application.Tests.dll"
```

Expected: Mevcut 112 test ve yeni kurulum testlerinin tamami PASS.

- [ ] **Step 3: Izole prova veritabani icin ayri SQL instance veya gecici sunucu hazirla**

Canli `KantarPro` veritabaninda deneme yapma. Bos bir prova SQL Server instance'inda:

1. Programi admin ile ac.
2. Ayarlar > Baglanti Ayarlari'na git.
3. SQL Server bilgisini prova instance'ina ayarla.
4. Veritabani adini `KantarPro` olarak birak.
5. `Veritabanini Kur` dugmesine bas.
6. Onay ekranindaki sunucu ve veritabani bilgisini kontrol edip onayla.

Expected: Basari mesaji calistirilan 10 scripti ve `003` dosyasinin atlandigini bildirir.

- [ ] **Step 4: 003 scriptinin calismadigini SQL ile dogrula**

Prova SQL Server'da:

```sql
USE KantarPro;
SELECT COUNT(*) AS AracSayisi FROM dbo.Araclar;
SELECT COUNT(*) AS IslemSayisi FROM dbo.Islemler;
SELECT Version FROM dbo.__SchemaVersions ORDER BY Version;
```

Expected:

- `AracSayisi = 0`
- `IslemSayisi = 0`
- Sema surumleri bulunur.
- Ilk admin kurulumu `006` nedeniyle kullanici tablosunda bulunur.

- [ ] **Step 5: Mevcut semaya ikinci kurulumun reddedildigini dogrula**

Ayni prova instance'inda `Veritabanini Kur` dugmesine tekrar bas.

Expected: Program mevcut KantarPro tablolarini bildirir, hicbir script calistirmaz ve mevcut veriler degismez.

- [ ] **Step 6: Release paketini olustur ve database klasorunu kontrol et**

Run:

```powershell
& ".\tools\BuildReleasePackage.ps1" -Configuration Release -PackageName "KantarPro_VeritabaniKurulum_Prova"
Get-ChildItem ".\dist\KantarPro_VeritabaniKurulum_Prova\Program\database\*.sql" |
    Sort-Object Name |
    Select-Object -ExpandProperty Name
```

Expected: Release package succeeds and `Program\database` contains all SQL files.

- [ ] **Step 7: Son durumu kontrol et ve commit et**

```powershell
git status --short
git diff --check
git add src tests tools
git commit -m "Veritabani otomatik kurulumunu tamamla"
```

Bu son commit yalnizca onceki adimlarda commitlenmemis dogrulama veya kucuk duzeltmeler varsa olusturulmalidir. Kullaniciya ait ilgisiz calisma agaci degisiklikleri commit kapsaminda olmamalidir.
