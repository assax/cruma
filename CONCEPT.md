# Personal Notes & Workflow — koncept

> **Status:** vstupní koncept (nikoliv SPEC). Slouží jako vstup pro architektonický engine,
> který z něj vytvoří `spec.md`, `plan.md` a `tasks.md`.
> Vzniklo vyčištěním původního `spec.md` (escapovaný Markdown, poškozené diagramy).

## 1. Název problému / tématu

**Personal Notes & Workflow**

Pracovní název projektu vychází z původní úvahy vytvořit lepší alternativu ke Google Keep.

Cílem ale není vytvořit pouze klienta Google Keep. Výsledkem má být vlastní aplikace inspirovaná jeho
jednoduchostí.

---

## 2. Kontext

Google Keep vyhovuje jako jednoduchý systém pro:

* rychlé poznámky,
* nápady,
* krátké informace,
* checklisty,
* připomínky,
* lehkou organizaci pomocí štítků.

Nevyhovuje však dostatečně jeho:

* desktopové UI/UX,
* mobilní UI/UX,
* omezené možnosti formátování,
* omezená práce s technickým obsahem,
* omezená práce s vývojářskými poznámkami,
* absence vlastního jednoduchého workflow / Kanbanu,
* omezené možnosti AI integrace.

Cílem proto není Google Keep rozšiřovat, ale vytvořit vlastní systém.

---

## 3. Problém

Je potřeba osobní nástroj, který spojí:

1. jednoduchost Google Keep,
2. kvalitnější editor,
3. technické poznámky,
4. AI asistenci,
5. jednoduché osobní workflow,
6. práci offline,
7. synchronizaci mezi zařízeními.

Existující nástroje typu Notion nebo Evernote jsou pro tento účel příliš komplexní.

Aplikace nemá uživatele nutit do složité organizace dat ani složitého workflow.

---

## 4. Cíl

Vytvořit **local-first multi-device aplikaci** pro:

* poznámky,
* nápady,
* technické informace,
* osobní knowledge base,
* vývojářské workflow,
* jednoduché řízení vlastních projektů.

Hlavní princip:

> Pokročilé funkce mohou být velmi schopné, ale nesmí komplikovat jednoduché použití.

Musí být možné:

1. otevřít aplikaci,
2. napsat jednu větu,
3. uložit ji,
4. skončit.

Stejný systém ale musí zvládnout i rozsáhlou technickou poznámku s:

* kódem,
* diagramy,
* obrázky,
* checklisty,
* formátováním,
* AI operacemi.

---

## 5. Produktová filozofie

### 5.1 Keep-like jednoduchost

Google Keep je inspiračním vzorem především v oblasti:

* jednoduchosti,
* rychlosti,
* práce s kartami,
* štítků,
* barev,
* pinování,
* minimální povinné struktury.

Není cílem kopírovat jeho funkční omezení.

### 5.2 Progressive complexity

Pokročilé funkce mají být dostupné, ale ne vnucované.

Typický jednoduchý záznam:

```text
Zavolat Petrovi ohledně certifikátu.
```

nesmí vyžadovat:

* výběr projektu,
* složky,
* typu dokumentu,
* workflow,
* šablony,
* dalších metadat.

---

## 6. Hlavní moduly

Aplikace bude minimálně obsahovat dva samostatné moduly:

```text
Application
├── Notes
└── Projects / Kanban
```

Moduly mohou být vzájemně propojené, ale jejich doménové objekty zůstávají oddělené.

---

## 7. Modul Notes

### 7.1 Organizace

Organizace zůstane záměrně jednoduchá.

Podporováno bude:

* jedna jednoduchá úroveň kategorií / kolekcí,
* poznámka patří právě do jedné kategorie,
* štítky fungující napříč kategoriemi,
* více štítků na jedné poznámce.

Nebude podporována libovolně hluboká hierarchie složek.

### 7.2 Poznámka

Poznámka může obsahovat:

* volitelný název,
* obsah,
* kategorii,
* více štítků,
* barvu karty,
* pin,
* reminder,
* archivaci,
* trash,
* historii verzí.

Název není povinný.

AI může název:

* navrhnout,
* vytvořit,
* aktualizovat podle obsahu.

---

## 8. Editor poznámek

Editor používá model **souvislého dokumentu**, nikoliv blokový model typu Notion.

Obsah má být responsivní.

Na mobilu se přirozeně zobrazí jako úzký dokument.

Desktop zachová podobný způsob čtení, ale umožní:

* standardní užší šířku,
* rozšíření editoru,
* full-width režim.

### 8.1 Formátování

Editor má podporovat minimálně:

* headings,
* bold,
* italic,
* underline,
* strike-through,
* seznamy,
* číslované seznamy,
* checklist,
* odkazy,
* tabulky,
* zvýraznění,
* barvy textu,
* případně background textu.

### 8.2 Obrázky

Obrázky mají být vložené přímo do toku dokumentu.

Typický obsah:

```text
Text

[ obrázek ]

další text

[ diagram ]

další text
```

Obrázek se uživatelsky nechová jako příloha e-mailu.

---

## 9. Technický obsah

Aplikace je určena mimo jiné pro vývojářské a technické poznámky.

Proto musí být technický obsah prvotřídní funkcí.

### 9.1 Code blocks

Podporováno bude:

* inline code,
* code blocks,
* syntax highlighting,
* automatické rozpoznání jazyka,
* možnost ručního výběru jazyka,
* automatické formátování,
* copy,
* AI Explain,
* AI Modify.

Další funkce mohou být později doplněny.

### 9.2 Diagramy

První podporované diagramové formáty:

* Mermaid,
* PlantUML.

Každý diagram musí umožnit přepínání:

```text
Source ↔ Preview
```

Zdrojový text diagramu musí zůstat dostupný a editovatelný.

Systém musí umožnit přidávat další renderery později.

---

## 10. Reprezentace obsahu

Konkrétní interní formát není v konceptu určen.

Musí však umožňovat bezeztrátově zachovat:

* rich text,
* formátování,
* code blocks,
* diagramy,
* obrázky,
* checklisty,
* další podporované elementy.

Současně musí být možné kvalitně převádět/exportovat obsah do otevřených formátů, především Markdown.

Interní formát může být například HTML, strukturovaný dokument nebo jiný model.

Rozhodnutí patří do PLAN.

---

## 11. AI

AI je integrální součástí aplikace.

Není pouze doplňkovým chatbotem.

### 11.1 AI nad vybraným textem

AI má umožnit například:

* přeformulovat,
* opravit,
* zkrátit,
* rozšířit,
* přeložit,
* vysvětlit,
* změnit styl,
* vytvořit strukturu,
* vytvořit checklist.

### 11.2 AI nad poznámkou

AI může:

* vytvořit název,
* shrnout obsah,
* navrhnout štítky,
* změnit strukturu,
* vytvořit checklist,
* generovat diagram,
* upravovat kód,
* vysvětlovat kód.

### 11.3 AI napříč poznámkami

AI má mít možnost pracovat nad více poznámkami.

Například:

```text
Najdi všechny moje poznámky o ACME a shrň,
co jsem řešil kolem DNS-01.
```

To znamená podporu AI nad osobní znalostní databází.

### 11.4 AI providery

AI vrstva nesmí být závislá na jednom providerovi.

Počítá se minimálně s:

* OpenAI / GPT,
* Anthropic / Claude,
* LM Studio / lokální modely.

Konkrétní abstrakce bude řešena v PLAN.

---

## 12. Modul Projects / Kanban

Kanban je samostatný modul.

Není rozšířením Notes.

### 12.1 Projekty

Systém podporuje více jednoduchých projektů.

Například:

```text
PKI-NG
AI Cortex
Personal
Tool development
```

Projekt je jednoduchý organizační objekt.

Cílem není vytvořit plnohodnotný project-management systém.

### 12.2 Board

Základní model:

```text
Project
└── Board
    ├── Todo
    ├── In Progress
    └── Done
```

Volitelně mohou být přidány další stavy, například:

```text
Backlog
Review
Test
Blocked
```

Sloupce budou konfigurovatelné.

### 12.3 Cards

Kanban karta může obsahovat:

* název,
* popis,
* projekt,
* stav,
* štítky,
* prioritu,
* reminder,
* due date,
* checklist,
* komentáře,
* historii změn,
* archivaci.

### 12.4 Developer-oriented metadata

Do budoucna se počítá s lehkými developer funkcemi, například:

* task,
* bug,
* feature,
* improvement,
* branch reference,
* commit reference,
* pull request reference,
* external issue reference,
* lehká vazba na Jira issue.

Cílem není vytvořit náhradu Jira.

### 12.5 Kanban UX

Desktop musí podporovat:

* drag & drop,
* přesouvání mezi sloupci,
* více boardů,
* filtrování,
* fulltext search,
* archiv,
* backlog.

Možná budoucí rozšíření:

* WIP limits,
* swimlanes.

Nejsou požadavkem první verze.

---

## 13. AI v Kanbanu

AI má být dostupná také v Projects/Kanban modulu.

Například:

* rozpad tasku na checklist,
* návrh dalších kroků,
* vytvoření dalších karet,
* shrnutí projektu,
* shrnutí boardu,
* identifikace blokátorů,
* analýza otevřených úkolů,
* vytvoření tasku z poznámky.

---

## 14. Platformy

Cílové platformy:

1. Windows desktop
2. Web
3. Mobil
4. Tablet

První hlavní klient bude desktop.

Mobil a tablet mohou vzniknout později, ale architektura s nimi musí počítat od začátku.

---

## 15. Local-first

Nativní klienti budou local-first.

Každý klient má vlastní lokální databázi:

```text
SQLite
```

Uživatel musí být schopen plnohodnotně pracovat:

* online,
* offline.

Internet nesmí být podmínkou běžného použití aplikace.

---

## 16. Server

Server poskytuje minimálně:

* REST API,
* autentizaci / autorizaci,
* synchronizaci,
* centrální databázi,
* verzování,
* merge,
* AI integrační služby podle potřeby,
* audit,
* backup.

Server bude později zároveň backendem webové aplikace.

---

## 17. Synchronizace

Typický model:

```text
                 Web
                  │
                  ▼
              REST API
                  │
             Server DB
                  ▲
        ┌─────────┼─────────┐
        │         │         │
      Sync      Sync      Sync
        │         │         │
    Desktop    Mobile    Tablet
     SQLite     SQLite    SQLite
```

Nativní klienti mohou měnit data offline.

Po obnovení spojení musí dojít k automatické synchronizaci.

---

## 18. Konflikty a merge

Systém nesmí používat jednoduchý princip, při kterém může být změna uživatele tiše ztracena.

Požadované chování je podobné Confluence.

Pokud jsou změny kompatibilní:

```text
Version A
   │
   ├── Desktop change
   └── Mobile change
```

systém se je pokusí automaticky sloučit.

Pokud změny zasahují stejnou část obsahu a nelze je bezpečně sloučit:

* systém zachová obě varianty,
* konflikt explicitně označí,
* umožní uživateli rozhodnout.

Konkrétní merge algoritmus bude vybrán v PLAN.

---

## 19. Verzování

Poznámky musí podporovat historii verzí podobně jako Confluence.

Musí být možné:

* zobrazit historii,
* zobrazit starší verzi,
* porovnat změny,
* obnovit starší stav.

Rozsah verzování dalších objektů bude upřesněn později.

---

## 20. Identity a multi-user

Aplikace bude od počátku navržena jako multi-user.

Každý uživatel má vlastní:

* poznámky,
* štítky,
* projekty,
* boardy,
* karty,
* nastavení.

První identity provider:

**Google**

Datový model ani backend nesmí být pevně svázán pouze s Google identitou.

Do budoucna musí být možné přidat další providery.

---

## 21. Collaboration

Sdílení poznámek, projektů nebo boardů mezi uživateli není aktuálně hlavním cílem.

Multi-user znamená především:

> více nezávislých účtů v jednom systému.

Komplexní collaboration / ACL model není součástí základního scope.

---

## 22. Search

Aplikace musí obsahovat jednotné vyhledávání.

Vyhledávání má fungovat napříč minimálně:

* Notes,
* Projects,
* Kanban cards.

Požadován je fulltext.

AI může nad výsledky vyhledávání dále pracovat.

---

## 23. Reminders a notifications

Poznámky i Kanban položky mohou mít reminder.

Reminder musí být schopen vytvořit systémovou notifikaci minimálně na:

* desktopu,
* mobilu.

Přesný model server/client scheduling bude řešen v PLAN.

---

## 24. UI

Aplikace musí podporovat:

* Light mode,
* Dark mode.

UI musí být responsivní.

Jednotlivé platformy mají zachovat stejný mentální model aplikace.

---

## 25. Export a vlastnictví dat

Uživatel musí být vlastníkem svých dat.

Musí existovat možnost kompletního exportu.

Export nesmí být závislý na proprietárním formátu aplikace.

Předpokládané otevřené formáty mohou zahrnovat:

* Markdown,
* JSON,
* HTML,
* obrázky a další binární obsah.

Přesný exportní formát bude určen později.

---

## 26. Backup

Server musí mít samostatný mechanismus záloh.

Backup není totéž jako verzování poznámek.

Požadovány jsou:

* pravidelné zálohy,
* více verzí záloh,
* možnost obnovy systému.

Konkrétní retenční politika bude stanovena později.

---

## 27. Bezpečnost

Bezpečnost je požadavek od začátku projektu, nikoliv funkce doplněná později.

Systém musí počítat minimálně s:

* TLS,
* bezpečnou autentizací,
* server-side authorization,
* izolací dat jednotlivých uživatelů,
* bezpečným ukládáním secrets,
* ochranou API,
* zabezpečenou synchronizací,
* šifrováním dat,
* auditem.

Konkrétní kryptografický a autorizační model bude definován v PLAN.

---

## 28. Šifrování

Data musí být chráněna také při uložení.

Minimálním požadavkem je šifrování citlivých dat:

* při přenosu,
* na serveru,
* v zálohách.

Zda bude použito:

* storage/database encryption,
* aplikační encryption,
* per-user encryption,
* end-to-end encryption,

bude rozhodnuto v PLAN.

Je nutné přitom zachovat požadavky na:

* fulltext search,
* AI práci nad poznámkami,
* serverovou synchronizaci,
* webového klienta.

---

## 29. Audit

Systém musí obsahovat audit důležitých operací.

Audit není totéž jako běžný aplikační log.

Audit má být:

* strukturovaný,
* dohledatelný,
* oddělený od běžného technického logování.

Auditované operace mohou zahrnovat například:

* autentizaci,
* změny identity,
* přístup k datům,
* vytvoření,
* změnu,
* smazání,
* obnovu,
* synchronizaci,
* export,
* bezpečnostní události,
* administrativní zásahy.

Přesný auditní model bude definován v PLAN.

---

## 30. Out of scope

Aktuálně není cílem vytvořit:

* Notion,
* Evernote,
* Confluence,
* Jira,
* obecný project-management systém,
* komplexní collaboration platformu,
* hlubokou hierarchii znalostí,
* plnohodnotný issue tracker.

---

## 31. Hlavní kritéria úspěchu

Aplikace je úspěšná, pokud:

1. rychlost vytvoření jednoduché poznámky je srovnatelná s Google Keep,
2. běžné použití není komplikováno pokročilými funkcemi,
3. technická poznámka může obsahovat bohatý obsah,
4. _(doplnit — původní text byl v tomto bodě useknutý)_
