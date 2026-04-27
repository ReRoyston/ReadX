# Architecture

## Stack
- **Language / runtime:** C# on .NET 8 (LTS)
- **UI framework:** WPF
- **Theme library:** WPF UI (`Wpf.Ui`) — Fluent / Windows 11 styling
- **OCR engine:** Tesseract via the `Tesseract` NuGet package (charlesw); English, `tessdata/eng.traineddata`
- **Tests:** xUnit
- **Packaging:** `dotnet publish` self-contained Windows .exe (TBD)

## Product
ShareX-style RSVP utility. Global hotkey → drag region → screen capture → OCR → RSVP overlay plays the words above the captured region at a configurable WPM. v1 ships only this core loop plus a minimal main window.

v2 is planned as a daily-use release. It keeps the core loop, then adds persisted settings, raw-text history, quick manual import, configurable hotkeys, text cleanup, restartable playback controls, classic ORP focused-letter rendering, and a compact left-tab main window.

## Components
Single WPF project; modular layout, separated by concern.

- `Views/MainWindow` — single-pane v1 shell: Capture button, hotkey label, WPM slider, last-OCR preview, Replay button.
- `Views/RegionSelectOverlay` — borderless transparent topmost window across the virtual screen; drag-rectangle with size/origin readout; `Esc` cancels.
- `Views/RsvpOverlay` — borderless topmost window pinned above the captured region; renders one word at a time with progress bar and footer.
- `Services/HotkeyService` — Win32 `RegisterHotKey` via P/Invoke, registered against the main window HWND after `SourceInitialized`.
- `Services/ScreenCaptureService` — DPI-aware capture of the chosen rect to `Bitmap`.
- `Services/OcrService` — wrapper around the `Tesseract` NuGet package; image → text.
- `Services/RsvpPlayer` — WPM-driven word stream; pause/resume; `DispatcherTimer` for UI tick.
- `Services/RsvpPresenter` — owns RSVP overlay window lifecycle, positioning, keyboard routing, and binding player events to the view.
- `Models/CaptureRegion`, `Models/RsvpSession` — plain data.
- `Models/AppSettings`, `Models/HotkeyBinding` - persisted user preferences and shortcut bindings.
- `Models/HistoryItem`, `Models/HistorySource` - raw capture/import history records.
- `Services/JsonSettingsStore` - JSON settings persistence under user app data; saves through a same-directory temp file before replacing/moving into place.
- `Services/JsonHistoryStore` - JSON raw-text history persistence under user app data; serializes access and saves through unique same-directory temp files before moving into place with overwrite.
- `Text/TextCleanupService` - optional cleanup before tokenization; rejoins hyphenated line wraps, normalizes whitespace, and preserves paragraph breaks.
- `Text/TextPipeline` - shared raw text -> processed text -> word list path for capture, import, and history replay.
- `Tokenization/WordSplitter` — pure logic, processed text → ordered whitespace-delimited word tokens.

Planned v2 additions:

- `Rsvp/OrpCalculator`, `Rsvp/OrpWord` - pure ORP focus-letter calculation for overlay rendering.

Pure logic (`Tokenization/`, `Models/`, `RsvpPlayer` minus its timer) is testable without WPF. OS-touching services sit behind small interfaces so they can be stubbed.

## Key Design Decisions
- **v2 history persistence is serialized and temp-file based.** `JsonHistoryStore` serializes public operations with a store-level semaphore, writes retained history to unique `history.json.*.tmp` files in the same directory, then moves into `history.json` with overwrite. Missing or invalid JSON history loads as empty; IO/read failures propagate so writes do not overwrite a real history file after a transient read failure. Whitespace-only entries are not stored, duplicates are kept, and retention trims the oldest items. (2026-04-28)
- **v2 text cleanup is a shared optional pipeline step.** `TextCleanupService` handles hyphenated line-wrap rejoining, line/paragraph normalization, and whitespace cleanup before `WordSplitter` runs. `TextPipeline` returns raw text, processed text, and words so capture, import, and history replay can share one processing path while respecting the cleanup toggle. (2026-04-28)
- **v2 settings persistence is temp-file based.** `JsonSettingsStore.SaveAsync` serializes to `settings.json.tmp` in the same directory, then uses `File.Replace` for existing settings files or `File.Move(..., overwrite: true)` for first save. This avoids truncating the existing settings file before serialization succeeds. (2026-04-28)
- **v2 foundation-first build order.** Settings/history persistence and text pipeline come before UI replacement so capture, import, and history replay share the same contracts. (2026-04-28)
- **v2 left-side navigation.** Main window moves from v1 single-pane to compact left tabs for Capture, Import, History, and Settings. This avoids a tall portrait layout while keeping the utility feel. (2026-04-28)
- **v2 history stores raw text only.** History is an activity log, not a library. Replays reprocess raw text through the current cleanup, tokenization, and RSVP renderer so future engine improvements apply to old entries. (2026-04-28)
- **v2 classic ORP rendering.** RSVP output will highlight the focus letter and align words around a stable ORP anchor; customization is deferred. (2026-04-28)
- **Single-pane main window for v1.** Sidebar / navigation-view deferred until History and Settings exist — avoids a UI that pretends to have features it doesn't. (2026-04-26)
- **No Optimal Recognition Point in v1.** RSVP overlay shows centred words only; ORP / focus-letter pivot deferred to a later release. (2026-04-26)
- **Defaults:** hotkey `Ctrl+Shift+R`, default WPM `300`, playback controls `Space` (pause) / `Esc` (cancel), OCR language English only. (2026-04-26)
- **No persistence in v1.** No history, no settings file, no rebindable hotkeys — keeps the v1 surface area to the core loop. (2026-04-26)
- **OCR scope (v1): computer-written text only.** No image preprocessing; `EngineMode.Default` (LSTM); `PageSegMode.Auto`. Handwriting and photographed text out of scope. If accuracy disappoints on real screen text, revisit preprocessing in v2. (2026-04-27)
- **Screen capture: GDI `Graphics.CopyFromScreen`.** Simple, Windows-only, works on Win10+. Phase 6 explicitly enables the required Windows desktop drawing support in the project file; Windows Graphics Capture (WGC) is flagged as a v2 lever for hardware-accelerated content. (2026-04-27)
- **DPI: Per-Monitor V2 via `app.manifest`.** Region select uses two coordinate systems on purpose — WPF DIPs for the drag preview, `GetCursorPos` physical pixels for both mouse-down and mouse-up, then commits a `CaptureRegion` relative to the virtual-screen origin. Avoids WPF transform pitfalls in mixed-DPI multi-monitor setups. (2026-04-27)
- **Orchestrator: dedicated `AppController` class.** `App.xaml.cs` stays a thin composition root; `AppController` holds the state machine (Idle → Selecting → Capturing → Recognising → Playing → Idle) and is independently testable. (2026-04-27)
- **RSVP presentation split.** `RsvpPlayer` owns timing/state, `RsvpOverlay` owns rendering, and `RsvpPresenter` owns WPF window lifecycle and keyboard routing. (2026-04-27)
- **Hotkey registration timing.** v1 registers `Ctrl+Shift+R` only after `MainWindow.SourceInitialized`, using the main window HWND rather than a hidden message window. (2026-04-27)
- **OCR unavailable mode.** If `OcrService` cannot be constructed, `AppController` receives `IOcrService?` as `null`, reports `OcrAvailable = false`, and disables Capture while keeping the app open. (2026-04-27)
- **RsvpPlayer testability via `ITicker` seam.** Production wraps `DispatcherTimer`; tests use a `FakeTicker`. Lets the state machine be unit-tested without WPF or sleeping. (2026-04-27)
- **WordSplitter stays deliberately narrow for v2.** Cleanup-specific transformations live in `TextCleanupService`; `WordSplitter` only splits processed text into whitespace-delimited tokens. (2026-04-28)
