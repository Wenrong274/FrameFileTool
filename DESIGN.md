# DESIGN.md

本文件是給 Claude Code 的設計決策指引。
當你要新增功能、修改邏輯或決定放在哪個類別時，依本文件判斷。
開發規則、套件清單與 commit 格式請以 [AGENTS.md](AGENTS.md) 為準。

---

## 新功能應該放在哪裡

```text
新的 UI 欄位或按鈕      → View (XAML) + ViewModel（屬性或 Command）
新的商業規則            → 新的 Service 或現有 Planner
新的檔案系統副作用      → FileOperationExecutor（唯一允許碰檔案的地方）
新的共用資料結構        → Models/
```

ViewModel 不包含演算法。Planner 不碰檔案系統。

---

## 新增一個操作功能的標準流程

1. 在 `Models/` 新增或確認輸入資料的 record（通常重用 `FileItem`）。
2. 在 `Services/Interfaces/` 定義 `IXxxPlanner`，方法簽章為：

   ```csharp
   IReadOnlyList<OperationPreviewItem> Plan(IReadOnlyList<FileItem> files, /* options */);
   ```

3. 在 `Services/` 實作 `XxxPlanner`：pure function，不讀寫檔案。
4. 若需要新的執行副作用，在 `IFileOperationExecutor` 新增方法，並在
   `FileOperationExecutor` 實作。
5. 在 `MainViewModel` 注入介面，新增對應的 `[ObservableProperty]`
   與 `[RelayCommand]`（含 `CanExecute`）。
6. 在 View 新增對應的控制項與 binding。
7. 先寫測試，再實作 planner。

---

## Planner 的設計規則

每個 Planner 必須是 **pure function**：

- 輸入：`IReadOnlyList<FileItem>` + options（值型別或 string）
- 輸出：`IReadOnlyList<OperationPreviewItem>`
- 禁止：`File.Exists()`、`Directory.*`、`File.Move()`、修改任何外部狀態

`OperationPreviewItem` 的 `Action` 欄位必須使用 `OperationAction` 常數，
不得用字串字面值。

子資料夾各自計數：使用 `Dictionary<string, int>` 以 `DirectoryPath`（大小寫不敏感）為鍵。

衝突偵測在 Planner 階段完成；有衝突的項目設 `HasError = true`，
Executor 會自動略過這些項目。

---

## ViewModel 的設計規則

`MainViewModel` 目前是唯一的 ViewModel。新功能優先加在這裡，
除非功能獨立到需要自己的視窗，才另開新的 ViewModel。

允許的行為：

- 新增 `[ObservableProperty]` 保存使用者輸入
- 新增 `[RelayCommand]` 觸發 preview / execute 流程
- 呼叫 service 並將結果填入 `ObservableCollection`
- 呼叫 `AddLog()` 記錄操作結果

不允許的行為：

- 自己計算哪些檔案要刪除或如何改名
- 直接呼叫 `File.*` 或 `Directory.*`
- 在 command 內嵌入排序或過濾邏輯

每個執行型 Command 必須有對應的 `CanExecute`，在沒有可執行項目時停用按鈕。

---

## 什麼時候新增 Interface

符合以下任一條件時才加 Interface：

- 測試需要 mock（例如 `IFolderPickerService` 因為會開對話框）
- 將來可能有多個實作（例如不同的刪除策略）
- ViewModel 依賴它，需要隔離副作用

純演算法類別（如 `NaturalStringComparer`、Planner）不需要 Interface，
因為可以直接在測試中實例化。

---

## 什麼時候新增 Model

以下情況在 `Models/` 新增或調整：

- 跨多個類別傳遞的資料結構 → `sealed record`（immutable）
- Planner 回傳給 Executor 的計畫項目 → 已有 `OperationPreviewItem`，優先擴充屬性
- 執行結果 → 已有 `OperationResult`，優先擴充
- 新的動作名稱 → 加在 `OperationAction` 靜態類別

不要為只在一個類別內部使用的資料建立 Model。

---

## 安全規則（不可違反）

- 任何刪除或改名操作，執行前必須先有 preview step。
- 刪除一律呼叫 `FileSystem.DeleteFile(..., RecycleOption.SendToRecycleBin)`，不得永久刪除。
- 改名必須走兩段式（先 → tmp，再 → 目標），避免撞名鏈。
- Executor 只處理 `HasError == false` 的項目。
- Planner 偵測到衝突時設 `HasError = true`，並在 `Status` 說明原因。
