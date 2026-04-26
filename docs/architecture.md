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

## Components
Single WPF project; modular layout, separated by concern.

- `Views/MainWindow` — single-pane v1 shell: Capture button, hotkey label, WPM slider, last-OCR preview, Replay button.
- `Views/RegionSelectOverlay` — borderless transparent topmost window across the virtual screen; drag-rectangle with size/origin readout; `Esc` cancels.
- `Views/RsvpOverlay` — borderless topmost window pinned above the captured region; renders one word at a time with progress bar and footer.
- `Services/HotkeyService` — Win32 `RegisterHotKey` via P/Invoke, hooked to the WPF message loop.
- `Services/ScreenCaptureService` — DPI-aware capture of the chosen rect to `Bitmap`.
- `Services/OcrService` — Tesseract.NET wrapper; image → text.
- `Services/RsvpPlayer` — WPM-driven word stream; pause/resume; `DispatcherTimer` for UI tick.
- `Models/CaptureRegion`, `Models/RsvpSession` — plain data.
- `Tokenization/WordSplitter` — pure logic, text → ordered word tokens.

Pure logic (`Tokenization/`, `Models/`, `RsvpPlayer` minus its timer) is testable without WPF. OS-touching services sit behind small interfaces so they can be stubbed.

## Key Design Decisions
- **Single-pane main window for v1.** Sidebar / navigation-view deferred until History and Settings exist — avoids a UI that pretends to have features it doesn't. (2026-04-26)
- **No Optimal Recognition Point in v1.** RSVP overlay shows centred words only; ORP / focus-letter pivot deferred to a later release. (2026-04-26)
- **Defaults:** hotkey `Ctrl+Shift+R`, default WPM `300`, playback controls `Space` (pause) / `Esc` (cancel), OCR language English only. (2026-04-26)
- **No persistence in v1.** No history, no settings file, no rebindable hotkeys — keeps the v1 surface area to the core loop. (2026-04-26)
- **OCR scope (v1): computer-written text only.** No image preprocessing; `EngineMode.Default` (LSTM); `PageSegMode.Auto`. Handwriting and photographed text out of scope. If accuracy disappoints on real screen text, revisit preprocessing in v2. (2026-04-27)
- **Screen capture: GDI `Graphics.CopyFromScreen`.** Simple, dependency-free, works on Win10+. Windows Graphics Capture (WGC) flagged as a v2 lever for hardware-accelerated content. (2026-04-27)
- **DPI: Per-Monitor V2 via `app.manifest`.** Region select uses two coordinate systems on purpose — WPF DIPs for the drag preview, `GetCursorPos` physical pixels at virtual-screen origin (which can be negative on multi-monitor) for the committed `CaptureRegion`. Avoids WPF transform pitfalls in mixed-DPI multi-monitor setups. (2026-04-27)
- **Orchestrator: dedicated `AppController` class.** `App.xaml.cs` stays a thin composition root; `AppController` holds the state machine (Idle → Selecting → Capturing → Recognising → Playing → Idle) and is independently testable. (2026-04-27)
- **RsvpPlayer testability via `ITicker` seam.** Production wraps `DispatcherTimer`; tests use a `FakeTicker`. Lets the state machine be unit-tested without WPF or sleeping. (2026-04-27)
- **WordSplitter: always rejoin hyphenated line-wrap fragments (Option A).** Trade-off — real hyphenated terms (`state-of-the-art`) get rejoined too. v2 review item: replace with a smarter heuristic. (2026-04-27)
