# DECISIONS.md

Architektonická rozhodnutí (`D-n`). Zamítnuté varianty zůstávají zachované a označené `REJECTED`.

**Upřesnění operátora k D-1 a D-6.1 (2026-09-15):** cílová platforma .NET 10; kontejnery spustitelné v Podmanu i Dockeru, lokální vývoj s databází v kontejneru (Podman).

Pořadí rozhodování vychází z `analysis.md` § 5. Stack je předsunut před reprezentaci obsahu, protože omezuje
výběr editoru, a tím i formát obsahu.

| ID | Téma | Stav |
|---|---|---|
| D-1 | Stack a shelly klientů | **ROZHODNUTO** (a), 2026-09-15 |
| D-2 | Reprezentace obsahu a editor | **ROZHODNUTO** (a), 2026-09-15 |
| D-3 | Model verzování a slučování | **ROZHODNUTO** 3.1 a, 3.2 a, 3.3 a, 3.4 a, 2026-09-15 |
| D-4 | Vyhledávání a normalizace textu | **ROZHODNUTO** (a), 2026-09-15 |
| D-5 | Úložiště binárního obsahu | **ROZHODNUTO** 5.1 a, 5.2 a, 2026-09-15 |
| D-6 | Provoz serveru, databáze, šifrování a identita (O-3) | **ROZHODNUTO** 6.1 a1, 6.2 a, 6.3 a, 6.4 a, 2026-09-15 |
| D-7 | Plánování připomínek | **ROZHODNUTO** (a), 2026-09-15 |
| D-8 | AI vrstva a hranice odesílání dat (O-5) | **ROZHODNUTO** 8.1 a, 8.2 c, 8.3 a, 2026-09-15 |
| D-9 | Rozsah první verze a pořadí inkrementů (O-4) | **ROZHODNUTO** (b), 2026-09-15 |

---

## D-1 — Stack a shelly klientů

**Stav:** ROZHODNUTO operátorem 2026-09-15 — varianta (a)
**Řeší:** O-2 · **Požadavky:** FR-7 až FR-11, FR-26, FR-29, NFR-15, NFR-19 · **Rizika:** R-5, R-8

**Varianty:**

| | Varianta | Pro | Proti |
|---|---|---|---|
| a | **.NET napříč.** Server ASP.NET Core. UI jako sdílená Razor Class Library. Desktop: WPF + BlazorWebView (WebView2). Web a tablet: Blazor WebAssembly jako PWA. Mobil později: MAUI Blazor Hybrid, nejdřív Android. Editor jako JS ostrov přes interop. | Zkušenost operátora. Jeden jazyk pro doménu, server a UI. Jedna knihovna komponent pro tři shelly (NFR-15, NFR-19). WPF shell je zralý, nasazuje se bez MSIX a jde snadno aktualizovat. | Editor, diagramy a zvýraznění kódu jsou v JS; interop je hranice, kterou je potřeba navrhnout a udržovat. Start WASM na mobilu je pomalejší (NFR-17). |
| a′ `REJECTED` | Jako (a), ale desktop také v MAUI Blazor Hybrid. | Jeden shell pro desktop i mobil. | Windows cíl MAUI (WinUI 3) je méně zralý než WPF, balíčkování a aktualizace jsou složitější. Mobil je až pozdější inkrement, takže výhoda sdílení se projeví později než cena. |
| b `REJECTED` | .NET server, Blazor WebAssembly všude včetně desktopu jako nainstalované PWA. | Nejméně shellů. | Local-first desktop s databází v prohlížeči je křehký: perzistenci může prohlížeč smazat, fulltext nad velkou lokální DB je pracný (FR-26, NFR-3, NFR-7). Porušuje roli desktopu jako hlavního klienta. |
| c `REJECTED` | Webový stack: TypeScript UI, Tauri pro desktop, Capacitor pro mobil, server libovolný. | Editor, diagramy a případné CRDT jsou nativní ekosystém, žádný interop. | Ztráta zkušenosti operátora. Při .NET serveru dva jazyky a duplicitní doménový model. |
| d `REJECTED` | Flutter klienti, .NET server. | Nejlepší pocit z mobilní aplikace. | Chybí zralý rich-text editor s tabulkami, code blocky a diagramy srovnatelný s JS ekosystémem (R-8). Dva jazyky. |

**Rozhodnutí:** (a). Desktop WPF + BlazorWebView, web a tablet Blazor WebAssembly PWA, mobil později MAUI Blazor Hybrid, sdílená Razor Class Library, server ASP.NET Core, editor jako JS ostrov.

## D-2 — Reprezentace obsahu a editor

**Stav:** ROZHODNUTO operátorem 2026-09-15 — varianta (a)
**Řeší:** R-3, K-3 · **Požadavky:** FR-6, FR-7, FR-10, FR-11, FR-12, FR-28, NFR-13 · **Závisí na:** D-1

**Varianty:**

| | Varianta | Pro | Proti |
|---|---|---|---|
| a | **Strukturovaný dokument jako JSON strom** podle vlastního, verzovaného schématu. Editor TipTap (ProseMirror). Každý blok má stabilní identifikátor. | Stabilní ID bloků umožňují slučování po blocích (FR-28) a porovnání verzí po blocích (FR-6). Vlastní typy uzlů pro diagramy a code blocky (FR-10, FR-11, NFR-13). Tabulky a formátování z FR-7 jsou pokryté. Export do HTML, Markdownu i bezeztrátový export je převod stromu (FR-12). | Schéma dokumentu je potřeba verzovat a migrovat. Formát je navázaný na model ProseMirroru; dokument je ale otevřený JSON a schéma vlastní. |
| b `REJECTED` | HTML jako kanonický formát. | Univerzální, snadné zobrazení i export. | Bloky nemají stabilní identitu, slučování i porovnání nad HTML je křehké a hlučné. Nutná sanitizace. Vlastní uzly jen přes atributy. |
| c `REJECTED` | Rozšířený Markdown jako kanonický formát. | Čitelný, výborný export, přehledné rozdíly. | Barvy textu, zvýraznění, podtržení a pozadí textu nejsou ve standardu (K-3); vznikne vlastní dialekt. Převod z WYSIWYG editoru a zpět je ztrátový. |
| d `REJECTED` | Vlastní dokumentový model v C# a vlastní editor. | Žádná JS závislost. | Roky práce (R-8). |

**Rozhodnutí:** (a). Kanonický formát obsahu je JSON strom podle vlastního verzovaného schématu, editor TipTap (ProseMirror), každý blok nese stabilní identifikátor.

## D-3 — Model verzování a slučování

**Stav:** ROZHODNUTO operátorem 2026-09-15 — D-3.1 (a), D-3.2 (a), D-3.3 (a), D-3.4 (a)
**Řeší:** K-2, K-7, R-2, R-7 · **Požadavky:** FR-6, FR-19, FR-27, FR-28, NFR-4, NFR-7 · **Závisí na:** D-2
**Již rozhodnuto:** server je autoritativní, jediný offline zapisovatel je desktop (frame 2.1); verzování jako
v Confluence a tříbodový merge (analysis § 7, K-2); bloky mají stabilní ID (D-2).

D-3 má čtyři dílčí volby. Každá je samostatně rozhodnutelná.

### D-3.1 — Kdy vzniká verze

| | Varianta | Pro | Proti |
|---|---|---|---|
| a | **Verze za editační relaci.** Obsah se ukládá průběžně, ale nová verze vznikne až po nečinnosti, zavření poznámky nebo synchronizaci. | Historie je čitelná jako v Confluence. Objem odpovídá NFR-7. | Uživatel nevidí v historii každý jednotlivý zápis. |
| b `REJECTED` | Verze za každé uložení. | Nejjemnější historie. | Průběžné ukládání editoru vytvoří tisíce verzí; historie je nečitelná a roste nad rámec NFR-7 (R-7). |

**Rozhodnutí:** (a). Verze vzniká za editační relaci.

### D-3.2 — Offline historie desktopu

| | Varianta | Pro | Proti |
|---|---|---|---|
| a | **Sloučit do jedné verze.** Mezilehlé offline verze existují lokálně; při synchronizaci se odešle výsledný stav se základní verzí a na serveru vznikne jedna verze. Po úspěšné synchronizaci se lokální mezilehlé verze zahodí. | Historie na serveru je lineární a stejná na všech klientech. Jednoduchý protokol. | Mezilehlé stavy z offline práce se na serveru neobjeví. |
| b `REJECTED` | Odeslat celý řetěz offline verzí jako větev a pak sloučit. | Úplná historie. | Větvená historie, složitější protokol i UI porovnání verzí; více dat (R-7). |

**Rozhodnutí:** (a). Offline práce desktopu se na serveru projeví jako jedna verze.

### D-3.3 — Slučování obsahu

Tříbodový merge nad bloky spárovanými podle ID. Blok změněný jen na jedné straně se převezme; stejná změna na
obou stranách je shoda; blok smazaný na jedné a upravený na druhé straně se zachová upravený (FR-28 akceptace 4);
změna pořadí bloků se slučuje jako seznam. Otevřená je jen jedna věc — co když obě strany změní tentýž blok:

| | Varianta | Pro | Proti |
|---|---|---|---|
| a | **Konflikt na úrovni bloku.** Obě varianty bloku se zachovají a uživatel rozhodne. | Předvídatelné, odpovídá konceptu 18 („stejná část obsahu“) a FR-28 akceptaci 2. | Dvě nepřekrývající se úpravy téhož dlouhého odstavce vyvolají konflikt. |
| b `REJECTED` | Slučování textu uvnitř bloku po znacích, konflikt jen při překryvu. | Méně konfliktů. | Formátování uvnitř odstavce (značky, odkazy) se po znacích slučuje křehce; výsledek může být sloučený, ale nesmyslný — tichá chyba, horší než konflikt (NFR-4). |

**Rozhodnutí:** (a). Souběžná změna téhož bloku je konflikt na úrovni bloku.

### D-3.4 — Slučování metadat

Týká se názvu, kategorie, štítků, barvy, připnutí, stavu a u karet polí z FR-19.

| | Varianta | Pro | Proti |
|---|---|---|---|
| a | **Množiny se slučují, skalární pole vyhrává poslední zápis.** Štítky a checklist se slučují přidáním a odebráním. U skalárního pole změněného na obou stranách vyhrává později přijatá změna, ale přepsaná hodnota zůstává v historii verzí a ve stavu synchronizace se zobrazí upozornění. | Žádné konfliktní dialogy kvůli barvě nebo připnutí. Nic se neztrácí — hodnotu jde dohledat. | Při striktním čtení NFR-4 jde o změnu přepsanou bez rozhodnutí uživatele; zmírněno upozorněním a historií. **Vyžaduje vědomé přijetí operátorem.** |
| b `REJECTED` | Konflikt i pro skalární pole. | Striktně splňuje NFR-4. | Konfliktní dialog kvůli barvě karty je nepřiměřený a zhoršuje NFR-2. |
| c `REJECTED` | Hybrid: název jako konflikt, ostatní skalární pole podle (a). | Chrání nejhodnotnější metadatum. | Dvě pravidla místo jednoho. |

**Rozhodnutí:** (a). Operátor vědomě přijímá, že u skalárních metadat vyhrává poslední zápis; tento výklad NFR-4 je schválen za podmínky, že přepsaná hodnota zůstává v historii verzí a stav synchronizace zobrazí upozornění. Obsah poznámky (bloky) tímto pravidlem dotčen není.

## D-4 — Vyhledávání a normalizace textu

**Stav:** ROZHODNUTO operátorem 2026-09-15 — varianta (a)
**Řeší:** K-6, O-9 · **Požadavky:** FR-24, FR-26, NFR-5, NFR-6, NFR-7 · **Závisí na:** D-1, D-2

Požadavek FR-24 akceptace 4 — stejný dotaz nad stejnými daty vrací na desktopu i na tenkém klientovi stejné
výsledky — vylučuje, aby každá strana normalizovala text po svém.

| | Varianta | Pro | Proti |
|---|---|---|---|
| a | **Sdílená normalizace a tokenizace v C#, databáze jen indexuje.** Z JSON dokumentu (D-2) se vytáhne prostý text; jedna knihovna v C# ho normalizuje (Unicode NFC, velikost písmen, diakritika) a rozdělí na tokeny. Desktop i server indexují již normalizované tokeny ve svém fulltextu a dotaz projde stejnou normalizací. | Shodná sada výsledků je zaručená stejným kódem (FR-24 akc. 4). Nezávislé na volbě serverové databáze (D-6). | Řazení podle relevance se mezi stranami může lišit; normalizovaná kopie textu zabírá místo. |
| b `REJECTED` | Stejná vyhledávací knihovna na obou stranách (Lucene.NET), index mimo databázi. | Identický analyzér včetně řazení; existuje český stemmer, tedy cesta ke skloňování. | Lucene.NET 4.8 je dlouhodobě v beta verzi. Druhé úložiště vedle databáze, které je nutné udržovat konzistentní na desktopu i na serveru. |
| c `REJECTED` | Nativní fulltext každé databáze s vlastní konfigurací (např. SQLite FTS5 s odstraněním diakritiky, PostgreSQL s `unaccent`). | Nejméně kódu. | Tokenizace a hledání podle prefixu se mezi enginy liší; FR-24 akceptaci 4 nelze zaručit. |
| d `REJECTED` | Vyhledávání pouze na serveru. | Jedna implementace. | Porušuje FR-26 akceptaci 1 (vyhledávání offline na desktopu). |

**Rozhodnutí:** (a). Normalizace a tokenizace je jedna sdílená knihovna v C#; desktop i server indexují již normalizované tokeny. Varianta (b) zůstává zaznamenaná jako budoucí cesta ke skloňování, nikoliv k implementaci. Pokud bude později požadováno skloňování, je (b) přirozená cesta a (a) ji nezavírá.

## D-5 — Úložiště binárního obsahu

**Stav:** ROZHODNUTO operátorem 2026-09-15 — D-5.1 (a), D-5.2 (a)
**Řeší:** K-9, R-7 · **Požadavky:** FR-9, FR-12, NFR-7, NFR-9 · **Závisí na:** D-2, D-3

### D-5.1 — Kde a jak jsou obrázky uloženy

| | Varianta | Pro | Proti |
|---|---|---|---|
| a | **Neměnné bloby adresované obsahem mimo databázi.** Obrázek dostane identifikátor odvozený z obsahu; dokument (D-2) na něj jen odkazuje. Server ukládá bloby do souborového nebo objektového úložiště, desktop do souborů v lokálním adresáři aplikace. Nahrazení obrázku je nový blob. | Verze dokumentu obrázek nekopírují — sto verzí poznámky s obrázkem 10 MB je jeden blob (R-7). Blob je neměnný, takže ho není třeba slučovat (D-3). Databáze zůstává malá. Stejný obrázek vložený dvakrát se uloží jednou. | Dvě úložiště k zálohování a konzistenci (databáze + bloby). Nepoužívané bloby je potřeba uklízet v návaznosti na retenci verzí. |
| b `REJECTED` | Bloby jako sloupce v databázi na obou stranách. | Transakční konzistence, záloha jedním krokem. | Databáze roste o jednotky GB na uživatele (NFR-7), zálohy i obnova jsou pomalé, SQLite s velkými bloby zpomaluje. |
| c `REJECTED` | Obrázek vložený přímo do JSON dokumentu (base64). | Nejjednodušší. | Každá verze nese kopii obrázku (R-7), dokument je obrovský, merge i synchronizace těžké. |

**Rozhodnutí:** (a). Neměnné bloby adresované obsahem, mimo databázi; dokument odkazuje identifikátorem.

### D-5.2 — Dostupnost obrázků na desktopu

| | Varianta | Pro | Proti |
|---|---|---|---|
| a | **Desktop stahuje všechny bloby uživatele průběžně; tenký klient na vyžádání.** | Desktop je plnohodnotný klient a obrázky jsou vždy offline (FR-9 akc. 4, FR-26). Jednotky GB jsou pro desktop přijatelné (NFR-7). | První synchronizace nového desktopu trvá déle. |
| b `REJECTED` | Desktop stahuje na vyžádání s lokální mezipamětí. | Úspora místa. | Obrázek v poznámce, kterou uživatel dlouho neotevřel, offline chybí — v rozporu s rolí hlavního klienta. |

**Rozhodnutí:** (a). Desktop drží všechny bloby uživatele, tenký klient stahuje na vyžádání.

## D-6 — Provoz serveru, databáze, šifrování a identita

**Stav:** ROZHODNUTO operátorem 2026-09-15 — D-6.1 (a1), D-6.2 (a), D-6.3 (a), D-6.4 (a)
**Řeší:** O-3, K-1 (zbytek) · **Požadavky:** FR-31, FR-32, FR-35, FR-36, NFR-8, NFR-9, NFR-14 · **Závisí na:** D-1, D-4, D-5

### D-6.1 — Provozní model serveru (O-3)

| | Varianta | Pro | Proti |
|---|---|---|---|
| a | Vlastní VPS nebo server, kontejnery. | Plná kontrola, nízké náklady, bez vazby na poskytovatele. | Záloha, TLS certifikáty, aktualizace OS a monitoring jsou na operátorovi (FR-36, NFR-8). |
| b `REJECTED` | Spravovaná cloudová platforma (např. Azure App Service + spravovaná databáze + blob storage + trezor klíčů). | Zálohy, šifrování úložiště a správa klíčů jsou součástí služby (FR-36, NFR-9). | Vyšší průběžné náklady, částečná vazba na poskytovatele. |
| c `REJECTED` | Domácí server nebo NAS za reverzní proxy. | Nejlevnější. | Dostupnost tenkého klienta na cestách závisí na domácím připojení; vystavení domácí sítě do internetu je bezpečnostní riziko (NFR-8). |

**Upřesnění operátora (2026-09-15):** hosting zatím nevybrán; požadavek zůstat u .NET / ASP.NET Core;
preferováno levnější, ale solidní řešení.

Upřesněné varianty. Ceny jsou orientační podle stavu znalostí a je nutné je ověřit v aktuálním ceníku.

| | Varianta | Orientační cena | Pro | Proti |
|---|---|---|---|---|
| a1 | **Linuxový VPS u evropského poskytovatele (např. Hetzner Cloud). Docker Compose: kontejner ASP.NET Core, kontejner PostgreSQL, bloby na svazku, reverzní proxy s automatickými TLS certifikáty. Automatické zálohy databáze a blobů mimo server do objektového úložiště.** | jednotky až nízké desítky EUR měsíčně | Nejlepší poměr ceny a výkonu. Data v EU. ASP.NET Core na Linuxu bez licence Windows. Kontejnery drží přenositelnost na a2 nebo b. | Aktualizace OS, monitoring a ověřování záloh jsou na operátorovi. |
| a2 `REJECTED` | Aplikační platforma se spravovanou databází (např. DigitalOcean App Platform + Managed PostgreSQL). | nízké desítky USD měsíčně | Spravované zálohy databáze a TLS bez správy OS. | Dražší než a1; bloby vyžadují samostatné objektové úložiště. |
| b `REJECTED` | Azure: App Service + Azure Database for PostgreSQL Flexible Server + Blob Storage + Key Vault. | desítky USD měsíčně | Nejvíce spravovaných služeb, trezor klíčů pro D-6.3. | Nejdražší; pro jednotky uživatelů předimenzované. |
| c `REJECTED` | Domácí server nebo NAS (původní varianta c). | — | — | Viz tabulka výše; operátor požaduje solidní řešení. |

**Rozhodnutí:** (a1), poskytovatel Hetzner Cloud (volba operátora). Podmínka rozhodnutí: automatizovaných záloh mimo server a pravidelně ověřené obnovy (FR-36
akceptace 2 a 3). Při (a1) je trezor klíčů z D-6.3 nahrazen tajemstvím předaným kontejneru z prostředí. Architektura
musí být přenositelná mezi (a) a (b): kontejnerizovaný server, úložiště blobů za rozhraním, konfigurace
z prostředí.

### D-6.2 — Serverová databáze

| | Varianta | Pro | Proti |
|---|---|---|---|
| a | **PostgreSQL.** | Bez licenčních nákladů, běží všude (D-6.1 a i b), dobrá podpora JSON. D-4 (a) je na enginu nezávislé. | — |
| b `REJECTED` | SQL Server. | Pravděpodobně známé prostředí. | Licence; bezplatná edice Express má limit 10 GB na databázi, na hranici NFR-7 s verzemi. |

**Rozhodnutí:** (a). PostgreSQL.

### D-6.3 — Šifrování dat na serveru a tajné údaje

| | Varianta | Pro | Proti |
|---|---|---|---|
| a | **Šifrování na úrovni úložiště** (disk, databáze, blob storage, zálohy) a TLS; aplikační šifrování pouze pro tajné údaje — zejména API klíče AI providerů uživatelů — klíčem z trezoru. | Splňuje NFR-9 bez dopadu na vyhledávání a AI. Tajné údaje jsou chráněné i před únikem databáze. | Správce s přístupem k běžící databázi vidí obsah poznámek. |
| b `REJECTED` | Aplikační šifrování obsahu per uživatel. | Chrání obsah i před správcem databáze. | Serverový index z D-4 obsahuje normalizované tokeny, tedy čitelný text — ochrana obsahu by byla jen zdánlivá, pokud by se nešifroval i index, a šifrovaný index znemožní fulltext. |

**Rozhodnutí:** (a). Šifrování úložiště a záloh, TLS; aplikační šifrování pouze tajných údajů.

### D-6.4 — Identita

| | Varianta | Pro | Proti |
|---|---|---|---|
| a | **Server jako vlastní vydavatel tokenů, Google jako externí přihlášení.** Uživatel má interní identifikátor; externí identity jsou k němu navázané. Klienti dostávají tokeny aplikace. Desktop se přihlašuje přes systémový prohlížeč a obnovovací token uchovává chráněný operačním systémem; spuštění offline token nevyžaduje. | Přidání dalšího poskytovatele se klientů ani dat netýká (NFR-14, FR-32 akc. 2). Autorizace je pod kontrolou serveru (NFR-8). Splňuje FR-26 akc. 2. | Server musí implementovat vydávání a obnovu tokenů. |
| b `REJECTED` | Klienti posílají API přímo tokeny Google. | Nejméně kódu. | API je navázané na Google; přidání poskytovatele mění ověřování na všech klientech (NFR-14). |

**Rozhodnutí:** (a). Server vydává vlastní tokeny, externí poskytovatelé identity jsou navázaní na interní identifikátor uživatele.

## D-7 — Plánování připomínek

**Stav:** ROZHODNUTO operátorem 2026-09-15 — varianta (a)
**Řeší:** K-8 · **Požadavky:** FR-33, FR-26, NFR-17 · **Závisí na:** D-1

| | Varianta | Pro | Proti |
|---|---|---|---|
| a | **Každé zařízení má právě jeden kanál.** Desktop plánuje připomínky lokálně ze synchronizovaných dat a zobrazuje systémové notifikace Windows; k serverovému push se nepřihlašuje. Tenký klient a mobil dostávají notifikaci push ze serveru (web push pro PWA, později nativní push přes MAUI). | Desktop upozorní i offline (FR-33 akc. 3). Zařízení nemůže dostat dvě notifikace, protože má jediný zdroj (akc. 2). | Dvě implementace plánování, lokální a serverová. Zavření připomínky na jednom zařízení se na ostatní propaguje až synchronizací. |
| b `REJECTED` | Plánuje jen server, push na všechna zařízení včetně desktopu. | Jedna implementace. | Desktop offline připomínku nezobrazí (porušuje FR-33 akc. 3). |
| c `REJECTED` | Plánuje jen klient na každém zařízení. | Bez serverové části. | Webová aplikace, která neběží, nic nenaplánuje; mobil notifikaci nedostane (porušuje FR-33 akc. 1). |

**Rozhodnutí:** (a). Desktop plánuje připomínky lokálně, tenký klient a mobil dostávají push ze serveru; každé zařízení má jediný zdroj notifikací.

## D-8 — AI vrstva a hranice odesílání dat

**Stav:** ROZHODNUTO operátorem 2026-09-15 — D-8.1 (a), D-8.2 (c), D-8.3 (a)
**Řeší:** O-5, K-5, R-6 · **Požadavky:** FR-13 až FR-16, FR-22, FR-25, FR-35, NFR-3, NFR-8, NFR-11, NFR-12 · **Závisí na:** D-1, D-4, D-6.3

### D-8.1 — Kudy jdou volání AI

| | Varianta | Pro | Proti |
|---|---|---|---|
| a | **Lokální provideři přímo z desktopu, cloudoví provideři vždy přes server.** Abstrakce providerů je sdílená knihovna v C#. API klíče existují jen na serveru (D-6.3). | Klíče nikdy neopouštějí server. Cloudová volání jdou auditovat na jednom místě (FR-35). Cloudová AI internet potřebuje tak jako tak, cesta přes server nic neztrácí (NFR-3). Lokální model funguje offline (FR-16 akc. 3). | Lokální provider (LM Studio na počítači uživatele) je dostupný jen z desktopu, ne z tenkého klienta. |
| b `REJECTED` | Vše přes server. | Jedna cesta. | Server se nedostane k LM Studio na počítači uživatele; lokální AI offline nefunguje (porušuje FR-16 akc. 3). |
| c `REJECTED` | Vše přímo z klientů. | Nejnižší latence. | Tenký klient by musel mít API klíče v prohlížeči (NFR-8). |

**Rozhodnutí:** (a). Lokální provideři přímo z desktopu, cloudoví vždy přes server; API klíče pouze na serveru.

### D-8.2 — AI napříč poznámkami

| | Varianta | Pro | Proti |
|---|---|---|---|
| a | Vyhledání relevantních poznámek fulltextem (D-4) a předání jejich textu modelu s odkazy na zdroj. | Žádná nová infrastruktura, funguje i s lokálním modelem, odkazy na zdroje pro FR-15 akc. 1. | Dotazy bez shody slov najdou méně (dotaz na „certifikáty“ nenajde poznámku, která mluví jen o „PKI“). |
| b `REJECTED` | Vektorový index (embeddingy) na serveru i desktopu. | Významové vyhledávání, lepší výsledky dotazů. | Druhý index na obou stranách; obsah se posílá k výpočtu embeddingů (NFR-12); změna modelu znamená přeindexování. |
| c | **(a) nyní, s místem pro rozšíření o (b) později.** | Levný začátek bez uzavření cesty. | Kvalitu dotazů napříč poznámkami je nutné ověřit provozem. |

**Rozhodnutí:** (c). Vyhledání přes fulltext nyní; vektorový index (b) je zaznamenané budoucí rozšíření, nikoliv součást plánu.

### D-8.3 — Hranice odesílání obsahu (O-5)

| | Varianta | Pro | Proti |
|---|---|---|---|
| a | **Obsah se ke cloudovému providerovi odešle jen na explicitní akci uživatele.** Před odesláním je vidět provider a rozsah (výběr / poznámka / seznam poznámek). Automatické AI funkce na pozadí jsou povolené pouze s lokálním providerem. | Splňuje NFR-12 bez výjimek. | Automatické návrhy (např. štítků) s cloudovým modelem nejsou k dispozici. |
| b `REJECTED` | Automatické funkce i s cloudovým providerem po jednorázovém souhlasu v nastavení. | Pohodlnější. | Po souhlasu odchází obsah na pozadí bez vědomí uživatele u konkrétní poznámky; slabší naplnění NFR-12. |

**Rozhodnutí:** (a). Odeslání obsahu do cloudu jen na explicitní akci s viditelným providerem a rozsahem; automatické funkce jen s lokálním providerem.

## D-9 — Rozsah první verze a pořadí inkrementů

**Stav:** ROZHODNUTO operátorem 2026-09-15 — varianta (b)
**Řeší:** O-4, R-4 · **Závisí na:** D-1 až D-8 a na návrhu komponent C-1 až C-19 (předložen operátorovi
2026-09-15, bude součástí `plan.md`)

Každý inkrement bude samostatným během architektonického enginu (FRAME → TASKS) nad schváleným `plan.md`
a architecture profilem projektu.

| | Varianta | Pro | Proti |
|---|---|---|---|
| a `REJECTED` | **Desktop nejdřív, lokálně.** I-1: desktop Notes offline bez serveru. I-2: server, identita, synchronizace, audit, zálohy. I-3: tenký klient. I-4: bohatý obsah. I-5: AI. I-6: Kanban. I-7: mobilní shell. | Nejrychleji k něčemu použitelnému na desktopu. Nevyžaduje server ani provoz na začátku. | Nejrizikovější část (synchronizace a slučování, R-2) se ověří až ve druhém inkrementu, když už lokální úložiště a změnové sledování na desktopu existují a mohou vyžadovat přepracování. Bez mobilu Google Keep dál běží souběžně. |
| b | **Walking skeleton napříč.** I-1: poznámky se základním formátováním a checklistem na desktopu i v PWA, server, identita, synchronizace se slučováním a konflikty, vyhledávání, audit, zálohy a provoz. I-2: bohatý obsah (tabulky, obrázky, code, diagramy), historie a porovnání verzí, export, připomínky. I-3: AI. I-4: Kanban. I-5: mobilní shell MAUI. | Nejrizikovější architektura se ověří hned, dřív než na ní stojí velký kód. Po I-1 nahrazuje Google Keep na desktopu i v telefonu. | I-1 je velký a trvá déle, než je cokoliv použitelné. Provoz serveru je potřeba od začátku. |
| c `REJECTED` | Jako (b), ale I-1 bez tenkého klienta (desktop + server + synchronizace mezi dvěma desktopy). | Menší I-1 se stále ověřenou synchronizací. | Po I-1 chybí mobil; synchronizace je ověřená jen mezi desktopy, ne s tenkým klientem a frontou zápisů. |

**Rozhodnutí:** (b). Walking skeleton napříč; pět inkrementů I-1 až I-5 podle `plan.md` § 5.
