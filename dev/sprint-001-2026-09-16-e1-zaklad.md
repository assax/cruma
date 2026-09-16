# Sprint 001 – E-1 Základ

| | |
|---|---|
| **Začátek** | 2026-09-16 22:16 |
| **Konec** | 2026-09-16 22:32 |
| **Délka** | 16 min (čistá, bez pauz: 16 min) |
| **Stav** | hotovo (ověřeno lokálně; CI se ověří při pushi) |
| **Verze** | – (nevydáno) |
| **Rozsah** | tasks.md T-2..T-7 (etapa E-1) · před sprintem CHANGES 1 (PER-006, commit `4ba9411`) |
| **Tokeny** | ~74 000 (součet úseků, odhad; bez CHANGES 1) |
| **Změny kódu** | 68 souborů, +1 236 / −3 řádků (bez záznamu sprintu) |

## Zadání

> Pak spusť sprint na etapu E-1 (úkoly T-2 až T-7: solution a props, kostry projektů,
> architektonický test referencí, compose s PostgreSQL a ověření Testcontainers nad Podmanem,
> NLog ve třech kompozičních kořenech, CI build na GitHub Actions).
> Hotovo je, když projde build, architektonický test a test proti PostgreSQL v kontejneru,
> lokálně i v CI. Nepushuj bez mého pokynu.

## Plán

- [x] T-2 – `Cruma.slnx`, `Directory.Build.props` (nullable, implicit usings, warnings as errors),
  `Directory.Packages.props` (centrální verze), `global.json`, složky `src/`, `tests/`, `deploy/`
- [x] T-3 – kostry projektů I-1 podle `solution-structure-template.md` §2, §4, §5 a reference podle §3;
  Cruma.Web hostovaný ze Cruma.Server
- [x] T-4 – architektonický test: reference projektů v `src/` proti §3 šablony (čteno přímo ze šablony)
- [x] T-5 – `deploy/compose.yaml` s profilem `dev` (jen PostgreSQL), ověřovací test Testcontainers nad Podmanem
- [x] T-6 – NLog v Cruma.Desktop, Cruma.Web, Cruma.Server; výchozí úroveň Information; cíle podle
  `logging-and-audit-policy.md` §4 (desktop logy respektují PER-006)
- [x] T-7 – `.github/workflows/build.yml` – build a všechny testy na Linux runneru (běh v CI neověřen – bez pushe)
- [x] ověření lokálně: build, architektonický test, test PostgreSQL; CHANGELOG, commit

## Průběh

| Čas | Co | Tokeny (zbývá / úsek) |
|---|---|---|
| 22:16 | začátek – analýza AGENTS, profilu, tasks E-1, plan N-5/N-8 hotová | 14 892 000 / 108 000 (vč. CHANGES 1) |
| 22:19 | první build: NU1507 – uživatelská konfigurace NuGet má druhý zdroj (M+); přidán `nuget.config` jen s nuget.org | 14 866 000 / 26 000 |
| 22:21 | T-2, T-3 – solution, props, 27 projektů (16 src, 11 tests), `dotnet build` zelený (8 s) | 14 857 000 / 9 000 |
| 22:23 | T-4 – architektonický test 11/11; ověřeno, že přidaná nepovolená reference test shodí s názvy obou projektů | 14 846 000 / 11 000 |
| 22:24 | T-5 – Testcontainers PostgreSQL 18.6 nad Podmanem 2/2 (první běh 40 s se stažením image, dál 7 s); compose `--profile dev` v Podmanu healthy | 14 840 000 / 6 000 |
| 22:25 | T-6 – desktop padal při startu (WebView2 potřebuje TFM s Windows SDK) → `net10.0-windows10.0.19041.0`; server v Production z build výstupu nehostuje statické soubory webu | 14 834 000 / 6 000 |
| 22:28 | T-6 ověřeno za běhu: server (Development i `dotnet publish`) hostuje Cruma.Web; web loguje do konzole prohlížeče; desktop loguje do `%LOCALAPPDATA%\Cruma\dev\logs`, titulek „Cruma (dev)“ | 14 829 000 / 5 000 |
| 22:30 | T-7 – workflow; Release `dotnet test` lokálně 13/13 (21 s); simulace Linux runneru v kontejneru `dotnet/sdk:10.0`: build 0 varování (40 s), architektonický test 11/11 | 14 825 000 / 4 000 |
| 22:32 | konec – dokumentace, CHANGELOG, commit | 14 818 000 / 7 000 |

## Rozhodnutí během sprintu

- **Nový projekt `Cruma.Architecture.Tests`** (agent). Úkol T-4 má scope `tests/`, ale šablona §4 pro
  architektonický test projekt neměla. Podle STR-001 přidán řádek do `solution-structure-template.md` §4 ve
  stejné změně. Test čte povolené reference přímo z šablony, takže profil zůstává jediným zdrojem pravdy.
- **Architektonický test hlídá víc než reference** (agent): i seznam projektů (STR-001), balíčky sdílené logiky
  (DEP-002) a NLog jen v kompozičních kořenech (LOG-003) – levné a přímo z pravidel.
- **`nuget.config` s jediným zdrojem nuget.org** (agent) – centrální správa balíčků s dvěma zdroji hlásí NU1507;
  zároveň je build stejný lokálně i v CI.
- **Warnings as errors pro všechny projekty včetně testů** (agent) – STR-004 žádá produkční; přísnější nevadí.
- **Compose: služba `db-dev` jen v profilu `dev`**, port jen na `127.0.0.1`, heslo z `deploy/.env`
  (SEC-006). Produkční `db` bez publikovaného portu přibude v E-6 jako samostatná služba.
- **Image PostgreSQL `postgres:18.6-alpine`** (agent); test ho čte z `compose.yaml`, takže testy a vývoj mají
  vždy stejnou verzi (OPS-004).
- **Desktop TFM `net10.0-windows10.0.19041.0`** (agent) – WebView2 ve WPF bez něj padá na chybějícím
  `Microsoft.Windows.SDK.NET`.
- **Datová složka desktopu podle PER-006 už teď** (agent) – logy z T-6 potřebují složku; `CrumaAppData` je
  jediné místo, kde se řeší, T-47 na něj naváže.
- **Server hostuje web přes `UseBlazorFrameworkFiles` + `UseStaticFiles`** – `MapStaticAssets` s
  `UseBlazorFrameworkFiles` vracel 500 na `_framework/*`.

## Výsledek

- **Commity:** `4ba9411` Profil: oddělená data vývojového buildu desktopu (PER-006) · commit sprintu
  „E-1: kostra řešení, architektonický test, PostgreSQL přes Testcontainers, NLog, CI“
- **Vydání:** žádné
- **Testy:** lokálně Release 13/13 (architektonický 11, PostgreSQL přes Testcontainers 2); Linux kontejner
  (simulace CI) build 0 varování, architektonický 11/11
- **Diagnostika:** tokeny ~74 000 (součet úseků), změny 68 souborů +1 236/−3
- **Stav bodů:** CHANGES 1 → hotovo (nevydáno)

## Nestihlo se / otevřené

- **Běh v CI neověřen** – workflow vyžaduje push, ten je jen na pokyn autora. Autor 2026-09-16 rozhodl, že etapa
  se ověřuje lokálně a CI se zkontroluje při pushi (pravidlo v `AGENTS.md`, sekce Sprinty).
- Compose v Dockeru ověří až CI (lokálně Docker není); v Podmanu ověřeno.

## Poznámky

- Bash nástroj agenta slučuje `\\` na `\` i v heredocu s uvozovkami – soubory s obrácenými lomítky psát přes
  Python skript v souboru nebo Write.
- Podman machine (`podman-machine-default`, rootful, WSL) vystavuje Docker API, Testcontainers včetně Ryuk
  fungují bez `DOCKER_HOST`.
- `podman compose` používá jako provider `docker-compose.exe` v5.3.1.
- Server z build výstupu v prostředí Production statické soubory webu nemá (static web assets jen v Development);
  publikovaný server je má ve `wwwroot`.
