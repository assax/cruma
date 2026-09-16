# DECISIONS.md — běh I-1

Rozhodnutí tohoto běhu. Zpřesňují schválenou architekturu (`docs/arch/DECISIONS.md` D-1..D-9) na úroveň
potřebnou pro akceptační kritéria I-1; žádné ji nemění. Identifikátory jsou lokální pro běh I-1 a v textu se
uvádějí s prefixem `I1-`, aby se nepletly se systémovými D-1..D-9.

| ID | Téma | Stav | Zdroj |
|---|---|---|---|
| I1-D-1 | Úprava poznámky z tenkého klienta | **ROZHODNUTO** 2026-09-16 | analysis RB-1, M-1 |
| I1-D-2 | Vznik verzí při úpravách z tenkého klienta | **ROZHODNUTO** 2026-09-16 | analysis RB-2, M-2 |
| I1-D-3 | První spuštění desktopu | **ROZHODNUTO** 2026-09-16 | analysis RB-3, M-3 |
| I1-D-4 | Vynucení povolených referencí | **ROZHODNUTO** 2026-09-16 | analysis RB-4 |
| I1-D-5 | Distribuce desktopu | **ROZHODNUTO** 2026-09-16 | analysis RB-5, M-5 |
| I1-D-6 | Repozitář | **ROZHODNUTO** 2026-09-16 | analysis O-1, O-6 |

---

## I1-D-1 — Úprava poznámky z tenkého klienta

**Rozhodnutí:** Tenký klient posílá s úpravou existující poznámky identifikátor základní verze. Server ji
zpracuje stejnou cestou jako změnu z desktopu: tříbodový merge přes `Cruma.Versioning`; při souběžné změně téhož
bloku vznikne konflikt k vyřešení. Server úpravu nikdy neodmítne kvůli souběhu.
**`REJECTED`:** odmítnutí s `version_conflict` a nutnost načíst a upravit znovu — telefon by ztrácel úpravy (NFR-4).
**Váže se na:** FR-28, FR-29, NFR-4, VER-002, VER-003.

## I1-D-2 — Vznik verzí při úpravách z tenkého klienta

**Rozhodnutí:** Server spojuje po sobě jdoucí uložení téže poznámky od téhož uživatele z téhož klienta do jedné
verze, dokud nepřijde změna z jiného zdroje nebo neuplyne interval nečinnosti. Délka intervalu je konfigurační
hodnota serveru.
**`REJECTED`:** verze za každé uložení (systémové D-3.1 b); spoléhání na signál konce relace z prohlížeče.
**Váže se na:** FR-6, VER-006.

## I1-D-3 — První spuštění desktopu

**Rozhodnutí:** První spuštění desktopu vyžaduje přihlášení a připojení k serveru. Po prvním přihlášení
a stažení dat pracuje desktop offline bez omezení (FR-26). Stav „lokální data bez uživatele“ neexistuje.
**`REJECTED`:** lokální používání před přihlášením s pozdějším převodem dat.
**Váže se na:** FR-26, NFR-3, `desktop-pattern.md` §2. Důsledek: výchozí kategorii vytváří server při založení
uživatele a desktop ji získá při prvním přihlášení (analysis M-4).

## I1-D-4 — Vynucení povolených referencí

**Rozhodnutí:** Automatický architektonický test v sadě testů ověřuje reference mezi projekty podle
`solution-structure-template.md` §3 a selže při jakékoliv nepovolené referenci.
**`REJECTED`:** kontrola pouze při review.
**Váže se na:** DEP-001..DEP-004, STR-001.

## I1-D-5 — Distribuce desktopu

**Rozhodnutí:** Instalátor a aktualizační feed desktopu jsou statické soubory vystavené serverem Cruma za proxy,
bez autentizace.
**`REJECTED`:** samostatná služba pro distribuci.
**Váže se na:** FR-37, `desktop-pattern.md` §6.

## I1-D-6 — Repozitář

**Rozhodnutí:** Repozitář se jmenuje `cruma`; vzdálený repozitář je na GitHubu.
**Navazující předpoklady:** A-3 (umístění lokální složky), A-4 (CI) — viz `spec.md` §8.
