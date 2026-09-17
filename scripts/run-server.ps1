<#
.SYNOPSIS
    Spustí vývojový server Cruma (API + webový klient) na https://localhost:5001.

.DESCRIPTION
    1. nastartuje Podman machine, pokud neběží
    2. spustí vývojovou databázi PostgreSQL (compose profil dev) a počká, až je připravená
    3. poprvé vytvoří deploy/.env s náhodným heslem a uloží připojení do dotnet user-secrets serveru
    4. obnoví lokální nástroje a aplikuje migrace databáze
    5. spustí server (ukončení Ctrl+C; databáze běží dál, zastaví ji scripts/stop-dev.ps1)

.EXAMPLE
    ./scripts/run-server.ps1
#>
# Nativní příkazy (podman, dotnet) píšou i na stderr – chyby se hlídají přes $LASTEXITCODE,
# ne přes ErrorActionPreference Stop (Windows PowerShell 5.1 by z výpisu na stderr udělal chybu).
$ErrorActionPreference = "Continue"
$root = Resolve-Path "$PSScriptRoot/.."
Set-Location $root

function Step($text) { Write-Host "==> $text" -ForegroundColor Cyan }

# 1. Podman
Step "Podman machine"
$machine = podman machine list --format "{{.Name}} {{.Running}}" 2>$null | Select-Object -First 1
if (-not $machine) { throw "Podman machine neexistuje – vytvořte ji: podman machine init" }
if ($machine -notmatch "true") {
    podman machine start
    if ($LASTEXITCODE -ne 0) { throw "Podman machine se nespustila" }
}

# 2. Heslo databáze v deploy/.env (necommituje se)
$envFile = Join-Path $root "deploy/.env"
if (-not (Test-Path $envFile) -or -not (Select-String -Path $envFile -Pattern "^CRUMA_DEV_DB_PASSWORD=.+" -Quiet)) {
    Step "Vytvářím deploy/.env s náhodným heslem databáze"
    $password = [Guid]::NewGuid().ToString("N") + [Guid]::NewGuid().ToString("N")
    # Bez BOM, aby compose přečetl název proměnné na prvním řádku.
    [IO.File]::AppendAllText($envFile, "CRUMA_DEV_DB_PASSWORD=$password`n", (New-Object Text.UTF8Encoding $false))
}
$password = ((Select-String -Path $envFile -Pattern "^CRUMA_DEV_DB_PASSWORD=(.+)$").Matches[0].Groups[1].Value).Trim()

Step "Vývojová databáze"
podman compose -f deploy/compose.yaml --profile dev up -d 2>&1 | Where-Object { $_ -notmatch "Executing external compose provider" } | Out-Host
if ($LASTEXITCODE -ne 0) { throw "Databáze se nespustila" }
for ($i = 0; $i -lt 60; $i++) {
    $health = podman inspect --format "{{.State.Health.Status}}" cruma-db-dev-1 2>$null
    if ($health -eq "healthy") { break }
    Start-Sleep -Seconds 1
}
if ($health -ne "healthy") { throw "Databáze není připravená (stav: $health)" }

# 3. Připojení v user-secrets
$connection = "Host=localhost;Port=5432;Database=cruma;Username=cruma;Password=$password"
$secrets = dotnet user-secrets list --project src/Cruma.Server 2>$null
if (-not ($secrets -match [Regex]::Escape("Cruma:Database:ConnectionString = $connection"))) {
    Step "Ukládám připojení k databázi do user-secrets"
    dotnet user-secrets set "Cruma:Database:ConnectionString" $connection --project src/Cruma.Server | Out-Null
}

# 4. Nástroje a migrace
Step "Nástroje a migrace"
dotnet tool restore | Out-Null
if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore selhal" }
dotnet ef database update --project src/Cruma.Server --connection $connection
if ($LASTEXITCODE -ne 0) { throw "Migrace selhaly" }

# 5. Server
Step "Server běží na https://localhost:5001 (vývojové přihlášení: jméno libovolné, např. autor). Ukončení Ctrl+C."
dotnet run --project src/Cruma.Server --launch-profile Cruma.Server
