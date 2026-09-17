<#
.SYNOPSIS
    Sestaví instalátor a aktualizační balíčky desktopu Cruma (T-53, desktop-pattern.md §6).

.DESCRIPTION
    Publikuje Cruma.Desktop jako samostatnou aplikaci pro win-x64 a zabalí ji nástrojem Velopack (vpk).
    Výstupní složka obsahuje Setup.exe (instalace pro uživatele bez práv administrátora) a soubory feedu
    (RELEASES, *.nupkg, releases.win.json). Obsah složky se nahraje na server do cesty z konfigurace
    Cruma:Desktop:FeedPath; desktop pak hledá aktualizace na https://<server>/desktop/ (I1-D-5).

.EXAMPLE
    ./deploy/desktop/pack.ps1 -Version 0.1.0
#>
param(
    [Parameter(Mandatory)] [string] $Version,
    [string] $OutputDir = "artifacts/desktop-feed",
    [string] $FeedUrl = ""
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path "$PSScriptRoot/../.."
$publishDir = Join-Path $root "artifacts/desktop-publish"

Push-Location $root
try {
    dotnet tool restore
    dotnet publish src/Cruma.Desktop/Cruma.Desktop.csproj -c Release -r win-x64 --self-contained -p:Version=$Version -o $publishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish selhal" }

    # Adresa feedu se zapíše do nastavení publikované aplikace.
    if ($FeedUrl) {
        $settingsPath = Join-Path $publishDir "appsettings.json"
        $settings = Get-Content $settingsPath -Raw | ConvertFrom-Json
        $settings.UpdateFeedUrl = $FeedUrl
        $settings | ConvertTo-Json | Set-Content $settingsPath -Encoding utf8
    }

    dotnet vpk pack --packId Cruma --packVersion $Version --packDir $publishDir --mainExe Cruma.Desktop.exe --packTitle Cruma --outputDir $OutputDir
    if ($LASTEXITCODE -ne 0) { throw "vpk pack selhal" }

    Write-Host "Feed desktopu je ve složce $OutputDir"
}
finally {
    Pop-Location
}
