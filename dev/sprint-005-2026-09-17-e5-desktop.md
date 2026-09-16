# Sprint 005 – E-5 Desktop

| | |
|---|---|
| **Začátek** | 2026-09-17 00:18 |
| **Konec** | 2026-09-17 01:15 |
| **Délka** | 57 min (čistá, bez pauz: 57 min; z toho ~25 min běh výkonového scénáře) |
| **Stav** | hotovo bez skutečného přihlášení desktopu (T-52 čeká na T-19, T-20) a bez ruční zkoušky instalace |
| **Verze** | – (nevydáno) |
| **Rozsah** | tasks.md T-47..T-55 (etapa E-5) · základ `df8c1a2` |
| **Tokeny** | ~129 000 (součet úseků, odhad) |
| **Změny kódu** | 58 souborů, +3 970 / −11 řádků (vč. migrací a záznamu sprintu) |

## Zadání

> (pokračování autonomní práce – „bez dál co můžeš“)

## Plán

- [x] T-47 – `Cruma.Desktop.Storage`: SQLite pro uživatele (poznámky, kategorie, štítky, lokální verze, log čekajících
  změn, kurzor), migrace při startu po záloze souboru, bez tajných údajů, PER-006
- [x] T-48 – adaptér FTS5 v konformní sadě spolu s PostgreSQL
- [x] T-49 – datové služby desktopu nad lokálním úložištěm, lokální editační relace (5 min)
- [x] T-50 – `Cruma.Sync.Client`: relace handshake → push → pull → commit, spouštěče, stav, minimální verze
- [x] T-51 – WPF shell s `Cruma.Ui`, platformní služby Windows, schopnosti desktopu
- [ ] T-52 – úložiště tokenu DPAPI hotové; přihlášení systémovým prohlížečem čeká na T-20 (vývojový most jen Debug)
- [x] T-53 – instalátor a aktualizace (Velopack), feed na serveru; ruční zkouška instalace → `dev/OTEVRENE.md` 6
- [x] T-54 – integrační testy synchronizace proti testovacímu serveru
- [x] T-55 – generátor 50 000 poznámek a výkonový scénář (výsledky k posouzení autorem níže)
- [x] build a všechny testy lokálně, ověření desktopu proti lokálnímu serveru, CHANGELOG, commit

## Průběh

| Čas | Co | Tokeny (zbývá / úsek) |
|---|---|---|
| 00:18 | začátek – analýza E-5, desktop pattern, verze EF Core SQLite / ProtectedData / Velopack | 14 596 000 / 16 000 |
| 00:22 | T-47, T-48 – lokální úložiště SQLite, migrace se zálohou, FTS5 adaptér; konformní sada SQLite + PostgreSQL 53/53 napoprvé | 14 563 000 / 33 000 |
| 00:24 | testy lokálního úložiště 16/16 | 14 553 000 / 10 000 |
| 00:28 | T-50, T-54 – synchronizační engine a přenos HTTP; integrační testy proti serveru 9/9 (jedno chybné očekávání verze – server správně spojil uložení téže instance, N-3) | 14 534 000 / 19 000 |
| 00:31 | UI: akce povolené offline při schopnosti WorksOffline, ukazatel synchronizace, aktualizace v Nastavení; bUnit 16/16 | 14 523 000 / 11 000 |
| 00:32 | T-49, T-51, T-52 – WPF shell: runtime relace, datové služby nad úložištěm, platformní služby, DPAPI, Velopack Main | 14 507 000 / 16 000 |
| 00:35 | desktop naživo přes CDP (WebView2 s ladicím portem): přihlášení, první synchronizace, zápis → server, offline zápis → po obnovení serveru odeslán, šířka editoru, vyřešení konfliktu → „Synchronizováno“ | 14 498 000 / 9 000 |
| 00:36 | T-53 – feed na serveru pod `/desktop/`, `deploy/desktop/pack.ps1`: Setup 80 MB, balíček a feed za 18 s | 14 495 000 / 3 000 |
| 00:59 | T-55 – první běh výkonového scénáře: generování 50 000 poznámek 203 s, ale první synchronizace se zpomalovala (stránka 1 s → 6 s) – přerušeno | 14 478 000 / 17 000 |
| 01:03 | příčiny: mazání z FTS5 podle neindexovaného sloupce (průchod celou tabulkou) a načítání poznámek po jedné při pull; opraveno mapou rowid (migrace `SearchIndexRows`) a dávkovým načtením; pull stránky v jedné transakci | 14 470 000 / 8 000 |
| 01:12 | výkonový scénář znovu: první synchronizace 24 s (viz Výsledek) | 14 468 000 / 2 000 |
| 01:14 | celé řešení `dotnet test Cruma.slnx` 324/324 (64 s) | 14 467 000 / 1 000 |
| 01:15 | konec – dokumentace, CHANGELOG, commit | 14 466 000 / 1 000 |

## Rozhodnutí během sprintu

- **Obsah čekající změny se sestaví až při odeslání** z aktuálního lokálního stavu; neodeslaná změna téže entity se
  jen aktualizuje (offline práce = jedna změna, FR-6 akc. 4). Odeslaná, ale nepotvrzená změna se nemění a další
  úprava vytvoří novou – opakované odeslání se stejným identifikátorem pak server rozpozná (SYN-003) a nová změna už
  vychází z potvrzené verze.
- **Základ změny = poslední verze známá ze serveru** (`ServerVersion`), aktualizuje se potvrzením i stažením.
- **Stažená změna nepřepíše entitu s čekající lokální změnou** – lokální práce se nejdřív odešle a server ji sloučí;
  sloučený stav přijde dalším pull (ERR-002, NFR-4).
- **Zamítnutá změna zůstává čekat s kódem chyby** a jde dál upravovat; smazání, které server nezná (`not_found`),
  se bere jako potvrzené.
- **Lokální mazání kategorie/štítku** upraví poznámky jen lokálně, bez vlastních čekajících změn – totéž udělá server
  při zpracování smazání a výsledek přijde pullem.
- **FTS5 tokenizer `ascii`** – dělí jen na ASCII oddělovačích, ne-ASCII znaky bere jako součást slova; s tokeny
  z Cruma.Search dává stejné výsledky jako PostgreSQL `simple` (konformní sada 53/53).
- **Mapa `search_index_rows` (entita → rowid FTS5)** – přepis a mazání řádku indexu bez průchodu celou tabulkou;
  oba příkazy INSERT v jednom volání kvůli `last_insert_rowid()`.
- **Stav synchronizace**: zamítnuté změny → chyba, konflikt → konflikt, čekající změny → čeká, jinak synchronizováno;
  offline je samostatný příznak (ukazatel „Offline“ v hlavičce).
- **Sdílené UI**: akce na existujících poznámkách jsou vypnuté jen tehdy, když shell není offline-schopný
  (`WorksOffline`) – desktop je má povolené i bez sítě (FR-26 akc. 1). Nové rozhraní `ISyncStatusView`
  a `IAppUpdates` (tenký klient je implementuje prázdně).
- **Vývojový most přihlášení desktopu** (jen Debug): `POST /auth/dev/sign-in`, session cookie uložená DPAPI
  v `session.token` (SEC-003, PER-005); Release build ho vypíná. Skutečné PKCE přihlášení → `dev/OTEVRENE.md` 5.
- **Vlastní `Main` kvůli Velopacku** (`App.xaml` jako stránka), feed desktopu vystavený serverem jako statické
  soubory z `Cruma:Desktop:FeedPath` pod `/desktop/` (I1-D-5).
- **Identifikátor instance desktopu** je stálý pro instalaci (`client-instance.id`), server podle něj spojuje uložení.
- **Výkonový scénář jako `[Explicit]` test** v `Cruma.Sync.Client.Tests` (kategorie Performance) – spouští se ručně.
- **Testovací server bez technických logů** – výpis NLog do konzole zkresloval měření (24 MB logu).

## Výsledek

- **Commity:** „E-5: desktop – lokální úložiště, synchronizace, WPF shell, instalátor, výkonový scénář“
- **Vydání:** žádné (instalátor 0.1.0 sestaven lokálně do `artifacts/desktop-feed`, necommituje se)
- **Testy:** celé řešení 324/324 za 64 s – Server 85, konformní sada 53 (SQLite 26 + PostgreSQL 27),
  Desktop.Storage 16, Sync.Client 9, Ui 16, Architecture 13, Content 32, Notes 22, Versioning 41, Search 22, Sync 15;
  editor vitest 22/22
- **Ověření desktopu naživo** proti lokálnímu serveru: první spuštění vyžaduje přihlášení (FR-26 akc. 3), první
  synchronizace stáhla data (FR-27 akc. 2), zápis na desktopu je na serveru (FR-27 akc. 1), bez serveru jde
  zapisovat, ukazuje se Offline a po obnovení se změna odešle sama (FR-26 akc. 1, FR-27 akc. 1), režimy šířky
  editoru (FR-8 akc. 1), konflikt vyřešený na desktopu se synchronizuje (FR-28 akc. 3), titulek „Cruma (dev)“ a data
  v `%LOCALAPPDATA%\Cruma\dev\` (PER-006), token nečitelný bez DPAPI (SEC-003)
- **Výkonový scénář N-7 (50 000 poznámek, jeden uživatel) – k posouzení autorem (S-I1-5):**

  | Měření | Čas |
  |---|---|
  | generování 50 000 poznámek přes zápisovou cestu serveru | 203 s |
  | server hledání `certif` (33 317 shod) | 362 ms |
  | server hledání `zlutoucky kun` (24 341) / `rijen` (33 280) / `databaze zalohovani obnova` (18 790) | 95 / 65 / 79 ms |
  | server hledání bez shody | 20 ms |
  | **první synchronizace desktopu (50 000 poznámek)** | **24,2 s** |
  | desktop hledání `certif` / `zlutoucky kun` / `rijen` / `databaze zalohovani obnova` | 168 / 132 / 148 / 130 ms |
  | desktop přehled první stránky | 492 ms |

  Počty výsledků jsou na serveru i desktopu shodné (FR-24 akc. 4).
- **Diagnostika:** tokeny ~129 000 (součet úseků), změny 58 souborů +3 970/−11
- **Stav bodů:** –

## Nestihlo se / otevřené

- **T-52 přihlášení systémovým prohlížečem** – čeká na T-19/T-20 (`dev/OTEVRENE.md` 2 a 5).
- **Ruční zkouška instalace a aktualizace** – `dev/OTEVRENE.md` 6.
- **Přehled poznámek na desktopu** načítá stav do paměti kvůli filtru štítků v JSON sloupci (492 ms při 50 000);
  pro výrazně víc poznámek by chtělo samostatnou tabulku štítků.
- **Přeindexování při změně verze normalizátoru (SRC-002)** stále chybí na obou stranách (viz sprint 003).

## Poznámky

- **Výkonová chyba nalezená až scénářem:** filtr `DELETE … WHERE entity_id = ?` na FTS5 prochází celou tabulku –
  první synchronizace rostla kvadraticky (odhad > 10 min). Konformní sada funkčně prošla, výkon neměří.
- WebView2 jde ladit přes `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS=--remote-debugging-port=9222` a CDP z Node.js
  (skript `cdp.mjs` ve scratchpadu) – spolehlivý způsob, jak ovládat UI desktopu bez klikání do okna.
- `System.Security.Cryptography.ProtectedData` je součástí Windows Desktop frameworku – explicitní balíček hlásí NU1510.
- SQLite s `Pooling=False` a EF Core: každý příkaz může jít přes jiné připojení – `last_insert_rowid()` musí být ve
  stejném příkazu.
- Běžící vývojový server zamyká `apphost.exe` – před buildem testů ho zastavit.
