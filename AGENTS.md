# Pokyny pro agenty

Cruma – osobní local-first aplikace pro poznámky, technický obsah a jednoduchý Kanban (.NET 10). Desktop
(WPF + BlazorWebView) je plnohodnotný offline klient, web a mobil (Blazor WebAssembly PWA) jsou tenké klienty,
data se synchronizují přes vlastní server (ASP.NET Core, PostgreSQL).

Záměry v `CONCEPT.md`, historie změn v `CHANGELOG.md`, chyby v `BUGS.md`, připomínky k zapracování
v `CHANGES.md`, rozcestník v `README.md`.

## Architektura a zdroje pravdy

- **Závazná pravidla:** `.architecture/architecture-cruma/` – manifest `PROFILE.md`, registr pravidel
  `shared/architecture-rules.md` (načítat vždy), struktura projektů `shared/solution-structure-template.md`.
- **Architektura systému:** `docs/arch/plan.md` a rozhodnutí `docs/arch/DECISIONS.md` (D-1..D-9).
- **Co se právě staví:** inkrement I-1, `docs/arch/i-1/tasks.md` (úkoly T-n v etapách E-1..E-6) spolu
  s `docs/arch/i-1/spec.md` a `docs/arch/i-1/plan.md`. Implementuje se odsud.
- Při rozporu: profil > `spec.md` > `plan.md` > `tasks.md`. Rozpor neřeš sám – zastav a zeptej se.
- Varianty označené `REJECTED` v `plan.md` a `DECISIONS.md` se nikdy neimplementují.
- Úkol se `Scope hint: unresolved` → zastav a zeptej se, místo si nevybírej.
- Úkoly s `Owner: operator` dělá autor; agent na ně čeká a neobchází je.
- Dokumenty v `docs/arch/` jsou uzavřené výstupy architektonických běhů. Neupravují se při implementaci;
  změna architektury jde přes nový běh architektonického enginu.

## Sprinty

- Když autor zadá **sprint** (ucelený vývoj – řekne slovo *sprint*, „vyvinout etapu E-n“, „sprint na …“),
  založ **před prvním zásahem do kódu** záznam v `dev/` podle šablony v `dev/README.md`
  (`dev/sprint-NNN-YYYY-MM-DD-nazev.md`) a řádek v `dev/SPRINTY.md`. Během sprintu zapisuj milníky s časem
  a tokeny, na konci čas konce, délku, výsledek (commity, verze, testy) a co se nestihlo.
- Sprint při stavbě I-1 obvykle odpovídá jedné etapě z `docs/arch/i-1/tasks.md`; v záznamu uveď rozsah jako
  úkoly `T-n`.
- Časy zjišťuj příkazem (`Get-Date -Format 'yyyy-MM-dd HH:mm'`), nehádej je.
- Drobné úkoly mimo sprint se do `dev/` nezapisují.

## CHANGES a BUGS

- Zpráva, která začíná **`bug:`**, se jen zapíše do `BUGS.md` s rozborem – neopravuje se, dokud autor neřekne.
- Připomínky se sbírají do `CHANGES.md` s rozborem a návrhem; zapracovávají se až na pokyn
  **„zapracuj změny“**.

## Větve

- **`dev`** je výchozí větev a probíhá na ní veškerý vývoj; commituje se sem.
- **`main`** je větev vydaných verzí. Slučuje se do ní jen vydání a jen tam se dávají tagy verzí.
- Sprint pracuje na `dev`; větev pro jednotlivý sprint se nezakládá, pokud to autor nevyžádá.
- **Nepushovat bez výslovného pokynu autora** – ani na `dev`.

## CHANGELOG a vydání

- **Každá změna, kterou uživatel uvidí, jde do `CHANGELOG.md` pod `## [Nevydáno]`** – ve stejném commitu jako
  kód. Nikdy ne pod sekci už vydané verze.
- Kategorie `### Přidáno`, `### Změněno`, `### Opraveno`, `### Vývojářské` (v tomhle pořadí, každá v sekci
  nejvýš jednou). Položka začíná tučným shrnutím na jeden řádek, pod ním stručně *proč*.
- Commit popisuje totéž co CHANGELOG; verze určují tagy.
- **Nepushovat bez výslovného pokynu autora.**

## Stavba a ověřování

Kód zatím neexistuje; vzniká úkoly T-2 a T-3. Po nich platí:

- Build: `dotnet build Cruma.slnx`. Testy: `dotnet test Cruma.slnx` (NUnit, Moq, bUnit; integrační testy
  s PostgreSQL přes Testcontainers).
- Kontejnery: lokálně **Podman**; `deploy/compose.yaml` musí fungovat v Podmanu i Dockeru. Testcontainers se
  připojují přes socket Podmanu.
- Nová funkce = nový test. Scénáře slučování, konformní testy vyhledávání a testy izolace uživatelů jsou
  povinné (`shared/testing-strategy.md`).
- Architektonický test hlídá povolené reference mezi projekty – nepovolenou referenci neobcházej, oprav návrh.
- Logování přes `ILogger` s NLog, výchozí úroveň Information; obsah poznámek, dotazy, tokeny a tajné údaje se
  nikdy nelogují (LOG-002).
- Tajné údaje (Google OAuth, hesla databáze, klíče) nikdy do repozitáře (SEC-006).

## Zásady projektu

- Komentáře v kódu česky, identifikátory anglicky, názvy odvozené od `Cruma` (`shared/coding-conventions.md`);
  „Keep“ se v kódu nepoužívá.
- Obsah poznámek neopouští zařízení bez vědomé akce uživatele (NFR-12, AI-002).
- Žádná tichá ztráta změny uživatele (NFR-4) – při pochybnosti zachovat obě varianty a ohlásit konflikt.
