# Cruma — Coding Conventions

> **Type:** Convention
> **Scope:** shared
> **Registered rules:** NAM-001..NAM-003, LOG-001, STR-004
> **Note:** conventions keep code predictable; violating one does not break architecture, but consistently
> ignoring them degrades maintainability.

## Purpose

Naming and style for Cruma code in C#, Razor, TypeScript, and the database.

---

# 1. Names derived from "Cruma"

| Thing | Convention | Example |
|---|---|---|
| Solution | `Cruma.slnx` | — |
| Project | `Cruma.<Area>[.<Sub>]` (NAM-001) | `Cruma.Versioning`, `Cruma.Desktop.Storage` |
| Root namespace | project name (NAM-001) | `Cruma.Server.Notes.Application` |
| Server module registration | `Add<Module>Module`, `Map<Module>Endpoints` | `AddNotesModule` |
| DbContext | `CrumaDbContext` (server), `CrumaLocalDbContext` (desktop) | — |
| Database (server) | `cruma` | — |
| Database schemas | module name, snake_case (NAM-003) | `notes`, `audit`, `sync` |
| Local database file | `cruma-<userId>.db` in `%LOCALAPPDATA%\Cruma\` | — |
| App data folder | `%LOCALAPPDATA%\Cruma\` | logs, blobs, tokens |
| Editor JS package | `@cruma/editor` | — |
| Container images | `cruma-server` | `cruma-server:1.4.0` |
| Compose project | `cruma` | — |
| Configuration section root | `Cruma` | `Cruma:Sync:MinimumClientVersion` |
| Environment variables | `CRUMA__<SECTION>__<KEY>` | `CRUMA__BLOBS__ROOT` |
| Error codes | snake_case | `client_version_unsupported` |
| Audit operation types | `<area>.<verb_past>` | `note.trashed` |
| CSS custom properties | `--cruma-<token>` | `--cruma-surface` |

"Keep" / "GoogleKeep" never appear in names (NAM-002).

---

# 2. C#

- File-scoped namespaces; one public type per file; file name equals type name.
- Nullable reference types enabled; no `!` suppression without a comment explaining why the value cannot be null.
- `async` all the way; methods returning `Task` end with `Async`; accept `CancellationToken` on I/O methods.
- Prefer records for immutable data (contracts, domain values, merge inputs and results).
- Domain libraries expose behavior through methods on types or small static functions; no service locators.
- Interfaces are prefixed with `I`; implementations are not suffixed with `Impl`.
- `TimeProvider` for time; no `DateTime.Now` / `DateTime.UtcNow` in logic (TST-005).
- Identifiers of entities are `Guid` values generated as version 7 GUIDs (sortable, client-generatable for
  SYN-003 and SYN-006).
- Structured logging templates, no interpolation (LOG-001).

# 3. Razor

- Page components in `Pages/`, reusable components in `Components/`, grouped by feature area (`Notes`, `Search`,
  `History`, …).
- Component parameters are immutable inputs; state changes flow through callbacks or data services.
- CSS isolation files (`.razor.css`) for component styles; colors only through `--cruma-*` tokens (UI-006).

# 4. TypeScript (Cruma.Ui.Editor)

- TypeScript sources under `Cruma.Ui.Editor/editor/`; build output to the static web assets folder.
- One exported interop module; exported function names describe commands (`loadDocument`, `insertImage`).
- Cruma extensions named `Cruma<Feature>Extension` (`CrumaBlockIdExtension`).

# 5. Database

- snake_case identifiers (NAM-003) through an EF Core naming convention, not hand-written column attributes.
- Primary keys `id`; foreign keys `<entity>_id`; timestamps `*_at_utc`.

# 6. Tests

- Test project `Cruma.<Project>.Tests`; test class `<TypeUnderTest>Tests`.
- Test method names: `<Method>_<Condition>_<ExpectedResult>`.

---

# End of Document
