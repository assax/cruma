# Cruma — Content Document Pattern

> **Type:** Pattern
> **Scope:** shared, clients
> **Enforces:** CNT-001..CNT-009, UI-001 (editor exception)
> **Derived from:** D-2, D-3.3, D-5.1, C-3, C-10; SPEC FR-7, FR-9..FR-12, NFR-13

## Purpose

Describes how note content is represented, how the editor integrates with it, and how it is exported. Content
representation is the root decision of the architecture (analysis §5): merge, versions, search, AI and export
all consume it.

---

# 1. Document model

A note's content is a **document**: a JSON tree defined by the Cruma schema, owned by `Cruma.Content` (CNT-001).
The shape follows the ProseMirror node model so that the TipTap editor can load and emit it without translation,
but the schema — which node types and marks exist and what attributes they carry — is Cruma's.

```text
Document
├── schemaVersion                      (CNT-003)
└── content: Block[]
      Block
      ├── type        paragraph | heading | bulletList | orderedList | taskList | table | codeBlock | diagram | image | …
      ├── id          stable block identifier (CNT-002)
      ├── attrs       type-specific attributes (heading level, code language, diagram kind, blob id, …)
      └── content     inline nodes with marks (bold, italic, underline, strike, code, link, highlight, textColor, backgroundColor)
```

## 1.1 What counts as a block

A **block** is a top-level child of the document. Merge (VER-003) and version comparison operate on top-level
blocks. Nested structures (list items, table cells) belong to their top-level block; a change inside a list is a
change of that list block.

*Why top-level only:* block-level conflicts must be understandable to the user. A conflict inside the third cell
of a table row is shown as a conflict of the table. Finer granularity can be introduced later by increasing the
schema version; it cannot be taken back once users rely on it.

## 1.2 Block identifiers

- Assigned when the block is created, by a Cruma editor extension on the client or by `Cruma.Content` on the
  server (for AI-generated or imported content) (CNT-002, CNT-006).
- Never changed when a block is edited, moved, or its type changes (paragraph → heading keeps the id).
- Pasted or duplicated blocks always receive a new id.
- Splitting a block keeps the id on the first part and assigns a new id to the second; joining two blocks keeps
  the id of the first.

## 1.3 Type registry and unknown nodes

`Cruma.Content` keeps a registry of block and mark types with the schema version that introduced them. A client
that reads a node type it does not know **preserves it byte-for-byte** on save and renders a neutral placeholder
("content from a newer version") (CNT-005). This is what allows the desktop to be one version behind the server
without destroying content.

## 1.4 Schema versions and migrations

- Each stored document states `schemaVersion`.
- `Cruma.Content` contains one migration per version step; reading a document applies migrations in order
  (CNT-003).
- Migrations are pure functions `Document(vN) → Document(vN+1)` and have fixture-based tests.
- A version bump that only adds node types needs no migration, only a registry entry.

---

# 2. Binary content

Images are blocks of type `image` whose attributes carry the blob identifier (SHA-256), dimensions and
alternative text — never the bytes (CNT-004, BLB-001). See `versioning-and-sync-pattern.md` §4 for blob sync.

---

# 3. Editor integration

```text
Cruma.Ui (Razor)                 Cruma.Ui.Editor
┌──────────────────┐   load(doc json)   ┌─────────────────────────────────┐
│ NoteEditor page  │ ─────────────────► │ interop layer (only JS access)  │
│                  │ ◄───────────────── │  TipTap + Cruma extensions:     │
│                  │  changed(doc json) │   blockId, codeBlock, diagram,  │
│                  │  (debounced)       │   image (blob id)               │
└──────────────────┘                    └─────────────────────────────────┘
```

- The interop surface is intentionally small: load a document, receive change notifications with the full
  document, execute a named command (e.g. insert image with blob id), receive selection text for AI operations.
- C# never inspects or mutates TipTap state (CNT-006).
- The JS bundle is built from sources inside `Cruma.Ui.Editor` and shipped as a static web asset; the same
  bundle runs in BlazorWebView (desktop) and WebAssembly (web).
- Third-party editor extensions may be used only if they are free to use; the block id extension is always
  Cruma's own (CNT-006).

## 3.1 Code blocks (increment I-2)

Attributes: `language` (explicit or null), `detectedLanguage`. Highlighting and detection run in the editor.
Formatting runs only on explicit user action. See profile open item OP-2 for languages without a browser-side
formatter.

## 3.2 Diagrams (increment I-2)

Attributes: `kind` (`mermaid` | `plantuml` | registered future kinds), source text as block content. The source
is the stored truth; the preview is derived and never stored in the document. Invalid source shows the error in
preview and keeps the source (FR-11). Mermaid renders in the editor. PlantUML rendering: see open item OP-1.

---

# 4. Plain-text extraction

`Cruma.Content` provides one extraction used by both search adapters and by AI context assembly (CNT-007):
text of inline nodes in reading order, table cells separated by whitespace, code block text included, diagram
source included, image alternative text included.

---

# 5. Export

| Format | Purpose | Guarantees |
|---|---|---|
| Lossless (Cruma JSON package) | backup, migration to another tool | Round-trips exactly, including block ids and unknown nodes (CNT-008) |
| HTML | readable, keeps most formatting | Self-contained per note; images as files next to it |
| Markdown | readable, portable | CommonMark + GFM tables and task lists only; lossy by design (CNT-009) |

A full export (FR-12) is a package: one folder per format, notes grouped by category, metadata in JSON, images
as files named by blob id.

---

# 6. Anti-patterns

- Storing HTML produced by the editor "because it is easier to render" — loses block identity (D-2 REJECTED b).
- Adding a node type in the editor without registering it in `Cruma.Content`.
- Dropping unknown nodes when saving.
- Regenerating block ids on every load or on paste-in-place.
- Rendering diagram previews into the document.

---

# End of Document
