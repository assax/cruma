# PROTOKOL.md — beh I-1

Append-only auditni log. Interni, zobrazuje se jen na vyzadani.

```text
[2026-09-16 09:10] GATE    kind=premise detail=session premise confirmation for increment I-1 (12 items) decision=pending
[2026-09-16 09:12] GATE    kind=premise detail=operator confirmed all items unchanged decision=approved
[2026-09-16 09:12] LOAD    doc=docs\arch\spec.md class=mandatory phase=SESSION_START reason=zdroj pozadavku pro I-1 escalated=no
[2026-09-16 09:12] LOAD    doc=docs\arch\plan.md class=mandatory phase=SESSION_START reason=rozsah I-1 (par.4, par.5) a komponenty escalated=no
[2026-09-16 09:12] LOAD    doc=docs\arch\DECISIONS.md class=mandatory phase=SESSION_START reason=zavazna rozhodnuti D-1..D-9 escalated=no
[2026-09-16 09:12] LOAD    doc=.architecture\architecture-cruma\PROFILE.md class=mandatory phase=SESSION_START reason=manifest profilu, basis=PROFILE escalated=no
[2026-09-16 09:13] ASSUME  id=A-1 phase=SESSION_START chosen=schvalene artefakty behu 2026-09-15 plati nezmenene alternatives=nektery artefakt je zastaraly accepted_by=operator
[2026-09-16 09:13] ASSUME  id=A-2 phase=SESSION_START chosen=profil Cruma je autoritativni i tam kde jde nad ramec plan.md alternatives=plan.md ma prednost pred profilem accepted_by=operator
[2026-09-16 09:13] CERT    phase=FRAME completeness=Y uniqueness=U traceability=C outcome=rozsah I-1 odvozen z plan.md par.4, mezery vedeny jako O-1..O-5
[2026-09-16 09:20] GATE    kind=phase detail=FRAME completion proposal -> ANALYSIS decision=pending
[2026-09-16 09:31] GATE    kind=phase detail=FRAME -> ANALYSIS decision=approved
[2026-09-16 09:31] LOAD    doc=docs\arch\i-1\frame.md class=mandatory phase=ANALYSIS reason=schvaleny vystup FRAME escalated=no
[2026-09-16 09:31] LOAD    doc=.architecture\architecture-cruma\shared\architecture-rules.md class=mandatory phase=ANALYSIS reason=zavazna pravidla, dopad na rozsah I-1 escalated=no
[2026-09-16 09:32] LOAD    doc=.architecture\architecture-cruma\shared\solution-structure-template.md class=supplementary phase=ANALYSIS reason=projekty I-1 a povolene reference (RB-4) escalated=no
[2026-09-16 09:32] LOAD    doc=.architecture\architecture-cruma\{shared,server,clients,infra}\*-pattern.md class=supplementary phase=ANALYSIS reason=patterny dotcene komponentami I-1 (content, versioning-and-sync, search, server, ui, desktop, deployment) escalated=no
[2026-09-16 09:33] LOAD    doc=.architecture\architecture-cruma\shared\{security,error-handling,logging-and-audit}-policy.md class=supplementary phase=ANALYSIS reason=zdroj NFR a povinne prace I-1 escalated=no
[2026-09-16 09:33] LOAD    doc=.architecture\architecture-cruma\shared\testing-strategy.md class=supplementary phase=ANALYSIS reason=povinne testy TST-001..TST-005 escalated=no
[2026-09-16 09:34] LOAD    doc=docs\arch\plan.md par.5 class=mandatory phase=ANALYSIS reason=overeni zarazeni C-7 do I-1 (reread: zmena nejistoty) escalated=no
[2026-09-16 09:34] CERT    phase=ANALYSIS completeness=Y uniqueness=A traceability=C outcome=chyba frame.md par.2.2 (C-7 neni v I-1); mezery M-1..M-6; rozhodovaci body RB-1..RB-5; nova O-6
[2026-09-16 09:48] GATE    kind=phase detail=ANALYSIS completion proposal -> SPEC, vcetne opravy frame.md par.2.2 a odpovedi na RB-1..RB-3 decision=pending
[2026-09-16 10:02] GATE    kind=decision detail=RB-1..RB-5 prijaty (I1-D-1..I1-D-5); O-1 cruma, O-6 GitHub (I1-D-6) decision=approved
[2026-09-16 10:02] GATE    kind=phase detail=ANALYSIS -> SPEC vcetne opravy frame.md par.2.2 (C-7) decision=approved
[2026-09-16 10:03] LOAD    doc=docs\arch\i-1\analysis.md class=mandatory phase=SPEC reason=schvaleny vystup ANALYSIS escalated=no
[2026-09-16 10:03] LOAD    doc=docs\arch\spec.md class=mandatory phase=SPEC reason=zdroj akceptacnich kriterii FR-n pro I-1 (reread: zmena faze) escalated=no
[2026-09-16 10:04] CERT    phase=SPEC completeness=Y uniqueness=A traceability=C outcome=umisteni slozky a rozsah CI po odpovedi O-1/O-6 nejednoznacne, navrzeny A-3 a A-4
[2026-09-16 10:04] ASSUME  id=A-3 phase=SPEC chosen=prejmenovat stavajici slozku na c:\MyWork\_project\cruma alternatives=nova slozka jinde accepted_by=model (ceka na operatora)
[2026-09-16 10:04] ASSUME  id=A-4 phase=SPEC chosen=GitHub Actions: build+testy, publikace image do GHCR a instalatoru; soukromy repozitar alternatives=CI bez publikovani | bez CI accepted_by=model (ceka na operatora)
[2026-09-16 10:21] CERT    phase=SPEC completeness=Y uniqueness=A traceability=C outcome=21 FR (FR-6, FR-7, FR-24, FR-29, FR-35 zuzene), 17 NFR, S-I1-1..S-I1-5, R-8; A-3 a A-4 cekaji
[2026-09-16 10:21] GATE    kind=phase detail=SPEC completion proposal -> PLAN vcetne potvrzeni A-3, A-4 decision=pending
[2026-09-16 10:26] ASSUME  id=A-3 phase=SPEC chosen=prejmenovat slozku na c:\MyWork\_project\cruma alternatives=nova slozka jinde accepted_by=operator
[2026-09-16 10:26] ASSUME  id=A-4 phase=SPEC chosen=GitHub Actions build+testy, GHCR image, instalator; soukromy repozitar alternatives=CI bez publikovani | bez CI accepted_by=operator
[2026-09-16 10:26] GATE    kind=phase detail=SPEC -> PLAN decision=approved
[2026-09-16 10:27] LOAD    doc=docs\arch\i-1\spec.md class=mandatory phase=PLAN reason=schvaleny vystup SPEC escalated=no
[2026-09-16 10:27] CERT    phase=PLAN completeness=Y uniqueness=U traceability=C outcome=zadne rozhodnuti s realnymi tradeoffy krome O-2 (struktura tasks.md)
[2026-09-16 10:44] CERT    phase=PLAN completeness=Y uniqueness=U traceability=C outcome=15 komponent, N-1..N-8, E-1..E-6, vsech 21 FR prirazeno, zadna odchylka od profilu
[2026-09-16 10:44] GATE    kind=phase detail=PLAN completion proposal -> TASKS vcetne rozhodnuti O-2 decision=pending
[2026-09-16 10:52] GATE    kind=decision detail=O-2 = a (tasks.md seskupeny do etap E-1..E-6; operator "ok" k doporuceni) decision=approved
[2026-09-16 10:52] GATE    kind=phase detail=PLAN -> TASKS decision=approved
[2026-09-16 10:53] LOAD    doc=docs\arch\i-1\plan.md class=mandatory phase=TASKS reason=schvaleny vystup PLAN escalated=no
[2026-09-16 10:53] LOAD    doc=.architecture\architecture-cruma\shared\solution-structure-template.md class=mandatory phase=TASKS reason=Template -> scope hinty escalated=no
[2026-09-16 10:53] LOAD    doc=.architecture\architecture-cruma\shared\testing-strategy.md class=mandatory phase=TASKS reason=Strategy -> done-when testu escalated=no
[2026-09-16 10:54] CERT    phase=TASKS completeness=Y uniqueness=U traceability=C outcome=lokace komponent zapsany do TRACEABILITY ze sablony profilu
[2026-09-16 11:40] CERT    phase=TASKS completeness=Y uniqueness=U traceability=C outcome=63 ukolu (3 operator), mechanicky prenos done-when ze spec/plan; inheritance obnovena po kazde etape
[2026-09-16 11:42] CERT    phase=TASKS completeness=Y uniqueness=U traceability=C outcome=validace: vsechna FR I-1 a S-I1-2..S-I1-5 pokryta, 0 unresolved scope hintu, zadna zavislost na neexistujici ukol, zadny ukol na hranici modulu bez hintu
[2026-09-16 11:42] GATE    kind=phase detail=TASKS completion proposal -> CLOSED decision=pending
[2026-09-16 12:30] GATE    kind=phase detail=TASKS -> CLOSED decision=approved
```
