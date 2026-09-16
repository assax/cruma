# frame.md — Personal Notes & Workflow

```text
Phase: FRAME          Run mode: CONCEPTUAL          Architecture basis: greenfield
Output language: czech
Open items: O-1, O-2, O-3, O-4, O-5, O-6          Carried assumptions: A-1
Last approved gate: SESSION_PREMISE @ 2026-09-15 20:19
```

Zdroj: `c:\MyWork\_project\google-keep-client\concept.md` (dále „koncept“).

---

## 1. Rozsah tohoto běhu

Tento běh vytváří **FRAME → ANALYSIS → SPEC → PLAN pro celý systém**. Rozhoduje, co se staví a jak je to
strukturované. Nerozhoduje, v jakém pořadí se to implementuje po úkolech — dekompozice na `tasks.md` proběhne
v samostatných bězích po jednotlivých inkrementech nad schváleným PLAN.

## 2. Produktový rozsah

Součástí systému je:

* **Modul Notes** — kategorie (jedna úroveň), štítky, poznámka s volitelným názvem, barva, pin, reminder,
  archivace, koš, historie verzí (koncept 7).
* **Editor** — model souvislého dokumentu, rich text formátování, obrázky v toku dokumentu, checklisty
  (koncept 8).
* **Technický obsah jako prvotřídní funkce** — inline code, code blocks se zvýrazněním a detekcí jazyka,
  diagramy Mermaid a PlantUML s přepínáním zdroj/náhled a rozšiřitelností o další renderery (koncept 9).
* **Modul Projects / Kanban** — projekty, boardy s konfigurovatelnými sloupci, karty s metadaty, drag & drop,
  filtrování, archiv, backlog (koncept 12).
* **AI vrstva** — nad vybraným textem, nad poznámkou, napříč poznámkami (osobní knowledge base) a v Kanbanu;
  více providerů bez závislosti na jednom (koncept 11, 13).
* **Local-first desktopový klient** — vlastní lokální databáze, plná funkčnost offline (koncept 15,
  upřesněno operátorem: viz § 2.1 Topologie klientů).
* **Server** — REST API, autentizace a autorizace, synchronizace, centrální databáze, verzování, merge, audit,
  backup; později backend webového klienta (koncept 16).
* **Synchronizace a merge** — automatické slučování kompatibilních změn, explicitní konflikt a rozhodnutí
  uživatele tam, kde sloučit nelze; žádná tichá ztráta změny (koncept 17, 18).
* **Verzování poznámek** — historie, zobrazení starší verze, porovnání, obnovení (koncept 19).
* **Multi-user identita** — data oddělená po uživatelích, první provider Google, model nesmí být na Google
  vázaný (koncept 20).
* **Jednotný fulltext search** napříč Notes, Projects a Kanban kartami (koncept 22).
* **Reminders a systémové notifikace** na desktopu a mobilu (koncept 23).
* **Bezpečnost, šifrování a audit** jako požadavek od začátku (koncept 27, 28, 29).
* **Export a vlastnictví dat** v otevřených formátech (koncept 25) a **backup** serveru (koncept 26).

Cílové platformy: Windows desktop jako první klient, dále web, mobil a tablet. Architektura s nimi musí počítat
od začátku, i když vzniknou později (koncept 14).

### 2.1 Topologie klientů (upřesnění operátora, 2026-09-15)

Koncept 14, 15 a 17 předpokládal, že všichni nativní klienti včetně mobilu a tabletu budou local-first
s vlastní SQLite databází. Operátor tento předpoklad upřesnil:

| Klient | Role | Data | Offline |
|---|---|---|---|
| Windows desktop | **hlavní, plnohodnotný klient** | vlastní lokální databáze | plná funkčnost offline, synchronizace se serverem |
| Web, mobil, tablet | **tenký, doplňkový klient** pro práci na cestách | žádná lokální databáze, pracuje proti REST API | pouze fronta rozepsaných zápisů, viz níže |

Důsledky, které musí PLAN respektovat:

* **Server je autoritativní zdroj dat.** Offline zapisovatel je pouze jeden — desktop. Konflikt vzniká jen
  mezi desktopem pracujícím offline a změnami provedenými z tenkého klienta (zmírňuje R-2).
* **End-to-end šifrování je vyloučeno.** Tenký klient vyžaduje, aby server obsah zpřístupnil, takže ochrana
  dat bude na úrovni serveru a úložiště, nikoliv end-to-end (rozplétá R-1). Fulltext, serverová AI i webový
  klient tím zůstávají možné.
* **Fronta offline zápisů na tenkém klientovi** je úzká výjimka z „žádná lokální databáze“: tenký klient musí
  umět bez spojení vytvořit nebo dopsat poznámku, uložit ji lokálně a po obnovení spojení odeslat. Nezahrnuje
  offline čtení celé databáze, merge ani historii verzí. Bez této výjimky selhává hlavní scénář rychlého
  zachycení na cestách.
  **Fronta není databáze.** Je to seznam čekajících zápisových operací, nikoliv kopie datového modelu — bez
  schématu, dotazů, indexů, fulltextu a verzování. Jakmile by tenký klient dostal skutečnou lokální databázi,
  stane se druhým offline zapisovatelem a vrátí se s ním merge, verzování i obousměrná synchronizace, tedy
  přesně ta složitost, kterou tato topologie odstraňuje. Rozšíření tenkého klienta na plnohodnotný je proto
  samostatné rozhodnutí `D-n` s vlastní cenou, ne přirozené pokračování fronty.
* **Tenkost je vlastnost dat, nikoliv obalu.** Tabulka výše rozhoduje, kde jsou data. Nerozhoduje, zda tenký
  klient běží v prohlížeči, nebo jako nainstalovaná aplikace s nativním shellem. Nativní shell na mobilu
  nepřidává lokální databázi — přidává systémové notifikace, sdílení do aplikace, rychlý start a přístup
  k fotoaparátu a galerii. PLAN musí tuto možnost nechat otevřenou, viz kandidáty v § 7.
* **Omezení tenkého klienta na mobilu** — pomalejší start webové aplikace a slabé možnosti systémových
  notifikací (na iOS jen po instalaci na plochu) jsou vědomé kompromisy; patří do SPEC jako NFR, ne do rizik.

## 3. Mimo rozsah

**Mimo rozsah produktu** (koncept 21, 30):

* sdílení a spolupráce mezi uživateli, ACL model — multi-user znamená pouze více nezávislých účtů;
* náhrada Notion, Evernote, Confluence, Jira, obecného project-managementu nebo issue trackeru;
* hluboká hierarchie složek a znalostí;
* WIP limits a swimlanes v Kanbanu (možné rozšíření, ne požadavek první verze).

**Mimo rozsah tohoto enginu:**

* implementační detail — pojmenování, tvar polí, formátování, lokální idiom;
* dekompozice na úkoly a jejich pořadí (samostatné běhy);
* vytvoření architecture profilu pro nový projekt (navazující krok po PLAN);
* `keep.png` jako vizuální vzor — vizuální sample je pod hranicí tohoto enginu.

## 4. Počáteční rizika

| ID | Riziko | Dopad |
|---|---|---|
| R-1 | **Šifrování v přímém střetu s fulltextem, serverovou AI a webovým klientem.** Koncept 28 požaduje obojí současně. _Zmírněno § 2.1: tenký klient vylučuje end-to-end šifrování, rozpor je tím rozhodnut ve prospěch ochrany na serveru a v úložišti._ | Zbývá určit konkrétní model (storage vs. aplikační vs. per-user) v PLAN. |
| R-2 | **Merge bohatého obsahu bez tiché ztráty změn** (koncept 18). Volba mezi CRDT, operační transformací a tříbodovým merge svazuje formát obsahu, schéma databáze i sync protokol. _Zmírněno § 2.1: jediný offline zapisovatel je desktop, plný CRDT nemusí být nutný._ | Špatná volba se opravuje přepsáním datové vrstvy. |
| R-3 | **Reprezentace obsahu** (koncept 10) je uzel, do kterého ústí editor, technický obsah, merge, verzování, export i AI. Každý z nich na ni klade jiné požadavky. | Změna formátu později znamená migraci všech dat. |
| R-4 | **Velikost rozsahu.** Koncept popisuje produkt na roky práce (čtyři platformy, sync, verzování, šifrování, AI, dva moduly). Bez rozdělení na inkrementy nebude co implementovat. | Bez rozhodnutí o MVP nelze smysluplně vyrobit TASKS. |
| R-5 | **Multiplatformnost od začátku** při jediném vývojáři. Volba stacku určuje, kolik kódu se mezi desktopem, webem a mobilem sdílí, a je prakticky nevratná. | Špatná volba znamená psát aplikaci dvakrát. |
| R-6 | **AI nad osobní znalostní bází** (koncept 11.3) implicitně vyžaduje indexaci obsahu a odesílání dat k providerovi. Naráží na R-1 a na požadavek vlastnictví dat. | Může vynutit lokální modely nebo omezení funkce. |
| R-7 | **Historie verzí bohatého obsahu** roste rychle a na klientovi s SQLite má limity (koncept 19). | Ovlivňuje retenci, sync i velikost lokální DB. |
| R-8 | **Editor je sám o sobě velký projekt.** Tabulky, obrázky v toku, code blocky, diagramy a checklisty jsou funkce, které se nepíšou od nuly. | Závislost na konkrétním editor enginu je architektonické rozhodnutí, ne detail. |

## 5. Otevřené neznámé

| ID | Fáze | Mezera | Dopad | Podmínka vyřešení |
|---|---|---|---|---|
| O-1 | FRAME | Čtvrté kritérium úspěchu (koncept 31) je useknuté. | Chybí část měřítka úspěchu pro SPEC. | Operátor doplní, nebo potvrdí, že tři kritéria stačí. |
| O-2 | FRAME | Není určen technologický stack — desktop framework, jazyk a runtime serveru, editor engine. | Blokuje PLAN. Bez něj nelze navrhnout komponenty. | Rozhodnutí operátora v PLAN jako D-n, na základě variant s tradeoffy. |
| O-3 | FRAME | Není znám provozní model serveru — vlastní hosting, cloud, počet uživatelů a instancí. | Ovlivňuje bezpečnostní model, sync, backup i náklady. | Operátor upřesní před SPEC (NFR). |
| O-4 | FRAME | Není určen rozsah první verze (MVP) ani pořadí inkrementů. | Bez něj nelze udělat smysluplné TASKS; viz R-4. | Rozhodnutí operátora na konci PLAN. |
| O-5 | FRAME | Není určena hranice mezi tím, co dělá AI lokálně, a co se odesílá providerovi. | Ovlivňuje R-1, R-6 a bezpečnostní model. | Rozhodnutí v SPEC (NFR) nebo PLAN jako D-n. |
| O-6 | FRAME | ~~Není určeno, zda je webový klient rovnocenný klient, nebo jen omezený náhled.~~ **VYŘEŠENO 2026-09-15** rozhodnutím operátora, viz § 2.1: tenký klient proti REST bez lokální databáze. | — | — |
| O-7 | FRAME | Není určen rozsah fronty offline zápisů na tenkém klientovi — pouze nové poznámky, nebo i úpravy existujících, a jak se chová při konfliktu na serveru. | Určuje složitost tenkého klienta a chování při kolizi; malý rozsah je zde přednost. | Rozhodnutí v SPEC nebo PLAN. |

## 6. Doporučený další směr

Pokračovat fází ANALYSIS. V režimu `CONCEPTUAL` a `GREENFIELD` nemá analýza co číst v kódu, proto se zaměří na:

1. **rozpor mezi požadavky konceptu** — především šifrování vs. fulltext vs. serverová AI vs. webový klient
   (R-1), a merge vs. formát obsahu vs. verzování (R-2, R-3);
2. **závislosti mezi požadavky** — co čemu předchází a co se vzájemně vylučuje;
3. **rozhodovací body**, které musí PLAN vyřešit, s podklady pro varianty;
4. **podklad pro rozdělení na inkrementy** (O-4), aby TASKS mohly vzniknout po částech.

Analýza nebude navrhovat architekturu ani vybírat technologie — to patří do PLAN.

## 7. Zaznamenané kandidáty rozhodnutí

Nejde o rozhodnutí. Jde o podněty operátora, které PLAN musí vyhodnotit jako varianty s tradeoffy.

| Kandidát | K čemu se váže | Zdroj |
|---|---|---|
| Postavit klienty na .NET / Blazor (WebAssembly, případně Blazor Hybrid pro desktop a mobil), sdílet Razor komponenty mezi platformami a mít stejný jazyk na klientu i serveru. Uváděný důvod: pokrytí více platforem jedním řešením a existující zkušenost operátora. | O-2 (stack), R-5 (multiplatformnost) | operátor, 2026-09-15 |
| Windows desktop jako nativní plnohodnotný klient (nativní shell s vlastní databází), tenký klient jako Blazor WebAssembly pro web, mobil a tablet, se sdílenými Razor komponentami mezi oběma. | O-2 (stack), § 2.1 | operátor, 2026-09-15 |
| Úložiště fronty offline zápisů v prohlížeči — IndexedDB přes JS interop jako nejlehčí varianta; SQLite ve WebAssembly nad OPFS je pro tento rozsah nepřiměřené a otevírá cestu k druhému plnému klientovi. | O-7 | analýza, 2026-09-15 |
| UI jako sdílená knihovna Razor komponent pro všechny shelly (Windows desktop, prohlížeč, případně MAUI Blazor Hybrid na mobilu a tabletu), s platformními službami — úložiště fronty, notifikace, sdílení, výběr souboru, schránka — výhradně za rozhraními. Bez tohoto oddělení prorostou volání prohlížeče do komponent a nativní mobilní shell se později dělá přepsáním. | O-2, R-5, § 2.1 | analýza, 2026-09-15 |
| Mobil a tablet postupně: nejdřív PWA nad WASM klientem (nulová práce navíc), nativní MAUI shell až podle potřeby, Android dřív než iOS (iOS vyžaduje Mac pro build a podpis a placený vývojářský účet). | O-2, O-4 (pořadí inkrementů) | analýza, 2026-09-15 |
| Kanban na mobilu omezit na čtení a lehké úpravy; drag & drop náročný na gesta zůstává doménou desktopu, protože WebView na telefonu nedosáhne nativního pocitu. | § 2.1, koncept 12.5 | analýza, 2026-09-15 |
| Editor a případný merge algoritmus jako JS ostrov (TipTap / ProseMirror, případně Yjs) obalený přes JS interop — v C# pro tuto oblast není srovnatelně zralá knihovna. | O-2, R-2, R-8 | analýza, 2026-09-15 |

