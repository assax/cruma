# plan.md — Cruma, inkrement I-1 (walking skeleton)

```text
Phase: PLAN          Run mode: CONCEPTUAL          Architecture basis: profile @ .architecture\architecture-cruma\PROFILE.md
Output language: czech
Open items: O-3, O-4, O-5          Carried assumptions: A-1, A-2, A-3, A-4
Last approved gate: PLAN->TASKS @ 2026-09-16 (O-2 = etapy E-1..E-6)
```

Vstupy: `i-1/spec.md`, `i-1/DECISIONS.md`, systémový `docs/arch/plan.md`, profil Cruma.

**Závazné:** §1–§5 a §7. **Informativní:** §6 a §8.

Tento plán nepřebírá obsah profilu. Kde profil určuje *jak* (struktura projektů, patterny, pravidla), plán na něj
odkazuje. Plán doplňuje jen to, co je specifické pro I-1: rozsah komponent, návrhové prvky, které profil
nepokrývá, a pořadí.

---

## 1. Rámec

* Architektura: systémový `docs/arch/plan.md` §1 a `docs/arch/DECISIONS.md` D-1..D-9.
* Struktura projektů a povolené reference: `solution-structure-template.md` §2, §3, §5 (řádek I-1).
* Závazná pravidla: `architecture-rules.md` kromě BLB-001..BLB-004, SYN-005, AI-001..AI-005, SEC-007
  (`i-1/spec.md` §5).
* Repozitář: `c:\MyWork\_project\cruma` (A-3), GitHub, soukromý, CI na GitHub Actions (A-4).

## 2. Komponenty v I-1

| C-n | Rozsah v I-1 | Projekty | Pattern profilu | Požadavky |
|---|---|---|---|---|
| C-1 | poznámka, kategorie, štítek, stav, pravidlo výchozí kategorie | `Cruma.Notes` | — | FR-1..FR-5 |
| C-2 | prázdný projekt kvůli referencím | `Cruma.Kanban` | — | — |
| C-3 | schéma v1 (bloky a značky z FR-7), ID bloků, registr typů, rámec migrací, extrakce textu | `Cruma.Content`, `Cruma.Ui.Editor` (rozšíření ID bloků) | content-document | FR-7, NFR-13 |
| C-4 | model verzí, merge dokumentu a metadat, model konfliktu | `Cruma.Versioning` | versioning-and-sync §2–§3 | FR-6, FR-28, NFR-4 |
| C-5 | normalizátor, tokenizátor, dotaz, rozhraní adaptéru; adaptéry SQLite a PostgreSQL | `Cruma.Search`, `Cruma.Desktop.Storage`, `Cruma.Server/Search` | search | FR-24, NFR-5 |
| C-6 | kontrakty protokolu, handshake, sync engine desktopu | `Cruma.Sync`, `Cruma.Sync.Client` | versioning-and-sync §5 | FR-27, FR-37 |
| C-8 | lokální databáze, log čekajících změn, lokální verze, kurzor | `Cruma.Desktop.Storage` | desktop §2 | FR-26 |
| C-9 | stránky Notes, vyhledávání, konflikty, nastavení, motivy; rozhraní služeb; popis schopností | `Cruma.Ui`, `Cruma.Api.Client` | ui | FR-1..FR-5, FR-8, FR-28, FR-34 |
| C-10 | editor s formátováním FR-7 a rozšířením ID bloků | `Cruma.Ui.Editor` | content-document §3 | FR-7 |
| C-11 | WPF shell, přihlášení, DPAPI token, NLog, instalátor a aktualizace | `Cruma.Desktop` | desktop §1, §6 | FR-26, FR-32, FR-37 |
| C-12 | WASM PWA shell, cookie session, fronta zápisů, offline stav | `Cruma.Web` | ui §6 | FR-29, FR-30 |
| C-13 | infrastruktura, moduly Notes, Search, Sync; změnový feed; feed desktopu | `Cruma.Server` | server | FR-24, FR-27..FR-29, FR-31, FR-37 |
| C-14 | OpenIddict, Google login, cookie i PKCE, vazba identit, založení uživatele | `Cruma.Server/Identity` | server §4 | FR-31, FR-32 |
| C-17 | auditní úložiště a události I-1 | `Cruma.Server/Audit` | logging-and-audit | FR-35 |
| C-18 | compose (vývoj i produkce), proxy, deploy skript, zálohy, obnova, CI | `deploy/`, `.github/workflows/` | deployment | FR-36 |

## 3. Návrhové prvky specifické pro I-1

Profil tyto prvky neurčuje nebo je určuje jen obecně. Plán je fixuje, aby TASKS měly jednoznačné done-when.

### N-1 — Jedna zápisová cesta pro poznámky

Všechny zápisy poznámek — REST z tenkého klienta i změny ze sync protokolu — procházejí **jednou aplikační
službou** modulu Notes, která: ověří vlastnictví, načte základní a aktuální verzi, provede merge přes
`Cruma.Versioning`, uloží verzi, aktualizuje index, zapíše audit a zapíše záznam do změnového feedu (N-2).
REST endpoint i Sync modul jsou jen vstupy do této služby.

*Proč:* I1-D-1 vyžaduje stejné chování pro oba klienty; dvě implementace by se rozešly.
**Požadavky:** FR-28 akc. 1–5, FR-29 akc. 2. **Pravidla:** VER-002, DEP-007, API-002.

### N-2 — Změnový feed uživatele

Server vede pro každého uživatele monotónně rostoucí pořadové číslo změny. Každý zápis (N-1) i změna kategorie
nebo štítku zapíše záznam `(pořadí, typ entity, id entity, verze)`. Desktop si pamatuje poslední zpracované
pořadí jako kurzor a pull vrací záznamy s vyšším pořadím.

*Proč:* Pull podle kurzoru je deterministický a nevyžaduje porovnávání časů mezi zařízeními.
**Požadavky:** FR-27 akc. 2. **Pravidla:** SYN-004.

### N-3 — Spojování uložení do verzí na serveru

Podle I1-D-2: uložení od téhož uživatele, téhož klienta (identifikátor instance klienta posílaný s požadavkem) a
téže poznámky, přijaté do intervalu nečinnosti od předchozího uložení, **aktualizují poslední verzi** místo
vytvoření nové, pokud mezi nimi nepřišla změna z jiného zdroje. Interval nečinnosti je konfigurační hodnota
s výchozí hodnotou **5 minut**. Stejný interval používá desktop pro ukončení lokální editační relace.

**Požadavky:** FR-6 akc. 2–3. **Pravidla:** VER-006.

### N-4 — Založení uživatele

Při prvním úspěšném externím přihlášení server v jedné transakci: založí uživatele s interním identifikátorem,
naváže externí identitu, vytvoří výchozí kategorii a zapíše audit `identity.provider_linked`.

**Požadavky:** FR-3 akc. 5, FR-32. **Pravidla:** SEC-001.

### N-5 — Architektonický test referencí

Test čte reference všech projektů v `src/` a porovná je se seznamem povolených referencí převzatým ze
`solution-structure-template.md` §3; nepovolená reference test shodí s názvem obou projektů.

**Požadavky:** S-I1-2. **Pravidla:** DEP-001..DEP-004, STR-001. **Rozhodnutí:** I1-D-4.

### N-6 — Minimální verze klienta a feed desktopu

* Minimální podporovaná verze desktopu je konfigurační hodnota serveru; handshake ji porovnává s verzí klienta
  (SYN-001).
* Instalátor a aktualizační feed jsou statické soubory, které server vystavuje pod veřejnou cestou bez
  autentizace (výjimka v anonymním allowlistu, API-003). Nahrávají se při vydání desktopu.

**Požadavky:** FR-37. **Rozhodnutí:** I1-D-5.

### N-7 — Generátor výkonových dat

Nástroj v testech vygeneruje 50 000 poznámek s realistickým českým textem pro jednoho uživatele. Výkonový
scénář měří vyhledávání na serveru, na desktopu a první synchronizaci desktopu.

**Požadavky:** S-I1-5, NFR-7.

### N-8 — CI

* Workflow **build**: při každém push a pull requestu sestaví řešení a spustí všechny testy včetně
  Testcontainers na Linux runneru; editorový JS balík se sestaví jako součást buildu.
* Workflow **release**: na značku verze sestaví a publikuje image serveru do GitHub Container Registry s pevnou
  verzí (OPS-004) a sestaví instalátor desktopu na Windows runneru.
* Tajné údaje CI jsou v GitHub Secrets (SEC-006).

**Požadavky:** S-I1-2. **Předpoklad:** A-4.

## 4. Pořadí implementace — etapy

Každá etapa má vstupní podmínku a výstup ověřitelný na jejím konci. Etapa nezačne, dokud předchozí nesplní
výstup. Úkoly operátora (O-3..O-5) jsou zařazeny tam, kde poprvé blokují.

### E-1 — Základ

**Vstup:** —
**Obsah:** přejmenování složky a založení git repozitáře na GitHubu (A-3); solution a props (STR-002, STR-004);
kostry všech projektů I-1 podle šablony; `compose.yaml` s profilem pro vývoj (OPS-001); NLog ve třech kořenech
(LOG-003, LOG-004); architektonický test (N-5); ověřovací test Testcontainers nad Podmanem (frame R-4); CI build
(N-8).
**Výstup:** CI build prochází; architektonický test prochází; test proti PostgreSQL v kontejneru prochází lokálně
i v CI.

### E-2 — Sdílená logika

**Vstup:** E-1.
**Obsah:** `Cruma.Content` v1 (C-3); `Cruma.Notes` (C-1); `Cruma.Versioning` s deseti scénáři
(`testing-strategy.md` §3); `Cruma.Search` normalizátor a tokenizátor (C-5); `Cruma.Sync` kontrakty a handshake
model (C-6).
**Výstup:** všechny unit testy TST-001 procházejí, včetně FR-28 akc. 1–4 jako scénářů merge.

### E-3 — Server

**Vstup:** E-2; **operátor:** Google OAuth přihlašovací údaje (O-4).
**Obsah:** infrastruktura (DbContext, globální filtr uživatele, ProblemDetails, aktuální uživatel, correlation id);
identita a založení uživatele (C-14, N-4) — **přihlášení z desktopu ověřit prototypem jako první úkol etapy**
(frame R-3); modul Notes se zápisovou cestou (N-1), verzemi (N-3) a změnovým feedem (N-2); modul Search
s adaptérem PostgreSQL a konformní sadou; modul Sync s handshake a minimální verzí (N-6); audit (C-17);
testy izolace uživatelů (TST-004).
**Výstup:** integrační testy procházejí; lokální přihlášení Googlem funguje pro web i pro prototyp desktopu.

### E-4 — Sdílené UI a tenký klient

**Vstup:** E-3.
**Obsah:** `Cruma.Ui` s rozhraními služeb a popisem schopností (C-9); editor s rozšířením ID bloků (C-10);
`Cruma.Api.Client`; `Cruma.Web` s cookie session, frontou zápisů a offline stavem (C-12); rychlé zachycení
(UI-005); řešení konfliktů; motivy (FR-34).
**Výstup:** v prohlížeči proti lokálnímu serveru jde splnit akceptační kritéria FR-1..FR-5, FR-7, FR-8, FR-24,
FR-28 akc. 3, FR-29, FR-30, FR-34.

### E-5 — Desktop

**Vstup:** E-4.
**Obsah:** `Cruma.Desktop.Storage` s adaptérem SQLite zapojeným do konformní sady (C-8, TST-002);
`Cruma.Sync.Client` (C-6); WPF shell, přihlášení a DPAPI (C-11); instalátor a kontrola aktualizací (N-6).
**Výstup:** desktop pracuje offline a synchronizuje s lokálním serverem; souběžná úprava s prohlížečem vede ke
sloučení nebo konfliktu podle FR-28; konformní sada prochází nad oběma adaptéry; výkonový scénář N-7 prochází.

### E-6 — Nasazení

**Vstup:** E-5; **operátor:** doména a DNS (O-3), úložiště záloh (O-5), Hetzner Cloud VPS.
**Obsah:** produkční `compose.yaml` s proxy, migrací a zálohami (C-18); deploy skript (OPS-005); CI release
(N-8); feed desktopu na serveru (N-6); zkouška obnovy na jednorázovém serveru a `RESTORE.md` (OPS-003).
**Výstup:** kritéria S-I1-3 a S-I1-4 splněna na produkci.

```text
E-1 ─► E-2 ─► E-3 ─► E-4 ─► E-5 ─► E-6
               ▲                    ▲
            O-4 Google OAuth     O-3 doména, O-5 zálohy, VPS
```

## 5. Přiřazení požadavků

| Požadavek | Komponenty | Návrhový prvek | Etapa (implementace → ověření) |
|---|---|---|---|
| FR-1, FR-2, FR-4, FR-5 | C-1, C-9, C-13 | N-1 | E-2 → E-4 |
| FR-3 | C-1, C-9, C-13, C-14 | N-4 | E-2 → E-4 |
| FR-6 | C-4, C-8, C-13 | N-3 | E-2 → E-5 |
| FR-7 | C-3, C-10 | — | E-2 → E-4 |
| FR-8 | C-9 | — | E-4 → E-5 (šířky na desktopu) |
| FR-24 | C-5, C-8, C-13 | N-7 | E-2 → E-5 |
| FR-26 | C-8, C-11 | — | E-5 |
| FR-27 | C-6, C-8, C-13 | N-2 | E-3 → E-5 |
| FR-28 | C-4, C-9, C-13 | N-1 | E-2 → E-5 |
| FR-29, FR-30 | C-12, C-13 | N-1 | E-4 → E-6 (instalace PWA do telefonu) |
| FR-31 | C-13 | — | E-3 |
| FR-32 | C-11, C-12, C-14 | N-4 | E-3 → E-5 |
| FR-34 | C-9 | — | E-4 |
| FR-35 | C-17 | — | E-3 |
| FR-36 | C-18 | — | E-6 |
| FR-37 | C-6, C-11, C-13 | N-6 | E-3 → E-6 |
| S-I1-2 | testy, C-18 | N-5, N-8 | E-1 → E-6 |
| S-I1-5 | C-5, C-6, C-8 | N-7 | E-5 |

## 6. Rizika a zmírnění (informativní)

| ID | Riziko | Zmírnění v plánu |
|---|---|---|
| R-1 | Velikost I-1 | Šest etap s ověřitelným výstupem; po E-4 je systém použitelný v prohlížeči. |
| R-2 | Slučování | Merge v E-2 jako čistá knihovna se scénáři, dřív než síť; jedna zápisová cesta (N-1). |
| R-3 | Přihlášení z desktopu | Prototyp jako první úkol E-3, ne až v E-5. |
| R-4 | Testcontainers nad Podmanem | Ověřovací test v E-1 lokálně i v CI. |
| R-5 | Sdílení editoru mezi WebView a WASM | Editor vzniká v E-4 pro web; E-5 ho jen hostí. Selhání v E-5 se projeví jako chyba hostování, ne editoru. |
| R-6 | První nasazení a obnova | Obnova je výstupní podmínkou E-6, ne následný úkol. |
| R-7 | Stabilní ID bloků | FR-7 akc. 5 jako testy rozdělení a spojení bloků v E-4. |
| R-8 | Výkon při 50 000 poznámkách | Generátor N-7 a výkonový scénář ve výstupu E-5. |
| R-9 | **Nové.** Externí závislosti operátora zablokují rozpracovanou etapu. | O-4 je vstupní podmínkou E-3, O-3 a O-5 vstupní podmínkou E-6; TASKS je vedou jako samostatné úkoly operátora. |

## 7. Soulad s profilem

Žádná odchylka od závazných pravidel. Pravidla neaplikovaná v I-1 jsou vyjmenovaná v §1; aplikují se
v inkrementu, který zavede dotčenou funkci.

## 8. Zamítnuté varianty (informativní)

Nikdy neimplementovat.

| Rozhodnutí | `REJECTED` |
|---|---|
| I1-D-1 | Odmítnutí souběžné úpravy z tenkého klienta s `version_conflict` |
| I1-D-2 | Verze za každé uložení z tenkého klienta; spoléhání na signál konce relace z prohlížeče |
| I1-D-3 | Používání desktopu před prvním přihlášením |
| I1-D-4 | Kontrola referencí pouze při review |
| I1-D-5 | Samostatná služba pro distribuci desktopu |
| N-1 | Samostatná zápisová logika pro REST a pro synchronizaci |
| N-2 | Pull podle časových značek |
