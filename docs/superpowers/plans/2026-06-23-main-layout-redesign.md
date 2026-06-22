# 主視窗版面優化 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development
> (recommended) or superpowers:executing-plans to implement this plan task-by-task.
> Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 砍掉與原生標題列重複的自製頁首，並把來源設定改為頂部「漸進式收合」
（空狀態引導 → 設定後縮成單行 chip → 需要時展開），讓動線變成 來源 → 工具 → 預覽 → Log。

**Architecture:** 純 View（`MainWindow.xaml`）改版 + `MainViewModel` 少量收合狀態。
沿用既有 service / planner / 掃描命令，無檔案操作邏輯變動。
來源三態（空 / chip / 展開）由 `MainViewModel.IsSourceExpanded` 與 `HasSource` 驅動。

**Tech Stack:** .NET 10、WPF、CommunityToolkit.Mvvm、xUnit + FluentAssertions + NSubstitute。

設計來源：`docs/superpowers/specs/2026-06-23-main-layout-redesign-design.md`

---

## File Structure

- `FrameFileTool/ViewModels/MainViewModel.cs` — 新增來源收合狀態（`IsSourceExpanded`、`HasSource`、`ToggleSourceCommand`）與掃描後自動收合。
- `FrameFileTool/MainWindow.xaml` — 刪除自製頁首；來源區移到 TabControl 上方並改為三態；調整外層 `RowDefinitions`。
- `FrameFileTool.Tests/ViewModels/MainViewModelSourceCollapseTests.cs` — 新增收合狀態的單元測試。
- `TODO.md` — 將 `shared-source-bar` 標記為被本設計取代。
- `UI_UX_DESIGN_RULES.md` — 更新資訊架構（來源位置改為頂部漸進收合）。

## 分支策略

目前在 `feat/tool-tabs-layout`，且有未提交的 TabControl View 變更（含已修正的 `RowDefinitions` bug）。

- **Task 1** 先把現有 tool-tabs-layout View 變更提交成乾淨基線。
- 之後任務在同一分支接續（版面優化緊接 tool-tabs，屬同一條 UI 演進）。

---

### Task 1: 提交 tool-tabs-layout View 基線

**Files:**

- Modify: `FrameFileTool/MainWindow.xaml`（已含 TabControl 改版 + RowDefinition 修正）
- Modify: `TODO.md`、`UI_UX_DESIGN_RULES.md`（已改）

- [ ] **Step 1: 確認 build 與測試通過**

Run: `dotnet build FrameFileTool/FrameFileTool.csproj -nologo`
Expected: `建置成功` 0 警告 0 錯誤

Run: `dotnet test FrameFileTool.Tests/FrameFileTool.Tests.csproj -nologo`
Expected: `通過: 309`

- [ ] **Step 2: 提交**

```bash
git add FrameFileTool/MainWindow.xaml TODO.md UI_UX_DESIGN_RULES.md
git commit -m "feat(view): 主內容改用全寬 TabControl 取代 2×2 工具切換"
```

---

### Task 2: MainViewModel 新增來源收合狀態（TDD）

**Files:**

- Modify: `FrameFileTool/ViewModels/MainViewModel.cs`（屬性區約 64 行後、`RefreshScanFilesCore` 約 226 行、建構子約 175 行）
- Test: `FrameFileTool.Tests/ViewModels/MainViewModelSourceCollapseTests.cs`（新建）

- [ ] **Step 1: 寫失敗測試**

Create `FrameFileTool.Tests/ViewModels/MainViewModelSourceCollapseTests.cs`：

```csharp
using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels;
using NSubstitute;

namespace FrameFileTool.Tests.ViewModels;

public sealed class MainViewModelSourceCollapseTests
{
    [Fact]
    public void IsSourceExpanded_預設應為true()
    {
        var sut = CreateSut(Substitute.For<IFileScanner>());

        sut.IsSourceExpanded.Should().BeTrue();
    }

    [Fact]
    public void HasSource_無檔案時false_掃描出檔案後true()
    {
        var scanner = Substitute.For<IFileScanner>();
        scanner.Scan(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<bool>())
            .Returns(new FileScanResult(
                [new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10)], []));
        var sut = CreateSut(scanner);

        sut.HasSource.Should().BeFalse();

        sut.SelectedFolder = @"C:\imgs";
        sut.ScanFilesCommand.Execute(null);

        sut.HasSource.Should().BeTrue();
    }

    [Fact]
    public void ScanFiles_成功掃描出檔案後_IsSourceExpanded應自動收合為false()
    {
        var scanner = Substitute.For<IFileScanner>();
        scanner.Scan(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<bool>())
            .Returns(new FileScanResult(
                [new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10)], []));
        var sut = CreateSut(scanner);
        sut.SelectedFolder = @"C:\imgs";

        sut.ScanFilesCommand.Execute(null);

        sut.IsSourceExpanded.Should().BeFalse();
    }

    [Fact]
    public void ScanFiles_掃描結果為空_IsSourceExpanded應維持展開()
    {
        var scanner = Substitute.For<IFileScanner>();
        scanner.Scan(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<bool>())
            .Returns(new FileScanResult([], []));
        var sut = CreateSut(scanner);
        sut.SelectedFolder = @"C:\imgs";

        sut.ScanFilesCommand.Execute(null);

        sut.IsSourceExpanded.Should().BeTrue();
    }

    [Fact]
    public void ToggleSource_應切換IsSourceExpanded()
    {
        var sut = CreateSut(Substitute.For<IFileScanner>());
        sut.IsSourceExpanded = false;

        sut.ToggleSourceCommand.Execute(null);

        sut.IsSourceExpanded.Should().BeTrue();
    }

    private static MainViewModel CreateSut(IFileScanner scanner) => new(
        scanner,
        Substitute.For<IFrameDeletePlanner>(),
        Substitute.For<IRenamePlanner>(),
        Substitute.For<IFileOperationExecutor>(),
        Substitute.For<IFolderPickerService>(),
        Substitute.For<IImageResizeExecutor>(),
        Substitute.For<IResizePreviewService>(),
        Substitute.For<IOutputFolderResolver>(),
        Substitute.For<IDenoisePlanner>(),
        Substitute.For<IDenoiseExecutor>(),
        Substitute.For<IDenoisePreviewService>(),
        Substitute.For<IFileExistenceService>(),
        Substitute.For<IFileImportService>(),
        Substitute.For<IUpdateService>(),
        Substitute.For<IExternalLinkService>());
}
```

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test FrameFileTool.Tests/FrameFileTool.Tests.csproj --filter MainViewModelSourceCollapseTests -nologo`
Expected: 編譯失敗 / 測試失敗（`IsSourceExpanded`、`HasSource`、`ToggleSourceCommand` 尚不存在）

- [ ] **Step 3: 在 MainViewModel 新增狀態**

在「UI 狀態」屬性區（約第 64 行 `_includeSubfolders` 之後或 134 行 `_isLogExpanded` 附近）新增：

```csharp
/// <summary>來源區是否展開。未設來源時維持展開作為空狀態引導；成功掃描後自動收合。</summary>
[ObservableProperty]
private bool _isSourceExpanded = true;

/// <summary>是否已有來源檔案，驅動來源區顯示 chip（true）或空狀態引導（false）。</summary>
public bool HasSource => Files.Count > 0;
```

在建構子（約第 175 行 `DenoiseTool = ...` 之後）掛上 Files 變動通知：

```csharp
Files.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSource));
```

新增切換命令（與其他 `[RelayCommand]` 同區，例如檔案末段命令區）：

```csharp
[RelayCommand]
private void ToggleSource() => IsSourceExpanded = !IsSourceExpanded;
```

- [ ] **Step 4: 掃描成功後自動收合**

在 `RefreshScanFilesCore`（約第 233 行 `RefreshCommands();` 之前）加入：

```csharp
// 掃描到檔案後自動收合來源區，把空間還給預覽；空結果維持展開引導使用者。
if (Files.Count > 0)
{
    IsSourceExpanded = false;
}
```

- [ ] **Step 5: 跑測試確認通過**

Run: `dotnet test FrameFileTool.Tests/FrameFileTool.Tests.csproj --filter MainViewModelSourceCollapseTests -nologo`
Expected: `通過: 5`

- [ ] **Step 6: 跑全部測試確認無回歸**

Run: `dotnet test FrameFileTool.Tests/FrameFileTool.Tests.csproj -nologo`
Expected: `通過: 314`

- [ ] **Step 7: 提交**

```bash
git add FrameFileTool/ViewModels/MainViewModel.cs FrameFileTool.Tests/ViewModels/MainViewModelSourceCollapseTests.cs
git commit -m "feat(vm): 新增來源區收合狀態與掃描後自動收合"
```

---

### Task 3: 刪除自製頁首並調整 RowDefinitions

**Files:**

- Modify: `FrameFileTool/MainWindow.xaml`（頁首約第 34–50 行、RowDefinitions 約第 26–32 行）

- [ ] **Step 1: 刪除頁首 Border**

刪除 `<!-- ── 頁首：產品識別 -->` 整個 `Grid.Row="0"` 的 `Border`
（約第 34–50 行，含 DockPanel/Image/TextBlock）。產品識別交給 Window 既有的 `Title` 與 `Icon`。

- [ ] **Step 2: 調整外層 RowDefinitions 為 4 列**

將外層 `Grid.RowDefinitions`（約第 26–32 行）改為：

```xml
<Grid.RowDefinitions>
    <RowDefinition Height="Auto" />  <!-- Row 0：更新通知 -->
    <RowDefinition Height="Auto" />  <!-- Row 1：來源區 -->
    <RowDefinition Height="*" />     <!-- Row 2：TabControl 主內容 -->
    <RowDefinition Height="Auto" />  <!-- Row 3：Log -->
</Grid.RowDefinitions>
```

- [ ] **Step 3: 重新指派各區 Grid.Row**

- 更新通知 Border：`Grid.Row="1"` → `Grid.Row="0"`
- 來源 Border（目前 `Grid.Row="3"`）：暫時改 `Grid.Row="1"`（Task 4 會整個改寫，這步只先讓版面不錯位）
- TabControl：`Grid.Row="2"` 維持不變
- Log Border：`Grid.Row="4"` → `Grid.Row="3"`

同時把來源 Border 的 `Margin="0,10,0,0"` 改為 `Margin="0,0,0,10"`（從底部間距改為與下方 TabControl 的間距）。

- [ ] **Step 4: build 驗證**

Run: `dotnet build FrameFileTool/FrameFileTool.csproj -nologo`
Expected: `建置成功`

- [ ] **Step 5: 手動驗收**

Run: `dotnet run --project FrameFileTool/FrameFileTool.csproj`
確認：無自製頁首（只剩 Windows 標題列）；由上而下為 更新通知 → 來源列 → 分頁 → Log；視窗不錯位。關閉視窗。

- [ ] **Step 6: 提交**

```bash
git add FrameFileTool/MainWindow.xaml
git commit -m "feat(view): 移除與原生標題列重複的自製頁首，來源列移至頂部"
```

---

### Task 4: 來源區改為三態（空 / chip / 展開）

**Files:**

- Modify: `FrameFileTool/MainWindow.xaml`（來源 `Grid.Row="1"` 的 Border）

- [ ] **Step 1: 以三態 Grid 取代來源 Border 內容**

將來源 Border 的內容替換為下列結構：chip（收合且 `HasSource`）、展開橫列
（`IsSourceExpanded`）、空狀態（無來源）三者互斥。沿用既有綁定
`SelectedFolder`/`FileSummary`/`Include*`/命令；
新增 `ToggleSourceCommand`、`IsSourceExpanded`、`HasSource`。

需要既有轉換器 `BoolToVis`（已存在，見現有 XAML）。chip 的「有來源且未展開」
條件用 `MultiDataTrigger` 切 `Visibility`，避免新增反向轉換器。

替換來源 `Border` 的子節點為：

```xml
<Grid>
    <!-- 收合 chip：有來源且未展開 -->
    <Border Padding="7,6" CornerRadius="6"
            Background="{StaticResource SurfaceSubtleBrush}"
            BorderBrush="{StaticResource CardBorderBrush}"
            BorderThickness="1">
        <Border.Style>
            <Style TargetType="Border">
                <Setter Property="Visibility" Value="Collapsed" />
                <Style.Triggers>
                    <MultiDataTrigger>
                        <MultiDataTrigger.Conditions>
                            <Condition Binding="{Binding HasSource}" Value="True" />
                            <Condition Binding="{Binding IsSourceExpanded}" Value="False" />
                        </MultiDataTrigger.Conditions>
                        <Setter Property="Visibility" Value="Visible" />
                    </MultiDataTrigger>
                </Style.Triggers>
            </Style>
        </Border.Style>
        <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
            <TextBlock Text="📁" Margin="0,0,8,0" />
            <TextBlock Text="{Binding FileSummary}" FontWeight="SemiBold" />
            <TextBlock Text="{Binding SelectedFolder}"
                       Foreground="{StaticResource TextSecondary}"
                       FontSize="12" Margin="12,0,0,0"
                       TextTrimming="CharacterEllipsis" MaxWidth="520" />
            <Button Content="✎ 變更"
                    Command="{Binding ToggleSourceCommand}"
                    Margin="14,0,0,0" />
        </StackPanel>
    </Border>

    <!-- 展開橫列：IsSourceExpanded（含空狀態，因空狀態強制展開） -->
    <Grid Visibility="{Binding IsSourceExpanded, Converter={StaticResource BoolToVis}}">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto" />
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="Auto" />
            <ColumnDefinition Width="Auto" />
            <ColumnDefinition Width="Auto" />
            <ColumnDefinition Width="Auto" />
        </Grid.ColumnDefinitions>

        <TextBlock Grid.Row="0" Text="來源資料夾" FontWeight="SemiBold" Margin="0,0,10,0"
                   VerticalAlignment="Center" />
        <TextBox Grid.Row="0" Grid.Column="1" Margin="0,0,8,0" VerticalAlignment="Center"
                 Text="{Binding SelectedFolder, UpdateSourceTrigger=PropertyChanged}" />
        <Button Grid.Row="0" Grid.Column="2" Content="選擇資料夾" Margin="0,0,6,0"
                Command="{Binding BrowseFolderCommand}" />
        <Button Grid.Row="0" Grid.Column="3" Content="重新掃描"
                Command="{Binding ScanFilesCommand}" />
        <Button Grid.Row="0" Grid.Column="4" Content="清空全部" Margin="6,0,0,0"
                Command="{Binding ClearFolderAndFilesCommand}" />
        <Button Grid.Row="0" Grid.Column="5" Content="▲ 收合" Margin="6,0,0,0"
                Command="{Binding ToggleSourceCommand}"
                Visibility="{Binding HasSource, Converter={StaticResource BoolToVis}}" />

        <TextBlock Grid.Row="1" Text="掃描格式" FontWeight="SemiBold" Margin="0,8,10,0" />
        <WrapPanel Grid.Row="1" Grid.Column="1" Grid.ColumnSpan="5" Margin="0,8,0,0"
                   VerticalAlignment="Center">
            <CheckBox Content=".png"  Style="{StaticResource PillCheckBox}" IsChecked="{Binding IncludePng}" />
            <CheckBox Content=".jpg"  Style="{StaticResource PillCheckBox}" IsChecked="{Binding IncludeJpg}" />
            <CheckBox Content=".jpeg" Style="{StaticResource PillCheckBox}" IsChecked="{Binding IncludeJpeg}" />
            <CheckBox Content=".webp" Style="{StaticResource PillCheckBox}" IsChecked="{Binding IncludeWebp}" />
            <CheckBox Content=".bmp"  Style="{StaticResource PillCheckBox}" IsChecked="{Binding IncludeBmp}" />
            <CheckBox Content="包含子資料夾" Style="{StaticResource PillCheckBox}" MinWidth="110"
                      IsChecked="{Binding IncludeSubfolders}" />
        </WrapPanel>
    </Grid>
</Grid>
```

> 註：空狀態（無來源）在此實作中等同「展開橫列且尚未掃到檔案」，`FileSummary` 預設顯示「尚未掃描」，「▲ 收合」按鈕隱藏，使用者只能選資料夾＋掃描。spec 的拖放引導已由既有預覽區拖放覆蓋層提供，本區不重複做拖放框。

- [ ] **Step 2: build 驗證**

Run: `dotnet build FrameFileTool/FrameFileTool.csproj -nologo`
Expected: `建置成功`

- [ ] **Step 3: 手動驗收三態**

Run: `dotnet run --project FrameFileTool/FrameFileTool.csproj`
確認：

1. 啟動（無來源）→ 顯示展開橫列、無「▲ 收合」、`FileSummary`=「尚未掃描」。
2. 選資料夾並掃描出檔案 → 自動收合成 chip（`📁 已掃描 N 個圖片檔　路徑　✎ 變更`）。
3. 點「✎ 變更」→ 展開橫列、出現「▲ 收合」；點「▲ 收合」→ 回 chip。
4. 切換 4 個分頁 → chip／展開狀態保持不變。
5. 視窗縮到 `MinWidth=900` → 格式 pills 換行不溢出。

關閉視窗。

- [ ] **Step 4: 提交**

```bash
git add FrameFileTool/MainWindow.xaml
git commit -m "feat(view): 來源區改為頂部漸進式收合（空/chip/展開三態）"
```

---

### Task 5: 同步文件與 TODO

**Files:**

- Modify: `UI_UX_DESIGN_RULES.md`、`TODO.md`

- [ ] **Step 1: 更新 UI_UX_DESIGN_RULES 資訊架構**

把「主視窗區塊順序」與「掃描設定列位於 TabControl 下方」改為：
順序為 更新通知 → 來源（頂部漸進收合）→ TabControl → Log；來源區依狀態顯示 chip 或展開橫列，無自製頁首（識別由原生標題列負責）。

- [ ] **Step 2: 將 shared-source-bar 標記為被取代**

在 `TODO.md` 的 `shared-source-bar` 任務區塊頂部加註：

```markdown
> ⛔ 已被 docs/superpowers/specs/2026-06-23-main-layout-redesign-design.md 取代。
> 改為頂部漸進式收合來源，不再放入各 TabItem 底部。原任務作廢。
```

- [ ] **Step 3: markdownlint 驗證**

Run: `npx markdownlint-cli2 "*.md" "docs/**/*.md"`
Expected: `0 error(s)`

- [ ] **Step 4: 提交**

```bash
git add UI_UX_DESIGN_RULES.md TODO.md
git commit -m "docs: 更新資訊架構並作廢 shared-source-bar，改為頂部漸進收合來源"
```

---

### Task 6: 收尾驗證

- [ ] **Step 1: 全套驗證**

Run: `dotnet build FrameFileTool/FrameFileTool.csproj -nologo` → `建置成功`
Run: `dotnet test FrameFileTool.Tests/FrameFileTool.Tests.csproj -nologo` → `通過: 314`
Run: `dotnet format --verify-no-changes --severity warn` → 無變更
Run: `npx markdownlint-cli2 "*.md" "docs/**/*.md"` → `0 error(s)`

- [ ] **Step 2: 提交 spec 與 plan 文件（若尚未提交）**

```bash
git add docs/superpowers/specs/2026-06-23-main-layout-redesign-design.md docs/superpowers/plans/2026-06-23-main-layout-redesign.md
git commit -m "docs: 新增主視窗版面優化 spec 與實作計畫"
```

---

## Self-Review

- **Spec coverage：** 砍頁首 → Task 3；來源頂部漸進收合三態 → Task 2（狀態）+ Task 4（View）；
  版面順序 → Task 3；切分頁狀態保持 → Task 4 Step 3.4；MinWidth pills 換行 → Task 4 Step 3.5；
  取代 shared-source-bar → Task 5。預覽 ×4 重複明確不在範圍（spec 已聲明）。✅ 無缺漏。
- **Placeholder scan：** 無 TBD／TODO；測試與 XAML 皆給完整 code。✅
- **Type consistency：** `IsSourceExpanded`、`HasSource`、`ToggleSourceCommand` 在 Task 2 定義、
  Task 4 綁定，名稱一致；`BoolToVis`、`PillCheckBox`、`SurfaceSubtleBrush`、`CardBorderBrush`、
  `TextSecondary` 皆為既有資源。✅
- **測試數：** 現有 309 + 新增 5 = 314，全程一致。✅
