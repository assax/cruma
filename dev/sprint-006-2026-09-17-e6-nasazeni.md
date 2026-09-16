# Sprint 006 – E-6 Nasazení (příprava bez produkce)

| | |
|---|---|
| **Začátek** | 2026-09-17 01:16 |
| **Konec** | 2026-09-17 01:28 |
| **Délka** | 12 min (čistá, bez pauz: 12 min) |
| **Stav** | připraveno a vyzkoušeno nanečisto; produkce (T-56, T-62 na VPS, T-63) čeká na autora |
| **Verze** | – (nevydáno) |
| **Rozsah** | tasks.md T-57..T-62 připravené a vyzkoušené lokálně; T-56 a T-63 jsou produkce (autor) · základ `dc3b824` |
| **Tokeny** | ~33 000 (součet úseků, odhad) |
| **Změny kódu** | 17 souborů, +608 / −5 řádků (vč. záznamu sprintu) |

## Zadání

> (pokračování autonomní práce – „bez dál co můžeš“; E-6 formálně čeká na T-56)

## Plán

- [x] T-57 – image serveru (Dockerfile), produkční `compose.yaml`: proxy Caddy, server, migrate, db, backup; sítě,
  svazky, pevné verze, tajné údaje z `.env`; příkaz `migrate` serveru; forwarded headers za proxy
- [x] T-58 – automatické zálohy restic (denně + před nasazením), šifrované, retence
- [x] T-59 – `deploy/deploy.sh` podle deployment-pattern §3 s kontrolou zdraví a návratem
- [x] T-60 – workflow release: image do GHCR, instalátor desktopu na Windows runneru (neověřeno – běží až na značku)
- [x] T-61 – feed desktopu ve svazku serveru, allowlist, postup nahrání (`deploy/README.md`)
- [x] T-62 – `deploy/RESTORE.md` a zkouška obnovy nanečisto lokálně (produkční zkouška autor)
- [x] lokální zkouška celého stacku v Podmanu, CHANGELOG, commit

## Průběh

| Čas | Co | Tokeny (zbývá / úsek) |
|---|---|---|
| 01:16 | začátek – analýza E-6, deployment pattern, dostupné verze image (aspnet 10.0.12, sdk 10.0.401, caddy 2.11.4, restic 0.19.1) | 14 455 000 / 12 000 |
| 01:19 | image serveru `cruma-server:0.1.0` v Podmanu (1 min 52 s), příkaz `migrate`, forwarded headers | 14 451 000 / 4 000 |
| 01:21 | compose produkce, Caddyfile, image záloh; povinné proměnné (`:?`) rozbíjely druhý profil – kontrolu převzal `deploy.sh` | 14 441 000 / 10 000 |
| 01:22 | `deploy.sh 0.1.0` nanečisto (Podman, localhost, porty 18080/18443): 40 s, zdravý; TLS přes Caddy, HSTS, 308 na HTTPS, jen proxy publikuje porty, migrace aplikované, vývojové přihlášení v Production vypnuté | 14 434 000 / 7 000 |
| 01:24 | záloha restic → smazání svazku databáze → `restore.sh latest` → značkový záznam obnoven, server zdravý | 14 429 000 / 5 000 |
| 01:27 | RESTORE.md, release workflow, README; úklid stacku (vývojová DB zachována); celé řešení 324/324 | 14 423 000 / 6 000 |
| 01:28 | konec – CHANGELOG, commit | 14 422 000 / 1 000 |

## Rozhodnutí během sprintu

- **Image serveru z chiseled `aspnet:10.0.12`** – neprivilegovaný uživatel `app`, bez shellu; sestavení v SDK image
  s Node.js zkopírovaným z `node:24.9.0` (editor webového klienta).
- **Migrace jako `Cruma.Server migrate`** stejného image (služba `migrate`, profil `tools`), nikdy při startu (PER-003).
- **Server za proxy věří `X-Forwarded-*` jen s `Cruma__BehindProxy=true`** (nastaveno v image) – v produkci je
  server dostupný jen v síti kontejnerů.
- **Sítě:** `internal` (bez výstupu) pro db, server, migrate, backup; `edge` pro proxy a server (server potřebuje
  výstup kvůli Google OAuth); `egress` pro zálohy do S3. Porty publikuje jen proxy.
- **Zálohy:** image z `postgres:18.6-alpine` (pg_dump ve verzi serveru) + restic 0.19.1, neprivilegovaný uživatel,
  plánování jednoduchou smyčkou (bez cronu vyžadujícího root), retence 7 denních / 5 týdenních / 12 měsíčních.
- **Povinné proměnné nejsou v compose (`:?`)** – compose interpoluje celý soubor pro všechny profily, takže by vývoj
  vyžadoval produkční tajné údaje a naopak. Přítomnost produkčních proměnných kontroluje `deploy.sh`.
- **Feed desktopu** jako svazek `desktop-feed` připojený do serveru jen pro čtení.
- **Release workflow** buildí image z `deploy/server.Dockerfile` a instalátor přes `pack.ps1` na Windows runneru;
  adresa feedu z proměnné repozitáře `CRUMA_DESKTOP_FEED_URL`.

## Výsledek

- **Commity:** „E-6: příprava nasazení – image, produkční compose, zálohy, deploy, obnova, release“
- **Vydání:** žádné
- **Testy:** celé řešení 324/324 (beze změny počtu; změny serveru jen v Program.cs)
- **Zkouška nanečisto:** nasazení 0.1.0 přes `deploy.sh` v Podmanu, HTTPS přes Caddy, záloha a obnova po smazání
  svazku databáze – zapsáno v `deploy/RESTORE.md` (nenahrazuje zkoušku na jednorázovém serveru)
- **Diagnostika:** tokeny ~33 000 (součet úseků), změny 17 souborů +608/−5
- **Stav bodů:** –

## Nestihlo se / otevřené

- **T-56** doména, VPS, úložiště záloh (autor) → pak první produkční `deploy.sh`.
- **T-62 na jednorázovém VPS** a zápis data do `RESTORE.md` (OPS-003) – před uložením skutečných dat.
- **T-63** ověření na produkci (PWA v telefonu, desktop, sloučení, konflikt) – spolu s `dev/OTEVRENE.md` 4, 6.
- **Release workflow neověřen** – spustí se až na značku `v*.*.*` po pushi.
- **Docker** (ne Podman) ověří CI krok compose při pushi; produkční profil v Dockeru neověřen.

## Poznámky

- Git Bash převádí argumenty začínající `/` na cesty Windows – `compose run --entrypoint /usr/local/bin/…` potřebuje
  `MSYS_NO_PATHCONV=1`.
- `compose down` bez výběru svazků by smazal i vývojovou databázi (`cruma_db-dev-data`, stejný projekt `cruma`) –
  úklid zkoušky mazal svazky jmenovitě.
- Caddy pro doménu `localhost` automaticky použije vlastní lokální CA – vhodné pro zkoušku nanečisto.
