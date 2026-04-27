# Changelog

## [Unreleased]
- Added v2 raw-text history persistence with `HistoryItem`, `HistorySource`, `IHistoryStore`, and `JsonHistoryStore`.
- History saves now write through `history.json.tmp` before moving into `history.json`, preserving duplicate entries, trimming oldest entries by limit, ignoring whitespace-only text, and cleaning stale temp files after successful saves.
- Added focused history-store coverage; verified `dotnet test -m:1` passes with 24 tests.
- Hardened v2 settings persistence: settings saves now write through `settings.json.tmp` and atomically replace/move into place, and non-finite window settings normalize to safe defaults.
- Added focused settings-store coverage for temp-file cleanup and non-finite window value normalization; verified full test suite passes with 20 tests.
- Updated architecture, project status, and journal docs with the approved v2 planning decisions and workflow lessons.
- Added the ReadX v2 implementation plan under `docs/superpowers/plans/`, sequenced foundation-first across persistence, text processing, playback, ORP rendering, hotkeys, UI wiring, verification, and docs.
- Added the approved ReadX v2 design spec covering daily-use features, left-side tab navigation, JSON settings, raw-text history, quick import, configurable hotkeys, text cleanup, replay controls, and classic ORP rendering.
- Installed Superpowers as external Codex workflow tooling via `C:\Users\Royston\.codex\superpowers` and the `C:\Users\Royston\.agents\skills\superpowers` junction; no ReadX app source or stack changes.
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
- Phase 3 implemented: `WordSplitter`, `ITicker`, `DispatcherTicker`, `RsvpPlayer`, and `RsvpSession`, with focused xUnit coverage for tokenization and playback state.
- Phase 3 verified: `dotnet test -m:1` passes with 13 tests.
- Phase 4 implemented: borderless `RsvpOverlay`, `IRsvpPresenter` / `RsvpPresenter` lifecycle, `Space` pause/resume routing, `Esc` cancel routing, and `CaptureRegion` moved forward to support the presenter contract.
- Phase 4 verified: `dotnet build -m:1` passes with zero warnings and `dotnet test -m:1 --no-build` passes with 13 tests.
- Phase 5 implemented: Per-Monitor V2 `app.manifest`, `RegionSelectOverlay`, `IRegionSelector`, and `RegionSelector`; selection previews use WPF DIPs while committed `CaptureRegion` values use physical cursor coordinates.
- Phase 5 verified by build/regression tests: `dotnet build -m:1` passes with zero warnings and `dotnet test -m:1 --no-build` passes with 13 tests.
- Phase 6 implemented: `IScreenCaptureService` / `ScreenCaptureService` using GDI `Graphics.CopyFromScreen`; enabled Windows desktop drawing support via `UseWindowsForms`.
- Phase 6 verified by build/regression tests: `dotnet build -m:1` passes with zero warnings and `dotnet test -m:1 --no-build` passes with 13 tests.
- Phase 7 implemented: `IOcrService` / `OcrService` using `TesseractEngine`, `EngineMode.Default`, `PageSegMode.Auto`, and bundled `tessdata/eng.traineddata`.
- Phase 7 verified by build/regression tests: `dotnet build -m:1` passes with zero warnings and `dotnet test -m:1 --no-build` passes with 13 tests.
- Phase 8 implemented: `IHotkeyService`, `HotkeyService`, and `HotkeyMapping` using Win32 `RegisterHotKey`, WPF `HwndSource` message hooks, and an internal busy guard.
- Phase 8 verified by build/regression tests: `dotnet build -m:1` passes with zero warnings and `dotnet test -m:1 --no-build` passes with 13 tests.
- Phase 9 implemented: WPF UI-themed `MainWindow` shell with Capture, hotkey/OCR status, WPM slider, last OCR preview, Replay last, and event/state API for controller wiring.
- Phase 9 verified: `dotnet build -m:1`, `dotnet test -m:1 --no-build`, and a brief WPF launch smoke all pass.
- Phase 10 wiring implemented: added `AppController`, `AppState`, explicit `App.OnStartup` composition root, OCR degraded mode, hotkey registration after `MainWindow.SourceInitialized`, manual capture/replay wiring, and controller-driven main-window state updates.
- Phase 10 verified: `dotnet build -m:1`, `dotnet test -m:1 --no-build`, brief wired-app launch smoke, and user-confirmed interactive golden path all pass.
- Added journal notes capturing lessons from the v1 core-loop build.
- Cleaned up stale pre-PR status docs to reflect that `v1` is committed and pushed, with only final verification and PR creation remaining.
- Recorded final v1 verification: build/test passed and all manual pre-PR checks were user-confirmed.
- v1 phase plans deepened (design only, no implementation yet): locked decisions for tokenization (Option A hyphen rejoin, 100–800 WPM clamp, `ITicker` seam for unit-testability), region select (Per-Monitor V2 via `app.manifest`, dual DIP/physical-pixel coordinate systems), capture (GDI `CopyFromScreen`; WGC flagged as v2 lever), OCR (computer-written text only, no preprocessing, `EngineMode.Default` + `PageSegMode.Auto`, no extra NuGets), hotkey (in-service `SetBusy` re-entrant guard, degraded mode on conflict), and orchestration (dedicated `AppController` state machine)
- Plan concerns resolved: hotkey registration moved after `MainWindow.SourceInitialized`, OCR degraded mode uses nullable `IOcrService?`, RSVP overlay lifecycle assigned to `IRsvpPresenter`/`RsvpPresenter`, GDI capture now has an explicit Windows drawing project-file step, tessdata must be fetched from a pinned official commit URL with SHA256 recorded, region select commits from physical mouse-down/up points, and stale Phase 2 `.gitignore` / PATH notes were corrected

## [0.0.1] - 2026-04-26
- Initial project setup
