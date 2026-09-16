# analysis.md — Cruma, inkrement I-1

```text
Phase: ANALYSIS          Run mode: CONCEPTUAL          Architecture basis: profile @ .architecture\architecture-cruma\PROFILE.md
Output language: czech
Open items: O-1, O-2, O-3, O-4, O-5, O-6          Carried assumptions: A-1, A-2
Last approved gate: FRAME->ANALYSIS @ 2026-09-16
```

Vstupy: `i-1/frame.md`, `docs/arch/spec.md`, `docs/arch/plan.md`, profil Cruma — `architecture-rules.md`,
`solution-structure-template.md`, patterny `content-document`, `versioning-and-sync`, `search`, `server`, `ui`,
`desktop`, `deployment`, policy `security`, `error-handling`, `logging-and-audit`, strategie `testing-strategy`.

---

## 1. Současný stav

Kód neexistuje. Existuje schválená systémová architektura a závazný profil. Všechny lokace jsou nové; scope
hinty v TASKS budou cesty ze `solution-structure-template.md` ve tvaru `new — C-n`.

**Oprava FRAME.** `frame.md` §2.2 zařadil do I-1 komponentu C-7 (bloby). `plan.md` §5 ji v I-1 nemá a v I-1
žádný dokument na blob neodkazuje — obrázky jsou I-2. Pravidla BLB-001 až BLB-004 a SYN-005 se proto v I-1
neuplatní. Rozsah se tím zužuje ve shodě se schváleným plánem; navrhuji opravit text `frame.md` v rámci
schválení této fáze.

## 2. Práce, kterou vytvářejí pravidla profilu

`plan.md` říká, *co* I-1 dodá. Profil přidává povinnou práci, na kterou plán výslovně neukazuje. Bez tohoto
seznamu by TASKS pravidla porušily, nebo by je implementace objevila až při review.

| Pravidlo | Povinná práce v I-1 | Komponenta |
|---|---|---|
| STR-001, STR-004 | Solution, `Directory.Build.props` (nullable, warnings as errors), `Directory.Packages.props` | kostra |
| DEP-001..DEP-007 | Vynucení povolených referencí mezi projekty (viz rozhodovací bod RB-4) | kostra |
| LOG-003, LOG-004 | NLog za `ILogger` ve třech kompozičních kořenech, výchozí úroveň Information, rotující soubory na desktopu | C-11, C-12, C-13 |
| CNT-002, CNT-006 | Vlastní rozšíření editoru pro ID bloků včetně rozdělení a spojení bloku | C-10 |
| CNT-003, CNT-005 | Verze schématu a rámec migrací už ve v1; zachování neznámých uzlů | C-3 |
| CNT-007 | Extrakce prostého textu pro vyhledávání | C-3 |
| VER-001..VER-007 | Merge jako čistá knihovna; verze za editační relaci; obnovení vytváří verzi | C-4 |
| SYN-001 | Handshake s verzí aplikace a protokolu; konfigurace minimální verze klienta | C-6, C-13 |
| SYN-003 | Evidence zpracovaných identifikátorů změn na serveru (idempotence) | C-13 |
| SYN-004, SYN-007 | Lokální log čekajících změn; stav synchronizace se čtyřmi stavy | C-6, C-8 |
| SYN-006, UI-004 | Fronta nových poznámek v prohlížeči s idempotentním přehráním | C-12 |
| SRC-001..SRC-004 | Normalizátor s verzí a přeindexováním; dva adaptéry indexu; filtr uživatele uvnitř adaptéru | C-5, C-8, C-13 |
| API-001..API-005 | Verzované route skupiny, anonymní allowlist, stránkování | C-13 |
| PER-002 | Globální filtr uživatele s pojmenovaným obejitím pro práci na pozadí | C-13 |
| PER-003, OPS-005 | Migrace jako samostatný krok nasazení, záloha před migrací | C-13, C-18 |
| ERR-001 | Globální handler, ProblemDetails s kódy z `error-handling-policy.md` §2 | C-13 |
| SEC-002, SEC-003 | Přihlášení desktopu přes systémový prohlížeč a PKCE, token chráněný DPAPI | C-11, C-14 |
| SEC-004 | Cookie session pro web na stejném originu | C-12, C-14 |
| SEC-006, SEC-008 | Tajné údaje z prostředí; TLS na proxy | C-18 |
| AUD-001..AUD-004 | Auditní úložiště a události z katalogu relevantní pro I-1 (`auth.*`, `identity.*`, `note.*`, `sync.*`, `security.*`) | C-17 |
| UI-001..UI-006 | Rozhraní platformních a datových služeb, popis schopností shellu, rychlé zachycení, barvy přes tokeny | C-9 |
| TST-001..TST-005 | Deset scénářů merge; konformní testy vyhledávání nad SQLite a PostgreSQL; test izolace pro každou entitu; injektovaný čas | testy |
| OPS-001..OPS-004 | `compose.yaml` pro Podman i Docker; zálohy mimo server; **zkouška obnovy**; pevné verze image | C-18 |

## 3. Kontrola úplnosti vůči cíli I-1

Cíl z `frame.md` §1 je pokrytý přiřazenými požadavky s těmito mezerami:

| ID | Mezera | Proč vadí | Kam |
|---|---|---|---|
| M-1 | **Úprava existující poznámky z tenkého klienta.** SPEC i profil řeší slučování pro desktop (sync protokol). Pro REST úpravu z telefonu není určeno, zda server změnu proti základní verzi **slučuje** (jako u desktopu), nebo ji při souběhu **odmítne** (`version_conflict`). | Při odmítnutí telefon ztrácí úpravu, pokud mezitím synchronizoval desktop — v rozporu s NFR-4. | RB-1 |
| M-2 | **Vznik verzí na tenkém klientovi.** VER-006 definuje verzi za editační relaci; tenký klient ukládá průběžně přes REST a server neví, kdy relace skončila. | Bez pravidla vznikne verze za každé uložení (zamítnutá varianta D-3.1 b). | RB-2 |
| M-3 | **První spuštění desktopu.** NFR-3 vyjímá z práce offline „přihlášení nového uživatele“, ale neříká, zda lze desktop používat lokálně ještě před prvním přihlášením. `desktop-pattern.md` §2 počítá s databází na přihlášeného uživatele. | Určuje, zda existuje stav „lokální data bez uživatele“ a jejich pozdější převod. | RB-3 |
| M-4 | **Výchozí kategorie nového uživatele.** A-3 (systémová SPEC) požaduje nesmazatelnou výchozí kategorii. Není určeno, kdo ji vytvoří a jak se shodne její identifikátor na desktopu a na serveru. | Duplicitní výchozí kategorie po první synchronizaci. | PLAN (odvoditelné, bez rozhodnutí operátora — vytváří ji server při založení uživatele a desktop ji získá při prvním přihlášení, pokud platí RB-3 a) |
| M-5 | **Distribuce desktopové aplikace.** FR-37 a `desktop-pattern.md` §6 počítají s instalátorem a aktualizacemi, ale není určeno, odkud se instalátor a aktualizační feed stahují. | Bez toho nejde desktop nainstalovat do druhého počítače ani vynutit aktualizaci. | RB-5 |
| M-6 | **Zdrojový repozitář a sestavení.** Není určeno, kde je vzdálený git repozitář a zda existuje automatické sestavení a testy (CI). | Bez CI se pravidla profilu hlídají jen ručně; publikování image a instalátoru je ruční. | O-6 |

## 4. Rozhodovací body pro PLAN tohoto běhu

| ID | Otázka | Doporučení pro PLAN | Proč |
|---|---|---|---|
| RB-1 | Jak server zpracuje úpravu existující poznámky z tenkého klienta? | **Stejně jako desktopovou změnu:** klient posílá základní verzi, server slučuje přes `Cruma.Versioning` a při souběhu vrací konflikt k vyřešení, nikdy neodmítne změnu. | Jedna cesta pro všechny zápisy; telefon nikdy neztratí úpravu (NFR-4, VER-002). |
| RB-2 | Kdy vzniká verze při úpravách z tenkého klienta? | **Server slučuje po sobě jdoucí uložení téhož uživatele, téhož klienta a téže poznámky do jedné verze**, dokud nepřijde změna z jiného zdroje nebo neuplyne interval nečinnosti. | Splní VER-006 bez závislosti na tom, zda prohlížeč stihne ohlásit konec relace. |
| RB-3 | Lze desktop použít před prvním přihlášením? | **Ne.** První spuštění vyžaduje přihlášení a připojení; potom desktop funguje offline trvale. | Žádný stav „data bez uživatele“ a žádná migrace dat mezi uživateli; odpovídá NFR-3 a databázi na uživatele. |
| RB-4 | Jak se vynutí pravidla povolených referencí (DEP-001..DEP-004)? | **Automatický architektonický test** kontrolující reference projektů podle `solution-structure-template.md` §3. | Pravidla s nejvyšší hodnotou jsou tak kontrolovaná při každém běhu testů, ne až při review; implementační model si je ověří sám. |
| RB-5 | Odkud se stahuje instalátor a aktualizace desktopu? | **Ze statického feedu vystaveného vlastním serverem Cruma** (za proxy, bez autentizace). | Žádná další služba; stejná doména a TLS jako zbytek systému. |

Žádný z bodů nemění schválenou architekturu; všechny ji zpřesňují na úroveň potřebnou pro done-when.

## 5. Podklad pro etapy uvnitř I-1 (O-2)

Vnitřní pořadí z `plan.md` §5 tvoří přirozené etapy s ověřitelným výstupem:

| Etapa | Obsah | Výstup ověřitelný na konci etapy | Vyžaduje od operátora |
|---|---|---|---|
| E-1 Základ | repozitář, solution, props, `compose.yaml` s databází pro vývoj, NLog, architektonický test (RB-4), ověření Testcontainers nad Podmanem | build a prázdné testy procházejí; test proti PostgreSQL v kontejneru projde | O-1 |
| E-2 Sdílená logika | `Cruma.Content` v1, `Cruma.Notes`, `Cruma.Versioning` s deseti scénáři, `Cruma.Search` normalizátor a tokenizátor, `Cruma.Sync` kontrakty | všechny unit testy TST-001 procházejí | — |
| E-3 Server | infrastruktura, identita (Google, cookie, PKCE), modul Notes, vyhledávání s konformními testy, sync endpoint, audit | integrační testy včetně izolace uživatelů procházejí; přihlášení Googlem funguje lokálně | O-4 |
| E-4 UI a tenký klient | `Cruma.Ui`, editor s ID bloků, `Cruma.Api.Client`, `Cruma.Web` s frontou | poznámky jdou vytvořit, upravit, hledat a řešit konflikty v prohlížeči proti lokálnímu serveru | — |
| E-5 Desktop | lokální úložiště, sync klient, WPF shell, přihlášení, instalátor a aktualizace | desktop pracuje offline a synchronizuje s lokálním serverem; souběžná úprava s prohlížečem vede ke sloučení nebo konfliktu | — |
| E-6 Nasazení | produkční compose, proxy s TLS, skript nasazení, zálohy, zkouška obnovy, feed desktopu | systém běží na doméně; PWA jde nainstalovat do telefonu; obnova ze zálohy byla provedena a zapsána | O-3, O-5 |

Etapy E-3 až E-5 jsou použitelné lokálně bez domény; doména a zálohy blokují až E-6.

## 6. Otevřené položky

| ID | Mezera | Dopad | Podmínka vyřešení |
|---|---|---|---|
| O-1 | Umístění a název repozitáře | Kořen všech scope hintů | Operátor před TASKS |
| O-2 | Etapy vs. souvislá řada úkolů | Struktura `tasks.md` | Operátor na konci PLAN; podklad v §5 |
| O-3 | Doména a DNS | Blokuje E-6 | Akce operátora před E-6 |
| O-4 | Google OAuth přihlašovací údaje | Blokuje E-3 | Akce operátora před E-3 |
| O-5 | Úložiště záloh mimo server | Blokuje E-6 | Akce operátora před E-6 |
| O-6 | **Nové.** Vzdálený git repozitář a CI (automatický build, testy, publikování image a instalátoru) | Bez CI se pravidla profilu hlídají ručně; publikování je ruční | Operátor před TASKS; pokud CI nebude, TASKS zahrnou ruční postup publikování |

## 7. Doporučení pro pokračování

Pokračovat fází SPEC tohoto běhu. SPEC pro I-1 bude **výběrem** požadavků ze systémové `spec.md` se stejnými
identifikátory, s vyznačením zúžení (FR-6, FR-7) a s akceptačními kritérii rozšířenými o zpřesnění z RB-1 až
RB-3, pokud je operátor přijme. Nové FR ani NFR nevzniknou.
