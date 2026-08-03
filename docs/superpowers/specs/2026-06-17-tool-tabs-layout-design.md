# 工具分頁導航設計文件

日期：2026-06-17
議題 ID：`tool-tabs-layout`

## 背景

使用者反饋希望每個工具獨立一個分頁，像資料夾標籤切換，
取代現有左側 RadioButton 2×2 格的工具選擇方式。

## 決策

| 項目 | 決定 |
| ---- | ---- |
| 佈局方案 | 方案 C：全寬 TabControl，每個 TabItem 內維持左右分欄（參數左 / 預覽右） |
| 標籤樣式 | 樣式 B：底線強調，選中分頁藍色 2px 底線 + SemiBold，未選中灰色文字 |
| 執行中切換 | 允許，IsBatchExecuting 不鎖定分頁 |

## 架構

### XAML 結構（MainWindow.xaml）

`Grid Row="3"` 的 3 欄佈局替換為 `TabControl`：

```text
現在
  ┌──────────────┬────┬──────────────────────┐
  │ 左側 290px   │ 12 │ 右側 *（預覽）       │
  └──────────────┴────┴──────────────────────┘

改後
  ┌──────────────────────────────────────────┐
  │ TabControl                               │
  │  ┌─────────┬────────┬────────┬────────┐  │
  │  │抽幀刪除 │批次改名│批次縮放│批次降噪│  │
  │  └─────────┴────────┴────────┴────────┘  │
  │  ┌──────────────┬──────────────────────┐  │
  │  │ 左側 290px  │ 右側 *（預覽）       │  │
  │  └──────────────┴──────────────────────┘  │
  └──────────────────────────────────────────┘
```

每個 `TabItem` 的 Content 為兩欄 `Grid`：

- 左欄（290px）：對應工具的 `ScrollViewer` + 參數面板
- 右欄（*）：現有預覽摘要列 + `ContentControl`（DataTemplate dispatch 不動）

**移除：**

- `UniformGrid` RadioButton 工具選擇器（4 顆）
- 4 個疊層 `ScrollViewer` 上的 `EnumToVisibilityConverter` binding（TabItem 自動控制可見性）
- 工具選擇用的 `EnumToBoolConverter` binding（4 個）

**保留不動：**

- 預覽 `ContentControl` + `PreviewTemplates.xaml` 中的所有 DataTemplate
- 工具面板內的 RadioButton（輸出模式）
- 掃描列（`Grid Row="2"`）
- Log（`Grid Row="4"`）
- 更新 Banner（`Grid Row="1"`）

### 樣式（MainWindowStyles.xaml）

新增：

- `TabControl` 樣式：移除預設邊框與 padding，背景透明
- `TabItem` 樣式：
  - 預設（未選中）：灰色文字，無底線，hover 略深
  - 選中：深色文字 + `FontWeight="SemiBold"` + 藍色 2px 底線
  - 分頁列白底，底部 1px `CardBorderBrush` 分隔線

移除：`ToolTabButton` RadioButton 樣式（不再使用）

### ViewModel（MainViewModel.cs）

新增 `SelectedToolIndex` int 屬性，供 `TabControl.SelectedIndex` binding 使用：

```csharp
public int SelectedToolIndex
{
    get => (int)SelectedTool;
    set => SelectedTool = (PreviewTool)value;
}
```

`OnSelectedToolChanged` 補一行：

```csharp
OnPropertyChanged(nameof(SelectedToolIndex));
```

**不動：**

- `SelectedTool` enum 本身
- `OnSelectedToolChanged` 所有既有邏輯
- 所有 ToolViewModel
- 所有測試

### 文件（UI_UX_DESIGN_RULES.md）

「資訊架構」章節更新，納入分頁導航模式：

- 工具以全寬 TabControl 呈現，每個工具一個 TabItem
- 掃描設定與 Log 維持在 TabControl 上下，所有分頁共用
- 切換分頁時既有預覽若屬不同工具則清除（現有邏輯保留）

## 範圍邊界

**本次不包含：**

- 新增任何工具
- 修改工具參數面板內容
- 修改預覽表格欄位
- 調整掃描流程

## 影響範圍

```text
MainWindow.xaml
MainWindowStyles.xaml
MainViewModel.cs（小改）
UI_UX_DESIGN_RULES.md
```

不影響任何 Service / Planner / Model / 測試。
