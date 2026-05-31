# DONE

已完成並通過驗證的功能歸檔。
功能群組所有子任務與完成判定均為 `[x]` 後，從 [TODO.md](./TODO.md) 移入此處。
歸檔前必須先檢視實際程式與文件改動，將原 TODO 計劃更新為最終完成範圍，
再完整搬入此處。

## 發布版本標記

每個功能群組搬入本文件時，必須在 `完成日期` 下方加入 `發布版本` 欄位。

- 尚未發版時，標記為 `發布版本：未發布`。
- 發布後，回填實際版本號，例如 `發布版本：v1.2.0`。
- 若功能不單獨發布，而是併入後續版本，發版後仍回填實際包含該功能的版本號。
- 發布版本應以 git tag 是否包含該功能 commit 為準，不只依 release notes 文字判斷。

每次功能群組歸檔後，必須評估是否需要建立新版本發布。

- 新增使用者可見功能，或修正會影響檔案操作正確性的 bug，應優先評估發布。
- 純重構、測試補強或文件格式修正可延後發布，併入下一版的「開發品質」或「改善」。
- 評估結論需留下明確狀態：`發布版本：未發布`、`發布版本：vX.Y.Z`，或在 release notes
  草稿中說明併入哪一版。

---

## 抽幀刪除、批次改名、批次縮放均可指定輸出資料夾

ID：`output-folder`
完成日期：2026-05-31
發布版本：未發布

優先度：中
分支：feat/output-folder
前置條件：`checkbox` 已完成（已於 [DONE.md](./DONE.md) 歸檔）
被依賴：無

影響範圍：`FrameDeletePlanner` / `RenamePlanner` / `ResizePlanner` / `ResizeOptions` /
`ResizeOutputMode` → 對應 executor → MainViewModel → 三個操作面板 View

邊界案例：目標資料夾路徑含非法字元、目標資料夾不存在、同名檔案輸出至同一資料夾

實作結果：

- 新增 `FrameDeleteOutputMode` 與 `RenameOutputMode`，支援抽幀複製保留幀與複製改名。
- `OperationPreviewItem` 新增 `TargetPath`，讓 executor 使用完整目標路徑執行複製。
- 批次縮放不再提供另存子資料夾模式，只保留「覆寫原始」與「指定資料夾」。
- 抽幀刪除、批次改名與批次縮放的指定資料夾輸出，均改為按下執行時跳出資料夾選擇視窗。
- 執行時選完目標資料夾後會重新規劃並檢查目標檔已存在與同名衝突；有錯誤則停止執行。
- 目標資料夾不存在時由 executor 自動建立，建立或寫入失敗會寫入 log 的錯誤清單。
- README 已同步指定資料夾輸出的執行時選擇流程。

### 抽幀刪除

- [x] [Model] 新增 `FrameDeleteOutputMode`，加入「複製保留幀到指定資料夾」選項。
- [x] [Service] 調整 `FrameDeletePlanner`：檢查指定資料夾路徑是否合法，偵測目標衝突。
- [x] [Test] 補上 `FrameDeletePlannerTests`：合法路徑、非法路徑、目標衝突的測試。
- [x] [Service] 調整 `FileOperationExecutor`：支援複製保留幀到指定資料夾的執行路徑，目標資料夾不存在時自動建立。
- [x] [Test] 補上 executor 指定資料夾路徑的測試：正常執行、建立資料夾、衝突處理。
- [x] [View] 抽幀刪除操作面板加入輸出模式；複製模式在執行時選擇目標資料夾。

### 批次改名

- [x] [Model] 新增 `RenameOutputMode`，加入「複製改名到指定資料夾」選項。
- [x] [Service] 調整 `RenamePlanner`：檢查指定資料夾路徑是否合法，偵測目標衝突。
- [x] [Test] 補上 `RenamePlannerTests`：合法路徑、非法路徑、目標衝突的測試。
- [x] [Service] 調整 `FileOperationExecutor`：支援複製改名到指定資料夾的執行路徑，目標資料夾不存在時自動建立。
- [x] [Test] 補上 executor 指定資料夾路徑的測試。
- [x] [View] 批次改名操作面板加入輸出模式；複製模式在執行時選擇目標資料夾。

### 批次縮放

- [x] [Model] 調整 `ResizeOutputMode`，保留覆寫原始並加入指定資料夾輸出模式。
- [x] [Model] 擴充 `ResizeOptions`，加入指定輸出資料夾路徑欄位。
- [x] [Service] 調整 `ResizePlanner`：檢查指定資料夾路徑是否合法，偵測同名衝突。
- [x] [Test] 補上 `ResizePlannerTests`：合法路徑、非法路徑、同名衝突的測試。
- [x] [Service] 調整 `ImageResizeExecutor`：輸出到指定資料夾，目標資料夾不存在時自動建立。
- [x] [Test] 補上 `ImageResizeExecutorTests` 指定資料夾路徑的測試。
- [x] [MainViewModel] 指定資料夾輸出模式在執行時選擇目標資料夾並重新檢查預覽。
- [x] [View] 批次縮放輸出位置加入「指定資料夾」模式；目標資料夾在執行時選擇。

完成判定：

- [x] [正常路徑] 抽幀複製、複製改名與縮放指定資料夾輸出，均在執行時選擇資料夾並完成輸出。
- [x] [邊界] 目標資料夾不存在時，執行前自動建立；建立失敗時 log 記錄錯誤且該項目標記失敗。
- [x] [錯誤狀態] 目標檔已存在或同名衝突時，預覽標示錯誤且執行按鈕停用。
- [x] [一致性] 批次縮放：覆寫原檔、指定資料夾兩種輸出模式都維持可用。

## 批次縮放改用倍率輸入

ID：`scale-factor`
完成日期：2026-05-31
發布版本：未發布

優先度：中
分支：feat/scale-factor
前置條件：無
被依賴：`output-folder` 若同期進行需注意 `ResizeOptions` 欄位版本一致性

影響範圍：`ResizeOptions.ScalePercent` (int) → `ScaleFactor` (double) →
`ResizeMode.ScaleFactor` → `ResizePlanner` → `ResizePreviewService` →
`ImageResizeExecutor` → `MainViewModel._scaleFactor` → `MainWindow.xaml` →
`ResizePlannerTests` → `ResizePreviewServiceTests`

邊界案例：倍率輸入 0、負數、非數字字串；倍率 0.5 時 1px 下限保護。

實作結果：

- 批次縮放的比例設定由百分比整數改為倍率 `double`。
- `ResizePlanner`、`ResizePreviewService` 與 `ImageResizeExecutor` 均改用
  `ScaleFactor` 計算與顯示倍率語意。
- 倍率模式提供 Slider 與手動輸入欄位，兩者共用同一個 `ScaleFactor`。
- Slider 範圍為 `0.1` 到 `4.0`，步進為 `0.1`。
- 倍率輸入欄位最多顯示兩位小數，避免 Slider 浮點誤差造成過長數字。
- UI、README、AGENTS 與錯誤訊息已改為倍率語意，避免殘留舊百分比文案。

- [x] [Model] 將 `ResizeOptions.ScalePercent` (int) 改為 `ScaleFactor` (double)。
- [x] [Model] 將倍率縮放模式命名為 `ResizeMode.ScaleFactor`。
- [x] [Service] 調整 `ResizePlanner` 驗證規則：`ScaleFactor` 必須大於 0。
- [x] [Service] 調整 `ResizePlanner` 目標尺寸計算，改用 `ScaleFactor` 乘以原始尺寸。
- [x] [Test] 補上 `ResizePlannerTests`：`ScaleFactor` ≤ 0、等於 1、0.5、2 的驗證與尺寸計算測試。
- [x] [Service] 調整 `ResizePreviewService` 預覽文字，改用倍率語意（移除百分比用詞）。
- [x] [Test] 補上 `ResizePreviewServiceTests`：預覽文字包含倍率而非百分比。
- [x] [Service] 調整 `ImageResizeExecutor` 的 Magick.NET resize geometry 建立方式，改用 `ScaleFactor`。
- [x] [MainViewModel] 將 `_scalePercent` (int) 改為 `_scaleFactor` (double)。
- [x] [View] 調整 `MainWindow.xaml`：輸入欄位改為 double 類型，label 文案改為倍率說明。
- [x] [ViewModel] 新增倍率 Slider 範圍設定：最小 `0.1`、最大 `4.0`、步進 `0.1`。
- [x] [View] 倍率模式同時提供 Slider 與手動輸入欄位，兩者共用 `ScaleFactor`。
- [x] [View] 倍率輸入欄位顯示最多兩位小數，避免 Slider 浮點誤差造成過長數字。

完成判定：

- [x] [正常路徑] UI 顯示倍率 Slider 與輸入欄位；倍率 `1` 產生與原圖相同的目標尺寸，倍率 `0.5` 產生約一半尺寸，倍率 `2` 產生兩倍尺寸。
- [x] [邊界] 倍率 `0.5` 對 1px 來源圖計算結果至少保留 1px。
- [x] [錯誤狀態] 輸入倍率 `0` 或負數時，預覽顯示錯誤訊息且執行按鈕停用。
- [x] [清理] 舊的百分比文案、log 與錯誤訊息已全部改為倍率語意，無殘留百分比用詞。

## 拖曳檔案或資料夾新增資料

ID：`drag-import`
完成日期：2026-05-29
發布版本：v1.2.0

- [x] 新增拖放匯入服務，將 dropped paths 轉成 `FileItem` 清單與錯誤訊息。
- [x] 支援拖曳單一檔案、多個檔案、資料夾，以及檔案與資料夾混合輸入。
- [x] 拖入資料夾時套用目前的「包含子資料夾」設定。
- [x] 拖入檔案時套用目前勾選的副檔名過濾規則。
- [x] 重複路徑不得重複加入 `Files`。
- [x] 拖放成功後必須清除既有預覽，避免預覽與檔案清單不一致。
- [x] 拖放略過或錯誤項目必須寫入 log。
- [x] 預覽區提供明確 drop target 狀態，不影響既有 empty state。
- [x] 補上拖放匯入服務測試。

完成判定：

- [x] 拖入支援格式檔案後，檔案出現在可預覽資料清單中。
- [x] 拖入資料夾後，依目前副檔名與子資料夾設定匯入。
- [x] 拖入不支援副檔名、重複檔案或無法讀取路徑時，log 有明確訊息。
- [x] 拖放後既有 `CurrentPreview` 失效，執行按鈕不可沿用舊預覽。

## 可勾選要變動的檔案

ID：`checkbox`
完成日期：2026-05-30
發布版本：v1.2.0

優先度：高
分支：feat/checkbox
前置條件：無
被依賴：`output-folder`（executor 需識別 `IsIncluded` 篩選條件）

影響範圍：`OperationPreviewItem` → 三個 `PreviewViewModel` →
`MainViewModel.HasExecutable*Preview` → 三個 DataTemplate → `FileOperationExecutor` → `ImageResizeExecutor`

決策：已在 `OperationPreviewItem` 加入可寫 `IsIncluded`，避免只為勾選狀態建立額外 per-item wrapper ViewModel。

實作結果：

- `OperationPreviewItem` 實作 `INotifyPropertyChanged`，加入可繫結的 `IsIncluded`。
  正常列預設納入；錯誤列預設不納入，且無法重新勾選。
- 三個預覽 ViewModel 會監聽項目勾選變化，動態更新 `Summary`、`HasErrors` 與 `HasExecutableItems`。
- `MainViewModel` 會依目前預覽的 `HasExecutableItems` 與 `HasErrors` 更新三個執行按鈕狀態。
- `FileOperationExecutor` 與 `ImageResizeExecutor` 只執行已勾選、無錯誤且動作相符的項目。
- 三個預覽表格都加入「納入」checkbox 欄位；未納入列以淡灰色弱化顯示，錯誤列以淡紅色標示。
- 批次改名會額外偵測已勾選改名目標撞到未勾選來源檔的情境，將該列標示為錯誤並停用執行。

- [x] [Model] 依設計決策調整 `OperationPreviewItem`，加入可寫的 `IsIncluded` 屬性；預設值依 `HasError` 決定（有錯誤則為 false）。
- [x] [PreviewViewModel] 三個 PreviewViewModel 的 `Summary` 依已勾選項目與錯誤列計算；`HasErrors` 保留所有錯誤列與勾選狀態造成的動態衝突，避免預覽含錯誤時仍可執行。
- [x] [Test] 補上三個 PreviewViewModel 摘要隨勾選狀態變化的測試。
- [x] [MainViewModel] 調整 `HasExecutable*Preview()`：無任何已勾選且可執行項目時回傳 false。
- [x] [Test] 補上 CanExecute 隨勾選狀態變化的測試。
- [x] [Service] `FileOperationExecutor` 與 `ImageResizeExecutor` 執行前過濾 `IsIncluded = false` 的項目。
- [x] [Test] 補上 executor 只執行已勾選且無錯誤項目的測試。
- [x] [View] 三個工具的 DataTemplate 加入 checkbox 欄位，繫結 `IsIncluded`；錯誤列 checkbox 停用，未納入列與錯誤列使用不同配色。
- [x] [Bugfix] 批次改名目標若撞到未勾選來源檔，預覽會標示動態衝突並停用執行。

完成判定：

- [x] [正常路徑] 取消勾選單一項目後點擊執行，該項目對應的檔案操作不會被執行。
- [x] [邊界] 全部取消勾選時，摘要顯示「0 個項目」且執行按鈕停用。
- [x] [錯誤狀態] 錯誤列的 checkbox 為停用狀態，即使資料列顯示也不會被執行。
- [x] [一致性] 抽幀刪除、批次改名、批次縮放三個工具勾選行為一致，摘要文字隨勾選同步更新。

## 即時預覽前的預覽流程與 MainViewModel 整理

ID：`preview-flow`
完成日期：2026-05-30
發布版本：v1.3.0

優先度：高
分支：refactor/preview-flow
前置條件：無
被依賴：`live-preview`、`clear-remove`

計劃校正：`checkbox` 已歸檔，且決策為 `OperationPreviewItem` 直接承載 `IsIncluded`
與 `HasSelectionConflict`。本輪保留既有勾選狀態承載方式，只收斂動作判斷、
預覽生命週期與非同步預覽取消流程。

實作結果：

- 將動作判斷從中文字串改為強型別 `OperationActionKind`。
  `OperationPreviewItem.Action` 保留為 UI 顯示文字，executor 與 ViewModel 改用 `ActionKind`。
- 保留 `OperationPreviewItem.IsIncluded` 與 `HasSelectionConflict`，避免推翻 `checkbox` 已驗收設計。
- `MainViewModel` 以 `PreviewTool`、`InvalidateAnyPreview()`、`InvalidatePreviewFor(...)`
  與 `GetPreviewTool(...)` 集中管理預覽失效範圍。
- 三個手動預覽流程共用 `SetCurrentPreview(...)`，統一設定 `CurrentPreview`、寫入 log 與刷新 commands。
- 批次縮放預覽加入 cancellation token 與 request id，避免舊尺寸讀取結果覆蓋新設定。
- 批次縮放讀取圖片尺寸失敗時，預覽階段即標示錯誤列，避免無效圖片到 executor 階段才失敗。
- `PreviewTemplates.xaml` 改以 `ActionKind` 判斷樣式，不再用中文字串常數判斷行為。

- [x] [Model] 將 `OperationAction` 從字串常數改為強型別 `OperationActionKind`，
  UI 顯示文字由獨立轉換或顯示屬性提供，避免 executor 依賴中文字串判斷動作。
- [x] [Test] 更新 planner / executor 測試，確認動作判斷使用 `OperationActionKind`，
  且既有中文顯示文字不影響執行邏輯。
- [x] [Model] 保留 `OperationPreviewItem` 的 `IsIncluded` / `HasSelectionConflict` 繫結狀態，
  但確保 executor 與 ViewModel 的可執行判斷均改用 `OperationActionKind`。
- [x] [Test] 補上預覽 row 勾選狀態測試：錯誤列不可納入、全部取消勾選時無可執行項目、
  批次改名目標撞到未勾選來源檔時標示動態衝突。
- [x] [MainViewModel] 集中整理預覽生命週期：建立目前工具、設定快照、檔案清單與
  `CurrentPreview` 是否有效的判斷流程，取代分散的 `OnXChanged() -> InvalidatePreview()`。
- [x] [Test] 補上預覽失效測試：變更來源資料夾、格式篩選、工具參數、輸出設定與
  Tab 時，只讓受影響的預覽失效。
- [x] [MainViewModel] 將三個 `PreviewXxx()` 的共同流程收斂：建立輸入快照、呼叫 planner /
  preview service、設定 `CurrentPreview`、寫入 log、刷新 commands。
- [x] [Test] 補上三個預覽流程測試，確認預覽完成後 summary、log、CanExecute 與錯誤狀態維持不變。
- [x] [MainViewModel] 調整縮放預覽取消策略，呼叫 `ResizePreviewService` 時傳入 cancellation token，
  避免舊的尺寸讀取結果覆蓋新的設定。
- [x] [Test] 補上縮放預覽取消測試：設定變更或切換工具後，舊請求完成也不會覆蓋目前狀態。
- [x] [Bugfix] 縮放預覽讀取圖片尺寸失敗時，將該檔案標示為錯誤列並停用執行，
  避免無效圖片到 executor 階段才失敗。
- [x] [View] 確認 `PreviewTemplates.xaml` 只繫結 `OperationActionKind` 與顯示文字，
  不直接依賴中文字串常數判斷行為。

完成判定：

- [x] [正常路徑] 三個工具的手動預覽與執行行為與重構前一致，且所有既有單元測試通過。
- [x] [邊界] 預覽勾選、取消勾選、錯誤列與批次改名動態衝突的摘要與按鈕狀態正確。
- [x] [錯誤狀態] 縮放預覽讀取尺寸失敗或被取消時，舊預覽不會覆蓋新設定，log 或狀態可定位問題.
- [x] [維護性] `MainViewModel` 不再直接散落大量屬性變更失效邏輯，新增工具或即時預覽不需複製整段流程。

## 移除預覽按鈕，改為即時自動預覽

ID：`live-preview`
完成日期：2026-05-30
發布版本：v1.3.0

優先度：高
分支：feat/live-preview
前置條件：`preview-flow` 已完成（已於 [DONE.md](./DONE.md) 歸檔）
被依賴：`clear-remove`（`RemoveFileCommand` 移除後需觸發即時預覽更新）

影響範圍：`MainViewModel`（移除三個手動預覽 command、新增 `PropertyChanged` 監聽與 debounce 機制）→ `MainWindow.xaml`（移除三個預覽按鈕）

實作結果：

- 成功移除三個手動預覽 Command（`PreviewFrameDeleteCommand`、`PreviewRenameCommand`、`PreviewResizeCommand`）。
- 移除 `MainWindow.xaml` 的三個手動觸發預覽按鈕，改為輸入變更時和 Tab 切換時自動觸發。
- 批次縮放引入了 350ms 的 Debounce 防抖，利用 CancellationToken 取消舊請求以防止覆蓋最新狀態，保障連續輸入時的流暢與正確性。
- 非同步計算期間，預覽摘要列會立刻顯示 busy loading 文字「正在讀取圖片尺寸…」，計算結束後自動切換回完成狀態，並適當禁用/解鎖其他命令按鈕。
- 補上屬性變更、Tab 切換、取消非同步請求等即時觸發機制的單元測試，全部 156 個測試點均順利通過。

- [x] [MainViewModel] 移除 `PreviewFrameDeleteCommand`、`PreviewRenameCommand`、`PreviewResizeCommand` 三個手動觸發指令。
- [x] [View] 移除 `MainWindow.xaml` 上的三個預覽按鈕。
- [x] [MainViewModel] 抽幀刪除與批次改名：監聽 `Files` 與相關設定的 `PropertyChanged`，同步觸發預覽計算。
- [x] [MainViewModel] 批次縮放：設定變更時以 debounce（300–500 ms）觸發非同步預覽，避免連續輸入時重複讀取圖片尺寸。
- [x] [MainViewModel] 批次縮放預覽計算中維持 `IsPreparingPreview` 狀態與摘要列提示。
- [x] [MainViewModel] 切換工具 Tab 時自動觸發對應工具的即時預覽。
- [x] [MainViewModel] 調整 `ExecuteXxx` 的 CanExecute：不再依賴手動觸發的預覽結果，改為依 `CurrentPreview` 是否有效且無錯誤。
- [x] [Test] 補上即時觸發邏輯的 ViewModel 測試（屬性變更 → 預覽更新、Tab 切換觸發預覽）。

完成判定：

- [x] [正常路徑] 載入圖片後，三個工具各自的預覽自動顯示，不需點擊按鈕。
- [x] [即時] 調整間隔、前綴、縮放倍率等設定時，預覽即時反映最新參數。
- [x] [邊界] 切換工具 Tab 時，目標工具的預覽自動更新至最新狀態。
- [x] [防抖] 批次縮放連續調整設定時不會同時發起多個非同步請求；預覽計算中顯示 loading 狀態，完成後自動解除。

## 清空全部與剔除檔案功能

ID：`clear-remove`
完成日期：2026-05-30
發布版本：v1.3.1

優先度：中
分支：feat/clear-remove
前置條件：`live-preview` 已完成（`RemoveFileCommand` 移除後需觸發即時預覽更新）
被依賴：無

影響範圍：`MainViewModel`（新增兩個 command）→ `MainWindow.xaml`（清空按鈕）→ 三個工具 DataTemplate（剔除欄位）

實作結果：

- 新增 `ClearFolderAndFilesCommand` 與 `RemoveFileCommand` 兩個 RelayCommand，提供資料夾整理的重啟與單個剔除能力。
- `RemoveFileCommand` 支援多型別相容（FileItem 與 OperationPreviewItem），會將被剔除檔案的 FullPath 加入記憶清單 `_excludedFilePaths` 中。
- 重構掃描機制為 `RefreshScanFilesCore(bool keepExclusions)`：
  - 當點擊 UI 上的「重新掃描」按鈕時，會清除剔除清單並重新完整讀取檔案。
  - 當執行操作完成後的背景自動重新整理時，會保留剔除清單，防止剔除的檔案在執行完成後死而復生。
  - 切換來源資料夾或清空時會自動清空剔除記錄。
- 於 `MainWindow.xaml` 頂部加入「清空全部」按鈕，並於三個 DataGrid 最右側追加紅色「剔除」按鈕欄。
- 補上了 ViewModel 指令、重新掃描保留狀態與執行操作後維持過濾的 3 個單元測試，全部 160 個測試點均順利通過。

- [x] [MainViewModel] 新增 `ClearFolderAndFilesCommand`，清空 `SelectedFolder` 與 `Files`，並重設 `CurrentPreview`。
- [x] [Test] 補上 `ClearFolderAndFilesCommand` 執行後 `SelectedFolder`、`Files`、`CurrentPreview` 全部重設，且執行按鈕停用的測試。
- [x] [MainViewModel] 新增 `RemoveFileCommand(FileItem)`，從 `Files` 移除指定項目，移除後觸發即時預覽更新。
- [x] [Test] 補上 `RemoveFileCommand`：移除後預覽更新、移除最後一個檔案後執行按鈕停用的測試。
- [x] [View] `MainWindow.xaml` 加入「清空全部」按鈕，繫結 `ClearFolderAndFilesCommand`。
- [x] [View] 三個工具的 DataTemplate 各自新增「剔除」欄位，繫結 `RemoveFileCommand`，傳入對應的 `FileItem`。

完成判定：

- [x] [正常路徑] 點擊「清空全部」按鈕，來源路徑與檔案清單清空，預覽重設且執行按鈕停用。
- [x] [正常路徑] 點擊預覽表格任意檔案的「剔除」按鈕，該檔案從清單移出，其他項目的預覽立即更新。
- [x] [邊界] 剔除最後一個檔案後，三個工具的執行按鈕全部停用，預覽顯示空狀態。
- [x] [正常路徑] 點擊「重新掃描」按鈕，原本被剔除的檔案應被重新加載並顯示於預覽清單中。
- [x] [正常路徑] 執行抽幀、改名或縮放操作完成後，背景自動掃描應維持檔案的剔除狀態（不被加回清單）。
