# plan.md — Personal Notes & Workflow

```text
Phase: PLAN          Run mode: CONCEPTUAL          Architecture basis: greenfield
Output language: czech
Open items: none          Carried assumptions: A-1, A-2, A-3, A-4, A-5
Last approved gate: PLAN->CLOSED @ 2026-09-15 22:55
```

Vstupy: `spec.md` (dále „SPEC“), `DECISIONS.md` (D-1 až D-9), `analysis.md`, `frame.md`.

Tento dokument říká, **jak je systém strukturovaný a v jakém pořadí se staví**. Neurčuje pojmenování, tvar
polí, strukturu složek ani styl kódu — ty patří do architecture profilu projektu a do implementace.

**Závazné vs. informativní:** závazné jsou sekce 1–5 a 7. Sekce 6 (rizika) a 8 (zamítnuté varianty) jsou
informativní. Zamítnuté varianty jsou uvedeny jen proto, aby se neimplementovaly.

---

## 1. Architektonický rámec

| Oblast | Rozhodnutí | Zdroj |
|---|---|---|
| Platforma | .NET 10 (LTS) na klientu i serveru | D-1, upřesnění operátora 2026-09-15 |
| Server | ASP.NET Core, verzované REST API | D-1, NFR-18 |
| UI | Sdílená Razor Class Library pro všechny shelly | D-1, NFR-15, NFR-19 |
| Desktop | WPF + BlazorWebView, lokální SQLite, plnohodnotný offline klient | D-1, frame 2.1 |
| Web, tablet | Blazor WebAssembly jako PWA, tenký klient | D-1, frame 2.1 |
| Mobil | PWA, později MAUI Blazor Hybrid (Android dřív než iOS) | D-1, D-9 |
| Editor | TipTap (ProseMirror) jako JS ostrov | D-1, D-2 |
| Obsah | JSON strom podle vlastního verzovaného schématu, bloky se stabilním ID | D-2 |
| Verzování | Verze za editační relaci; offline práce desktopu = jedna verze na serveru | D-3.1, D-3.2 |
| Slučování | Tříbodové nad bloky; souběžná změna bloku = konflikt; množiny metadat se slučují, skalární pole vyhrává poslední zápis se záznamem v historii | D-3.3, D-3.4 |
| Vyhledávání | Sdílená normalizace a tokenizace v C#; databáze indexují hotové tokeny | D-4 |
| Binární obsah | Neměnné bloby adresované obsahem mimo databázi; desktop drží všechny | D-5 |
| Serverová databáze | PostgreSQL | D-6.2 |
| Provoz | Hetzner Cloud VPS, OCI kontejnery, automatické zálohy mimo server | D-6.1 |
| Šifrování | Úložiště, zálohy, TLS; aplikačně jen tajné údaje | D-6.3, NFR-9 |
| Identita | Server vydává vlastní tokeny, Google jako externí přihlášení | D-6.4 |
| Připomínky | Desktop plánuje lokálně, tenký klient a mobil přes push; jeden zdroj na zařízení | D-7 |
| AI | Lokální provideři z desktopu, cloudoví přes server; kontext napříč poznámkami přes fulltext; odeslání do cloudu jen na explicitní akci | D-8 |
| Pořadí | Walking skeleton napříč, pět inkrementů | D-9 |

## 2. Komponenty

Každá komponenta uvádí odpovědnost, hranici (co nesmí dělat) a požadavky, které pokrývá.

### 2.1 Sdílené knihovny (bez závislosti na UI, úložišti a platformě)

#### C-1 — Doména Notes
**Odpovědnost:** poznámky, kategorie, štítky, stav (aktivní / archiv / koš), barva, připnutí, pravidla
výchozí kategorie.
**Hranice:** nezná úložiště, síť ani UI. Nezná doménu Kanban (NFR-16).
**Požadavky:** FR-1, FR-2, FR-3, FR-4, FR-5 · A-3

#### C-2 — Doména Kanban
**Odpovědnost:** projekty, boardy, sloupce, karty, historie změn karty, odkaz karty na poznámku.
**Hranice:** nezná úložiště, síť ani UI. Na poznámku odkazuje pouze identifikátorem; nepřebírá doménový model
Notes (NFR-16, FR-23 akc. 3).
**Požadavky:** FR-17, FR-18, FR-19, FR-20, FR-21, FR-23 · A-5

#### C-3 — Obsah
**Odpovědnost:** schéma dokumentu a jeho verze, stabilní identifikátory bloků, migrace dokumentu mezi verzemi
schématu, extrakce prostého textu pro vyhledávání a AI, export do bezeztrátového formátu, HTML a Markdownu,
registr typů bloků (code, diagram) rozšiřitelný bez změny uložených dat.
**Hranice:** jediný vlastník schématu. Editor (C-10) i všechny ostatní komponenty pracují s dokumentem pouze
podle schématu z C-3. Obrázky obsahuje jen jako odkaz na blob.
**Požadavky:** FR-7, FR-9, FR-10, FR-11, FR-12, NFR-13, NFR-20

#### C-4 — Verzování a slučování
**Odpovědnost:** vznik verze za editační relaci, tříbodové slučování dokumentu nad bloky, slučování metadat
podle D-3.4, model konfliktu (obě varianty bloku, stav vyřešení), porovnání dvou verzí po blocích.
**Hranice:** čistá logika bez úložiště. Autoritativní slučování provádí server (C-13); desktop logiku používá
jen k zobrazení porovnání a konfliktu. Tenký klient neslučuje.
**Požadavky:** FR-6, FR-19 (historie), FR-28, NFR-4

#### C-5 — Vyhledávání
**Odpovědnost:** normalizace textu (Unicode, velikost písmen, diakritika), tokenizace, zpracování dotazu
včetně hledání podle začátku slova; rozhraní indexu s adaptérem pro SQLite (desktop) a PostgreSQL (server).
**Hranice:** normalizace a tokenizace existuje pouze zde. Adaptéry indexu nesmějí text normalizovat po
svém.
**Požadavky:** FR-24, NFR-5, NFR-6

#### C-6 — Synchronizace
**Odpovědnost:** verzovaný synchronizační protokol (kontrakty), kontrola kompatibility verze klienta,
desktopový synchronizační klient: sledování lokálních změn, odeslání se základní verzí, příjem změn ze serveru,
stav synchronizace, synchronizace blobů.
**Hranice:** protokol je jediné rozhraní mezi desktopem a serverem pro změny dat. Neslučuje (to dělá server
přes C-4). Tenký klient protokol nepoužívá, pracuje přímo s REST API.
**Požadavky:** FR-26, FR-27, FR-37, NFR-18

#### C-15a — AI abstrakce
**Odpovědnost:** rozhraní providerů a jejich adaptéry (OpenAI, Anthropic, lokální provider kompatibilní
s LM Studio), definice AI operací, sestavení kontextu napříč poznámkami z výsledků C-5, popis rozsahu
odesílaných dat pro zobrazení uživateli.
**Hranice:** nezná API klíče; dostává je od volající strany. Nerozhoduje, zda smí obsah odejít — to vynucuje
C-15b a UI.
**Požadavky:** FR-13, FR-14, FR-15, FR-16, FR-22, FR-25, NFR-11, NFR-12

### 2.2 Uživatelské rozhraní

#### C-9 — Sdílené UI
**Odpovědnost:** Razor komponenty pro Notes, Kanban, vyhledávání, AI, historii verzí, konflikty, nastavení,
světlý a tmavý režim; rozhraní platformních služeb (úložiště, notifikace, sdílení, výběr souboru, schránka,
stav připojení).
**Hranice:** komponenty nevolají přímo API prohlížeče ani Windows; vše platformní jde přes rozhraní
implementovaná shellem. Stejné pojmy a navigace na všech klientech; rozdíly jen v rozsahu funkcí.
**Požadavky:** FR-8, FR-21, FR-34, NFR-1, NFR-2, NFR-15, NFR-17, NFR-19

#### C-10 — Editor
**Odpovědnost:** integrace TipTap, formátování z FR-7, uzly pro code blocky (zvýraznění, rozpoznání jazyka,
formátování, kopírování) a diagramy (Mermaid, PlantUML, přepínání zdroj / náhled), vkládání obrázků.
**Hranice:** rozhraní vůči C# je úzké: editor přijímá a vrací dokument podle schématu C-3 a hlásí změny.
C# nemanipuluje vnitřním stavem editoru. Stejný balík běží v BlazorWebView i ve WebAssembly.
**Požadavky:** FR-7, FR-9, FR-10, FR-11

### 2.3 Desktop

#### C-8 — Lokální úložiště desktopu
**Odpovědnost:** perzistence domény Notes a Kanban v SQLite, lokální mezilehlé verze do synchronizace,
lokální index vyhledávání (adaptér C-5), záznam změn pro C-6.
**Hranice:** jediný přístup k lokální databázi. Nešifruje (NFR-9).
**Požadavky:** FR-26, NFR-3, NFR-7

#### C-11 — Desktopový shell
**Odpovědnost:** WPF hostitel BlazorWebView, implementace platformních služeb C-9 pro Windows, přihlášení
přes systémový prohlížeč a uložení obnovovacího tokenu chráněného operačním systémem, lokální plánovač
připomínek a systémové notifikace, kontrola a instalace aktualizací.
**Hranice:** k serverovému push se nepřihlašuje (D-7). Spuštění aplikace nevyžaduje platný token (FR-26 akc. 2).
**Požadavky:** FR-26, FR-32, FR-33, FR-37, NFR-3

#### C-7a — Lokální úložiště blobů
**Odpovědnost:** uložení blobů do souborů v adresáři aplikace, průběžné stažení všech blobů uživatele.
**Hranice:** blob se nemění; nahrazení obrázku je nový blob.
**Požadavky:** FR-9 akc. 4, D-5.2

### 2.4 Tenký klient

#### C-12 — Webový shell (PWA)
**Odpovědnost:** Blazor WebAssembly hostitel, implementace platformních služeb C-9 pro prohlížeč, fronta
zápisů nových poznámek s perzistencí v prohlížeči, přihlášení k web push.
**Hranice:** nemá databázi. Fronta obsahuje pouze nové poznámky (A-4) a neslučuje. Nevolá cloudové AI
providery přímo.
**Požadavky:** FR-29, FR-30, FR-33, NFR-17 · A-4

#### C-19 — Mobilní shell (MAUI Blazor Hybrid)
**Odpovědnost:** nativní hostitel sdíleného UI na Androidu (později iOS), nativní push a sdílení do aplikace.
**Hranice:** tenký klient se stejnými pravidly jako C-12; nemá databázi.
**Požadavky:** NFR-15

### 2.5 Server

#### C-13 — Serverové API
**Odpovědnost:** verzované REST API pro tenkého klienta, koncový bod synchronizace pro desktop, autorizace
na každém požadavku, izolace dat uživatelů, autoritativní slučování přes C-4, perzistence v PostgreSQL,
serverový index (adaptér C-5), kontrola minimální verze klienta.
**Hranice:** jediný zapisovatel do serverové databáze. Každý dotaz je omezen na přihlášeného uživatele.
**Požadavky:** FR-27, FR-28, FR-29, FR-31, FR-37, NFR-8, NFR-18

#### C-14 — Identita
**Odpovědnost:** vydávání a obnova tokenů aplikace, externí přihlášení Google, vazba externích identit na
interní identifikátor uživatele.
**Hranice:** žádná jiná komponenta nezná identitu Google; pracují s interním identifikátorem.
**Požadavky:** FR-31, FR-32, NFR-14

#### C-7b — Serverové úložiště blobů
**Odpovědnost:** uložení a výdej blobů za rozhraním (souborový svazek na VPS; výměnné za objektové
úložiště), úklid blobů bez odkazu z jakékoliv verze.
**Hranice:** blob se nemění. Přístup k blobu je autorizován stejně jako k poznámce.
**Požadavky:** FR-9, FR-12, NFR-7

#### C-15b — AI gateway
**Odpovědnost:** volání cloudových providerů za uživatele, uložení API klíčů uživatelů šifrovaně (D-6.3),
auditní záznam o odeslání obsahu.
**Hranice:** odešle obsah pouze na explicitní požadavek klienta s uvedeným rozsahem (D-8.3). Nepouští
automatické operace s cloudovým providerem.
**Požadavky:** FR-13 až FR-16, FR-22, FR-25, FR-35, NFR-8, NFR-12

#### C-16 — Serverové připomínky
**Odpovědnost:** plánování připomínek pro tenké klienty a mobil, odeslání push notifikace.
**Hranice:** neposílá push na desktop (D-7).
**Požadavky:** FR-33

#### C-17 — Audit
**Odpovědnost:** strukturované auditní záznamy operací z FR-35, dohledání podle uživatele, času a typu.
**Hranice:** oddělené od technického logování. Offline operace desktopu se auditují v okamžiku synchronizace.
**Požadavky:** FR-35, NFR-10

### 2.6 Provoz

#### C-18 — Provoz a nasazení
**Odpovědnost:** definice kontejnerů (serverová aplikace, PostgreSQL, reverzní proxy s automatickými TLS
certifikáty) spustitelná v Podmanu i Dockeru; tajné údaje předávané z prostředí; automatické zálohy databáze
a blobů mimo server; zdokumentovaný a vyzkoušený postup obnovy; stejná databáze v kontejneru pro lokální vývoj
a testy.
**Hranice:** nezávislé na konkrétním nástroji pro kontejnery a na poskytovateli; přesun na jiný hosting nesmí
vyžadovat změnu aplikace (D-6.1).
**Požadavky:** FR-36, NFR-8, NFR-9

## 3. Závislosti mezi komponentami

```text
Sdílené jádro        C-1 Notes    C-2 Kanban    C-3 Obsah
                         │            │            │
                         └──────┬─────┴──────┬─────┘
                                ▼            ▼
                         C-4 Verze/merge   C-5 Vyhledávání   C-15a AI abstrakce
                                │            │                  │
                                ▼            │                  │
                         C-6 Synchronizace   │                  │
                                │            │                  │
            ┌───────────────────┼────────────┼──────────────────┼──────────────┐
            ▼                   ▼            ▼                  ▼              ▼
Desktop   C-8 Úložiště ──► C-11 Shell     Server  C-13 API ◄── C-14 Identita
          C-7a Bloby          │                    │  ├─ C-7b Bloby
                              │                    │  ├─ C-15b AI gateway
UI        C-9 Sdílené UI ◄────┤                    │  ├─ C-16 Připomínky
          C-10 Editor ◄───────┘                    │  └─ C-17 Audit
                              │                    │
Tenký     C-12 PWA shell ─────┘                    ▼
klient    C-19 MAUI shell (I-5)             C-18 Provoz
```

Pravidla směru závislostí:

1. Sdílené knihovny (C-1 až C-6, C-15a) nezávisí na žádné komponentě z vrstev UI, desktop, tenký klient,
   server ani provoz.
2. C-9 závisí na sdílených knihovnách a na rozhraních platformních služeb, nikdy na shellu.
3. Shelly (C-11, C-12, C-19) závisí na C-9 a implementují jeho platformní služby.
4. Desktop a server spolu komunikují výhradně přes C-6; tenký klient výhradně přes REST API C-13.
5. Doména Notes a Kanban na sobě nezávisí; propojuje je pouze identifikátor a vyhledávání.

## 4. Přiřazení požadavků

| Požadavek | Komponenty | Inkrement |
|---|---|---|
| FR-1, FR-2, FR-3, FR-4, FR-5 | C-1, C-8, C-9, C-13 | I-1 |
| FR-6 (mechanismus verzí) | C-4, C-8, C-13 | I-1 |
| FR-6 (zobrazení a porovnání historie) | C-4, C-9 | I-2 |
| FR-7 (bez tabulek, barvy textu a pozadí textu) | C-3, C-10 | I-1 |
| FR-7 (tabulky, barva textu, pozadí textu) | C-3, C-10 | I-2 |
| FR-8 | C-9 | I-1 |
| FR-9 | C-3, C-7a, C-7b, C-10 | I-2 |
| FR-10, FR-11 | C-3, C-10 | I-2 |
| FR-12 | C-3, C-7b, C-13 | I-2 |
| FR-13, FR-14, FR-15, FR-16, FR-25 | C-15a, C-15b, C-9, C-11 | I-3 |
| FR-17, FR-18, FR-19, FR-20, FR-21, FR-23 | C-2, C-8, C-9, C-13 | I-4 |
| FR-22 | C-2, C-15a, C-15b, C-9 | I-4 |
| FR-24 | C-5, C-8, C-13 | I-1 |
| FR-26, FR-27 | C-6, C-8, C-11, C-13 | I-1 |
| FR-28 | C-4, C-6, C-9, C-13 | I-1 |
| FR-29, FR-30 | C-12, C-13 | I-1 |
| FR-31, FR-32 | C-13, C-14, C-11, C-12 | I-1 |
| FR-33 | C-11, C-12, C-16 | I-2 |
| FR-34 | C-9 | I-1 |
| FR-35 | C-17, C-13 | I-1 (AI události v I-3) |
| FR-36 | C-18 | I-1 |
| FR-37 | C-6, C-11, C-13 | I-1 |
| NFR-1, NFR-2, NFR-19 | C-9 | I-1 a průběžně |
| NFR-3, NFR-6 | C-8, C-6 | I-1 |
| NFR-4 | C-4, C-6, C-13 | I-1 |
| NFR-5 | C-5 | I-1 |
| NFR-7 | C-4, C-7a, C-7b, C-8, C-13 | I-1 a I-2 |
| NFR-8, NFR-9 | C-13, C-14, C-18, C-15b | I-1 (C-15b v I-3) |
| NFR-10 | C-17 | I-1 |
| NFR-11, NFR-12 | C-15a, C-15b, C-9 | I-3 |
| NFR-13 | C-3 | I-1 (registr typů) a I-2 (diagramy) |
| NFR-14 | C-14 | I-1 |
| NFR-15 | C-9 (I-1), C-19 (I-5) | I-1, I-5 |
| NFR-16 | C-1, C-2 | I-1, I-4 |
| NFR-17 | C-12 | I-1 |
| NFR-18 | C-6, C-13 | I-1 |
| NFR-20 | C-3 | I-2 |

## 5. Pořadí implementace

Každý inkrement je samostatný běh architektonického enginu (FRAME → TASKS) nad tímto plánem a nad
architecture profilem projektu. Inkrement je dokončen, když jsou splněna akceptační kritéria požadavků, které
mu sekce 4 přiřazuje.

### I-1 — Walking skeleton

**Cíl:** nahradit Google Keep na desktopu i v telefonu pro textové poznámky; ověřit nejrizikovější
architekturu dřív, než na ní stojí velký kód.
**Komponenty:** C-1, C-3 (schéma v1 s ID bloků a registrem typů), C-4, C-5, C-6, C-8, C-9, C-10 (formátování
bez tabulek a barev), C-11, C-12, C-13, C-14, C-17, C-18.
**Vnitřní pořadí:**
1. C-18 lokálně (databáze v kontejneru) a kostra řešení se sdílenými knihovnami;
2. C-1, C-3, C-4, C-5 — sdílená logika, včetně scénářů FR-28 akceptace 1–4 jako testů;
3. C-13, C-14, C-17 — server s identitou a auditem;
4. C-9, C-10, C-12 — sdílené UI a tenký klient proti serveru;
5. C-8, C-6, C-11 — desktop s lokálním úložištěm a synchronizací;
6. C-18 na Hetzner Cloud, zálohy a vyzkoušená obnova.

**Proč toto pořadí:** tenký klient (krok 4) ověří API a UI dřív, než vznikne složitější desktopová
synchronizace (krok 5). Slučování (krok 2) je hotové a otestované dřív, než se na něj napojí síť.

### I-2 — Bohatý obsah

**Komponenty:** C-3, C-10 (tabulky, barvy, obrázky, code, diagramy), C-7a, C-7b, C-4 a C-9 (historie
a porovnání verzí), C-3 a C-13 (export), C-11, C-12, C-16 (připomínky).
**Závisí na:** I-1.

### I-3 — AI

**Komponenty:** C-15a, C-15b, C-9, C-11 (lokální provider), C-17 (AI události).
**Závisí na:** I-1; využívá bohatý obsah z I-2 (generování diagramů, úprava kódu).

### I-4 — Projects / Kanban

**Komponenty:** C-2, C-8, C-13, C-9, C-15a a C-15b (AI v Kanbanu).
**Závisí na:** I-1, I-3.

### I-5 — Mobilní shell

**Komponenty:** C-19.
**Závisí na:** I-1, I-2. Spouští se podle potřeby, až omezení PWA (NFR-17) začnou vadit.

## 6. Rizika a zmírnění (informativní)

| ID | Riziko | Zmírnění v plánu |
|---|---|---|
| R-2 | Slučování bez ztráty změny | Jediný offline zapisovatel; slučování v čisté knihovně C-4 testované scénáři FR-28 dřív než síť (I-1 krok 2). |
| R-3 | Změna schématu obsahu | Schéma verzované od prvního dne, migrace v C-3; registr typů bloků v I-1, i když code a diagramy přijdou v I-2. |
| R-4 | Velikost rozsahu | Pět inkrementů; I-1 je největší, ale po něm je systém v provozu. |
| R-5 | Multiplatformnost | Platformní služby za rozhraním v C-9 od I-1; C-19 bez přepsání UI. |
| R-6 | AI a data | Klíče jen na serveru, odeslání jen na explicitní akci (C-15b). |
| R-7 | Růst historie | Verze za relaci; bloby adresované obsahem sdílené mezi verzemi; úklid blobů bez odkazu. Verze se automaticky nemažou (NFR-7: bez limitu počtu); s trvalým smazáním poznámky se smažou i její verze. |
| R-8 | Editor | Hotový editor (TipTap); úzké rozhraní C-10 ↔ C-3. |
| R-9 | **Nové.** Velikost I-1 | Vnitřní pořadí I-1 dodává použitelný tenký klient dřív než desktop; I-1 lze v běhu pro TASKS rozdělit na dávky. |
| R-10 | **Nové.** Přihlášení Google z desktopové aplikace (přesměrování zpět do nativní aplikace) | Přihlášení přes systémový prohlížeč s návratem do aplikace; ověřit prototypem na začátku I-1 kroku 3. |
| R-11 | **Nové.** Provoz na VPS jedním člověkem | Vše v kontejnerech, zálohy automatické a mimo server, obnova vyzkoušená před nasazením reálných dat. |

## 7. Soulad s architektonickým profilem

Architecture basis je `greenfield`; profil neexistuje, a proto neexistují pravidla ani odchylky.
**Navazující krok mimo tento běh:** vytvořit architecture profile projektu z tohoto plánu a z `DECISIONS.md`.
Pravidla směru závislostí ze sekce 3 a hranice komponent ze sekce 2 jsou prvními kandidáty na závazná
pravidla profilu.

## 8. Zamítnuté varianty (informativní)

Nikdy neimplementovat. Podrobnosti a důvody jsou v `DECISIONS.md`.

| Rozhodnutí | `REJECTED` |
|---|---|
| D-1 | MAUI shell pro desktop; Blazor WebAssembly jako desktopový klient; TypeScript + Tauri + Capacitor; Flutter |
| D-2 | HTML jako kanonický formát; Markdown jako kanonický formát; vlastní editor v C# |
| D-3.1 | Verze za každé uložení |
| D-3.2 | Odeslání řetězu offline verzí jako větve |
| D-3.3 | Slučování textu uvnitř bloku po znacích |
| D-3.4 | Konflikt pro skalární metadata; konflikt jen pro název |
| D-4 | Lucene.NET na obou stranách; nativní fulltext každé databáze zvlášť; vyhledávání jen na serveru |
| D-5 | Bloby v databázi; obrázky vložené v dokumentu; stahování blobů na desktopu na vyžádání |
| D-6 | Spravovaná platforma, Azure, domácí server; SQL Server; aplikační šifrování obsahu per uživatel; tokeny Google přímo v API |
| D-7 | Plánování jen na serveru; plánování jen na klientu |
| D-8 | Vše přes server; vše přímo z klientů; vektorový index nyní; automatické AI funkce s cloudovým providerem |
| D-9 | Desktop nejdřív lokálně; walking skeleton bez tenkého klienta |
| A-3 (c) | Nová poznámka přebírá kategorii z aktuálního pohledu |
