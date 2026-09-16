# Cruma — UI Pattern

> **Type:** Pattern
> **Scope:** clients
> **Enforces:** UI-001..UI-006, DEP-003, DEP-004, SEC-004, AI-002, AI-003
> **Derived from:** D-1, frame §2.1 and §7, C-9, C-10, C-12, C-19; SPEC FR-1, FR-8, FR-21, FR-29, FR-30, FR-34, NFR-1, NFR-2, NFR-15, NFR-17, NFR-19

## Purpose

Describes how one Razor component library serves three shells — WPF desktop, WebAssembly PWA, and later MAUI —
without platform code leaking into components.

---

# 1. Layers

```text
┌───────────────────────────────── Cruma.Ui (Razor Class Library) ─────────────────────────────────┐
│ Pages & components (Notes, search, history, conflicts, AI, settings; Kanban in I-4)               │
│        │ uses                                                                                    │
│        ▼                                                                                         │
│ Data service interfaces         Platform service interfaces        Capability description        │
│ INoteData, ISearchData, …       IPlatformStorage, INotifications,  ICapabilities                 │
│                                 IShare, IFilePicker, IClipboard,                                  │
│                                 IConnectivity                                                     │
└──────────────────────────────────────────────────────────────────────────────────────────────────┘
          ▲ implemented by                         ▲ implemented by
┌─────────┴──────────────┐      ┌──────────────────┴────────┐      ┌───────────────────────────┐
│ Cruma.Desktop          │      │ Cruma.Web                 │      │ Cruma.Mobile (I-5)        │
│ data: local storage +  │      │ data: Cruma.Api.Client    │      │ data: Cruma.Api.Client    │
│       sync             │      │ platform: browser APIs    │      │ platform: MAUI essentials │
│ platform: Windows      │      │ write queue (IndexedDB)   │      │ write queue               │
└────────────────────────┘      └───────────────────────────┘      └───────────────────────────┘
```

- Components depend only on the interfaces (UI-001, UI-002).
- Each shell registers its implementations in its composition root (DEP-004).
- **Data service** interfaces are shaped by what the UI needs, not by REST endpoints or tables. The desktop
  implementation reads local storage; the thin-client implementation calls the API.

---

# 2. Capability description

The shell provides `ICapabilities` (UI-003), for example:

| Capability | Desktop | Web / tablet | Mobile |
|---|---|---|---|
| Works offline | yes | no (write queue only) | no (write queue only) |
| Kanban drag & drop | yes | no (A-5) | no (A-5) |
| Editor width modes | standard / wide / full | responsive | responsive |
| Local AI provider | yes | no | no |
| Conflict resolution | yes | yes | yes |

Components show, hide or disable features based on capabilities; they never check the platform type.

---

# 3. Quick capture (FR-1, NFR-1)

- The main view opens with the input focused.
- Typing and leaving the input (or pressing the save shortcut) saves the note into the default category
  (UI-005). No dialog, no required field.
- Everything else (title, tags, color, category) is reachable after saving, never before.

This flow is the success criterion S-1; any change that adds a step to it is a regression.

---

# 4. Editor

- The note page hosts the editor component from `Cruma.Ui.Editor` (see `content-document-pattern.md` §3).
- Only `Cruma.Ui.Editor` touches `IJSRuntime` (UI-001 exception).
- Desktop width modes are a page-level setting; on narrow screens tables and code blocks scroll on their own
  (FR-8).

---

# 5. AI interactions (increment I-3)

Every AI action follows one component flow:

```text
choose operation ─► confirmation panel: provider (local/cloud) + scope (selection / note / N notes)   (AI-002)
                 ─► run ─► proposal panel: diff or result ─► apply | discard                          (AI-003)
```

Cloud providers are never invoked without the confirmation panel. Background suggestions are offered only when
the configured provider is local.

---

# 6. Thin client specifics (Cruma.Web)

- Blazor WebAssembly PWA hosted by `Cruma.Server` on the same origin; cookie session (SEC-004).
- Service worker caches the application shell so the app starts without network; data still requires network.
- When offline: new-note capture writes to the queue (SYN-006); other actions are disabled with a visible
  offline indicator.
- No domain data in browser storage (UI-004).
- Known limitations accepted in SPEC NFR-17: slower cold start on phones; push notifications on iOS only after
  installation to the home screen.

---

# 7. Theme and style

- Light and dark themes defined once as CSS custom properties in `Cruma.Ui` (UI-006).
- Card colors of notes are theme tokens with a light and a dark value, not raw colors stored per note.

---

# 8. Anti-patterns

- `@inject IJSRuntime` in a page to "just read localStorage".
- `if (OperatingSystem.IsBrowser())` inside components.
- A component calling `HttpClient` directly.
- Duplicated `NoteListDesktop.razor` and `NoteListWeb.razor`.
- A save dialog on note creation.

---

# End of Document
