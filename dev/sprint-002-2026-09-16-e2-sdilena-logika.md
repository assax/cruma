# Sprint 002 – E-2 Sdílená logika

| | |
|---|---|
| **Začátek** | 2026-09-16 22:48 |
| **Konec** | 2026-09-16 22:59 |
| **Délka** | 11 min (čistá, bez pauz: 11 min) |
| **Stav** | hotovo |
| **Verze** | – (nevydáno) |
| **Rozsah** | tasks.md T-8..T-18 (etapa E-2) · základ `35d13cf` |
| **Tokeny** | ~78 000 (součet úseků, odhad) |
| **Změny kódu** | 58 souborů, +2 937 / −0 řádků (vč. záznamu sprintu) |

## Zadání

> zmenit, pokud buildy a testy lokalne prochazi mzues komitovat a pokracovat ci stejne nemas jak overit,
> vse lokalne

(navazuje na „pokracuj“ po sprintu 001 – další etapa E-2)

## Plán

- [x] T-8 – `Cruma.Content`: dokument se `schemaVersion`, bloky s `attrs.id`, registr typů (bloky a značky FR-7
  akc. 1), zachování neznámých uzlů, blok konfliktu (versioning-and-sync §3.3), povolená schémata odkazů
- [x] T-9 – rámec migrací: dopředné migrace v pořadí, test s fixturou
- [x] T-10 – extrakce prostého textu (content-document §4)
- [x] T-11 – `Cruma.Notes`: poznámka, kategorie, štítky, stavy, pravidla FR-1..FR-5
- [x] T-12 – `Cruma.Versioning`: model verzí (číslo, čas, zdroj, základní verze), obnovení = nová verze
- [x] T-13 – merge dokumentu po blocích, konflikt; scénáře 1, 2, 4, 5, 6, 7, 10
- [x] T-14 – merge metadat; scénáře 8, 9
- [x] T-15 – vyřešení konfliktu; scénář 3
- [x] T-16 – `Cruma.Search`: normalizátor s verzí
- [x] T-17 – tokenizátor a prefixový dotaz
- [x] T-18 – `Cruma.Sync`: kontrakty protokolu (handshake, push, výsledky, pull podle kurzoru), kompatibilita verzí
- [x] build a všechny testy lokálně, CHANGELOG, commit

## Průběh

| Čas | Co | Tokeny (zbývá / úsek) |
|---|---|---|
| 22:48 | začátek – analýza tasks E-2, spec I-1 §3, content-document, versioning-and-sync, search pattern | 14 964 000 / 36 000 |
| 22:50 | T-8..T-10 – Cruma.Content (dokument, registr, migrace, extrakce, blok konfliktu, odkazy), testy 32/32 | 14 943 000 / 21 000 |
| 22:52 | T-11 – Cruma.Notes (poznámka, kategorie, štítky, stavy, přehledy), testy 22/22 | 14 931 000 / 12 000 |
| 22:56 | T-12..T-15 – Cruma.Versioning (verze, merge dokumentu a vlastností, konflikt, vyřešení), tabulkové scénáře 1–10, testy 41/41 | 14 902 000 / 29 000 |
| 22:57 | T-16, T-17 – Cruma.Search (normalizátor s verzí, tokenizátor, prefixový dotaz), testy 22/22 | 14 897 000 / 5 000 |
| 22:58 | T-18 – Cruma.Sync (handshake, push, výsledky, pull podle kurzoru, kompatibilita verzí), nový Cruma.Sync.Tests 15/15 | 14 888 000 / 9 000 |
| 22:58 | celé řešení: `dotnet test Cruma.slnx` 145/145 (25 s, vč. PostgreSQL v kontejneru) | 14 887 000 / 1 000 |
| 22:59 | konec – CHANGELOG, záznam, commit | 14 886 000 / 1 000 |

## Rozhodnutí během sprintu

- **Nový projekt `Cruma.Sync.Tests`** (agent) – šablona §4 testy pro `Cruma.Sync` neměla, kompatibilitu verzí
  a serializaci kontraktů bylo potřeba otestovat. Řádek doplněn do `solution-structure-template.md` §4 (STR-001).
- **Dokument drží původní JSON uzly** (`System.Text.Json.Nodes`, jen BCL – DEP-002). Neznámé bloky, atributy
  i vlastnosti kořene tak projdou načtením, merge a uložením beze změny (CNT-005).
- **Identifikátor bloku v `attrs.id`**, verze schématu jako vlastnost kořene `schemaVersion` – tvar, který TipTap
  načte bez překladu (content-document §1).
- **Blok konfliktu je typ schématu v `Cruma.Content`** (`conflict` se dvěma `conflictVariant`), ne ve Versioning –
  schéma definuje jen Content (CNT-001). Blok konfliktu nese ID původního bloku.
- **Značky VER-004 nejsou v dokumentu**, ale v seznamu upozornění výsledku merge (`MergeNotice`) – zápis do
  dokumentu by změnil obsah bloku a rozbil další merge. Uložení upozornění k verzi řeší server (E-3).
- **Pořadí bloků při merge** (§3.2): pořadí strany, která jediná přeuspořádala, jinak serveru; bloky druhé strany
  za nejbližší předchozí společný blok a za bloky přidané první stranou (scénář 7: server první).
- **Poznámka v koši upravená klientem** se vrátí mezi aktivní jen tehdy, když by po merge vlastností zůstala
  v koši; změnil-li klient stav jinak, platí VER-005 (později přijatá změna).
- **`Cruma.Notes.Note` nenese obsah** – `Cruma.Notes` podle šablony §3 nesmí referencovat `Cruma.Content`.
  Obsah a vlastnosti spojuje až verze (`Cruma.Versioning.NoteVersion`) a payload protokolu.
- **Barva karty jako klíč tématu** (`yellow`), ne hodnota barvy – hodnoty pro světlý a tmavý režim patří tématu
  UI (UI-006, FR-2 akc. 5).
- **Rozhraní adaptéru indexu** (součást C-5) zatím nevzniklo – žádný úkol E-2 ho nežádá; vznikne s prvním
  adaptérem, aby se navrhlo podle skutečného použití.

## Výsledek

- **Commity:** „E-2: sdílená logika – obsah, poznámky, verze a merge, vyhledávání, kontrakty synchronizace“
- **Vydání:** žádné
- **Testy:** celé řešení 145/145 (Content 32, Notes 22, Versioning 41, Search 22, Sync 15, Architecture 11,
  Server 2); výstup E-2 – všechny unit testy TST-001 včetně FR-28 akc. 1–4 jako scénářů merge – splněn
- **Diagnostika:** tokeny ~78 000 (součet úseků), změny 58 souborů +2 937
- **Stav bodů:** –

## Nestihlo se / otevřené

- Nic z rozsahu E-2. Další etapa E-3 má vstupní podmínku **T-19 (Google OAuth, operátor)** – bez ní nezačne.

## Poznámky

- Normalizátor používá `string.Normalize` (NFC/NFD). V Blazor WebAssembly funguje jen s ICU; při zapnutí
  `InvariantGlobalization` ve webu by vyhledávání přestalo odpovídat desktopu – ověřit v E-4 konformním testem.
