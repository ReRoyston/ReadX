# Journal

A running log of my learnings from building with AI.

---

## 2026-04-27

### Absolute rules only work if the worker actually enforces them
The user asked Codex to "implement everything" and later explicitly said to ignore the `continue` checkpoint rule. Because `AGENTS.md` marked those rules as absolute, Codex stopped and enforced the process anyway. That felt slower, but it kept the build reviewable and prevented a huge invisible change set.

### Deferring manual verification is okay only when the path is not reachable yet
Several phases produced services or overlays that could compile but were not yet reachable through the app. It was better to record "manual verification deferred until wired" than to pretend a service-level build proved an interactive behavior. Once the controller connected the full flow, the golden path was actually tested.

### Coordinate contracts must be checked again at integration time
`CaptureRegion` was defined as physical pixels relative to the virtual-screen origin. That was correct for region selection, but integration exposed that `ScreenCaptureService` and `RsvpPresenter` needed to add the virtual-screen origin back before calling OS APIs or positioning windows. The contract was good; the important step was re-reading it when the modules finally met.

### Windows desktop settings can affect global namespace resolution
Enabling `<UseWindowsForms>true</UseWindowsForms>` for GDI capture pulled WinForms types into scope and created ambiguous names like `Application`, `Point`, `MouseEventArgs`, `KeyEventArgs`, and `Brushes`. The fix was explicit WPF qualification plus `<DisableImplicitNamespaceImports>true</DisableImplicitNamespaceImports>`. In mixed WPF/WinForms projects, assume namespace collisions are part of the cost.

### Build output can verify deployment assumptions before runtime wiring
Before OCR was reachable through the app, checking the build output confirmed `Tesseract.dll`, native x64 Tesseract/Leptonica DLLs, and `tessdata/eng.traineddata` were actually copied beside the executable. That caught the packaging path early without needing to force a throwaway OCR harness into the repo.

### What didn't go as planned

- **NuGet and GitHub network calls needed explicit escalation.** Package install and tessdata download initially failed inside the sandbox. The fix was to rerun the exact commands with approval, not work around the approval path.
- **The first solution build failed with no diagnostics.** Project-level restore/build gave useful output, and single-process `dotnet build -m:1` avoided transient file locks from parallel WPF builds.
- **WPF file edits hit BOM/context mismatches.** Some early patches failed because the XAML files had BOM/line-ending details. Smaller edits or replacing tiny scaffold files was faster than fighting brittle patch context.
- **Interactive verification could not be automated from the shell.** Region dragging, hotkey behavior, OCR quality, and overlay placement needed the user to run the app. The docs now distinguish build/regression checks from manual golden-path checks.
