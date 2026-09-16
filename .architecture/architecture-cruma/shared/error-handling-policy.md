# Cruma — Error Handling Policy

> **Type:** Policy
> **Scope:** shared
> **Derived rules:** ERR-001..ERR-003, SYN-001, SYN-004
> **Derived from:** SPEC NFR-4, FR-27, FR-37

## Purpose

Governing principles for how failures are represented, propagated, and shown, with the one overriding concern of
this system: a failure must never cost the user a change.

---

# 1. Principles

## P-1 — Losing data is worse than failing loudly

When in doubt, keep both states and report a problem. No error path on the desktop may delete, overwrite, or
reset local changes (ERR-002). A sync that cannot complete leaves everything pending.

## P-2 — Expected failures are results, not surprises

Validation errors, not-found, version conflicts, unsupported client version, and authorization denials are
expected outcomes. They are modeled explicitly and returned as ProblemDetails with a stable error code
(ERR-001). Exceptions are for the unexpected.

## P-3 — One place turns the unexpected into a response

A single global exception handler on the server logs unexpected exceptions with a correlation identifier and
returns a generic ProblemDetails. No endpoint has its own catch-all.

## P-4 — Never swallow

A catch block handles, rethrows, or converts — and logs when the failure is not propagated (ERR-003).

## P-5 — Users see meaning, logs hold detail

The UI shows what happened and what the user can do ("Sync paused — sign in again"); technical detail goes to
logs with the correlation identifier, never content (LOG-002).

---

# 2. Error codes

Stable, lowercase, snake_case codes in the ProblemDetails `code` extension. Initial set:

| Code | HTTP | Meaning |
|---|---|---|
| `validation_failed` | 400 | Input invalid; `errors` extension lists fields |
| `not_found` | 404 | Entity does not exist for this user (also used for entities of other users) |
| `version_conflict` | 409 | Optimistic concurrency failure on a non-merged update |
| `client_version_unsupported` | 426 | Client below minimum version; `minimumVersion` extension (SYN-001) |
| `blob_missing` | 422 | Change references a blob not uploaded (SYN-005) |
| `unauthenticated` | 401 | No valid session or token |
| `forbidden` | 403 | Authenticated but not allowed |
| `internal_error` | 500 | Unexpected; no details |

Accessing another user's entity returns `not_found`, not `forbidden`, so existence is not disclosed.

---

# 3. Client handling

| Situation | Desktop | Thin client |
|---|---|---|
| Network unavailable | continue offline, status `pending` | queue new notes; disable other actions |
| `unauthenticated` | keep working locally, prompt sign-in for sync | redirect to sign-in; queue survives |
| `client_version_unsupported` | stop sync, keep changes, prompt update | reload application |
| `validation_failed` on a pending change | keep change, status `error`, show which note | show inline error |
| `internal_error` | retry with backoff, status `error` after repeated failure | show message with correlation id |

---

# End of Document
