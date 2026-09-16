# Cruma — Testing Strategy

> **Type:** Strategy
> **Scope:** shared
> **Mandatory parts registered as:** TST-001..TST-005
> **Relates to:** AI-DevKit `testing/` documents (generic methodology); this document states what is specific to Cruma

## Purpose

States where testing effort goes in Cruma and why. The system's risk is concentrated in a few pure libraries
(merge, normalization, schema) and in user isolation; tests are weighted accordingly.

---

# 1. Priorities

| Priority | Area | Why | Kind |
|---|---|---|---|
| 1 | Merge and conflicts (`Cruma.Versioning`) | Silent data loss is the worst failure (NFR-4, R-2) | unit, scenario tables (TST-001) |
| 2 | User isolation (server) | One user seeing another's notes is the worst security failure (FR-31) | integration (TST-004) |
| 3 | Search equivalence | Different results on desktop and phone break trust (FR-24) | conformance suite on both adapters (TST-002) |
| 4 | Schema migrations and export round-trip (`Cruma.Content`) | Content must survive upgrades and exports (R-3, FR-12) | unit with fixtures (TST-001) |
| 5 | Sync protocol end-to-end | Pending changes must survive every failure (SYN-004, ERR-002) | integration: desktop sync client against a test server |
| 6 | Domain rules (`Cruma.Notes`, `Cruma.Kanban`) | Business rules such as default category | unit |
| 7 | UI components with logic | Quick capture flow, conflict resolution panel | bUnit |

Low value, not written by default: tests of thin endpoints that only map, tests of EF configuration without
queries, snapshot tests of markup.

---

# 2. Tooling

| Purpose | Tool |
|---|---|
| Test framework | NUnit |
| Test doubles | Moq, only at module and platform boundaries |
| Component tests | bUnit |
| PostgreSQL in tests | Testcontainers for .NET (TST-003) |
| Container runtime locally | Podman; Testcontainers connects through the Podman socket (`DOCKER_HOST` pointing to the Podman API socket; the resource reaper may need to be disabled for rootless Podman) |
| SQLite in tests | real SQLite file in a temporary folder |

Library versions and exact configuration live in `Directory.Packages.props` and the test projects, not here.

---

# 3. Merge scenario tests

Merge tests are table-driven: each scenario is `base`, `current`, `incoming`, `expected` documents plus expected
conflict markers. Minimum scenario set:

1. Different blocks changed on each side → both changes kept (FR-28 acceptance 1).
2. Same block changed differently → conflict with both variants (acceptance 2).
3. Conflict resolved → new version without conflict marker (acceptance 3).
4. Edit vs. delete, both orders → edit preserved (acceptance 4).
5. Identical change on both sides → no conflict.
6. Reordering on one side + edit on the other → both kept, no conflict.
7. Block added on both sides at the same position → both kept, deterministic order.
8. Tags added and removed on both sides → set merge.
9. Title changed on both sides → later wins, overwritten value present in version record.
10. Unknown block type present → preserved through merge.

A merge bug found later adds a scenario before it is fixed.

---

# 4. Search conformance suite

One abstract test class with fixture data (Czech text with diacritics, mixed case, digits, punctuation) and
queries with expected result identifiers; two concrete classes run it against the SQLite adapter and the
PostgreSQL adapter (TST-002). Required queries include `certifikat` → `Certifikát` and `certif` →
`certifikátu`.

---

# 5. User isolation tests

For every user-owned entity and endpoint: two users, user B attempts read, list, search, update, delete, blob
download, export, and AI scope load of user A's data; every attempt yields `not_found` or an empty result
(TST-004).

---

# 6. Determinism

Time, identifiers, and randomness are injected (`TimeProvider`, identifier generator) (TST-005). Tests run in any
order; unit test fixtures are marked parallelizable (NUnit `[Parallelizable]`), and each integration test
fixture uses its own database. Merge scenario tables (§3) and the search conformance suite (§4) use NUnit
parameterized tests (`[TestCaseSource]`) and an abstract base fixture respectively.

---

# End of Document
