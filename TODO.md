# TODO

本文件追蹤目前已確認但尚未完成的功能計劃。
執行功能開發前，請先確認本文件是否有相關項目，並在完成後更新狀態。

**新增任務規範：** 每個新加入本文件的功能，必須先通過 [TODO_SPEC.md](./TODO_SPEC.md) 的五步驟流程：
分析可執行度 → 分析優先度 → 拆解任務 → 定義驗證方法 → 標記注意事項。
未通過規範的任務不可直接進入近期功能計劃。

執行本文件項目時，必須確實同步更新本文件：

- 開始執行某個功能群組時，將該群組正在處理的項目標記為 `[~]`。
- 每完成一個子項目，立即將該項目更新為 `[x]`，不可等到整批功能結束才回填。
- 若實作過程發現範圍、風險或完成判定需要調整，必須在同一輪變更中更新本文件。
- 若某項目暫停、拆分或延後，必須保留未完成狀態，並補上原因或後續處理方向。
- 每次 commit 前，必須確認本文件狀態與實際完成內容一致。
- **完成判定的 `[x]` 只能由開發者親自確認後標記，不得由 agent 代為勾選。**
  所有完成判定均標記為 `[x]` 後，才可將該功能移入 [DONE.md](./DONE.md)。

## 狀態標記

- `[ ]` 尚未開始。
- `[~]` 進行中。
- `[x]` 已完成並通過驗證。

## 近期功能計劃

### 2. 可勾選要變動的檔案

優先度：高
分支：feat/checkbox（開始時建立）
前置條件：無
被依賴：功能 3（executor 需識別 `IsIncluded` 篩選條件）

⚠ 影響範圍：`OperationPreviewItem` → 三個 `PreviewViewModel` →
`MainViewModel.HasExecutable*Preview` → 三個 DataTemplate → `FileOperationExecutor` → `ImageResizeExecutor`

⚠ 邊界案例：全部取消勾選、錯誤列的 checkbox 狀態、只剩一個項目時取消勾選

⚠ 待確認：`OperationPreviewItem` 目前為 `init-only` immutable class，`IsIncluded` 需要可寫屬性供 WPF checkbox binding。
決策：在 `OperationPreviewItem` 加入可寫屬性，或為每個項目建立 per-item wrapper ViewModel？

- [ ] [Model] 依設計決策調整 `OperationPreviewItem`，加入可寫的 `IsIncluded` 屬性；預設值依 `HasError` 決定（有錯誤則為 false）。
- [ ] [PreviewViewModel] 三個 PreviewViewModel 的 `Summary` 與 `HasErrors` 改依已勾選項目計算。
- [ ] [Test] 補上三個 PreviewViewModel 摘要隨勾選狀態變化的測試。
- [ ] [MainViewModel] 調整 `HasExecutable*Preview()`：無任何已勾選且可執行項目時回傳 false。
- [ ] [Test] 補上 CanExecute 隨勾選狀態變化的測試。
- [ ] [Service] `FileOperationExecutor` 與 `ImageResizeExecutor` 執行前過濾 `IsIncluded = false` 的項目。
- [ ] [Test] 補上 executor 只執行已勾選且無錯誤項目的測試。
- [ ] [View] 三個工具的 DataTemplate 加入 checkbox 欄位，繫結 `IsIncluded`；錯誤列 checkbox 停用。

完成判定：

- [ ] [正常路徑] 取消勾選單一項目後點擊執行，該項目對應的檔案操作不會被執行。
- [ ] [邊界] 全部取消勾選時，摘要顯示「0 個項目」且執行按鈕停用。
- [ ] [錯誤狀態] 錯誤列的 checkbox 為停用狀態，即使資料列顯示也不會被執行。
- [ ] [一致性] 抽幀刪除、批次改名、批次縮放三個工具勾選行為一致，摘要文字隨勾選同步更新。

### 3. 抽幀刪除、批次改名、批次縮放均可指定輸出資料夾

優先度：中
分支：feat/output-folder（開始時建立）
前置條件：功能 2 已完成（executor 需能識別 `IsIncluded` 後再套用輸出資料夾邏輯）
被依賴：無

⚠ 影響範圍：`FrameDeletePlanner` / `RenamePlanner` / `ResizePlanner` / `ResizeOptions` /
`ResizeOutputMode` → 對應 executor → MainViewModel → 三個操作面板 View

⚠ 邊界案例：目標資料夾路徑含非法字元、目標資料夾不存在、同名檔案輸出至同一資料夾

⚠ 待確認：目標資料夾不存在時，執行前自動建立，還是顯示錯誤並停用執行按鈕？（完成判定第二條預設為自動建立，需確認後同步子任務）

#### 抽幀刪除

- [ ] [Model] 新增或擴充輸出模式，加入「複製保留幀到指定資料夾」選項。
- [ ] [Service] 調整 `FrameDeletePlanner`：檢查指定資料夾路徑是否合法，偵測目標衝突。
- [ ] [Test] 補上 `FrameDeletePlannerTests`：合法路徑、非法路徑、目標衝突的測試。
- [ ] [Service] 調整 `FileOperationExecutor`：支援複製保留幀到指定資料夾的執行路徑，目標資料夾不存在時自動建立。
- [ ] [Test] 補上 executor 指定資料夾路徑的測試：正常執行、建立資料夾、衝突處理。
- [ ] [View] 抽幀刪除操作面板加入輸出資料夾選項與選擇按鈕。

#### 批次改名

- [ ] [Model] 新增或擴充輸出模式，加入「複製改名到指定資料夾」選項。
- [ ] [Service] 調整 `RenamePlanner`：檢查指定資料夾路徑是否合法，偵測目標衝突。
- [ ] [Test] 補上 `RenamePlannerTests`：合法路徑、非法路徑、目標衝突的測試。
- [ ] [Service] 調整 `FileOperationExecutor`：支援複製改名到指定資料夾的執行路徑，目標資料夾不存在時自動建立。
- [ ] [Test] 補上 executor 指定資料夾路徑的測試。
- [ ] [View] 批次改名操作面板加入輸出資料夾選項與選擇按鈕。

#### 批次縮放

- [ ] [Model] 擴充 `ResizeOutputMode`，加入指定資料夾輸出模式。
- [ ] [Model] 擴充 `ResizeOptions`，加入指定輸出資料夾路徑欄位。
- [ ] [Service] 調整 `ResizePlanner`：檢查指定資料夾路徑是否合法，偵測同名衝突。
- [ ] [Test] 補上 `ResizePlannerTests`：合法路徑、非法路徑、同名衝突的測試。
- [ ] [Service] 調整 `ImageResizeExecutor`：輸出到指定資料夾，目標資料夾不存在時自動建立。
- [ ] [Test] 補上 `ImageResizeExecutorTests` 指定資料夾路徑的測試。
- [ ] [MainViewModel] 指定資料夾路徑變更時使既有預覽失效。
- [ ] [View] 批次縮放輸出位置加入「指定資料夾」選項與選擇按鈕。

完成判定：

- [ ] [正常路徑] 三個工具均可選擇指定輸出資料夾並產生對應預覽。
- [ ] [邊界] 目標資料夾不存在時，執行前自動建立；建立失敗時 log 記錄錯誤且該項目標記失敗。
- [ ] [錯誤狀態] 目標檔已存在或同名衝突時，預覽標示錯誤且執行按鈕停用。
- [ ] [一致性] 批次縮放：覆寫原檔、另存子資料夾、指定資料夾三種輸出模式都維持可用。

### 4. 批次縮放改用倍率輸入

優先度：中
分支：feat/scale-factor（開始時建立）
前置條件：無（獨立改動，不依賴其他功能）
被依賴：功能 3 若同期進行需注意 `ResizeOptions` 欄位版本一致性

⚠ 影響範圍：`ResizeOptions.ScalePercent` (int) → `ScaleFactor` (double) →
`ResizePlanner` → `ResizePreviewService` → `ImageResizeExecutor` →
`MainViewModel._scalePercent` → `MainWindow.xaml` → `ResizePlannerTests` → `ResizePreviewServiceTests`

⚠ 邊界案例：倍率輸入 0、負數、非數字字串；倍率 0.5 時 1px 下限保護

- [ ] [Model] 將 `ResizeOptions.ScalePercent` (int) 改為 `ScaleFactor` (double)。
- [ ] [Service] 調整 `ResizePlanner` 驗證規則：`ScaleFactor` 必須大於 0。
- [ ] [Service] 調整 `ResizePlanner` 目標尺寸計算，改用 `ScaleFactor` 乘以原始尺寸。
- [ ] [Test] 補上 `ResizePlannerTests`：`ScaleFactor` ≤ 0、等於 1、0.5、2 的驗證與尺寸計算測試。
- [ ] [Service] 調整 `ResizePreviewService` 預覽文字，改用倍率語意（移除百分比用詞）。
- [ ] [Test] 補上 `ResizePreviewServiceTests`：預覽文字包含倍率而非百分比。
- [ ] [Service] 調整 `ImageResizeExecutor` 的 Magick.NET resize geometry 建立方式，改用 `ScaleFactor`。
- [ ] [MainViewModel] 將 `_scalePercent` (int) 改為 `_scaleFactor` (double)。
- [ ] [View] 調整 `MainWindow.xaml`：輸入欄位改為 double 類型，label 文案改為倍率說明。

完成判定：

- [ ] [正常路徑] UI 顯示倍率輸入欄位；倍率 `1` 產生與原圖相同的目標尺寸，倍率 `0.5` 產生約一半尺寸，倍率 `2` 產生兩倍尺寸。
- [ ] [邊界] 倍率 `0.5` 對 1px 來源圖計算結果至少保留 1px。
- [ ] [錯誤狀態] 輸入倍率 `0` 或負數時，預覽顯示錯誤訊息且執行按鈕停用。
- [ ] [清理] 舊的百分比文案、log 與錯誤訊息已全部改為倍率語意，無殘留百分比用詞。

### 5. 移除預覽按鈕，改為即時自動預覽

優先度：高（進行中）
分支：feat/live-preview
前置條件：無
被依賴：功能 6（`RemoveFileCommand` 移除後需觸發即時預覽更新）

⚠ 影響範圍：`MainViewModel`（移除三個手動預覽 command、新增 `PropertyChanged` 監聽與 debounce 機制）→ `MainWindow.xaml`（移除三個預覽按鈕）

⚠ 邊界案例：`Files` 為空時觸發預覽、縮放設定連續快速變更時的 debounce 行為、Tab 切換時目標工具尚無檔案

⚠ 待確認：批次縮放 debounce 的 unit test 策略——透過 mock timer / `IScheduler` 注入，還是驗證 `CancellationToken` 被取消？確認後統一測試做法。

- [~] [MainViewModel] 移除 `PreviewFrameDeleteCommand`、`PreviewRenameCommand`、`PreviewResizeCommand` 三個手動觸發指令。
- [~] [View] 移除 `MainWindow.xaml` 上的三個預覽按鈕。
- [~] [MainViewModel] 抽幀刪除與批次改名：監聽 `Files` 與相關設定的 `PropertyChanged`，同步觸發預覽計算。
- [~] [MainViewModel] 批次縮放：設定變更時以 debounce（300–500 ms）觸發非同步預覽，避免連續輸入時重複讀取圖片尺寸。
- [~] [MainViewModel] 批次縮放預覽計算中維持 `IsPreparingPreview` 狀態與摘要列提示。
- [~] [MainViewModel] 切換工具 Tab 時自動觸發對應工具的即時預覽。
- [~] [MainViewModel] 調整 `ExecuteXxx` 的 CanExecute：不再依賴手動觸發的預覽結果，改為依 `CurrentPreview` 是否有效且無錯誤。
- [~] [Test] 補上即時觸發邏輯的 ViewModel 測試（屬性變更 → 預覽更新、Tab 切換觸發預覽）。

完成判定：

- [ ] [正常路徑] 載入圖片後，三個工具各自的預覽自動顯示，不需點擊按鈕。
- [ ] [即時] 調整間隔、前綴、縮放倍率等設定時，預覽即時反映最新參數。
- [ ] [邊界] 切換工具 Tab 時，目標工具的預覽自動更新至最新狀態。
- [ ] [防抖] 批次縮放連續調整設定時不會同時發起多個非同步請求；預覽計算中顯示 loading 狀態，完成後自動解除。

### 6. 清空全部與剔除檔案功能

優先度：中
分支：feat/clear-remove（開始時建立）
前置條件：功能 5 已完成（`RemoveFileCommand` 移除後需觸發即時預覽更新）
被依賴：無

⚠ 影響範圍：`MainViewModel`（新增兩個 command）→ `MainWindow.xaml`（清空按鈕）→ 三個工具 DataTemplate（剔除欄位）

⚠ 邊界案例：剔除最後一個檔案後的預覽與按鈕狀態、清空後立即再次載入、`Files` 為空時點擊清空

- [ ] [MainViewModel] 新增 `ClearFolderAndFilesCommand`，清空 `SelectedFolder` 與 `Files`，並重設 `CurrentPreview`。
- [ ] [Test] 補上 `ClearFolderAndFilesCommand` 執行後 `SelectedFolder`、`Files`、`CurrentPreview` 全部重設，且執行按鈕停用的測試。
- [ ] [MainViewModel] 新增 `RemoveFileCommand(FileItem)`，從 `Files` 移除指定項目，移除後觸發即時預覽更新。
- [ ] [Test] 補上 `RemoveFileCommand`：移除後預覽更新、移除最後一個檔案後執行按鈕停用的測試。
- [ ] [View] `MainWindow.xaml` 加入「清空全部」按鈕，繫結 `ClearFolderAndFilesCommand`。
- [ ] [View] 三個工具的 DataTemplate 各自新增「剔除」欄位，繫結 `RemoveFileCommand`，傳入對應的 `FileItem`。

完成判定：

- [ ] [正常路徑] 點擊「清空全部」按鈕，來源路徑與檔案清單清空，預覽重設且執行按鈕停用。
- [ ] [正常路徑] 點擊預覽表格任意檔案的「剔除」按鈕，該檔案從清單移出，其他項目的預覽立即更新。
- [ ] [邊界] 剔除最後一個檔案後，三個工具的執行按鈕全部停用，預覽顯示空狀態。

## 通用驗證

每完成一個功能群組，都必須執行：

```powershell
dotnet test -p:UseAppHost=false
dotnet format --verify-no-changes --severity warn
npx markdownlint-cli2 "**/*.md"
```

若有修改 UI，也需要手動確認：

- 空狀態、錯誤狀態、忙碌狀態都能正確顯示。
- 預覽必須先產生，執行按鈕才可使用。
- 長檔名與大量檔案不造成表格排版破裂。
- Log 訊息能定位發生問題的檔案或資料夾。
