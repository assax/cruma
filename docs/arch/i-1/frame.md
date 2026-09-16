# frame.md — Cruma, inkrement I-1 (walking skeleton)

```text
Phase: FRAME          Run mode: CONCEPTUAL          Architecture basis: profile @ .architecture\architecture-cruma\PROFILE.md
Output language: czech
Open items: O-1, O-2, O-3, O-4, O-5          Carried assumptions: A-1, A-2
Last approved gate: SESSION_PREMISE @ 2026-09-16
```

Vstupy: `docs/arch/spec.md`, `docs/arch/plan.md`, `docs/arch/DECISIONS.md`, profil Cruma.
Tento běh nevytváří nové požadavky ani komponenty; pracuje s existujícími `FR-n`, `NFR-n` a `C-n`.

---

## 1. Cíl inkrementu

Po I-1 musí platit:

> Poznámky se základním formátováním jdou vytvářet, hledat a upravovat na desktopu i v telefonu,
> data se synchronizují přes vlastní server, souběžné změny se slučují bez ztráty a systém běží
> na Hetzneru se zálohami, jejichž obnova byla vyzkoušena.

Hodnota pro operátora: Google Keep lze přestat používat pro textové poznámky. Hodnota pro projekt:
nejrizikovější část architektury (slučování a synchronizace) je ověřená dřív, než na ní stojí velký kód
(`plan.md` R-2, D-9).

## 2. V rozsahu

### 2.1 Požadavky (podle `plan.md` §4)

| Oblast | Požadavky |
|---|---|
| Poznámky | FR-1 rychlé vytvoření, FR-2 vlastnosti, FR-3 kategorie, FR-4 štítky, FR-5 archiv a koš |
| Verze | FR-6 **pouze mechanismus** — vznik verzí, základní verze pro merge; zobrazení a porovnání historie je I-2 |
| Editor | FR-7 **podmnožina** — nadpisy, tučné, kurzíva, podtržení, přeškrtnutí, seznamy, číslované seznamy, checklist, odkazy, zvýraznění; FR-8 šířka editoru |
| Vyhledávání | FR-24 jednotné vyhledávání |
| Klienti a sync | FR-26 desktop offline, FR-27 automatická synchronizace, FR-28 slučování a konflikty, FR-29 tenký klient, FR-30 fronta zápisů |
| Identita | FR-31 oddělení uživatelů, FR-32 přihlášení Google |
| UI | FR-34 světlý a tmavý režim |
| Provoz | FR-35 audit, FR-36 backup a obnova, FR-37 kompatibilita verzí klienta |

Nefunkční požadavky platné pro I-1: NFR-1 až NFR-10, NFR-13 (registr typů bloků), NFR-14, NFR-15 (platformní
služby za rozhraním), NFR-16, NFR-17, NFR-18, NFR-19.

### 2.2 Komponenty (podle `plan.md` §2)

C-1, C-3, C-4, C-5, C-6, C-8, C-9, C-10, C-11, C-12, C-13, C-14, C-17, C-18. `C-2` vzniká jako prázdný projekt
kvůli referencím.

_Opraveno 2026-09-16 při schválení ANALYSIS: původní text zařazoval C-7 (bloby); `plan.md` §5 ji do I-1
nezařazuje a v I-1 žádný dokument na blob neodkazuje (analysis §1)._

### 2.3 Průřezové výstupy

* Kostra repozitáře a řešení podle `solution-structure-template.md`.
* Sada testů podle `testing-strategy.md`: scénáře slučování, konformní testy vyhledávání, testy izolace
  uživatelů.
* Nasazení na Hetzner Cloud podle `deployment-pattern.md` včetně záloh a **provedené zkoušky obnovy**
  (OPS-003).

## 3. Mimo rozsah

| Mimo rozsah | Kam patří |
|---|---|
| Tabulky, barva textu, pozadí textu, obrázky v dokumentu, code blocky, diagramy | I-2 |
| Zobrazení a porovnání historie verzí, export, připomínky a notifikace | I-2 |
| AI v jakékoliv podobě | I-3 |
| Projects / Kanban | I-4 |
| Nativní mobilní shell (MAUI) | I-5 |
| PlantUML offline (OP-1), formátování kódu (OP-2) | I-2 |
| Změna schválené architektury | zpětný přechod do systémového `plan.md`, se schválením operátora |

Mimo rozsah enginu zůstává implementace samotná; `tasks.md` je předávaný artefakt pro `.aidevkit`.

## 4. Rizika

| ID | Riziko | Dopad | Zmírnění v tomto inkrementu |
|---|---|---|---|
| R-1 | **Velikost I-1.** Je to největší inkrement a obsahuje jak nejtěžší logiku, tak první nasazení. | Dlouhá doba do prvního použitelného stavu. | Vnitřní pořadí podle `plan.md` §5: sdílená logika s testy → server → tenký klient → desktop → nasazení. Po kroku 4 je systém použitelný v prohlížeči. |
| R-2 | **Slučování.** Merge bez tiché ztráty je nejtěžší část celého systému. | Ztráta dat uživatele. | `Cruma.Versioning` jako čistá knihovna s deseti scénáři z `testing-strategy.md` §3 dřív, než se napojí síť. |
| R-3 | **Přihlášení Google z desktopu** (loopback redirect, systémový prohlížeč). | Blokuje synchronizaci i test izolace uživatelů. | Ověřit prototypem hned na začátku serverového kroku (`plan.md` R-10, SEC-002). |
| R-4 | **Testcontainers nad Podmanem.** Rootless Podman vyžaduje nastavení socketu a bývá potřeba vypnout resource reaper. | Zablokované integrační testy hned na začátku. | Ověřit jedním triviálním testem v prvním kroku, před psaním testů proti databázi. |
| R-5 | **Sdílení JS balíku editoru** mezi BlazorWebView a WebAssembly. | Dvě různé cesty načítání editoru, duplicitní kód. | Jeden balík jako static web asset v `Cruma.Ui.Editor` (CNT-006); ověřit v obou shellech ve stejném kroku. |
| R-6 | **První nasazení a obnova.** Zálohy, TLS, migrace a obnova se dělají poprvé. | Riziko provozu bez ověřené obnovy, tedy přesně to, co D-6.1 zakazuje. | Zkouška obnovy na jednorázovém serveru je součástí definice hotového I-1 (OPS-003). |
| R-7 | **Stabilní ID bloků v editoru.** Musí přežít úpravy, rozdělení a spojení bloků. | Bez nich nefunguje merge ani porovnání verzí. | Vlastní rozšíření editoru (CNT-002, CNT-006) s testy na rozdělení a spojení bloku. |

## 5. Otevřené neznámé

| ID | Mezera | Dopad | Podmínka vyřešení |
|---|---|---|---|
| O-1 | Umístění a název repozitáře (dnes `c:\MyWork\_project\google-keep-client`, doporučeno `cruma`) a založení gitu. | Scope hinty v TASKS jsou relativní k němu; první úkol repozitář zakládá. | Rozhodnutí operátora před TASKS. |
| O-2 | Struktura `tasks.md`: jedna souvislá řada úkolů, nebo šest pojmenovaných etap podle `plan.md` §5. | Ovlivňuje čitelnost a dávkování v devkitu. | Rozhodnutí operátora na konci PLAN. |
| O-3 | **Doména a DNS** pro server (automatické TLS certifikáty je vyžadují). | Blokuje nasazení a tím dokončení I-1; blokuje i přihlášení Google (registrované redirect URI). | Akce operátora před krokem nasazení. |
| O-4 | **Přihlašovací údaje Google OAuth** (projekt v Google Cloud, client ID a secret, redirect URI pro server i desktop). | Blokuje FR-32 a tím synchronizaci i tenkého klienta. | Akce operátora před serverovým krokem. |
| O-5 | **Cílové úložiště záloh** mimo server (objektové úložiště, přístupové údaje). | Blokuje FR-36 a OPS-002. | Akce operátora před nasazením. |

O-3 až O-5 nejsou rozhodnutí, ale externí závislosti. V TASKS budou vedené jako úkoly s vlastníkem „operátor“,
aby nebyly objevené až ve chvíli, kdy na ně narazí implementace.

## 6. Doporučený další směr

Pokračovat fází ANALYSIS. V režimu `CONCEPTUAL` s basis `PROFILE` se analýza zaměří na:

1. **načtení závazných pravidel profilu** (`architecture-rules.md`) a jejich dopad na rozsah I-1 — která
   pravidla vytvářejí práci, na kterou `plan.md` výslovně neukazuje (např. globální filtr uživatele,
   konformní testy vyhledávání, verzování protokolu);
2. **kontrolu úplnosti** — zda požadavky přiřazené I-1 pokrývají cíl z §1 beze zbytku;
3. **rozhodovací body**, které musí PLAN tohoto běhu uzavřít, aby TASKS měly done-when bez domýšlení;
4. **podklad pro etapy** uvnitř I-1 (O-2).

Analýza nebude navrhovat novou architekturu ani měnit schválené rozhodnutí; to by vyžadovalo zpětný přechod
do systémového plánu.
