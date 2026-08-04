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

### 批次改名命名樣板與沿用原編號

ID：`rename-numbering`
優先度：高
分支：feat/rename-numbering（已建立）
前置條件：無
被依賴：無

⚠ 影響範圍：新增 `Models/RenameOptions.cs` → `IRenamePlanner` / `RenamePlanner` 兩個方法簽章 →
`RenameToolViewModel` 狀態與 `TriggerPreview` / `Execute` → `MainWindow.xaml` 改名面板欄位 →
`RenamePlannerTests` 既有 17 個測試需改呼叫方式

⚠ 邊界案例：副檔名本身含數字（`.mp3`、`.x264`）不得被誤判為編號、檔名完全無數字、
檔名純數字（`0037.png`）、樣板無 `[###]` token、樣板含多個 token、
樣板 token 後方文字與原檔名尾綴同時存在、跨資料夾合併複製時同編號撞名

⚠ 已釐清（spec 階段結論，不再變動）：

- 編號 = 檔名剝除副檔名後的**最後一組**連續數字。
- 沿用原編號時，數字前文字由樣板取代，數字後尾綴原封不動保留，且 `[###]` 位數不生效。
- 組合順序為「樣板完整展開 + 原尾綴 + 副檔名」。
- `[` + 一個以上 `#` + `]` 才是 token，不提供跳脫語法，其餘字元一律字面。
- 起始編號固定從 0 起算的需求已由現況滿足（`_startIndex` 無初始值即為 0，專案無設定持久化），不需改動。

#### Model 與規劃層

- [x] [Model] 新增 `RenameOptions` immutable record（`Template`、`StartIndex`、`UseOriginalNumber`、
      `OutputMode`、`TargetFolderPath`），比照 `ResizeOptions`。
- [x] [Test] 先補上 `RenamePlanner` 新行為的失敗測試：token 位數解析、多 token、缺 token 報錯、
      最後一組數字擷取、副檔名含數字不誤抓、無數字判 `Keep`、尾綴保留與組合順序、
      沿用原編號時忽略位數、兩種輸出模式 × 兩種編號模式。
- [x] [Service] `IRenamePlanner.Plan` 與 `ProjectTargetPaths` 改為接受 `RenameOptions`，
      確保兩者共用同一組命名公式。
- [x] [Service] `RenamePlanner` 實作樣板展開與原編號擷取，缺 token 時每個項目回傳
      `HasError` 與狀態「命名樣板缺少 [###] 編號欄位」，無數字時回傳 `Keep` 與狀態「無編號，不處理」。
- [x] [Test] 改寫既有 17 個測試的呼叫方式，維持原有衝突偵測與逐資料夾計數的驗證。

#### ViewModel 與 UI

- [x] [ViewModel] `RenameToolViewModel` 以 `Template`（預設 `Symbol_[#]`）取代 `Prefix` 與 `ZeroPadding`，
      新增 `UseOriginalNumber`，並建立 `RenameOptions` 傳入 planner。
- [x] [Test] 補上 ViewModel 測試：`UseOriginalNumber` 變更會觸發預覽重算。
- [x] [View] 改名面板以單一「命名樣板」欄位取代前綴與補零位數兩欄，
      新增「沿用原檔名編號」CheckBox，勾選時停用（不隱藏）起始編號欄位，
      並在樣板欄下方顯示「編號沿用原檔名，`[###]` 位數不生效」。
- [x] [Docs] 更新 README 的批次改名功能說明與範例。

完成判定：

- [ ] [正常路徑] 樣板 `Symbol_[###]` 搭配重新編號，預覽產出 `Symbol_000.png` 起的連續檔名並可成功執行。
- [ ] [正常路徑] 勾選「沿用原檔名編號」後，`frame_0037.png` 預覽為 `Symbol_0037.png`，
      跳號檔案的編號完全保持不變。
- [ ] [正常路徑] 樣板 token 後方有文字時，`frame_0037_final.png` 預覽為 `Symbol_0037_v2_final.png`。
- [ ] [邊界] 資料夾內混入 `title.png` 時顯示「無編號，不處理」，且不阻擋其他檔案執行。
- [ ] [錯誤狀態] 樣板未包含 `[###]` 時，預覽全部標記錯誤且執行按鈕停用。
- [ ] [UI] 勾選沿用原編號時，起始編號欄位呈現明顯低對比的停用狀態且仍可看出用途。

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
