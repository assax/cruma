# Změny k zapracování

Sběr připomínek. Zapracovává se až na výslovný pokyn **„zapracuj změny“**.
Do té doby se nic nestaví ani nespouští – jen se sem zapisuje a u každé
položky je rozbor, příčina (když je z kódu jasná) a návrh řešení.

Stav: `čeká` → `hotovo (verze)`

---

## 1) Vývojový build desktopu s vlastními daty

*Zadáno 2026-09-16* · Stav: **hotovo (nevydáno)**

**Připomínka:** v Susceptoru má vývojový build (Debug) vlastní data v `%LOCALAPPDATA%\Susceptor\dev` a v titulku
„(dev)“, takže vývoj a zkoušky nesahají na data nainstalované verze. Cruma má podle profilu
(`shared/coding-conventions.md` §1) data jen v `%LOCALAPPDATA%\Cruma\`. U aplikace s lokální databází poznámek
by vývojový build pracoval nad skutečnými poznámkami autora.

**Návrh:** doplnit do profilu pravidlo, že Debug build používá `%LOCALAPPDATA%\Cruma\dev\` a má v titulku
„(dev)“, a promítnout ho do úkolu T-47 (lokální úložiště desktopu). Změna profilu – potvrdit autorem.

**Zapracováno 2026-09-16** (potvrzeno autorem): pravidlo `PER-006` v `shared/architecture-rules.md`, promítnuto do
`coding-conventions.md` §1, `desktop-pattern.md` §2, `logging-and-audit-policy.md` §4 a do úkolu T-47.
