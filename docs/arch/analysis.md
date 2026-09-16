# analysis.md — Personal Notes & Workflow

```text
Phase: ANALYSIS          Run mode: CONCEPTUAL          Architecture basis: greenfield
Output language: czech
Open items: O-1, O-2, O-3, O-4, O-5, O-7, O-11          Carried assumptions: A-1, A-2 (navrženo)
Last approved gate: FRAME->ANALYSIS @ 2026-09-15 20:41
```

Vstupy: `concept.md`, `frame.md`. Zdrojový kód neexistuje (režim `CONCEPTUAL`, basis `GREENFIELD`).

---

## 1. Současný stav

Neexistuje žádná implementace, repozitář ani architektonický profil. Jediným zdrojem pravdy je koncept
a upřesnění topologie klientů ve `frame.md` § 2.1.

Z toho plyne pro celý zbytek běhu:

* **Nejsou žádné nalezené lokace.** `TRACEABILITY.md` proto neobsahuje žádný řádek `C-n @ cesta` a všechny
  scope hinty v budoucích `tasks.md` budou tvaru `new — C-n`. To je normální stav greenfieldu, ne mezera.
* **Nejsou závazná architektonická pravidla.** Neexistuje profil, vůči kterému by šlo měřit divergenci.
  Vytvoření profilu pro tento projekt je navazující krok po PLAN, mimo rozsah tohoto enginu.
* **Neexistuje nic, co by bylo nutné zachovat.** Všechna rozhodnutí v PLAN jsou volná, ale zároveň
  nevratná ve smyslu ceny pozdější změny — nemají oporu v existujícím kódu, který by je ospravedlnil.

## 2. Rozpory v požadavcích

Rozpor znamená, že dva požadavky konceptu nelze splnit současně v plné míře. Každý musí být vyřešen
nejpozději v PLAN.

### K-1 — Šifrování versus fulltext, AI a tenký klient — VYŘEŠENO

Koncept 28 požaduje šifrování dat na serveru a současně zachování fulltextu, AI nad poznámkami a webového
klienta. End-to-end šifrování tyto tři věci vylučuje. Rozhodnutím o topologii (`frame.md` § 2.1) je E2E ze hry.

**Zbytek k rozhodnutí v PLAN:** zda šifrovat na úrovni úložiště (transparentní, chrání ukradený disk a zálohu),
nebo aplikačně per uživatel (chrání i před správcem databáze, ale komplikuje serverový index a AI).
Otevřená otázka zůstává i pro **lokální databázi desktopu** — ta leží na disku uživatele a koncept se k jejímu
šifrování nevyjadřuje.

### K-2 — Autoritativní zdroj dat versus offline desktop

Server je autoritativní (`frame.md` § 2.1), ale desktop musí být plně použitelný offline (koncept 15). Po dobu
offline práce je autoritativní lokálně a při připojení se dvě historie musí sejít. To znamená, že
**„autoritativní“ nesmí být implementováno jako „server přepíše klienta“** — přesně to by způsobilo tichou
ztrátu změny, kterou koncept 18 zakazuje.

Rozhodnutí do PLAN: model verzování napříč hranicí (vektorové hodiny, sekvence na server, hybrid), a co se
stane s poznámkou, kterou desktop offline upravil a tenký klient mezitím smazal.

### K-3 — Bohatý obsah versus bezeztrátový export do Markdownu

Koncept 10 chce bezeztrátově zachovat rich text, tabulky, barvy textu, zvýraznění, obrázky, diagramy
a checklisty. Koncept 25 chce export do otevřených formátů, „především Markdown“. **Markdown tyto prvky
neumí** — barvy textu, zvýraznění ani background v něm neexistují, tabulky jen v omezené podobě.

Buď je interním formátem něco bohatšího a Markdown je ztrátový export, nebo je interním formátem Markdown
a část požadavků editoru padá. Nelze obojí. Rozhodnutí do PLAN, kandidát: interní strukturovaný dokument,
Markdown jako primární ztrátový export a HTML nebo JSON jako export bezeztrátový.

### K-4 — Oddělené moduly versus AI napříč nimi

Koncept 6 říká, že doménové objekty Notes a Kanbanu zůstávají oddělené. Koncept 13 požaduje „vytvoření tasku
z poznámky“ a koncept 22 jednotné vyhledávání napříč oběma moduly. To vyžaduje **vazbu mezi moduly a společný
vyhledávací index**, tedy alespoň jeden sdílený koncept nad oběma doménami.

Není to neřešitelné, ale PLAN musí explicitně určit, kde ta hranice vede: sdílené identifikátory a index ano,
sdílený doménový model ne.

### K-5 — AI versus práce offline

Koncept 15 říká, že internet nesmí být podmínkou běžného použití. Koncept 11 dělá z AI integrální součást
aplikace. Cloudová AI offline nefunguje. **AI je tedy vědomá výjimka z pravidla „vše funguje offline“**, nebo
musí být na desktopu k dispozici lokální model (koncept 11.4 zmiňuje LM Studio).

Rozhodnutí do SPEC jako NFR: které operace jsou offline nedostupné a jak se to uživateli projeví. Toto je
současně vázáno na O-5 (co opouští zařízení).

### K-6 — Jednotné vyhledávání versus dvě různá úložiště

Koncept 22 požaduje jedno vyhledávání napříč Notes, Projects a kartami. Desktop offline ho ale musí obsloužit
z lokální databáze, zatímco tenký klient ze serveru. To znamená **dvě implementace fulltextu se stejným
chováním** — jinak bude stejný dotaz vracet na desktopu a na mobilu jiné výsledky.

Rozhodnutí do PLAN: buď jedna technologie na obou stranách, nebo explicitně přijatý rozdíl v kvalitě
vyhledávání mezi klienty. Souvisí s O-9 (jazyk a diakritika).

### K-7 — Historie verzí versus synchronizace

Koncept 19 chce historii podobnou Confluence. V kombinaci s offline desktopem vzniká otázka, zda se
synchronizují i mezilehlé verze, nebo jen výsledný stav. Plná synchronizace historie je drahá na objem dat
(riziko R-7), synchronizace pouze výsledku znamená, že historie je na každém klientu jiná.

### K-8 — Připomínky na dvou místech

Koncept 23 požaduje systémovou notifikaci na desktopu i na mobilu. Desktop může být offline a tenký klient
nemusí běžet. Pokud plánuje obojí, uživatel dostane notifikaci dvakrát; pokud jen server, offline desktop
připomínku neukáže. PLAN musí určit jedno místo plánování a pravidlo pro potlačení duplicit.

### K-9 — Obrázky a binární obsah

Koncept 8.2 chce obrázky v toku dokumentu, ale nikde neřeší, kde jsou uloženy, jak se synchronizují, zda jsou
offline dostupné a jak se chovají při exportu. Binární obsah je jiná třída problému než text: nejde efektivně
mergovat, nafukuje zálohy a na tenkém klientovi s pomalým připojením je citelný.

## 3. Mezery v konceptu

Tyto věci koncept neřeší vůbec a SPEC je bude potřebovat.

| ID | Mezera | Proč je to důležité |
|---|---|---|
| O-8 | **Import existujících dat.** Projekt vznikl jako reakce na Google Keep, ale koncept nikde nezmiňuje převzetí stávajících poznámek. | Bez importu je aplikace při prvním spuštění prázdná a Keep poběží dál vedle ní. Ovlivňuje datový model i první inkrement. |
| O-9 | **Jazyk a diakritika ve vyhledávání.** Poznámky budou česky. Naivní fulltext bez normalizace diakritiky a bez skloňování je v češtině znatelně horší. | Určuje volbu vyhledávací technologie na obou stranách (K-6). |
| O-10 | **Aktualizace desktopové aplikace.** Není určeno, jak se nainstalovaný desktop dostane k nové verzi, ani jak se chová při nesouladu verze klienta a serverového API. | Nainstalovaná aplikace bez aktualizačního mechanismu stárne. Nesoulad verzí je při vlastním sync protokolu zdroj tichých chyb. |
| O-11 | **Objemy a výkonnostní cíle.** Kolik poznámek, jak velkých, kolik verzí, jak velké obrázky. | Bez řádového odhadu nelze odpovědně rozhodnout mezi variantami v K-6, K-7 a K-9; NFR by byly nepodložené. |

Dále zůstávají otevřené z FRAME: O-1 (čtvrté kritérium úspěchu), O-2 (stack), O-3 (provozní model serveru),
O-4 (rozsah první verze), O-5 (hranice odesílání dat AI), O-7 (rozsah fronty offline zápisů).

## 4. Nejistoty

* **Tolerance vůči nedokonalému mobilnímu UI.** Rozhodnutí o WebView na mobilu (`frame.md` § 7) je přijatelné
  jen tehdy, pokud jsou hlavní mobilní scénáře čtení, hledání a rychlé zachycení. Pokud má být mobil
  rovnocenným pracovištěm včetně Kanbanu, je volba stacku nedostatečná. Zatím vyhodnoceno jako přijatelné.
* **Jednoúrovňové kategorie** (koncept 7.1) při velkém počtu poznámek. Koncept to označuje jako záměr, ne
  omezení; analýza to nezpochybňuje, ale zaznamenává jako předpoklad k ověření provozem.
* **Rozsah AI napříč poznámkami** (koncept 11.3). Vyhledání a shrnutí nad vlastní bází může znamenat jednoduché
  hledání plus předání textu modelu, nebo plnou vektorovou indexaci. Cena obou se liší řádově a koncept
  nerozlišuje.

## 5. Závislosti mezi rozhodnutími

Pořadí, ve kterém musí PLAN rozhodovat, protože pozdější volby jsou svázány dřívějšími:

```text
1. Reprezentace obsahu (K-3)
      ├─> 2. Model merge a konfliktů (K-2)
      │        └─> 3. Model verzování a jeho synchronizace (K-7)
      ├─> 4. Model vyhledávání (K-6, O-9)
      └─> 5. Úložiště binárního obsahu (K-9)

6. Model šifrování (K-1) ──> omezuje 4. a rozsah serverové AI (K-5)
7. Volba stacku a shellů (O-2) ──> omezuje 1. (editor engine) a 5.
8. Rozsah první verze (O-4) ──> závisí na všech předchozích
```

**Reprezentace obsahu je kořen.** Vstupuje do ní editor, technický obsah, merge, verzování, export i AI.
Rozhodnout ji jako první a vědomě, ne jako vedlejší produkt volby editoru.

## 6. Podklad pro rozdělení na inkrementy

Nejde o rozhodnutí (to je O-4), ale o zjištění, které části jsou oddělitelné:

* **Nezávislé na synchronizaci:** Notes, editor, technický obsah, lokální vyhledávání, kategorie a štítky.
  Celý modul Notes je na desktopu použitelný bez serveru.
* **Vyžaduje server:** identita, synchronizace, tenký klient, serverová AI, audit, backup.
* **Oddělitelný modul:** Projects / Kanban nemá na Notes datovou závislost, jen sdílí vyhledávání a případnou
  vazbu „task z poznámky“ (K-4).
* **Průřezové a nelze odložit do konce:** reprezentace obsahu, identita v datovém modelu, verzování. Dodatečné
  zavedení znamená migraci.

Z toho plyne přirozený řez: **desktop s modulem Notes bez serveru** je nejmenší celek, který má sám o sobě
hodnotu, a přitom vynutí rozhodnutí o kořeni (reprezentace obsahu). Neznamená to, že se sync navrhne až
později — návrh musí být hotový v PLAN, jen se neimplementuje v prvním inkrementu.

## 7. Rozhodnutí operátora k analýze (2026-09-15)

| Položka | Rozhodnutí operátora | Stav | Důsledek pro SPEC / PLAN |
|---|---|---|---|
| K-1 | Lokální databázi desktopu nešifrovat. | VYŘEŠENO | Ochrana lokálních dat při ztrátě zařízení je ponechána na šifrování disku operačním systémem; SPEC to uvede jako vědomé omezení. |
| K-2 | Verzování jako v Confluence, merge změn. | VYŘEŠENO, model do PLAN | Kandidát pro PLAN: lineární verze přidělované serverem; desktop si nese základní verzi; při synchronizaci tříbodový merge (základ, server, desktop) po blocích strukturovaného dokumentu; neslučitelná změna téhož bloku = konflikt se zachováním obou variant. CRDT není nutné, protože offline zapisovatel je jediný. |
| K-3 | Markdown je jeden z exportů a je z principu ztrátový. | VYŘEŠENO | Interní formát je bohatší strukturovaný dokument; SPEC odliší bezeztrátový export od čitelného ztrátového. |
| K-5 | AI podporuje i lokální providery (LM Studio apod.), obdobně jako aplikace Symbolon, kterou operátor uvádí jako vzor. | VYŘEŠENO | S lokálním providerem je AI dostupná offline, cloudoví provideři pouze online. SPEC to uvede jako NFR. |
| K-6 | Není rozpor; zpoždění výsledků vyhledávání mezi klienty daného synchronizací je přijatelné. Operátor se dotázal, zda nestačí Unicode v databázi. | PŘEKLASIFIKOVÁNO na požadavek | Unicode úložiště je nutné, ale nestačí. Desktop i server musí text při indexaci i dotazu shodně (1) normalizovat Unicode zápis, (2) sjednotit velikost písmen, (3) odstranit diakritiku; jinak se liší výsledky, nejen jejich aktuálnost. Kroky (1)–(3) potvrzeny operátorem 2026-09-15 jako závazný požadavek. (4) Skloňování je samostatné rozhodnutí — navrženo: v první verzi nahrazeno hledáním podle začátku slova, plná podpora později. Čeká na potvrzení operátora. |
| O-8 | Import z Google Keep se nepožaduje. | UZAVŘENO | Mimo rozsah. |
| O-9 | Vyhledávání s podporou češtiny a diakritiky je požadováno. | UZAVŘENO | Vstup do volby vyhledávací technologie na obou stranách (K-6). |
| O-10 | Verzované API. | UZAVŘENO, model do PLAN | Kandidát pro PLAN: verze API v adrese, verze synchronizačního protokolu ověřovaná při připojení, minimální podporovaná verze desktopu, pod kterou server synchronizaci odmítne a vyzve k aktualizaci. Mechanismus aktualizace desktopu zůstává k určení v PLAN. |
| O-11 | Operátor souhlasí s tím, že odhad objemů je potřeba. Konkrétní hodnoty nebyly dosud navrženy. | NAVRŽENO jako A-2 | Viz níže; čeká na potvrzení operátora. |

**Navrhovaný předpoklad A-2 (O-11), čeká na potvrzení:**

* uživatelé: jednotky (osobní systém, několik nezávislých účtů);
* poznámky: do 50 000 na uživatele;
* velikost poznámky: typicky do 100 kB textu, výjimečně do 1 MB;
* obrázky: do 10 MB jeden, celkem jednotky GB na uživatele;
* Kanban: desítky projektů, stovky až nízké tisíce karet na uživatele;
* historie verzí: bez pevného limitu počtu, retence určena v PLAN.

Odvozené položky (budou nést `A-2`): všechny NFR na výkon, vyhledávání a objem synchronizace.

## 8. Doporučení pro pokračování

Pokračovat fází SPEC. SPEC musí:

1. převzít topologii klientů z `frame.md` § 2.1 jako závazné omezení, ne jako návrh;
2. pojmenovat výjimky z pravidla „vše funguje offline“ — AI (K-5) a tenký klient — jako explicitní NFR;
3. formulovat požadavek na vyhledávání tak, aby bylo zřejmé, zda se očekává shodné chování na obou klientech
   (K-6);
4. formulovat požadavek na export tak, aby odlišil bezeztrátový a čitelný formát (K-3);
5. uzavřít O-1 a potvrdit A-2 (O-11); O-8 a O-9 jsou uzavřené v § 7;
6. nepředjímat rozhodnutí z § 5 — ta patří do PLAN.
