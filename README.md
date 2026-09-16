# Cruma

Osobní local-first aplikace pro poznámky, technický obsah a jednoduchý Kanban. Desktop (Windows) je plnohodnotný
offline klient, web a mobil jsou tenké klienty; data se synchronizují přes vlastní server.

Záměry: [CONCEPT.md](CONCEPT.md) · historie změn: [CHANGELOG.md](CHANGELOG.md) ·
chyby: [BUGS.md](BUGS.md) · připomínky k zapracování: [CHANGES.md](CHANGES.md) ·
pokyny pro agenty: [AGENTS.md](AGENTS.md) · sprinty: [dev/SPRINTY.md](dev/SPRINTY.md).

## Stav

Architektura je navržená, hotová je kostra řešení (etapa E-1). Staví se inkrement **I-1 (walking skeleton)**: poznámky se
základním formátováním na desktopu i v PWA, server, přihlášení Googlem, synchronizace se slučováním
a konflikty, vyhledávání, audit, zálohy a nasazení na Hetzner Cloud.

## Architektura

| Vrstva | Projekty | Co dělá |
|---|---|---|
| Sdílená logika | `Cruma.Notes`, `Cruma.Content`, `Cruma.Versioning`, `Cruma.Search`, `Cruma.Sync`, `Cruma.Blobs` | Doména, schéma dokumentu s ID bloků, verze a tříbodový merge, normalizace pro vyhledávání, synchronizační protokol – bez závislosti na platformě |
| UI | `Cruma.Ui`, `Cruma.Ui.Editor` | Sdílené Razor komponenty pro všechny shelly; editor TipTap jako JS ostrov |
| Desktop | `Cruma.Desktop`, `Cruma.Desktop.Storage`, `Cruma.Sync.Client` | WPF + BlazorWebView, lokální SQLite, offline práce a synchronizace |
| Tenký klient | `Cruma.Web` | Blazor WebAssembly PWA pro web, tablet a mobil; fronta nových poznámek offline |
| Server | `Cruma.Server` | ASP.NET Core, PostgreSQL, moduly Notes, Sync, Search, Identity, Audit |
| Provoz | `deploy/` | Kontejnery (Podman i Docker), proxy s TLS, zálohy a obnova |

Podrobně: [docs/arch/plan.md](docs/arch/plan.md), rozhodnutí [docs/arch/DECISIONS.md](docs/arch/DECISIONS.md),
závazná pravidla [.architecture/architecture-cruma/PROFILE.md](.architecture/architecture-cruma/PROFILE.md).

## Kde co je

| Cesta | Obsah |
|---|---|
| `CONCEPT.md` | původní koncept produktu |
| `docs/arch/` | architektonický běh celého systému: frame, analysis, spec, plan, DECISIONS |
| `docs/arch/i-1/` | inkrement I-1: spec, plan, **tasks.md** (63 úkolů v etapách E-1..E-6) |
| `docs/concept/` | podklady ke konceptu |
| `.architecture/architecture-cruma/` | architecture profile – pravidla, patterny, policy |
| `.aidevkit/` | pointer na profil pro AI-DevKit |
| `src/`, `tests/` | produkční a testovací projekty (`Cruma.slnx`) |
| `deploy/` | `compose.yaml` (profil `dev` = PostgreSQL pro vývoj) |
| `.github/workflows/` | CI build a testy |
| `dev/` | záznamy sprintů |
