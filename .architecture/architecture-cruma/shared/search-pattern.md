# Cruma — Search Pattern

> **Type:** Pattern
> **Scope:** shared, server, desktop
> **Enforces:** SRC-001..SRC-004, PER-004, CNT-007, TST-002
> **Derived from:** D-4, D-8.2, C-5; SPEC FR-24, FR-25, NFR-5, NFR-6; analysis K-6, O-9

## Purpose

Describes how search produces identical result sets on the desktop (SQLite) and on the server (PostgreSQL) for
Czech text with diacritics.

---

# 1. Pipeline

```text
Indexing:  Note / card / project ─► Cruma.Content plain-text extraction (CNT-007)
                                  ─► Cruma.Search Normalizer ─► Tokenizer ─► tokens
                                  ─► index adapter stores tokens (SQLite FTS5 | PostgreSQL tsvector)

Query:     user query ─► Normalizer ─► Tokenizer ─► tokens ─► adapter: every token as prefix match, AND
```

Normalization and tokenization exist only in `Cruma.Search` (SRC-001). Adapters store and match already
produced tokens.

---

# 2. Normalizer

Applied in this order:

1. Unicode normalization to NFC.
2. Case folding (culture-invariant).
3. Decomposition to NFD, removal of combining marks, recomposition — `Certifikát` → `certifikat`.
4. Mapping of characters without decomposition used in Czech and nearby languages where needed (none required
   initially; additions increment the normalizer version).

The normalizer has a version constant (SRC-002). Stored index rows carry the normalizer version; on mismatch
the adapter reindexes.

# 3. Tokenizer

- Splits on anything that is not a letter or digit.
- Keeps tokens of length ≥ 1; no stop words; no stemming (NFR-5).
- Emits tokens in a canonical single-space-separated string for storage.

---

# 4. Adapters

| Adapter | Location | Storage | Matching |
|---|---|---|---|
| SQLite | `Cruma.Desktop.Storage` | FTS5 virtual table with a tokenizer that only splits on whitespace (no built-in diacritics removal, no stemming) | FTS5 prefix queries per token |
| PostgreSQL | `Cruma.Server/Search` | `tsvector` built with the `simple` configuration from the token string | `to_tsquery('simple', 'tok1:* & tok2:*')` |

Both adapters: filter by entity type if requested; restrict to the current user inside the adapter on the
server (SRC-004).

**Ranking** may differ between adapters; **result sets must not** (TST-002). The UI sorts by ranking where
available and falls back to modification time.

---

# 5. What is indexed

| Entity | Indexed text |
|---|---|
| Note | title + plain text of the document; archived and trashed notes indexed with state for filtering |
| Card (I-4) | title + description text + checklist item text |
| Project (I-4) | name |

---

# 6. AI context retrieval (increment I-3)

Cross-note AI questions (FR-15) retrieve candidate notes through this search (D-8.2): the question is reduced to
query tokens, top results are loaded, their plain text is passed to the model together with note identifiers
(AI-005). A vector index is a recorded future extension and is not implemented.

---

# 7. Anti-patterns

- Enabling `remove_diacritics` in the SQLite tokenizer or `unaccent` in PostgreSQL instead of normalizing in C#.
- Using a language-specific PostgreSQL text search configuration (stemming differs from desktop).
- Filtering by user only in the endpoint.

---

# End of Document
