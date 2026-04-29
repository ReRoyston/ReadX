# ReadX v2 Implementation Plan

> Status: historical implementation plan. Completed and released as v2.0.0; v2.1 is docs-only cleanup for release status.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the v2 daily-use release: persisted settings, raw-text history, quick import, configurable hotkeys, cleanup, restartable playback, classic ORP rendering, and a compact left-tab UI.

**Architecture:** Keep the approved .NET 8/WPF architecture. Add focused models and services for settings/history/text/ORP, route all capture/import/history replay through one text pipeline, keep workflow state in `AppController`, and expose UI behavior through events/state setters on `MainWindow`.

**Tech Stack:** C# / .NET 8, WPF, WPF UI, Tesseract, xUnit. No new NuGet packages are required for v2.

---

## File Structure

Create:

- `src/Models/AppSettings.cs` - persisted settings model and defaults.
- `src/Models/HotkeyBinding.cs` - serializable hotkey binding.
- `src/Models/HistoryItem.cs` - raw-text history record.
- `src/Models/HistorySource.cs` - `Capture` / `Import` source enum.
- `src/Services/ISettingsStore.cs` - settings persistence contract.
- `src/Services/JsonSettingsStore.cs` - JSON settings load/save with fallback.
- `src/Services/IHistoryStore.cs` - history persistence contract.
- `src/Services/JsonHistoryStore.cs` - JSON history load/save/retention.
- `src/Services/IAppDataPathProvider.cs` - app-data path seam.
- `src/Services/AppDataPathProvider.cs` - production app-data path provider.
- `src/Text/TextCleanupService.cs` - optional text cleanup.
- `src/Text/TextPipeline.cs` - raw text to playable words.
- `src/Rsvp/OrpWord.cs` - word plus focus-letter metadata.
- `src/Rsvp/OrpCalculator.cs` - classic ORP calculation.
- `tests/ReadX.Tests/SettingsStoreTests.cs`
- `tests/ReadX.Tests/HistoryStoreTests.cs`
- `tests/ReadX.Tests/TextCleanupServiceTests.cs`
- `tests/ReadX.Tests/TextPipelineTests.cs`
- `tests/ReadX.Tests/OrpCalculatorTests.cs`
- `tests/ReadX.Tests/HotkeyBindingTests.cs`
- `tests/ReadX.Tests/Fakes/InMemorySettingsStore.cs`
- `tests/ReadX.Tests/Fakes/InMemoryHistoryStore.cs`

Modify:

- `src/Models/RsvpSession.cs` - store raw text, words, optional region, and source.
- `src/Services/RsvpPlayer.cs` - add restart support.
- `src/Services/IRsvpPresenter.cs` / `src/Services/RsvpPresenter.cs` - support ORP word presentation and restart/cancel routing.
- `src/Views/RsvpOverlay.xaml` / `.cs` - render left segment, focus letter, right segment around stable anchor.
- `src/Services/IHotkeyService.cs` / `HotkeyService.cs` - support multiple configurable hotkey registrations.
- `src/Services/HotkeyMapping.cs` - add binding conversion/format helpers.
- `src/AppController.cs` - orchestrate capture/import/history replay/settings/hotkeys.
- `src/App.xaml.cs` - compose new services, load settings/history, save on exit, register hotkeys after source initialization.
- `src/Views/MainWindow.xaml` / `.cs` - replace single-pane layout with compact left tabs and new event/state API.
- `docs/changelog.md`
- `docs/project_status.md`
- `docs/architecture.md`
- `docs/journal.md` only if a notable lesson emerges during implementation.

---

### Task 1: Settings Model And JSON Store

**Files:**
- Create: `src/Models/HotkeyBinding.cs`
- Create: `src/Models/AppSettings.cs`
- Create: `src/Services/IAppDataPathProvider.cs`
- Create: `src/Services/AppDataPathProvider.cs`
- Create: `src/Services/ISettingsStore.cs`
- Create: `src/Services/JsonSettingsStore.cs`
- Test: `tests/ReadX.Tests/SettingsStoreTests.cs`
- Test: `tests/ReadX.Tests/HotkeyBindingTests.cs`

- [ ] **Step 1: Write failing tests for settings defaults and fallback**

```csharp
using System.Text.Json;
using ReadX.Models;
using ReadX.Services;

namespace ReadX.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ReadX.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task LoadAsync_WhenFileMissing_ReturnsDefaults()
    {
        var store = new JsonSettingsStore(new TestPathProvider(root));

        var settings = await store.LoadAsync();

        Assert.Equal(300, settings.DefaultWpm);
        Assert.True(settings.CleanupEnabled);
        Assert.Equal(20, settings.HistoryLimit);
        Assert.Equal("Ctrl+Shift+R", settings.CaptureHotkey.ToDisplayText());
    }

    [Fact]
    public async Task LoadAsync_WhenJsonInvalid_ReturnsDefaults()
    {
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "settings.json"), "{ broken json");
        var store = new JsonSettingsStore(new TestPathProvider(root));

        var settings = await store.LoadAsync();

        Assert.Equal(AppSettings.CreateDefault(), settings);
    }

    [Fact]
    public async Task SaveAsync_WritesReadableJsonAndLoadAsyncReadsIt()
    {
        var store = new JsonSettingsStore(new TestPathProvider(root));
        var settings = AppSettings.CreateDefault() with
        {
            DefaultWpm = 450,
            CleanupEnabled = false,
            HistoryLimit = 7
        };

        await store.SaveAsync(settings);
        var reloaded = await store.LoadAsync();

        Assert.Equal(450, reloaded.DefaultWpm);
        Assert.False(reloaded.CleanupEnabled);
        Assert.Equal(7, reloaded.HistoryLimit);
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class TestPathProvider(string path) : IAppDataPathProvider
    {
        public string ReadXDirectory => path;
    }
}
```

```csharp
using System.Windows.Input;
using ReadX.Models;

namespace ReadX.Tests;

public sealed class HotkeyBindingTests
{
    [Fact]
    public void ToDisplayText_IncludesModifiersAndKey()
    {
        var binding = new HotkeyBinding(ModifierKeys.Control | ModifierKeys.Shift, Key.R);

        Assert.Equal("Ctrl+Shift+R", binding.ToDisplayText());
    }

    [Fact]
    public void CreateDefault_ReturnsApprovedV2Bindings()
    {
        var settings = AppSettings.CreateDefault();

        Assert.Equal("Ctrl+Shift+R", settings.CaptureHotkey.ToDisplayText());
        Assert.Equal("Ctrl+Shift+E", settings.ReplayLastHotkey.ToDisplayText());
        Assert.Equal("Space", settings.PauseResumeHotkey.ToDisplayText());
        Assert.Equal("Esc", settings.CancelHotkey.ToDisplayText());
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests\ReadX.Tests\ReadX.Tests.csproj --filter "SettingsStoreTests|HotkeyBindingTests" -m:1`

Expected: FAIL because the new settings and hotkey binding types do not exist.

- [ ] **Step 3: Add settings and path provider implementation**

Create `src/Models/HotkeyBinding.cs`:

```csharp
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace ReadX.Models;

public sealed record HotkeyBinding(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ModifierKeys Modifiers,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] Key Key)
{
    public string ToDisplayText()
    {
        var parts = new List<string>();

        if (Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");

        parts.Add(Key == Key.Escape ? "Esc" : Key.ToString());
        return string.Join("+", parts);
    }
}
```

Create `src/Models/AppSettings.cs`:

```csharp
using System.Windows.Input;

namespace ReadX.Models;

public sealed record AppSettings
{
    public int DefaultWpm { get; init; } = 300;
    public bool CleanupEnabled { get; init; } = true;
    public int HistoryLimit { get; init; } = 20;
    public HotkeyBinding CaptureHotkey { get; init; } = new(ModifierKeys.Control | ModifierKeys.Shift, Key.R);
    public HotkeyBinding ReplayLastHotkey { get; init; } = new(ModifierKeys.Control | ModifierKeys.Shift, Key.E);
    public HotkeyBinding PauseResumeHotkey { get; init; } = new(ModifierKeys.None, Key.Space);
    public HotkeyBinding CancelHotkey { get; init; } = new(ModifierKeys.None, Key.Escape);
    public double WindowWidth { get; init; } = 860;
    public double WindowHeight { get; init; } = 560;
    public double WindowLeft { get; init; } = double.NaN;
    public double WindowTop { get; init; } = double.NaN;
    public bool WindowMaximized { get; init; }

    public static AppSettings CreateDefault() => new();

    public AppSettings Normalized() => this with
    {
        DefaultWpm = Math.Clamp(DefaultWpm, Services.RsvpPlayer.MinimumWpm, Services.RsvpPlayer.MaximumWpm),
        HistoryLimit = Math.Clamp(HistoryLimit, 1, 500),
        WindowWidth = WindowWidth < 680 ? 860 : WindowWidth,
        WindowHeight = WindowHeight < 480 ? 560 : WindowHeight
    };
}
```

Create `src/Services/IAppDataPathProvider.cs`:

```csharp
namespace ReadX.Services;

public interface IAppDataPathProvider
{
    string ReadXDirectory { get; }
}
```

Create `src/Services/AppDataPathProvider.cs`:

```csharp
namespace ReadX.Services;

public sealed class AppDataPathProvider : IAppDataPathProvider
{
    public string ReadXDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ReadX");
}
```

Create `src/Services/ISettingsStore.cs`:

```csharp
using ReadX.Models;

namespace ReadX.Services;

public interface ISettingsStore
{
    Task<AppSettings> LoadAsync();
    Task SaveAsync(AppSettings settings);
}
```

Create `src/Services/JsonSettingsStore.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using ReadX.Models;

namespace ReadX.Services;

public sealed class JsonSettingsStore(IAppDataPathProvider pathProvider) : ISettingsStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string settingsPath = Path.Combine(pathProvider.ReadXDirectory, "settings.json");

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(settingsPath))
        {
            return AppSettings.CreateDefault();
        }

        try
        {
            await using var stream = File.OpenRead(settingsPath);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, Options);
            return (settings ?? AppSettings.CreateDefault()).Normalized();
        }
        catch
        {
            return AppSettings.CreateDefault();
        }
    }

    public async Task SaveAsync(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        await using var stream = File.Create(settingsPath);
        await JsonSerializer.SerializeAsync(stream, settings.Normalized(), Options);
    }
}
```

- [ ] **Step 4: Run tests to verify settings pass**

Run: `dotnet test tests\ReadX.Tests\ReadX.Tests.csproj --filter "SettingsStoreTests|HotkeyBindingTests" -m:1`

Expected: PASS for the new settings tests.

- [ ] **Step 5: Commit**

```bash
git add src/Models/HotkeyBinding.cs src/Models/AppSettings.cs src/Services/IAppDataPathProvider.cs src/Services/AppDataPathProvider.cs src/Services/ISettingsStore.cs src/Services/JsonSettingsStore.cs tests/ReadX.Tests/SettingsStoreTests.cs tests/ReadX.Tests/HotkeyBindingTests.cs
git commit -m "add settings persistence"
```

### Task 2: Raw-Text History Store

**Files:**
- Create: `src/Models/HistorySource.cs`
- Create: `src/Models/HistoryItem.cs`
- Create: `src/Services/IHistoryStore.cs`
- Create: `src/Services/JsonHistoryStore.cs`
- Test: `tests/ReadX.Tests/HistoryStoreTests.cs`

- [ ] **Step 1: Write failing history tests**

```csharp
using ReadX.Models;
using ReadX.Services;

namespace ReadX.Tests;

public sealed class HistoryStoreTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ReadX.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task AddAsync_KeepsDuplicatesAndTrimsOldestByLimit()
    {
        var store = new JsonHistoryStore(new TestPathProvider(root));

        await store.AddAsync("same text", HistorySource.Import, limit: 2);
        await store.AddAsync("same text", HistorySource.Import, limit: 2);
        await store.AddAsync("new text", HistorySource.Capture, limit: 2);

        var items = await store.LoadAsync();

        Assert.Equal(2, items.Count);
        Assert.Equal(["new text", "same text"], items.Select(item => item.RawText));
        Assert.Equal([HistorySource.Capture, HistorySource.Import], items.Select(item => item.Source));
    }

    [Fact]
    public async Task AddAsync_DoesNotStoreWhitespaceOnlyText()
    {
        var store = new JsonHistoryStore(new TestPathProvider(root));

        await store.AddAsync(" \r\n ", HistorySource.Import, limit: 20);

        Assert.Empty(await store.LoadAsync());
    }

    [Fact]
    public async Task ApplyLimitAsync_TrimsExistingHistory()
    {
        var store = new JsonHistoryStore(new TestPathProvider(root));
        await store.AddAsync("one", HistorySource.Import, limit: 20);
        await store.AddAsync("two", HistorySource.Import, limit: 20);
        await store.AddAsync("three", HistorySource.Import, limit: 20);

        await store.ApplyLimitAsync(2);
        var items = await store.LoadAsync();

        Assert.Equal(["three", "two"], items.Select(item => item.RawText));
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class TestPathProvider(string path) : IAppDataPathProvider
    {
        public string ReadXDirectory => path;
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests\ReadX.Tests\ReadX.Tests.csproj --filter HistoryStoreTests -m:1`

Expected: FAIL because history types do not exist.

- [ ] **Step 3: Add history implementation**

Create `src/Models/HistorySource.cs`:

```csharp
namespace ReadX.Models;

public enum HistorySource
{
    Capture,
    Import
}
```

Create `src/Models/HistoryItem.cs`:

```csharp
namespace ReadX.Models;

public sealed record HistoryItem(Guid Id, DateTimeOffset CreatedAt, HistorySource Source, string RawText);
```

Create `src/Services/IHistoryStore.cs`:

```csharp
using ReadX.Models;

namespace ReadX.Services;

public interface IHistoryStore
{
    Task<IReadOnlyList<HistoryItem>> LoadAsync();
    Task AddAsync(string rawText, HistorySource source, int limit);
    Task ApplyLimitAsync(int limit);
}
```

Create `src/Services/JsonHistoryStore.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using ReadX.Models;

namespace ReadX.Services;

public sealed class JsonHistoryStore(IAppDataPathProvider pathProvider) : IHistoryStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string historyPath = Path.Combine(pathProvider.ReadXDirectory, "history.json");

    public async Task<IReadOnlyList<HistoryItem>> LoadAsync()
    {
        if (!File.Exists(historyPath))
        {
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(historyPath);
            var items = await JsonSerializer.DeserializeAsync<List<HistoryItem>>(stream, Options);
            return (items ?? [])
                .Where(item => !string.IsNullOrWhiteSpace(item.RawText))
                .OrderByDescending(item => item.CreatedAt)
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    public async Task AddAsync(string rawText, HistorySource source, int limit)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return;
        }

        var items = (await LoadAsync()).ToList();
        items.Insert(0, new HistoryItem(Guid.NewGuid(), DateTimeOffset.Now, source, rawText));
        await SaveAsync(Trim(items, limit));
    }

    public async Task ApplyLimitAsync(int limit)
    {
        var items = await LoadAsync();
        await SaveAsync(Trim(items, limit));
    }

    private async Task SaveAsync(IReadOnlyList<HistoryItem> items)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(historyPath)!);
        await using var stream = File.Create(historyPath);
        await JsonSerializer.SerializeAsync(stream, items, Options);
    }

    private static IReadOnlyList<HistoryItem> Trim(IEnumerable<HistoryItem> items, int limit)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 500);
        return items
            .OrderByDescending(item => item.CreatedAt)
            .Take(normalizedLimit)
            .ToArray();
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test tests\ReadX.Tests\ReadX.Tests.csproj --filter HistoryStoreTests -m:1`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Models/HistorySource.cs src/Models/HistoryItem.cs src/Services/IHistoryStore.cs src/Services/JsonHistoryStore.cs tests/ReadX.Tests/HistoryStoreTests.cs
git commit -m "add raw text history"
```

### Task 3: Text Cleanup And Shared Pipeline

**Files:**
- Create: `src/Text/TextCleanupService.cs`
- Create: `src/Text/TextPipeline.cs`
- Modify: `src/Tokenization/WordSplitter.cs`
- Test: `tests/ReadX.Tests/TextCleanupServiceTests.cs`
- Test: `tests/ReadX.Tests/TextPipelineTests.cs`

- [ ] **Step 1: Write failing cleanup and pipeline tests**

```csharp
using ReadX.Text;

namespace ReadX.Tests;

public sealed class TextCleanupServiceTests
{
    [Fact]
    public void Clean_RejoinsHyphenatedLineWrapAndCollapsesWhitespace()
    {
        var clean = TextCleanupService.Clean(" recom-\r\n mend   this\r\nsentence ");

        Assert.Equal("recommend this sentence", clean);
    }

    [Fact]
    public void Clean_PreservesParagraphBreaksAsSingleReadableBreak()
    {
        var clean = TextCleanupService.Clean("First paragraph.\r\n\r\nSecond paragraph.");

        Assert.Equal("First paragraph.\nSecond paragraph.", clean);
    }
}
```

```csharp
using ReadX.Text;

namespace ReadX.Tests;

public sealed class TextPipelineTests
{
    [Fact]
    public void BuildWords_WhenCleanupEnabled_UsesCleanedText()
    {
        var result = TextPipeline.BuildWords("recom-\nmend   this", cleanupEnabled: true);

        Assert.Equal("recommend this", result.ProcessedText);
        Assert.Equal(["recommend", "this"], result.Words);
    }

    [Fact]
    public void BuildWords_WhenCleanupDisabled_StillSplitsRawText()
    {
        var result = TextPipeline.BuildWords("one   two", cleanupEnabled: false);

        Assert.Equal("one   two", result.ProcessedText);
        Assert.Equal(["one", "two"], result.Words);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests\ReadX.Tests\ReadX.Tests.csproj --filter "TextCleanupServiceTests|TextPipelineTests" -m:1`

Expected: FAIL because `ReadX.Text` does not exist.

- [ ] **Step 3: Add cleanup and pipeline**

Create `src/Text/TextCleanupService.cs`:

```csharp
using System.Text.RegularExpressions;

namespace ReadX.Text;

public static partial class TextCleanupService
{
    public static string Clean(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var text = HyphenatedLineWrapRegex().Replace(raw, string.Empty);
        text = ParagraphBreakRegex().Replace(text, "\n");
        text = SingleLineBreakRegex().Replace(text, " ");
        text = HorizontalWhitespaceRegex().Replace(text, " ");
        text = SpaceAroundNewlineRegex().Replace(text, "\n");
        return text.Trim();
    }

    [GeneratedRegex(@"(?<=\p{L})-\s*[\r\n]+\s*(?=\p{L})")]
    private static partial Regex HyphenatedLineWrapRegex();

    [GeneratedRegex(@"(\r?\n\s*){2,}")]
    private static partial Regex ParagraphBreakRegex();

    [GeneratedRegex(@"\r?\n")]
    private static partial Regex SingleLineBreakRegex();

    [GeneratedRegex(@"[^\S\r\n]+")]
    private static partial Regex HorizontalWhitespaceRegex();

    [GeneratedRegex(@" *\n *")]
    private static partial Regex SpaceAroundNewlineRegex();
}
```

Create `src/Text/TextPipeline.cs`:

```csharp
using ReadX.Tokenization;

namespace ReadX.Text;

public sealed record TextPipelineResult(string RawText, string ProcessedText, IReadOnlyList<string> Words);

public static class TextPipeline
{
    public static TextPipelineResult BuildWords(string rawText, bool cleanupEnabled)
    {
        var processed = cleanupEnabled ? TextCleanupService.Clean(rawText) : rawText;
        var words = WordSplitter.Split(processed);
        return new TextPipelineResult(rawText, processed, words);
    }
}
```

Modify `src/Tokenization/WordSplitter.cs` to remove cleanup-like hyphen responsibility after the new cleanup service is in place:

```csharp
using System.Text.RegularExpressions;

namespace ReadX.Tokenization;

public static partial class WordSplitter
{
    public static IReadOnlyList<string> Split(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return WhitespaceRegex()
            .Split(raw.Trim())
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .ToArray();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
```

- [ ] **Step 4: Update existing word splitter test expectation**

Modify `tests/ReadX.Tests/WordSplitterTests.cs`: replace `Split_RejoinsHyphenatedLineWrapFragments` with:

```csharp
[Fact]
public void Split_TreatsLineWrappedHyphenAsTokenTextWhenCleanupIsSkipped()
{
    var words = WordSplitter.Split("recom-\nmend reading");

    Assert.Equal(["recom-", "mend", "reading"], words);
}
```

- [ ] **Step 5: Run tests**

Run: `dotnet test tests\ReadX.Tests\ReadX.Tests.csproj --filter "TextCleanupServiceTests|TextPipelineTests|WordSplitterTests" -m:1`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/Text/TextCleanupService.cs src/Text/TextPipeline.cs src/Tokenization/WordSplitter.cs tests/ReadX.Tests/TextCleanupServiceTests.cs tests/ReadX.Tests/TextPipelineTests.cs tests/ReadX.Tests/WordSplitterTests.cs
git commit -m "add text cleanup pipeline"
```

### Task 4: Restartable RSVP Playback

**Files:**
- Modify: `src/Services/RsvpPlayer.cs`
- Modify: `src/Models/RsvpSession.cs`
- Test: `tests/ReadX.Tests/RsvpPlayerTests.cs`

- [ ] **Step 1: Write failing restart test**

Add to `tests/ReadX.Tests/RsvpPlayerTests.cs`:

```csharp
[Fact]
public void Restart_ReturnsToFirstWordAndStartsPlayback()
{
    var ticker = new FakeTicker();
    var player = new RsvpPlayer(ticker);
    var seenWords = new List<string>();
    player.WordChanged += seenWords.Add;

    player.Load(["one", "two"], 300);
    player.Start();
    ticker.Pulse();
    ticker.Pulse();
    player.Restart();
    ticker.Pulse();

    Assert.Equal(PlayerState.Playing, player.State);
    Assert.Equal(0, player.Index);
    Assert.Equal(["one", "two", "one"], seenWords);
}
```

- [ ] **Step 2: Run test to verify failure**

Run: `dotnet test tests\ReadX.Tests\ReadX.Tests.csproj --filter Restart_ReturnsToFirstWordAndStartsPlayback -m:1`

Expected: FAIL because `Restart` does not exist.

- [ ] **Step 3: Implement restart and update session model**

Add to `src/Services/RsvpPlayer.cs`:

```csharp
public void Restart()
{
    if (words.Count == 0)
    {
        return;
    }

    ticker.Stop();
    Index = -1;
    State = PlayerState.Playing;
    ticker.Start();
}
```

Replace `src/Models/RsvpSession.cs`:

```csharp
namespace ReadX.Models;

public sealed record RsvpSession(
    IReadOnlyList<string> Words,
    CaptureRegion? Region,
    string RawText,
    string ProcessedText,
    HistorySource Source);
```

- [ ] **Step 4: Run playback tests**

Run: `dotnet test tests\ReadX.Tests\ReadX.Tests.csproj --filter RsvpPlayerTests -m:1`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Services/RsvpPlayer.cs src/Models/RsvpSession.cs tests/ReadX.Tests/RsvpPlayerTests.cs
git commit -m "add restartable playback"
```

### Task 5: ORP Calculation And Overlay Rendering

**Files:**
- Create: `src/Rsvp/OrpWord.cs`
- Create: `src/Rsvp/OrpCalculator.cs`
- Modify: `src/Views/RsvpOverlay.xaml`
- Modify: `src/Views/RsvpOverlay.xaml.cs`
- Modify: `src/Services/RsvpPresenter.cs`
- Test: `tests/ReadX.Tests/OrpCalculatorTests.cs`

- [ ] **Step 1: Write failing ORP tests**

```csharp
using ReadX.Rsvp;

namespace ReadX.Tests;

public sealed class OrpCalculatorTests
{
    [Theory]
    [InlineData("I", 0)]
    [InlineData("to", 0)]
    [InlineData("read", 1)]
    [InlineData("reading", 2)]
    [InlineData("recognition", 3)]
    [InlineData("internationalization", 4)]
    public void Create_UsesClassicApproximateFocusIndex(string word, int expectedIndex)
    {
        var result = OrpCalculator.Create(word);

        Assert.Equal(expectedIndex, result.FocusIndex);
        Assert.Equal(word[expectedIndex].ToString(), result.FocusLetter);
    }
}
```

- [ ] **Step 2: Run test to verify failure**

Run: `dotnet test tests\ReadX.Tests\ReadX.Tests.csproj --filter OrpCalculatorTests -m:1`

Expected: FAIL because `ReadX.Rsvp` does not exist.

- [ ] **Step 3: Add ORP model and calculator**

Create `src/Rsvp/OrpWord.cs`:

```csharp
namespace ReadX.Rsvp;

public sealed record OrpWord(string Left, string FocusLetter, string Right, int FocusIndex)
{
    public string Text => Left + FocusLetter + Right;
}
```

Create `src/Rsvp/OrpCalculator.cs`:

```csharp
namespace ReadX.Rsvp;

public static class OrpCalculator
{
    public static OrpWord Create(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return new OrpWord(string.Empty, string.Empty, string.Empty, 0);
        }

        var focusIndex = GetFocusIndex(word.Length);
        return new OrpWord(
            word[..focusIndex],
            word[focusIndex].ToString(),
            word[(focusIndex + 1)..],
            focusIndex);
    }

    private static int GetFocusIndex(int length) => length switch
    {
        <= 1 => 0,
        <= 5 => 1,
        <= 9 => 2,
        <= 13 => 3,
        _ => 4
    };
}
```

- [ ] **Step 4: Update overlay to render ORP segments**

Replace the central `TextBlock` in `src/Views/RsvpOverlay.xaml` with:

```xml
<Grid Grid.Row="0" HorizontalAlignment="Stretch" VerticalAlignment="Center">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="Auto" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>

    <TextBlock x:Name="LeftWordText"
               Grid.Column="0"
               HorizontalAlignment="Right"
               VerticalAlignment="Center"
               FontSize="48"
               FontWeight="SemiBold"
               TextAlignment="Right"
               TextTrimming="CharacterEllipsis" />

    <TextBlock x:Name="FocusLetterText"
               Grid.Column="1"
               HorizontalAlignment="Center"
               VerticalAlignment="Center"
               FontSize="48"
               FontWeight="SemiBold"
               Foreground="#EF4444"
               TextAlignment="Center" />

    <TextBlock x:Name="RightWordText"
               Grid.Column="2"
               HorizontalAlignment="Left"
               VerticalAlignment="Center"
               FontSize="48"
               FontWeight="SemiBold"
               TextAlignment="Left"
               TextTrimming="CharacterEllipsis" />
</Grid>
```

Modify `src/Views/RsvpOverlay.xaml.cs`:

```csharp
using System.Windows;
using System.Windows.Input;
using ReadX.Rsvp;

namespace ReadX.Views;

public partial class RsvpOverlay : Window
{
    public RsvpOverlay()
    {
        InitializeComponent();
    }

    public event Action? PauseRequested;
    public event Action? RestartRequested;
    public event Action? CancelRequested;

    public void ShowWord(string word, int index, int total)
    {
        ShowWord(OrpCalculator.Create(word), index, total);
    }

    public void ShowWord(OrpWord word, int index, int total)
    {
        LeftWordText.Text = word.Left;
        FocusLetterText.Text = word.FocusLetter;
        RightWordText.Text = word.Right;
        Progress.Value = total <= 0 ? 0 : (double)index / total;
        FooterText.Text = $"{index} / {total}";
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            PauseRequested?.Invoke();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.R)
        {
            RestartRequested?.Invoke();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            CancelRequested?.Invoke();
            e.Handled = true;
        }
    }
}
```

In `src/Services/RsvpPresenter.cs`, subscribe restart:

```csharp
overlay.RestartRequested += player.Restart;
```

- [ ] **Step 5: Run ORP tests and build**

Run: `dotnet test tests\ReadX.Tests\ReadX.Tests.csproj --filter OrpCalculatorTests -m:1`

Expected: PASS.

Run: `dotnet build -m:1`

Expected: PASS with zero errors.

- [ ] **Step 6: Commit**

```bash
git add src/Rsvp/OrpWord.cs src/Rsvp/OrpCalculator.cs src/Views/RsvpOverlay.xaml src/Views/RsvpOverlay.xaml.cs src/Services/RsvpPresenter.cs tests/ReadX.Tests/OrpCalculatorTests.cs
git commit -m "add ORP overlay rendering"
```

### Task 6: Multi-Hotkey Service

**Files:**
- Modify: `src/Services/IHotkeyService.cs`
- Modify: `src/Services/HotkeyService.cs`
- Modify: `src/Services/HotkeyMapping.cs`
- Test: `tests/ReadX.Tests/HotkeyBindingTests.cs`

- [ ] **Step 1: Add mapping tests**

Add to `HotkeyBindingTests`:

```csharp
[Fact]
public void ToDisplayText_ForEscape_UsesEscLabel()
{
    var binding = new HotkeyBinding(ModifierKeys.None, Key.Escape);

    Assert.Equal("Esc", binding.ToDisplayText());
}
```

- [ ] **Step 2: Run focused tests**

Run: `dotnet test tests\ReadX.Tests\ReadX.Tests.csproj --filter HotkeyBindingTests -m:1`

Expected: PASS after Task 1; this protects display labels before changing service code.

- [ ] **Step 3: Replace hotkey service contract**

Replace `src/Services/IHotkeyService.cs`:

```csharp
using ReadX.Models;
using System.Windows.Input;

namespace ReadX.Services;

public enum HotkeyAction
{
    Capture,
    ReplayLast,
    PauseResume,
    Cancel
}

public sealed record HotkeyRegistration(HotkeyAction Action, HotkeyBinding Binding, bool IsRegistered);

public interface IHotkeyService : IDisposable
{
    IReadOnlyList<HotkeyRegistration> RegisterAll(IntPtr hwnd, IReadOnlyDictionary<HotkeyAction, HotkeyBinding> bindings, Action<HotkeyAction> callback);
    bool TryRegister(IntPtr hwnd, ModifierKeys mods, Key key, Action callback);
    void Unregister();
    void SetBusy(bool busy);
}
```

Update `src/Services/HotkeyService.cs` to use one Win32 id per action and keep the old `TryRegister` wrapper until `App.xaml.cs` moves to `RegisterAll`:

```csharp
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using ReadX.Models;

namespace ReadX.Services;

public sealed class HotkeyService : IHotkeyService
{
    private const int HotkeyBaseId = 0x5258;
    private const int WmHotkey = 0x0312;

    private readonly Dictionary<int, HotkeyAction> actionsById = [];
    private IntPtr hwnd;
    private HwndSource? source;
    private Action<HotkeyAction>? callback;
    private bool isBusy;

    public IReadOnlyList<HotkeyRegistration> RegisterAll(IntPtr hwnd, IReadOnlyDictionary<HotkeyAction, HotkeyBinding> bindings, Action<HotkeyAction> callback)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new ArgumentException("A valid window handle is required.", nameof(hwnd));
        }

        ArgumentNullException.ThrowIfNull(callback);
        Unregister();

        this.hwnd = hwnd;
        this.callback = callback;
        source = HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProc);

        var results = new List<HotkeyRegistration>();
        foreach (var pair in bindings)
        {
            var id = HotkeyBaseId + (int)pair.Key;
            var registered = RegisterHotKey(
                hwnd,
                id,
                HotkeyMapping.ToModifierFlags(pair.Value.Modifiers),
                HotkeyMapping.ToVirtualKey(pair.Value.Key));

            if (registered)
            {
                actionsById[id] = pair.Key;
            }

            results.Add(new HotkeyRegistration(pair.Key, pair.Value, registered));
        }

        return results;
    }

    public bool TryRegister(IntPtr hwnd, ModifierKeys mods, Key key, Action callback)
    {
        var results = RegisterAll(
            hwnd,
            new Dictionary<HotkeyAction, HotkeyBinding>
            {
                [HotkeyAction.Capture] = new HotkeyBinding(mods, key)
            },
            action =>
            {
                if (action == HotkeyAction.Capture)
                {
                    callback();
                }
            });

        return results.Count == 1 && results[0].IsRegistered;
    }

    public void Unregister()
    {
        if (source is not null)
        {
            source.RemoveHook(WndProc);
            source = null;
        }

        foreach (var id in actionsById.Keys.ToArray())
        {
            UnregisterHotKey(hwnd, id);
        }

        actionsById.Clear();
        hwnd = IntPtr.Zero;
        callback = null;
        isBusy = false;
    }

    public void SetBusy(bool busy)
    {
        isBusy = busy;
    }

    public void Dispose()
    {
        Unregister();
    }

    private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmHotkey && actionsById.TryGetValue(wParam.ToInt32(), out var action))
        {
            handled = true;
            if (!isBusy || action is HotkeyAction.PauseResume or HotkeyAction.Cancel)
            {
                callback?.Invoke(action);
            }
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
```

- [ ] **Step 4: Run build**

Run: `dotnet build -m:1`

Expected: PASS because `TryRegister` compatibility remains available for the current `App.xaml.cs`.

- [ ] **Step 5: Commit after App composition compiles**

```bash
git add src/Services/IHotkeyService.cs src/Services/HotkeyService.cs src/Services/HotkeyMapping.cs tests/ReadX.Tests/HotkeyBindingTests.cs
git commit -m "support configurable hotkeys"
```

### Task 7: Controller Integration For Capture, Import, History Replay, Settings

**Files:**
- Modify: `src/AppController.cs`
- Test: `tests/ReadX.Tests/Fakes/InMemorySettingsStore.cs`
- Test: `tests/ReadX.Tests/Fakes/InMemoryHistoryStore.cs`

- [ ] **Step 1: Add fake stores for later controller tests**

Create `tests/ReadX.Tests/Fakes/InMemorySettingsStore.cs`:

```csharp
using ReadX.Models;
using ReadX.Services;

namespace ReadX.Tests.Fakes;

public sealed class InMemorySettingsStore(AppSettings initial) : ISettingsStore
{
    public AppSettings Current { get; private set; } = initial;

    public Task<AppSettings> LoadAsync() => Task.FromResult(Current);

    public Task SaveAsync(AppSettings settings)
    {
        Current = settings;
        return Task.CompletedTask;
    }
}
```

Create `tests/ReadX.Tests/Fakes/InMemoryHistoryStore.cs`:

```csharp
using ReadX.Models;
using ReadX.Services;

namespace ReadX.Tests.Fakes;

public sealed class InMemoryHistoryStore : IHistoryStore
{
    private readonly List<HistoryItem> items = [];

    public Task<IReadOnlyList<HistoryItem>> LoadAsync() => Task.FromResult<IReadOnlyList<HistoryItem>>(items.ToArray());

    public Task AddAsync(string rawText, HistorySource source, int limit)
    {
        if (!string.IsNullOrWhiteSpace(rawText))
        {
            items.Insert(0, new HistoryItem(Guid.NewGuid(), DateTimeOffset.Now, source, rawText));
            while (items.Count > limit)
            {
                items.RemoveAt(items.Count - 1);
            }
        }

        return Task.CompletedTask;
    }

    public Task ApplyLimitAsync(int limit)
    {
        while (items.Count > limit)
        {
            items.RemoveAt(items.Count - 1);
        }

        return Task.CompletedTask;
    }
}
```

- [ ] **Step 2: Update controller shape**

Modify `src/AppController.cs` to:

- Accept `ISettingsStore`, `IHistoryStore`, and current `AppSettings`.
- Add `ImportTextAsync(string rawText)`.
- Add `ReplayHistoryAsync(HistoryItem item)`.
- Add `PauseOrResumePlayback()`, `RestartPlayback()`, and `CancelPlayback()`.
- Use `TextPipeline.BuildWords(rawText, Settings.CleanupEnabled)` for capture/import/history replay.
- Add capture/import raw text to history before playback.
- Do not add a history item for history replay.
- Keep the existing constructor and methods available by delegating to defaults until `App.xaml.cs` is updated in Task 9.

Core helper to add:

```csharp
private async Task<RsvpSession?> CreateSessionAsync(string rawText, HistorySource source, CaptureRegion? region, bool addToHistory)
{
    if (string.IsNullOrWhiteSpace(rawText))
    {
        SetIdle("No text found.");
        return null;
    }

    if (addToHistory)
    {
        await history.AddAsync(rawText, source, Settings.HistoryLimit);
        History = await history.LoadAsync();
    }

    var pipeline = TextPipeline.BuildWords(rawText, Settings.CleanupEnabled);
    if (pipeline.Words.Count == 0)
    {
        SetIdle("No playable words.");
        return null;
    }

    return new RsvpSession(pipeline.Words, region, pipeline.RawText, pipeline.ProcessedText, source);
}
```

- [ ] **Step 3: Build after controller changes**

Run: `dotnet build -m:1`

Expected: PASS because the old constructor and v1 UI-facing methods still delegate through the new pipeline.

### Task 8: Left-Tab Main Window UI

**Files:**
- Modify: `src/Views/MainWindow.xaml`
- Modify: `src/Views/MainWindow.xaml.cs`

- [ ] **Step 1: Replace XAML with left-tab utility layout**

Use this structure in `src/Views/MainWindow.xaml`:

```xml
<Window x:Class="ReadX.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
        Title="ReadX"
        Height="560"
        Width="860"
        MinHeight="480"
        MinWidth="680"
        Background="#111827"
        Foreground="#F8FAFC">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="148" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>

        <Border Grid.Row="0" Grid.Column="0" Background="#0F172A" BorderBrush="#334155" BorderThickness="0,0,1,0" Padding="14">
            <StackPanel>
                <TextBlock Text="ReadX" FontSize="24" FontWeight="SemiBold" Margin="0,0,0,18" />
                <RadioButton x:Name="CaptureTabButton" Content="Capture" IsChecked="True" Checked="TabButton_Checked" Margin="0,0,0,8" />
                <RadioButton x:Name="ImportTabButton" Content="Import" Checked="TabButton_Checked" Margin="0,0,0,8" />
                <RadioButton x:Name="HistoryTabButton" Content="History" Checked="TabButton_Checked" Margin="0,0,0,8" />
                <RadioButton x:Name="SettingsTabButton" Content="Settings" Checked="TabButton_Checked" />
            </StackPanel>
        </Border>

        <Grid Grid.Row="0" Grid.Column="1" Margin="22">
            <Grid x:Name="CapturePanel">
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="*" />
                </Grid.RowDefinitions>
                <DockPanel Margin="0,0,0,18">
                    <ui:Button x:Name="CaptureButton" Width="160" Height="42" Appearance="Primary" Content="Capture Region" Click="CaptureButton_Click" />
                    <ui:Button x:Name="ReplayButton" Width="132" Height="42" Margin="12,0,0,0" Content="Replay Last" Click="ReplayButton_Click" IsEnabled="False" />
                </DockPanel>
                <StackPanel Grid.Row="1" Margin="0,0,0,18">
                    <TextBlock Text="Words per minute" Foreground="#94A3B8" />
                    <DockPanel>
                        <Slider x:Name="WpmSlider" Minimum="100" Maximum="800" TickFrequency="50" Value="300" IsSnapToTickEnabled="True" ValueChanged="WpmSlider_ValueChanged" />
                        <TextBlock x:Name="WpmValueText" Width="48" TextAlignment="Right" FontWeight="SemiBold" />
                    </DockPanel>
                    <StackPanel Orientation="Horizontal" Margin="0,12,0,0">
                        <ui:Button x:Name="PauseResumeButton" Width="132" Content="Pause/Resume" Click="PauseResumeButton_Click" />
                        <ui:Button x:Name="RestartButton" Width="96" Margin="8,0,0,0" Content="Restart" Click="RestartButton_Click" />
                        <ui:Button x:Name="CancelButton" Width="88" Margin="8,0,0,0" Content="Cancel" Click="CancelButton_Click" />
                    </StackPanel>
                </StackPanel>
                <TextBox x:Name="LastTextBox" Grid.Row="2" IsReadOnly="True" TextWrapping="Wrap" AcceptsReturn="True" VerticalScrollBarVisibility="Auto" Background="#0F172A" Foreground="#CBD5E1" Text="No text yet." />
            </Grid>

            <Grid x:Name="ImportPanel" Visibility="Collapsed">
                <Grid.RowDefinitions>
                    <RowDefinition Height="*" />
                    <RowDefinition Height="Auto" />
                </Grid.RowDefinitions>
                <TextBox x:Name="ImportTextBox" TextWrapping="Wrap" AcceptsReturn="True" VerticalScrollBarVisibility="Auto" Background="#0F172A" Foreground="#CBD5E1" />
                <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,14,0,0">
                    <ui:Button x:Name="PlayImportButton" Width="120" Appearance="Primary" Content="Play Text" Click="PlayImportButton_Click" />
                    <ui:Button Width="90" Margin="8,0,0,0" Content="Clear" Click="ClearImportButton_Click" />
                </StackPanel>
            </Grid>

            <Grid x:Name="HistoryPanel" Visibility="Collapsed">
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="*" />
                </Grid.RowDefinitions>
                <TextBlock x:Name="HistoryLimitText" Foreground="#94A3B8" Margin="0,0,0,10" />
                <ListBox x:Name="HistoryList" Grid.Row="1" Background="#0F172A" Foreground="#CBD5E1" />
            </Grid>

            <StackPanel x:Name="SettingsPanel" Visibility="Collapsed">
                <TextBlock Text="Reading" FontSize="18" FontWeight="SemiBold" />
                <CheckBox x:Name="CleanupCheckBox" Content="Clean text before playback" Margin="0,12,0,0" Checked="SettingsChanged" Unchecked="SettingsChanged" />
                <TextBlock Text="History limit" Margin="0,16,0,4" Foreground="#94A3B8" />
                <TextBox x:Name="HistoryLimitTextBox" Width="80" HorizontalAlignment="Left" TextChanged="SettingsTextChanged" />
                <TextBlock Text="Hotkeys" FontSize="18" FontWeight="SemiBold" Margin="0,22,0,8" />
                <TextBlock x:Name="HotkeySummaryText" Foreground="#CBD5E1" />
            </StackPanel>
        </Grid>

        <TextBlock x:Name="StatusText" Grid.Row="1" Grid.ColumnSpan="2" Padding="14,8" Foreground="#94A3B8" Text="Ready." />
    </Grid>
</Window>
```

- [ ] **Step 2: Update code-behind events**

`MainWindow.xaml.cs` must expose:

```csharp
public event EventHandler? CaptureRequested;
public event EventHandler? ReplayRequested;
public event EventHandler? PauseResumeRequested;
public event EventHandler? RestartRequested;
public event EventHandler? CancelRequested;
public event EventHandler<string>? ImportRequested;
public event EventHandler? SettingsChangedByUser;
```

and setters:

```csharp
public void ApplySettings(AppSettings settings)
public AppSettings ReadSettings(AppSettings current)
public void SetHistory(IReadOnlyList<HistoryItem> items)
public void SetHotkeyStatus(IReadOnlyList<HotkeyRegistration> registrations)
```

The `HistoryList` item template can be built in code for v2: display timestamp/source/preview and a Replay button whose `Tag` is the `HistoryItem`.

- [ ] **Step 3: Build**

Run: `dotnet build -m:1`

Expected: build still fails until AppController/App.xaml composition is connected, or passes if all events are wired immediately.

### Task 9: Composition, Settings Save, Hotkey Registration

**Files:**
- Modify: `src/App.xaml.cs`
- Modify: `src/AppController.cs`
- Modify: `src/Views/MainWindow.xaml.cs`

- [ ] **Step 1: Wire services in `App.xaml.cs`**

In `OnStartup`, create and load:

```csharp
var pathProvider = new AppDataPathProvider();
var settingsStore = new JsonSettingsStore(pathProvider);
var historyStore = new JsonHistoryStore(pathProvider);
var settings = await settingsStore.LoadAsync();
var history = await historyStore.LoadAsync();
```

Because `OnStartup` is not async, use a private async method:

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    _ = StartAsync();
}

private async Task StartAsync()
{
    var pathProvider = new AppDataPathProvider();
    settingsStore = new JsonSettingsStore(pathProvider);
    historyStore = new JsonHistoryStore(pathProvider);
    var settings = await settingsStore.LoadAsync();
    var historyItems = await historyStore.LoadAsync();

    mainWindow = new MainWindow();
    mainWindow.ApplySettings(settings);
    mainWindow.SetHistory(historyItems);

    hotkey = new HotkeyService();
    presenter = new RsvpPresenter();
    ocr = TryCreateOcrService();

    controller = new AppController(
        hotkey,
        new RegionSelector(),
        new ScreenCaptureService(),
        ocr,
        new RsvpPlayer(new DispatcherTicker()),
        presenter,
        settingsStore,
        historyStore,
        settings,
        historyItems);

    controller.StateChanged += ApplyControllerState;
    mainWindow.SourceInitialized += (_, _) => RegisterHotkeys();
    mainWindow.Show();
}
```

- [ ] **Step 2: Register all hotkeys after `SourceInitialized`**

Use:

```csharp
private void RegisterHotkeys()
{
    if (mainWindow is null || controller is null || hotkey is null)
    {
        return;
    }

    var hwnd = new WindowInteropHelper(mainWindow).Handle;
    var registrations = hotkey.RegisterAll(
        hwnd,
        controller.GetHotkeyBindings(),
        action => _ = controller.HandleHotkeyAsync(action));

    controller.SetHotkeyRegistrations(registrations);
}
```

- [ ] **Step 3: Save settings when changed and on exit**

On settings changes:

```csharp
mainWindow.SettingsChangedByUser += async (_, _) =>
{
    var updated = mainWindow.ReadSettings(controller.Settings);
    await controller.UpdateSettingsAsync(updated);
    RegisterHotkeys();
};
```

On exit:

```csharp
protected override void OnExit(ExitEventArgs e)
{
    if (mainWindow is not null && controller is not null)
    {
        var finalSettings = mainWindow.ReadSettings(controller.Settings);
        settingsStore?.SaveAsync(finalSettings).GetAwaiter().GetResult();
    }

    hotkey?.Dispose();
    ocr?.Dispose();
    presenter?.Close();
    base.OnExit(e);
}
```

- [ ] **Step 4: Connect UI events**

Wire:

```csharp
mainWindow.CaptureRequested += (_, _) => _ = controller.StartCaptureAsync();
mainWindow.ImportRequested += (_, text) => _ = controller.ImportTextAsync(text);
mainWindow.ReplayRequested += (_, _) => _ = controller.ReplayLastAsync();
mainWindow.PauseResumeRequested += (_, _) => controller.PauseOrResumePlayback();
mainWindow.RestartRequested += (_, _) => controller.RestartPlayback();
mainWindow.CancelRequested += (_, _) => controller.CancelPlayback();
```

- [ ] **Step 5: Build**

Run: `dotnet build -m:1`

Expected: PASS with zero errors.

- [ ] **Step 6: Run full tests**

Run: `dotnet test -m:1`

Expected: PASS.

- [ ] **Step 7: Commit controller/UI composition**

```bash
git add src/App.xaml.cs src/AppController.cs src/Views/MainWindow.xaml src/Views/MainWindow.xaml.cs src/Services/IHotkeyService.cs src/Services/HotkeyService.cs
git commit -m "wire v2 workflows"
```

### Task 10: Manual Golden Path And Documentation

**Files:**
- Modify: `docs/changelog.md`
- Modify: `docs/project_status.md`
- Modify: `docs/architecture.md`
- Modify: `docs/journal.md` only if a notable implementation lesson emerges.

- [ ] **Step 1: Run automated verification**

Run: `dotnet build -m:1`

Expected: PASS with zero errors.

Run: `dotnet test -m:1`

Expected: PASS with all tests passing.

- [ ] **Step 2: Launch app for manual verification**

Run: `dotnet run --project src\ReadX.csproj --no-build`

Expected: app launches and remains open.

- [ ] **Step 3: Manual golden path checklist**

Verify:

- App restart preserves WPM, cleanup toggle, history limit, hotkey settings, and window size.
- Capture adds raw OCR text to history and starts ORP playback.
- Import adds raw pasted text to history and starts ORP playback.
- History replay starts playback and does not add a new history entry.
- Cleanup toggle changes the text fed to playback.
- Configured capture and replay-last hotkeys work.
- Pause/resume, restart, and cancel work from UI and hotkeys.
- A deliberately conflicting hotkey shows a failed binding without disabling unrelated controls.
- ORP focus letter stays on a stable visual anchor.
- OCR unavailable mode still allows import and history replay.

- [ ] **Step 4: Update docs**

Add changelog entry:

```markdown
- v2 implemented: persisted settings, raw-text history, quick import, configurable hotkeys, text cleanup toggle, restartable replay controls, classic ORP rendering, and compact left-tab UI.
```

Update `docs/project_status.md`:

```markdown
**Current Phase:** v2 - Implementation complete; final PR preparation pending
```

Update `docs/architecture.md` with:

```markdown
- `Settings` / `JsonSettingsStore` persist user preferences under app data.
- `History` / `JsonHistoryStore` store raw capture/import text only.
- `TextPipeline` runs optional cleanup before tokenization for capture, import, and history replay.
- `Rsvp/OrpCalculator` computes the fixed v2 focus letter used by `RsvpOverlay`.
- `HotkeyService` registers configurable capture, replay, pause/resume, and cancel hotkeys.
```

- [ ] **Step 5: Commit docs**

```bash
git add docs/changelog.md docs/project_status.md docs/architecture.md docs/journal.md
git commit -m "document v2 implementation"
```

---

## Execution Notes

- Do not install packages unless the user explicitly approves a new dependency.
- Keep history raw-text only. Do not store tokens, cleaned text, WPM, ORP positions, or playback position.
- Do not add manual history deletion or clear-history UI in v2.
- Do not add overlay customization in v2.
- Do not make the UI a large dashboard; keep the left navigation compact.
- Run the golden path manually before declaring v2 complete.
