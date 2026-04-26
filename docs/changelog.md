# Changelog

## [Unreleased]
- Tech stack chosen: C# / .NET 8, WPF, WPF UI theme library, Tesseract.NET OCR, xUnit tests
- v1 scope locked: core loop (hotkey → region select → OCR → RSVP overlay) + minimal main window; no history, no settings persistence, no rebindable hotkeys
- UI design locked: single-pane main window (Option B), no Optimal Recognition Point in v1
- Defaults locked: `Ctrl+Shift+R` hotkey, 300 WPM, `Space`/`Esc` playback controls, English OCR only
- Branch `v1` created off `main`
- Phase 2 partial: solution + WPF project (`src/`) + xUnit test project (`tests/ReadX.Tests/`) scaffolded; both target `net8.0-windows`; LFS initialised and tracking `tessdata/*.traineddata`; `MainWindow` moved into `src/Views/` (namespaces not yet updated — build-blocking until fixed); NuGet packages, tessdata download, csproj content wiring, and verification still pending
- v1 phase plans deepened (design only, no implementation yet): locked decisions for tokenization (Option A hyphen rejoin, 100–800 WPM clamp, `ITicker` seam for unit-testability), region select (Per-Monitor V2 via `app.manifest`, dual DIP/physical-pixel coordinate systems), capture (GDI `CopyFromScreen`; WGC flagged as v2 lever), OCR (computer-written text only, no preprocessing, `EngineMode.Default` + `PageSegMode.Auto`, no extra NuGets), hotkey (in-service `SetBusy` re-entrant guard, degraded mode on conflict), and orchestration (dedicated `AppController` state machine)

## [0.0.1] - 2026-04-26
- Initial project setup
