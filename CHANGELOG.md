# Changelog

Všechny podstatné změny v tomto projektu.

Formát vychází z [Keep a Changelog](https://keepachangelog.com/cs/1.1.0/),
verzování podle [SemVer](https://semver.org/lang/cs/).

## [Nevydáno]

### Vývojářské

- **Architektura a plán prvního inkrementu.** Koncept, architektura celého systému (`docs/arch/`), architecture
  profile (`.architecture/architecture-cruma/`) a úkoly pro inkrement I-1 (`docs/arch/i-1/tasks.md`) –
  aby vývoj začínal od schválených rozhodnutí, ne od kódu.
- **Struktura repozitáře podle Susceptoru a Symbolonu.** Dokumenty v kořeni (`AGENTS.md`, `CHANGES.md`,
  `BUGS.md`, `CHANGELOG.md`) a záznamy sprintů v `dev/`.
- **Oddělená data vývojového buildu desktopu (pravidlo PER-006).** Debug build bude mít data
  v `%LOCALAPPDATA%\Cruma\dev\` a v titulku „(dev)“ – aby vývoj a zkoušky nesahaly na skutečné poznámky
  nainstalované verze.
- **Kostra řešení I-1 (etapa E-1).** `Cruma.slnx` s 16 produkčními a 11 testovacími projekty podle šablony
  profilu, centrální verze balíčků, warnings as errors – aby další etapy stavěly na hlídané struktuře.
- **Architektonický test.** Porovnává reference projektů se šablonou profilu, hlídá seznam projektů, balíčky
  sdílené logiky a NLog mimo kompoziční kořeny – porušení architektury shodí build v CI.
- **PostgreSQL pro vývoj a testy.** `deploy/compose.yaml` s profilem `dev` (Podman i Docker) a ověřovací test
  přes Testcontainers nad stejnou verzí image – integrační testy běží proti skutečné databázi.
- **NLog v desktopu, webu a serveru.** Výchozí úroveň Information; desktop do souborů v datové složce, server
  do konzole, web do konzole prohlížeče.
- **CI build na GitHub Actions.** Při každém push a pull requestu build, všechny testy a kontrola compose
  v Dockeru na Linux runneru.
