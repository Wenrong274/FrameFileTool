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

### Spritesheet 打包工具

ID：`spritesheet-packer`
優先度：中
分支：feat/spritesheet-packer（開始時建立）
前置條件：無
被依賴：無

⚠ 影響範圍：全新工具 Tab → 新增 `SpriteSheetOptions` / `SpriteFrame` /
`SpriteSheetResult` → `ISpriteSheetPlanner` + `SpriteSheetPlanner` →
`ISpriteSheetExecutor` + `SpriteSheetExecutor` →
`SpriteSheetPreviewViewModel` → `MainViewModel` → `MainWindow.xaml`

⚠ 邊界案例：單張圖片、圖片尺寸超過 sheet 上限、全透明圖片的 trim 結果、
輸出路徑已存在同名 .png 或 .json、所有圖片加總面積超過最大 sheet 尺寸

⚠ 待確認（研究後才可開始實作）：

- **JSON 格式**：輸出哪種 atlas 格式？
  候選：PixiJS（最廣泛）、Phaser 3、LibGDX、自訂簡易格式。
  需確認目標使用者的遊戲引擎或工具鏈。
- **Packing 算法**：MaxRects（效率最佳）、Shelf（實作最簡）、Grid（固定格）？
  是否引入現成 NuGet 套件或自行實作？
- **Transparent trim**：是否裁切每張圖的透明邊框？
  若支援，JSON 須額外記錄 `sourceSize` 與 `spriteSourceSize`。
- **Sheet 尺寸限制**：固定上限（2048 × 2048）還是使用者自訂？是否強制 2 的冪次？
- **Padding**：sprite 之間的間距預設值？是否讓使用者調整？

#### Spritesheet Packer 研究

- [ ] [研究] 評估 MaxRects 算法的實作複雜度與可用 NuGet 套件（例如 `RectanglePacker`）；
      決定自行實作還是引入套件。
- [ ] [研究] 確認輸出 JSON 格式：以 PixiJS 格式為基準，驗證能否同時覆蓋 Phaser 3 需求；
      若差異過大，考慮支援多格式選擇。
- [ ] [研究] 驗證 Magick.NET 合成多圖的效能：對 100 張 256×256 圖片計時，
      確認完整流程（packing + 合成 + 輸出 PNG）在合理時間內完成。
- [ ] [研究] 確認 trim（透明邊框裁切）對 JSON 欄位的影響，決定是否列為 MVP 功能。
- [ ] [決策] 根據以上研究，定義：算法、JSON 格式、sheet 上限、padding 預設、
      trim 是否納入第一版。**決策記錄於本文件後才可開始實作階段。**

#### Spritesheet Packer 實作（研究完成後展開）

- [ ] [Model] 新增 `SpriteFrame` record（name, x, y, w, h, sourceSize, trimmed 等）
      與 `SpriteSheetOptions`（sheet 上限、padding、trim 開關、輸出格式）。
- [ ] [Service] 定義 `ISpriteSheetPlanner` 介面。
- [ ] [Service] 實作 `SpriteSheetPlanner`（pure function）：
      輸入 `FileItem` 清單與選項，輸出含座標的 `SpriteFrame` 清單與 sheet 尺寸；
      超過 sheet 上限的圖片標記為錯誤列。
- [ ] [Test] 補上 `SpriteSheetPlannerTests`：
      正常打包、單張圖片、超出上限標記錯誤、padding 計算、空清單。
- [ ] [Service] 定義 `ISpriteSheetExecutor` 介面。
- [ ] [Service] 實作 `SpriteSheetExecutor`：
      使用 Magick.NET 合成 sprite sheet PNG，
      使用 `System.Text.Json` 輸出 atlas JSON。
- [ ] [Test] 補上 `SpriteSheetExecutorTests`：
      輸出檔案存在、JSON 欄位正確、錯誤列不寫入 sheet。
- [ ] [PreviewViewModel] 新增 `SpriteSheetPreviewViewModel`，
      顯示每個 sprite 的座標、尺寸與錯誤訊息；計算 `Summary` / `HasErrors`。
- [ ] [Test] 補上 `SpriteSheetPreviewViewModelTests`：
      Summary 計數、有錯誤時 HasErrors 為 true。
- [ ] [MainViewModel] 新增 `TriggerSpriteSheetPreview()`、
      `HasExecutableSpriteSheetPreview()`、`ExecuteSpriteSheetCommand`。
- [ ] [Test] 補上 CanExecute 邏輯測試。
- [ ] [View] `MainWindow.xaml` 新增「Spritesheet」Tab，
      含 sheet 尺寸、padding、trim、輸出格式設定，
      並在 `ContentControl.Resources` 加入對應 `DataTemplate`。
- [ ] [DI] 在 `App.xaml.cs` 註冊 `ISpriteSheetPlanner` 與 `ISpriteSheetExecutor`。

完成判定：

- [ ] [正常路徑] 載入 10 張不同尺寸圖片後執行打包，輸出 .png 與 .json；
      JSON 中每個 sprite 的座標與尺寸正確對應 sheet 中的實際位置。
- [ ] [正常路徑] 輸出的 atlas JSON 格式符合目標引擎（PixiJS 或研究決定的格式）
      可直接讀取，不需人工修改。
- [ ] [邊界] 單張圖片打包時，sheet 尺寸等於圖片尺寸，JSON 只有一筆 sprite。
- [ ] [邊界] 某張圖片尺寸超過 sheet 上限時，預覽標示該圖錯誤，
      其餘圖片仍可正常打包，執行按鈕視錯誤規則決定是否停用。
- [ ] [錯誤狀態] 輸出路徑已存在同名檔案時，log 記錄警告且不覆蓋（或明確告知覆蓋行為）。
- [ ] [效能] 100 張 256×256 圖片的完整打包流程在合理時間內完成
      （研究階段定義具體閾值）。

### 批次縮放 Magick.NET 多模式降噪與預覽

ID：`resize-denoise-modes-preview`
優先度：中
分支：feat/resize-denoise-modes-preview（開始時建立）
前置條件：`resize-denoise` 完成 GUI 驗收並歸檔
被依賴：`resize-denoise-advanced`

⚠ 影響範圍：`ResizeOptions` 降噪模式 → `ResizePlanner` 狀態文字 →
`ImageResizeExecutor` Magick.NET 降噪 pipeline → 降噪預覽 service →
`MainViewModel` → 批次縮放 UI 面板

⚠ 邊界案例：原尺寸輸出時強力模式導致過度模糊、縮小倍率時降噪與 resampler 疊加造成細節流失、
大型圖片產生多模式預覽時 UI 卡頓、預覽來源檔案無法讀取、使用者切換資料夾後舊預覽失效

#### 設計決策（已確認，實作依據）

- **模式清單**：固定四模式 `Off`（關閉）/ `Detail`（保留細節）/ `Standard`（標準）/ `Strong`（強力）。
  首版不保留擴充位，UI 直接露出四個選項。
- **各模式 Magick.NET pipeline**：
  - `Off`：不套用降噪。
  - `Detail`：`WaveletDenoise(10%)` — 輕度 Wavelet 降噪，保留邊緣，適合細節豐富的圖片。
  - `Standard`：`ReduceNoise(3)` — 現有行為，平衡降噪與細節保留。
  - `Strong`：`ReduceNoise(3)` 後接 `WaveletDenoise(new Percentage(25))` — 最大化降噪，
    可能使影像偏柔和。
- **預設模式**：`Off`（保留原有預設行為；使用者如需降噪應主動選擇）。
- **預覽裁切策略**：中央裁切，最大 256×256；若原圖小於 256px 則取完整圖片。
  確定性高、效能好，不需複雜的噪點分析。
- **預覽範圍**：只套用降噪，不套用縮放或 resampler。
  讓使用者專注比較各模式的降噪差異。
- **強力模式提示**：在 UI 中於選擇 Strong 時顯示警示文字「強力模式可能使影像偏柔和」。
- **不加入輕微銳化**：首版保持簡單；如需探索可在 `resize-denoise-advanced` 中研究。

#### 多模式降噪研究

- [x] [研究] 使用兩組實圖樣本比較 `ReduceNoise(3)`、`WaveletDenoise(25%)`、
      `ReduceNoise(3) + WaveletDenoise(15%)` 與必要的輕微銳化組合，
      記錄視覺效果、顆粒指標、細節保留與耗時。
      （以現有測試的顆粒指標量化驗證；視覺比較留待 GUI 驗收。）
- [x] [研究] 確認多模式名稱與預設值：
      保留細節、標準、強力各自對應的 Magick.NET pipeline。
      （結論：見「設計決策」。）
- [x] [研究] 驗證局部預覽產生效能：
      對 4K 圖片裁切預覽區後產生三種模式，確認不阻塞 UI。
      （`DenoisePreviewService` 使用 `Task.Run` 在背景執行緒產生；測試確認裁切尺寸上限。）
- [x] [決策] 根據研究結果決定模式清單、預設模式、預覽裁切策略與是否加入輕微銳化。
      （決策記錄於上方「設計決策」節，實作據此展開。）

#### 多模式降噪實作

- [x] [Model] 將 `ResizeOptions.DenoiseEnabled` 擴充為降噪模式，並保留關閉狀態。
      （`DenoiseMode` enum：Off / Detail / Standard / Strong；`ResizeOptions.DenoiseMode` 取代 `DenoiseEnabled`。）
- [x] [Service] 新增 Magick.NET 降噪 pipeline 對應邏輯，集中處理各模式的 API 呼叫順序。
      （`DenoisePreviewService.ApplyDenoise` 為 internal static，供 `ImageResizeExecutor` 共用；Detail 用
      `WaveletDenoise(10%)`，Standard 用 `ReduceNoise(3)`，Strong 用 `ReduceNoise(3) + WaveletDenoise(25%)`。）
- [x] [Test] 補上降噪 pipeline 測試：
      各模式輸出尺寸不變、顆粒指標降低幅度符合預期、關閉模式不套用降噪。
      （Detail 因效果細微，僅驗證尺寸與不報錯；Standard/Strong 驗證顆粒指標降低 25%+ 。）
- [x] [Service] 新增局部預覽產生 service：
      輸入單張圖片、裁切策略與模式清單，輸出可供 UI 顯示的預覽圖。
      （`IDenoisePreviewService` / `DenoisePreviewService`；中央裁切 256×256，在背景執行緒產生。）
- [x] [Test] 補上局部預覽 service 測試：
      大圖只處理裁切區、來源無法讀取時回傳結構化錯誤、模式順序穩定。
- [x] [ResizePlanner] 預覽狀態文字顯示降噪模式名稱，而不是單純「降噪」。
- [x] [MainViewModel] 加入降噪模式選擇、預覽產生命令、忙碌狀態與錯誤訊息。
      （`SelectedDenoiseMode`、`GenerateDenoisePreviewCommand`、`IsGeneratingDenoisePreview`、
      `DenoisePreviewErrorMessage`、各模式 `BitmapSource?` 屬性。）
- [x] [Test] 補上 MainViewModel 測試：
      切換降噪模式會更新縮放預覽，預覽產生中不阻塞既有執行按鈕邏輯。
- [x] [View] 批次縮放 UI 將 checkbox 改為模式選擇控制，並加入局部預覽區。
      （RadioButton 四模式選擇、Strong 模式警示、三欄預覽圖展示、「產生預覽」按鈕。）
- [x] [Docs] 更新 README 與 TODO 研究結論，說明各模式適用場景與限制。

完成判定：

- [ ] [正常路徑] 對人像粗顆粒圖片產生多模式預覽時，使用者能看出保留細節、標準與強力的差異。
- [ ] [正常路徑] 對夜景高 ISO 圖片選擇標準或強力模式後，
      輸出結果比目前單一降噪模式更乾淨，且建築邊緣不明顯糊成一片。
- [ ] [邊界] 縮放倍率為 1 且選擇強力模式時，UI 或 log 能清楚提示可能產生較柔和的結果。
- [ ] [錯誤狀態] 預覽來源檔案不存在、損毀或格式不支援時，預覽區顯示錯誤且不影響批次縮放執行流程。
- [ ] [效能] 對 4K 圖片產生三種模式局部預覽時，UI 不凍結且耗時在研究階段定義的可接受範圍內。

### 進階降噪引擎研究

ID：`resize-denoise-advanced`
優先度：低
分支：feat/resize-denoise-advanced（開始時建立）
前置條件：`resize-denoise-modes-preview` 完成 GUI 驗收並歸檔
被依賴：無

⚠ 影響範圍：`ResizeOptions` / 降噪模式模型 → 進階降噪 service 介面 →
`ImageResizeExecutor` 或獨立圖片處理 pipeline → `MainViewModel` → 批次縮放 UI 面板 →
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
- [ ] [MainViewModel] 加入進階降噪模式狀態、錯誤訊息與執行前可用性檢查。
- [ ] [Test] 補上 MainViewModel 測試：進階引擎不可用時不阻塞既有 Magick.NET 降噪。
- [ ] [View] 批次縮放 UI 加入進階降噪模式選擇，避免把商業工具誤導成內建功能。
- [ ] [Docs] 更新 README 與發佈說明，明確列出進階引擎的安裝需求、授權限制與離線可用性。

完成判定：

- [ ] [正常路徑] 使用研究決定的進階降噪引擎處理夜景高 ISO 圖片，
      輸出結果比 Magick.NET 標準降噪更乾淨且不產生明顯模糊。
- [ ] [正常路徑] 進階降噪不可用時，既有 Magick.NET 降噪模式仍可正常執行。
- [ ] [邊界] 在無 GPU 或模型檔不存在的環境啟動程式時，不崩潰且 UI 明確顯示該進階引擎不可用。
- [ ] [錯誤狀態] 外部工具未安裝、未授權或執行失敗時，log 記錄原因並略過該項目，
      不影響其他圖片與既有縮放流程。
- [ ] [效能] 對 20 張 1920×1080 圖片啟用進階降噪的總耗時與記憶體用量，
      在研究階段定義的可接受範圍內。

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
