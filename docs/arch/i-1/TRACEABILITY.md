# TRACEABILITY.md — beh I-1

Append-only, jedna vazba na radek. Identifikatory FR-n, NFR-n a C-n odkazuji do docs\arch\spec.md a plan.md.

```text
I-1  <- plan.md par.5 (D-9 varianta b)
I-1  -> FR-1..FR-5, FR-6 (mechanismus), FR-7 (podmnozina), FR-8, FR-24, FR-26..FR-32, FR-34..FR-37
I-1  -> NFR-1..NFR-10, NFR-13, NFR-14, NFR-15, NFR-16, NFR-17, NFR-18, NFR-19
I-1  -> C-1, C-3..C-14, C-17, C-18   (C-2 prazdny projekt kvuli referencim)
O-3  -> blokuje C-18 (TLS) a C-14 (redirect URI)
O-4  -> blokuje FR-32, tim i FR-27, FR-29
O-5  -> blokuje FR-36, OPS-002
C-7  -X  I-1   # oprava frame.md par.2.2: C-7 neni v I-1 (plan.md par.5), BLB-001..BLB-004 a SYN-005 se v I-1 neuplatni
M-1  -> RB-1 -> FR-28, FR-29, NFR-4, VER-002
M-2  -> RB-2 -> FR-6, VER-006
M-3  -> RB-3 -> FR-26, NFR-3
M-4  -> A-3 (system spec) -> FR-1, FR-3
M-5  -> RB-5 -> FR-37
M-6  -> O-6
RB-4 -> DEP-001..DEP-004, STR-001
E-1..E-6 <- plan.md par.5 vnitrni poradi -> O-2
I1-D-1 -> FR-28 akc.5, FR-29
I1-D-2 -> FR-6 akc.3
I1-D-3 -> FR-26 akc.3, FR-3 akc.5
I1-D-4 -> S-I1-2
I1-D-5 -> FR-37 akc.3
I1-D-6 -> A-3, A-4
FR-7   rule: CNT-002, CNT-003, CNT-005
FR-24  rule: SRC-001, SRC-003, TST-002, CNT-007
FR-27  rule: SYN-003, SYN-004, SYN-007, ERR-002
FR-28  rule: VER-005
FR-30  rule: SYN-006
FR-32  rule: SEC-002, SEC-004
FR-35  rule: AUD-001, AUD-002, AUD-004
FR-36  rule: OPS-002, OPS-003, OPS-005
S-I1-2 assumes: A-4
S-I1-5 <- R-8, NFR-7
N-1  -> FR-28, FR-29          rule: VER-002, DEP-007, API-002   <- I1-D-1
N-2  -> FR-27                 rule: SYN-004
N-3  -> FR-6                  rule: VER-006                     <- I1-D-2
N-4  -> FR-3, FR-32           rule: SEC-001                     <- I1-D-3
N-5  -> S-I1-2                rule: DEP-001..DEP-004, STR-001   <- I1-D-4
N-6  -> FR-37                 rule: SYN-001, API-003            <- I1-D-5
N-7  -> S-I1-5, NFR-7
N-8  -> S-I1-2                rule: OPS-004, SEC-006            assumes: A-4
E-1  -> A-3, N-5, N-8, frame R-4
E-2  -> C-1, C-3, C-4, C-5, C-6 (kontrakty)
E-3  -> C-13, C-14, C-17, N-1..N-4, N-6     requires: O-4
E-4  -> C-9, C-10, C-12
E-5  -> C-6, C-8, C-11, N-7
E-6  -> C-18, N-6, N-8               requires: O-3, O-5
(scope hinty) C-n -> projekty dle .architecture\architecture-cruma\shared\solution-structure-template.md par.2, koren c:\MyWork\_project\cruma
# location from profile Template solution-structure-template.md par.2 (new), relative to c:\MyWork\_project\cruma
C-1   @ src/Cruma.Notes/
C-2   @ src/Cruma.Kanban/
C-3   @ src/Cruma.Content/
C-4   @ src/Cruma.Versioning/
C-5   @ src/Cruma.Search/
C-5   @ src/Cruma.Desktop.Storage/Search/
C-5   @ src/Cruma.Server/Search/
C-6   @ src/Cruma.Sync/
C-6   @ src/Cruma.Sync.Client/
C-8   @ src/Cruma.Desktop.Storage/
C-9   @ src/Cruma.Ui/
C-9   @ src/Cruma.Api.Client/
C-10  @ src/Cruma.Ui.Editor/
C-11  @ src/Cruma.Desktop/
C-12  @ src/Cruma.Web/
C-13  @ src/Cruma.Server/
C-13  @ src/Cruma.Server/Infrastructure/
C-13  @ src/Cruma.Server/Notes/
C-13  @ src/Cruma.Server/Sync/
C-13  @ src/Cruma.Api.Contracts/
C-14  @ src/Cruma.Server/Identity/
C-17  @ src/Cruma.Server/Audit/
C-18  @ deploy/
C-18  @ .github/workflows/
(repo) @ Cruma.slnx, Directory.Build.props, Directory.Packages.props, src/, tests/
(tests) @ tests/<Project>.Tests/ per solution-structure-template.md par.4
T-1..T-7    -> E-1   (T-1 owner: operator)
T-8..T-18   -> E-2
T-19..T-31  -> E-3   (T-19 owner: operator, O-4)
T-32..T-46  -> E-4
T-47..T-55  -> E-5
T-56..T-63  -> E-6   (T-56 owner: operator, O-3, O-5)
T-13  done-when: FR-28 akc.1, 2, 4 + testing-strategy par.3 scenare 1,2,4,5,6,7,10
T-14  done-when: FR-28 akc.6 + scenare 8, 9
T-15  done-when: FR-28 akc.3 + scenar 3
T-24  done-when: plan N-1, FR-28 akc.5, FR-5 akc.3
T-29, T-48 done-when: FR-24 akc.2, 3, 4 (TST-002)
T-31  done-when: FR-31 akc.1, 2
T-54  done-when: FR-27 akc.4, FR-28 akc.1, 2, 4, 5
T-55  done-when: S-I1-5, plan N-7
T-62  done-when: FR-36 akc.3, 4, S-I1-4
T-63  done-when: S-I1-3
T-1   assumes: A-3, A-4
T-7, T-60 assumes: A-4
```
