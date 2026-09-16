# Sprint 003 – E-3 Server

| | |
|---|---|
| **Začátek** | 2026-09-16 23:16 |
| **Konec** | 2026-09-16 23:49 |
| **Délka** | 33 min (čistá, bez pauz: 33 min) |
| **Stav** | hotovo bez přihlášení Googlem a desktopových tokenů (čeká na T-19, T-20) |
| **Verze** | – (nevydáno) |
| **Rozsah** | tasks.md T-21..T-31 (etapa E-3) bez T-19 (autor) a T-20 · základ `f626c84` |
| **Tokeny** | ~191 000 (součet úseků, odhad) |
| **Změny kódu** | 65 souborů, +6 235 / −20 řádků (vč. migrace a záznamu sprintu) |

## Zadání

> rozpor zkus vyresit ale tak aby to zapadalo do celkove architektury nebo pristupu, poku nejsi na 99% jist ze
> to je dobre reseni zapsi a prreskoc a bez dal co muzes, toto je muj projekt a vse se da spravit

(E-3 bez čekání na T-19; Google a desktopové tokeny se doplní po T-19.)

## Plán

- [x] T-21 – infrastruktura: `CrumaDbContext` (PostgreSQL, snake_case), globální filtr vlastníka s pojmenovaným
  obejitím, migrace (neaplikují se při startu), ProblemDetails s kódy, anonymní allowlist, correlation id
- [x] T-22 – identita: uživatelé a navázané identity, založení uživatele s výchozí kategorií v jedné transakci,
  cookie session, odhlášení; Google napojený podmíněně (po T-19); vývojové přihlášení jen v Development
- [x] T-23 – audit: append-only schéma `audit` (i triggerem), dotaz podle uživatele, času a typu, přihlašovací události
- [x] T-24 – modul Notes: jednotná zápisová cesta (vlastnictví, merge, verze, audit, index, feed)
- [x] T-25 – spojování uložení do verzí (interval 5 min, konfigurace)
- [x] T-26 – změnový feed uživatele s monotónním pořadím
- [x] T-27 – REST API v1 poznámek, kategorií, štítků, konfliktů a vyhledávání; kontrakty v `Cruma.Api.Contracts`
- [x] T-28 – modul Search s adaptérem PostgreSQL (tsvector `simple` z tokenů, omezení na uživatele v adaptéru)
- [x] T-29 – konformní sada vyhledávání: abstraktní základ + PostgreSQL
- [x] T-30 – synchronizační endpoint: handshake s minimální verzí, push s deduplikací, pull podle kurzoru, audit
- [x] T-31 – testy izolace uživatelů pro všechny endpointy i synchronizaci
- [x] build a všechny testy lokálně, CHANGELOG, commit
- [x] T-20 a přihlášení Googlem → `dev/OTEVRENE.md` (čeká na T-19)

## Průběh

| Čas | Co | Tokeny (zbývá / úsek) |
|---|---|---|
| 23:16 | začátek – analýza E-3, server pattern, error, audit, security policy, I-1 DECISIONS; vysvětlení T-19 autorovi | 14 972 000 / 28 000 |
| 23:30 | moduly Infrastructure, Audit, Search, Sync (feed), Notes, Identity, REST API, migrace `Initial` s triggerem append-only; server se sestaví | 14 866 000 / 106 000 |
| 23:33 | první běh integračních testů: `SqlQuery` s `INSERT … RETURNING` nejde skládat; slovník atributů auditu do jsonb potřebuje převodník; konflikt verzí EF Core → transitive pinning | 14 830 000 / 36 000 |
| 23:37 | testy REST poznámek, infrastruktury, identity a auditu 37/37 | 14 819 000 / 11 000 |
| 23:41 | spojování verzí, vyhledávání, synchronizace, feed, izolace uživatelů – server 77/77 | 14 802 000 / 17 000 |
| 23:43 | konformní sada vyhledávání nad PostgreSQL 27/27 (dvě chybná očekávání v testu, adaptér odpovídal referenci) | 14 792 000 / 10 000 |
| 23:46 | živý běh serveru nad vývojovou databází (migrace `dotnet ef database update`, vývojové přihlášení, poznámka, hledání, odhlášení); neplatné UTF-8 v těle vracelo 500 → opraveno na 400 + test | 14 782 000 / 10 000 |
| 23:48 | celé řešení `dotnet test Cruma.slnx` 248/248 (42 s) | 14 781 000 / 1 000 |
| 23:49 | konec – otevřené body, dokumentace, CHANGELOG, commit | 14 780 000 / 1 000 |

## Rozhodnutí během sprintu

- **E-3 bez T-19** (autor): vše, co nepotřebuje Google, je hotové; přihlášení Googlem je zapojené podmíněně –
  aktivuje se samo, když jsou v user-secrets `Cruma:Identity:Google:ClientId` a `ClientSecret`.
- **Vývojové přihlášení** `POST /auth/dev/sign-in` (agent) – jen v prostředí Development a se zapnutou volbou
  `Cruma:Identity:DevelopmentSignIn`; jde přes stejné založení uživatele jako Google (poskytovatel `development`).
  Umožní E-4 proti lokálnímu serveru i bez Googlu. Mimo Development vrací `not_found` (test).
- **Globální filtr vlastníka** jako pojmenovaný filtr EF Core 10 `Owner`; obejití jen přes
  `CrumaDbContext.AcrossAllUsers<T>(reason)` s povinným zdůvodněním. `SaveChanges` odmítne zápis entity jiného
  uživatele i zápis bez uživatele (PER-002). Založení nového uživatele jedná jeho jménem přes `CurrentUser.ActAs`.
- **Očekávaná selhání jako `ServiceResult`**, ne výjimky (error-handling P-2); doménová pravidla se vrací jako
  `validation_failed` s rozšířením `rule` (např. `default_category_cannot_be_deleted`) – kódy z §2 zůstávají stabilní.
- **Zámek řádku poznámky** (`SELECT … FOR UPDATE`) na začátku každého zápisu – souběžné zápisy téže poznámky jdou
  za sebou, verze se nepřekrývají.
- **Monotónní pořadí feedu** přes `INSERT … ON CONFLICT … RETURNING` na počítadle uživatele se zámkem řádku do konce
  transakce; test 10 paralelních zápisů dává pořadí 1..11 bez mezer.
- **Mazání kategorie a štítku jde přes zápisovou cestu poznámek** – dotčené poznámky dostanou novou verzi, aby
  aktuální stav poznámky vždy odpovídal poslední verzi (základ merge).
- **Vyřešení konfliktu se provede nad verzí, kterou klient viděl, a sloučí se jako úprava** – mezitím vzniklé
  změny jiných bloků zůstanou (I1-D-1); vyřešení se nikdy nespojuje s předchozí verzí.
- **Spojování verzí (N-3)** jen pro běžné uložení, jen se stejnou instancí klienta (`X-Cruma-Client-Instance`),
  stejným zdrojem, bez konfliktu a v intervalu nečinnosti; změna stavu a vyřešení konfliktu vždy novou verzí.
- **Synchronizace**: výsledek zpracované změny se ukládá (`sync.processed_changes`) a opakování vrátí původní
  výsledek; **zamítnutá změna se neukládá**, aby ji klient po nápravě (např. doplnění kategorie) mohl poslat znovu.
  Verze klienta se kontroluje i u push/pull z hlaviček `X-Cruma-App-Version`, `X-Cruma-Protocol-Version`.
- **Pull vrací aktuální stav entity** a z více záznamů téže entity v jedné stránce jen poslední.
- **Úprava ze synchronizace, která mění stav**, se audituje jako `note.archived` / `note.trashed` / `note.restored`.
- **Audit append-only i v databázi** – trigger v migraci odmítne UPDATE, DELETE i TRUNCATE (test).
- **`Cruma.Api.Contracts` nese dokument jako `JsonElement`** (čitelný JSON pro web), protokol synchronizace jako
  řetězec (`DocumentJson`) – kontrakty synchronizace z E-2 zůstaly beze změny.
- **Jedno volání služby na endpoint (API-002)** – PUT poznámky nemění stav (`State: null` = ponechat), vyhledávání
  vrací poznámky přes `INotesService.SearchNotesAsync`.
- **Vývojová databáze**: připojení v user-secrets `Cruma:Database:ConnectionString`, migrace
  `dotnet ef database update --project src/Cruma.Server`; `dotnet-ef` je lokální nástroj (`dotnet-tools.json`).

## Výsledek

- **Commity:** „E-3: server – infrastruktura, identita, audit, poznámky, vyhledávání, synchronizace“
- **Vydání:** žádné
- **Testy:** celé řešení 248/248 za 42 s – Server 78 (infrastruktura 11, identita 6, audit 3, poznámky 16,
  spojování verzí 6, vyhledávání 7, synchronizace 10, izolace 17, kontejner 2), konformní sada 27, Content 32,
  Notes 22, Versioning 41, Search 22, Sync 15, Architecture 11
- **Živý běh:** server nad vývojovou databází v Podmanu – migrace, vývojové přihlášení s cookie `__Host-cruma`
  (HttpOnly, Secure, SameSite=Lax), vytvoření poznámky, hledání `certif`, odhlášení → 401
- **Diagnostika:** tokeny ~191 000 (součet úseků), změny 65 souborů +6 235/−20
- **Stav bodů:** –

## Nestihlo se / otevřené

- **Přihlášení Googlem neověřeno** (výstup E-3) – čeká na T-19; kód je zapojený podmíněně. Viz `dev/OTEVRENE.md` 1.
- **T-20 a tokeny pro desktop** (authorization code + PKCE, OpenIddict) – čeká na T-19; synchronizační endpointy
  zatím přijímají cookie session. Viz `dev/OTEVRENE.md` 2.
- **Přeindexování při změně verze normalizátoru (SRC-002)** – verze se ukládá u každého řádku indexu, úloha, která
  zastaralé řádky přeindexuje, zatím není (žádný úkol E-3 ji nežádá; vznikne s první změnou normalizátoru nebo v E-5).
- **Omezení četnosti na autentizačních endpointech a audit `security.rate_limited` / `security.authorization_denied`**
  (security-policy §4, FR-35 akc. 1) – úkoly E-3 ho nejmenují; zapsáno do `dev/OTEVRENE.md` 3.

## Poznámky

- `SqlQuery` s `INSERT … RETURNING` nejde skládat (`SingleAsync`) – materializovat přes `ToListAsync`.
- Npgsql zapíše `Dictionary` do jsonb jen s převodníkem hodnot nebo `EnableDynamicJson`.
- Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 táhne EF Core 10.0.4, Design 10.0.12 → `CentralPackageTransitivePinningEnabled`.
- curl z Git Bash v tomto prostředí posílá tělo s ne-ASCII znaky poškozené; pro ruční zkoušky API použít Python
  (skript `smoke.py` ve scratchpadu).
- Podman machine po restartu počítače musí běžet (`podman machine start`), jinak integrační testy selžou na Testcontainers.
