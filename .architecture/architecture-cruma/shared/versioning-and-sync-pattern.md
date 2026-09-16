# Cruma — Versioning & Sync Pattern

> **Type:** Pattern
> **Scope:** shared, server, desktop
> **Enforces:** VER-001..VER-008, SYN-001..SYN-007, BLB-001..BLB-004, ERR-002
> **Derived from:** frame §2.1, D-3, D-5, C-4, C-6, C-7; SPEC FR-6, FR-26..FR-30, FR-37, NFR-4, NFR-7

## Purpose

Describes how versions come into existence, how concurrent changes are merged without silent loss, and how the
desktop and thin clients exchange changes with the server.

---

# 1. Topology recap

```text
            Thin clients (web, tablet, mobile)          Desktop (offline-capable)
            online, REST API, write queue only          local SQLite, pending change log
                         │                                         │
                         │ REST /api/v1                            │ Sync protocol
                         ▼                                         ▼
                 ┌────────────────────────────────────────────────────────┐
                 │ Server — authoritative source, authoritative merge     │
                 │ PostgreSQL + blob store                                │
                 └────────────────────────────────────────────────────────┘
```

- The server is authoritative; the desktop is the only offline writer (frame §2.1).
- "Authoritative" never means "server overwrites client": every incoming change is merged against its base
  (SYN-002).

---

# 2. Versions

## 2.1 Version lifecycle

```text
edit ─► autosave (content persisted, no version) ─► session end ─► version
                                                     ├─ inactivity timeout
                                                     ├─ note closed
                                                     └─ sync of this note
```

- Autosave persists the current state; it does not create a version (VER-006).
- On the **server**, a version gets a monotonically increasing number per note. Version numbers are assigned
  only by the server.
- On the **desktop**, versions created offline are *local intermediate versions*. They exist so the user can
  undo offline work, and are discarded after the server acknowledges the resulting change (SYN-004, D-3.2).

## 2.2 Version record

Each server version records: note identifier, version number, base version number (for merges), author user,
source (`desktop`, `web`, `mobile`, `ai`, `merge`, `restore`), timestamp, the document, and the metadata
snapshot. Scalar metadata overwritten by last-writer-wins stays visible here (VER-005).

## 2.3 Restore

Restoring version *k* creates version *n+1* with the content of *k* and source `restore` (VER-007).

---

# 3. Merge

Implemented in `Cruma.Versioning` as pure functions (VER-001); executed on the server only (VER-002).

## 3.1 Inputs

```text
base     — the version the incoming change started from
current  — the latest server version
incoming — the change sent by the client
```

If `base == current`, the incoming change is applied as the next version without merging.

## 3.2 Document merge (by top-level block id)

| Block in base | Change in current | Change in incoming | Result |
|---|---|---|---|
| exists | unchanged | changed | take incoming |
| exists | changed | unchanged | take current |
| exists | changed | changed identically | take either |
| exists | changed | changed differently | **conflict block** with both variants (VER-003) |
| exists | deleted | changed | keep incoming, mark as "restored after deletion" (VER-004) |
| exists | changed | deleted | keep current, mark (VER-004) |
| exists | deleted | deleted | deleted |
| absent | added | — | take current |
| absent | — | added | take incoming |

**Order of blocks:** merged as a list: relative order from the side that reordered; if both reordered
differently, use current's order and append incoming-only blocks after their nearest preceding common block.
Reordering alone never creates a conflict.

## 3.3 Conflict representation

A conflict is stored in the merged version as a block of type `conflict` wrapping two variants (`current`,
`incoming`), each with its origin (client type, time). The note carries a conflict flag. The conflict is
resolved on any client by choosing a variant or editing; resolution creates a new version (FR-28 acceptance 3).
Thin clients can resolve conflicts; they do not merge.

## 3.4 Metadata merge (D-3.4)

| Field kind | Examples | Rule |
|---|---|---|
| Set | tags, checklist items of a card | apply additions and removals from both sides |
| Scalar | title, category, color, pinned, state, priority, due date | changed on one side → take it; changed on both → later server receive wins; overwritten value remains in version history; sync status reports the overwrite |

## 3.5 Note-level delete vs. edit

Deleting a note moves it to trash (FR-5). An incoming edit of a note that is in trash restores it to active and
marks it (VER-004). Permanent deletion is possible only from trash and only as an explicit action; it deletes
the note's versions too (VER-008).

---

# 4. Blobs

- Identifier = SHA-256 of content (BLB-001); stored per user (BLB-002); never in a database (BLB-003).
- Upload is idempotent: uploading an existing blob identifier for the same user is a no-op.
- Clients upload referenced blobs before sending the document change (SYN-005).
- Desktop downloads all blobs of the user in the background (D-5.2); thin clients fetch on demand.
- Garbage collection: blobs unreferenced by any version of any note of the user, older than the grace period
  (BLB-004).

---

# 5. Desktop sync protocol

## 5.1 Session

```text
1. Handshake     client → appVersion, protocolVersion
                 server → accepted | rejected(client_version_unsupported, minimumVersion)   (SYN-001)
2. Upload blobs  for all pending changes                                                  (SYN-005)
3. Push          pending changes, each: changeId, entity, baseVersion, payload           (SYN-002, SYN-003)
                 server → per change: applied(version) | merged(version) | conflict(version) | rejected(code)
4. Pull          changes since the client's last server cursor
5. Commit        client removes acknowledged pending changes and their local intermediate versions (SYN-004)
```

- Push before pull, so the server merges the client's work against the newest state.
- A rejected change stays pending and is surfaced in sync status as `error`; it is never dropped (ERR-002).
- A handshake rejection keeps every pending change; the user is prompted to update (FR-37 acceptance 2).

## 5.2 Triggers

Sync runs: on start when online, on regaining connectivity, periodically while online, and shortly after a local
edit session ends.

## 5.3 Status

`synced`, `pending`, `conflict`, `error` (SYN-007).

---

# 6. Thin client write queue

- Holds only creations of new notes (A-4, SYN-006).
- Each item: client-generated note identifier, content, creation time.
- Stored in the browser (IndexedDB) through the platform storage service; survives restart.
- Replayed automatically when connectivity returns; the server treats a known note identifier as already
  created (SYN-003).
- Everything else on a thin client requires connectivity; the UI disables actions that would need it.

---

# 7. Anti-patterns

- Last-writer-wins for document content.
- Merging on the client and uploading the result as the new version.
- Deleting pending changes before the server acknowledgement.
- Creating a version per autosave.
- Letting the thin-client queue grow into an offline cache of existing notes.
- Storing image bytes inside versions.

---

# End of Document
