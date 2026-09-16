# Cruma — Desktop Pattern

> **Type:** Pattern
> **Scope:** desktop
> **Enforces:** PER-001, PER-005, PER-006, SEC-002, SEC-003, SYN-004, SYN-007, ERR-002, AI-001
> **Derived from:** D-1, D-3.2, D-5.2, D-7, D-8.1, C-6, C-7a, C-8, C-11; SPEC FR-26, FR-27, FR-32, FR-33, FR-37, NFR-3

## Purpose

Describes the Windows desktop client: the only full, offline-capable client of Cruma.

---

# 1. Composition

```text
Cruma.Desktop (WPF, net10.0-windows)
├── MainWindow with BlazorWebView hosting Cruma.Ui
├── Platform services      Windows implementations of Cruma.Ui interfaces
├── Data services          over Cruma.Desktop.Storage (reads/writes) + Cruma.Sync.Client (status)
├── Sign-in                system browser, authorization code + PKCE, loopback redirect      SEC-002
├── Token store            DPAPI-protected file in the user's app data folder               SEC-003
├── Local blob store       files under the user's app data folder                            C-7a
├── Reminder scheduler     local, from synchronized reminders; Windows notifications         D-7
├── Local AI adapter       direct calls to local providers (e.g. LM Studio on localhost)      AI-001 (I-3)
└── Update                 check and install new versions; enforce minimum version on sync    FR-37
```

WebView2 is the rendering engine of BlazorWebView; it is present on Windows 11 and must be verified by the
installer on older systems.

---

# 2. Local storage (Cruma.Desktop.Storage)

- EF Core with SQLite, one database file per signed-in user in the user's app data folder (PER-001).
- Contains: notes, categories, tags, documents, local intermediate versions, pending change log, server sync
  cursor, search index (FTS5 via the search adapter), reminder schedule.
- Never contains tokens or API keys (PER-005). Not encrypted by the application (NFR-9).
- Local migrations are applied automatically at application start (the desktop has no separate deployment
  step), after copying the database file to a backup file next to it.
- The app data folder is resolved in one place in the composition root. A Debug build uses
  `%LOCALAPPDATA%\Cruma\dev\` for the database, blobs, logs and tokens and shows "(dev)" in the window title, so
  development never touches the data of the installed application (PER-006).

## 2.1 Pending change log

Every local write appends a pending change (entity, change identifier, base server version, payload). The sync
client consumes the log; entries are removed only after acknowledgement (SYN-004).

---

# 3. Offline behavior

- The application starts and shows data without network and without a valid token (SEC-003, FR-26).
- All note operations and search work offline. Unavailable offline: cloud AI, sync, sign-in of a new user
  (NFR-3).
- When the refresh token has expired, the application keeps working locally and shows "sign in to sync".

---

# 4. Sync client (Cruma.Sync.Client)

- Implements the session in `versioning-and-sync-pattern.md` §5.
- Runs on a background loop with the triggers from §5.2 of that document; never blocks the UI thread.
- Publishes status (`synced`, `pending`, `conflict`, `error`) to the UI (SYN-007).
- On `client_version_unsupported`: stops syncing, keeps all pending changes, prompts to update (ERR-002,
  FR-37).
- Downloads all blobs of the user in the background with low priority (D-5.2).

---

# 5. Reminders

- The desktop schedules reminders locally from synchronized reminder data and shows Windows notifications; it
  does not subscribe to server push (D-7).
- Reminders fire offline if they were synchronized before (FR-33 acceptance 3).

---

# 6. Updates

- The installer and updater install per user without administrator rights.
- Recommended implementation: **Velopack** (installer, delta updates, update check from a release feed).
- The update check runs at start and periodically; the server's minimum version (FR-37) is the hard stop, the
  update feed is the convenience.

---

# 7. Anti-patterns

- Opening Google sign-in inside the BlazorWebView.
- Storing the refresh token in SQLite or in a plain JSON settings file.
- Treating a failed sync as a reason to reload data from the server and discard local state.
- Doing sync work on the UI thread.

---

# End of Document
