# PROTOKOL.md

Append-only auditní log běhu. Interní, zobrazuje se jen na vyžádání.

```text
[2026-09-15 20:15] GATE    kind=premise detail=session premise confirmation presented (12 items) decision=pending
[2026-09-15 20:19] GATE    kind=premise detail=operator confirmed all items unchanged decision=approved
[2026-09-15 20:19] LOAD    doc=c:\MyWork\_project\google-keep-client\concept.md class=mandatory phase=SESSION_START reason=jediny vstupni artefakt, zaklad premisy escalated=no
[2026-09-15 20:19] CERT    phase=FRAME completeness=P uniqueness=U traceability=C outcome=mezery zaznamenany jako O-1..O-6, interpretace konceptu jednoznacna, pokracuji
[2026-09-15 20:19] ASSUME  id=A-1 phase=SESSION_START chosen=koncept je aktualni a uplny vstup alternatives=existuje novejsi zadani nebo neuvedena omezeni accepted_by=operator
[2026-09-15 20:24] LOAD    doc=operator message class=mandatory phase=FRAME reason=kandidat na stack (.NET/Blazor) k O-2 escalated=no
[2026-09-15 20:25] GATE    kind=phase detail=FRAME completion proposal -> ANALYSIS decision=pending
[2026-09-15 20:31] LOAD    doc=operator message class=mandatory phase=FRAME reason=upresneni topologie klientu (desktop full, web/mobil/tablet tenky) escalated=no
[2026-09-15 20:31] GATE    kind=phase detail=FRAME completion proposal stazen, doplnen par.2.1 a O-7, O-6 vyresen decision=rejected
[2026-09-15 20:33] CERT    phase=FRAME completeness=P uniqueness=U traceability=C outcome=topologie klientu jednoznacna, zbyle mezery vedeny jako O-1..O-5, O-7
[2026-09-15 20:33] GATE    kind=phase detail=FRAME completion proposal (2) -> ANALYSIS decision=pending
[2026-09-15 20:38] LOAD    doc=operator message class=mandatory phase=FRAME reason=pozadavek na vymazlene UI na mobilu a tabletu escalated=no
[2026-09-15 20:38] GATE    kind=phase detail=FRAME completion proposal (2) stazen, doplneno oddeleni osy dat a osy shellu, 3 kandidati v par.7 decision=rejected
[2026-09-15 20:39] GATE    kind=phase detail=FRAME completion proposal (3) -> ANALYSIS decision=pending
[2026-09-15 20:41] GATE    kind=phase detail=FRAME -> ANALYSIS decision=approved
[2026-09-15 20:41] LOAD    doc=docs\arch\frame.md class=mandatory phase=ANALYSIS reason=schvaleny vystup predchozi faze escalated=no
[2026-09-15 20:41] CERT    phase=ANALYSIS completeness=Y uniqueness=U traceability=C outcome=greenfield, zadny kod ke cteni, pokracuji
[2026-09-15 20:48] CERT    phase=ANALYSIS completeness=Y uniqueness=U traceability=C outcome=9 rozporu K-1..K-9, nove mezery O-8..O-11, zadna lokace (greenfield)
[2026-09-15 20:48] GATE    kind=phase detail=ANALYSIS completion proposal -> SPEC decision=pending
[2026-09-15 20:55] LOAD    doc=operator message class=mandatory phase=ANALYSIS reason=odpovedi ke K-1..K-6, O-8..O-11 escalated=no
[2026-09-15 20:55] GATE    kind=phase detail=ANALYSIS completion proposal stazen, zapracovana rozhodnuti operatora do par.7 decision=rejected
[2026-09-15 20:56] ASSUME  id=A-2 phase=ANALYSIS chosen=objemy dle analysis.md par.7 alternatives=vyssi objemy (vicero uzivatelu, statisice poznamek) accepted_by=model (ceka na operatora)
[2026-09-15 20:56] CERT    phase=ANALYSIS completeness=P uniqueness=U traceability=O outcome=O-1 bez odpovedi, A-2 ceka na potvrzeni
[2026-09-15 20:56] GATE    kind=phase detail=ANALYSIS completion proposal (2) -> SPEC decision=pending
[2026-09-15 21:01] LOAD    doc=operator message class=mandatory phase=ANALYSIS reason=dotaz K-6 (unicode), upresneni K-5 (Symbolon) escalated=no
[2026-09-15 21:01] GATE    kind=phase detail=ANALYSIS completion proposal (2) stazen, K-6 rozpracovan na 4 kroky normalizace decision=rejected
[2026-09-15 21:02] GATE    kind=phase detail=ANALYSIS completion proposal (3) -> SPEC decision=pending
[2026-09-15 21:04] LOAD    doc=operator message class=mandatory phase=ANALYSIS reason=potvrzeni K-6 kroky 1-3 (shodna normalizace) escalated=no
[2026-09-15 21:04] GATE    kind=phase detail=ANALYSIS completion proposal (3) trvá; K-6 krok 4, O-1, A-2 bez odpovedi decision=pending
[2026-09-15 21:08] LOAD    doc=operator message class=mandatory phase=ANALYSIS reason=K-6 sklonovani ano (prefix), O-1 tri kriteria staci, A-2 potvrzeno jako horni hranice escalated=no
[2026-09-15 21:08] ASSUME  id=A-2 phase=ANALYSIS chosen=objemy dle NFR-7, verze pocitany zvlast, horni hranice pro navrh alternatives=verze zapocitany do 50 000 accepted_by=operator
[2026-09-15 21:08] GATE    kind=phase detail=ANALYSIS -> SPEC (podminky proposal (3) splneny) decision=approved
[2026-09-15 21:08] LOAD    doc=docs\arch\analysis.md class=mandatory phase=SPEC reason=schvaleny vystup predchozi faze escalated=no
[2026-09-15 21:08] LOAD    doc=c:\MyWork\_project\google-keep-client\concept.md class=mandatory phase=SPEC reason=zdroj pozadavku (reread: zmena faze) escalated=no
[2026-09-15 21:09] CERT    phase=SPEC completeness=Y uniqueness=A traceability=C outcome=3 nejednoznacnosti (kategorie 5.2 x 7.1, rozsah fronty O-7, Kanban na mobilu) navrzeny jako A-3, A-4, A-5
[2026-09-15 21:09] ASSUME  id=A-3 phase=SPEC chosen=nesmazatelna vychozi kategorie alternatives=kategorie volitelna | kategorie povinna pri vytvoreni accepted_by=model (ceka na operatora)
[2026-09-15 21:09] ASSUME  id=A-4 phase=SPEC chosen=fronta pouze pro nove poznamky alternatives=i doplneni na konec | i uprava existujicich accepted_by=model (ceka na operatora)
[2026-09-15 21:09] ASSUME  id=A-5 phase=SPEC chosen=Kanban na tenkem klientovi cteni a lehke upravy alternatives=plna prace s boardem accepted_by=model (ceka na operatora)
[2026-09-15 21:24] CERT    phase=SPEC completeness=Y uniqueness=A traceability=C outcome=FR-1..FR-37, NFR-1..NFR-20, kazdy s pramenem; A-3..A-5 cekaji
[2026-09-15 21:24] GATE    kind=phase detail=SPEC completion proposal -> PLAN, vcetne potvrzeni A-3, A-4, A-5 decision=pending
[2026-09-15 21:31] LOAD    doc=operator message class=mandatory phase=SPEC reason=A-3 upresneno (vse do vychozi kategorie), A-4 ok, A-5 ok escalated=no
[2026-09-15 21:31] ASSUME  id=A-3 phase=SPEC chosen=kazda nova poznamka do vychozi kategorie bez ohledu na kontext alternatives=volitelna | povinna | dle aktualniho pohledu (REJECTED) accepted_by=operator
[2026-09-15 21:31] ASSUME  id=A-4 phase=SPEC chosen=fronta pouze pro nove poznamky alternatives=doplneni | uprava accepted_by=operator
[2026-09-15 21:31] ASSUME  id=A-5 phase=SPEC chosen=Kanban na tenkem klientovi cteni a lehke upravy alternatives=plna prace accepted_by=operator
[2026-09-15 21:31] GATE    kind=phase detail=SPEC -> PLAN decision=approved
[2026-09-15 21:31] LOAD    doc=docs\arch\spec.md class=mandatory phase=PLAN reason=schvaleny vystup predchozi faze escalated=no
[2026-09-15 21:32] CERT    phase=PLAN completeness=Y uniqueness=A traceability=C outcome=D-1 a D-2 maji realne tradeoffy, vyzaduji rozhodnuti operatora
[2026-09-15 21:38] GATE    kind=decision detail=D-1 stack a shelly, D-2 reprezentace obsahu decision=pending
[2026-09-15 21:42] GATE    kind=decision detail=D-1 = a (.NET napric, WPF+BlazorWebView, WASM PWA, MAUI pozdeji) decision=approved
[2026-09-15 21:42] GATE    kind=decision detail=D-2 = a (JSON strom, TipTap, stabilni ID bloku) decision=approved
[2026-09-15 21:43] CERT    phase=PLAN completeness=Y uniqueness=A traceability=C outcome=D-3 (4 dilci volby) a D-4 maji realne tradeoffy
[2026-09-15 21:50] GATE    kind=decision detail=D-3 verzovani a slucovani (D-3.1..D-3.4), D-4 vyhledavani decision=pending
[2026-09-15 21:53] GATE    kind=decision detail=D-3.1 a, D-3.2 a, D-3.3 a, D-3.4 a (vedome prijat vyklad NFR-4), D-4 a decision=approved
[2026-09-15 21:54] CERT    phase=PLAN completeness=P uniqueness=A traceability=C outcome=D-6.1 vyzaduje informace operatora (O-3); D-5 a D-6 predlozeny
[2026-09-15 22:02] GATE    kind=decision detail=D-5 (5.1, 5.2) bloby, D-6 (6.1..6.4) provoz, DB, sifrovani, identita decision=pending
[2026-09-15 22:08] GATE    kind=decision detail=D-5.1 a, D-5.2 a, D-6.2 a, D-6.3 a, D-6.4 a decision=approved
[2026-09-15 22:08] LOAD    doc=operator message class=mandatory phase=PLAN reason=D-6.1 hosting nevybran, pozadavek .NET, levne ale solidne escalated=no
[2026-09-15 22:09] CERT    phase=PLAN completeness=Y uniqueness=A traceability=C outcome=D-6.1 upresneno na a1/a2/b s doporucenim a1; D-7, D-8 predlozeny
[2026-09-15 22:18] GATE    kind=decision detail=D-6.1 hosting, D-7 pripominky, D-8 (8.1..8.3) AI vrstva decision=pending
[2026-09-15 22:22] GATE    kind=decision detail=D-6.1 a1 (Hetzner Cloud), D-7 a, D-8.1 a, D-8.2 c, D-8.3 a decision=approved
[2026-09-15 22:23] CERT    phase=PLAN completeness=Y uniqueness=A traceability=C outcome=komponenty C-1..C-19 navrzeny; D-9 (rozsah a poradi inkrementu) ma realne tradeoffy
[2026-09-15 22:31] GATE    kind=decision detail=D-9 rozsah prvni verze a poradi inkrementu decision=pending
[2026-09-15 22:34] GATE    kind=decision detail=D-9 = b (walking skeleton, I-1..I-5) decision=approved
[2026-09-15 22:36] LOAD    doc=operator message class=mandatory phase=PLAN reason=lokalni Podman, .NET 10; upresneni D-1 a D-6.1 bez zmeny rozhodnuti escalated=no
[2026-09-15 22:37] CERT    phase=PLAN completeness=Y uniqueness=U traceability=C outcome=vsechna D-n rozhodnuta, pokracuji zapisem plan.md
[2026-09-15 22:52] CERT    phase=PLAN completeness=Y uniqueness=U traceability=C outcome=C-1..C-19 (C-7a/b, C-15a/b), FR-1..FR-37 a NFR-1..NFR-20 prirazeny komponentam i inkrementum, zadny sirotek
[2026-09-15 22:52] GATE    kind=phase detail=PLAN completion proposal -> CLOSED (beh konci po PLAN dle premisy bod 6; enum v7.1 nema PLAN->CLOSED) decision=pending
[2026-09-15 22:55] GATE    kind=phase detail=PLAN -> CLOSED decision=approved
```
