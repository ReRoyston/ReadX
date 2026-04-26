# Project Status

**Current Phase:** v1 - Core loop (Phase 2 scaffold verified, pending commit)

## Done
- Initial project scaffold (CLAUDE.md, AGENTS.md, docs/, .gitignore)
- Branch `v1` created
- Tech stack chosen and approved
- v1 scope locked
- UI design mockups approved (single-pane main window, no ORP for v1)
- v1 defaults locked
- **Phase 2 partial:**
  - .NET 8.0.420 SDK confirmed at `C:\Program Files\dotnet\`; current PowerShell sees `dotnet` on PATH
  - Git LFS 3.7.1 confirmed; `git lfs install` run for repo; `tessdata/*.traineddata` tracked via `.gitattributes`
  - `ReadX.sln` created at repo root
  - WPF project at `src/ReadX.csproj` (target `net8.0-windows`)
  - xUnit test project at `tests/ReadX.Tests/ReadX.Tests.csproj` (target bumped to `net8.0-windows` so it can reference the WPF project)
  - Both projects added to `ReadX.sln`; test project references `src/ReadX.csproj`
  - `MainWindow.xaml` and `MainWindow.xaml.cs` physically moved from `src/` to `src/Views/`
  - Moved `MainWindow` wiring fixed: namespace is `ReadX.Views`, `App.xaml` starts `Views/MainWindow.xaml`, and the window title is `ReadX`
  - Verified `dotnet build --no-restore -m:1` succeeds after project-level restore
  - Renamed placeholder xUnit test from `UnitTest1` to `SmokeTest`; verified `dotnet test tests\ReadX.Tests\ReadX.Tests.csproj --no-restore -m:1` passes
  - Installed approved NuGet packages in `src/ReadX.csproj`: `WPF-UI` 4.2.0 and `Tesseract` 5.2.0
  - Downloaded `tessdata/eng.traineddata` from pinned official `tesseract-ocr/tessdata` commit `ced78752cc61322fb554c280d13360b35b8684e4`
  - `eng.traineddata` SHA256: `DAA0C97D651C19FBA3B25E81317CD697E9908C8208090C94C3905381C23FC047`; size: `23466654` bytes
  - Added `src/ReadX.csproj` content wiring so `tessdata/*.traineddata` copies to `src/bin/Debug/net8.0-windows/tessdata/`
  - Verified copied output `eng.traineddata` exists and matches SHA256 `DAA0C97D651C19FBA3B25E81317CD697E9908C8208090C94C3905381C23FC047`
  - Verified `dotnet build -m:1` passes with zero warnings
  - Verified `dotnet test -m:1` passes: 1 passed, 0 failed
  - Verified `dotnet run --project src\ReadX.csproj --no-build` launch smoke: app stayed running after startup
  - `.gitignore` already ignores `bin/`, `obj/`, and `*.user`
  - Plan concerns resolved: hotkey registration after `SourceInitialized`, nullable OCR dependency for degraded mode, `RsvpPresenter` overlay ownership, explicit Windows drawing support for GDI capture, pinned tessdata download rule, and physical mouse-down/up coordinates for region select

## Up Next (resume Phase 2)
1. Commit Phase 2 scaffold on `v1` branch

## Known Issues
- None for Phase 2 scaffold.
- **Shell PATH note.** Current PowerShell sees `dotnet`. Older bash sessions may still need a terminal restart if they do not see `C:\Program Files\dotnet`.
