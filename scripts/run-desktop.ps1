<#
.SYNOPSIS
    Spustí desktopového klienta Cruma (Debug build, data v %LOCALAPPDATA%\Cruma\dev\).

.DESCRIPTION
    Desktop pracuje offline; první přihlášení a synchronizace ale potřebují běžící server
    (scripts/run-server.ps1 v jiném okně). Adresa serveru je v src/Cruma.Desktop/appsettings.json.

.PARAMETER Reset
    Smaže vývojová data desktopu (lokální databázi, přihlášení, předvolby) a začne od prvního spuštění.

.EXAMPLE
    ./scripts/run-desktop.ps1
    ./scripts/run-desktop.ps1 -Reset
#>
param([switch] $Reset)

# Nativní příkazy (podman, dotnet) píšou i na stderr – chyby se hlídají přes $LASTEXITCODE,
# ne přes ErrorActionPreference Stop (Windows PowerShell 5.1 by z výpisu na stderr udělal chybu).
$ErrorActionPreference = "Continue"
$root = Resolve-Path "$PSScriptRoot/.."
Set-Location $root

$dataFolder = Join-Path $env:LOCALAPPDATA "Cruma\dev"
if ($Reset -and (Test-Path $dataFolder)) {
    Write-Host "==> Mažu vývojová data desktopu v $dataFolder" -ForegroundColor Yellow
    Remove-Item $dataFolder -Recurse -Force
}

try {
    Invoke-WebRequest -Uri "https://localhost:5001/health" -TimeoutSec 5 -UseBasicParsing | Out-Null
    Write-Host "==> Server běží" -ForegroundColor Cyan
}
catch {
    Write-Host "==> Server na https://localhost:5001 neodpovídá – desktop poběží offline." -ForegroundColor Yellow
    Write-Host "    První přihlášení vyžaduje server: spusťte scripts/run-server.ps1 v jiném okně." -ForegroundColor Yellow
}

Write-Host "==> Spouštím desktop (okno 'Cruma (dev)')" -ForegroundColor Cyan
dotnet run --project src/Cruma.Desktop
