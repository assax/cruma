# Cruma — Solution Structure Template

> **Type:** Template
> **Scope:** shared
> **Enforces:** STR-001..STR-004, DEP-001..DEP-007
> **Derived from:** `docs/arch/plan.md` §2 (components C-1..C-19) and §3 (dependency direction)

## Purpose

This document is the authoritative map from plan components to projects and folders. When a task's scope hint
names a component `C-n`, this template says where that code lives.

---

# 1. Repository layout

```text
<repo root>/
├── Cruma.slnx
├── Directory.Build.props          # nullable, implicit usings, warnings as errors (STR-004)
├── Directory.Packages.props       # central package versions (STR-004)
├── src/
├── tests/
├── deploy/                        # compose.yaml, proxy config, backup and restore scripts (OPS-xxx)
├── docs/
│   └── arch/                      # frame, analysis, spec, plan, DECISIONS
└── .architecture/
    └── architecture-cruma/        # this profile
```

---

# 2. Projects

## 2.1 Shared logic (net10.0, no platform dependencies — DEP-001, DEP-002)

| Project | Plan component | Contains |
|---|---|---|
| `Cruma.Notes` | C-1 | Note, category, tag, note state; default-category rule |
| `Cruma.Kanban` | C-2 | Project, board, column, card, card history; note reference by identifier |
| `Cruma.Content` | C-3 | Document schema, block identifiers, type registry, schema migrations, plain-text extraction, export (lossless, HTML, Markdown) |
| `Cruma.Versioning` | C-4 | Version model, three-way block merge, metadata merge, conflict model, version comparison |
| `Cruma.Search` | C-5 | Normalizer, tokenizer, query model, index adapter interface |
| `Cruma.Sync` | C-6 (protocol) | Sync protocol contracts, protocol and client version compatibility |
| `Cruma.Blobs` | C-7 (shared) | Blob identifier (SHA-256), blob store interface |
| `Cruma.Ai` | C-15a | Provider interface and adapters, AI operation definitions, context assembly, scope description |
| `Cruma.Api.Contracts` | C-13 (contracts) | REST request and response contracts shared with thin clients |

Packages allowed in shared logic beyond the BCL and `Microsoft.Extensions.*.Abstractions`: none initially.
Adding one requires updating this list (STR-001).

## 2.2 UI

| Project | Plan component | Type | Contains |
|---|---|---|---|
| `Cruma.Ui` | C-9 | Razor Class Library | Pages and components for Notes, Kanban, search, AI, history, conflicts, settings; theme tokens; platform service and data service interfaces; capability description |
| `Cruma.Ui.Editor` | C-10 | Razor Class Library with JS bundle | Editor component, TipTap integration, Cruma editor extensions (block identifiers, code block, diagram), interop layer |
| `Cruma.Api.Client` | — | Class library | Typed REST client over `Cruma.Api.Contracts` used by thin clients |

## 2.3 Desktop

| Project | Plan component | Type | Contains |
|---|---|---|---|
| `Cruma.Desktop` | C-11, C-7a | WPF application | BlazorWebView host, Windows platform services, sign-in, local reminder scheduler, notifications, update check, local blob store, composition root |
| `Cruma.Desktop.Storage` | C-8 | Class library | EF Core SQLite context, migrations, local repositories, pending change log, SQLite search index adapter |
| `Cruma.Sync.Client` | C-6 (client) | Class library | Desktop sync engine: change upload with base version, download, blob sync, sync status |

## 2.4 Thin clients

| Project | Plan component | Type | Contains |
|---|---|---|---|
| `Cruma.Web` | C-12 | Blazor WebAssembly PWA | Browser platform services, write queue (IndexedDB), web push subscription, service worker, composition root |
| `Cruma.Mobile` | C-19 | MAUI Blazor Hybrid | Created in increment I-5 only |

## 2.5 Server

| Project | Plan component | Type | Contains |
|---|---|---|---|
| `Cruma.Server` | C-13, C-14, C-7b, C-15b, C-16, C-17 | ASP.NET Core application | Host, endpoints, server modules, EF Core PostgreSQL context and migrations, PostgreSQL search index adapter; also serves `Cruma.Web` static assets |

Server modules are top-level folders of `Cruma.Server` (STR-003):

```text
Cruma.Server/
├── Program.cs                     # composition root: module registration, middleware, endpoint mapping
├── Infrastructure/                # DbContext base, global query filter, ProblemDetails handler, current user
├── Notes/                         # C-13 for C-1
├── Kanban/                        # C-13 for C-2 (increment I-4)
├── Sync/                          # sync endpoint, authoritative merge via Cruma.Versioning
├── Search/                        # PostgreSQL index adapter
├── Blobs/                         # C-7b file-system blob store, garbage collection
├── Identity/                      # C-14 token and session issuing, external login, identity linking
├── Audit/                         # C-17
├── Ai/                            # C-15b AI gateway, encrypted provider keys (increment I-3)
├── Reminders/                     # C-16 scheduling and web push (increment I-2)
└── Export/                        # FR-12 (increment I-2)
```

Each module folder contains:

```text
<Module>/
├── <Module>Module.cs              # one registration method: services + endpoints (STR-003)
├── I<Module>Service.cs            # public surface for other modules (DEP-007)
├── Endpoints/                     # thin endpoints (API-002)
├── Application/                   # application services
└── Persistence/                   # entities and EF configuration, internal to the module
```

---

# 3. Allowed references

```text
Cruma.Notes, Cruma.Kanban, Cruma.Blobs, Cruma.Search   → (nothing)
Cruma.Content                                           → Cruma.Blobs
Cruma.Versioning                                        → Cruma.Content, Cruma.Notes, Cruma.Kanban
Cruma.Sync                                              → Cruma.Versioning, Cruma.Content, Cruma.Notes, Cruma.Kanban, Cruma.Blobs
Cruma.Ai                                                → Cruma.Content, Cruma.Search
Cruma.Api.Contracts                                     → Cruma.Notes, Cruma.Kanban, Cruma.Content

Cruma.Ui                                                → shared logic, Cruma.Ui.Editor
Cruma.Ui.Editor                                         → Cruma.Content
Cruma.Api.Client                                        → Cruma.Api.Contracts

Cruma.Desktop.Storage                                   → shared logic
Cruma.Sync.Client                                       → Cruma.Sync, Cruma.Desktop.Storage
Cruma.Desktop                                           → Cruma.Ui, Cruma.Desktop.Storage, Cruma.Sync.Client, Cruma.Ai
Cruma.Web                                               → Cruma.Ui, Cruma.Api.Client
Cruma.Mobile                                            → Cruma.Ui, Cruma.Api.Client

Cruma.Server                                            → shared logic, Cruma.Web (static assets hosting only)
```

Any reference not listed is forbidden (STR-001, DEP-001..DEP-004).

---

# 4. Test projects

| Project | Tests | Kind |
|---|---|---|
| `Cruma.Content.Tests` | schema, migrations, export round-trip | unit |
| `Cruma.Versioning.Tests` | merge scenarios, metadata merge, comparison | unit |
| `Cruma.Search.Tests` | normalizer, tokenizer, query | unit |
| `Cruma.Sync.Tests` | protocol contracts serialization, client version compatibility | unit |
| `Cruma.Notes.Tests`, `Cruma.Kanban.Tests` | domain rules | unit |
| `Cruma.Search.Conformance.Tests` | shared suite over SQLite and PostgreSQL adapters (TST-002) | integration |
| `Cruma.Server.Tests` | endpoints, modules, user isolation, sync over real PostgreSQL (TST-003, TST-004) | integration |
| `Cruma.Desktop.Storage.Tests` | local storage, pending change log | integration (SQLite file) |
| `Cruma.Sync.Client.Tests` | sync engine against a test server | integration |
| `Cruma.Ui.Tests` | components with logic (bUnit) | unit |
| `Cruma.Architecture.Tests` | allowed references (§3), projects listed in this template, shared logic packages, NLog only in composition roots (plan N-5; DEP-001..DEP-004, STR-001, LOG-003) | architecture |

---

# 5. Projects per increment

| Increment | New projects |
|---|---|
| I-1 | all of §2.1 except `Cruma.Ai`; `Cruma.Ui`, `Cruma.Ui.Editor`, `Cruma.Api.Client`; all of §2.3; `Cruma.Web`; `Cruma.Server`; tests for these |
| I-2 | none (new server folders Reminders, Export) |
| I-3 | `Cruma.Ai` (server folder Ai) |
| I-4 | none (new server folder Kanban; `Cruma.Kanban` may be created empty in I-1 to fix references) |
| I-5 | `Cruma.Mobile` |

---

# End of Document
