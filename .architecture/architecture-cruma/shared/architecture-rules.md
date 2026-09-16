```doc-format
BlockType: rule
RequiredFields: RuleID, Category, Severity, Scope, Rule, Rationale
Required: yes
UniqueKey: RuleID
```

# Cruma — Architecture & Development Rules

> **Type:** Rules (mandatory baseline)
> **Applies to:** all Cruma code, all scopes
> **Derived from:** `docs/arch/plan.md`, `docs/arch/DECISIONS.md`, and the Pattern / Policy / Strategy documents
> of this profile. Every mandatory statement in those documents is registered here.

Severity: **High** = violation must be fixed or explicitly approved as an exception before work continues.
**Medium** = fix in the same batch unless the operator defers it. **Low** = fix when touching the code.

Plan references in `Rationale` (C-n, D-n, FR-n, NFR-n) point to `docs/arch/plan.md`, `docs/arch/DECISIONS.md`
and `docs/arch/spec.md`.

## Table of Contents
1. Dependency Rules (DEP)
2. Structure Rules (STR)
3. Naming Rules (NAM)
4. Content Document Rules (CNT)
5. Versioning & Merge Rules (VER)
6. Synchronization Rules (SYN)
7. Search Rules (SRC)
8. Blob Rules (BLB)
9. API Rules (API)
10. Persistence Rules (PER)
11. UI Rules (UI)
12. Security Rules (SEC)
13. AI Rules (AI)
14. Error Handling Rules (ERR)
15. Logging Rules (LOG)
16. Audit Rules (AUD)
17. Testing Rules (TST)
18. Operations Rules (OPS)

---

# 1. Dependency Rules

Source: `plan.md` §3, `solution-structure-template.md`

```rule
RuleID: DEP-001
Category: Dependency
Severity: High
Scope: Project
Rule: Shared logic projects (Cruma.Notes, Cruma.Kanban, Cruma.Content, Cruma.Versioning, Cruma.Search, Cruma.Sync, Cruma.Blobs, Cruma.Ai, Cruma.Api.Contracts) must not reference any UI, shell, server, storage, or test project.
Rationale: Shared logic runs on desktop, server and in WebAssembly; plan.md §3 rule 1.
```

```rule
RuleID: DEP-002
Category: Dependency
Severity: High
Scope: Project
Rule: Shared logic projects may reference only the .NET base class library and Microsoft.Extensions.*.Abstractions packages, plus packages explicitly listed in solution-structure-template.md. They must not reference ASP.NET Core, EF Core, WPF, MAUI, or any JS interop package.
Rationale: Keeps shared logic runnable in every host, including browser WebAssembly.
```

```rule
RuleID: DEP-003
Category: Dependency
Severity: High
Scope: Project
Rule: Cruma.Ui and Cruma.Ui.Editor must not reference any shell project (Cruma.Desktop, Cruma.Web, Cruma.Mobile), any storage project, or any server project.
Rationale: UI is shared by all shells; plan.md §3 rule 2.
```

```rule
RuleID: DEP-004
Category: Dependency
Severity: High
Scope: Project
Rule: Shell projects (Cruma.Desktop, Cruma.Web, Cruma.Mobile) implement the platform service interfaces declared by Cruma.Ui and must not be referenced by any other production project. The only exception is Cruma.Server referencing Cruma.Web solely to host its WebAssembly static assets; server code must not use any type from Cruma.Web.
Rationale: Shells are composition roots; plan.md §3 rule 3.
```

```rule
RuleID: DEP-005
Category: Dependency
Severity: High
Scope: Project
Rule: Cruma.Notes and Cruma.Kanban must not reference each other. A card refers to a note only by note identifier.
Rationale: Separate domains; NFR-16, FR-23.
```

```rule
RuleID: DEP-006
Category: Dependency
Severity: High
Scope: CrossLayer
Rule: The desktop exchanges data changes with the server only through the Cruma.Sync protocol. Thin clients (Cruma.Web, Cruma.Mobile) communicate with the server only through the versioned REST API via Cruma.Api.Client.
Rationale: plan.md §3 rule 4.
```

```rule
RuleID: DEP-007
Category: Dependency
Severity: High
Scope: CrossModule
Rule: A server module must not access another module's persistence entities, DbContext configuration, or internal classes; it calls only the other module's public service interface.
Rationale: Server modules are folders in one project; boundaries are enforced by rule and review, not by project references.
```

---

# 2. Structure Rules

Source: `solution-structure-template.md`

```rule
RuleID: STR-001
Category: Structure
Severity: High
Scope: Project
Rule: The solution contains only the projects listed in solution-structure-template.md. Adding, removing, or renaming a project requires updating that template in the same change.
Rationale: The template is the authoritative map from plan components to projects.
```

```rule
RuleID: STR-002
Category: Structure
Severity: Medium
Scope: Project
Rule: Repository layout is `src/` (production projects), `tests/` (test projects), `deploy/` (container and backup definitions), `docs/` (architecture artifacts), `.architecture/` (this profile). One solution file `Cruma.slnx` at the repository root.
Rationale: Predictable locations for humans and agents.
```

```rule
RuleID: STR-003
Category: Structure
Severity: High
Scope: Project
Rule: Server modules live as top-level folders of Cruma.Server (Notes, Kanban, Sync, Search, Blobs, Identity, Audit, Ai, Reminders, Export). Each module exposes its public surface through interfaces in its own folder and registers its services through one module registration method.
Rationale: C-13..C-17 as server modules without a project explosion for a single developer.
```

```rule
RuleID: STR-004
Category: Structure
Severity: Medium
Scope: Project
Rule: Package versions are managed centrally in Directory.Packages.props. Directory.Build.props enables nullable reference types, implicit usings, and treats warnings as errors for all production projects.
Rationale: One place for versions and compiler strictness.
```

---

# 3. Naming Rules

Source: `coding-conventions.md`

```rule
RuleID: NAM-001
Category: Naming
Severity: Medium
Scope: Code
Rule: Every project name starts with `Cruma.`; the root namespace of a project equals its project name; sub-namespaces follow folder names.
Rationale: The application is named Cruma.
```

```rule
RuleID: NAM-002
Category: Naming
Severity: Medium
Scope: Code
Rule: Code, packages, database objects, container names and configuration keys must not use the names "Keep" or "GoogleKeep"; the product name is Cruma. Google appears only in the Google identity provider integration.
Rationale: The project folder name is historical; the product is not a Google Keep client.
```

```rule
RuleID: NAM-003
Category: Naming
Severity: Low
Scope: Data
Rule: PostgreSQL schemas, tables and columns use snake_case. Each server module owns one schema named after the module in snake_case.
Rationale: PostgreSQL convention; ownership visible in the database.
```

---

# 4. Content Document Rules

Source: `content-document-pattern.md`

```rule
RuleID: CNT-001
Category: Content
Severity: High
Scope: CrossLayer
Rule: The note document schema is defined only in Cruma.Content. No other project defines, extends, or reinterprets document node types.
Rationale: C-3 is the single owner of the schema; D-2.
```

```rule
RuleID: CNT-002
Category: Content
Severity: High
Scope: Code
Rule: Every block node has a stable block identifier assigned when the block is created. The identifier never changes during the block's life and is never reused. A block created by copy or paste receives a new identifier.
Rationale: Block-level merge and version comparison depend on identity; D-2, D-3.3.
```

```rule
RuleID: CNT-003
Category: Content
Severity: High
Scope: Code
Rule: Every stored document carries its schema version. A document with an older schema version is migrated by Cruma.Content migrations when read. Migrations are forward-only, deterministic, and covered by tests.
Rationale: Schema evolves across increments; R-3.
```

```rule
RuleID: CNT-004
Category: Content
Severity: High
Scope: Code
Rule: A document references binary content only by blob identifier. Binary or base64 data must never be embedded in a document.
Rationale: D-5.1; versions must not copy images.
```

```rule
RuleID: CNT-005
Category: Content
Severity: High
Scope: Code
Rule: Block and mark types are registered in the Cruma.Content type registry. A node of an unknown type must be preserved unchanged on load and save; it must never be dropped or rewritten.
Rationale: An older client must not destroy content created by a newer one; NFR-13, NFR-4.
```

```rule
RuleID: CNT-006
Category: Content
Severity: High
Scope: Code
Rule: The editor exchanges documents with C# only as schema JSON defined by Cruma.Content. C# code must not depend on the editor's internal state or model. Block identifiers are assigned by a Cruma-owned editor extension, not by a third-party paid extension.
Rationale: Narrow editor boundary; C-10, R-8.
```

```rule
RuleID: CNT-007
Category: Content
Severity: Medium
Scope: Code
Rule: Plain-text extraction of a document (for search and AI context) is implemented only in Cruma.Content.
Rationale: One extraction feeds both search adapters identically; D-4.
```

```rule
RuleID: CNT-008
Category: Content
Severity: High
Scope: Code
Rule: The lossless export format must round-trip: importing an exported document yields a document equal to the original, including block identifiers and unknown node types.
Rationale: FR-12 acceptance 2.
```

```rule
RuleID: CNT-009
Category: Content
Severity: Medium
Scope: Code
Rule: Markdown export produces standard CommonMark with GitHub-style tables and task lists only. Unsupported marks are dropped or simplified; no custom Markdown dialect is introduced.
Rationale: FR-12 acceptance 3, K-3.
```

---

# 5. Versioning & Merge Rules

Source: `versioning-and-sync-pattern.md`

```rule
RuleID: VER-001
Category: Versioning
Severity: High
Scope: Code
Rule: Merge, conflict detection and version comparison are implemented only in Cruma.Versioning as pure functions without I/O, clock access, or randomness.
Rationale: Deterministic, fully unit-testable merge; R-2.
```

```rule
RuleID: VER-002
Category: Versioning
Severity: High
Scope: CrossLayer
Rule: The authoritative merge runs only on the server. Clients never persist a merged result as an authoritative version.
Rationale: Server is the authoritative source; frame 2.1, C-4.
```

```rule
RuleID: VER-003
Category: Versioning
Severity: High
Scope: Code
Rule: Document merge is three-way (base, server, incoming) over blocks matched by block identifier. When both sides changed the same block differently, the result is a conflict that preserves both variants; the merge must never select one variant silently.
Rationale: D-3.3, FR-28 acceptance 2, NFR-4.
```

```rule
RuleID: VER-004
Category: Versioning
Severity: High
Scope: Code
Rule: When one side deletes a note or block and the other side modifies it, the modification is preserved and the result is marked for the user.
Rationale: FR-28 acceptance 4.
```

```rule
RuleID: VER-005
Category: Versioning
Severity: High
Scope: Code
Rule: Set-like metadata (tags, checklist items) merges by applying additions and removals from both sides. For a scalar field changed on both sides, the change received later by the server wins; the overwritten value remains in version history and the sync status reports the overwrite.
Rationale: D-3.4, operator-approved interpretation of NFR-4.
```

```rule
RuleID: VER-006
Category: Versioning
Severity: High
Scope: Code
Rule: A new version is created per edit session (after inactivity, on closing the note, or at sync), never per autosave.
Rationale: D-3.1, R-7.
```

```rule
RuleID: VER-007
Category: Versioning
Severity: High
Scope: Code
Rule: Restoring an older version creates a new version. Version history is never rewritten or reordered.
Rationale: FR-6 acceptance 4.
```

```rule
RuleID: VER-008
Category: Versioning
Severity: High
Scope: Code
Rule: Versions are not deleted automatically. Versions of a note are deleted only together with permanent deletion of the note.
Rationale: NFR-7 (no count limit), plan.md R-7.
```

---

# 6. Synchronization Rules

Source: `versioning-and-sync-pattern.md`

```rule
RuleID: SYN-001
Category: Sync
Severity: High
Scope: CrossLayer
Rule: Every sync session starts with the client declaring its application version and sync protocol version. The server rejects a client below the minimum supported version with a dedicated error code, and the client keeps all local changes and prompts for update.
Rationale: FR-37, NFR-18.
```

```rule
RuleID: SYN-002
Category: Sync
Severity: High
Scope: CrossLayer
Rule: Every change sent by the desktop carries the identifier of the version it was based on. The server never applies an incoming change without merging against that base.
Rationale: D-3, prevents silent overwrite.
```

```rule
RuleID: SYN-003
Category: Sync
Severity: High
Scope: CrossLayer
Rule: Every client change carries a client-generated change identifier. The server processes a change identifier at most once; retries must not duplicate effects.
Rationale: Unreliable networks require idempotent replay.
```

```rule
RuleID: SYN-004
Category: Sync
Severity: High
Scope: Desktop
Rule: A local pending change and its local intermediate versions are removed only after the server acknowledges the change.
Rationale: D-3.2; no loss on interrupted sync.
```

```rule
RuleID: SYN-005
Category: Sync
Severity: High
Scope: CrossLayer
Rule: A client uploads every blob referenced by a change before sending the change. The server rejects a document change that references a blob unknown for that user.
Rationale: No dangling image references; D-5.
```

```rule
RuleID: SYN-006
Category: Sync
Severity: High
Scope: Web
Rule: The thin-client write queue holds only creations of new notes. Each queued item carries a client-generated note identifier so replay is idempotent. The queue survives closing and reopening the client.
Rationale: A-4, FR-30.
```

```rule
RuleID: SYN-007
Category: Sync
Severity: Medium
Scope: Desktop
Rule: The desktop exposes a sync status with exactly these states: synced, pending, conflict, error.
Rationale: FR-27 acceptance 3.
```

---

# 7. Search Rules

Source: `search-pattern.md`

```rule
RuleID: SRC-001
Category: Search
Severity: High
Scope: CrossLayer
Rule: Text normalization (Unicode NFC, case folding, diacritics removal) and tokenization are implemented only in Cruma.Search. Index adapters store and query only tokens produced by Cruma.Search and must not apply database-specific normalization, stemming, or stop words.
Rationale: D-4; identical result sets on desktop and server (FR-24 acceptance 4).
```

```rule
RuleID: SRC-002
Category: Search
Severity: High
Scope: CrossLayer
Rule: The normalizer has an explicit version. Any change to normalization or tokenization increments that version and triggers reindexing on desktop and server.
Rationale: Mixed normalizer versions produce divergent results.
```

```rule
RuleID: SRC-003
Category: Search
Severity: High
Scope: CrossLayer
Rule: A search query passes through the same normalizer and tokenizer as indexed text; matching is by token prefix.
Rationale: NFR-5, FR-24 acceptance 2 and 3.
```

```rule
RuleID: SRC-004
Category: Search
Severity: High
Scope: Server
Rule: Every server search query is restricted to the current user inside the index adapter, not only by the caller.
Rationale: FR-31; defense in depth for user isolation.
```

---

# 8. Blob Rules

Source: `versioning-and-sync-pattern.md`

```rule
RuleID: BLB-001
Category: Blob
Severity: High
Scope: CrossLayer
Rule: Blobs are immutable. A blob identifier is the SHA-256 hash of the blob content. Replacing an image creates a new blob.
Rationale: D-5.1.
```

```rule
RuleID: BLB-002
Category: Blob
Severity: High
Scope: Server
Rule: Blobs are stored and deduplicated per user. Access to a blob is authorized by ownership; knowing a blob identifier never grants access.
Rationale: A content hash must not become a cross-user existence oracle or capability; FR-31.
```

```rule
RuleID: BLB-003
Category: Blob
Severity: High
Scope: CrossLayer
Rule: Blob content is never stored in a database, neither on the server nor on the desktop.
Rationale: D-5.1.
```

```rule
RuleID: BLB-004
Category: Blob
Severity: Medium
Scope: Server
Rule: A blob is deleted only when no version of any note of the owning user references it and a grace period has elapsed.
Rationale: Safe garbage collection with version history; plan.md R-7.
```

---

# 9. API Rules

Source: `server-pattern.md`

```rule
RuleID: API-001
Category: API
Severity: High
Scope: Endpoint
Rule: All REST endpoints are under `/api/v{major}/`. A breaking contract change requires a new major version; the previous version keeps working while clients using it are supported.
Rationale: NFR-18.
```

```rule
RuleID: API-002
Category: API
Severity: High
Scope: Endpoint
Rule: An endpoint only binds input, relies on authorization, delegates to one application service call, and maps the result. It must not contain business logic and must not access DbContext.
Rationale: Thin transport layer.
```

```rule
RuleID: API-003
Category: API
Severity: High
Scope: Endpoint
Rule: Every endpoint requires an authenticated user, except endpoints on the explicit anonymous allowlist (health check, authentication flow endpoints, static client assets).
Rationale: NFR-8.
```

```rule
RuleID: API-004
Category: API
Severity: High
Scope: Endpoint
Rule: Request and response contracts are defined in Cruma.Api.Contracts. Persistence entities are never serialized to clients.
Rationale: Contracts shared with thin clients; no persistence leakage.
```

```rule
RuleID: API-005
Category: API
Severity: Medium
Scope: Endpoint
Rule: Endpoints returning collections are paginated.
Rationale: NFR-7 volumes.
```

---

# 10. Persistence Rules

Source: `server-pattern.md`, `desktop-pattern.md`

```rule
RuleID: PER-001
Category: Persistence
Severity: High
Scope: Data
Rule: Server persistence uses EF Core with PostgreSQL only inside Cruma.Server; desktop persistence uses EF Core with SQLite only inside Cruma.Desktop.Storage.
Rationale: D-6.2, C-8, C-13.
```

```rule
RuleID: PER-002
Category: Persistence
Severity: High
Scope: Data
Rule: Every user-owned server entity has an owner user identifier, and every query of such an entity is filtered by the current user through a global query filter. Bypassing the filter requires an explicit, reviewed exception.
Rationale: FR-31 user isolation.
```

```rule
RuleID: PER-003
Category: Persistence
Severity: High
Scope: Data
Rule: Schema changes are made only through EF Core migrations committed to the repository. The server does not apply migrations automatically at startup; migrations run as an explicit deployment step.
Rationale: Controlled, reversible production changes; OPS-005.
```

```rule
RuleID: PER-004
Category: Persistence
Severity: High
Scope: Data
Rule: Full-text index structures (SQLite FTS5 tables, PostgreSQL tsvector columns) are read and written only by the Cruma.Search index adapters.
Rationale: SRC-001.
```

```rule
RuleID: PER-005
Category: Persistence
Severity: High
Scope: Desktop
Rule: The desktop local database must not contain authentication tokens, API keys, or other secrets.
Rationale: The local database is not encrypted (NFR-9).
```

```rule
RuleID: PER-006
Category: Persistence
Severity: High
Scope: Desktop
Rule: A development (Debug) build of the desktop uses its own app data folder `%LOCALAPPDATA%\Cruma\dev\` for the local database, blobs, logs and tokens, and shows "(dev)" in the window title. It must never read or write the app data of an installed (Release) build.
Rationale: The local database holds the author's real notes; development and testing must not touch them (operator change 2026-09-16, CHANGES.md 1).
```

---

# 11. UI Rules

Source: `ui-pattern.md`

```rule
RuleID: UI-001
Category: UI
Severity: High
Scope: Component
Rule: Razor components in Cruma.Ui must not call IJSRuntime, browser APIs, Windows APIs, or MAUI APIs directly. Platform capabilities are used only through platform service interfaces declared in Cruma.Ui. The only exception is the interop layer inside Cruma.Ui.Editor.
Rationale: NFR-15; the same components run in three shells.
```

```rule
RuleID: UI-002
Category: UI
Severity: High
Scope: Component
Rule: Razor components must not use HttpClient or any storage directly; they use data service interfaces implemented per shell (API client for thin clients, local storage and sync for desktop).
Rationale: Desktop works offline from local data; thin clients work online.
```

```rule
RuleID: UI-003
Category: UI
Severity: Medium
Scope: Component
Rule: Platform differences in available features are expressed through a capability description supplied by the shell, not by duplicating components per platform.
Rationale: NFR-19 same mental model.
```

```rule
RuleID: UI-004
Category: UI
Severity: High
Scope: Web
Rule: Thin clients must not persist domain data locally. The only allowed local persistence is the write queue defined by SYN-006 and user interface preferences.
Rationale: frame 2.1, A-4.
```

```rule
RuleID: UI-005
Category: UI
Severity: High
Scope: Component
Rule: Creating a note must not require any input other than its content. Every new note is saved into the default category.
Rationale: FR-1, A-3, NFR-1.
```

```rule
RuleID: UI-006
Category: UI
Severity: Medium
Scope: Component
Rule: Colors are applied only through theme tokens (CSS custom properties) defined once for light and dark themes; components must not hard-code color values.
Rationale: FR-34.
```

---

# 12. Security Rules

Source: `security-policy.md`

```rule
RuleID: SEC-001
Category: Security
Severity: High
Scope: Server
Rule: The server is the only token and session issuer. External identity providers (Google first) are used only for external login inside the Identity module; all other code uses the internal user identifier.
Rationale: D-6.4, NFR-14.
```

```rule
RuleID: SEC-002
Category: Security
Severity: High
Scope: Desktop
Rule: Desktop sign-in uses the authorization code flow with PKCE through the system browser and a loopback redirect. Sign-in inside an embedded WebView is forbidden.
Rationale: Identity providers block embedded-browser sign-in; R-10.
```

```rule
RuleID: SEC-003
Category: Security
Severity: High
Scope: Desktop
Rule: The desktop stores its refresh token protected for the current Windows user (DPAPI). Starting the application and working with local data must not require a valid token.
Rationale: FR-26 acceptance 2, PER-005.
```

```rule
RuleID: SEC-004
Category: Security
Severity: High
Scope: Web
Rule: The web client authenticates with an HttpOnly, Secure, SameSite cookie session issued by the same-origin server. Access or refresh tokens must not be stored in browser storage.
Rationale: Tokens in browser storage are exposed to script injection.
```

```rule
RuleID: SEC-005
Category: Security
Severity: High
Scope: Server
Rule: Authorization is enforced on the server for every request; client-side checks are never the only protection.
Rationale: NFR-8.
```

```rule
RuleID: SEC-006
Category: Security
Severity: High
Scope: CrossCutting
Rule: Secrets (database credentials, signing keys, encryption keys, provider client secrets) are supplied through environment variables or mounted secret files. Secrets must never be committed to the repository or placed in committed configuration files.
Rationale: D-6.3, NFR-8.
```

```rule
RuleID: SEC-007
Category: Security
Severity: High
Scope: Server
Rule: User AI provider API keys are encrypted at application level with a key supplied as a secret, are never returned to any client after being saved, and are never logged.
Rationale: D-6.3, D-8.1.
```

```rule
RuleID: SEC-008
Category: Security
Severity: High
Scope: CrossCutting
Rule: External traffic uses TLS only. Unencrypted HTTP is allowed only inside the container network behind the reverse proxy.
Rationale: NFR-8, NFR-9.
```

---

# 13. AI Rules

Source: `security-policy.md`, `server-pattern.md`

```rule
RuleID: AI-001
Category: AI
Severity: High
Scope: CrossLayer
Rule: Calls to cloud AI providers are made only by the server AI module. The desktop calls only local providers directly. Thin clients never call any AI provider directly.
Rationale: D-8.1.
```

```rule
RuleID: AI-002
Category: AI
Severity: High
Scope: CrossLayer
Rule: Content is sent to a cloud AI provider only as a result of an explicit user action, after the user has been shown the provider and the scope (selection, note, or list of notes). Automatic or background AI operations may use only a local provider.
Rationale: D-8.3, NFR-12.
```

```rule
RuleID: AI-003
Category: AI
Severity: High
Scope: Component
Rule: An AI result that would change content or metadata is presented as a proposal and applied only after user confirmation. Applying it creates a new version with source AI.
Rationale: FR-13, FR-14, FR-22.
```

```rule
RuleID: AI-004
Category: AI
Severity: Medium
Scope: Code
Rule: Provider-specific code exists only in provider adapters in Cruma.Ai; AI operations depend only on the provider-agnostic interface.
Rationale: NFR-11.
```

```rule
RuleID: AI-005
Category: AI
Severity: Medium
Scope: Code
Rule: Context for AI across notes is assembled from Cruma.Search results, and the identifiers of the source notes are returned with the answer.
Rationale: D-8.2, FR-15 acceptance 1.
```

---

# 14. Error Handling Rules

Source: `error-handling-policy.md`

```rule
RuleID: ERR-001
Category: ErrorHandling
Severity: High
Scope: Server
Rule: Expected failures (validation, not found, conflict, version not supported) are returned as ProblemDetails carrying a stable machine-readable error code. Unexpected exceptions are handled by one global handler that logs them and returns a generic ProblemDetails without internal details.
Rationale: Predictable client behavior; no information leakage.
```

```rule
RuleID: ERR-002
Category: ErrorHandling
Severity: High
Scope: Desktop
Rule: No sync, network, or server error may discard or overwrite local changes.
Rationale: NFR-4.
```

```rule
RuleID: ERR-003
Category: ErrorHandling
Severity: High
Scope: Code
Rule: Exceptions must not be swallowed. A catch block either handles the failure meaningfully, rethrows, or converts it into a defined error result, and logs it when it is not propagated.
Rationale: Silent failures break sync diagnostics.
```

---

# 15. Logging Rules

Source: `logging-and-audit-policy.md`

```rule
RuleID: LOG-001
Category: Logging
Severity: Medium
Scope: CrossCutting
Rule: Technical logging uses Microsoft.Extensions.Logging with structured message templates; string interpolation in log messages is not used.
Rationale: Searchable structured logs.
```

```rule
RuleID: LOG-002
Category: Logging
Severity: High
Scope: CrossCutting
Rule: Note and card content, titles, search queries, AI prompts and AI responses, tokens and secrets must never be written to technical logs. Identifiers may be logged.
Rationale: Personal knowledge base content is sensitive; NFR-8.
```

```rule
RuleID: LOG-003
Category: Logging
Severity: Medium
Scope: CrossCutting
Rule: NLog is the logging provider behind Microsoft.Extensions.Logging. Code depends only on ILogger; NLog types must not appear outside the logging configuration of a composition root.
Rationale: Operator decision 2026-09-16; keeps logging provider replaceable and shared libraries platform-free (DEP-002).
```

```rule
RuleID: LOG-004
Category: Logging
Severity: Medium
Scope: CrossCutting
Rule: The default minimum log level is Information in every environment. Debug and Trace are enabled only temporarily through configuration, never by a committed default.
Rationale: Operator decision 2026-09-16; Debug logging on a personal knowledge base increases the chance of content leaking into logs (LOG-002).
```

---

# 16. Audit Rules

Source: `logging-and-audit-policy.md`

```rule
RuleID: AUD-001
Category: Audit
Severity: High
Scope: Server
Rule: Audit events are written only by the server Audit module into the audit schema. Audit events are never written through ILogger.
Rationale: FR-35 acceptance 3, NFR-10.
```

```rule
RuleID: AUD-002
Category: Audit
Severity: High
Scope: Server
Rule: An audit event contains UTC timestamp, user identifier, operation type, object type and identifier, client type, and result. It never contains content.
Rationale: FR-35 acceptance 1, LOG-002.
```

```rule
RuleID: AUD-003
Category: Audit
Severity: High
Scope: Server
Rule: Operations listed in FR-35 emit audit events, including every cloud AI call (provider, operation, scope identifiers). Offline desktop operations are audited when the server accepts them during sync.
Rationale: FR-35, D-8.
```

```rule
RuleID: AUD-004
Category: Audit
Severity: High
Scope: Server
Rule: Audit records are append-only; no code path updates or deletes them.
Rationale: Audit integrity.
```

---

# 17. Testing Rules

Source: `testing-strategy.md`

```rule
RuleID: TST-001
Category: Testing
Severity: High
Scope: UnitTest
Rule: Cruma.Versioning merge rules, Cruma.Search normalization and tokenization, and Cruma.Content migrations and lossless export round-trip are covered by unit tests. Merge rules are covered by scenario tests including FR-28 acceptance 1 to 4.
Rationale: Highest-risk logic; R-2, R-3.
```

```rule
RuleID: TST-002
Category: Testing
Severity: High
Scope: IntegrationTest
Rule: Both search index adapters (SQLite and PostgreSQL) run one shared conformance test suite that asserts identical result sets for the same data and queries.
Rationale: FR-24 acceptance 4.
```

```rule
RuleID: TST-003
Category: Testing
Severity: High
Scope: IntegrationTest
Rule: Server data-access integration tests run against real PostgreSQL started through Testcontainers. The EF Core in-memory provider is not used for server persistence tests.
Rationale: Query filters, JSON columns and full-text behavior differ in the in-memory provider.
```

```rule
RuleID: TST-004
Category: Testing
Severity: High
Scope: IntegrationTest
Rule: Every new user-owned entity or endpoint is covered by a test proving that another user cannot read, modify, or find it.
Rationale: FR-31.
```

```rule
RuleID: TST-005
Category: Testing
Severity: Medium
Scope: UnitTest
Rule: Tests do not depend on wall-clock time, random values, or execution order; time and identifiers are injected.
Rationale: Deterministic tests; VER-001.
```

---

# 18. Operations Rules

Source: `deployment-pattern.md`

```rule
RuleID: OPS-001
Category: Operations
Severity: High
Scope: Infra
Rule: Deployment is defined in `deploy/compose.yaml` and must run unchanged with both Podman and Docker; features specific to one container tool are not used.
Rationale: D-6.1; local Podman, portable hosting.
```

```rule
RuleID: OPS-002
Category: Operations
Severity: High
Scope: Infra
Rule: PostgreSQL data and blobs are backed up automatically at least daily to storage outside the server, keeping multiple backup versions.
Rationale: FR-36, D-6.1 condition.
```

```rule
RuleID: OPS-003
Category: Operations
Severity: High
Scope: Infra
Rule: The restore procedure is documented in `deploy/` and has been executed successfully before real user data is stored, and again after any change to backups.
Rationale: FR-36 acceptance 3; an untested backup is not a backup.
```

```rule
RuleID: OPS-004
Category: Operations
Severity: High
Scope: Infra
Rule: Production container images are referenced by explicit version tags; floating tags such as `latest` are not used.
Rationale: Reproducible deployments.
```

```rule
RuleID: OPS-005
Category: Operations
Severity: High
Scope: Infra
Rule: A deployment applies database migrations as a separate step before the new server version starts, and a backup is taken before migrations run.
Rationale: PER-003; recoverable failed migrations.
```

---

# End of Document
