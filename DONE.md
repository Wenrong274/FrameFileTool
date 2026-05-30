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
發布版本：未發布

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
發布版本：未發布

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
