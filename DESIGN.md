# DESIGN.md

本文件說明 FrameFileTool 的架構設計、資料流與關鍵決策。
開發規則與 commit 規範請以 [AGENTS.md](AGENTS.md) 為唯一依據；
本文件專注於「為什麼這樣設計」與「各元件如何協作」。

## 目錄

- [系統目標](#系統目標)
- [分層架構](#分層架構)
- [元件說明](#元件說明)
- [資料流](#資料流)
- [關鍵設計決策](#關鍵設計決策)
- [測試策略](#測試策略)
- [目前限制與未來擴充](#目前限制與未來擴充)

---

## 系統目標

FrameFileTool 是一個 Windows WPF 桌面工具，用於處理序列圖檔的兩種批次操作：

| 功能     | 說明                                                     |
| -------- | -------------------------------------------------------- |
| 抽幀刪除 | 每 N 張刪除第 N 張，預設移至回收桶                       |
| 批次改名 | 以前綴 + 序號（可補零）重新命名，支援每個子資料夾各自計數 |

設計核心原則：**所有破壞性操作都必須先預覽、再執行**。

---

## 分層架構

```text
┌──────────────────────────────────────┐
│  Views (XAML + code-behind)          │  僅包含 layout、binding、DI 初始化
├──────────────────────────────────────┤
│  ViewModels                          │  UI 狀態、command 生命週期、log
├──────────────────────────────────────┤
│  Services / Interfaces               │  掃描、規劃、執行（各自獨立）
├──────────────────────────────────────┤
│  Models                              │  共用 immutable 資料物件
└──────────────────────────────────────┘
```

每一層只往下依賴，絕不往上。商業規則不出現在 Views 或 code-behind。

### 命名空間對應

| 命名空間                          | 路徑                              |
| --------------------------------- | --------------------------------- |
| `FrameFileTool`                   | `App.xaml.cs`、`MainWindow.xaml`  |
| `FrameFileTool.ViewModels`        | `ViewModels/`                     |
| `FrameFileTool.Services`          | `Services/`                       |
| `FrameFileTool.Services.Interfaces` | `Services/Interfaces/`          |
| `FrameFileTool.Models`            | `Models/`                         |

---

## 元件說明

### Models（資料物件）

所有模型皆為 immutable 或 init-only，建立後不可變更狀態。

| 類別                 | 類型          | 說明                                                        |
| -------------------- | ------------- | ----------------------------------------------------------- |
| `FileItem`           | `sealed record` | 一個已掃描圖片檔，含完整路徑、資料夾、檔名、副檔名、大小  |
| `OperationPreviewItem` | `sealed class` | 單一檔案的操作預覽，含動作、目標檔名、狀態、是否有錯誤    |
| `OperationAction`    | `static class` | 動作名稱常數：`刪除`、`保留`、`改名`、`錯誤`               |
| `OperationResult`    | `sealed class` | 執行結果，含成功筆數與錯誤訊息清單                          |

`OperationAction` 是消除魔法字串的關鍵，所有 planner、executor 與 ViewModel
都引用此常數，確保動作名稱一致且可重構。

### Services / Interfaces（服務層）

服務層分為三個職責群：

#### 掃描

| 介面 / 實作         | 方法簽章                                                                      |
| ------------------- | ----------------------------------------------------------------------------- |
| `IFileScanner`      | `Scan(folder, extensions, includeSubfolders) → IReadOnlyList<FileItem>`       |
| `FileScanner`       | 同上實作，使用 `NaturalStringComparer` 排序，先依資料夾、再依自然排序檔名      |
| `NaturalStringComparer` | 實作 `IComparer<string>`，數字段以數值大小比較，確保 `2.png` < `10.png`  |

#### 規劃（Pure Planner）

| 介面 / 實作           | 方法簽章                                                                          |
| --------------------- | --------------------------------------------------------------------------------- |
| `IFrameDeletePlanner` | `Plan(files, interval) → IReadOnlyList<OperationPreviewItem>`                     |
| `FrameDeletePlanner`  | 每個資料夾獨立計數，`index % interval == 0` 時標記刪除                            |
| `IRenamePlanner`      | `Plan(files, prefix, startIndex, padding) → IReadOnlyList<OperationPreviewItem>`  |
| `RenamePlanner`       | 每個資料夾獨立計數，偵測計畫內重複目標與計畫外已存在檔案                          |

兩個 planner 都是 **pure function**：不讀寫檔案、不修改 shared state、
只接受輸入資料並回傳計畫物件。

#### 執行（副作用集中點）

| 介面 / 實作              | 方法                                   |
| ------------------------ | -------------------------------------- |
| `IFileOperationExecutor` | `DeleteToRecycleBin(items)`            |
|                          | `RenameFiles(items)`                   |
| `FileOperationExecutor`  | 同上實作，為唯一直接碰觸檔案系統的元件 |

#### UI 輔助

| 介面 / 實作          | 說明                                        |
| -------------------- | ------------------------------------------- |
| `IFolderPickerService` | 開啟 `FolderBrowserDialog`，回傳所選路徑  |
| `FolderPickerService`  | 需要 `UseWindowsForms = true` 才能運作     |

### ViewModels

`MainViewModel` 是唯一的 ViewModel，繼承 `CommunityToolkit.Mvvm` 的 `ObservableObject`。

| 職責     | 說明                                                               |
| -------- | ------------------------------------------------------------------ |
| UI 狀態  | 11 個 `[ObservableProperty]`，包含資料夾路徑、副檔名勾選、參數設定  |
| Commands | 6 個 `[RelayCommand]`，含 CanExecute 守衛                          |
| Log      | `ObservableCollection<string>`，最新訊息插入最上方                  |
| 委派     | 不直接操作檔案，全部委派給 service；不含排序或衝突演算法            |

CanExecute 守衛規則：

| Command                  | 條件                                          |
| ------------------------ | --------------------------------------------- |
| `PreviewFrameDeleteCommand` | `Files.Count > 0`                          |
| `ExecuteFrameDeleteCommand` | 預覽清單中有可執行刪除項目（無 error）     |
| `PreviewRenameCommand`      | `Files.Count > 0`                          |
| `ExecuteRenameCommand`      | 有可執行改名項目，且整個預覽清單無 error   |

### App.xaml.cs（DI 容器）

所有 service 與 ViewModel 在 `App` 建構時透過
`Microsoft.Extensions.DependencyInjection` 注冊為 `Singleton`。
`MainWindow` 透過建構子注入接收 `MainViewModel`，再設定 `DataContext`。

```text
App()
  └─ ConfigureServices()
       ├─ IFileScanner → FileScanner (Singleton)
       ├─ IFrameDeletePlanner → FrameDeletePlanner (Singleton)
       ├─ IRenamePlanner → RenamePlanner (Singleton)
       ├─ IFileOperationExecutor → FileOperationExecutor (Singleton)
       ├─ IFolderPickerService → FolderPickerService (Singleton)
       ├─ MainViewModel (Singleton)
       └─ MainWindow (Singleton)
```

---

## 資料流

### 抽幀刪除

```text
使用者點擊「選擇資料夾」
  → FolderPickerService.PickFolder()
  → 設定 SelectedFolder

使用者點擊「掃描」
  → FileScanner.Scan(folder, extensions, includeSubfolders)
  → 回傳 IReadOnlyList<FileItem>（自然排序）
  → 填入 Files (ObservableCollection)

使用者點擊「預覽抽幀」
  → FrameDeletePlanner.Plan(files, interval)
  → 回傳 IReadOnlyList<OperationPreviewItem>（僅含計畫，無副作用）
  → 填入 PreviewItems，顯示於預覽 DataGrid

使用者確認後點擊「執行」
  → FileOperationExecutor.DeleteToRecycleBin(previewItems)
  → 僅處理 Action == "刪除" && HasError == false 的項目
  → 回傳 OperationResult（成功筆數 + 錯誤清單）
  → ScanFiles() 重新掃描更新 UI
```

### 批次改名

```text
（掃描步驟同上）

使用者填入前綴、起始編號、補零位數
  → 點擊「預覽改名」
  → RenamePlanner.Plan(files, prefix, startIndex, padding)
  → 偵測：計畫內重複目標 / 計畫外已存在檔案 / 同名不處理
  → 填入 PreviewItems

若預覽無錯誤，使用者點擊「執行」
  → FileOperationExecutor.RenameFiles(previewItems)
  → 兩段式改名：
      第一階段：所有來源 → 暫存檔名（GUID）
      第二階段：所有暫存檔名 → 最終目標檔名
  → 回傳 OperationResult
  → ScanFiles() 重新掃描
```

---

## 關鍵設計決策

### 1. Pure Planner 模式

規劃邏輯（`FrameDeletePlanner`、`RenamePlanner`）完全不接觸檔案系統，
只接收資料並回傳計畫物件。

**好處：**

- 可在 unit test 中直接呼叫，無需 mock 檔案系統。
- 規劃邏輯與副作用隔離，可安全重構或替換。
- 確保「預覽即計畫」：執行階段直接使用預覽結果，不重新計算。

### 2. 兩段式改名（防止撞名鏈）

直接做 `A → B`、`B → C` 的改名，如果 B 已存在就會失敗。
FrameFileTool 的解法：

```text
第一階段：A → .__FrameFileTool_<GUID>.tmp
          B → .__FrameFileTool_<GUID>.tmp
第二階段：tmp1 → B
          tmp2 → C
```

這樣所有來源都先脫離原始名稱，消除順序依賴。

### 3. 自然排序（NaturalStringComparer）

標準字典序會讓 `frame10.png` 排在 `frame2.png` 前面。
`NaturalStringComparer` 在遇到數字段時改用數值大小比較：

```text
字典序：1.png, 10.png, 2.png, 20.png
自然序：1.png, 2.png, 10.png, 20.png
```

演算法要點：

- 逐字元掃描，遇到數字段時提取整段數字塊。
- 去除前導零後比較：`"007"` 與 `"7"` 視為相等。
- 先比數字塊長度（位數多者較大），再比字典序（位數相同時等同數值序）。

### 4. 每個子資料夾獨立計數

勾選「包含子資料夾」時，抽幀與改名都以資料夾為邊界各自重置計數器。

**抽幀**：`folderA` 的第 1 張是 `folderA` 裡的序號 1，不是全域序號。
**改名**：`folderA` 從 `F_0` 開始，`folderB` 也從 `F_0` 開始。

實作方式：`Dictionary<string, int>` 以資料夾路徑（大小寫不敏感）為鍵，
在迭代 `files` 清單時查詢並遞增。

### 5. OperationAction 常數（消除魔法字串）

動作名稱 `"刪除"` / `"保留"` / `"改名"` / `"錯誤"` 原本散落在多個檔案。
改用 `OperationAction` 靜態常數後：

- 重構動作名稱只需修改一處。
- 在比較邏輯（`item.Action == OperationAction.Delete`）中有 IDE 自動完成支援。
- 測試中仍可用字串字面值（`"刪除"`）驗證使用者看到的文字是否正確。

### 6. 衝突偵測（RenamePlanner）

RenamePlanner 在規劃階段偵測兩種衝突，避免執行時覆蓋非預期的檔案：

| 衝突類型         | 說明                                               | 狀態文字         |
| ---------------- | -------------------------------------------------- | ---------------- |
| 計畫內重複       | 兩個檔案的目標路徑相同                             | `目標檔名重複`   |
| 計畫外已存在     | 目標路徑對應的檔案存在，但不在本次來源清單中       | `目標檔案已存在` |
| 同名不處理       | 來源與目標路徑相同（原本就是目標檔名）             | `檔名相同，不處理` |

只要預覽清單中任一項目有 `HasError == true`，「執行改名」按鈕就維持停用。

### 7. DI + interface 設計（可測試性）

ViewModel 依賴 5 個 service 的 interface，而非具體類別。
測試時可用 `NSubstitute` 提供假實作，完全不需要真實的檔案系統。
未來新增功能（例如雲端儲存、不同刪除策略）也只需新增實作，不修改 ViewModel。

---

## 測試策略

測試專案：`FrameFileTool.Tests`（xUnit + FluentAssertions + NSubstitute）。

| 測試類別                    | 測試數 | 涵蓋範圍                                              |
| --------------------------- | ------ | ----------------------------------------------------- |
| `NaturalStringComparerTests` | 8     | 自然排序、null 邊界、數字塊、前導零、字典序 fallback  |
| `FrameDeletePlannerTests`    | 7     | 正常抽幀、邊界案例（interval ≤ 0）、子資料夾計數      |
| `RenamePlannerTests`         | 10    | 基本改名、補零、起始編號、子資料夾計數、同名保留      |

Pure planner 設計使得測試不需要任何 mock 或 stub，直接呼叫即可驗證。

### TDD 工作流程

```text
1. 撰寫或更新一個會失敗的 unit test。
2. 實作最小範圍的 production code 使測試通過。
3. 執行 dotnet test 確認通過。
4. 在測試維持通過的狀態下重構。
```

### 未覆蓋的測試範圍

目前以下邏輯尚無 unit test，依賴手動 GUI 驗證：

- `FileScanner`：需要真實的暫存目錄，建議以 integration test 補充。
- `FileOperationExecutor`：有真實檔案系統副作用，建議用暫存目錄做 integration test。
- `MainViewModel`：可用 NSubstitute mock 所有 service，未來可加入。
- `FolderPickerService`：屬於 UI 對話框，通常以 E2E 手動測試為主。

---

## 目前限制與未來擴充

### 目前限制

- 僅支援 Windows（WPF + `FolderBrowserDialog` + 回收桶 API）。
- 無法取消執行中的批次操作（現階段操作量小，尚可接受）。
- 執行期間不顯示進度條（未來可加 `IProgress<T>`）。

### 擴充指引

新增功能時，遵循下列模式可避免修改現有邏輯：

| 需求                   | 建議做法                                          |
| ---------------------- | ------------------------------------------------- |
| 新增圖片格式轉換       | 新增 `IConvertPlanner` + `ConvertPlanner`         |
| 支援永久刪除模式       | 新增 `IFileOperationExecutor` 的第二個實作        |
| 支援撤銷（Undo）       | 讓 executor 回傳 rollback plan，另建 `UndoService` |
| 多視窗 / 多功能頁籤    | 新增對應的 ViewModel 與 View，共用現有 services   |
| 設定檔持久化           | 新增 `ISettingsService`，注冊為 Singleton         |

每個新功能都應先有 unit test，規劃邏輯應遵循 pure planner 模式。
