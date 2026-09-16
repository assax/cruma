# spec.md — Cruma, inkrement I-1 (walking skeleton)

```text
Phase: SPEC          Run mode: CONCEPTUAL          Architecture basis: profile @ .architecture\architecture-cruma\PROFILE.md
Output language: czech
Open items: O-2, O-3, O-4, O-5          Carried assumptions: A-1, A-2, A-3, A-4
Last approved gate: SPEC->PLAN @ 2026-09-16
```

**Vztah k systémové specifikaci.** Tento dokument je výběrem ze `docs/arch/spec.md` pro inkrement I-1.
Identifikátory `FR-n` a `NFR-n` jsou **stejné** jako v systémové specifikaci; nové požadavky nevznikají.
Akceptační kritéria jsou převzata a pro I-1 upravena:

* **[I-1 zúženo]** — kritérium platí v I-1 jen částečně; zbytek patří do uvedeného inkrementu;
* **[I-1 zpřesněno]** — kritérium doplněné rozhodnutím běhu I-1 (`i-1/DECISIONS.md`);
* **[profil]** — kritérium odvozené ze závazného pravidla profilu; pravidlo je citováno ID, ne parafrází.

Pro implementaci platí **tento** dokument. Systémovou specifikaci není potřeba otevírat.

---

## 1. Cíl inkrementu

Poznámky se základním formátováním jdou vytvářet, hledat a upravovat na desktopu i v telefonu, data se
synchronizují přes vlastní server, souběžné změny se slučují bez ztráty a systém běží na Hetzner Cloud se
zálohami, jejichž obnova byla vyzkoušena.

## 2. Mimo rozsah I-1

Tabulky, barva textu, pozadí textu, obrázky, code blocky, diagramy; zobrazení a porovnání historie verzí;
export; připomínky a notifikace; AI; Projects / Kanban; nativní mobilní shell; bloby (C-7). Zdroj: `frame.md` §3.

---

## 3. Funkční požadavky

### Poznámky

#### FR-1 — Rychlé vytvoření poznámky
1. Poznámku jde vytvořit v posloupnosti: otevřít aplikaci → napsat text → uložit. Žádný krok navíc není povinný.
2. Aplikace během vytvoření nevyžaduje název, kategorii, štítek, typ, šablonu ani barvu.
3. Každá nově vytvořená poznámka je zařazena do výchozí kategorie bez ohledu na to, odkud byla vytvořena
   (systémové A-3; [profil] UI-005).

#### FR-2 — Vlastnosti poznámky
1. Poznámka má volitelný název, obsah, právě jednu kategorii, nula až více štítků, barvu karty, příznak
   připnutí a stav (aktivní / archivovaná / v koši). **[I-1 zúženo]** Připomínka je I-2.
2. Poznámku bez názvu jde uložit a zobrazit.
3. Připnuté poznámky se v přehledu zobrazují před nepřipnutými.
4. Každou z vlastností z bodu 1 jde nastavit i zrušit po vytvoření poznámky.
5. **[profil]** Barva karty má hodnotu pro světlý i tmavý režim (UI-006).

#### FR-3 — Kategorie
1. Kategorii jde vytvořit, přejmenovat a smazat.
2. Kategorii nejde vnořit do jiné kategorie.
3. Poznámky mazané kategorie se přesunou do výchozí kategorie; nesmažou se.
4. Výchozí kategorii nejde smazat.
5. **[I-1 zpřesněno]** Výchozí kategorii vytvoří server při založení uživatele; desktop i tenký klient používají
   tutéž kategorii se stejným identifikátorem (I1-D-3, analysis M-4).

#### FR-4 — Štítky
1. Stejný štítek jde přiřadit poznámkám v různých kategoriích.
2. Filtr podle štítku zobrazí poznámky ze všech kategorií.
3. Smazání štítku odebere štítek z poznámek, poznámky zachová.

#### FR-5 — Archiv a koš
1. Archivovaná poznámka se nezobrazuje v běžném přehledu a je dohledatelná v archivu.
2. Poznámka v koši se nezobrazuje v přehledu ani v archivu a jde ji obnovit.
3. Trvalé odstranění poznámky z koše vyžaduje explicitní akci uživatele a odstraní i její verze ([profil] VER-008).

### Verze

#### FR-6 — Historie verzí poznámky — **[I-1 zúženo: pouze mechanismus]**
1. Server přiděluje verzím poznámky vzestupná čísla; verze nese čas vzniku, zdroj (`desktop`, `web`, `merge`)
   a základní verzi, ze které vznikla.
2. **[profil]** Nová verze nevzniká za každé automatické uložení, ale za editační relaci (VER-006).
3. **[I-1 zpřesněno]** Po sobě jdoucí uložení téže poznámky od téhož uživatele z téhož tenkého klienta server
   spojí do jedné verze, dokud nepřijde změna z jiného zdroje nebo neuplyne interval nečinnosti (I1-D-2).
4. **[profil]** Offline práce desktopu se na serveru projeví jako jedna verze; lokální mezilehlé verze se smažou
   až po potvrzení serverem (SYN-004).
5. Zobrazení historie, otevření starší verze, porovnání a obnovení jsou I-2.

### Editor

#### FR-7 — Editor souvislého dokumentu — **[I-1 zúženo]**
1. Editor podporuje: nadpisy, tučné písmo, kurzívu, podtržení, přeškrtnutí, odrážkový seznam, číslovaný seznam,
   checklist, odkazy a zvýraznění. Tabulky, barva textu a barva pozadí textu jsou I-2.
2. Každý uvedený prvek jde vložit, upravit a odstranit.
3. Checklist v obsahu umožňuje položku označit a odznačit přímo v zobrazení poznámky.
4. Po uložení, zavření a opětovném otevření poznámky jsou všechny prvky zachovány beze změny.
5. **[profil]** Blok si ponechá svůj identifikátor při úpravě textu, přesunu a změně typu; při rozdělení bloku
   si identifikátor ponechá první část a druhá dostane nový; při spojení se ponechá identifikátor prvního
   bloku; vložený nebo duplikovaný blok dostane nový identifikátor (CNT-002).
6. **[profil]** Dokument nese verzi schématu (CNT-003); uzel neznámého typu se při načtení a uložení zachová
   beze změny (CNT-005).
7. **[profil]** Odkaz povoluje pouze schémata `http`, `https` a `mailto` (`security-policy.md` §3).
8. Editor funguje stejně na desktopu i v tenkém klientovi.

#### FR-8 — Šířka editoru
1. Desktop nabízí tři režimy šířky (standardní, rozšířená, plná) a zvolený režim se projeví okamžitě.
2. Na úzkém displeji se obsah nezobrazuje s vodorovným posunem celé stránky.

### Vyhledávání

#### FR-24 — Jednotné vyhledávání
1. **[I-1 zúženo]** Výsledky obsahují poznámky. Projekty a karty přibudou v I-4.
2. Dotaz `certifikat` najde text `Certifikát`.
3. Dotaz `certif` najde text `certifikátu`.
4. Stejný dotaz nad stejnými synchronizovanými daty vrací na desktopu i na tenkém klientovi stejnou množinu
   výsledků ([profil] TST-002).
5. **[profil]** Hledá se v názvu a v prostém textu obsahu poznámky (CNT-007); normalizace a tokenizace jsou
   jediné pro obě strany (SRC-001, SRC-003).
6. Filtr výsledků podle stavu: aktivní, archiv, koš.

### Klienti a synchronizace

#### FR-26 — Desktop offline
1. Po odpojení od sítě jde vytvářet, upravovat, archivovat, mazat a vyhledávat poznámky.
2. Aplikace se spustí a zobrazí data bez připojení k internetu, i když vypršelo přihlášení.
3. **[I-1 zpřesněno]** První spuštění desktopu vyžaduje přihlášení a připojení k serveru (I1-D-3).

#### FR-27 — Automatická synchronizace desktopu
1. Změny provedené offline se po připojení odešlou bez akce uživatele.
2. Změny provedené na tenkém klientovi se objeví na desktopu bez akce uživatele.
3. Uživatel vidí stav synchronizace: synchronizováno / čeká / konflikt / chyba ([profil] SYN-007).
4. **[profil]** Přerušená synchronizace neztratí ani nezdvojí žádnou změnu (SYN-003, SYN-004, ERR-002).

#### FR-28 — Slučování změn a konflikty
1. Změna odstavce A na desktopu a odstavce B na tenkém klientovi vede ke sloučené verzi s oběma změnami.
2. Změna téhož odstavce na obou stranách vede ke konfliktu; obě varianty jsou uložené a viditelné.
3. Uživatel konflikt vyřeší výběrem jedné varianty nebo ruční úpravou na desktopu i na tenkém klientovi;
   vyřešení vytvoří novou verzi.
4. Úprava poznámky na jedné straně a její smazání na druhé nevede ke ztrátě úpravy.
5. **[I-1 zpřesněno]** Body 1–4 platí stejně pro souběh desktop ↔ tenký klient i tenký klient ↔ tenký klient;
   server úpravu z tenkého klienta nikdy neodmítne kvůli souběhu (I1-D-1).
6. **[profil]** Současná změna skalární vlastnosti (název, kategorie, barva, připnutí, stav) na obou stranách:
   vyhraje později přijatá; přepsaná hodnota zůstane v záznamu verze a stav synchronizace přepsání ohlásí
   (VER-005). Štítky se slučují přidáním a odebráním.

#### FR-29 — Tenký klient
1. **[I-1 zúženo]** Tenký klient zobrazuje a upravuje poznámky, kategorie a štítky, vyhledává a řeší konflikty.
2. Změna provedená na tenkém klientovi je okamžitě uložena na serveru.
3. Tenký klient jde nainstalovat jako PWA do telefonu a spustit bez prohlížečového rozhraní.
4. **[profil]** Bez připojení jsou akce kromě vytvoření nové poznámky nedostupné a klient zobrazuje stav offline
   (UI-004).

#### FR-30 — Fronta zápisů na tenkém klientovi
1. Bez připojení jde napsat a uložit novou poznámku.
2. Uživatel vidí, že poznámka čeká na odeslání.
3. Po obnovení spojení se poznámka odešle bez akce uživatele a zmizí z fronty.
4. Zavření a opětovné otevření klienta frontu nevymaže.
5. Úprava existujících poznámek bez připojení není podporována.
6. **[profil]** Opakované odeslání téže položky fronty nevytvoří druhou poznámku (SYN-006).

### Identita

#### FR-31 — Oddělení uživatelů
1. Uživatel nevidí, nenajde, neupraví ani nesmaže data jiného uživatele žádnou cestou (seznam, detail,
   vyhledávání, synchronizace).
2. **[profil]** Přístup k poznámce jiného uživatele vrací `not_found`, ne `forbidden`
   (`error-handling-policy.md` §2).

#### FR-32 — Přihlášení
1. Uživatel se přihlásí účtem Google na desktopu i na tenkém klientovi.
2. Přidání dalšího poskytovatele nevyžaduje změnu uložených dat uživatelů.
3. **[profil]** Desktop se přihlašuje přes systémový prohlížeč, nikdy uvnitř okna aplikace (SEC-002).
4. **[profil]** Tenký klient neuchovává přístupové tokeny v úložišti prohlížeče (SEC-004).
5. Uživatel se může odhlásit na desktopu i na tenkém klientovi.

### UI

#### FR-34 — Světlý a tmavý režim
1. Uživatel přepne mezi světlým a tmavým režimem na desktopu i na tenkém klientovi.

### Provoz

#### FR-35 — Audit
1. **[I-1 zúženo]** Auditní záznam vzniká pro: přihlášení, odhlášení, neúspěšné přihlášení, navázání identity,
   vytvoření, úpravu, archivaci, přesun do koše, obnovení a trvalé smazání poznámky, vyřešení konfliktu,
   synchronizační relaci, odmítnutou změnu, omezení četnosti a zamítnutou autorizaci
   (`logging-and-audit-policy.md` §2).
2. Každý záznam obsahuje čas, uživatele, typ operace, typ a identifikátor objektu, typ klienta a výsledek;
   nikdy obsah ([profil] AUD-002).
3. Auditní záznamy jsou dohledatelné podle uživatele, času a typu operace.
4. Auditní záznamy jsou uložené odděleně od technických logů a nejdou upravit ani smazat (AUD-001, AUD-004).

#### FR-36 — Backup a obnova
1. Zálohy databáze vznikají automaticky alespoň denně a před každým nasazením, bez zásahu uživatele
   ([profil] OPS-002, OPS-005).
2. Zálohy jsou uložené mimo server a uchovávají více verzí.
3. Z libovolné uchované zálohy jde obnovit funkční systém.
4. Obnova byla provedena na jednorázovém serveru podle zdokumentovaného postupu a datum úspěšné zkoušky je
   zapsané ([profil] OPS-003).

#### FR-37 — Kompatibilita verzí klienta
1. Desktop pod minimální podporovanou verzí nesynchronizuje a uživatel dostane výzvu k aktualizaci.
2. Odmítnutá synchronizace neztratí lokální změny; po aktualizaci se odešlou.
3. **[I-1 zpřesněno]** Instalátor a aktualizace desktopu se stahují ze serveru Cruma (I1-D-5).
4. Desktop nabídne dostupnou aktualizaci a nainstaluje ji bez práv administrátora.

---

## 4. Nefunkční požadavky platné pro I-1

| ID | Požadavek (zkráceně; plné znění v systémové SPEC) | Poznámka pro I-1 |
|---|---|---|
| NFR-1 | Vytvoření jednoduché poznámky bez povinného dialogu | ověřuje FR-1 |
| NFR-2 | Pokročilé funkce nevyžadují interakci při jednoduchém použití | |
| NFR-3 | Desktop funguje offline kromě synchronizace a přihlášení nového uživatele | AI není v I-1 |
| NFR-4 | Žádná tichá ztráta změny | ověřuje FR-27, FR-28, FR-30 |
| NFR-5 | Vyhledávání nerozlišuje velikost písmen, diakritiku ani Unicode zápis; hledá podle začátku slova; shodné na všech klientech | ověřuje FR-24 |
| NFR-6 | Rozdíl výsledků daný neproběhlou synchronizací je přijatelný | |
| NFR-7 | Dimenzování do 50 000 poznámek na uživatele (A-2 systémové SPEC) | viz §6 |
| NFR-8 | TLS, bezpečná autentizace, autorizace na serveru, izolace uživatelů, tajné údaje mimo repozitář | pravidla SEC-001..SEC-008 |
| NFR-9 | Šifrování při přenosu, na serveru a v zálohách; lokální data desktopu aplikace nešifruje | |
| NFR-10 | Audit strukturovaný, dohledatelný, oddělený od logů | ověřuje FR-35 |
| NFR-13 | Přidání typu bloku nevyžaduje změnu uložených dat | ověřuje FR-7 akc. 6 |
| NFR-14 | Identita není vázaná na Google | ověřuje FR-32 akc. 2 |
| NFR-15 | Sdílené UI bez přímého volání platformních API | pravidla UI-001..UI-003 |
| NFR-16 | Notes a Kanban oddělené | pravidlo DEP-005 |
| NFR-17 | Pomalejší start PWA na telefonu je přijatelný | |
| NFR-18 | Verzované API a synchronizační protokol | pravidla API-001, SYN-001 |
| NFR-19 | Stejné pojmy a navigace na všech klientech | pravidlo UI-003 |

---

## 5. Omezení

Závazná jsou všechna pravidla `.architecture/architecture-cruma/shared/architecture-rules.md` s výjimkou
pravidel, která se týkají funkcí mimo I-1: BLB-001..BLB-004, SYN-005, AI-001..AI-005, SEC-007. Pravidla se
v tomto dokumentu ani v TASKS neparafrázují; citují se jejich ID.

## 6. Rizika

Převzata z `frame.md` §4 (R-1 až R-7) beze změny. Nové riziko:

| ID | Riziko | Zmírnění |
|---|---|---|
| R-8 | Výkon při NFR-7 se v I-1 ověří jen syntetickými daty, protože skutečný objem vznikne až používáním. | Test s generovanými 50 000 poznámkami pro vyhledávání a první synchronizaci desktopu. |

## 7. Kritéria dokončení I-1

| ID | Kritérium | Ověřeno |
|---|---|---|
| S-I1-1 | Všechna akceptační kritéria v §3 jsou splněna. | testy a ruční ověření |
| S-I1-2 | Všechny sady testů požadované `testing-strategy.md` pro rozsah I-1 procházejí, včetně deseti scénářů slučování, konformních testů vyhledávání, testů izolace uživatelů a architektonického testu referencí (I1-D-4). | CI |
| S-I1-3 | Na produkčním serveru: poznámka vytvořená v PWA v telefonu se objeví na desktopu; současná úprava různých odstavců na obou stranách se sloučí; úprava téhož odstavce vytvoří konflikt a jde vyřešit. | ruční scénář na produkci |
| S-I1-4 | Zkouška obnovy ze zálohy proběhla a je zapsaná (FR-36 akc. 4). | `deploy/RESTORE.md` |
| S-I1-5 | Vyhledávání a první synchronizace desktopu jsou použitelné nad 50 000 generovanými poznámkami (R-8). | výkonový test |

## 8. Předpoklady

| ID | Předpoklad | Alternativy | Přijal | Odvozené položky |
|---|---|---|---|---|
| A-1 | Schválené systémové artefakty z 2026-09-15 platí nezměněné. | — | operátor | celý dokument |
| A-2 | Profil Cruma je autoritativní i tam, kde jde nad rámec `plan.md`. | `plan.md` má přednost | operátor | kritéria označená [profil] |
| A-3 | Repozitář `cruma` vznikne přejmenováním stávající složky na `c:\MyWork\_project\cruma`; dokumentace a profil se přesunou s ní. | nová složka jinde, dokumentace zůstane | operátor, 2026-09-16 | TASKS: kořen scope hintů |
| A-4 | CI na GitHub Actions: build a všechny testy při každém push a pull requestu (PostgreSQL přes Testcontainers na Linux runneru); vydání vytvoří image serveru do GitHub Container Registry a instalátor desktopu. Repozitář je soukromý. | CI bez publikování; bez CI | operátor, 2026-09-16 | S-I1-2, TASKS etapy E-1 a E-6 |

## 9. Otevřené položky

| ID | Mezera | Kde se vyřeší |
|---|---|---|
| O-2 | Struktura `tasks.md`: etapy E-1..E-6, nebo souvislá řada | konec PLAN |
| O-3 | Doména a DNS | akce operátora před etapou E-6 |
| O-4 | Google OAuth přihlašovací údaje | akce operátora před etapou E-3 |
| O-5 | Úložiště záloh mimo server | akce operátora před etapou E-6 |

Uzavřené: O-1 (I1-D-6, A-3), O-6 (I1-D-6, A-4).
