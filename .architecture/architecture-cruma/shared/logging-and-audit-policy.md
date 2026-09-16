# Cruma — Logging & Audit Policy

> **Type:** Policy
> **Scope:** shared
> **Derived rules:** LOG-001, LOG-002, AUD-001..AUD-004
> **Derived from:** SPEC FR-35, NFR-10; D-8.1; C-17

## Purpose

Separates two things that are often mixed: **technical logs** help the developer diagnose the system; **audit
records** state who did what to which data. They have different readers, retention, and integrity requirements
(NFR-10).

---

# 1. Principles

## P-1 — Logs and audit are different channels

Audit events go only through the Audit module into the audit store (AUD-001). Logs go through
`Microsoft.Extensions.Logging` (LOG-001). An audit event is never "just a log line with a special category".

## P-2 — Identifiers, never content

Both channels may contain identifiers (user, note, card, blob, change, correlation). Neither contains note or
card content, titles, search queries, AI prompts or responses, tokens, or secrets (LOG-002, AUD-002).

## P-3 — Audit is complete and immutable

Every operation in the audit catalog produces exactly one event; audit records are never updated or deleted
(AUD-003, AUD-004).

---

# 2. Audit catalog

| Operation type | Emitted when | Object |
|---|---|---|
| `auth.sign_in`, `auth.sign_out`, `auth.sign_in_failed` | authentication flow | user |
| `identity.provider_linked`, `identity.provider_unlinked` | identity change | user |
| `note.created`, `note.updated`, `note.archived`, `note.trashed`, `note.restored`, `note.deleted_permanently` | note lifecycle | note |
| `note.version_restored`, `note.conflict_resolved` | version operations | note |
| `sync.session`, `sync.change_rejected` | per sync session; per rejected change | device |
| `blob.uploaded`, `blob.deleted` | blob lifecycle | blob |
| `export.started`, `export.downloaded` | data export | user |
| `ai.cloud_call` | every cloud AI call; attributes: provider, operation, scope identifiers | user |
| `security.rate_limited`, `security.authorization_denied` | security events | user or request |
| `admin.*` | administrative interventions | varies |
| `kanban.*` (I-4) | project, board, card lifecycle | project / board / card |

Offline desktop operations are audited when the server accepts them during sync, with the event time set to the
acceptance time and the client-reported time as an attribute (AUD-003).

Read access to data is audited at the level of export and AI scope loading, not per note view — per-view
auditing of a personal notes app would produce noise without value.

---

# 3. Audit event shape

```text
id, occurredAtUtc, userId, operationType, objectType, objectId, clientType, result (success|failure),
correlationId, attributes (small, content-free key/value map)
```

---

# 4. Technical logging

**Provider:** NLog behind `Microsoft.Extensions.Logging` (LOG-003). Code uses `ILogger<T>` only; NLog appears
solely in the logging configuration of each composition root (`Cruma.Desktop`, `Cruma.Server`, `Cruma.Web`) and
in its `nlog.config`.

**Default level: Information** in every environment (LOG-004). Debug and Trace are switched on temporarily
through configuration or an environment variable and switched back off; the committed configuration never
defaults to them.

| Level | Use for |
|---|---|
| Error | unexpected exceptions, failed background jobs |
| Warning | rejected sync changes, retries exhausted, rate limiting |
| Information | application start/stop, migrations applied, sync session summaries, background job summaries |
| Debug | detailed flow for temporary diagnosis, off by default |
| Trace | protocol-level detail for temporary diagnosis, off by default |

**Targets:**

| Host | Target |
|---|---|
| Desktop | rolling files in `%LOCALAPPDATA%\Cruma\logs\`, size-capped with a small number of archives, so logs cannot fill the disk |
| Server | console (collected by the container runtime); optionally a rolling file on the volume |
| Web (WebAssembly) | browser console only; no file target |

- Structured templates only (LOG-001): `logger.LogWarning("Sync change {ChangeId} rejected with {Code}", …)`.
- Every server request has a correlation identifier propagated to logs, ProblemDetails, and audit events. The
  desktop logs the same identifier for the sync session it received it in.
- Desktop log files follow the same content rule as server logs (LOG-002) — they sit unencrypted in the user's
  profile.

---

# End of Document
