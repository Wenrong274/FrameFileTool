# DONE

已完成並通過驗證的功能歸檔。
功能群組所有子任務與完成判定均為 `[x]` 後，從 [TODO.md](./TODO.md) 移入此處。
歸檔前必須先檢視實際程式與文件改動，將原 TODO 計劃更新為最終完成範圍，
再完整搬入此處。

---

## 1. 拖曳檔案或資料夾新增資料

完成日期：2026-05-29

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

## 2. 可勾選要變動的檔案

完成日期：2026-05-30

優先度：高
分支：feat/checkbox
前置條件：無
被依賴：功能 3（executor 需識別 `IsIncluded` 篩選條件）

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
