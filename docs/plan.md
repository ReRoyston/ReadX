# ReadX v1 — Plan

## Context
ReadX is a ShareX-style RSVP (Rapid Serial Visual Presentation) utility. The user invokes a global hotkey, drags a rectangle around text on screen, and an overlay above that region plays the extracted text back word-by-word at a configurable WPM. Long-term it will grow history, persistent settings, rebindable hotkeys, and more — but **v1 ships only the core loop** plus a minimal main window. Branch: `v1`.

## Decisions confirmed by the user
- **Language / runtime:** C# on .NET 8 (LTS).
- **UI framework:** WPF.
- **Theme library:** WPF UI (`Wpf.Ui`, wpfui.lepo.co) for Fluent / Windows 11 styling.
- **OCR engine:** Tesseract via the `Tesseract` NuGet package (charlesw); bundles `tessdata/eng.traineddata`, ~22 MB.
- **v1 scope:** core loop + minimal main window. **No** persistent history, **no** settings persistence, **no** rebindable hotkeys yet.
- **Design pass:** ASCII / text mockups of all 3 surfaces produced and approved **before** any code is written.

## Defaults locked
- Default hotkey: `Ctrl + Shift + R`.
- Default WPM: `300` (slider clamp: 100–800).
- OCR language: English only (`eng.traineddata`).
- RSVP overlay placement: above the captured region (fallback below if no room).
- Playback controls: `Space` toggle pause, `Esc` cancel.
- Multi-monitor: region overlay spans the virtual screen across all monitors.

## v1 surfaces
1. **Main window** — small ShareX-style shell. v1 contents: "Capture" button, current hotkey label, WPM slider, last-OCR preview, "Replay last" button. Built with WPF UI controls in dark mode by default.
2. **Region-select overlay** — borderless, topmost, transparent window covering the virtual screen. Crosshair cursor; user drags a rectangle; `Esc` cancels.
3. **RSVP overlay** — borderless, topmost, frameless window pinned above the captured region. Single large word in centre, plus a thin progress bar. (No ORP / focus-letter cue in v1.)

## Architecture (modular, per CLAUDE.md)
Single WPF project for v1. Files separated by concern:

```
ReadX/
├── ReadX.sln
├── src/
│   ├── ReadX.csproj
│   ├── App.xaml(.cs)                  # composition root
│   ├── AppController.cs               # state machine / orchestrator
│   ├── app.manifest                   # PerMonitorV2 (added in Phase 5)
│   ├── Views/
│   │   ├── MainWindow.xaml(.cs)
│   │   ├── RegionSelectOverlay.xaml(.cs)
│   │   └── RsvpOverlay.xaml(.cs)
│   ├── Services/
│   │   ├── IHotkeyService.cs / HotkeyService.cs / HotkeyMapping.cs
│   │   ├── IScreenCaptureService.cs / ScreenCaptureService.cs
│   │   ├── IOcrService.cs / OcrService.cs
│   │   ├── IRegionSelector.cs / RegionSelector.cs
│   │   ├── IRsvpPresenter.cs / RsvpPresenter.cs
│   │   ├── ITicker.cs / DispatcherTicker.cs
│   │   └── RsvpPlayer.cs
│   ├── Models/
│   │   ├── AppState.cs                # enum
│   │   ├── CaptureRegion.cs
│   │   └── RsvpSession.cs
│   └── Tokenization/
│       └── WordSplitter.cs
├── tessdata/
│   └── eng.traineddata                # via Git LFS
├── tests/
│   └── ReadX.Tests/                   # xUnit (net8.0-windows; refs WPF project)
└── docs/
```

Key separations:
- **Pure logic** (`Tokenization/`, `Models/`, `RsvpPlayer` minus its ticker) is testable without WPF.
- **OS-touching code** (`HotkeyService`, `ScreenCaptureService`, `OcrService`, `RegionSelector`, `RsvpPresenter`) is in `Services/` behind small interfaces so it can be stubbed.
- **Views** hold no logic beyond DataContext binding and trivial event forwarding.
- **`AppController`** holds the state machine; `App.xaml.cs` stays a thin composition root.

## Implementation phases
Per CLAUDE.md "one step at a time", we stop after each phase, summarise, and wait for "continue". Package installs **require explicit approval** when reached.

1. **Design** ✅ — ASCII/text mockups locked, defaults locked, docs updated.
2. **Scaffold** — solution, WPF project, xUnit project, NuGet packages (approval gate), tessdata, csproj wiring. *Currently paused mid-phase — see detailed section below.*
3. **Tokenization + RsvpPlayer** — pure-logic word splitter and WPM-driven word stream. Unit-tested via `ITicker` seam.
4. **RsvpOverlay window** — renders one word at a time from `RsvpPlayer`. Visual polish per design.
5. **RegionSelectOverlay window** — full virtual-screen transparent window with drag-rect and `Esc` cancel. Adds `app.manifest` (PerMonitorV2).
6. **ScreenCaptureService** — captures the chosen rect as `Bitmap`, DPI-aware.
7. **OcrService** — Tesseract wrapper that turns the captured `Bitmap` into a string.
8. **HotkeyService** — Win32 `RegisterHotKey` registration tied to the app message loop.
9. **MainWindow** — minimal shell wired to manual capture, WPM slider, last-text preview, replay.
10. **End-to-end wiring** — `AppController` orchestrator: hotkey → region select → capture → OCR → RSVP; manual golden-path test.
11. **Release** — push `v1`, open PR, merge, pull, tag `v1.0.0`. Auto-update `docs/changelog.md` and `docs/project_status.md` along the way.

## Verification (end-to-end)
- **Unit tests:** `dotnet test` passes for `WordSplitter` and `RsvpPlayer` (state machine via FakeTicker).
- **Build:** `dotnet build -c Release` succeeds with no warnings introduced by us.
- **Golden path (manual):** launch app → press hotkey → drag a rectangle around a paragraph → RSVP overlay plays the words above the region at the chosen WPM → `Esc` cancels, `Space` pauses → main window's "Replay last" replays the same text.
- **Multi-monitor:** repeat the golden path on a secondary monitor.
- **DPI:** repeat at 125% / 150% display scaling — capture rect must align with what the user dragged (PMv2 canary).

## Out of scope for v1 (deferred)
History list · settings persistence · hotkey rebinding UI · multi-language OCR · cloud OCR · auto-update · installer · system tray icon · pause/resume from main window · ORP / focus-letter cue · OCR preprocessing · WGC capture mechanism · smarter hyphenated-wrap heuristic. All planned for later releases.

---

## Phase 1 — Design ✅ DONE
- Layout B (single pane) locked.
- ORP / focus letter dropped for v1.
- Defaults locked: `Ctrl+Shift+R`, 300 WPM, `Space`/`Esc`, English only.
- `docs/architecture.md`, `docs/changelog.md`, `docs/project_status.md` updated.

---

## Phase 2 — Scaffold (RESUME FROM CURRENT STATE)

### Already done (from prior session)
- .NET 8.0.420 SDK installed at `C:\Program Files\dotnet\`; current PowerShell sees `dotnet` on PATH. Older bash sessions may still need a terminal restart.
- Git LFS 3.7.1 confirmed; `git lfs install` run for the repo; `tessdata/*.traineddata` tracked in `.gitattributes`.
- `ReadX.sln` created at repo root.
- `src/ReadX.csproj` created (WPF, `net8.0-windows`).
- `tests/ReadX.Tests/ReadX.Tests.csproj` created — target bumped to `net8.0-windows` so the test project can reference the WPF project.
- Both projects added to `ReadX.sln`; test project references `src/ReadX.csproj`.
- `MainWindow.xaml` and `MainWindow.xaml.cs` physically moved from `src/` to `src/Views/`.
- `.gitignore` already ignores `bin/`, `obj/`, and `*.user`.

### Locked decisions (deltas from the original Phase 2 plan)
- **Test target framework:** `net8.0-windows`, NOT `net8.0`. The test project references the WPF project, which forces a Windows-platform target. Original plan was wrong.
- **Tesseract package name:** the NuGet is `Tesseract` (charlesw). The architecture doc previously said "Tesseract.NET" — corrected.
- **`app.manifest` (PerMonitorV2):** moved out of Phase 2; lands in Phase 5 where DPI awareness becomes load-bearing for region select.

### Known issues to fix in this phase (build-blocking)
1. **C# namespace mismatch.** `src/Views/MainWindow.xaml.cs` declares `namespace ReadX;` — must become `namespace ReadX.Views;`.
2. **XAML `x:Class` mismatch.** `src/Views/MainWindow.xaml`: `x:Class="ReadX.MainWindow"` → `"ReadX.Views.MainWindow"`.
3. **XAML `xmlns:local` mismatch.** Same file: `xmlns:local="clr-namespace:ReadX"` → `"clr-namespace:ReadX.Views"`.
4. **`StartupUri`.** `src/App.xaml`: `StartupUri="MainWindow.xaml"` → `"Views/MainWindow.xaml"`.
5. **Window title.** `MainWindow.xaml`: `Title="MainWindow"` → `Title="ReadX"`.

### Remaining work (in order)
1. Fix the 5 namespace/path/title issues above.
2. Rename `tests/ReadX.Tests/UnitTest1.cs` → `SmokeTest.cs` (and class name).
3. **APPROVAL GATE — NuGet packages.**
   - `dotnet add src/ReadX.csproj package Wpf.Ui`
   - `dotnet add src/ReadX.csproj package Tesseract`
   - I'll report the exact stable versions chosen before running.
4. Download `tessdata/eng.traineddata` from a pinned official `tesseract-ocr/tessdata` commit URL, not `raw/main`; record the commit URL and SHA256 checksum in this plan when the file is fetched. **Done:** `https://raw.githubusercontent.com/tesseract-ocr/tessdata/ced78752cc61322fb554c280d13360b35b8684e4/eng.traineddata`; SHA256 `DAA0C97D651C19FBA3B25E81317CD697E9908C8208090C94C3905381C23FC047`; size `23466654` bytes.
5. Add `<Content>` block to `src/ReadX.csproj`:
   ```xml
   <ItemGroup>
     <Content Include="..\tessdata\*.traineddata">
       <Link>tessdata\%(Filename)%(Extension)</Link>
       <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
     </Content>
   </ItemGroup>
   ```
6. Verify (all four must pass): **Done for scaffold smoke verification.**
   - `dotnet build` — 0 errors, 0 new warnings.
   - `dotnet test` — smoke test green.
   - `src/bin/Debug/net8.0-windows/tessdata/eng.traineddata` exists.
   - `dotnet run --project src/ReadX.csproj --no-build` stays running after startup smoke test.
7. Update `docs/changelog.md` and `docs/project_status.md`. **Done.**
8. Commit on `v1` (no push — release-time only). **Next.**

### Approval gates remaining in Phase 2
- NuGet package install (`Wpf.Ui`, `Tesseract`) — explicit user approval before `dotnet add`.

---

## Phase 3 — Tokenization + RsvpPlayer (DETAILED)

### Locked decisions
- **WordSplitter:** Option A — always rejoin hyphenated line-wrap fragments (e.g. `recom-\nmend` → `recommend`). **v2 review:** real hyphenated terms like `state-of-the-art` get rejoined too; revisit with a smarter heuristic post-v1.
- **WPM clamp range:** 100–800. Slider enforces it; `RsvpPlayer.Load` also clamps defensively.
- **ITicker seam:** `RsvpPlayer` takes an `ITicker` dependency. Production = `DispatcherTicker` (wraps `DispatcherTimer`); tests = `FakeTicker` (manually pumped, no real time). Makes the state machine fully unit-testable without WPF or sleeping.

### Files this phase creates
- `src/Tokenization/WordSplitter.cs`
- `src/Services/ITicker.cs`
- `src/Services/DispatcherTicker.cs`
- `src/Services/RsvpPlayer.cs`
- `src/Models/RsvpSession.cs`
- `tests/ReadX.Tests/WordSplitterTests.cs`
- `tests/ReadX.Tests/RsvpPlayerTests.cs`
- `tests/ReadX.Tests/Fakes/FakeTicker.cs`

### Contracts (sketch)
```csharp
namespace ReadX.Tokenization;

public static class WordSplitter
{
    // Splits raw OCR text into word tokens for RSVP playback.
    // - Rejoins hyphenated line-wrap fragments (Option A — v2 review).
    // - Splits on whitespace.
    // - Strips empty tokens.
    // - Preserves intra-word punctuation ("don't").
    public static IReadOnlyList<string> Split(string raw);
}
```
```csharp
namespace ReadX.Services;

public interface ITicker
{
    TimeSpan Interval { get; set; }
    event Action? Tick;
    void Start();
    void Stop();
}

public sealed class DispatcherTicker : ITicker { /* wraps DispatcherTimer */ }
```
```csharp
namespace ReadX.Services;

public enum PlayerState { Idle, Playing, Paused, Finished }

public sealed class RsvpPlayer
{
    public RsvpPlayer(ITicker ticker);

    public void Load(IReadOnlyList<string> words, int wpm);
    public void Start();
    public void Pause();   // toggles Paused <-> Playing
    public void Cancel();

    public PlayerState State { get; }
    public int Index { get; }
    public int Total { get; }
    public string? CurrentWord { get; }

    public event Action<string>? WordChanged;
    public event Action? Completed;
}
```

### Tests (xUnit, brief — main functionality only)
- **WordSplitter:**
  - `"hello world"` → `["hello","world"]`
  - `"recom-\nmend reading"` → `["recommend","reading"]`
  - `"don't stop"` → `["don't","stop"]`
  - empty / whitespace-only → empty list
- **RsvpPlayer (using FakeTicker):**
  - `Load + Start` advances one word per tick; `WordChanged` fires per advance.
  - `Pause` halts ticks; `Pause` again resumes.
  - End of word list fires `Completed`; `State == Finished`.
  - Tick interval = `60000 / wpm` ms (sanity-checked at WPM = 100, 300, 800).

### Verification gate to leave Phase 3
- `dotnet test` green. **Done:** `dotnet test -m:1` passes with 13 tests.
- (No manual UI test yet — Phase 4 wires this to a window.)

---

## Phase 4 — RsvpOverlay window
Per the high-level summary above. No further detail locked yet — design in detail at the start of the phase if needed. Notable constraint: borderless topmost frameless window pinned above the captured region; shows one large word + thin progress bar; no ORP.

### Ownership decision
- `RsvpPlayer` owns playback timing and state only.
- `RsvpOverlay` owns WPF rendering only.
- `RsvpPresenter` owns the RSVP window lifecycle: create/show/position/close the overlay, bind player events to the overlay, and route overlay keyboard input (`Space` pause/resume, `Esc` cancel) back to playback.
- `AppController` depends on `IRsvpPresenter`, not directly on `RsvpOverlay`, so orchestration stays independent of view lifecycle details.

### Files this phase creates
- `src/Views/RsvpOverlay.xaml` / `.cs`
- `src/Services/IRsvpPresenter.cs`
- `src/Services/RsvpPresenter.cs`
- `src/Models/CaptureRegion.cs` (moved forward from Phase 5 because the presenter contract depends on it)

### Contracts (sketch)
```csharp
namespace ReadX.Services;

public interface IRsvpPresenter
{
    Task PlayAsync(RsvpPlayer player, CaptureRegion region);
    void Close();
}
```

### Verification gate to leave Phase 4
- `dotnet build -m:1` passes with zero warnings. **Done.**
- `dotnet test -m:1 --no-build` passes with 13 tests. **Done.**

---

## Phase 5 — RegionSelectOverlay window (DETAILED)

### Locked decisions
- **Two coordinate systems on purpose:**
  - **Visuals** (the dragged rectangle preview) live in WPF DIPs.
  - **Final commit** captures both mouse-down and mouse-up physical cursor positions via `GetCursorPos`, then calculates the committed `CaptureRegion` from those two physical points relative to the virtual-screen origin (which can be negative on multi-monitor setups).
  - Avoids WPF transform pitfalls in mixed-DPI multi-monitor scenarios.
- **DPI awareness:** Per-Monitor V2 declared via `src/app.manifest`. Without this, capture coords drift on non-100% scaling.
- **Cancel:** `Esc` closes the overlay and returns `null` from `SelectAsync`.
- **Click without drag:** treated as cancel (returns `null`) — no zero-size capture.
- **Multi-monitor:** overlay window spans the virtual screen rectangle (`SystemParameters.VirtualScreen{Left,Top,Width,Height}`), topmost.

### Files this phase creates
- `src/app.manifest` (PerMonitorV2 declaration) **Done.**
- `src/Views/RegionSelectOverlay.xaml` / `.cs` **Done.**
- `src/Services/IRegionSelector.cs` **Done.**
- `src/Services/RegionSelector.cs` (shows the overlay window, returns the chosen region) **Done.**
- `src/Models/CaptureRegion.cs` **Done in Phase 4 because the presenter contract needed it.**

### Contracts (sketch)
```csharp
namespace ReadX.Models;

// Physical pixels relative to virtual-screen origin (origin can be negative on multi-monitor).
public sealed record CaptureRegion(int X, int Y, int Width, int Height);
```
```csharp
namespace ReadX.Services;

public interface IRegionSelector
{
    // Shows the region-select overlay; returns the chosen region or null if cancelled / zero-size.
    Task<CaptureRegion?> SelectAsync();
}
```

### app.manifest essentials
```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <application xmlns="urn:schemas-microsoft-com:asm.v3">
    <windowsSettings>
      <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
    </windowsSettings>
  </application>
</assembly>
```
And in `src/ReadX.csproj`: `<ApplicationManifest>app.manifest</ApplicationManifest>`.

### Verification gate to leave Phase 5
- `dotnet build -m:1` passes with zero warnings. **Done.**
- `dotnet test -m:1 --no-build` passes with 13 tests. **Done.**
- Manual drag-path verification is deferred until the selector is reachable through the app flow.

---

## Phase 6 — ScreenCaptureService (DETAILED)

### Locked decisions
- **Capture mechanism:** GDI `Graphics.CopyFromScreen` from the virtual-screen origin into a `Bitmap` sized to the `CaptureRegion`. Simple, dependency-free, works on Win10+. **v2 lever:** Windows Graphics Capture (WGC) is faster and cleaner for hardware-accelerated content; defer until a real complaint.
- **Coordinates:** input `CaptureRegion` is already physical pixels at virtual-screen origin (Phase 5's contract). No DPI math needed inside this service.
- **Output:** `System.Drawing.Bitmap` (consumed by `OcrService` in Phase 7).
- **Windows-only dependency path:** v1 is Windows-only WPF. Phase 6 explicitly enables the required Windows desktop drawing support in `src/ReadX.csproj` (preferred: `<UseWindowsForms>true</UseWindowsForms>` for access to `System.Drawing`/GDI types) before implementing capture. If that project setting proves insufficient during build, stop and ask before adding any extra package.

### Files this phase creates
- `src/Services/IScreenCaptureService.cs`
- `src/Services/ScreenCaptureService.cs`

### Project file change
```xml
<UseWindowsForms>true</UseWindowsForms>
```
Implemented with `<DisableImplicitNamespaceImports>true</DisableImplicitNamespaceImports>` to avoid WinForms/WPF implicit namespace ambiguity.

### Contracts (sketch)
```csharp
namespace ReadX.Services;

public interface IScreenCaptureService
{
    Bitmap Capture(CaptureRegion region);
}
```

### Verification gate to leave Phase 6
- `dotnet build -m:1` passes with zero warnings. **Done.**
- `dotnet test -m:1 --no-build` passes with 13 tests. **Done.**
- Manual: capture a known region (e.g. a Notepad window), save bitmap to temp file, eyeball it → matches expectation at 100%, 125%, 150% scaling on primary and secondary monitors. **Deferred until capture is reachable through the app flow.**
- No unit tests (pure I/O against a screen — covered by manual golden path).

---

## Phase 7 — OcrService (DETAILED)

### Locked decisions
- **Scope:** computer-written text only (browser, Notepad, IDE screenshots). **Not** handwriting, **not** photographed text. Lowers the bar for image preprocessing.
- **No preprocessing in v1.** Bitmap goes straight to Tesseract. No grayscale, no thresholding, no upscaling. v2 lever if real screen text accuracy disappoints.
- **No extra NuGet packages.** Just `Tesseract` (charlesw).
- **Tesseract config:** `EngineMode.Default` (LSTM), `PageSegMode.Auto`.
- **Language:** `eng` only.
- **tessdata path:** loaded from the `tessdata/` directory next to the executable (Phase 2's `<Content>` block guarantees it's there).

### Files this phase creates
- `src/Services/IOcrService.cs` **Done.**
- `src/Services/OcrService.cs` **Done.**

### Contracts (sketch)
```csharp
namespace ReadX.Services;

public interface IOcrService : IDisposable
{
    string Recognise(Bitmap image);
}

public sealed class OcrService : IOcrService
{
    // tessdataPath = folder containing eng.traineddata.
    public OcrService(string tessdataPath, string language = "eng");
    public string Recognise(Bitmap image);
    public void Dispose();
}
```

### Failure modes
- **Missing `eng.traineddata`** (file deleted, copy-to-output broken, etc.):
  - Constructor throws.
  - `App.OnStartup` catches and passes `IOcrService?` as `null` to `AppController`; `OcrAvailable = false`, MainWindow paints OCR status red, and Capture is disabled.

### Verification gate to leave Phase 7
- `dotnet build -m:1` passes with zero warnings. **Done.**
- `dotnet test -m:1 --no-build` passes with 13 tests. **Done.**
- Manual: screenshot a known paragraph in Notepad, run through `OcrService`, eyeball the string. Should be near-perfect for clean computer-written text. **Deferred until OCR is reachable through the app flow.**
- No unit tests (pure I/O against an external library — covered by manual).

---

## Phase 8 — HotkeyService (DETAILED)

### Locked decisions
- **Mechanism:** Win32 `RegisterHotKey` via P/Invoke. WPF message-loop hook through `HwndSource.AddHook` to catch `WM_HOTKEY` (`0x0312`).
- **HWND ownership:** v1 registers the hotkey against the main window HWND from `WindowInteropHelper`. `HotkeyService` does not create a hidden message window.
- **Registration timing:** `TryRegister` is called only after `MainWindow.SourceInitialized`, when the WPF HWND exists and `HwndSource.AddHook` can be attached safely.
- **Default hotkey:** `Ctrl+Shift+R` (locked v1 default; not rebindable).
- **Re-entrant guard:** `SetBusy(bool busy)` lives **inside** `HotkeyService`. While busy, the hook drops `WM_HOTKEY` messages. Keeps the orchestrator simpler — it just calls `SetBusy(true)` on entry and `SetBusy(false)` on exit/failure.
- **Conflict handling:** if `RegisterHotKey` fails (e.g. another app holds the same combo), `TryRegister` returns `false`. App keeps running in degraded mode — manual Capture button still works; main window paints the hotkey label red.
- **Cleanup:** `Unregister` on app shutdown, plus removing the hook.

### Files this phase creates
- `src/Services/IHotkeyService.cs` **Done.**
- `src/Services/HotkeyService.cs` **Done.**
- `src/Services/HotkeyMapping.cs` (small helper — `ModifierKeys` + `Key` → Win32 modifier + virtual-key int) **Done.**

### Contracts (sketch)
```csharp
namespace ReadX.Services;

public interface IHotkeyService : IDisposable
{
    bool TryRegister(IntPtr hwnd, ModifierKeys mods, Key key, Action callback);
    void Unregister();
    void SetBusy(bool busy);   // re-entrant guard; drops hotkey while a capture/playback is in-flight
}
```

### Verification gate to leave Phase 8
- `dotnet build -m:1` passes with zero warnings. **Done.**
- `dotnet test -m:1 --no-build` passes with 13 tests. **Done.**
- Manual: launch app → hotkey registration happens after `MainWindow.SourceInitialized`; no startup race or missing HWND. **Deferred until app wiring.**
- Manual: press `Ctrl+Shift+R` → callback fires. **Deferred until app wiring.**
- Manual: press it again while busy → silently ignored. **Deferred until app wiring.**
- Manual: launch with another app already holding `Ctrl+Shift+R` (e.g. AutoHotkey) → app starts, hotkey label red, manual capture still works. **Deferred until app wiring.**
- Cleanup: close app → hotkey released (reproducible by registering same combo with another tool afterwards). **Deferred until app wiring.**

---

## Phase 9 — MainWindow
Per the high-level summary. Wires Capture button, hotkey label (green/red), WPM slider (clamped 100–800), last-OCR preview, Replay button. WPF UI (`Wpf.Ui`) styling lands here. No further detail locked yet — design in detail at the start of the phase.

### Verification gate to leave Phase 9
- `dotnet build -m:1` passes with zero warnings. **Done.**
- `dotnet test -m:1 --no-build` passes with 13 tests. **Done.**
- `dotnet run --project src\ReadX.csproj --no-build` launch smoke stays running after startup. **Done.**

---

## Phase 10 — End-to-end wiring (DETAILED)

### Locked decisions
- **Orchestrator:** dedicated `AppController` class (NOT inlined in `App.xaml.cs`). `App.xaml.cs` stays a thin composition root; `AppController` holds the state machine, independently testable in v2 if/when needed.
- **State machine:**
  ```
  Idle → Selecting → Capturing → Recognising → Playing → Idle
                                    ↓ (Esc any time)
                                  Idle
                                    ↓ (failure any step)
                                  Idle (with status message)
  ```
- **LastSession:** orchestrator caches the last `(words, region)` after a successful OCR pass. WPM is read live from the slider on every Replay (so changing WPM mid-session affects the next replay).
- **No state-machine unit tests in v1.** Manual golden path covers it. Add tests in v2 if the state machine grows.

### Files this phase creates
- `src/AppController.cs` **Done.**
- `src/Models/AppState.cs` (enum) **Done.**
- `src/Models/RsvpSession.cs` (if not already created in Phase 3) **Updated to cache words, region, and OCR text.**
- `src/Services/IRsvpPresenter.cs` / `src/Services/RsvpPresenter.cs` (if not already created in Phase 4) **Done in Phase 4.**

### Contracts (sketch)
```csharp
namespace ReadX.Models;

public enum AppState { Idle, Selecting, Capturing, Recognising, Playing }
```
```csharp
namespace ReadX;

public sealed class AppController
{
    public AppController(
        IHotkeyService hotkey,
        IRegionSelector selector,
        IScreenCaptureService capture,
        IOcrService? ocr,
        RsvpPlayer player,
        IRsvpPresenter presenter);

    public AppState State { get; }
    public string? Status { get; }              // last user-visible message ("OCR failed.", etc.)
    public RsvpSession? LastSession { get; }
    public bool HotkeyAvailable { get; }
    public bool OcrAvailable { get; }
    public int Wpm { get; set; }                // bound from MainWindow slider

    public event Action? StateChanged;

    public void SetHotkeyAvailable(bool available);
    public Task StartCaptureAsync();            // hotkey or button entry point
    public Task ReplayLastAsync();
}
```

### Composition root sequence (`App.OnStartup`)
1. Build `IScreenCaptureService` (no I/O).
2. Build `IOcrService` — try/catch for missing tessdata. On fail, keep `IOcrService?` as `null`; `AppController.OcrAvailable = false`.
3. Build `RsvpPlayer` with `DispatcherTicker`.
4. Build `IRsvpPresenter` for RSVP overlay lifecycle and keyboard routing.
5. Build `IHotkeyService` but do not register it yet.
6. Build `AppController`, wire to `MainWindow`, and show window.
7. After `MainWindow.SourceInitialized`, call `TryRegister(hwnd, ModifierKeys.Control | ModifierKeys.Shift, Key.R, ...)` against the main window HWND. Pass the result to `AppController.SetHotkeyAvailable(...)`; if registration fails, manual Capture remains available.

### Failure handling table (8 modes)
| # | Failure | Behaviour |
|---|---------|-----------|
| 1 | Tessdata missing at startup | `OcrAvailable = false`; main window OCR label red; Capture button disabled |
| 2 | Hotkey conflict at startup | `HotkeyAvailable = false`; main window hotkey label red; Capture button still works |
| 3 | User cancels region select (`Esc`) | Return to Idle; no status message |
| 4 | Click without drag (zero-size) | Return to Idle; no status message |
| 5 | Screen capture throws | Return to Idle; status `"Capture failed."` |
| 6 | OCR returns empty string | Return to Idle; status `"No text recognised."` |
| 7 | OCR throws | Return to Idle; status `"OCR failed."` |
| 8 | RSVP overlay fails to position (impossible-rect) | Centre overlay on primary monitor; status `"Overlay fallback."` |

### Verification gate to leave Phase 10 (10-case manual golden path)
Startup verification:
- `dotnet build -m:1` passes with zero warnings. **Done.**
- `dotnet test -m:1 --no-build` passes with 13 tests. **Done.**
- `dotnet run --project src\ReadX.csproj --no-build` wired startup smoke stays running after startup. **Done.**

Manual golden path:
1. Launch app → main window appears, OCR + hotkey labels green. **User-confirmed.**
2. Press `Ctrl+Shift+R` → region overlay appears. **User-confirmed.**
3. Drag rectangle around a paragraph in Notepad → RSVP overlay plays words above the region at 300 WPM. **User-confirmed.**
4. `Space` pauses, `Space` resumes, `Esc` cancels. **User-confirmed.**
5. `Replay last` plays the same words again. **User-confirmed.**
6. Change WPM slider to 500, `Replay last` → faster playback.
7. Repeat steps 2–3 on a secondary monitor.
8. Repeat steps 2–3 at 125% display scaling — capture aligns with drag (PMv2 canary).
9. Repeat steps 2–3 at 150% display scaling on the secondary monitor (mixed-DPI canary).
10. Quit and relaunch with `Ctrl+Shift+R` already held by another tool → hotkey label red, manual Capture button still works.

### Approval gates remaining
- None — Phase 10 is wiring only, no new packages.

---

## Phase 11 — Release
Push `v1`, open PR, merge via PR on GitHub, pull locally, tag `v1.0.0`. Per CLAUDE.md: never merge to main locally. Update `docs/changelog.md` with the release entry; bump `docs/project_status.md` to "v1 shipped".
