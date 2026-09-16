# Sprinty – záznamy o vývoji

Každý **sprint** má vlastní záznam v této složce. Sprint je **ucelený vývoj**,
který autor výslovně zadá („vyvinout vše ze seznamu“, „sprint na …“).
Drobnosti mimo sprint (zápis do CHANGES, jednorázová oprava, dotaz) se sem
nezapisují. Princip je převzatý ze Symbolonu (`md-split-editor/dev`) přes Susceptor.

## Soubory

| Soubor | Co v něm je |
|---|---|
| `README.md` | tahle pravidla a šablona |
| `SPRINTY.md` | přehled všech sprintů – jeden řádek na sprint |
| `OTEVRENE.md` | otevřené body k rozhodnutí autora – rozpory a přeskočené části |
| `sprint-NNN-YYYY-MM-DD-kratky-nazev.md` | záznam jednoho sprintu (`NNN` = pořadí od `001`) |

## Pravidla

- **Začátek:** hned po zadání sprintu – ještě před prvním zásahem do kódu –
  založit záznam ze šablony níže, vyplnit **čas začátku**, zadání (citace
  autora) a rozsah, a přidat řádek do `SPRINTY.md` se stavem *běží*.
- **Průběh:** do sekce *Průběh* zapisovat **milníky s časem** (analýza hotová,
  testy zelené, commit, vydání…), rozhodnutí během práce, problémy a slepé
  uličky. Ne každý příkaz – jen to, co je potřeba k pochopení, kam šel čas.
- **Konec:** **čas konce** a **délka**, výsledek (commity, verze, instalátor,
  testy s čísly), co se nestihlo a proč, a stav bodů v `CHANGES.md` /
  `BUGS.md`. Řádek v `SPRINTY.md` doplnit a přepnout na *hotovo* / *přerušeno*.
- **Časy** místní (`Europe/Prague`) ve tvaru `YYYY-MM-DD HH:MM`; zjistit je
  příkazem (`Get-Date -Format 'yyyy-MM-dd HH:mm'`), nehádat. Délka = konec −
  začátek; čekání na autora (otázka bez odpovědi) se zapíše jako pauza a do
  čisté délky se nepočítá.
- **Diagnostika:** u milníků zapisovat i **tokeny** – kolik agentovi zbývá
  z rozpočtu (`total_tokens left`) a spotřebu od předchozího zápisu. Počitadlo
  se vrací na začátek s každou novou zprávou autora, takže spotřeba se počítá
  **po úsecích** a souhrn sprintu je součet úseků (odhad). Dál délky buildů
  a testů a na konci rozsah změn (`git diff --stat` od začátku sprintu).
- Záznam je **česky**, věcně; žádná tajemství, hesla, klíče ani obsah
  pracovních dokumentů autora.
- Záznamy se commitují spolu s prací sprintu.

## Šablona

```markdown
# Sprint NNN – krátký název

| | |
|---|---|
| **Začátek** | YYYY-MM-DD HH:MM |
| **Konec** | YYYY-MM-DD HH:MM |
| **Délka** | X h Y min (čistá, bez pauz: X h Y min) |
| **Stav** | běží / hotovo / přerušeno |
| **Verze** | 0.X.Y (vydáno / nevydáno) |
| **Rozsah** | tasks.md T-2..T-7 (etapa E-1) · CHANGES 3, 4, 5 · BUGS B001 |
| **Tokeny** | ~N (součet úseků, odhad) |
| **Změny kódu** | N souborů, +N / −N řádků |

## Zadání

> citace autora, jak sprint zadal

## Plán

- [ ] bod – co přesně se udělá (odkaz na úkol T-n / CHANGES / BUGS)
- [ ] testy, dokumentace, CHANGELOG, vydání

## Průběh

| Čas | Co | Tokeny (zbývá / úsek) |
|---|---|---|
| HH:MM | začátek – analýza … | 15 000 000 / – |
| HH:MM | … | 14 950 000 / 50 000 |
| HH:MM | konec | … |

## Rozhodnutí během sprintu

- co se rozhodlo, kdo (autor / agent) a proč

## Výsledek

- **Commity:** `abc1234` popis, …
- **Vydání:** 0.X.Y, image serveru, instalátor desktopu
- **Testy:** N/N (doba)
- **Diagnostika:** tokeny ~N (součet úseků), změny N souborů +N/−N
- **Stav bodů:** CHANGES 3 → hotovo (0.X.Y) …

## Nestihlo se / otevřené

- co zbylo a proč, návrh na další sprint

## Poznámky

- problémy prostředí, věci k zapamatování
```
