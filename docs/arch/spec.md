# spec.md — Personal Notes & Workflow

```text
Phase: SPEC          Run mode: CONCEPTUAL          Architecture basis: greenfield
Output language: czech
Open items: O-2, O-3, O-4, O-5          Carried assumptions: A-1, A-2, A-3, A-4, A-5
Last approved gate: SPEC->PLAN @ 2026-09-15 21:31
```

Zdroje: `concept.md` (dále „koncept“), `frame.md`, `analysis.md`.
Každý požadavek uvádí svůj zdroj. Požadavky, které vycházejí z předpokladu, nesou jeho `A-n`.

Tento dokument říká **co** má systém dělat. Neříká, jak — technologie, formáty, algoritmy a struktura
komponent patří do `plan.md`.

---

## 1. Cíle

| ID | Cíl | Zdroj |
|---|---|---|
| G-1 | Zachytit jednoduchou myšlenku stejně rychle jako v Google Keep. | koncept 4, 31.1 |
| G-2 | Pojmout rozsáhlou technickou poznámku s kódem, diagramy, obrázky a formátováním ve stejném nástroji. | koncept 4, 9, 31.3 |
| G-3 | Nabídnout pokročilé funkce, aniž by komplikovaly jednoduché použití. | koncept 5.2, 31.2 |
| G-4 | Umožnit plnohodnotnou práci na desktopu bez připojení k internetu. | koncept 15, frame 2.1 |
| G-5 | Mít stejná data dostupná na cestách z webu, mobilu a tabletu. | koncept 14, frame 2.1 |
| G-6 | Integrovat AI jako součást práce s obsahem, bez závislosti na jednom providerovi. | koncept 11 |
| G-7 | Řídit vlastní projekty jednoduchým Kanbanem odděleným od poznámek. | koncept 12 |
| G-8 | Zajistit, aby uživatel vlastnil svá data a nikdy tiše nepřišel o změnu. | koncept 18, 25 |

## 2. Non-goals

* Sdílení a spolupráce mezi uživateli, ACL model. Multi-user znamená pouze více nezávislých účtů. (koncept 21)
* Náhrada Notion, Evernote, Confluence, Jira, obecného project-managementu nebo issue trackeru. (koncept 30)
* Hierarchie kategorií hlubší než jedna úroveň. (koncept 7.1, 30)
* Blokový model editoru typu Notion. (koncept 8)
* Import dat z Google Keep. (analysis 7, O-8)
* Tenký klient jako plnohodnotný offline klient s vlastní databází. (frame 2.1)
* End-to-end šifrování. (frame 2.1, analysis K-1)
* Šifrování lokálních dat desktopu aplikací. (analysis 7, K-1)
* Skloňování ve vyhledávání v první verzi. (analysis 7, K-6)
* WIP limits a swimlanes v Kanbanu. (koncept 12.5)
* Developer metadata karet (typ task/bug/feature, reference na branch, commit, PR, Jira). Zmíněno v konceptu
  12.4 jako budoucí rozšíření; tato specifikace je nepožaduje. (koncept 12.4)

## 3. Hranice rozsahu

### 3.1 Moduly

| Modul | Obsah | Zdroj |
|---|---|---|
| Notes | poznámky, kategorie, štítky, editor, technický obsah, verze | koncept 6, 7, 8, 9, 19 |
| Projects / Kanban | projekty, boardy, sloupce, karty | koncept 6, 12 |
| Průřezové | vyhledávání, AI, identita, synchronizace, připomínky, export, audit, backup | koncept 11, 16–29 |

### 3.2 Klienti

| Klient | Role | Offline |
|---|---|---|
| Windows desktop | hlavní, plnohodnotný | plná funkčnost kromě výjimek v NFR-3 |
| Web, mobil, tablet | tenký, doplňkový, pro práci na cestách | pouze fronta zápisů podle FR-31 |

Zdroj: frame 2.1.

---

## 4. Funkční požadavky

### Notes

#### FR-1 — Rychlé vytvoření poznámky
**Zdroj:** koncept 4, 5.2, 31.1 · **Nese:** A-3

Uživatel vytvoří poznámku napsáním textu a jejím uložením, bez povinného výběru jakýchkoliv metadat.

**Akceptace:**
1. Poznámku jde vytvořit v posloupnosti: otevřít aplikaci → napsat text → uložit. Žádný krok navíc není povinný.
2. Aplikace během vytvoření nevyžaduje název, kategorii, štítek, projekt, typ, šablonu ani barvu.
3. Každá nově vytvořená poznámka je zařazena do výchozí kategorie, bez ohledu na to, odkud byla vytvořena;
   uživatel ji může přesunout později (A-3).

#### FR-2 — Vlastnosti poznámky
**Zdroj:** koncept 7.2

Poznámka má: volitelný název, obsah, právě jednu kategorii, nula až více štítků, barvu karty, příznak
připnutí, volitelnou připomínku, stav (aktivní / archivovaná / v koši) a historii verzí.

**Akceptace:**
1. Poznámku bez názvu jde uložit a zobrazit.
2. Připnuté poznámky se v přehledu zobrazují před nepřipnutými.
3. Každou z uvedených vlastností jde nastavit i zrušit po vytvoření poznámky.

#### FR-3 — Kategorie
**Zdroj:** koncept 7.1 · **Nese:** A-3

Uživatel spravuje kategorie v jedné úrovni. Poznámka patří právě do jedné kategorie.

**Akceptace:**
1. Kategorii jde vytvořit, přejmenovat a smazat.
2. Kategorii nejde vnořit do jiné kategorie.
3. Poznámky mazané kategorie se přesunou do výchozí kategorie; nesmažou se.
4. Výchozí kategorii nejde smazat (A-3).

#### FR-4 — Štítky
**Zdroj:** koncept 7.1

Uživatel přiřazuje poznámce libovolný počet štítků. Štítek je nezávislý na kategorii.

**Akceptace:**
1. Stejný štítek jde přiřadit poznámkám v různých kategoriích.
2. Filtr podle štítku zobrazí poznámky ze všech kategorií.
3. Smazání štítku odebere štítek z poznámek, poznámky zachová.

#### FR-5 — Archiv a koš
**Zdroj:** koncept 7.2

Uživatel poznámku archivuje, přesune do koše a obnoví z obou.

**Akceptace:**
1. Archivovaná poznámka se nezobrazuje v běžném přehledu a je dohledatelná v archivu.
2. Poznámka v koši se nezobrazuje v přehledu ani v archivu a jde ji obnovit.
3. Trvalé odstranění poznámky z koše vyžaduje explicitní akci uživatele.

#### FR-6 — Historie verzí poznámky
**Zdroj:** koncept 19, analysis 7 (K-2)

Systém uchovává historii verzí poznámky. Uživatel si ji zobrazí, otevře starší verzi, porovná dvě verze
a obnoví starší stav.

**Akceptace:**
1. Uživatel vidí seznam verzí poznámky s časem vzniku a zdrojem změny (klient nebo AI).
2. Uživatel otevře libovolnou starší verzi pouze pro čtení.
3. Uživatel porovná dvě verze a vidí přidaný, odebraný a změněný obsah.
4. Obnovení starší verze vytvoří novou verzi; stávající historie zůstane zachována.

### Editor a obsah

#### FR-7 — Editor souvislého dokumentu a formátování
**Zdroj:** koncept 8, 8.1

Obsah poznámky je souvislý dokument. Editor podporuje: nadpisy, tučné písmo, kurzívu, podtržení,
přeškrtnutí, odrážkový seznam, číslovaný seznam, checklist, odkazy, tabulky, zvýraznění, barvu textu a barvu
pozadí textu.

**Akceptace:**
1. Každý uvedený prvek jde vložit, upravit a odstranit.
2. Checklist v obsahu umožňuje položku označit a odznačit přímo v zobrazení poznámky.
3. Po uložení, zavření a opětovném otevření poznámky jsou všechny prvky zachovány beze změny.

#### FR-8 — Šířka editoru
**Zdroj:** koncept 8

Na desktopu uživatel přepíná šířku editoru mezi standardní, rozšířenou a plnou. Na úzkém displeji se
dokument zobrazí v jeho šířce.

**Akceptace:**
1. Desktop nabízí tři režimy šířky a zvolený režim se projeví okamžitě.
2. Na úzkém displeji se obsah nezobrazuje s vodorovným posunem celé stránky; výjimku tvoří tabulky a code
   blocky, které se posouvají samostatně.

#### FR-9 — Obrázky v toku dokumentu
**Zdroj:** koncept 8.2, analysis K-9

Uživatel vloží obrázek na libovolné místo dokumentu. Obrázek je součástí toku textu, ne příloha.

**Akceptace:**
1. Obrázek se zobrazí na místě vložení mezi odstavci.
2. Obrázek jde přesunout, odstranit a nahradit.
3. Obrázek je dostupný na všech klientech uživatele.
4. Na desktopu je obrázek dostupný offline, pokud byl na desktop synchronizován.

#### FR-10 — Code
**Zdroj:** koncept 9.1

Editor podporuje inline code a code blocky. Code block má zvýraznění syntaxe, automatické rozpoznání jazyka,
ruční volbu jazyka, automatické formátování kódu a kopírování obsahu.

**Akceptace:**
1. Code block bez zvoleného jazyka má jazyk rozpoznaný automaticky; ruční volba má přednost.
2. Automatické formátování se provede na explicitní akci uživatele, nikoliv samovolně.
3. Kopírování zkopíruje obsah code blocku bez formátování editoru.

#### FR-11 — Diagramy
**Zdroj:** koncept 9.2

Editor podporuje diagramy Mermaid a PlantUML. Každý diagram přepíná mezi zdrojem a náhledem. Zdroj zůstává
uložen a editovatelný.

**Akceptace:**
1. Diagram jde přepnout ze zdroje do náhledu a zpět bez ztráty zdroje.
2. Neplatný zdroj diagramu zobrazí chybu v náhledu a zdroj zachová.
3. Exportovaná poznámka obsahuje zdroj diagramu.

#### FR-12 — Export dat
**Zdroj:** koncept 10, 25, analysis 7 (K-3)

Uživatel exportuje kompletně všechna svá data. Export poskytuje formát bezeztrátový a formát čitelný.
Markdown je čitelný formát a je z principu ztrátový.

**Akceptace:**
1. Export zahrnuje poznámky včetně archivu a koše, kategorie, štítky, projekty, boardy, karty a obrázky.
2. Bezeztrátový export zachová všechny prvky obsahu z FR-7, FR-9, FR-10 a FR-11.
3. Markdown export je čitelný bez aplikace; prvky, které Markdown neumí, jsou vynechány nebo zjednodušeny.
4. Žádný exportní formát nevyžaduje ke čtení tuto aplikaci.

### AI

#### FR-13 — AI nad vybraným textem
**Zdroj:** koncept 11.1

Nad výběrem v poznámce uživatel spustí: přeformulování, opravu, zkrácení, rozšíření, překlad, vysvětlení,
změnu stylu, vytvoření struktury, vytvoření checklistu.

**Akceptace:**
1. Operace pracuje pouze s vybraným textem.
2. Výsledek, který mění obsah, uživatel před použitím vidí a potvrdí, nebo zamítne.
3. Použitý výsledek vytvoří novou verzi poznámky se zdrojem změny AI (FR-6).

#### FR-14 — AI nad poznámkou
**Zdroj:** koncept 7.2, 11.2

Nad celou poznámkou uživatel spustí: vytvoření nebo aktualizaci názvu, shrnutí, návrh štítků, změnu struktury,
vytvoření checklistu, generování diagramu, úpravu kódu a vysvětlení kódu.

**Akceptace:**
1. Operace měnící obsah nebo metadata se před použitím zobrazí k potvrzení.
2. Použitá změna obsahu vytvoří novou verzi se zdrojem změny AI.
3. Operace, které obsah nemění (shrnutí, vysvětlení), jej nemění.

#### FR-15 — AI napříč poznámkami
**Zdroj:** koncept 11.3

Uživatel položí dotaz nad svými poznámkami a AI odpoví na základě jejich obsahu.

**Akceptace:**
1. Odpověď uvádí poznámky, ze kterých vychází, a uživatel je z odpovědi otevře.
2. AI pracuje pouze s daty přihlášeného uživatele.

#### FR-16 — AI provideři
**Zdroj:** koncept 11.4, analysis 7 (K-5)

Uživatel konfiguruje AI providery, cloudové i lokální, a volí, kterého použít.

**Akceptace:**
1. Systém podporuje alespoň OpenAI, Anthropic a lokálního providera kompatibilního s LM Studio.
2. Změna providera nevyžaduje změnu dat ani ztrátu historie.
3. S lokálním providerem na desktopu jsou AI operace dostupné bez připojení k internetu.

### Projects / Kanban

#### FR-17 — Projekty
**Zdroj:** koncept 12.1

Uživatel vytváří, přejmenovává, archivuje a maže projekty.

**Akceptace:**
1. Uživatel má libovolný počet projektů.
2. Archivovaný projekt se nezobrazuje v běžném přehledu a jde obnovit.

#### FR-18 — Board a sloupce
**Zdroj:** koncept 12.2, 12.5

Projekt má jeden nebo více boardů. Nový board má sloupce Todo, In Progress a Done. Uživatel sloupce přidává,
přejmenovává, přeřazuje a odebírá.

**Akceptace:**
1. Nový board obsahuje tři výchozí sloupce.
2. Odebrání sloupce s kartami vyžaduje určit, kam se karty přesunou.
3. Projekt podporuje více boardů.

#### FR-19 — Karta
**Zdroj:** koncept 12.3

Karta má: název, popis, projekt, stav (sloupec), štítky, prioritu, připomínku, termín, checklist, komentáře,
historii změn a stav archivace.

**Akceptace:**
1. Kartu jde vytvořit pouze s názvem.
2. Historie změn karty zaznamenává změnu stavu, termínu a priority s časem.
3. Archivovaná karta se nezobrazuje na boardu a je dohledatelná v archivu.

#### FR-20 — Práce s boardem na desktopu
**Zdroj:** koncept 12.5

Na desktopu uživatel přesouvá karty tažením mezi sloupci i v rámci sloupce, filtruje karty, pracuje s archivem
a backlogem.

**Akceptace:**
1. Karta přesunutá tažením změní sloupec i pořadí a změna se uloží.
2. Filtr podle štítku, priority a termínu omezí zobrazené karty.
3. Backlog je sloupec, který jde na boardu skrýt.

#### FR-21 — Kanban na tenkém klientovi
**Zdroj:** frame 7 · **Nese:** A-5

Na tenkém klientovi uživatel board čte a provádí lehké úpravy karet.

**Akceptace:**
1. Uživatel zobrazí board a detail karty.
2. Uživatel změní stav karty, upraví text, checklist a termín.
3. Přesun karet tažením na tenkém klientovi není požadován.

#### FR-22 — AI v Kanbanu
**Zdroj:** koncept 13

Uživatel spustí: rozpad karty na checklist, návrh dalších kroků, vytvoření dalších karet, shrnutí projektu,
shrnutí boardu, identifikaci blokátorů a analýzu otevřených úkolů.

**Akceptace:**
1. Operace vytvářející nebo měnící karty se před použitím zobrazí k potvrzení.
2. Shrnutí a analýzy data nemění.

#### FR-23 — Karta z poznámky
**Zdroj:** koncept 13, analysis K-4

Uživatel vytvoří kartu z poznámky, ručně nebo pomocí AI. Karta nese odkaz na poznámku.

**Akceptace:**
1. Z karty jde otevřít zdrojovou poznámku a z poznámky kartu.
2. Smazání poznámky kartu nesmaže; odkaz se zobrazí jako nedostupný.
3. Poznámka a karta zůstávají samostatné objekty; úprava jedné nemění druhou.

### Vyhledávání

#### FR-24 — Jednotné vyhledávání
**Zdroj:** koncept 22, analysis 7 (K-6, O-9)

Uživatel vyhledává jedním dotazem napříč poznámkami, projekty a kartami.

**Akceptace:**
1. Výsledky obsahují poznámky, projekty i karty a jsou rozlišené podle typu.
2. Dotaz `certifikat` najde text `Certifikát`.
3. Dotaz `certif` najde text `certifikátu`.
4. Stejný dotaz nad stejnými synchronizovanými daty vrací na desktopu i na tenkém klientovi stejné výsledky.

#### FR-25 — AI nad výsledky vyhledávání
**Zdroj:** koncept 22

Uživatel předá výsledky vyhledávání AI k další práci.

**Akceptace:**
1. AI pracuje pouze s položkami z výsledků, které uživatel předal.

### Klienti a synchronizace

#### FR-26 — Desktop offline
**Zdroj:** koncept 15, frame 2.1

Desktop poskytuje veškerou funkčnost bez připojení k internetu, s výjimkami uvedenými v NFR-3.

**Akceptace:**
1. Po odpojení od sítě jde vytvářet, upravovat, mazat a vyhledávat poznámky a karty.
2. Aplikace se spustí a zobrazí data bez připojení k internetu, i když vypršelo přihlášení.

#### FR-27 — Automatická synchronizace desktopu
**Zdroj:** koncept 17

Po obnovení spojení desktop automaticky synchronizuje změny se serverem v obou směrech.

**Akceptace:**
1. Změny provedené offline se po připojení odešlou bez akce uživatele.
2. Změny provedené na tenkém klientovi se objeví na desktopu bez akce uživatele.
3. Uživatel vidí stav synchronizace (synchronizováno / čeká / konflikt / chyba).

#### FR-28 — Slučování změn a konflikty
**Zdroj:** koncept 18, analysis 7 (K-2)

Souběžné kompatibilní změny systém sloučí automaticky. Neslučitelné změny téže části obsahu systém zachová
obě, označí konflikt a nechá rozhodnout uživatele.

**Akceptace:**
1. Změna odstavce A na desktopu a odstavce B na tenkém klientovi vede ke sloučené verzi s oběma změnami.
2. Změna téhož odstavce na obou stranách vede ke konfliktu; obě varianty jsou uložené a viditelné.
3. Uživatel konflikt vyřeší výběrem jedné varianty nebo ruční úpravou; vyřešení vytvoří novou verzi.
4. Úprava poznámky na jedné straně a její smazání na druhé nevede ke ztrátě úpravy.

#### FR-29 — Tenký klient
**Zdroj:** frame 2.1

Web, mobil a tablet pracují online proti serveru bez vlastní databáze.

**Akceptace:**
1. Tenký klient zobrazuje a upravuje data uživatele, včetně funkcí Notes, vyhledávání a AI.
2. Změna provedená na tenkém klientovi je okamžitě uložena na serveru.

#### FR-30 — Fronta zápisů na tenkém klientovi
**Zdroj:** frame 2.1, analysis O-7 · **Nese:** A-4

Bez připojení uživatel na tenkém klientovi vytvoří novou poznámku. Poznámka čeká ve frontě a po obnovení spojení
se odešle.

**Akceptace:**
1. Bez připojení jde napsat a uložit novou poznámku.
2. Uživatel vidí, že poznámka čeká na odeslání.
3. Po obnovení spojení se poznámka odešle bez akce uživatele a zmizí z fronty.
4. Zavření a opětovné otevření klienta frontu nevymaže.
5. Úprava existujících poznámek bez připojení není požadována (A-4).

### Identita

#### FR-31 — Oddělení uživatelů
**Zdroj:** koncept 20, 21

Každý uživatel má vlastní poznámky, štítky, kategorie, projekty, boardy, karty a nastavení.

**Akceptace:**
1. Uživatel nevidí ani nevyhledá data jiného uživatele žádnou cestou, včetně AI a exportu.

#### FR-32 — Přihlášení
**Zdroj:** koncept 20

Uživatel se přihlásí účtem Google. Další poskytovatelé identity jdou přidat později.

**Akceptace:**
1. Uživatel se přihlásí účtem Google na desktopu i na tenkém klientovi.
2. Přidání dalšího poskytovatele nevyžaduje změnu uložených dat uživatelů.

### Připomínky

#### FR-33 — Připomínky a notifikace
**Zdroj:** koncept 23, analysis K-8

Poznámka i karta mají připomínku. V čase připomínky systém zobrazí systémovou notifikaci na desktopu a na mobilu.

**Akceptace:**
1. Připomínka nastavená na libovolném klientovi se zobrazí na desktopu i na mobilu.
2. Jedna připomínka nevyvolá na stejném zařízení více notifikací.
3. Připomínka na desktopu funguje i bez připojení, pokud byla na desktop synchronizována.
4. Omezení notifikací na mobilu jsou uvedena v NFR-17.

### UI

#### FR-34 — Světlý a tmavý režim
**Zdroj:** koncept 24

**Akceptace:**
1. Uživatel přepne mezi světlým a tmavým režimem na všech klientech.

### Provoz serveru

#### FR-35 — Audit
**Zdroj:** koncept 29

Systém zaznamenává auditní události: autentizaci, změny identity, přístup k datům, vytvoření, změnu, smazání,
obnovení, synchronizaci, export, bezpečnostní události a administrativní zásahy.

**Akceptace:**
1. Každá uvedená operace vytvoří auditní záznam s časem, uživatelem, typem operace a dotčeným objektem.
2. Auditní záznamy jsou dohledatelné podle uživatele, času a typu operace.
3. Auditní záznamy jsou uloženy odděleně od technických logů.

#### FR-36 — Backup a obnova
**Zdroj:** koncept 26

Server pravidelně zálohuje data, uchovává více verzí záloh a umožňuje obnovu systému.

**Akceptace:**
1. Zálohy vznikají automaticky bez zásahu uživatele.
2. Z libovolné uchované zálohy jde obnovit funkční systém.
3. Obnova ze zálohy je ověřena alespoň jedním zkušebním obnovením.

#### FR-37 — Kompatibilita verzí klienta
**Zdroj:** analysis 7 (O-10)

Server rozpozná nekompatibilní verzi desktopu a synchronizaci odmítne s výzvou k aktualizaci.

**Akceptace:**
1. Desktop pod minimální podporovanou verzí nesynchronizuje a uživatel dostane výzvu k aktualizaci.
2. Odmítnutá synchronizace neztratí lokální změny; po aktualizaci se odešlou.

---

## 5. Nefunkční požadavky

| ID | Požadavek | Zdroj | Nese |
|---|---|---|---|
| NFR-1 | **Rychlost zachycení.** Vytvoření jednoduché poznámky nevyžaduje víc kroků než Google Keep a žádný povinný dialog. | koncept 4, 31.1 | |
| NFR-2 | **Progressive complexity.** Žádná pokročilá funkce nevyžaduje interakci uživatele při jednoduchém použití. | koncept 5.2, 31.2 | |
| NFR-3 | **Offline na desktopu.** Veškerá funkčnost desktopu je dostupná bez internetu, s výjimkou: AI s cloudovým providerem, synchronizace, přihlášení nového uživatele. | koncept 15, analysis K-5 | |
| NFR-4 | **Žádná tichá ztráta změny.** Žádná kombinace souběžných úprav, smazání a offline práce nesmí vést ke ztrátě změny bez vědomí uživatele. | koncept 18 | |
| NFR-5 | **Normalizace vyhledávání.** Vyhledávání nerozlišuje velikost písmen, diakritiku ani různý Unicode zápis téhož znaku, hledá podle začátku slova a chová se na všech klientech shodně. Skloňování není požadováno. | analysis 7 (K-6, O-9) | |
| NFR-6 | **Aktuálnost mezi klienty.** Rozdíl ve výsledcích mezi klienty daný dosud neproběhlou synchronizací je přijatelný. | analysis 7 (K-6) | |
| NFR-7 | **Objemy.** Systém je dimenzován na: jednotky uživatelů; do 50 000 poznámek na uživatele včetně archivu a koše, verze se počítají zvlášť; poznámku typicky do 100 kB a výjimečně do 1 MB; obrázek do 10 MB a celkem jednotky GB na uživatele; stovky až nízké tisíce karet. Jde o horní hranici pro návrh, ne očekávaný běžný stav. | analysis 7 (O-11) | A-2 |
| NFR-8 | **Bezpečnost.** TLS, bezpečná autentizace, autorizace vynucená na serveru, izolace dat uživatelů, bezpečné uložení tajných údajů, ochrana API a zabezpečená synchronizace. | koncept 27 | |
| NFR-9 | **Šifrování.** Data jsou šifrovaná při přenosu, na serveru a v zálohách. End-to-end šifrování se nepoužívá. Lokální data desktopu aplikace nešifruje; jejich ochrana při ztrátě zařízení je ponechána šifrování disku operačním systémem. | koncept 28, frame 2.1, analysis 7 (K-1) | |
| NFR-10 | **Audit oddělený od logů.** Audit je strukturovaný, dohledatelný a oddělený od technického logování. | koncept 29 | |
| NFR-11 | **Nezávislost na AI providerovi.** Žádná funkce není vázaná na konkrétního providera. | koncept 11.4 | |
| NFR-12 | **Transparentnost AI.** Uživatel vždy ví, zda operace odešle obsah externímu providerovi, a jaký obsah. | koncept 25, frame O-5 | |
| NFR-13 | **Rozšiřitelnost diagramů.** Přidání dalšího typu diagramu nevyžaduje změnu uložených dat. | koncept 9.2 | |
| NFR-14 | **Nezávislost identity.** Data ani server nejsou vázané na Google jako jediného poskytovatele identity. | koncept 20 | |
| NFR-15 | **Připravenost na mobil a tablet.** Mobilní a tabletový klient jdou dodat bez přepsání uživatelského rozhraní a bez změny serveru, včetně varianty nainstalované aplikace. | koncept 14, frame 2.1, frame 7 | |
| NFR-16 | **Oddělení modulů.** Notes a Projects / Kanban jsou oddělené doménové celky; sdílejí pouze identifikaci objektů, vyhledávání a vzájemné odkazy. | koncept 6, analysis K-4 | |
| NFR-17 | **Vědomá omezení tenkého klienta na mobilu.** Pomalejší start a omezené systémové notifikace (na iOS pouze po instalaci na plochu) jsou přijatelné. | frame 2.1 | |
| NFR-18 | **Verzované rozhraní.** Rozhraní serveru i synchronizace je verzované; změna nesmí rozbít podporovanou verzi klienta. | analysis 7 (O-10) | |
| NFR-19 | **Stejný mentální model.** Klienti používají stejné pojmy, strukturu a navigaci; liší se rozsahem funkcí, ne jejich významem. | koncept 24 | |
| NFR-20 | **Otevřenost exportu.** Exportní formáty jsou otevřené a čitelné bez této aplikace. | koncept 25 | |

---

## 6. Omezení

* Hlavní klient je Windows desktop. (koncept 14)
* Server je autoritativní zdroj dat; jediným offline zapisovatelem je desktop. (frame 2.1)
* Tenký klient nemá vlastní databázi; výjimkou je pouze fronta zápisů podle FR-30. (frame 2.1)
* Konkrétní technologie, formát obsahu, algoritmus slučování, model verzování, model šifrování a model
  plánování připomínek určuje `plan.md`. (koncept 10, 11.4, 18, 23, 27, 28)

## 7. Rizika

| ID | Riziko | Stav po SPEC |
|---|---|---|
| R-1 | Šifrování versus fulltext a AI | Vyřešeno topologií a NFR-9. |
| R-2 | Slučování bohatého obsahu bez ztráty změny | Zmírněno jediným offline zapisovatelem; požadavky FR-28 a NFR-4 zůstávají náročné. |
| R-3 | Reprezentace obsahu jako kořen rozhodnutí | Otevřené, první rozhodnutí v PLAN. |
| R-4 | Velikost rozsahu | Otevřené, rozdělení na inkrementy na konci PLAN (O-4). |
| R-5 | Multiplatformnost | Zmírněno NFR-15; volba stacku v PLAN (O-2). |
| R-6 | AI nad osobní znalostní bází | Zmírněno NFR-12 a lokálními providery (FR-16). |
| R-7 | Objem historie verzí | Zmírněno NFR-7; retence v PLAN. |
| R-8 | Editor jako velký projekt | Otevřené, volba editoru v PLAN. FR-7 až FR-11 z něj dělají nejrozsáhlejší část klienta. |

## 8. Kritéria úspěchu

| ID | Kritérium | Měřeno |
|---|---|---|
| S-1 | Rychlost vytvoření jednoduché poznámky je srovnatelná s Google Keep. | FR-1 akceptace 1–2, NFR-1 |
| S-2 | Běžné použití není komplikováno pokročilými funkcemi. | NFR-2 |
| S-3 | Technická poznámka může obsahovat bohatý obsah. | FR-7 až FR-11 |

Zdroj: koncept 31; operátor potvrdil, že tři kritéria stačí (O-1).

---

## 9. Předpoklady

| ID | Předpoklad | Alternativy | Přijal | Odvozené položky |
|---|---|---|---|---|
| A-1 | Koncept je aktuální a úplný vstup. | Existuje novější zadání nebo neuvedená omezení. | operátor | celý dokument |
| A-2 | Objemy podle NFR-7, s verzemi počítanými zvlášť. | Verze se započítávají do 50 000. | operátor | NFR-7 |
| A-3 | Koncept 5.2 zakazuje vyžadovat výběr kategorie, koncept 7.1 říká, že poznámka patří právě do jedné kategorie. Řešení: existuje nesmazatelná výchozí kategorie; **každá** nově vytvořená poznámka do ní spadne automaticky, bez ohledu na kontext vytvoření, a uživatel ji přesune později. | (a) kategorie je volitelná a poznámka nemusí patřit do žádné; (b) kategorie je povinná a vybírá se při vytvoření; (c) nová poznámka přebírá kategorii z aktuálního pohledu — `REJECTED` operátorem ve prospěch jednotného chování. | operátor, 2026-09-15 | FR-1, FR-3 |
| A-4 | Fronta zápisů na tenkém klientovi pokrývá pouze vytvoření nové poznámky. | (a) také doplnění textu na konec existující poznámky; (b) také úprava existující poznámky, což vyžaduje slučování na tenkém klientovi. | operátor, 2026-09-15 | FR-30 |
| A-5 | Kanban na tenkém klientovi je omezen na čtení a lehké úpravy, bez přesunu tažením. | Plná práce s boardem včetně tažení na všech klientech. | operátor, 2026-09-15 | FR-21 |

## 10. Otevřené položky

| ID | Mezera | Kde se vyřeší |
|---|---|---|
| O-2 | Technologický stack a shelly klientů | PLAN, jako D-n |
| O-3 | Provozní model serveru | PLAN; NFR v tomto dokumentu jsou formulované nezávisle na hostingu |
| O-4 | Rozsah první verze a pořadí inkrementů | konec PLAN |
| O-5 | Konkrétní hranice, které AI operace odesílají obsah mimo zařízení | PLAN; požadavek na transparentnost je NFR-12 |

Uzavřené: O-1 (tři kritéria stačí), O-6, O-8, O-9, O-10, O-11 (A-2). O-7 převedena na navržený předpoklad A-4.
