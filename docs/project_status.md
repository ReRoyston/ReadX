# Project Status

**Current Phase:** v1 — Core loop (Phase 2 partially scaffolded, paused mid-step)

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
  - `.gitignore` already ignores `bin/`, `obj/`, and `*.user`
  - Plan concerns resolved: hotkey registration after `SourceInitialized`, nullable OCR dependency for degraded mode, `RsvpPresenter` overlay ownership, explicit Windows drawing support for GDI capture, pinned tessdata download rule, and physical mouse-down/up coordinates for region select

## Up Next (resume Phase 2)
1. Fix namespace mismatch in moved files (see Known Issues)
2. Update `src/App.xaml` `StartupUri` from `MainWindow.xaml` to `Views/MainWindow.xaml`
3. Rename `tests/ReadX.Tests/UnitTest1.cs` → `SmokeTest.cs` (and class)
4. **Approval gate:** install `Wpf.Ui` and `Tesseract` NuGet packages
5. Download `tessdata/eng.traineddata` from a pinned official Tesseract commit URL and record its SHA256 checksum
6. Add `<Content>` block to `src/ReadX.csproj` so tessdata copies to output
7. Verify: `dotnet build`, `dotnet test`, `dotnet run --project src/ReadX.csproj`
8. Commit on `v1` branch

## Known Issues
- **MainWindow namespace mismatch (build-blocking).** The files now live at `src/Views/MainWindow.xaml` and `src/Views/MainWindow.xaml.cs` but their declared namespace is still `ReadX` (not `ReadX.Views`). `App.xaml`'s `StartupUri` still points at `MainWindow.xaml` (no `Views/` prefix). The project will not build until both are fixed.
- **Shell PATH note.** Current PowerShell sees `dotnet`. Older bash sessions may still need a terminal restart if they do not see `C:\Program Files\dotnet`.
