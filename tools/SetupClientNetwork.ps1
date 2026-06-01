$ErrorActionPreference = "Stop"

param(
    [string]$EthernetAlias = "Ethernet",
    [string]$ClientIp = "192.168.50.2",
    [int]$PrefixLength = 24
)

Write-Host "KantarPro istemci ag ayarlari yapiliyor..." -ForegroundColor Cyan

$adapter = Get-NetAdapter -Name $EthernetAlias -ErrorAction Stop
if ($adapter.Status -ne "Up") {
    throw "Ethernet baglantisi aktif degil. Kabloyu kontrol edin."
}

Get-NetIPAddress -InterfaceAlias $EthernetAlias -AddressFamily IPv4 -ErrorAction SilentlyContinue |
    Where-Object { $_.IPAddress -ne $ClientIp } |
    Remove-NetIPAddress -Confirm:$false -ErrorAction SilentlyContinue

if (-not (Get-NetIPAddress -InterfaceAlias $EthernetAlias -IPAddress $ClientIp -ErrorAction SilentlyContinue)) {
    New-NetIPAddress -InterfaceAlias $EthernetAlias -IPAddress $ClientIp -PrefixLength $PrefixLength | Out-Null
}

Set-DnsClientServerAddress -InterfaceAlias $EthernetAlias -ResetServerAddresses

Write-Host ""
Write-Host "Tamamlandi." -ForegroundColor Green
Write-Host "Bu bilgisayar IP: $ClientIp"
Write-Host "Program Ayarlari SQL Server/IP: tcp:192.168.50.1,1433"
Write-Host ""
Read-Host "Kapatmak icin Enter'a basin"
