# Otevřené body

Rozpory, nejasnosti a části přeskočené kvůli úkolům autora, které agent nerozhodl s jistotou
(pravidlo v `AGENTS.md`, sekce Architektura a zdroje pravdy). Autor je projde a rozhodne; vyřešený bod se
přesune do *Vyřešeno* s rozhodnutím a datem.

## Otevřené

| # | Zapsáno | Sprint / úkol | Bod | Proč nejisté | Varianty a doporučení |
|---|---|---|---|---|---|
| 1 | 2026-09-16 | 003 / T-22, výstup E-3 | Přihlášení Googlem na webu neověřeno | Chybí OAuth klient (T-19, autor). Kód je zapojený podmíněně: `/auth/sign-in/google` → `/auth/callback` → založení uživatele a cookie session. | Po T-19 (user-secrets `Cruma:Identity:Google:ClientId`/`ClientSecret`, redirect `https://localhost:5001/signin-google`) spustit server a přihlásit se v prohlížeči; agent pak doplní případné opravy. |
| 2 | 2026-09-16 | 003 / T-20, T-22 | Přihlášení desktopu tokenem (authorization code + PKCE přes systémový prohlížeč, SEC-002) | Prototyp T-20 musí ověřit člověk přihlášením Googlem; profil doporučuje OpenIddict. Synchronizační endpointy zatím přijímají cookie session – pro desktop nepoužitelné. | Po T-19: OpenIddict na serveru (authorization + token endpoint, PKCE, refresh token), desktop s loopback redirectem; bearer schéma pro `/api/sync/v1`. Doporučuji udělat jako první úkol dalšího sprintu. |
| 4 | 2026-09-17 | 004 / T-45, T-46; FR-29 akc. 3, FR-30 akc. 4 | Instalace PWA do telefonu a přežití fronty po zavření klienta bez serveru | Service worker se v prohlížeči s vývojovým certifikátem localhost nezaregistruje; bez něj se shell bez serveru nenačte. Kód (manifest, ikony, publikovaný service worker, fronta v IndexedDB) je hotový. | Ověřit na produkci s platným TLS v rámci T-63: nainstalovat PWA do telefonu, vypnout data, zapsat poznámku, zavřít a znovu otevřít, zapnout data. |
| 3 | 2026-09-16 | 003 / security-policy §4, FR-35 akc. 1 | Omezení četnosti (rate limiting) na autentizačních endpointech a audit `security.rate_limited`, `security.authorization_denied` | Žádný úkol I-1 to nejmenuje, ale FR-35 akc. 1 tyto události vyjmenovává. Nejasné, zda patří do I-1. | ASP.NET Core rate limiter na `/auth/*` s auditem odmítnutí; `authorization_denied` při 403. Doporučuji zařadit do E-6 (nasazení) jako součást hardeningu. |

## Vyřešeno

| # | Vyřešeno | Bod | Rozhodnutí |
|---|---|---|---|
