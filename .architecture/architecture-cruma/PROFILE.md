# PROFILE.md — Cruma Architecture Profile

> **Type:** Profile manifest (consumed by AI-DevKit v4/v5 per `engine/architecture-profile-model.md`, and by
> AI-BIG-ARCH-ASSISTENT ENGINE v7.1 §12)
> **Profile root path:** this folder (`architecture-cruma/`)
> **Owner:** Richard Ludvik (System Architect) — authored and owned by the project, not by `.aidevkit`
> **Stack:** .NET 10 / C#; ASP.NET Core server; Blazor (shared Razor Class Library) hosted in WPF BlazorWebView
> (desktop), Blazor WebAssembly PWA (web, tablet), MAUI Blazor Hybrid (mobile, later); TipTap editor as a JS
> island; EF Core with PostgreSQL (server) and SQLite (desktop); OCI containers on Hetzner Cloud
> **Derived from:** `docs/arch/plan.md` (C-1..C-19, §2–§5) and `docs/arch/DECISIONS.md` (D-1..D-9), both
> approved 2026-09-15

This manifest declares what belongs to the Cruma profile, what loads unconditionally, and what loads only when
relevant to the current feature's scope. Where this profile and `plan.md` disagree, the conflict is escalated to
the operator — neither silently wins.

---

## Mandatory baseline

Loaded unconditionally whenever this profile is active, regardless of feature scope.

| Document | Type | Role |
|---|---|---|
| `shared/architecture-rules.md` | Rules | Central enforceable rule registry: DEP, STR, NAM, CNT, VER, SYN, SRC, BLB, API, PER, UI, SEC, AI, ERR, LOG, AUD, TST, OPS. Every mandatory statement in any other document of this profile is registered here with a RuleID. |

---

## Supporting documents

| Document | Type | Scope | Load when relevant to | How to apply |
|---|---|---|---|---|
| `shared/solution-structure-template.md` | Template | shared | creating a project, placing code in a project or folder, adding a server module | Reference skeleton — enforces STR-xxx, DEP-xxx |
| `shared/content-document-pattern.md` | Pattern | shared, clients | document schema, block types, editor integration, export, plain-text extraction | Default design — enforces CNT-xxx |
| `shared/versioning-and-sync-pattern.md` | Pattern | shared, server, desktop | versions, merge, conflicts, sync protocol, write queue, blob sync | Default design — enforces VER-xxx, SYN-xxx, BLB-xxx |
| `shared/search-pattern.md` | Pattern | shared, server, desktop | search, normalization, index adapters, AI context retrieval | Default design — enforces SRC-xxx |
| `server/server-pattern.md` | Pattern | server | endpoints, application services, persistence, server modules, identity, AI gateway, reminders | Default server design — enforces API-xxx, PER-xxx |
| `clients/ui-pattern.md` | Pattern | clients | Razor components, platform services, shells, editor island, thin client | Default UI design — enforces UI-xxx |
| `clients/desktop-pattern.md` | Pattern | desktop | WPF shell, local storage, sync client, local reminders, login, updates | Default desktop design |
| `shared/security-policy.md` | Policy | shared | authentication, authorization, secrets, user isolation, AI data boundary | Governing principles — rules in SEC-xxx, AI-xxx |
| `shared/error-handling-policy.md` | Policy | shared | exception flow, ProblemDetails, sync failures, user-facing errors | Governing principles — rules in ERR-xxx |
| `shared/logging-and-audit-policy.md` | Policy | shared | logging, audit event emission, sensitive data | Governing principles — rules in LOG-xxx, AUD-xxx |
| `shared/testing-strategy.md` | Strategy | shared | test design, test scope, integration tests, conformance suites | Directional; mandatory parts registered in TST-xxx |
| `shared/coding-conventions.md` | Convention | shared | naming, namespaces, C# style, file organization | Apply consistently — not architecture-breaking |
| `infra/deployment-pattern.md` | Pattern | infra | containers, local dev environment, deployment, backups, restore | Default infra design — enforces OPS-xxx |

---

## Scope tags

- `shared` — cross-cutting libraries and rules that apply regardless of declared scope
- `server` — `Cruma.Server*` (ASP.NET Core host, server modules, PostgreSQL persistence)
- `clients` — `Cruma.Ui`, `Cruma.Ui.Editor`, `Cruma.Web`, `Cruma.Mobile`, `Cruma.Api.Client`
- `desktop` — `Cruma.Desktop`, `Cruma.Desktop.Storage`, `Cruma.Sync.Client` (a desktop feature also implies `clients`)
- `infra` — `deploy/`, container definitions, backups

A feature whose Code Scope includes `desktop` must also load documents tagged `clients`.

---

## Excluded from runtime load set

| Document | Reason |
|---|---|
| `README.md` | Human index of this folder; duplicates this manifest. |

---

## Open profile items

Not blocking I-1. Must be resolved by the run for the increment that needs them.

| ID | Item | Needed by | Why it is open |
|---|---|---|---|
| OP-1 | Offline preview of PlantUML diagrams on desktop. PlantUML renders with a Java-based renderer, not in the browser. | I-2 (FR-11) | Conflicts with SPEC NFR-3 (all desktop functionality offline). Candidate resolution: render on a server-side renderer container and cache the rendered SVG as a blob keyed by source hash; offline shows the last cached preview, uncached shows source. This narrows NFR-3 and needs operator approval. Mermaid is not affected (renders in the browser). |
| OP-2 | Automatic code formatting (FR-10) for languages without a browser-side formatter, notably C#. | I-2 (FR-10) | Browser formatters cover web languages only. Candidate resolution: format only supported languages; hide the action otherwise. |

---

# End of Document
