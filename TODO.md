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

#### 降噪研究結論

決策日期：2026-06-01

- MVP 採用 `ReduceNoise(2)`，在縮放前套用。
- 第一版只提供「啟用降噪」開關，不提供強度滑桿；避免 UI 複雜化，也降低使用者輸入錯誤。
- 追加驗證發現 Magick.NET Q8 的 `ReduceNoise(1)` 對隨機噪點圖為 no-op，
  因此改用 `ReduceNoise(2)` 作為第一版固定值。
- `AdaptiveBlur(0.7, 0.4)` 在 20 張 1920×1080 測試中約 13.7–15.8 秒，明顯慢於其他方案，
  且視覺上容易讓線條變糊，不納入 MVP。
- `UnsharpMask` 適合銳化但不是降噪；放大倍率 2.0 時 20 張測試約 23.5 秒，
  且會放大顆粒感，不納入 MVP。
- `ReduceNoise(2)` 在 20 張 1920×1080 測試中：
  倍率 0.5 約 5.4 秒、倍率 1.0 約 6.1 秒、倍率 2.0 約 9.5 秒；
  相較不降噪會增加耗時，但仍在可接受範圍。
- 倍率 1.0 啟用 `ReduceNoise(2)` 時可降低顆粒，線條仍保留，視覺差異可接受。

#### 降噪研究

- [x] [研究] 使用 Magick.NET 對不同縮放比例的圖片分別試用
      `ReduceNoise`、`AdaptiveBlur`、`UnsharpMask`，
      記錄主觀視覺效果與每張圖的耗時差異。
- [x] [研究] 確認最適合縮小場景（倍率 < 1）與放大場景（倍率 > 1）的算法組合。
- [x] [決策] 根據研究結果決定：算法、套用時機、強度是否可調、UI 控制方式。
      **研究結論未記錄於本文件前，不得開始實作階段。**

#### 降噪實作（研究完成後展開）

- [x] [Model] 在 `ResizeOptions` 新增 `DenoiseMode`（啟用 / 停用）
      或等價布林欄位；第一版不提供強度值。
- [x] [Service] 調整 `ImageResizeExecutor`：
      啟用降噪時在縮放前套用 Magick.NET `ReduceNoise(2)`。
- [x] [Test] 補上 `ImageResizeExecutorTests`：
      啟用降噪時仍輸出正確尺寸、停用時維持原本縮放結果。
- [x] [ViewModel] 在 `MainViewModel` 新增降噪相關屬性與 `PropertyChanged` 觸發。
- [x] [View] 批次縮放 UI 面板加入降噪開關。

完成判定：

- [ ] [正常路徑] 啟用降噪、縮放比例 0.5 執行後，輸出圖片的噪點明顯少於未啟用時
      （手動視覺比對）。
- [ ] [邊界] 縮放比例為 1 且啟用降噪時，輸出圖片與原圖的視覺差異在可接受範圍內，
      不造成明顯模糊或劣化。
- [ ] [邊界] 停用降噪時，縮放行為與原本完全相同，log 不出現降噪相關訊息。
- [ ] [錯誤狀態] 啟用降噪後遇到無法讀取或寫入的圖片時，沿用既有縮放錯誤處理，
      log 記錄失敗項目且其他檔案可繼續處理。
- [ ] [效能] 對 20 張 1920×1080 圖片啟用降噪縮放的總耗時，
      相對於未啟用時的增幅在可接受範圍內（研究階段定義具體閾值）。

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
