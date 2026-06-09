param(
    [string]$SqlServer = ".\SQLEXPRESS",
    [string]$Database = "KantarPro",
    [string]$BackupDirectory = "C:\KantarProBackups",
    [string]$RunAt = "18:30",
    [switch]$UseSqlLogin,
    [string]$SqlUser = "sa",
    [string]$SqlPassword = ""
)

$ErrorActionPreference = "Stop"

$scriptPath = Join-Path $PSScriptRoot "BackupKantarPro.ps1"
if (-not (Test-Path -LiteralPath $scriptPath)) {
    throw "BackupKantarPro.ps1 bulunamadi: $scriptPath"
}

$argumentParts = @(
    "-NoProfile",
    "-ExecutionPolicy", "Bypass",
    "-File", "`"$scriptPath`"",
    "-SqlServer", "`"$SqlServer`"",
    "-Database", "`"$Database`"",
    "-BackupDirectory", "`"$BackupDirectory`""
)

if ($UseSqlLogin) {
    $argumentParts += @("-UseSqlLogin", "-SqlUser", "`"$SqlUser`"", "-SqlPassword", "`"$SqlPassword`"")
}

$action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument ($argumentParts -join " ")
$trigger = New-ScheduledTaskTrigger -Weekly -DaysOfWeek Monday, Tuesday, Wednesday, Thursday, Friday -At $RunAt
$settings = New-ScheduledTaskSettingsSet -MultipleInstances IgnoreNew -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Hours 2)

Register-ScheduledTask `
    -TaskName "KantarPro Hafta Ici Yedek" `
    -Description "KantarPro SQL veritabanini hafta ici otomatik yedekler." `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -RunLevel Highest `
    -Force | Out-Null

Write-Host "KantarPro yedekleme gorevi kuruldu. Saat: $RunAt, gunler: Pazartesi-Cuma"
