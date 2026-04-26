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
  - .NET 8.0.420 SDK confirmed at `C:\Program Files\dotnet\` (winget install; current shell does not have it on PATH yet)
  - Git LFS 3.7.1 confirmed; `git lfs install` run for repo; `tessdata/*.traineddata` tracked via `.gitattributes`
  - `ReadX.sln` created at repo root
  - WPF project at `src/ReadX.csproj` (target `net8.0-windows`)
  - xUnit test project at `tests/ReadX.Tests/ReadX.Tests.csproj` (target bumped to `net8.0-windows` so it can reference the WPF project)
  - Both projects added to `ReadX.sln`; test project references `src/ReadX.csproj`
  - `MainWindow.xaml` and `MainWindow.xaml.cs` physically moved from `src/` to `src/Views/`

## Up Next (resume Phase 2)
1. Fix namespace mismatch in moved files (see Known Issues)
2. Update `src/App.xaml` `StartupUri` from `MainWindow.xaml` to `Views/MainWindow.xaml`
3. Rename `tests/ReadX.Tests/UnitTest1.cs` → `SmokeTest.cs` (and class)
4. **Approval gate:** install `Wpf.Ui` and `Tesseract` NuGet packages
5. Download `tessdata/eng.traineddata` from official Tesseract repo
6. Add `<Content>` block to `src/ReadX.csproj` so tessdata copies to output
7. Extend `.gitignore` with `bin/`, `obj/`, `*.user`
8. Verify: `dotnet build`, `dotnet test`, `dotnet run --project src/ReadX.csproj`
9. Commit on `v1` branch

## Known Issues
- **MainWindow namespace mismatch (build-blocking).** The files now live at `src/Views/MainWindow.xaml` and `src/Views/MainWindow.xaml.cs` but their declared namespace is still `ReadX` (not `ReadX.Views`). `App.xaml`'s `StartupUri` still points at `MainWindow.xaml` (no `Views/` prefix). The project will not build until both are fixed.
- **Shell PATH.** The current bash session does not see `dotnet`. New sessions either need a terminal restart or invocations need to use `PATH="/c/Program Files/dotnet:$PATH" dotnet ...`.
