# Cruma — Security Policy

> **Type:** Policy
> **Scope:** shared
> **Derived rules:** SEC-001..SEC-008, AI-001..AI-003, BLB-002, PER-002, PER-005, SRC-004, LOG-002, TST-004
> **Derived from:** D-6.3, D-6.4, D-8, frame §2.1; SPEC FR-31, FR-32, NFR-8, NFR-9, NFR-12, NFR-14

## Purpose

States the governing principles for protecting a personal knowledge base that holds technical notes, and
explains the rules derived from them. When a situation is not covered by a rule, decide by these principles.

---

# 1. Principles

## P-1 — The server trusts nothing it did not verify

Every request is authenticated and authorized on the server; user isolation is enforced at the data layer, not
only at the endpoint (SEC-005, PER-002, SRC-004, BLB-002). A client — including our own desktop — is an untrusted
party.

## P-2 — One identity, many providers

Cruma has its own user identity and issues its own sessions and tokens. External providers only prove who the
person is at sign-in (SEC-001). No module outside Identity knows that Google exists (NFR-14).

## P-3 — Content is the most sensitive data

Notes are a personal and technical knowledge base; they may contain credentials pasted by the user, internal
hostnames, or customer names. Therefore content never appears in logs, audit records, error messages, or
telemetry (LOG-002, AUD-002). Identifiers may.

## P-4 — Content leaves Cruma only when the user says so

Nothing is sent to a third party (cloud AI provider) without an explicit action by the user who sees provider
and scope first (AI-002, NFR-12). Local providers are not third parties.

## P-5 — Secrets live outside the code and outside user data

Secrets come from the environment (SEC-006). User-provided secrets (AI API keys) are encrypted at rest and
write-only from the client's perspective (SEC-007). Tokens on the desktop are protected by the operating system,
never stored in the local database (SEC-003, PER-005).

## P-6 — Honest threat model

The accepted protections are stated explicitly so nobody assumes more (NFR-9):

| Threat | Protected? | By |
|---|---|---|
| Network eavesdropping | yes | TLS (SEC-008) |
| Stolen server disk or backup | yes | storage and backup encryption (D-6.3) |
| Leaked database dump — AI API keys | yes | application-level encryption (SEC-007) |
| Leaked database dump — note content | **no** | accepted; end-to-end encryption rejected (D-6.3 b) |
| Another Cruma user | yes | server authorization and isolation (P-1) |
| Stolen or shared Windows account on the desktop | **no** | accepted; local database unencrypted, relies on OS disk encryption (NFR-9) |
| Script injection in the web client stealing tokens | yes | no tokens in browser storage, HttpOnly cookies (SEC-004) |

---

# 2. Authentication flows

| Client | Flow | Session material |
|---|---|---|
| Desktop | Authorization code + PKCE via system browser, loopback redirect (SEC-002) | Access token (short) + refresh token (DPAPI) |
| Web / tablet | External login on the same origin, cookie session (SEC-004) | HttpOnly, Secure, SameSite cookie |
| Mobile (I-5) | Authorization code + PKCE via system browser | Access token + refresh token in platform secure storage |

---

# 3. Rendering untrusted content

- Document content is rendered from the schema, never inserted as raw HTML.
- Link marks allow only `http`, `https` and `mailto` schemes.
- Diagram rendering runs with the renderer's safe/strict mode enabled; diagram SVG output is sanitized before
  display.
- Imported HTML (if ever supported) is sanitized into the schema, never stored as HTML.

---

# 4. Transport and hosting

- TLS terminated at the reverse proxy with automatically renewed certificates; HSTS enabled (SEC-008).
- Only the proxy port is exposed publicly; PostgreSQL is reachable only inside the container network.
- Rate limiting on authentication endpoints and on AI endpoints.

---

# End of Document
