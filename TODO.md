# TODO

本文件追蹤目前已確認但尚未完成的功能計劃。
執行功能開發前，請先確認本文件是否有相關項目，並在完成後更新狀態。

**新增任務規範：** 每個新加入本文件的功能，必須先通過 [TODO_SPEC.md](./TODO_SPEC.md) 的五步驟流程：
分析可執行度 → 分析優先度 → 拆解任務 → 定義驗證方法 → 標記注意事項。
未通過規範的任務不可直接進入近期功能計劃。

執行本文件項目時，必須確實同步更新本文件
（完整規則以 [TODO_SPEC.md](./TODO_SPEC.md) 為唯一依據，以下為精簡提醒）：

- 開始執行某個功能群組時，將該群組正在處理的項目標記為 `[~]`。
- 每完成一個子項目，立即將該項目更新為 `[x]`，不可等到整批功能結束才回填。
- 若實作過程發現範圍、風險或完成判定需要調整，必須在同一輪變更中更新本文件。
- 若某項目暫停、拆分或延後，必須保留未完成狀態，並補上原因或後續處理方向。
- 每次 commit 前，必須確認本文件狀態與實際完成內容一致。
- **完成判定的 `[x]` 只能由開發者親自確認後標記，不得由 agent 代為勾選。**
  所有完成判定均標記為 `[x]` 後，才可將該功能移入 [DONE.md](./DONE.md)。
- 功能歸檔前，必須先檢視實際程式與文件改動，將 TODO 計劃內容更新為最終完成範圍，
  包含實作結果、範圍調整、追加修正與已知限制，再搬入 [DONE.md](./DONE.md)。

## 狀態標記

- `[ ]` 尚未開始。
- `[~]` 進行中。
- `[x]` 已完成並通過驗證。

## 近期功能計劃

### 批次縮放支援像素噪點降低

ID：`resize-denoise`
優先度：低
分支：feat/resize-denoise（開始時建立）
前置條件：無
被依賴：無

⚠ 影響範圍：`ResizeOptions` → `ImageResizeExecutor` → `ResizePlanner`（驗證）
→ `MainViewModel` → 批次縮放 UI 面板

⚠ 邊界案例：縮放比例為 1 時不應劣化原圖；強度值為 0 等同未啟用；
大型圖片（4K+）套用降噪的效能影響需評估

⚠ 待確認（研究後才可開始實作）：

- 使用哪個 Magick.NET API？
  候選：`ReduceNoise(order)`（中值濾波）、`AdaptiveBlur(radius, sigma)`（邊緣保留模糊）、
  `UnsharpMask(radius, sigma, amount, threshold)`（銳化降噪）、或組合方式。
- 套用時機：縮放前、縮放後、或縮放前後各套用一次？
- 強度設計：固定預設值（使用者一鍵啟用）還是提供強度調整滑桿？
- 哪些格式效果最明顯（PNG / JPG / WebP 的噪點特性不同）？

#### 第一階段：技術研究

- [ ] [研究] 使用 Magick.NET 對不同縮放比例的圖片分別試用
      `ReduceNoise`、`AdaptiveBlur`、`UnsharpMask`，
      記錄主觀視覺效果與每張圖的耗時差異。
- [ ] [研究] 確認最適合縮小場景（倍率 < 1）與放大場景（倍率 > 1）的算法組合。
- [ ] [決策] 根據研究結果決定：算法、套用時機、強度是否可調、UI 控制方式。
      **研究結論未記錄於本文件前，不得開始第二階段。**

#### 第二階段：實作（研究完成後展開）

- [ ] [Model] 在 `ResizeOptions` 新增 `DenoiseMode`（啟用 / 停用）
      與 `DenoiseStrength`（若採可調強度設計）。
- [ ] [Service] 調整 `ImageResizeExecutor`：
      依 `DenoiseMode` 在縮放前後套用對應的 Magick.NET 降噪操作。
- [ ] [Test] 補上 `ImageResizeExecutorTests`：
      啟用降噪時執行降噪操作、停用時不執行、強度為 0 等同停用。
- [ ] [Service] 調整 `ResizePlanner`：
      若 `DenoiseMode` 啟用但強度值無效，在預覽標示錯誤。
- [ ] [Test] 補上 `ResizePlannerTests`：
      無效強度值的錯誤標示行為。
- [ ] [ViewModel] 在 `MainViewModel` 新增降噪相關屬性與 `PropertyChanged` 觸發。
- [ ] [View] 批次縮放 UI 面板加入降噪選項（開關與強度控制）。

完成判定：

- [ ] [正常路徑] 啟用降噪、縮放比例 0.5 執行後，輸出圖片的噪點明顯少於未啟用時
      （手動視覺比對）。
- [ ] [邊界] 縮放比例為 1 且啟用降噪時，輸出圖片與原圖的視覺差異在可接受範圍內，
      不造成明顯模糊或劣化。
- [ ] [邊界] 停用降噪時，縮放行為與原本完全相同，log 不出現降噪相關訊息。
- [ ] [錯誤狀態] 若強度設計為可調且使用者輸入無效值，預覽顯示錯誤且執行按鈕停用。
- [ ] [效能] 對 20 張 1920×1080 圖片啟用降噪縮放的總耗時，
      相對於未啟用時的增幅在可接受範圍內（研究階段定義具體閾值）。

## 技術債追蹤

### GetExistingRenameTargetPaths 邏輯重複

`MainViewModel.GetExistingRenameTargetPaths()` 在 ViewModel 內複製了
`RenamePlanner` 的逐資料夾計數與目標檔名公式（`startIndex + folderIndex`、
`PadLeft(padding, '0')`、`prefix + numberText + extension`），
導致兩份邏輯必須同步更新。

建議修正方向：在 `IRenamePlanner` 新增
`ProjectTargetPaths(files, prefix, startIndex, padding, targetFolderPath)` 方法，
讓 ViewModel 呼叫 planner 計算路徑，而非自行重現命名邏輯。

影響範圍：`IRenamePlanner` 介面變更 → `RenamePlanner`、`MainViewModel`、
相關測試均需同步調整，屬中型重構。

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
