> **READER CONTRACT**
> This is the work list. Implement from here.
> Each task states: what it implements (C-n / FR-n), where it belongs (Scope hint), when it is done (Done-when).
> Read further only when a task points you there:
>   FR-n / NFR-n  → spec.md          (what is required, and why)
>   C-n           → plan.md          (component design, boundaries, dependency order)
>   rule IDs      → .architecture/architecture-cruma/   (binding architecture rules — authoritative there, not here)
> On conflict: architecture profile > spec.md > plan.md > this file.
> plan.md contains rejected alternatives, marked REJECTED. Never implement one.
> If a task's Scope hint reads "unresolved", stop and ask — do not choose a location yourself.

# tasks.md — Cruma, inkrement I-1 (walking skeleton)

```text
Phase: TASKS          Run mode: CONCEPTUAL          Architecture basis: profile @ .architecture\architecture-cruma\PROFILE.md
Output language: czech
Open items: O-3, O-4, O-5          Carried assumptions: A-1, A-2, A-3, A-4
Last approved gate: TASKS->CLOSED @ 2026-09-16
```

**Odkazy v tomto souboru:** `spec.md`, `plan.md` a `DECISIONS.md` jsou soubory ve složce `docs/arch/i-1/`.
Scope hinty jsou relativní ke kořeni repozitáře `c:\MyWork\_project\cruma` a pocházejí ze
`solution-structure-template.md` profilu (zapsané v `TRACEABILITY.md`). Všechny lokace jsou nové.

**Etapy.** Úkoly jsou seskupené do etap E-1..E-6 (`plan.md` §4). Etapa nezačne, dokud předchozí nesplní svůj
výstup. Úkoly označené `Owner: operator` provádí operátor, ne implementační agent; agent na ně čeká.

**Pravidla profilu** jsou uvedena u úkolu, pokud ho zvlášť vážou. Celý registr
`shared/architecture-rules.md` platí vždy, s výjimkami v `spec.md` §5.

```text
Scope hint coverage: 63/63 resolved, 0 unresolved
```

---

## E-1 — Základ

**Výstup etapy (`plan.md` §4 E-1):** CI build prochází; architektonický test prochází; test proti PostgreSQL
v kontejneru prochází lokálně i v CI.

```text
T-1: Přejmenovat složku projektu a založit repozitář na GitHubu
  Owner:             operator
  PLAN linkage:      C-18 (plan.md §1, E-1)
  SPEC/FR linkage:   A-3, A-4
  Scope hint:        (repo) c:\MyWork\_project\cruma
  Depends on:        —
  Done-when:         Složka c:\MyWork\_project\google-keep-client je přejmenována na c:\MyWork\_project\cruma
                     včetně docs/ a .architecture/ (A-3); složka je git repozitář napojený na soukromý
                     repozitář `cruma` na GitHubu (A-4).
```

```text
T-2: Založit solution a společné soubory buildu
  PLAN linkage:      C-18 (E-1)
  SPEC/FR linkage:   S-I1-2
  Scope hint:        (repo) Cruma.slnx, Directory.Build.props, Directory.Packages.props, src/, tests/
  Depends on:        T-1
  Profile rules:     STR-002, STR-004
  Done-when:         Existuje Cruma.slnx v kořeni a složky src/, tests/, deploy/; Directory.Build.props zapíná
                     nullable reference types, implicit usings a warnings as errors pro produkční projekty;
                     verze balíčků jsou jen v Directory.Packages.props (STR-002, STR-004); `dotnet build` projde.
```

```text
T-3: Založit kostry všech projektů I-1 s povolenými referencemi
  PLAN linkage:      C-1, C-2, C-3, C-4, C-5, C-6, C-8, C-9, C-10, C-11, C-12, C-13 (plan.md §2)
  SPEC/FR linkage:   S-I1-2
  Scope hint:        src/, tests/ — projekty podle solution-structure-template.md §2, §4 a řádku I-1 v §5
  Depends on:        T-2
  Profile rules:     STR-001, DEP-001..DEP-004, NAM-001
  Done-when:         Existují právě projekty vyjmenované pro I-1 v solution-structure-template.md §5
                     (včetně prázdného Cruma.Kanban) a jejich testovací projekty; reference odpovídají §3 šablony;
                     Cruma.Web je hostitelný ze Cruma.Server (DEP-004 výjimka); řešení se sestaví.
```

```text
T-4: Architektonický test povolených referencí
  PLAN linkage:      N-5
  SPEC/FR linkage:   S-I1-2, I1-D-4
  Scope hint:        tests/
  Depends on:        T-3
  Profile rules:     DEP-001..DEP-004, STR-001
  Done-when:         Test čte reference všech projektů v src/ a porovná je se seznamem povolených referencí ze
                     solution-structure-template.md §3; nepovolená reference test shodí s názvem obou projektů
                     (plan.md N-5); test na aktuálním řešení prochází.
```

```text
T-5: Vývojová databáze v kontejneru a ověření Testcontainers nad Podmanem
  PLAN linkage:      C-18 (E-1), frame R-4
  SPEC/FR linkage:   S-I1-2
  Scope hint:        deploy/compose.yaml, tests/Cruma.Server.Tests/
  Depends on:        T-3
  Profile rules:     OPS-001, OPS-004, TST-003
  Done-when:         deploy/compose.yaml má profil pro vývoj, který spustí jen PostgreSQL, a funguje v Podmanu
                     i Dockeru bez úprav (OPS-001); ověřovací test spustí PostgreSQL přes Testcontainers, připojí
                     se a projde lokálně nad Podmanem (plan.md E-1 výstup).
```

```text
T-6: NLog ve třech kompozičních kořenech
  PLAN linkage:      C-11, C-12, C-13 (E-1)
  SPEC/FR linkage:   NFR-8
  Scope hint:        src/Cruma.Desktop/, src/Cruma.Web/, src/Cruma.Server/
  Depends on:        T-3
  Profile rules:     LOG-001, LOG-002, LOG-003, LOG-004
  Done-when:         Cruma.Desktop, Cruma.Web a Cruma.Server logují přes ILogger s NLog jako providerem; NLog typy
                     se vyskytují jen v konfiguraci logování kompozičního kořene a v nlog.config (LOG-003);
                     výchozí úroveň je Information (LOG-004); cíle odpovídají logging-and-audit-policy.md §4.
```

```text
T-7: CI build na GitHub Actions
  PLAN linkage:      N-8
  SPEC/FR linkage:   S-I1-2, A-4
  Scope hint:        .github/workflows/
  Depends on:        T-4, T-5
  Profile rules:     SEC-006
  Done-when:         Workflow build se spustí při každém push a pull requestu, sestaví řešení a spustí všechny
                     testy včetně Testcontainers na Linux runneru (plan.md N-8); architektonický test i test
                     proti PostgreSQL v CI procházejí (plan.md E-1 výstup).
```

---

## E-2 — Sdílená logika

**Výstup etapy (`plan.md` §4 E-2):** všechny unit testy TST-001 procházejí, včetně FR-28 akc. 1–4 jako scénářů
merge.

```text
T-8: Schéma dokumentu v1 a registr typů
  PLAN linkage:      C-3
  SPEC/FR linkage:   FR-7, NFR-13
  Scope hint:        src/Cruma.Content/
  Depends on:        E-1
  Profile rules:     CNT-001, CNT-002, CNT-003, CNT-005
  Done-when:         Schéma obsahuje bloky a značky z FR-7 akc. 1; každý blok nese identifikátor bloku; dokument
                     nese verzi schématu a uzel neznámého typu se při načtení a uložení zachová beze změny
                     (FR-7 akc. 6); pokryto unit testy (TST-001).
```

```text
T-9: Rámec migrací schématu
  PLAN linkage:      C-3
  SPEC/FR linkage:   FR-7, NFR-13
  Scope hint:        src/Cruma.Content/
  Depends on:        T-8
  Profile rules:     CNT-003
  Done-when:         Čtení dokumentu se starší verzí schématu aplikuje migrace v pořadí; migrace jsou dopředné
                     a deterministické (CNT-003); rámec je pokryt testem s fixturou dokumentu (TST-001).
```

```text
T-10: Extrakce prostého textu z dokumentu
  PLAN linkage:      C-3
  SPEC/FR linkage:   FR-24
  Scope hint:        src/Cruma.Content/
  Depends on:        T-8
  Profile rules:     CNT-007
  Done-when:         Extrakce vrací text názvu a obsahu poznámky pro vyhledávání (FR-24 akc. 5) podle
                     content-document-pattern.md §4; pokryto unit testy.
```

```text
T-11: Doména poznámek
  PLAN linkage:      C-1
  SPEC/FR linkage:   FR-1, FR-2, FR-3, FR-4, FR-5
  Scope hint:        src/Cruma.Notes/
  Depends on:        E-1
  Profile rules:     DEP-001, DEP-002, DEP-005
  Done-when:         Model splňuje: nová poznámka nevyžaduje nic kromě obsahu a patří do výchozí kategorie
                     (FR-1 akc. 2, 3); vlastnosti z FR-2 akc. 1 jdou nastavit i zrušit (FR-2 akc. 4); kategorie
                     jsou jednoúrovňové, mazaná kategorie přesune poznámky do výchozí, výchozí nejde smazat
                     (FR-3 akc. 1–4); štítky nezávislé na kategorii (FR-4 akc. 1, 3); stavy aktivní / archiv /
                     koš s obnovením (FR-5 akc. 1–3); pokryto unit testy.
```

```text
T-12: Model verzí
  PLAN linkage:      C-4
  SPEC/FR linkage:   FR-6
  Scope hint:        src/Cruma.Versioning/
  Depends on:        T-8, T-11
  Profile rules:     VER-001, VER-007
  Done-when:         Verze nese číslo, čas vzniku, zdroj (desktop, web, merge) a základní verzi (FR-6 akc. 1);
                     logika je bez I/O, času a náhody (VER-001); pokryto unit testy.
```

```text
T-13: Merge dokumentu po blocích a model konfliktu
  PLAN linkage:      C-4
  SPEC/FR linkage:   FR-28, NFR-4
  Scope hint:        src/Cruma.Versioning/
  Depends on:        T-12
  Profile rules:     VER-001, VER-003, VER-004, TST-001
  Done-when:         Scénáře 1, 2, 4, 5, 6, 7 a 10 z testing-strategy.md §3 procházejí: změny různých bloků se
                     sloučí (FR-28 akc. 1); změna téhož bloku vytvoří konflikt s oběma variantami (FR-28 akc. 2);
                     úprava proti smazání úpravu zachová (FR-28 akc. 4); neznámý typ bloku přežije merge.
```

```text
T-14: Merge metadat
  PLAN linkage:      C-4
  SPEC/FR linkage:   FR-28
  Scope hint:        src/Cruma.Versioning/
  Depends on:        T-12
  Profile rules:     VER-005
  Done-when:         Scénáře 8 a 9 z testing-strategy.md §3 procházejí: štítky se slučují přidáním a odebráním;
                     u skalární vlastnosti změněné na obou stranách vyhraje později přijatá a přepsaná hodnota
                     je v záznamu verze (FR-28 akc. 6).
```

```text
T-15: Vyřešení konfliktu
  PLAN linkage:      C-4
  SPEC/FR linkage:   FR-28
  Scope hint:        src/Cruma.Versioning/
  Depends on:        T-13
  Done-when:         Scénář 3 z testing-strategy.md §3 prochází: vyřešení konfliktu výběrem varianty nebo ruční
                     úpravou vytvoří novou verzi bez značky konfliktu (FR-28 akc. 3).
```

```text
T-16: Normalizátor textu
  PLAN linkage:      C-5
  SPEC/FR linkage:   FR-24, NFR-5
  Scope hint:        src/Cruma.Search/
  Depends on:        E-1
  Profile rules:     SRC-001, SRC-002
  Done-when:         Normalizátor provádí NFC, sjednocení velikosti písmen a odstranění diakritiky podle
                     search-pattern.md §2 a má verzi (SRC-002); `Certifikát` → `certifikat` (FR-24 akc. 2);
                     pokryto unit testy (TST-001).
```

```text
T-17: Tokenizátor a dotaz podle začátku slova
  PLAN linkage:      C-5
  SPEC/FR linkage:   FR-24, NFR-5
  Scope hint:        src/Cruma.Search/
  Depends on:        T-16
  Profile rules:     SRC-001, SRC-003
  Done-when:         Tokenizátor dělí text podle search-pattern.md §3 bez stop slov a stemmingu; dotaz projde
                     stejnou normalizací a tokenizací a každý token se hledá jako prefix (SRC-003); dotaz
                     `certif` odpovídá tokenu textu `certifikátu` (FR-24 akc. 3); pokryto unit testy.
```

```text
T-18: Kontrakty synchronizačního protokolu
  PLAN linkage:      C-6, N-2
  SPEC/FR linkage:   FR-27, FR-37
  Scope hint:        src/Cruma.Sync/
  Depends on:        T-12
  Profile rules:     SYN-001, SYN-002, SYN-003, NFR-18
  Done-when:         Kontrakty pokrývají handshake s verzí aplikace a protokolu a odmítnutím pod minimální verzí
                     (SYN-001, FR-37 akc. 1); změnu s identifikátorem změny a základní verzí (SYN-002, SYN-003);
                     výsledek po změnách (applied / merged / conflict / rejected) a pull podle kurzoru změnového
                     feedu (plan.md N-2); sestava podle versioning-and-sync-pattern.md §5.1.
```

---

## E-3 — Server

**Vstupní podmínka:** T-19 (Google OAuth) je hotový.
**Výstup etapy (`plan.md` §4 E-3):** integrační testy procházejí; lokální přihlášení Googlem funguje pro web i pro
prototyp desktopu.

```text
T-19: Přihlašovací údaje Google OAuth
  Owner:             operator
  PLAN linkage:      C-14 (E-3 vstup), O-4
  SPEC/FR linkage:   FR-32
  Scope hint:        src/Cruma.Server/Identity/
  Depends on:        E-2
  Profile rules:     SEC-006
  Done-when:         Existuje projekt v Google Cloud s OAuth klientem; client ID a secret jsou k dispozici jako
                     tajné údaje mimo repozitář (SEC-006); jsou zaregistrované redirect URI pro lokální vývojový
                     server (O-4).
```

```text
T-20: Prototyp přihlášení desktopu přes systémový prohlížeč
  PLAN linkage:      C-14, C-11, frame R-3
  SPEC/FR linkage:   FR-32
  Scope hint:        src/Cruma.Server/Identity/, src/Cruma.Desktop/
  Depends on:        T-19
  Profile rules:     SEC-001, SEC-002
  Done-when:         Desktop otevře systémový prohlížeč, uživatel se přihlásí účtem Google a desktop dostane
                     token vydaný serverem Cruma přes authorization code + PKCE s loopback redirectem (SEC-002,
                     FR-32 akc. 3); ověřeno jako první úkol etapy E-3 (plan.md §4 E-3).
```

```text
T-21: Serverová infrastruktura
  PLAN linkage:      C-13
  SPEC/FR linkage:   FR-31, NFR-8, NFR-18
  Scope hint:        src/Cruma.Server/Infrastructure/
  Depends on:        T-5, E-2
  Profile rules:     PER-001, PER-002, PER-003, ERR-001, ERR-003, API-001, API-003, NAM-003
  Done-when:         CrumaDbContext nad PostgreSQL s globálním filtrem aktuálního uživatele a pojmenovaným
                     obejitím (PER-002); migrace se neaplikují při startu (PER-003); globální handler vrací
                     ProblemDetails s kódy z error-handling-policy.md §2 (ERR-001); anonymní allowlist
                     (API-003); correlation id v logech a ProblemDetails; pokryto integračními testy.
```

```text
T-22: Modul identity a založení uživatele
  PLAN linkage:      C-14, N-4
  SPEC/FR linkage:   FR-32, FR-3, NFR-14
  Scope hint:        src/Cruma.Server/Identity/
  Depends on:        T-20, T-21
  Profile rules:     SEC-001, SEC-002, SEC-004, SEC-005
  Done-when:         Uživatel se přihlásí účtem Google na webu (cookie session, bez tokenů v prohlížeči,
                     FR-32 akc. 1, 4) i na desktopu (FR-32 akc. 1, 3); první přihlášení v jedné transakci založí
                     uživatele, naváže identitu a vytvoří výchozí kategorii (plan.md N-4, FR-3 akc. 5); přidání
                     poskytovatele nevyžaduje změnu uložených dat (FR-32 akc. 2); odhlášení funguje (FR-32 akc. 5).
```

```text
T-23: Modul auditu
  PLAN linkage:      C-17
  SPEC/FR linkage:   FR-35, NFR-10
  Scope hint:        src/Cruma.Server/Audit/
  Depends on:        T-21
  Profile rules:     AUD-001, AUD-002, AUD-004, LOG-002
  Done-when:         Auditní záznam obsahuje čas, uživatele, typ operace, typ a id objektu, typ klienta a výsledek,
                     nikdy obsah (FR-35 akc. 2); záznamy jsou dohledatelné podle uživatele, času a typu (FR-35
                     akc. 3); uložené ve schématu audit mimo technické logy, bez možnosti úpravy a smazání
                     (FR-35 akc. 4); přihlašovací události z FR-35 akc. 1 jsou zapisovány.
```

```text
T-24: Modul poznámek — perzistence a jednotná zápisová cesta
  PLAN linkage:      C-13, N-1
  SPEC/FR linkage:   FR-2, FR-3, FR-4, FR-5, FR-28, FR-29
  Scope hint:        src/Cruma.Server/Notes/
  Depends on:        T-21, T-22, T-23, T-13, T-14, T-15
  Profile rules:     VER-002, DEP-007, PER-002, AUD-003
  Done-when:         Všechny zápisy poznámek, kategorií a štítků procházejí jednou aplikační službou, která ověří
                     vlastnictví, provede merge proti základní verzi, uloží verzi, zapíše audit a vrátí výsledek
                     (plan.md N-1); úprava z tenkého klienta se při souběhu nikdy neodmítne (FR-28 akc. 5);
                     trvalé smazání odstraní i verze (FR-5 akc. 3); auditní události poznámek z FR-35 akc. 1.
```

```text
T-25: Spojování uložení do verzí
  PLAN linkage:      N-3
  SPEC/FR linkage:   FR-6
  Scope hint:        src/Cruma.Server/Notes/
  Depends on:        T-24
  Profile rules:     VER-006
  Done-when:         Po sobě jdoucí uložení téže poznámky od téhož uživatele z téže instance klienta do intervalu
                     nečinnosti aktualizují poslední verzi, dokud nepřijde změna z jiného zdroje (FR-6 akc. 3);
                     interval je konfigurační hodnota s výchozí hodnotou 5 minut (plan.md N-3); pokryto
                     integračními testy.
```

```text
T-26: Změnový feed uživatele
  PLAN linkage:      N-2
  SPEC/FR linkage:   FR-27
  Scope hint:        src/Cruma.Server/Sync/
  Depends on:        T-24
  Done-when:         Každý zápis přes zápisovou cestu i změna kategorie nebo štítku zapíše záznam s monotónně
                     rostoucím pořadím pro uživatele; dotaz vrátí záznamy s pořadím vyšším než kurzor
                     (plan.md N-2); pokryto integračními testy.
```

```text
T-27: REST API poznámek v1
  PLAN linkage:      C-13
  SPEC/FR linkage:   FR-1, FR-2, FR-3, FR-4, FR-5, FR-28, FR-29
  Scope hint:        src/Cruma.Server/Notes/, src/Cruma.Api.Contracts/
  Depends on:        T-24
  Profile rules:     API-001, API-002, API-003, API-004, API-005
  Done-when:         Endpointy pod /api/v1 pokrývají vytvoření, čtení, seznam se stránkováním, úpravu se základní
                     verzí, archivaci, přesun do koše, obnovení, trvalé smazání, kategorie, štítky a vyřešení
                     konfliktu (FR-28 akc. 3); změna je okamžitě uložena na serveru (FR-29 akc. 2); kontrakty
                     jsou v Cruma.Api.Contracts (API-004); endpointy jen delegují (API-002).
```

```text
T-28: Modul vyhledávání s adaptérem PostgreSQL
  PLAN linkage:      C-5, C-13
  SPEC/FR linkage:   FR-24, NFR-5
  Scope hint:        src/Cruma.Server/Search/
  Depends on:        T-10, T-17, T-24
  Profile rules:     SRC-001, SRC-004, PER-004
  Done-when:         Index ukládá tokeny z Cruma.Search podle search-pattern.md §4 bez databázové normalizace
                     (SRC-001); omezení na uživatele je uvnitř adaptéru (SRC-004); endpoint vrací poznámky
                     s filtrem stavu aktivní / archiv / koš (FR-24 akc. 1, 6); index se aktualizuje ze zápisové
                     cesty.
```

```text
T-29: Konformní sada vyhledávání — společný základ a PostgreSQL
  PLAN linkage:      C-5
  SPEC/FR linkage:   FR-24
  Scope hint:        tests/Cruma.Search.Conformance.Tests/
  Depends on:        T-28
  Profile rules:     TST-002
  Done-when:         Abstraktní sada s fixturou českého textu podle testing-strategy.md §4 prochází nad
                     adaptérem PostgreSQL, včetně dotazů `certifikat` → `Certifikát` a `certif` → `certifikátu`
                     (FR-24 akc. 2, 3).
```

```text
T-30: Synchronizační endpoint
  PLAN linkage:      C-13, C-6, N-6
  SPEC/FR linkage:   FR-27, FR-37
  Scope hint:        src/Cruma.Server/Sync/
  Depends on:        T-18, T-24, T-26
  Profile rules:     SYN-001, SYN-002, SYN-003, VER-002, AUD-003
  Done-when:         Handshake porovná verzi klienta s konfigurovanou minimální verzí a pod ní vrátí
                     client_version_unsupported (FR-37 akc. 1, plan.md N-6); push zpracuje každou změnu nejvýše
                     jednou podle identifikátoru změny přes zápisovou cestu (SYN-003, N-1); pull vrací změny podle
                     kurzoru (N-2); relace i odmítnuté změny jsou auditované (FR-35 akc. 1).
```

```text
T-31: Testy izolace uživatelů
  PLAN linkage:      C-13
  SPEC/FR linkage:   FR-31
  Scope hint:        tests/Cruma.Server.Tests/
  Depends on:        T-27, T-28, T-30
  Profile rules:     TST-004, PER-002, SRC-004
  Done-when:         Pro každý endpoint a entitu I-1 uživatel B při čtení, výpisu, vyhledání, úpravě, smazání
                     a synchronizaci dat uživatele A dostane not_found nebo prázdný výsledek (FR-31 akc. 1, 2;
                     testing-strategy.md §5).
```

---

## E-4 — Sdílené UI a tenký klient

**Výstup etapy (`plan.md` §4 E-4):** v prohlížeči proti lokálnímu serveru jde splnit akceptační kritéria FR-1..FR-5,
FR-7, FR-8, FR-24, FR-28 akc. 3, FR-29, FR-30, FR-34.

```text
T-32: Rozhraní služeb a popis schopností v Cruma.Ui
  PLAN linkage:      C-9
  SPEC/FR linkage:   NFR-15, NFR-19
  Scope hint:        src/Cruma.Ui/
  Depends on:        E-3
  Profile rules:     UI-001, UI-002, UI-003
  Done-when:         Cruma.Ui deklaruje datové služby, platformní služby a popis schopností podle ui-pattern.md
                     §1–§2; žádná komponenta nevolá IJSRuntime, HttpClient ani platformní API přímo (UI-001,
                     UI-002); architektonický test (T-4) prochází.
```

```text
T-33: Motivy světlý a tmavý
  PLAN linkage:      C-9
  SPEC/FR linkage:   FR-34, FR-2
  Scope hint:        src/Cruma.Ui/
  Depends on:        T-32
  Profile rules:     UI-006
  Done-when:         Uživatel přepne světlý a tmavý režim (FR-34 akc. 1); barvy jsou jen tokeny a barva karty
                     poznámky má hodnotu pro oba režimy (FR-2 akc. 5, UI-006).
```

```text
T-34: Editorový balík a interop
  PLAN linkage:      C-10
  SPEC/FR linkage:   FR-7
  Scope hint:        src/Cruma.Ui.Editor/
  Depends on:        T-8, T-32
  Profile rules:     CNT-006, UI-001
  Done-when:         TipTap balík se sestavuje jako součást buildu a je static web asset; interop podle
                     content-document-pattern.md §3 (načtení dokumentu, hlášení změn, příkazy); editor podporuje
                     prvky z FR-7 akc. 1 a každý jde vložit, upravit a odstranit (FR-7 akc. 2); po uložení
                     a opětovném otevření jsou prvky beze změny (FR-7 akc. 4).
```

```text
T-35: Rozšíření editoru pro identifikátory bloků
  PLAN linkage:      C-10, C-3
  SPEC/FR linkage:   FR-7
  Scope hint:        src/Cruma.Ui.Editor/
  Depends on:        T-34
  Profile rules:     CNT-002, CNT-006
  Done-when:         Identifikátor bloku přetrvá úpravu, přesun a změnu typu; rozdělení ponechá id první části,
                     spojení ponechá id prvního bloku; vložený nebo duplikovaný blok dostane nové id (FR-7 akc. 5);
                     pokryto testy editoru.
```

```text
T-36: Checklist v zobrazení a omezení odkazů
  PLAN linkage:      C-10
  SPEC/FR linkage:   FR-7
  Scope hint:        src/Cruma.Ui.Editor/
  Depends on:        T-34
  Done-when:         Položku checklistu jde označit a odznačit přímo v zobrazení poznámky (FR-7 akc. 3); odkaz
                     povoluje jen http, https a mailto (FR-7 akc. 7).
```

```text
T-37: Typovaný API klient
  PLAN linkage:      C-9
  SPEC/FR linkage:   FR-29
  Scope hint:        src/Cruma.Api.Client/
  Depends on:        T-27, T-28
  Profile rules:     API-004, DEP-006
  Done-when:         Klient pokrývá endpointy z T-27 a T-28 nad kontrakty Cruma.Api.Contracts a převádí
                     ProblemDetails na výsledky s kódem chyby (error-handling-policy.md §2).
```

```text
T-38: Přehled poznámek, archiv a koš
  PLAN linkage:      C-9
  SPEC/FR linkage:   FR-2, FR-4, FR-5
  Scope hint:        src/Cruma.Ui/
  Depends on:        T-32, T-33
  Done-when:         Připnuté poznámky jsou před nepřipnutými (FR-2 akc. 3); filtr podle štítku zobrazí poznámky
                     všech kategorií (FR-4 akc. 2); archiv a koš se zobrazují odděleně a z koše jde obnovit
                     a trvale smazat s explicitním potvrzením (FR-5 akc. 1–3).
```

```text
T-39: Rychlé zachycení poznámky
  PLAN linkage:      C-9
  SPEC/FR linkage:   FR-1, NFR-1, NFR-2
  Scope hint:        src/Cruma.Ui/
  Depends on:        T-34, T-38
  Profile rules:     UI-005
  Done-when:         Poznámka vznikne posloupností otevřít → napsat → uložit bez dalšího kroku (FR-1 akc. 1);
                     nevyžaduje název, kategorii, štítek ani barvu (FR-1 akc. 2); vždy spadne do výchozí
                     kategorie (FR-1 akc. 3).
```

```text
T-40: Detail poznámky
  PLAN linkage:      C-9
  SPEC/FR linkage:   FR-2, FR-3, FR-4
  Scope hint:        src/Cruma.Ui/
  Depends on:        T-35, T-36, T-38
  Done-when:         Poznámku bez názvu jde uložit a zobrazit (FR-2 akc. 2); název, kategorii, štítky, barvu,
                     připnutí a stav jde nastavit i zrušit po vytvoření (FR-2 akc. 4); obsah se edituje editorem
                     z T-34.
```

```text
T-41: Správa kategorií a štítků
  PLAN linkage:      C-9
  SPEC/FR linkage:   FR-3, FR-4
  Scope hint:        src/Cruma.Ui/
  Depends on:        T-38
  Done-when:         Kategorii jde vytvořit, přejmenovat a smazat, nejde vnořit a výchozí nejde smazat; poznámky
                     mazané kategorie se přesunou do výchozí (FR-3 akc. 1–4); smazání štítku ho odebere
                     z poznámek a poznámky zachová (FR-4 akc. 3).
```

```text
T-42: Vyhledávání v UI
  PLAN linkage:      C-9
  SPEC/FR linkage:   FR-24
  Scope hint:        src/Cruma.Ui/
  Depends on:        T-38
  Done-when:         Výsledky obsahují poznámky (FR-24 akc. 1) a jdou filtrovat podle stavu aktivní / archiv /
                     koš (FR-24 akc. 6).
```

```text
T-43: Řešení konfliktů v UI
  PLAN linkage:      C-9
  SPEC/FR linkage:   FR-28
  Scope hint:        src/Cruma.Ui/
  Depends on:        T-40
  Done-when:         Konfliktní blok zobrazí obě varianty (FR-28 akc. 2); uživatel vybere variantu nebo upraví
                     ručně a vyřešení vytvoří novou verzi (FR-28 akc. 3); ohlášení přepsané skalární hodnoty je
                     viditelné (FR-28 akc. 6).
```

```text
T-44: Šířka editoru a responzivita
  PLAN linkage:      C-9
  SPEC/FR linkage:   FR-8
  Scope hint:        src/Cruma.Ui/
  Depends on:        T-40
  Done-when:         Při schopnosti „editor width modes“ jsou dostupné režimy standardní, rozšířená a plná šířka
                     s okamžitým projevem (FR-8 akc. 1); na úzkém displeji nevzniká vodorovný posun celé stránky
                     (FR-8 akc. 2).
```

```text
T-45: Webový shell jako PWA
  PLAN linkage:      C-12
  SPEC/FR linkage:   FR-29, FR-32
  Scope hint:        src/Cruma.Web/
  Depends on:        T-37, T-39, T-40, T-41, T-42, T-43, T-44
  Profile rules:     SEC-004, UI-004, DEP-004
  Done-when:         Cruma.Web je hostovaný ze Cruma.Server na stejném originu s cookie session (FR-32 akc. 4);
                     zobrazuje a upravuje poznámky, kategorie a štítky, vyhledává a řeší konflikty (FR-29 akc. 1);
                     jde nainstalovat jako PWA a spustit bez prohlížečového rozhraní (FR-29 akc. 3).
```

```text
T-46: Offline stav a fronta zápisů
  PLAN linkage:      C-12
  SPEC/FR linkage:   FR-29, FR-30
  Scope hint:        src/Cruma.Web/
  Depends on:        T-45
  Profile rules:     SYN-006, UI-004
  Done-when:         Bez připojení jsou akce kromě nové poznámky nedostupné a klient ukazuje stav offline
                     (FR-29 akc. 4); novou poznámku jde uložit do fronty, fronta je viditelná, přežije zavření
                     klienta a po obnovení spojení se odešle bez akce uživatele (FR-30 akc. 1–4); opakované
                     odeslání nevytvoří druhou poznámku (FR-30 akc. 6).
```

---

## E-5 — Desktop

**Výstup etapy (`plan.md` §4 E-5):** desktop pracuje offline a synchronizuje s lokálním serverem; souběžná úprava
s prohlížečem vede ke sloučení nebo konfliktu podle FR-28; konformní sada prochází nad oběma adaptéry; výkonový
scénář N-7 prochází.

```text
T-47: Lokální úložiště desktopu
  PLAN linkage:      C-8
  SPEC/FR linkage:   FR-26, FR-6
  Scope hint:        src/Cruma.Desktop.Storage/
  Depends on:        E-4
  Profile rules:     PER-001, PER-005, SYN-004
  Done-when:         SQLite databáze pro přihlášeného uživatele s poznámkami, kategoriemi, štítky, lokálními
                     verzemi, logem čekajících změn a kurzorem podle desktop-pattern.md §2; lokální migrace se
                     aplikují při startu po zálohování souboru databáze; databáze neobsahuje tokeny ani tajné
                     údaje (PER-005).
```

```text
T-48: Adaptér vyhledávání SQLite v konformní sadě
  PLAN linkage:      C-5, C-8
  SPEC/FR linkage:   FR-24
  Scope hint:        src/Cruma.Desktop.Storage/Search/, tests/Cruma.Search.Conformance.Tests/
  Depends on:        T-29, T-47
  Profile rules:     SRC-001, TST-002, PER-004
  Done-when:         FTS5 index ukládá tokeny z Cruma.Search bez vlastní normalizace (SRC-001); konformní sada
                     z T-29 prochází nad SQLite i PostgreSQL se shodnou množinou výsledků (FR-24 akc. 4).
```

```text
T-49: Datové služby desktopu a lokální editační relace
  PLAN linkage:      C-8, C-9, N-3
  SPEC/FR linkage:   FR-26, FR-6
  Scope hint:        src/Cruma.Desktop/
  Depends on:        T-47, T-48
  Profile rules:     UI-002, VER-006
  Done-when:         Datové služby Cruma.Ui čtou a zapisují lokální úložiště; bez sítě jde vytvářet, upravovat,
                     archivovat, mazat a vyhledávat poznámky (FR-26 akc. 1); lokální verze vzniká za editační
                     relaci s intervalem nečinnosti 5 minut (FR-6 akc. 2, plan.md N-3).
```

```text
T-50: Synchronizační engine desktopu
  PLAN linkage:      C-6
  SPEC/FR linkage:   FR-27, FR-37, FR-28, FR-6
  Scope hint:        src/Cruma.Sync.Client/
  Depends on:        T-30, T-47
  Profile rules:     SYN-001, SYN-002, SYN-003, SYN-004, SYN-007, ERR-002
  Done-when:         Relace podle versioning-and-sync-pattern.md §5.1 se spouští spouštěči z §5.2 mimo UI vlákno;
                     offline změny se po připojení odešlou a změny z tenkého klienta se stáhnou bez akce uživatele
                     (FR-27 akc. 1, 2); stav synchronizováno / čeká / konflikt / chyba je publikován (FR-27 akc. 3);
                     lokální mezilehlé verze se smažou až po potvrzení (FR-6 akc. 4); pod minimální verzí se
                     synchronizace zastaví a změny zůstanou (FR-37 akc. 1, 2).
```

```text
T-51: WPF shell s BlazorWebView
  PLAN linkage:      C-11
  SPEC/FR linkage:   FR-7, FR-8, NFR-15
  Scope hint:        src/Cruma.Desktop/
  Depends on:        T-49
  Profile rules:     DEP-004, UI-001
  Done-when:         Desktop hostuje Cruma.Ui v BlazorWebView s Windows implementacemi platformních služeb a
                     popisem schopností podle desktop-pattern.md §1; editor funguje stejně jako v tenkém klientovi
                     (FR-7 akc. 8); režimy šířky editoru jsou dostupné (FR-8 akc. 1).
```

```text
T-52: Přihlášení desktopu
  PLAN linkage:      C-11, C-14
  SPEC/FR linkage:   FR-26, FR-32
  Scope hint:        src/Cruma.Desktop/
  Depends on:        T-20, T-22, T-51
  Profile rules:     SEC-002, SEC-003, PER-005
  Done-when:         Přihlášení přes systémový prohlížeč (FR-32 akc. 3) s obnovovacím tokenem chráněným DPAPI
                     (SEC-003); první spuštění vyžaduje přihlášení a připojení (FR-26 akc. 3); aplikace se spustí
                     a zobrazí data offline i s vypršelým přihlášením (FR-26 akc. 2); odhlášení funguje
                     (FR-32 akc. 5).
```

```text
T-53: Instalátor a aktualizace desktopu
  PLAN linkage:      C-11, N-6
  SPEC/FR linkage:   FR-37
  Scope hint:        src/Cruma.Desktop/
  Depends on:        T-51
  Done-when:         Desktop jde nainstalovat bez práv administrátora a nabídne a nainstaluje dostupnou
                     aktualizaci (FR-37 akc. 4); zdroj instalátoru a aktualizací je konfigurovatelná adresa feedu
                     na serveru Cruma (FR-37 akc. 3, I1-D-5).
```

```text
T-54: Integrační testy synchronizace
  PLAN linkage:      C-6, C-13
  SPEC/FR linkage:   FR-27, FR-28
  Scope hint:        tests/Cruma.Sync.Client.Tests/
  Depends on:        T-50
  Done-when:         Proti testovacímu serveru: přerušená synchronizace neztratí ani nezdvojí změnu (FR-27 akc. 4);
                     změny různých odstavců na desktopu a přes REST se sloučí (FR-28 akc. 1); změna téhož
                     odstavce vede ke konfliktu (FR-28 akc. 2); úprava proti smazání úpravu zachová (FR-28 akc. 4);
                     souběh dvou tenkých klientů se chová stejně (FR-28 akc. 5).
```

```text
T-55: Generátor výkonových dat a výkonový scénář
  PLAN linkage:      N-7
  SPEC/FR linkage:   S-I1-5, NFR-7
  Scope hint:        tests/
  Depends on:        T-48, T-50
  Done-when:         Nástroj vygeneruje 50 000 poznámek s českým textem pro jednoho uživatele; scénář změří
                     vyhledávání na serveru, na desktopu a první synchronizaci desktopu (plan.md N-7) a výsledky
                     operátor posoudí jako použitelné (S-I1-5).
```

---

## E-6 — Nasazení

**Vstupní podmínka:** T-56 (doména, VPS, úložiště záloh) je hotový.
**Výstup etapy (`plan.md` §4 E-6):** kritéria S-I1-3 a S-I1-4 splněna na produkci.

```text
T-56: Doména, server a úložiště záloh
  Owner:             operator
  PLAN linkage:      C-18 (E-6 vstup), O-3, O-5
  SPEC/FR linkage:   FR-36
  Scope hint:        deploy/
  Depends on:        E-5
  Profile rules:     SEC-006
  Done-when:         Doména s DNS záznamem ukazuje na Hetzner Cloud VPS (O-3); VPS splňuje kontrolní seznam
                     deployment-pattern.md §6; existuje úložiště záloh mimo server s přístupovými údaji jako tajnými
                     údaji mimo repozitář (O-5, SEC-006); redirect URI produkční domény jsou přidány do Google
                     OAuth klienta.
```

```text
T-57: Produkční definice kontejnerů
  PLAN linkage:      C-18
  SPEC/FR linkage:   NFR-8, NFR-9
  Scope hint:        deploy/compose.yaml
  Depends on:        T-56
  Profile rules:     OPS-001, OPS-004, SEC-006, SEC-008
  Done-when:         compose.yaml obsahuje služby proxy, server, migrate, db a backup se sítěmi a svazky podle
                     deployment-pattern.md §2; image mají pevné verze (OPS-004); veřejně jsou jen porty proxy
                     s TLS (SEC-008); tajné údaje z prostředí (SEC-006); běží v Podmanu i Dockeru (OPS-001).
```

```text
T-58: Automatické zálohy
  PLAN linkage:      C-18
  SPEC/FR linkage:   FR-36
  Scope hint:        deploy/
  Depends on:        T-57
  Profile rules:     OPS-002
  Done-when:         Zálohy databáze vznikají automaticky alespoň denně a před každým nasazením (FR-36 akc. 1);
                     jsou šifrované, uložené mimo server a uchovávají více verzí (FR-36 akc. 2).
```

```text
T-59: Skript nasazení
  PLAN linkage:      C-18
  SPEC/FR linkage:   FR-36
  Scope hint:        deploy/deploy.sh
  Depends on:        T-58
  Profile rules:     OPS-005, PER-003
  Done-when:         Skript provede kroky deployment-pattern.md §3: stažení image, záloha, migrace jako samostatný
                     krok, restart serveru, kontrola zdraví a návrat k předchozí verzi při selhání (OPS-005).
```

```text
T-60: CI release
  PLAN linkage:      N-8, N-6
  SPEC/FR linkage:   S-I1-2, FR-37
  Scope hint:        .github/workflows/
  Depends on:        T-7, T-53, T-57
  Profile rules:     OPS-004, SEC-006
  Done-when:         Na značku verze workflow release publikuje image serveru do GitHub Container Registry s pevnou
                     verzí a sestaví instalátor desktopu na Windows runneru (plan.md N-8); artefakty desktopu jsou
                     připravené k nahrání do feedu (N-6).
```

```text
T-61: Feed desktopu na serveru
  PLAN linkage:      N-6, C-13
  SPEC/FR linkage:   FR-37
  Scope hint:        src/Cruma.Server/, deploy/
  Depends on:        T-60
  Profile rules:     API-003
  Done-when:         Server vystavuje instalátor a aktualizační feed jako statické soubory na veřejné cestě bez
                     autentizace, uvedené v anonymním allowlistu (plan.md N-6, API-003); nainstalovaný desktop
                     z něj stáhne aktualizaci (FR-37 akc. 3, 4).
```

```text
T-62: Zkouška obnovy
  PLAN linkage:      C-18
  SPEC/FR linkage:   FR-36, S-I1-4
  Scope hint:        deploy/RESTORE.md
  Depends on:        T-59
  Profile rules:     OPS-003
  Done-when:         RESTORE.md popisuje obnovu podle deployment-pattern.md §5; obnova ze zálohy na jednorázovém
                     serveru proběhla a systém fungoval (FR-36 akc. 3); datum úspěšné zkoušky je v RESTORE.md
                     zapsáno (FR-36 akc. 4, S-I1-4).
```

```text
T-63: Ověření na produkci
  PLAN linkage:      E-6
  SPEC/FR linkage:   S-I1-3
  Scope hint:        deploy/
  Depends on:        T-61, T-62
  Done-when:         Na produkčním serveru: PWA nainstalovaná v telefonu vytvoří poznámku, která se objeví na
                     desktopu; současná úprava různých odstavců na obou stranách se sloučí; úprava téhož odstavce
                     vytvoří konflikt a jde vyřešit (S-I1-3).
```

---

## Otevřené položky

| ID | Mezera | Úkol |
|---|---|---|
| O-3 | Doména a DNS | T-56 (operátor) |
| O-4 | Google OAuth přihlašovací údaje | T-19 (operátor) |
| O-5 | Úložiště záloh | T-56 (operátor) |
