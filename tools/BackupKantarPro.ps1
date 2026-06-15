param(
    [string]$SqlServer = ".\SQLEXPRESS",
    [string]$Database = "KantarPro",
    [string]$BackupDirectory = "C:\KantarProBackups",
    [switch]$UseSqlLogin,
    [string]$SqlUser = "sa",
    [string]$SqlPassword = "",
    [switch]$IgnoreWeekend
)

$ErrorActionPreference = "Stop"

function Write-BackupLog {
    param([string]$Message)

    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $line = "[$timestamp] $Message"
    Write-Host $line
    Add-Content -Path $script:LogPath -Value $line -Encoding UTF8
}

function Get-SqlCmdPath {
    $cmd = Get-Command sqlcmd.exe -ErrorAction SilentlyContinue
    if ($cmd) {
        return $cmd.Source
    }

    $candidates = @(
        "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\130\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles}\Microsoft SQL Server\110\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles}\Microsoft SQL Server\100\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles(x86)}\Microsoft SQL Server\100\Tools\Binn\sqlcmd.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw "sqlcmd.exe bulunamadi. SQL Server command line tools yuklu olmali."
}

function Invoke-DatabaseCommand {
    param([string]$Query)

    $sqlcmd = Get-SqlCmdPath
    $args = @("-S", $SqlServer, "-b", "-Q", $Query)
    if ($UseSqlLogin) {
        $args += @("-U", $SqlUser, "-P", $SqlPassword)
    }
    else {
        $args += "-E"
    }

    & $sqlcmd @args
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd hata kodu: $LASTEXITCODE"
    }
}

$day = (Get-Date).DayOfWeek
if (-not $IgnoreWeekend -and ($day -eq [DayOfWeek]::Saturday -or $day -eq [DayOfWeek]::Sunday)) {
    Write-Host "Hafta sonu yedek alinmaz."
    exit 0
}

New-Item -ItemType Directory -Force -Path $BackupDirectory | Out-Null
$script:LogPath = Join-Path $BackupDirectory "KantarProBackup.log"

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $BackupDirectory ("{0}_FULL_{1}.bak" -f $Database, $stamp)
Write-BackupLog "Full yedek basladi: $backupFile"
Invoke-DatabaseCommand "BACKUP DATABASE [$Database] TO DISK = N'$backupFile' WITH INIT, NAME = N'$Database full backup', STATS = 10"
Write-BackupLog "Full yedek tamamlandi."

Get-ChildItem -Path $BackupDirectory -Filter "*.bak" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -Skip 2 |
    Remove-Item -Force

Write-BackupLog "Yedekleme islemi basariyla bitti."
