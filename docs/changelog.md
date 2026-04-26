# Changelog

## [Unreleased]
- Tech stack chosen: C# / .NET 8, WPF, WPF UI theme library, Tesseract OCR via the `Tesseract` NuGet package, xUnit tests
- v1 scope locked: core loop (hotkey → region select → OCR → RSVP overlay) + minimal main window; no history, no settings persistence, no rebindable hotkeys
- UI design locked: single-pane main window (Option B), no Optimal Recognition Point in v1
- Defaults locked: `Ctrl+Shift+R` hotkey, 300 WPM, `Space`/`Esc` playback controls, English OCR only
- Branch `v1` created off `main`
- Fixed Phase 2 scaffold wiring for the moved `MainWindow`: `ReadX.Views` namespace, `Views/MainWindow.xaml` startup URI, and `ReadX` window title; single-process build now succeeds.
- Renamed the placeholder xUnit test to `SmokeTest` and verified the test project passes.
- Installed approved Phase 2 NuGet packages: `WPF-UI` 4.2.0 and `Tesseract` 5.2.0.
- Downloaded `tessdata/eng.traineddata` from pinned official `tesseract-ocr/tessdata` commit `ced78752cc61322fb554c280d13360b35b8684e4`; SHA256 `DAA0C97D651C19FBA3B25E81317CD697E9908C8208090C94C3905381C23FC047`.
- Added project content wiring so `tessdata/*.traineddata` copies to app output under `tessdata/`.
- Phase 2 scaffold verified: `dotnet build -m:1` passes with zero warnings, `dotnet test -m:1` passes, tessdata copies to output, and the WPF app stays running after launch smoke test.
- v1 phase plans deepened (design only, no implementation yet): locked decisions for tokenization (Option A hyphen rejoin, 100–800 WPM clamp, `ITicker` seam for unit-testability), region select (Per-Monitor V2 via `app.manifest`, dual DIP/physical-pixel coordinate systems), capture (GDI `CopyFromScreen`; WGC flagged as v2 lever), OCR (computer-written text only, no preprocessing, `EngineMode.Default` + `PageSegMode.Auto`, no extra NuGets), hotkey (in-service `SetBusy` re-entrant guard, degraded mode on conflict), and orchestration (dedicated `AppController` state machine)
- Plan concerns resolved: hotkey registration moved after `MainWindow.SourceInitialized`, OCR degraded mode uses nullable `IOcrService?`, RSVP overlay lifecycle assigned to `IRsvpPresenter`/`RsvpPresenter`, GDI capture now has an explicit Windows drawing project-file step, tessdata must be fetched from a pinned official commit URL with SHA256 recorded, region select commits from physical mouse-down/up points, and stale Phase 2 `.gitignore` / PATH notes were corrected

## [0.0.1] - 2026-04-26
- Initial project setup
