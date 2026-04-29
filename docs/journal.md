# Journal

A running log of my learnings from building with AI.

---

## 2026-04-28

### V2 planning should follow daily workflows before visual polish
The first v2 fork in the road was whether to design UI first or build features first. The better answer was a foundation-first daily-use release: settings, history, import, hotkeys, cleanup, replay, and ORP output define the real workflows, then the UI structure follows those workflows. This avoids both a pretty shell with unstable requirements and feature work that gets bolted onto the v1 window.

### Browser companions are optional; decisions still need text fallbacks
The visual brainstorming companion produced server metadata but was not reachable from the user's browser in this environment. The planning did not block on it: ASCII wireframes were enough to identify that top tabs felt too portrait-like and left-side tabs fit the desktop utility better. Future visual tools should be treated as accelerators, not dependencies.

## 2026-04-27

### Agent workflow tooling belongs outside the product repo unless it changes the product
Superpowers was installed as Codex-level workflow tooling rather than vendored into ReadX. The app should not gain repository files, runtime dependencies, or architecture changes just because the development workflow improved. Record the setup in project docs, but keep product code focused on product behavior.

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
