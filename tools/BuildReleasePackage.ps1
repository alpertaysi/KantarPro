param(
    [string]$Configuration = "Release",
    [string]$PackageRoot = "dist",
    [string]$PackageName = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$solutionPath = Join-Path $repoRoot "KantarPro.sln"
$desktopOutput = Join-Path $repoRoot "src\KantarPro.Desktop\bin\$Configuration"

function Find-MSBuild {
    $candidates = @(
        "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\17\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    $fromPath = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($fromPath) {
        return $fromPath.Source
    }

    throw "MSBuild bulunamadi. Bu paket gelistirme bilgisayarinda Visual Studio/MSBuild ile uretilmelidir."
}

function Copy-IfExists {
    param(
        [string]$Path,
        [string]$Destination
    )

    if (Test-Path $Path) {
        Copy-Item -Path $Path -Destination $Destination -Recurse -Force
    }
}

if ([string]::IsNullOrWhiteSpace($PackageName)) {
    $PackageName = "KantarPro_Kurulum_Paketi_" + (Get-Date -Format "yyyyMMdd_HHmm")
}

$distRoot = Join-Path $repoRoot $PackageRoot
$packagePath = Join-Path $distRoot $PackageName
$programPath = Join-Path $packagePath "Program"
$kurulumPath = Join-Path $packagePath "Kurulum"
$databasePath = Join-Path $packagePath "Veritabani"
$docsPath = Join-Path $packagePath "Dokumanlar"

Write-Host "KantarPro paket derlemesi basliyor..." -ForegroundColor Cyan
Write-Host "Configuration: $Configuration"

$msbuild = Find-MSBuild
& $msbuild $solutionPath /p:Configuration=$Configuration /v:minimal

if (-not (Test-Path (Join-Path $desktopOutput "KantarPro.Desktop.exe"))) {
    throw "Derleme cikti dosyasi bulunamadi: $desktopOutput"
}

if (Test-Path $packagePath) {
    Remove-Item -LiteralPath $packagePath -Recurse -Force
}

New-Item -ItemType Directory -Path $programPath, $kurulumPath, $databasePath, $docsPath | Out-Null

Get-ChildItem -Path $desktopOutput -File |
    Where-Object { $_.Extension -in ".exe", ".dll", ".config" } |
    Copy-Item -Destination $programPath -Force

Copy-Item -Path (Join-Path $repoRoot "tools\SetupServerNetwork.ps1") -Destination (Join-Path $kurulumPath "Server_Ag_SQL_Ayarla.ps1") -Force
Copy-Item -Path (Join-Path $repoRoot "tools\SetupClientNetwork.ps1") -Destination (Join-Path $kurulumPath "Client_Ag_Ayarla.ps1") -Force

Copy-Item -Path (Join-Path $repoRoot "database\*.sql") -Destination $databasePath -Force
Copy-IfExists -Path (Join-Path $repoRoot "docs\KURULUM_AG_SQL.md") -Destination $docsPath
Copy-IfExists -Path (Join-Path $repoRoot "docs\GERCEK_KANTAR_KURULUM_REHBERI.md") -Destination $docsPath
Copy-IfExists -Path (Join-Path $repoRoot "docs\PROJE_DEVAM_REHBERI.md") -Destination $docsPath

$commit = "bilinmiyor"
try {
    $commit = (& git -C $repoRoot rev-parse --short HEAD).Trim()
}
catch {
    $commit = "git-okunamadi"
}

@"
KantarPro Kurulum Paketi
========================

Paket tarihi : $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Git commit   : $commit
Derleme tipi : $Configuration

Klasorler
---------
Program    : Gercek bilgisayara kopyalanacak calisir program dosyalari.
Kurulum    : Server/client ag ve SQL hazirlik scriptleri.
Veritabani : Yeni kurulum veya kontrollu sema guncellemesi icin SQL dosyalari.
Dokumanlar : Kurulum ve proje devam rehberleri.

Onemli
------
- Program.exe tek basina kopyalanmaz; Program klasoru komple kopyalanir.
- SQL/IP/COM/yazici ayarlari gercek bilgisayarda Ayarlar ekranindan yapilir.
- Windows 7 client bilgisayarda .NET Framework 4.8 kurulu olmalidir.
- Server bilgisayarda SQL Server 2008 Express varsa Server_Ag_SQL_Ayarla.ps1 SQL instance adini SQLEXPRESS olarak kullanabilir; farkli instance adi varsa -SqlInstanceName parametresi verilmelidir.
"@ | Set-Content -Path (Join-Path $packagePath "PAKET_BILGISI.txt") -Encoding UTF8

$zipPath = Join-Path $distRoot ($PackageName + ".zip")
if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $packagePath "*") -DestinationPath $zipPath -Force

Write-Host ""
Write-Host "Paket hazirlandi:" -ForegroundColor Green
Write-Host $packagePath
Write-Host "ZIP:" -ForegroundColor Green
Write-Host $zipPath
