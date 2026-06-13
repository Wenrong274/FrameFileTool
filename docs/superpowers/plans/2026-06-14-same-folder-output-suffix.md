# Same Folder Output Suffix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development
> (recommended) or superpowers:executing-plans to implement this plan task-by-task.
> Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 批次縮放指定輸出資料夾時，若使用者選到來源資料夾，自動改用來源內命名子資料夾並記錄 log。

**Architecture:** 新增純 service `OutputFolderResolver` 負責判斷來源與目標是否相同並產生命名子資料夾。
`ResizeToolViewModel` 在執行時選完資料夾後呼叫 resolver，再用解析後路徑重建預覽與執行；
planner、preview service 與 executor 只接收已解析路徑。

**Tech Stack:** .NET 10、WPF、CommunityToolkit.Mvvm、xUnit、FluentAssertions、NSubstitute。

---

## File Structure

- Create: `FrameFileTool/Models/ResolvedOutputFolder.cs`
  - 保存解析後輸出資料夾、是否自動改用子資料夾、log 訊息。
- Create: `FrameFileTool/Services/Interfaces/IOutputFolderResolver.cs`
  - 定義批次縮放輸出資料夾解析合約。
- Create: `FrameFileTool/Services/OutputFolderResolver.cs`
  - 實作 pure resolver，不讀寫檔案、不呼叫 WPF。
- Create: `FrameFileTool.Tests/Services/OutputFolderResolverTests.cs`
  - 覆蓋同路徑、防大小寫差異、倍率與絕對尺寸後綴、非法字元安全化。
- Modify: `FrameFileTool/ViewModels/Tools/ResizeToolViewModel.cs`
  - 注入 resolver；指定資料夾模式執行時解析目標路徑並寫 log。
- Modify: `FrameFileTool/ViewModels/MainViewModel.cs`
  - 建構 `ResizeToolViewModel` 時傳入 resolver。
- Modify: `FrameFileTool/App.xaml.cs`
  - DI 註冊 `IOutputFolderResolver`。
- Modify: `FrameFileTool.Tests/ViewModels/MainViewModelCanExecuteTests.cs`
  - test factory 補 resolver mock；新增執行流程測試。
- Modify: `README.md`
  - 批次縮放功能描述補同來源輸出會自動建立子資料夾。

---

### Task 1: Add Output Folder Resolver Model And Interface

**Files:**

- Create: `FrameFileTool/Models/ResolvedOutputFolder.cs`
- Create: `FrameFileTool/Services/Interfaces/IOutputFolderResolver.cs`
- Test: `FrameFileTool.Tests/Services/OutputFolderResolverTests.cs`

- [ ] **Step 1: Write the failing model/interface usage test**

Create `FrameFileTool.Tests/Services/OutputFolderResolverTests.cs` with this first test:

```csharp
using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services;

namespace FrameFileTool.Tests.Services;

public sealed class OutputFolderResolverTests
{
    [Fact]
    public void Resolve_來源與目標不同_應保留使用者選擇的資料夾()
    {
        var sut = new OutputFolderResolver();
        var options = ScaleOptions(0.5);

        var result = sut.ResolveForResize(@"C:\imgs\cloud", @"D:\out", options);

        result.TargetFolderPath.Should().Be(@"D:\out");
        result.WasAutoRedirected.Should().BeFalse();
        result.LogMessage.Should().BeNull();
    }

    private static ResizeOptions ScaleOptions(double factor) =>
        new(
            Mode: ResizeMode.ScaleFactor,
            ScaleFactor: factor,
            TargetWidth: 0,
            TargetHeight: 0,
            KeepAspectRatio: true,
            OutputMode: ResizeOutputMode.TargetFolder,
            TargetFolderPath: string.Empty,
            Resampler: ResamplerType.Bicubic);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test --filter "FullyQualifiedName~OutputFolderResolverTests" -p:UseAppHost=false
```

Expected: FAIL because `OutputFolderResolver` and `ResolvedOutputFolder` do not exist.

- [ ] **Step 3: Add minimal model and interface**

Create `FrameFileTool/Models/ResolvedOutputFolder.cs`:

```csharp
namespace FrameFileTool.Models;

/// <summary>
/// 輸出資料夾解析結果。resolver 只回傳資料，不執行檔案系統副作用。
/// </summary>
public sealed record ResolvedOutputFolder(
    string TargetFolderPath,
    bool WasAutoRedirected,
    string? LogMessage);
```

Create `FrameFileTool/Services/Interfaces/IOutputFolderResolver.cs`:

```csharp
using FrameFileTool.Models;

namespace FrameFileTool.Services.Interfaces;

/// <summary>
/// 將使用者選擇的輸出資料夾解析成實際安全輸出位置。
/// </summary>
public interface IOutputFolderResolver
{
    /// <summary>解析批次縮放的實際輸出資料夾。</summary>
    public ResolvedOutputFolder ResolveForResize(
        string sourceFolderPath,
        string selectedTargetFolderPath,
        ResizeOptions options);
}
```

- [ ] **Step 4: Add minimal implementation**

Create `FrameFileTool/Services/OutputFolderResolver.cs`:

```csharp
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;

namespace FrameFileTool.Services;

/// <inheritdoc/>
public sealed class OutputFolderResolver : IOutputFolderResolver
{
    /// <inheritdoc/>
    public ResolvedOutputFolder ResolveForResize(
        string sourceFolderPath,
        string selectedTargetFolderPath,
        ResizeOptions options) =>
        new(selectedTargetFolderPath, WasAutoRedirected: false, LogMessage: null);
}
```

- [ ] **Step 5: Run test to verify it passes**

Run:

```powershell
dotnet test --filter "FullyQualifiedName~OutputFolderResolverTests" -p:UseAppHost=false
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add FrameFileTool\Models\ResolvedOutputFolder.cs `
        FrameFileTool\Services\Interfaces\IOutputFolderResolver.cs `
        FrameFileTool\Services\OutputFolderResolver.cs `
        FrameFileTool.Tests\Services\OutputFolderResolverTests.cs
git commit -m "feat(OutputFolderResolver): 新增輸出資料夾解析模型"
```

---

### Task 2: Resolve Same Source Folder To Suffix Folder

**Files:**

- Modify: `FrameFileTool/Services/OutputFolderResolver.cs`
- Modify: `FrameFileTool.Tests/Services/OutputFolderResolverTests.cs`

- [ ] **Step 1: Add failing same-folder and suffix tests**

Append these tests to `OutputFolderResolverTests`:

```csharp
[Fact]
public void Resolve_來源與目標相同_倍率模式應改用來源內倍率後綴資料夾()
{
    var sut = new OutputFolderResolver();
    var options = ScaleOptions(0.5);

    var result = sut.ResolveForResize(@"C:\imgs\cloud", @"C:\imgs\cloud", options);

    result.TargetFolderPath.Should().Be(@"C:\imgs\cloud\cloud_x0.5");
    result.WasAutoRedirected.Should().BeTrue();
    result.LogMessage.Should().Contain(@"C:\imgs\cloud\cloud_x0.5");
}

[Fact]
public void Resolve_來源與目標大小寫不同但相同_應改用來源內子資料夾()
{
    var sut = new OutputFolderResolver();
    var options = ScaleOptions(0.75);

    var result = sut.ResolveForResize(@"C:\imgs\cloud", @"c:\IMGS\CLOUD", options);

    result.TargetFolderPath.Should().Be(@"C:\imgs\cloud\cloud_x0.75");
    result.WasAutoRedirected.Should().BeTrue();
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test --filter "FullyQualifiedName~OutputFolderResolverTests" -p:UseAppHost=false
```

Expected: FAIL because resolver still returns selected target folder unchanged.

- [ ] **Step 3: Implement same-folder detection and scale suffix**

Replace `OutputFolderResolver` with:

```csharp
using System.Globalization;
using System.IO;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;

namespace FrameFileTool.Services;

/// <inheritdoc/>
public sealed class OutputFolderResolver : IOutputFolderResolver
{
    /// <inheritdoc/>
    public ResolvedOutputFolder ResolveForResize(
        string sourceFolderPath,
        string selectedTargetFolderPath,
        ResizeOptions options)
    {
        var normalizedSource = NormalizePath(sourceFolderPath);
        var normalizedTarget = NormalizePath(selectedTargetFolderPath);

        if (!string.Equals(normalizedSource, normalizedTarget, StringComparison.OrdinalIgnoreCase))
        {
            return new(selectedTargetFolderPath, WasAutoRedirected: false, LogMessage: null);
        }

        var sourceFolderName = Path.GetFileName(normalizedSource);
        var suffix = BuildResizeSuffix(options);
        var targetFolder = Path.Combine(normalizedSource, $"{sourceFolderName}{suffix}");

        return new(
            targetFolder,
            WasAutoRedirected: true,
            LogMessage: $"輸出資料夾與來源相同，已自動改用：{targetFolder}");
    }

    private static string NormalizePath(string path) =>
        Path.GetFullPath(path.Trim()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static string BuildResizeSuffix(ResizeOptions options) =>
        options.Mode == ResizeMode.ScaleFactor
            ? $"_x{options.ScaleFactor.ToString("0.####", CultureInfo.InvariantCulture)}"
            : $"_{options.TargetWidth}x{options.TargetHeight}";
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:

```powershell
dotnet test --filter "FullyQualifiedName~OutputFolderResolverTests" -p:UseAppHost=false
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add FrameFileTool\Services\OutputFolderResolver.cs `
        FrameFileTool.Tests\Services\OutputFolderResolverTests.cs
git commit -m "feat(OutputFolderResolver): 支援同來源倍率輸出資料夾"
```

---

### Task 3: Add Absolute Size Naming And Safe Folder Name Handling

**Files:**

- Modify: `FrameFileTool/Services/OutputFolderResolver.cs`
- Modify: `FrameFileTool.Tests/Services/OutputFolderResolverTests.cs`

- [ ] **Step 1: Add failing absolute and sanitization tests**

Append these helpers and tests to `OutputFolderResolverTests`:

```csharp
[Fact]
public void Resolve_絕對尺寸模式_應使用尺寸後綴()
{
    var sut = new OutputFolderResolver();
    var options = AbsoluteOptions(width: 800, height: 600, keepAspectRatio: true);

    var result = sut.ResolveForResize(@"C:\imgs\cloud", @"C:\imgs\cloud", options);

    result.TargetFolderPath.Should().Be(@"C:\imgs\cloud\cloud_800x600");
}

[Fact]
public void Resolve_絕對尺寸單邊為零_應保留零值以反映使用者設定()
{
    var sut = new OutputFolderResolver();
    var options = AbsoluteOptions(width: 800, height: 0, keepAspectRatio: true);

    var result = sut.ResolveForResize(@"C:\imgs\cloud", @"C:\imgs\cloud", options);

    result.TargetFolderPath.Should().Be(@"C:\imgs\cloud\cloud_800x0");
}

[Fact]
public void Resolve_來源資料夾名稱含非法字元_應轉為安全資料夾名稱()
{
    var sut = new OutputFolderResolver();
    var options = ScaleOptions(0.5);

    var result = sut.ResolveForResize(@"C:\imgs\cloud.", @"C:\imgs\cloud.", options);

    result.TargetFolderPath.Should().Be(@"C:\imgs\cloud.\cloud_x0.5");
}

private static ResizeOptions AbsoluteOptions(int width, int height, bool keepAspectRatio) =>
    new(
        Mode: ResizeMode.Absolute,
        ScaleFactor: 1,
        TargetWidth: width,
        TargetHeight: height,
        KeepAspectRatio: keepAspectRatio,
        OutputMode: ResizeOutputMode.TargetFolder,
        TargetFolderPath: string.Empty,
        Resampler: ResamplerType.Bicubic);
```

- [ ] **Step 2: Run test to verify current state**

Run:

```powershell
dotnet test --filter "FullyQualifiedName~OutputFolderResolverTests" -p:UseAppHost=false
```

Expected: absolute size tests may already pass after Task 2.
The sanitization test documents safe naming behavior and must pass with `cloud.` converted to `cloud`.

- [ ] **Step 3: Implement safe folder segment**

Update `OutputFolderResolver`:

```csharp
private static string BuildSafeFolderName(string folderName)
{
    var invalidChars = Path.GetInvalidFileNameChars();
    var chars = folderName
        .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
        .ToArray();

    var safeName = new string(chars).Trim().TrimEnd('.');
    return string.IsNullOrWhiteSpace(safeName) ? "output" : safeName;
}
```

Change the target folder construction:

```csharp
var sourceFolderName = BuildSafeFolderName(Path.GetFileName(normalizedSource));
var suffix = BuildResizeSuffix(options);
var targetFolder = Path.Combine(normalizedSource, $"{sourceFolderName}{suffix}");
```

- [ ] **Step 4: Run test to verify it passes**

Run:

```powershell
dotnet test --filter "FullyQualifiedName~OutputFolderResolverTests" -p:UseAppHost=false
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add FrameFileTool\Services\OutputFolderResolver.cs `
        FrameFileTool.Tests\Services\OutputFolderResolverTests.cs
git commit -m "feat(OutputFolderResolver): 補齊尺寸後綴與安全命名"
```

---

### Task 4: Wire Resolver Into Resize Execution

**Files:**

- Modify: `FrameFileTool/ViewModels/Tools/ResizeToolViewModel.cs`
- Modify: `FrameFileTool/ViewModels/MainViewModel.cs`
- Modify: `FrameFileTool/App.xaml.cs`
- Modify: `FrameFileTool.Tests/ViewModels/MainViewModelCanExecuteTests.cs`

- [ ] **Step 1: Add failing ViewModel tests**

Modify `CreateSut` in `MainViewModelCanExecuteTests` to accept `IOutputFolderResolver? outputFolderResolver = null`.

Pass it into `MainViewModel` constructor after `resizePreviewService`:

```csharp
outputFolderResolver ?? Substitute.For<IOutputFolderResolver>(),
```

Add this test near existing resize execute tests:

```csharp
[Fact]
public async Task ExecuteResize_指定資料夾選到來源_應使用自動解析後路徑並寫入log()
{
    var resizePreviewService = Substitute.For<IResizePreviewService>();
    resizePreviewService
        .BuildPreviewAsync(
            Arg.Any<IReadOnlyList<FileItem>>(),
            Arg.Any<ResizeOptions>(),
            Arg.Any<CancellationToken>())
        .Returns([new ResizePreviewItem { ActionKind = OperationActionKind.Resize, TargetPath = @"C:\imgs\cloud\cloud_x0.5\a.png" }]);
    var resizeExecutor = Substitute.For<IImageResizeExecutor>();
    resizeExecutor
        .ExecuteAsync(
            Arg.Any<IEnumerable<OperationPreviewItem>>(),
            Arg.Any<ResizeOptions>(),
            Arg.Any<IProgress<ResizeProgressReport>>(),
            Arg.Any<CancellationToken>())
        .Returns(new OperationResult { SuccessCount = 1 });
    var folderPicker = Substitute.For<IFolderPickerService>();
    folderPicker.PickFolder(Arg.Any<string>()).Returns(@"C:\imgs\cloud");
    var resolver = Substitute.For<IOutputFolderResolver>();
    resolver
        .ResolveForResize(@"C:\imgs\cloud", @"C:\imgs\cloud", Arg.Any<ResizeOptions>())
        .Returns(new ResolvedOutputFolder(
            @"C:\imgs\cloud\cloud_x0.5",
            WasAutoRedirected: true,
            LogMessage: @"輸出資料夾與來源相同，已自動改用：C:\imgs\cloud\cloud_x0.5"));
    var sut = CreateSut(
        folderPicker: folderPicker,
        resizeExecutor: resizeExecutor,
        resizePreviewService: resizePreviewService,
        outputFolderResolver: resolver);
    sut.SelectedFolder = @"C:\imgs\cloud";
    sut.ResizeTool.OutputMode = ResizeOutputMode.TargetFolder;
    sut.Files.Add(new FileItem(@"C:\imgs\cloud\a.png", @"C:\imgs\cloud", "a.png", ".png", 10));
    sut.CurrentPreview = ResizePreview();

    await sut.ResizeTool.ExecuteCommand.ExecuteAsync(null);

    sut.Logs.Should().Contain(log => log.Contains(@"C:\imgs\cloud\cloud_x0.5"));
    await resizeExecutor.Received(1).ExecuteAsync(
        Arg.Any<IEnumerable<OperationPreviewItem>>(),
        Arg.Is<ResizeOptions>(options => options.TargetFolderPath == @"C:\imgs\cloud\cloud_x0.5"),
        Arg.Any<IProgress<ResizeProgressReport>>(),
        Arg.Any<CancellationToken>());
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test --filter "FullyQualifiedName~ExecuteResize_指定資料夾選到來源" -p:UseAppHost=false
```

Expected: FAIL because `MainViewModel` and `ResizeToolViewModel` do not accept resolver.

- [ ] **Step 3: Update constructors and execution flow**

In `ResizeToolViewModel`, add field and constructor parameter:

```csharp
private readonly IOutputFolderResolver _outputFolderResolver;
```

Constructor:

```csharp
internal ResizeToolViewModel(
    IImageResizeExecutor resizeExecutor,
    IResizePreviewService resizePreviewService,
    IOutputFolderResolver outputFolderResolver,
    IToolContext context,
    TimeSpan debounceDelay)
{
    _resizeExecutor = resizeExecutor;
    _resizePreviewService = resizePreviewService;
    _outputFolderResolver = outputFolderResolver;
    _context = context;
    _debounceDelay = debounceDelay;
}
```

In target-folder branch of `Execute`, replace:

```csharp
options = options with { TargetFolderPath = targetFolder };
```

with:

```csharp
var resolvedFolder = _outputFolderResolver.ResolveForResize(_context.SelectedFolder, targetFolder, options);
if (resolvedFolder.WasAutoRedirected && !string.IsNullOrWhiteSpace(resolvedFolder.LogMessage))
{
    _context.AddLog(resolvedFolder.LogMessage);
}

options = options with { TargetFolderPath = resolvedFolder.TargetFolderPath };
```

In `MainViewModel`, add constructor parameter:

```csharp
IOutputFolderResolver outputFolderResolver,
```

Pass it to `ResizeToolViewModel` construction:

```csharp
ResizeTool = new ResizeToolViewModel(
    imageResizeExecutor,
    resizePreviewService,
    outputFolderResolver,
    this,
    debounceDelay == default ? TimeSpan.FromMilliseconds(350) : debounceDelay);
```

In `App.xaml.cs`, add service registration:

```csharp
services.AddSingleton<IOutputFolderResolver, OutputFolderResolver>();
```

- [ ] **Step 4: Run targeted test to verify it passes**

Run:

```powershell
dotnet test --filter "FullyQualifiedName~ExecuteResize_指定資料夾選到來源" -p:UseAppHost=false
```

Expected: PASS.

- [ ] **Step 5: Run all ViewModel tests**

Run:

```powershell
dotnet test --filter "FullyQualifiedName~MainViewModelCanExecuteTests" -p:UseAppHost=false
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add FrameFileTool\ViewModels\Tools\ResizeToolViewModel.cs `
        FrameFileTool\ViewModels\MainViewModel.cs `
        FrameFileTool\App.xaml.cs `
        FrameFileTool.Tests\ViewModels\MainViewModelCanExecuteTests.cs
git commit -m "feat(ResizeTool): 同來源輸出時自動改用子資料夾"
```

---

### Task 5: Add Conflict Regression And Documentation

**Files:**

- Modify: `FrameFileTool.Tests/ViewModels/MainViewModelCanExecuteTests.cs`
- Modify: `README.md`

- [ ] **Step 1: Add failing conflict regression test**

Add this test near the resize execute tests:

```csharp
[Fact]
public async Task ExecuteResize_自動子資料夾已有衝突_應停止執行並保留錯誤預覽()
{
    var resizePreviewService = Substitute.For<IResizePreviewService>();
    resizePreviewService
        .BuildPreviewAsync(
            Arg.Any<IReadOnlyList<FileItem>>(),
            Arg.Any<ResizeOptions>(),
            Arg.Any<CancellationToken>())
        .Returns([
            new ResizePreviewItem
            {
                ActionKind = OperationActionKind.Error,
                HasError = true,
                Status = "目標檔案已存在",
            },
        ]);
    var resizeExecutor = Substitute.For<IImageResizeExecutor>();
    var folderPicker = Substitute.For<IFolderPickerService>();
    folderPicker.PickFolder(Arg.Any<string>()).Returns(@"C:\imgs\cloud");
    var resolver = Substitute.For<IOutputFolderResolver>();
    resolver
        .ResolveForResize(@"C:\imgs\cloud", @"C:\imgs\cloud", Arg.Any<ResizeOptions>())
        .Returns(new ResolvedOutputFolder(
            @"C:\imgs\cloud\cloud_x0.5",
            WasAutoRedirected: true,
            LogMessage: @"輸出資料夾與來源相同，已自動改用：C:\imgs\cloud\cloud_x0.5"));
    var sut = CreateSut(
        folderPicker: folderPicker,
        resizeExecutor: resizeExecutor,
        resizePreviewService: resizePreviewService,
        outputFolderResolver: resolver);
    sut.SelectedFolder = @"C:\imgs\cloud";
    sut.ResizeTool.OutputMode = ResizeOutputMode.TargetFolder;
    sut.Files.Add(new FileItem(@"C:\imgs\cloud\a.png", @"C:\imgs\cloud", "a.png", ".png", 10));
    sut.CurrentPreview = ResizePreview();

    await sut.ResizeTool.ExecuteCommand.ExecuteAsync(null);

    resizeExecutor.DidNotReceive().ExecuteAsync(
        Arg.Any<IEnumerable<OperationPreviewItem>>(),
        Arg.Any<ResizeOptions>(),
        Arg.Any<IProgress<ResizeProgressReport>>(),
        Arg.Any<CancellationToken>());
    sut.HasPreviewErrors.Should().BeTrue();
    sut.Logs.Should().Contain(log => log.Contains("縮放已停止"));
}
```

- [ ] **Step 2: Run test to verify it passes or fails for correct reason**

Run:

```powershell
dotnet test --filter "FullyQualifiedName~ExecuteResize_自動子資料夾已有衝突" -p:UseAppHost=false
```

Expected: PASS if Task 4 reused existing `ApplyPlannedPreviewOrLogConflict` correctly.
If it fails, fix only the resize execute conflict branch so it calls
`ApplyPlannedPreviewOrLogConflict` after resolver.

- [ ] **Step 3: Update README**

In `README.md`, change the resize feature bullet:

```markdown
- 批次縮放：支援倍率、絕對尺寸、等比縮放、置入指定範圍；若指定輸出資料夾與來源相同，會自動建立命名子資料夾輸出
```

- [ ] **Step 4: Run markdownlint**

Run:

```powershell
npx markdownlint-cli2 "*.md" "docs/**/*.md"
```

Expected: 0 errors.

- [ ] **Step 5: Commit**

```powershell
git add FrameFileTool.Tests\ViewModels\MainViewModelCanExecuteTests.cs README.md
git commit -m "docs(README): 補充同來源縮放輸出行為"
```

---

### Task 6: Final Verification And TODO Update

**Files:**

- Modify: `TODO.md`

- [ ] **Step 1: Run full verification**

Run:

```powershell
dotnet test -p:UseAppHost=false
dotnet format --verify-no-changes --severity warn
npx markdownlint-cli2 "*.md" "docs/**/*.md"
```

Expected:

- `dotnet test`: all tests pass.
- `dotnet format`: exit 0.
- `markdownlint-cli2`: 0 errors.

- [ ] **Step 2: Update TODO implementation progress**

In `TODO.md`, update completed implementation subitems for `same-folder-output-suffix` from `[ ]` to `[x]`.
Do not mark completion criteria `[x]`; those require developer manual confirmation.

- [ ] **Step 3: Commit TODO update**

```powershell
git add TODO.md
git commit -m "docs(TODO): 更新同來源輸出實作進度"
```

- [ ] **Step 4: Report remaining manual verification**

Report that completion criteria still need developer confirmation:

- 0.5 倍縮放輸出到 `cloud\cloud_x0.5`。
- 不同輸出資料夾仍維持既有行為。
- 大小寫不同但同路徑仍自動改用子資料夾。
- 子資料夾同名檔衝突時預覽標錯且不執行。
- log 顯示原選擇與實際輸出資料夾。
