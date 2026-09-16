# Cruma — Server Pattern

> **Type:** Pattern
> **Scope:** server
> **Enforces:** API-001..API-005, PER-001..PER-004, DEP-007, STR-003, SEC-001, SEC-004..SEC-007, AI-001..AI-003, AUD-001..AUD-004, ERR-001
> **Derived from:** D-1, D-6, D-7, D-8, C-13..C-17; SPEC FR-29, FR-31..FR-37

## Purpose

Describes how `Cruma.Server` is structured: host, modules, endpoints, application services, persistence,
identity, and the server-side parts of AI, reminders and audit.

---

# 1. Shape

One ASP.NET Core application on .NET 10, organized as modules in folders (STR-003). It serves:

- the REST API for thin clients (`/api/v1/...`),
- the sync endpoint for the desktop (`/api/sync/v1/...`),
- authentication endpoints,
- the static assets of `Cruma.Web` (same origin, which enables cookie sessions — SEC-004).

```text
Request ─► reverse proxy (TLS) ─► Cruma.Server
            ├─ authentication (cookie for web, bearer for desktop)
            ├─ endpoint (bind, delegate, map)                          API-002
            ├─ application service (use case, transaction, audit)
            ├─ module persistence (EF Core, global user filter)         PER-002
            └─ ProblemDetails on failure                                ERR-001
```

---

# 2. Module anatomy

```text
Notes/
├── NotesModule.cs            AddNotesModule(services) + MapNotesEndpoints(app)
├── INotesService.cs          public surface for other modules (DEP-007)
├── Endpoints/NotesEndpoints.cs
├── Application/NotesService.cs
└── Persistence/NoteEntity.cs, NoteConfiguration.cs
```

- Endpoints use Minimal API route groups per module, versioned by path (API-001).
- Application services implement use cases: validate input, load, apply domain logic from shared libraries
  (`Cruma.Notes`, `Cruma.Versioning`, …), persist, emit audit events, return a result.
- Other modules call only `I<Module>Service` (DEP-007).
- Domain rules live in the shared libraries, not in application services. An application service that contains
  an `if` about business meaning is a smell — move it into the domain library.

---

# 3. Persistence

- One `CrumaDbContext` in `Infrastructure/`; each module contributes its entity configurations through an
  `IEntityTypeConfiguration` set and uses its own PostgreSQL schema (NAM-003).
- Every user-owned entity implements an owner contract; the context applies a global query filter on the
  current user (PER-002). Background jobs that legitimately work across users (blob garbage collection, reminder
  scheduling) use an explicit, named bypass and are covered by tests.
- Documents are stored as `jsonb`. Versions are rows; the current version is referenced from the note row.
- Migrations are generated into `Cruma.Server/Infrastructure/Migrations` and applied by a deployment step
  (PER-003, OPS-005).
- Search index writes go only through the search adapter (PER-004).

---

# 4. Identity (C-14)

- The server is the only issuer (SEC-001). Recommended implementation: **OpenIddict** on ASP.NET Core with
  the authorization code flow + PKCE for the desktop (and later mobile), and Google as external login.
- The web client uses a same-origin cookie session created after the external login (SEC-004); it never holds
  tokens.
- Users table holds the internal identifier; a linked identities table maps provider + subject to it. Adding a
  provider adds a row type, not a schema change for other modules (NFR-14).
- Endpoints obtain the current user through one `ICurrentUser` abstraction.

---

# 5. Sync endpoint

Implements the protocol in `versioning-and-sync-pattern.md` §5. The Sync module:

1. validates versions (SYN-001),
2. verifies referenced blobs exist for the user (SYN-005),
3. deduplicates by change identifier (SYN-003),
4. merges via `Cruma.Versioning` (VER-002),
5. persists the new version, updates the search index, emits audit events (AUD-003),
6. returns per-change results and the pull set.

Steps 3–5 for one change run in one database transaction.

---

# 6. Blobs (C-7b)

- `IBlobStore` from `Cruma.Blobs`; first implementation stores files on a mounted volume under
  `<root>/<userId>/<first-2-hex>/<hash>` (BLB-002).
- Streaming upload and download; the server verifies the hash of uploaded content.
- A hosted service performs garbage collection (BLB-004).

---

# 7. AI gateway (C-15b, increment I-3)

- Holds provider API keys encrypted with a key supplied as a secret (SEC-007).
- Accepts only explicit AI requests that state provider, operation, and scope identifiers (AI-002); loads the
  scoped content itself — the client does not upload note content for AI.
- Emits one audit event per call without content (AUD-003).
- Returns proposals; applying a proposal is a normal update that creates a version with source `ai` (AI-003).

# 8. Reminders (C-16, increment I-2)

- A hosted service scans due reminders and sends Web Push (VAPID) to subscriptions of thin clients and mobile
  (D-7). It never targets desktop devices.
- Delivery state per reminder and device prevents duplicates.

# 9. Audit (C-17)

- `IAuditWriter` in `Audit/`, append-only table in schema `audit` (AUD-001, AUD-004).
- Called from application services, never from endpoints or `ILogger`.

---

# 10. Anti-patterns

- Controllers or endpoints with business logic or `DbContext` access.
- A module querying another module's tables "because it's the same database".
- Applying migrations in `Program.cs` on startup in production.
- Returning entities from endpoints.
- Accepting note content from the client for an AI call instead of loading it by scope on the server.

---

# End of Document
