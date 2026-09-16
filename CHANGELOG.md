# Changelog

Všechny podstatné změny v tomto projektu.

Formát vychází z [Keep a Changelog](https://keepachangelog.com/cs/1.1.0/),
verzování podle [SemVer](https://semver.org/lang/cs/).

## [Nevydáno]

### Přidáno

- **Webová aplikace Cruma (PWA).** Poznámky v prohlížeči i v telefonu: rychlé zachycení bez jakéhokoli dialogu,
  editor s nadpisy, tučným písmem, kurzívou, podtržením, přeškrtnutím, seznamy, checklistem, odkazy a zvýrazněním,
  přehled s připnutými poznámkami a filtrem štítků, archiv, koš s obnovením a trvalým smazáním, hledání bez ohledu
  na diakritiku, kategorie a štítky, světlý a tmavý režim.
- **Souběžné úpravy bez ztráty.** Změny téže poznámky z více zařízení se sloučí; když se stejný odstavec změní na
  dvou místech, aplikace ukáže obě verze a nechá vybrat nebo upravit.
- **Desktop pro Windows.** Plnohodnotná práce bez připojení nad lokální databází: poznámky, kategorie, štítky,
  hledání bez ohledu na diakritiku, archiv, koš i řešení konfliktů. Po připojení se změny samy synchronizují se
  serverem a změny z telefonu se stáhnou; stav synchronizace je vidět v hlavičce. Instalace pro uživatele bez práv
  administrátora s aktualizacemi ze serveru Cruma. Vývojový build má vlastní data a v titulku „(dev)“.
- **Poznámka i bez připojení.** Bez sítě jde napsat novou poznámku – počká ve frontě a po obnovení spojení se odešle
  sama; ostatní akce jsou do té doby vypnuté a aplikace ukazuje stav Offline.

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
- **Sdílená logika I-1 (etapa E-2).** Schéma dokumentu s identifikátory bloků, registrem typů, migracemi
  a extrakcí textu; doména poznámek, kategorií a štítků; verze poznámek, tříbodový merge po blocích a vlastností
  s konflikty a jejich vyřešením; normalizace a tokenizace pro vyhledávání; kontrakty synchronizačního
  protokolu – jádro, na kterém stojí server, desktop i web, ověřené 132 unit testy včetně scénářů slučování.
- **Server I-1 (etapa E-3).** ASP.NET Core nad PostgreSQL: jednotná zápisová cesta poznámek s tříbodovým merge,
  verzemi a spojováním uložení, kategorie a štítky, vyhledávání bez ohledu na diakritiku, REST API v1,
  synchronizační protokol pro desktop (handshake, push s deduplikací, pull podle kurzoru), audit bez obsahu
  a append-only, cookie session a vývojové přihlášení – aby web i desktop měly proti čemu běžet. Přihlášení
  Googlem se zapne po zadání OAuth údajů.
- **Sdílené UI, editor a API klient (etapa E-4).** `Cruma.Ui` s rozhraními služeb a schopnostmi shellu,
  `Cruma.Ui.Editor` s TipTap balíkem sestavovaným v buildu (vyžaduje Node.js) a vlastním rozšířením identifikátorů
  bloků, typovaný `Cruma.Api.Client`, bUnit a vitest testy – aby desktop v E-5 použil stejné komponenty.
- **Desktop: lokální úložiště, synchronizace a instalátor (etapa E-5).** SQLite s logem čekajících změn a lokálními
  verzemi, FTS5 index v konformní sadě spolu s PostgreSQL, synchronizační engine s integračními testy proti serveru,
  WPF shell se stejným UI jako web, balíček Velopack a feed na serveru, výkonový scénář s 50 000 poznámkami.
- **Příprava nasazení (etapa E-6).** Image serveru, produkční compose s proxy Caddy (automatické TLS), šifrované
  denní zálohy restic, `deploy.sh` se zálohou, migrací, kontrolou zdraví a návratem, postup obnovy a release workflow
  – vyzkoušeno nanečisto v Podmanu včetně obnovy po ztrátě databáze; produkce čeká na doménu a VPS.
- **Izolace uživatelů a konformní sada vyhledávání.** Každý endpoint i synchronizace ověřeně nevydá data jiného
  uživatele; vyhledávání nad PostgreSQL vrací stejné výsledky jako referenční vyhodnocení.
