param(
    [string]$EthernetAlias = "Ethernet",
    [string]$ServerIp = "192.168.50.1",
    [int]$PrefixLength = 24,
    [string]$SqlInstanceName = "SQLEXPRESS",
    [string]$DatabaseName = "KantarPro",
    [string]$SqlLogin = "kantar_app",
    [string]$SqlPassword = ""
)

$ErrorActionPreference = "Stop"

Write-Host "KantarPro sunucu ag ayarlari yapiliyor..." -ForegroundColor Cyan

function Get-SqlInstanceId {
    param([string]$InstanceName)

    $paths = @(
        "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server\Instance Names\SQL"
    )

    foreach ($path in $paths) {
        if (Test-Path $path) {
            $value = (Get-ItemProperty -Path $path -Name $InstanceName -ErrorAction SilentlyContinue).$InstanceName
            if (-not [string]::IsNullOrWhiteSpace($value)) {
                return $value
            }
        }
    }

    throw "SQL instance bulunamadi: $InstanceName. SQL Server Express instance adini kontrol edin."
}

function Get-SqlTcpRoot {
    param([string]$InstanceId)

    $paths = @(
        "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$InstanceId\MSSQLServer\SuperSocketNetLib\Tcp",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server\$InstanceId\MSSQLServer\SuperSocketNetLib\Tcp"
    )

    foreach ($path in $paths) {
        if (Test-Path $path) {
            return $path
        }
    }

    throw "SQL TCP/IP registry yolu bulunamadi: $InstanceId"
}

if ([string]::IsNullOrWhiteSpace($SqlPassword)) {
    $securePass = Read-Host "SQL kullanicisi icin sifre girin" -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePass)
    try {
        $SqlPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
    }
    finally {
        if ($bstr -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
        }
    }
}

if ([string]::IsNullOrWhiteSpace($SqlPassword)) {
    throw "SQL sifresi bos olamaz."
}

$serverName = $env:COMPUTERNAME
$sqlInstanceId = Get-SqlInstanceId -InstanceName $SqlInstanceName
$sqlTcpRoot = Get-SqlTcpRoot -InstanceId $sqlInstanceId
$sqlServiceName = if ($SqlInstanceName -eq "MSSQLSERVER") { "MSSQLSERVER" } else { "MSSQL`$$SqlInstanceName" }

$adapter = Get-NetAdapter -Name $EthernetAlias -ErrorAction Stop
if ($adapter.Status -ne "Up") {
    throw "Ethernet baglantisi aktif degil. Kabloyu kontrol edin."
}

Get-NetIPAddress -InterfaceAlias $EthernetAlias -AddressFamily IPv4 -ErrorAction SilentlyContinue |
    Where-Object { $_.IPAddress -ne $ServerIp } |
    Remove-NetIPAddress -Confirm:$false -ErrorAction SilentlyContinue

if (-not (Get-NetIPAddress -InterfaceAlias $EthernetAlias -IPAddress $ServerIp -ErrorAction SilentlyContinue)) {
    New-NetIPAddress -InterfaceAlias $EthernetAlias -IPAddress $ServerIp -PrefixLength $PrefixLength | Out-Null
}

Set-DnsClientServerAddress -InterfaceAlias $EthernetAlias -ResetServerAddresses

Set-ItemProperty -Path $sqlTcpRoot -Name Enabled -Value 1
Set-ItemProperty -Path "$sqlTcpRoot\IPAll" -Name TcpDynamicPorts -Value ""
Set-ItemProperty -Path "$sqlTcpRoot\IPAll" -Name TcpPort -Value "1433"

if (-not (Get-NetFirewallRule -DisplayName "KantarPro SQL Server 1433" -ErrorAction SilentlyContinue)) {
    New-NetFirewallRule -DisplayName "KantarPro SQL Server 1433" -Direction Inbound -Protocol TCP -LocalPort 1433 -Action Allow | Out-Null
}

Restart-Service -Name $sqlServiceName -Force

Add-Type -AssemblyName System.Data
$masterConnectionString = "Server=tcp:$serverName,1433;Database=master;Integrated Security=True;Encrypt=False;Connect Timeout=15"
$connection = New-Object System.Data.SqlClient.SqlConnection($masterConnectionString)
$connection.Open()
try {
    $command = $connection.CreateCommand()
    $command.CommandText = @"
EXEC xp_instance_regwrite
    N'HKEY_LOCAL_MACHINE',
    N'Software\Microsoft\MSSQLServer\MSSQLServer',
    N'LoginMode',
    REG_DWORD,
    2;
"@
    $command.ExecuteNonQuery() | Out-Null
}
finally {
    $connection.Close()
}

Restart-Service -Name $sqlServiceName -Force
Start-Sleep -Seconds 3

$appConnectionString = "Server=tcp:$serverName,1433;Database=master;Integrated Security=True;Encrypt=False;Connect Timeout=15"
$connection = New-Object System.Data.SqlClient.SqlConnection($appConnectionString)
$connection.Open()
try {
    $command = $connection.CreateCommand()
    $command.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'$SqlLogin')
BEGIN
    CREATE LOGIN [$SqlLogin] WITH PASSWORD = N'$SqlPassword', CHECK_POLICY = OFF, CHECK_EXPIRATION = OFF;
END;

IF DB_ID(N'$DatabaseName') IS NOT NULL
BEGIN
    USE [$DatabaseName];
    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$SqlLogin')
    BEGIN
        CREATE USER [$SqlLogin] FOR LOGIN [$SqlLogin];
    END;
    IF NOT EXISTS (
        SELECT 1
        FROM sys.database_role_members drm
        INNER JOIN sys.database_principals r ON r.principal_id = drm.role_principal_id
        INNER JOIN sys.database_principals m ON m.principal_id = drm.member_principal_id
        WHERE r.name = N'db_owner' AND m.name = N'$SqlLogin'
    )
    BEGIN
        EXEC sp_addrolemember N'db_owner', N'$SqlLogin';
    END;
END;
"@
    $command.ExecuteNonQuery() | Out-Null
}
finally {
    $connection.Close()
}

Write-Host ""
Write-Host "Tamamlandi." -ForegroundColor Green
Write-Host "Bu bilgisayar IP: $ServerIp"
Write-Host "Ikinci bilgisayarda SQL Server/IP: tcp:$ServerIp,1433"
Write-Host "Ikinci bilgisayarda SQL kullanici: $SqlLogin"
Write-Host "Ikinci bilgisayarda SQL sifre: $SqlPassword"
Write-Host ""
Read-Host "Kapatmak icin Enter'a basin"
