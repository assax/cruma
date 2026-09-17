<#
.SYNOPSIS
    Zastaví vývojovou databázi Cruma (data zůstávají ve svazku cruma_db-dev-data).

.EXAMPLE
    ./scripts/stop-dev.ps1
#>
# Nativní příkazy (podman, dotnet) píšou i na stderr – chyby se hlídají přes $LASTEXITCODE,
# ne přes ErrorActionPreference Stop (Windows PowerShell 5.1 by z výpisu na stderr udělal chybu).
$ErrorActionPreference = "Continue"
Set-Location (Resolve-Path "$PSScriptRoot/..")
podman compose -f deploy/compose.yaml --profile dev stop 2>&1 | Where-Object { $_ -notmatch "Executing external compose provider" }
Write-Host "==> Vývojová databáze zastavená" -ForegroundColor Cyan
