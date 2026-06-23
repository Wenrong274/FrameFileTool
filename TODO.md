# TODO

本文件追蹤目前已確認但尚未完成的功能計劃。
執行功能開發前，請先確認本文件是否有相關項目，並在完成後更新狀態。

**新增任務規範：** 每個新加入本文件的功能，必須先完成 [TODO_SPEC.md](./TODO_SPEC.md) 的
規劃前置階段（spec 需求釐清 → plan 計劃產出，工具不限），再通過五步驟流程：
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

### 工具分頁導航

ID：`tool-tabs-layout`
優先度：高
分支：feat/tool-tabs-layout（開始時建立）
前置條件：無
被依賴：無

⚠ 影響範圍：`MainWindow.xaml`（主內容 Grid → TabControl）→
`MainWindowStyles.xaml`（新增 ToolTabControl/ToolTabItem，移除 ToolTabButton）→
`MainViewModel.cs`（新增 SelectedToolIndex）→ `UI_UX_DESIGN_RULES.md`
（來源設定移至 `TabControl` 下方、Log 上方）

⚠ 邊界案例：縮放或降噪執行中切換分頁（允許，IsBatchExecuting 不鎖定）；
拖放檔案時預覽 Border 在四個 TabItem 中各複製一份，確認 DragDrop handler 正常路由

- [x] [Docs] 更新 `UI_UX_DESIGN_RULES.md` 資訊架構，納入全寬 TabControl 分頁導航規則。
- [x] [ViewModel] `MainViewModel.cs` 新增 `SelectedToolIndex` int 屬性（`SelectedTool` 的 int 包裝），
      並在 `OnSelectedToolChanged` 補 `OnPropertyChanged(nameof(SelectedToolIndex))`。
- [x] [Style] `MainWindowStyles.xaml` 新增 `ToolTabControl` 與 `ToolTabItem`（底線強調），
      移除不再使用的 `ToolTabButton` RadioButton 樣式。
- [x] [View] `MainWindow.xaml` 以全寬 `TabControl`（4 個 `TabItem`）取代主內容 3 欄 Grid；
      每個 TabItem 內維持左右分欄（工具參數 290px / 預覽 \*）；
      移除 4 個疊層 ScrollViewer 上的 `EnumToVisibilityConverter` binding；
      來源設定區移至 `TabControl` 下方、Log 上方。
- [x] [Test] 執行 `dotnet test` 確認現有測試全數通過（本次無新 unit test，XAML 行為由手動驗收確認）。

完成判定：

- [ ] [正常路徑] 點擊任一分頁標籤，對應工具參數顯示，分頁底線強調可見，其他工具不可見。
- [ ] [正常路徑] 切換至不同工具分頁，既有預覽清除，不顯示舊工具的預覽資料。
- [ ] [邊界] 縮放或降噪執行中，可自由切換分頁；切換回執行中分頁，進度仍在，不中斷。
- [ ] [邊界] 拖放圖片或資料夾到預覽區，DragDrop 行為與切換前相同，Log 正常記錄。
- [ ] [視覺] 未選中分頁文字灰色、無底線；選中分頁文字深色 SemiBold、藍色 2px 底線；hover 文字略深。
- [~] [視覺] 來源設定位置已由 `main-layout-redesign` 改為 `TabControl` 上方（本項驗收併入該任務）。

### 主視窗版面優化：砍頁首 ＋ 來源頂部漸進收合

ID：`main-layout-redesign`
優先度：高
分支：feat/tool-tabs-layout（接續 tool-tabs-layout）
前置條件：`tool-tabs-layout` 已完成
被依賴：無

spec：`docs/superpowers/specs/2026-06-23-main-layout-redesign-design.md`
plan：`docs/superpowers/plans/2026-06-23-main-layout-redesign.md`

⚠ 影響範圍：`MainWindow.xaml`（刪頁首、RowDefinitions 改 4 列、來源區改三態並移至頂部）→
`MainViewModel.cs`（新增 `IsSourceExpanded`/`HasSource`/`ToggleSourceCommand` 與掃描後自動收合）→
`UI_UX_DESIGN_RULES.md`（資訊架構）。取代 `shared-source-bar`。
⚠ 邊界案例：切分頁時收合狀態保持；視窗縮至 MinWidth 900px 時格式 pills 換行；
批次執行中來源按鈕停用。

- [x] [ViewModel] 新增 `IsSourceExpanded`/`HasSource`/`ToggleSourceCommand`，掃描出檔案後自動收合（+5 測試）。
- [x] [View] 刪除自製頁首 Border，外層 `RowDefinitions` 改為 4 列，來源區移至 `TabControl` 上方。
- [x] [View] 來源區改為三態：空狀態展開引導 / 收合 chip / 展開橫列。
- [x] [Docs] 更新 `UI_UX_DESIGN_RULES.md` 資訊架構，作廢 `shared-source-bar`。
- [x] [Test] `dotnet test` 314 全過、`dotnet format` 與 markdownlint 乾淨。

完成判定（須親自跑 app 驗收）：

- [ ] [正常路徑] 啟動無來源 → 來源區展開、無「▲ 收合」、`FileSummary` 顯示「尚未掃描」。
- [ ] [正常路徑] 選資料夾並掃描出檔案 → 自動收合成 chip（資料夾摘要＋路徑＋「✎ 變更」）。
- [ ] [正常路徑] 點「✎ 變更」展開、「▲ 收合」收回；切換 4 個分頁收合狀態保持不變。
- [ ] [邊界] 視窗縮至 MinWidth 900px，格式 pills 自動換行不溢出。
- [ ] [視覺] 無自製頁首，產品識別只由 Windows 原生標題列呈現。

### 分頁內底部來源列實作（已作廢）

ID：`shared-source-bar`

> ⛔ 已被 `docs/superpowers/specs/2026-06-23-main-layout-redesign-design.md` 取代。
> 改為頂部漸進式收合來源（單一區、空/chip/展開三態），不再放入各 TabItem 底部，
> 避免來源列在 4 個分頁各複製一份。原任務作廢，不再執行；原規劃細節已移除。

### 進階降噪引擎研究

ID：`resize-denoise-advanced`
優先度：低
分支：feat/resize-denoise-advanced（開始時建立）
前置條件：`denoise-tool` 已完成（降噪已自批次縮放獨立為專屬工具，進階引擎掛載於該工具）
被依賴：無

⚠ 影響範圍：`DenoiseOptions` / 降噪模式模型 → 進階降噪 service 介面 →
`DenoiseExecutor` 或獨立圖片處理 pipeline → `DenoiseToolViewModel` → 批次降噪 UI 面板 →
發佈包體積與執行環境需求

⚠ 邊界案例：大型圖片記憶體用量過高、無 GPU 或 GPU driver 不相容、模型檔遺失、
模型授權不允許再散布、外部工具未安裝或未授權、離線環境無法使用雲端 API

⚠ 待確認：

- OpenCV Non-local Means 是否能在夜景與人像樣本上明顯優於 Magick.NET `WaveletDenoise`。
- ONNX Runtime 是否有授權可接受、可離線散布、體積合理的降噪模型。
- Topaz / Adobe / 雲端 API 僅做外部工具整合還是完全不納入；不得把需付費或需授權的商業模型內建進發佈包。
- 是否需要 CPU/GPU 模式選擇，以及沒有硬體加速時的效能下限。

#### 進階降噪研究

- [ ] [研究] 以目前兩組實圖樣本比較 Magick.NET、OpenCV Non-local Means 與至少一個 ONNX
      開源降噪模型，記錄視覺效果、顆粒指標、耗時與輸出檔案大小。
- [ ] [研究] 逐一確認 OpenCV、ONNX Runtime、候選模型權重與外部工具的授權條款，
      標記是否可商用、是否可再散布、是否需要使用者自行安裝或登入。
- [ ] [研究] 評估發佈成本：NuGet / native runtime / 模型檔大小、Windows x64 單檔發佈相容性、
      無 GPU 環境可否接受。
- [ ] [決策] 根據研究結果決定第一個進階引擎：
      OpenCV、ONNX Runtime、外部工具整合，或暫不實作。**決策記錄於本文件後才可開始實作階段。**

#### 進階降噪實作（研究完成後展開）

- [ ] [Model] 新增降噪模式與引擎選項，保留既有 Magick.NET 模式作為預設與 fallback。
- [ ] [Service] 定義進階降噪 service 介面，使執行器不直接依賴 OpenCV、ONNX 或外部工具細節。
- [ ] [Service] 實作第一個決策通過的進階降噪引擎，並將模型或外部工具檢查集中隔離。
- [ ] [Test] 補上進階降噪 service 測試：模式選擇、fallback、模型缺失、外部工具不可用。
- [ ] [ViewModel] `DenoiseToolViewModel` 加入進階降噪模式狀態、錯誤訊息與執行前可用性檢查。
- [ ] [Test] 補上 ViewModel 測試：進階引擎不可用時不阻塞既有 Magick.NET 降噪。
- [ ] [View] 批次降噪 UI 加入進階降噪模式選擇，避免把商業工具誤導成內建功能。
- [ ] [Docs] 更新 README 與發佈說明，明確列出進階引擎的安裝需求、授權限制與離線可用性。

完成判定：

- [ ] [正常路徑] 使用研究決定的進階降噪引擎處理夜景高 ISO 圖片，
      輸出結果比 Magick.NET 標準降噪更乾淨且不產生明顯模糊。
- [ ] [正常路徑] 進階降噪不可用時，既有 Magick.NET 降噪模式仍可正常執行。
- [ ] [邊界] 在無 GPU 或模型檔不存在的環境啟動程式時，不崩潰且 UI 明確顯示該進階引擎不可用。
- [ ] [錯誤狀態] 外部工具未安裝、未授權或執行失敗時，log 記錄原因並略過該項目，
      不影響其他圖片與既有降噪流程。
- [ ] [效能] 對 20 張 1920×1080 圖片啟用進階降噪的總耗時與記憶體用量，
      在研究階段定義的可接受範圍內。

## 通用驗證

每完成一個功能群組，都必須執行：

```powershell
dotnet test -p:UseAppHost=false
dotnet format --verify-no-changes --severity warn
npx markdownlint-cli2 "*.md"
```

若有修改 UI，也需要手動確認：

- 空狀態、錯誤狀態、忙碌狀態都能接收顯示。
- 預覽必須先產生，執行按鈕才可使用。
- 長檔名與大量檔案不造成表格排版破裂。
- Log 訊息能定位發生問題的檔案或資料夾。
