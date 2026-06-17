# 工具分頁導航實作計劃

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 以 WPF `TabControl` 取代左側 RadioButton 2×2 格，實現全寬資料夾風格分頁導航。

**Architecture:** 主視窗 Row 3 的 3 欄 `Grid` 替換為 `TabControl`（4 個 `TabItem`），每個 `TabItem` 內部維持左右兩欄（工具參數 290px / 預覽 \*）。`SelectedTool` enum 透過新增的 `SelectedToolIndex` int 屬性與 `TabControl.SelectedIndex` 雙向綁定。

**Tech Stack:** .NET 10、WPF、CommunityToolkit.Mvvm、xUnit

---

## 異動檔案一覽

| 檔案 | 類型 | 說明 |
| ---- | ---- | ---- |
| `UI_UX_DESIGN_RULES.md` | Modify | 更新資訊架構章節 |
| `FrameFileTool/ViewModels/MainViewModel.cs` | Modify | 新增 `SelectedToolIndex` 屬性 |
| `FrameFileTool/Resources/MainWindowStyles.xaml` | Modify | 新增 TabControl/TabItem 樣式，移除 ToolTabButton |
| `FrameFileTool/MainWindow.xaml` | Modify | 以 TabControl 取代主內容 Grid |

---

## Task 1：更新 UI_UX_DESIGN_RULES.md

**Files:**

- Modify: `UI_UX_DESIGN_RULES.md`（資訊架構章節）

- [ ] **Step 1：定位並更新資訊架構段落**

找到「資訊架構」章節（目前列出 4 個區塊順序的部分），在「工具參數應依工具分組」前加入以下說明：

```markdown
工具以全寬 `TabControl` 呈現，每個工具獨立一個 `TabItem`，標籤列橫跨主要內容區頂部。
每個 `TabItem` 內部維持左右分欄：工具參數在左（290px），預覽摘要與預覽表格在右（\*）。
掃描設定列與 Log 區塊位於 `TabControl` 上下，所有分頁共用。
切換分頁時，若既有預覽屬於不同工具，預覽會自動清除（現有邏輯保留）。
```

- [ ] **Step 2：commit**

```bash
git add UI_UX_DESIGN_RULES.md
git commit -m "docs(ui): 更新資訊架構，納入 TabControl 分頁導航規則"
```

---

## Task 2：MainViewModel — 新增 SelectedToolIndex

**Files:**

- Modify: `FrameFileTool/ViewModels/MainViewModel.cs`（約 353 行附近）

- [ ] **Step 1：在 `_selectedTool` 欄位宣告後新增屬性**

找到：

```csharp
[ObservableProperty]
private PreviewTool _selectedTool = PreviewTool.FrameDelete;
```

在其後新增：

```csharp
/// <summary>
/// TabControl.SelectedIndex 的 int 包裝，對應 SelectedTool enum 值。
/// </summary>
public int SelectedToolIndex
{
    get => (int)SelectedTool;
    set => SelectedTool = (PreviewTool)value;
}
```

- [ ] **Step 2：在 `OnSelectedToolChanged` 最前面補通知**

找到：

```csharp
partial void OnSelectedToolChanged(PreviewTool value)
{
    if (value != PreviewTool.Resize)
```

改為：

```csharp
partial void OnSelectedToolChanged(PreviewTool value)
{
    OnPropertyChanged(nameof(SelectedToolIndex));
    if (value != PreviewTool.Resize)
```

- [ ] **Step 3：執行既有測試確認不破壞行為**

```powershell
dotnet test -p:UseAppHost=false
```

預期：所有測試通過，無新失敗。

- [ ] **Step 4：commit**

```bash
git add FrameFileTool/ViewModels/MainViewModel.cs
git commit -m "feat(vm): 新增 SelectedToolIndex，供 TabControl.SelectedIndex 雙向綁定"
```

---

## Task 3：MainWindowStyles — 新增 TabControl/TabItem 樣式

**Files:**

- Modify: `FrameFileTool/Resources/MainWindowStyles.xaml`

- [ ] **Step 1：在 DataGrid 樣式前插入 ToolTabControl 與 ToolTabItem 樣式**

找到：

```xml
<!-- ── DataGrid ──────────────────────────────────────── -->
```

在其前插入：

```xml
<!-- ── 工具 TabControl（分頁列 + 內容容器）──────────── -->
<Style x:Key="ToolTabControl" TargetType="TabControl">
    <Setter Property="Padding"          Value="0" />
    <Setter Property="BorderThickness"  Value="0" />
    <Setter Property="Background"       Value="Transparent" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="TabControl">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="*" />
                    </Grid.RowDefinitions>
                    <!-- 分頁標籤列：白底，底部分隔線 -->
                    <Border Grid.Row="0"
                            Background="{StaticResource SurfaceBrush}"
                            BorderBrush="{StaticResource CardBorderBrush}"
                            BorderThickness="0,0,0,1">
                        <TabPanel IsItemsHost="True"
                                  Background="Transparent" />
                    </Border>
                    <!-- 分頁內容區 -->
                    <ContentPresenter Grid.Row="1"
                                      ContentSource="SelectedContent" />
                </Grid>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>

<!-- ── 工具 TabItem（底線強調樣式 B）──────────────────── -->
<Style x:Key="ToolTabItem" TargetType="TabItem">
    <Setter Property="Padding"     Value="16,10" />
    <Setter Property="FontSize"    Value="12" />
    <Setter Property="Foreground"  Value="{StaticResource TextSecondary}" />
    <Setter Property="Background"  Value="Transparent" />
    <Setter Property="BorderThickness" Value="0" />
    <Setter Property="Cursor"      Value="Hand" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="TabItem">
                <Border x:Name="Root"
                        Background="Transparent"
                        BorderBrush="Transparent"
                        BorderThickness="0,0,0,2"
                        Padding="{TemplateBinding Padding}">
                    <ContentPresenter ContentSource="Header"
                                      HorizontalAlignment="Center"
                                      VerticalAlignment="Center" />
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsSelected" Value="True">
                        <Setter TargetName="Root"
                                Property="BorderBrush"
                                Value="{StaticResource AccentBrush}" />
                        <Setter Property="Foreground"
                                Value="{StaticResource TextPrimary}" />
                        <Setter Property="FontWeight" Value="SemiBold" />
                    </Trigger>
                    <MultiTrigger>
                        <MultiTrigger.Conditions>
                            <Condition Property="IsMouseOver" Value="True" />
                            <Condition Property="IsSelected" Value="False" />
                        </MultiTrigger.Conditions>
                        <Setter Property="Foreground"
                                Value="{StaticResource TextPrimary}" />
                    </MultiTrigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>

```

- [ ] **Step 2：移除 ToolTabButton RadioButton 樣式**

找到並刪除以下整個區塊（從 comment 到 `</Style>`）：

```xml
<!-- ── 工具選擇按鈕（RadioButton，2×2 格佈局）────────── -->
<Style x:Key="ToolTabButton" TargetType="RadioButton">
    ...（整個樣式區塊）...
</Style>
```

- [ ] **Step 3：commit**

```bash
git add FrameFileTool/Resources/MainWindowStyles.xaml
git commit -m "style: 新增 ToolTabControl/ToolTabItem 底線分頁樣式，移除 ToolTabButton"
```

---

## Task 4：MainWindow.xaml — 以 TabControl 取代主內容 Grid

**Files:**

- Modify: `FrameFileTool/MainWindow.xaml`（Row 3 主內容區，約 177–783 行）

這是整個計劃中改動最大的步驟，完整替換主內容區的 XAML。

- [ ] **Step 1：找到並刪除舊的主內容 Grid**

定位並刪除以下整段（約 177–783 行）：

```xml
<!-- ── 主內容：左側工具 + 右側預覽 ─────────────────── -->
<Grid Grid.Row="3">
    ...（整個 3 欄 Grid，包含左側 Border + 右側 Grid）...
</Grid>
```

- [ ] **Step 2：在原位插入新的 TabControl 結構**

說明：以下 `[抽幀刪除參數內容]`、`[批次改名參數內容]`、`[批次縮放參數內容]`、`[批次降噪參數內容]` 各自填入舊 ScrollViewer 內的 StackPanel，`[預覽區內容]` 填入舊右側 Grid 內的兩個子區塊（摘要列與預覽 Border）。

完整新結構：

```xml
<!-- ── 主內容：全寬 TabControl，每個 TabItem 維持左右分欄 ── -->
<TabControl Grid.Row="3"
            SelectedIndex="{Binding SelectedToolIndex, Mode=TwoWay}"
            Style="{StaticResource ToolTabControl}">

    <!-- ── 抽幀刪除 ─────────────────────────────────── -->
    <TabItem Header="抽幀刪除"
             Style="{StaticResource ToolTabItem}">
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="290" />
                <ColumnDefinition Width="12" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>

            <Border Grid.Column="0"
                    Style="{StaticResource Card}"
                    Padding="0"
                    ClipToBounds="True">
                <ScrollViewer VerticalScrollBarVisibility="Auto"
                              HorizontalScrollBarVisibility="Disabled">
                    <StackPanel Margin="12,10"
                                DataContext="{Binding FrameDeleteTool}"
                                Grid.IsSharedSizeScope="True">
                        [抽幀刪除參數內容]
                    </StackPanel>
                </ScrollViewer>
            </Border>

            <Grid Grid.Column="2">
                [預覽區內容]
            </Grid>
        </Grid>
    </TabItem>

    <!-- ── 批次改名 ─────────────────────────────────── -->
    <TabItem Header="批次改名"
             Style="{StaticResource ToolTabItem}">
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="290" />
                <ColumnDefinition Width="12" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>

            <Border Grid.Column="0"
                    Style="{StaticResource Card}"
                    Padding="0"
                    ClipToBounds="True">
                <ScrollViewer VerticalScrollBarVisibility="Auto"
                              HorizontalScrollBarVisibility="Disabled">
                    <StackPanel Margin="12,10"
                                DataContext="{Binding RenameTool}"
                                Grid.IsSharedSizeScope="True">
                        [批次改名參數內容]
                    </StackPanel>
                </ScrollViewer>
            </Border>

            <Grid Grid.Column="2">
                [預覽區內容]
            </Grid>
        </Grid>
    </TabItem>

    <!-- ── 批次縮放 ─────────────────────────────────── -->
    <TabItem Header="批次縮放"
             Style="{StaticResource ToolTabItem}">
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="290" />
                <ColumnDefinition Width="12" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>

            <Border Grid.Column="0"
                    Style="{StaticResource Card}"
                    Padding="0"
                    ClipToBounds="True">
                <ScrollViewer VerticalScrollBarVisibility="Auto"
                              HorizontalScrollBarVisibility="Disabled">
                    <StackPanel Margin="12,10"
                                DataContext="{Binding ResizeTool}">
                        [批次縮放參數內容]
                    </StackPanel>
                </ScrollViewer>
            </Border>

            <Grid Grid.Column="2">
                [預覽區內容]
            </Grid>
        </Grid>
    </TabItem>

    <!-- ── 批次降噪 ─────────────────────────────────── -->
    <TabItem Header="批次降噪"
             Style="{StaticResource ToolTabItem}">
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="290" />
                <ColumnDefinition Width="12" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>

            <Border Grid.Column="0"
                    Style="{StaticResource Card}"
                    Padding="0"
                    ClipToBounds="True">
                <ScrollViewer VerticalScrollBarVisibility="Auto"
                              HorizontalScrollBarVisibility="Disabled">
                    <StackPanel Margin="12,10"
                                DataContext="{Binding DenoiseTool}">
                        [批次降噪參數內容]
                    </StackPanel>
                </ScrollViewer>
            </Border>

            <Grid Grid.Column="2">
                [預覽區內容]
            </Grid>
        </Grid>
    </TabItem>

</TabControl>
```

**內容搬移對照表（MainWindow.xaml 行號）：**

| 佔位符 | 來源位置 | 說明 |
| ------ | -------- | ---- |
| `[抽幀刪除參數內容]` | 舊 238–291 行 ScrollViewer 內的 StackPanel 全部子元素 | 間隔、輸出模式 RadioButton、執行按鈕 |
| `[批次改名參數內容]` | 舊 293–379 行 ScrollViewer 內的 StackPanel 全部子元素 | 前綴、起始編號、補零位數、輸出模式、執行按鈕 |
| `[批次縮放參數內容]` | 舊 381–560 行 ScrollViewer 內的 StackPanel 全部子元素 | 縮放模式、倍率/尺寸、輸出位置、演算法、執行按鈕、進度區塊 |
| `[批次降噪參數內容]` | 舊 562–657 行 ScrollViewer 內的 StackPanel 全部子元素 | 降噪模式、放大比較、警示、執行按鈕、進度區塊 |
| `[預覽區內容]` | 舊 663–782 行 `Grid Grid.Column="2"` 的全部子元素 | RowDefinitions、預覽摘要 Border、預覽表格 Border（含 DragDrop 事件），四個 TabItem 各複製一份 |

注意：`[預覽區內容]` 中的 `<Grid.RowDefinitions>` 需放在四個 TabItem 的右側 `Grid` 內；DragDrop 事件（`DragEnter`、`DragOver`、`DragLeave`、`Drop`）四份相同，code-behind handler 照常工作。

- [ ] **Step 3：移除不再使用的 `EnumToVisibilityConverter` binding（工具面板 Visibility 用）**

確認沒有殘留的 `ConverterParameter=FrameDelete`、`ConverterParameter=Rename`、`ConverterParameter=Resize`、`ConverterParameter=Denoise` 作為 `ScrollViewer.Visibility` 的 binding（工具面板內的 output mode visibility binding 仍保留）。

- [ ] **Step 4：build 確認無 XAML 錯誤**

```powershell
dotnet build FrameFileTool/FrameFileTool.csproj
```

預期：Build succeeded，0 errors。

- [ ] **Step 5：commit**

```bash
git add FrameFileTool/MainWindow.xaml
git commit -m "feat(view): 以 TabControl 取代 RadioButton 工具選擇，實現全寬分頁導航"
```

---

## Task 5：驗證

- [ ] **Step 1：執行所有測試**

```powershell
dotnet test -p:UseAppHost=false
```

預期：所有測試通過。

- [ ] **Step 2：執行格式與 Markdown 檢查**

```powershell
dotnet format --verify-no-changes --severity warn
npx markdownlint-cli2 "*.md"
```

預期：無違規。

- [ ] **Step 3：手動執行 App 確認以下行為**

```powershell
dotnet run --project FrameFileTool/FrameFileTool.csproj
```

驗收清單：

- 啟動後預設顯示「抽幀刪除」分頁，底線強調可見
- 點擊各分頁標籤，對應工具參數顯示，其他工具不可見
- 切換分頁時，若先前有其他工具的預覽，預覽清除（摘要列顯示「載入圖片後將自動顯示預覽」）
- 掃描列（頂部）與 Log（底部）在所有分頁不變
- 縮放或降噪執行中，可自由切換至其他分頁；切換回執行中分頁，進度仍在
- 拖放圖片到預覽區，DragDrop 行為正常

- [ ] **Step 4：最終 commit（若 Step 3 發現需微調）**

若 Step 3 有任何 XAML 微調，補一個 fixup commit。
