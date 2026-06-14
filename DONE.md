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

### 同來源輸出自動子資料夾

ID：`same-folder-output-suffix`
完成日期：2026-06-14
發布版本：未發布

優先度：高
分支：feat/same-folder-output-suffix
前置條件：無
被依賴：無

來源：2026-06-14 使用者回饋。批次縮放指定資料夾輸出時，
若使用者選到與來源相同的資料夾，目前會因目標檔衝突無法輸出，
但缺少清楚提醒與順手的自動輸出策略。

Spec：[`docs/superpowers/specs/2026-06-14-same-folder-output-suffix-design.md`](./docs/superpowers/specs/2026-06-14-same-folder-output-suffix-design.md)

可執行度：可直接進計劃。現有 `ResizeToolViewModel` 已集中處理執行時目標資料夾選擇；
`ResizePlanner`、`ResizePreviewService` 與 `ImageResizeExecutor` 已支援 target folder path，
只需要在 ViewModel 與 planner/executor 之間加入 pure resolver。

優先度分析：使用者感知價值高，修正目前「選到同來源卻不清楚為何無法輸出」的流程卡點；
第一版只影響批次縮放，範圍可控，不依賴其他任務。

⚠ 影響範圍：`OutputFolderResolver` 新 service →
`ResizeToolViewModel.ExecuteAsync()` 目標資料夾解析 →
`ResizeOptions.TargetFolderPath` → 既有 `ResizePlanner` 衝突偵測 →
`ImageResizeExecutor` 實際輸出 → ViewModel log 與測試

⚠ 邊界案例：來源與目標路徑大小寫不同但實際相同；
來源資料夾名稱含不適合檔名的字元；自動子資料夾已存在同名輸出檔；
倍率文字包含小數點或尾端 0；絕對尺寸單邊為 0 時的後綴命名

決策（2026-06-14，開發者確認）：採方案 1。
當批次縮放指定輸出資料夾與來源資料夾相同時，自動改用來源內子資料夾；
倍率模式以 `{來源資料夾名}_x{倍率}` 命名，例如 `cloud_x0.5`。

實作結果：

- 新增 `ResolvedOutputFolder`、`IOutputFolderResolver` 與 `OutputFolderResolver`，將同來源輸出判斷集中在 pure service，
  不讀寫檔案、不依賴 WPF。
- 批次縮放指定輸出資料夾時，若選到來源資料夾，會自動改用來源內命名子資料夾；
  倍率模式使用 `{來源資料夾名}_x{倍率}`，絕對尺寸模式使用 `{來源資料夾名}_{寬}x{高}`。
- 來源與輸出資料夾大小寫不同但實際相同時，仍視為同一路徑；來源資料夾名稱會轉成安全子資料夾名稱，
  並保留 `.` / `..` 語意路徑段的正規化行為。
- `ResizeToolViewModel` 在使用者選擇目標資料夾後呼叫 resolver，將解析後路徑寫回 `ResizeOptions.TargetFolderPath`，
  並以解析後路徑重新建立預覽；自動子資料夾已有同名檔時，沿用既有衝突預覽停止流程，不執行 executor。
- 自動改用時 log 同時顯示原本選擇的資料夾與實際輸出資料夾，方便定位輸出位置。
- `README.md` 已補充批次縮放同來源輸出的自動子資料夾行為。

- [x] [Model] 新增 `ResolvedOutputFolder` record，包含解析後路徑、是否自動改用子資料夾與 log 訊息。
- [x] [Service] 定義 `IOutputFolderResolver` 介面。
- [x] [Service] 實作 `OutputFolderResolver` pure function：
      來源與目標不同時保留目標路徑；來源與目標相同時產生來源內子資料夾。
- [x] [Test] 補上 `OutputFolderResolverTests`：
      不同資料夾、同資料夾、大小寫不同但同路徑、倍率後綴、絕對尺寸後綴、非法字元安全化。
- [x] [ViewModel] `ResizeToolViewModel.ExecuteAsync()` 在使用者選擇目標資料夾後呼叫 resolver，
      將解析後路徑寫回 `ResizeOptions.TargetFolderPath`，並在自動改用時寫入 log。
- [x] [Test] 補上 ViewModel 執行測試：
      選到來源資料夾時 executor 收到自動子資料夾路徑，且 log 顯示自動改用資訊。
- [x] [Test] 補上衝突路徑測試：
      自動子資料夾已有同名輸出檔時，預覽標錯且 executor 不執行。
- [x] [DI] 在 `App.xaml.cs` 註冊 `IOutputFolderResolver`。
- [x] [Docs] 更新 `README.md` 批次縮放說明，補充同來源輸出會自動建立子資料夾。

發版評估（2026-06-14）：修正使用者可見的批次縮放輸出流程，且影響檔案輸出位置與衝突提示；
建議納入下一個版本發布，發布後依規則回填版本號。

完成判定：

- [x] [正常路徑] 批次縮放倍率 0.5、來源資料夾名為 `cloud`、
      執行時選擇來源資料夾作為輸出位置時，實際輸出到 `cloud\cloud_x0.5`。
- [x] [正常路徑] 來源與輸出資料夾不同時，仍輸出到使用者選擇的資料夾，
      不額外建立自動子資料夾。
- [x] [邊界] 目標資料夾與來源資料夾大小寫不同但實際相同時，仍自動改用來源內子資料夾。
- [x] [錯誤狀態] 自動子資料夾中已有同名輸出檔時，預覽標示衝突、執行停用，
      log 可定位衝突目標路徑。
- [x] [體驗] 自動改用子資料夾時，log 明確顯示原本選擇的資料夾與實際輸出資料夾。

---

## UI/UX 排版優化

ID：`ui-layout-optimize`
完成日期：2026-06-14
發布版本：未發布

優先度：高
分支：feat/ui-layout-optimize
前置條件：無
被依賴：無

⚠ 影響範圍：`MainWindowStyles.xaml`（間距 Token 調整）→
`MainWindow.xaml`（視窗尺寸、頁首精簡、來源 Card、左側面板寬度、
各工具欄位水平化、批次縮放寬高並排）

⚠ 邊界案例：縮小至 MinWidth 900px 時各列不斷行或截斷；
`SharedSizeGroup` 對齊在不同系統字型縮放下不破版

實作結果：

- 視窗預設尺寸調整為 1100×720，最小尺寸調整為 900×600，
  搭配精簡頁首與來源設定 Card，讓主要工作區取得更多可用高度。
- 來源資料夾列將檔案摘要移到同列右側，保留掃描格式與子資料夾設定的第二列空間。
- 左側工具面板加寬至 290px，四個工具的內容 Padding 收斂為 `12,10`，
  表單欄位更密集但維持可讀。
- 抽幀刪除的間隔欄位、批次改名的三個欄位改為水平對齊；
  批次縮放的目標寬度與高度改為同列並排。
- 追加修正：完全移除 `TabControl` / `TabItem` 工具切換，
  改為固定高度的 2×2 `RadioButton` 選擇格與 `Visibility` 內容切換，
  避免 TabPanel 選取位移與 StackPanel 取代後的高度分配問題。
- `PreviewTool` 文件註解同步改為工具選擇格，不再描述 Tab 順序。

- [x] [Style] `MainWindowStyles.xaml` 調整間距 Token：
      `FieldLabel` Margin `0,8,0,2` → `0,6,0,2`；
      `HintText` Margin `0,0,0,12` → `0,0,0,6`；
      `SectionTitle` Margin `0,0,0,10` → `0,0,0,6`；
      移除 `TabControl` / `TabItem` 樣式，改用固定高度的 2×2 `ToolTabButton`。
- [x] [View] `MainWindow.xaml` 視窗尺寸：Width 960→1100、Height 660→720、
      MinWidth 820→900、MinHeight 560→600。
- [x] [View] 頁首 Card 精簡：移除副標題 TextBlock、logo 34→28px、
      Card Padding `14,10`→`12,8`。
- [x] [View] 來源設定 Card：Padding `14,12`→`12,10`；
      FileSummary TextBlock 從頁首移至資料夾列右端同列。
- [x] [View] 左側工具面板：ColumnDefinition Width `245`→`290`；
      各工具 StackPanel Margin `14,12`→`12,10`。
- [x] [View] 抽幀刪除面板：`間隔` 欄位改為水平 Grid（標籤左、TextBox 右）。
- [x] [View] 批次改名面板：`前綴`、`起始編號`、`補零位數` 三欄位改為水平 Grid，
      以 `Grid.IsSharedSizeScope="True"` + `SharedSizeGroup="FieldLabel"` 對齊標籤。
- [x] [View] 批次縮放面板：`目標寬度` 與 `目標高度` 欄位並排為同列 Grid（2 欄）。
- [x] [View] 批次降噪面板：確認現有欄位佈局，維持垂直排列（ComboBox + Hint 搭配仍清晰）。

發版評估（2026-06-14）：使用者可見的 UI/UX 改善，建議納入下一個版本發布；
發布後依規則回填版本號。

完成判定：

- [x] [正常路徑] 在 1100×720 視窗切換到批次改名工具，
      三個欄位、輸出模式選項與執行按鈕均完整顯示，**不需捲動**。
- [x] [正常路徑] 在 1100×720 視窗切換到批次縮放工具、切換絕對尺寸模式，
      目標寬高並排顯示，所有設定與執行按鈕完整顯示，log 展開時**不需捲動**。
- [x] [邊界] 縮小至 MinWidth 900px，來源資料夾的標籤、TextBox 與三顆按鈕仍可操作，
      無截斷或換行。
- [x] [邊界] Log 展開後，主內容 DataGrid **至少可見 6 筆**資料列。
- [x] [錯誤狀態] 空狀態（無掃描結果）下，各工具的執行按鈕仍正確停用。

---

## 重構：MainViewModel 瘦身與執行流程統一

ID：`refactor-main-vm-slim`
完成日期：2026-06-14
發布版本：未發布

優先度：中
分支：refactor/main-vm-slim
前置條件：`refactor-preview-vm-base` 已完成
被依賴：無（完成後 `spritesheet-packer` 新增工具的改動面明顯縮小）

來源：2026-06-11 全專案健檢。`MainViewModel` 已達 1,179 行、
建構式注入 12 個相依，混合四種工具的設定、預覽失效、執行流程、降噪預覽與更新檢查。
主要債務：三個 Execute 命令重複「選目標資料夾 → 重新規劃 → 衝突檢查 → 執行 → log → 重掃」流程；
`SelectedToolIndex`（int）與 `PreviewTool` enum 依 Tab 順序魔法耦合；
建構式內呼叫 `StartUpdateCheck()` 產生副作用；
`RefreshCommands()` 手動列舉所有 command，新增 command 時容易漏補。

⚠ 影響範圍：`MainViewModel` → 新增 `ViewModels/Tools/` 工具 ViewModel →
`MainWindow.xaml` bindings 與 TabControl → `App.xaml.cs` DI →
`MainViewModelCanExecuteTests` / `MainViewModelScanTests` /
`MainViewModelDropImportTests` / `MainViewModelUpdateCheckTests`

✅ 決策（2026-06-11，開發者確認）：每工具一個子 ViewModel，組合進 `MainViewModel`，
XAML binding 改為 `Tool.Xxx` 路徑；Tab 選取改以 `PreviewTool` enum 繫結，
TabControl 透過 converter 雙向轉換。

實作結果：

- 新增 `ViewModels/Tools/IToolContext` 介面（回呼共用狀態與流程，可 mock 隔離測試）。
  新增 `FrameDeleteToolViewModel`、`RenameToolViewModel`、`ResizeToolViewModel`
  （`DenoiseToolViewModel` 同期新增，屬 `denoise-tool` 範圍）。
- `MainViewModel` 由 1,179 行縮減至 637 行，僅保留掃描狀態、當前預覽、log、更新檢查與工具協調。
  `IsResizing` 與執行進度移入 `ResizeTool`；
  `IToolContext.IsResizeExecuting` 更名為 `IsBatchExecuting`（= 縮放或降噪執行中），
  四個工具與掃描命令的 gating 統一。
- 抽出共用執行流程：`PickTargetFolderOrLogCancel` + `ApplyPlannedPreviewOrLogConflict`；
  同步流程（抽幀、改名）以 `PrepareCopyToTargetPreview<TPreview>` 組合，log 文字完全保留。
- 解除 Tab 順序魔法耦合：`PreviewTool` 移為 `Models` 公開 enum；
  `SelectedToolIndex`（int）改為 `SelectedTool`（enum），TabItem 以 `EnumToBoolConverter` 繫結 `IsSelected`。
- `StartUpdateCheck()` 改為 public，移出建構式，由 `MainWindow` wiring 階段呼叫。
- `MainWindow.xaml` 三個工具面板以 `DataContext={Binding XxxTool}` 範圍化，
  內部 binding 改用子 ViewModel 屬性名；code-behind 的降噪事件改掛 `DenoiseTool`。
- 工具 ViewModel 以 `MainViewModel` 建構式為組合根（傳入 `this` 作為 `IToolContext`），
  避免 DI 容器循環註冊；`App.xaml.cs` 建構式簽章不變，不需新增註冊。
- 全部既有 MainViewModel 測試遷移（`sut.Xxx` → `sut.XxxTool.Yyy`），未修改任何行為斷言；
  新增抽幀與改名「目標資料夾有衝突應停止並記錄 log」兩條測試補齊衝突路徑。
- `MainViewModelUpdateCheckTests` 四條測試改為明確呼叫 `StartUpdateCheck()`。
- `ResizeTool` 不再實作 `IDisposable`，需釋放資源改由 `DenoiseTool` 持有。

- [x] [ViewModel] 抽出共用的「複製到目標資料夾」執行流程：
      `PickTargetFolderOrLogCancel` + `ApplyPlannedPreviewOrLogConflict` 兩個步驟方法，
      同步流程（抽幀、改名）另以 `PrepareCopyToTargetPreview<TPreview>` 組合；log 文字完全保留。
- [x] [Test] 三個 Execute 命令行為不變：既有取消選擇與成功路徑測試未改斷言；
      新增抽幀與改名「目標資料夾有衝突應停止並記錄 log」兩條測試補齊衝突路徑。
- [x] [ViewModel] 解除魔法耦合：`PreviewTool` 移為 `Models` 公開 enum，
      `SelectedToolIndex`（int）改為 `SelectedTool`（enum），
      TabItem 以 `EnumToBoolConverter` 繫結 `IsSelected`，Tab 順序不再影響行為。
- [x] [Test] 工具切換時預覽清除與縮放預覽取消行為不變（既有測試更名為 `SelectedTool_*` 並改用 enum）。
- [x] [ViewModel] `StartUpdateCheck()` 改為 public，移出建構式，由 `MainWindow` 建構 wiring 階段呼叫。
- [x] [Test] `MainViewModelUpdateCheckTests` 四條測試改為明確呼叫 `StartUpdateCheck()`。
- [x] [ViewModel] 拆分完成：新增 `ViewModels/Tools/` 下的
      `FrameDeleteToolViewModel`、`RenameToolViewModel`、`ResizeToolViewModel`，
      以 internal `IToolContext` 介面回呼共用狀態與流程（可 mock 隔離測試）。
      `MainViewModel` 由 1,179 行縮減至 637 行，僅保留掃描狀態、當前預覽、log、更新檢查與工具協調。
      `IsResizing` 與執行進度移入 `ResizeTool`，跨工具 gating 透過 `IToolContext.IsBatchExecuting`。
- [x] [Test] 既有 MainViewModel 測試全數遷移（`sut.Xxx` → `sut.XxxTool.Yyy`），未修改任何行為斷言。
- [x] [View] `MainWindow.xaml` 三個工具面板以 `DataContext={Binding XxxTool}` 範圍化，
      內部 binding 改用子 ViewModel 屬性名；code-behind 的降噪事件改掛 `ResizeTool`。
      `PreviewTemplates.xaml` 僅引用留在 Main 的 `RemoveFileCommand`，不需變更。
- [x] [DI] 範圍調整：工具 ViewModel 以 `MainViewModel` 建構式為組合根（傳入 `this` 作為 `IToolContext`），
      避免與 DI 容器產生循環註冊；`App.xaml.cs` 不需新增註冊，建構式簽章不變。

發版評估（2026-06-14）：純重構，無使用者可見行為改變，延後併入下一版本。

完成判定：

- [x] [正常路徑] 四個工具（抽幀、改名、縮放、降噪預覽）操作流程與重構前完全一致，log 文字不變。
- [x] [邊界] 縮放執行中所有非取消操作維持停用；執行完成後排除清單仍生效。
- [x] [錯誤狀態] 複製模式目標資料夾衝突時，預覽標紅、執行停止且 log 說明原因。

---

## 重構：PreviewViewModel 共用基底類別

ID：`refactor-preview-vm-base`
完成日期：2026-06-14
發布版本：未發布

優先度：高
分支：refactor/preview-vm-base
前置條件：無
被依賴：無（建議於 `spritesheet-packer` 實作前完成，第 4 個 PreviewViewModel 可直接受益）

來源：2026-06-11 全專案健檢。三個 PreviewViewModel
（FrameDelete / Rename / Resize）重複實作相同的 Items 事件訂閱、
`IsIncluded` 變更後對 `Summary` / `HasErrors` / `HasExecutableItems` 的 PropertyChanged 轉發，
以及高度相似的計數邏輯。已有三個真實使用情境，且 `spritesheet-packer` 將是第四個，抽象化符合專案規則。

⚠ 影響範圍：`ViewModels/Previews/` 三個類別 → `IPreviewViewModel` →
`PreviewViewModelTests` → `MainViewModelCanExecuteTests`（型別判斷行為不變）

✅ 決策（2026-06-11，開發者確認）：採泛型 `PreviewViewModelBase<TItem>`，
Resize 子類直接持有 `ResizePreviewItem` 強型別清單。

實作結果：

- 新增 `PreviewViewModelBase<TItem>`：集中 Items 訂閱與 `IsIncluded` 變更的 PropertyChanged 轉發；
  項目生命週期與 ViewModel 相同，評估後不需解除訂閱機制。
- 新增 `PreviewViewModelBaseTests`：空清單、三屬性事件轉發、非 `IsIncluded` 屬性變更不轉發、
  Resize 強型別清單、Rename 建構時計算 selection conflict。
- `FrameDeletePreviewViewModel`、`ResizePreviewViewModel`、`RenamePreviewViewModel` 均改繼承基底；
  `RenamePreviewViewModel` 透過覆寫 `OnIncludedItemsChanged()` 保留 selection conflict 邏輯。
- 行為與重構前完全一致，既有測試未修改任何行為斷言（除建構方式外）。

- [x] [Test] 先確認既有 `PreviewViewModelTests` 為安全網（已涵蓋三個類別的摘要、衝突與事件轉發），未修改任何既有斷言。
- [x] [ViewModel] 新增 `PreviewViewModelBase<TItem>`：集中 Items 訂閱與 `IsIncluded` 變更的 PropertyChanged 轉發；
      項目生命週期與 ViewModel 相同，經評估不需解除訂閱機制。
- [x] [Test] 補 `PreviewViewModelBaseTests`：空清單、三屬性事件轉發、非 `IsIncluded` 屬性變更不轉發、
      Resize 強型別清單、Rename 建構時計算 selection conflict。
- [x] [ViewModel] `FrameDeletePreviewViewModel` 改繼承基底，行為不變。
- [x] [ViewModel] `ResizePreviewViewModel` 改繼承基底，保留 `ResizePreviewItem` 專屬欄位。
- [x] [ViewModel] `RenamePreviewViewModel` 改繼承基底，selection conflict 邏輯保留於子類
      （透過覆寫 `OnIncludedItemsChanged()` 掛入）。
- [x] [Test] 全部既有測試維持綠燈；除建構方式外不得修改測試斷言。（開發者本機 `dotnet test` 確認，2026-06-14）

發版評估（2026-06-14）：純重構，無使用者可見行為改變，延後併入下一版本。

完成判定：

- [x] [正常路徑] 三個工具的預覽摘要文字、錯誤標示與執行按鈕狀態與重構前完全一致。
- [x] [邊界] 預覽清單全部取消勾選時，Summary 即時更新且執行按鈕停用。
- [x] [錯誤狀態] 含錯誤項目的預覽仍顯示紅色警示、執行停用，log 行為不變。

---

## 重構：補強 PathSafetyValidator 直接測試

ID：`test-path-safety-validator`
完成日期：2026-06-14
發布版本：未發布

優先度：高
分支：test/path-safety-validator
前置條件：無
被依賴：無

來源：2026-06-11 全專案健檢。`PathSafetyValidator` 被 5 個檔案使用
（`FileOperationExecutor`、`FrameDeletePlanner`、`RenamePlanner`、`ResizePlanner`、`ImageResizeExecutor`），
是檔名安全的最後防線，但目前只透過 planner / executor 測試間接覆蓋，沒有直接的單元測試。

⚠ 影響範圍：僅新增 `FrameFileTool.Tests/Services/PathSafetyValidatorTests.cs`，不修改 production code
⚠ 邊界案例：空字串、純空白、rooted path（`C:\x.png`）、含路徑分隔符（`a\b.png`、`a/b.png`）、
含非法字元、`..` 相對路徑、超長檔名、結尾空白或點

實作結果：

- 新增 `FrameFileTool.Tests/Services/PathSafetyValidatorTests.cs`，不修改任何 production code。
- 涵蓋 `IsSafeFileName`：正常檔名、空字串、純空白、rooted path（`C:\x.png`）、
  含路徑分隔符（`a\b.png`、`a/b.png`）、含非法字元、`..` 相對路徑、超長檔名、尾端空白或點。
- 涵蓋 `IsSafeTargetDirectoryPath`：合法絕對路徑、相對路徑、非法字元、UNC 路徑行為記錄。
- 已知行為（文件記錄，不修改 production code）：`..`（單獨檔名）與尾端點號/空白目前放行；
  若要封鎖需另開任務修改 production code。

- [x] [Test] 新增 `PathSafetyValidatorTests`：`IsSafeFileName` 正常檔名、各類不安全輸入逐一覆蓋。
- [x] [Test] 補 `IsSafeTargetDirectoryPath`：合法絕對路徑、相對路徑、非法字元、UNC 路徑行為記錄。

發版評估（2026-06-14）：純測試補強，無使用者可見行為改變，延後併入下一版本。

完成判定：

- [x] [正常路徑] `dotnet test` 全綠，新測試涵蓋上述邊界案例且不依賴實際檔案系統。
- [x] [邊界] 至少一條測試明確記錄 `..`（路徑跳脫）輸入的判定結果。
- [x] [錯誤狀態] 不安全輸入全部回傳 false，無例外拋出。

---

## 獨立降噪工具（自批次縮放抽離）

ID：`denoise-tool`
完成日期：2026-06-13
發布版本：未發布

優先度：中
分支：feat/denoise-tool
前置條件：`refactor-main-vm-slim` 完成判定通過
被依賴：`resize-denoise-advanced`（進階引擎掛載於本工具）

來源：2026-06-12 spec。降噪原內嵌在批次縮放中（`ResizeOptions.DenoiseMode`、
executor 套用點、縮放 Tab 的降噪 UI），使用者無法只降噪不縮放。
本功能將降噪獨立為第四個工具 Tab，批次縮放完全移除降噪，各工具回歸單一職責。

✅ 決策（2026-06-12，開發者確認）：

- 獨立降噪工具僅支援「覆寫原檔」輸出模式，不提供輸出至指定資料夾。
- 批次縮放完全移除降噪：`ResizeOptions.DenoiseMode`、executor 套用點、
  planner 摘要文字與縮放 Tab 的降噪 UI 全部刪除，不保留隱藏欄位。
- `DenoiseMode` 三段強度、`DenoisePreviewService` 局部預覽與 `DenoiseCompareWindow`
  比較視窗搬移至新工具，演算法行為不變。
- 新工具的模式選單僅含 Detail / Standard / Strong，預設 Standard；
  `DenoiseMode.Off` 保留於 enum 供 planner / executor 作為最後防線，不出現在選單。

實作結果（影響範圍）：

- 新增：`DenoiseOptions`、`PreviewTool.Denoise`、`OperationActionKind.Denoise`、
  `DenoiseImageProcessor`（共用降噪演算法單一實作點）、
  `IDenoisePlanner` + `DenoisePlanner`（pure）、`IDenoiseExecutor` + `DenoiseExecutor`
  （暫存檔中轉覆寫、進度回報、取消）、`DenoisePreviewViewModel`、`DenoiseToolViewModel`、
  批次降噪 TabItem 與 Denoise DataTemplate、DI 註冊。
- 移除：縮放端所有降噪欄位、套用點、摘要文字與 UI；放大比較功能整組搬至降噪工具。
- 架構調整：`IToolContext.IsResizeExecuting` 更名 `IsBatchExecuting`
  （= 縮放或降噪執行中），四個工具與掃描命令的 gating 統一；
  `ResizeTool` 不再實作 `IDisposable`，需釋放資源改由 `DenoiseTool` 持有。

- [x] [Model] 新增 `DenoiseOptions` record（`DenoiseMode Mode`）；
      `PreviewTool` enum 加入 `Denoise` 成員；更新 `DenoiseMode` XML 註解中對 `ResizeOptions` 的引用；
      `OperationActionKind` 加入 `Denoise` 成員（顯示文字「降噪」）。
- [x] [Service] 將 `DenoisePreviewService.ApplyDenoise` 抽至共用的 internal static
      `DenoiseImageProcessor`，由預覽 service 與新 executor 共用，避免演算法分岔。
- [x] [Service] 定義 `IDenoisePlanner` 介面；實作 `DenoisePlanner`（pure function）：
      輸入 `FileItem` 清單 + `DenoiseOptions`，輸出 `OperationPreviewItem` 清單（覆寫原檔，目標名 = 原名）。
- [x] [Test] 補上 `DenoisePlannerTests`：正常路徑、空清單、各模式摘要描述、Off 模式標記錯誤。
- [x] [Service] 定義 `IDenoiseExecutor` 介面；實作 `DenoiseExecutor`：
      逐檔套用降噪並以暫存檔中轉覆寫原檔，回傳含成功數與錯誤明細的 `OperationResult`，
      支援進度回報與取消（比照 `ImageResizeExecutor` 模式，進度共用 `ResizeProgressReport`）。
- [x] [Test] 補上 `DenoiseExecutorTests`：成功覆寫、來源不存在列入錯誤明細且不中斷其餘檔案、
      取消中止、Off 拒絕執行、降噪後顆粒指標下降、不留暫存檔。
- [x] [PreviewViewModel] 新增 `DenoisePreviewViewModel`，繼承 `PreviewViewModelBase<OperationPreviewItem>`，
      Summary 顯示模式名稱與處理張數。
- [x] [Test] 補上 PreviewViewModel 測試：摘要文字、HasErrors、勾選變更轉發（於 `PreviewViewModelTests`）。
- [x] [ViewModel] 新增 `DenoiseToolViewModel`：模式選擇與 hint、預覽與執行命令、CanExecute；
      自 `ResizeToolViewModel` 搬移 `GenerateDenoisePreview` 相關狀態、命令與 `DenoisePreviewGenerated` 事件。
- [x] [ViewModel] `MainViewModel`：組合 `DenoiseTool`、`GetPreviewTool` 與即時預覽加入 Denoise 分支；
      `IToolContext.IsResizeExecuting` 更名為 `IsBatchExecuting`（= 縮放或降噪執行中），
      四個工具與掃描命令的 gating 統一改用此屬性。
- [x] [Test] `MainViewModelCanExecuteTests` 補 ExecuteDenoise CanExecute 全組測試與跨工具 gating
      （縮放中不可降噪、降噪中不可縮放/抽幀）；降噪局部預覽測試自 ResizeTool 遷移至 DenoiseTool。
- [x] [縮放端移除] `ResizeOptions` 刪除 `DenoiseMode` 參數、`ImageResizeExecutor` 移除套用點、
      `ResizePlanner` 移除降噪摘要文字、`ResizeToolViewModel` 移除降噪欄位與命令
      （`IDisposable` 一併移除，改由 `DenoiseTool` 持有需釋放的資源）。
- [x] [Test] 更新 `ResizePlannerTests` / `ImageResizeExecutorTests` 與 MainViewModel 相關測試，
      移除降噪斷言；保留「縮放 Status 不含降噪字樣」回歸測試。
- [x] [View] `MainWindow.xaml` 新增「批次降噪」TabItem（含覆寫警示與進度區塊）；
      `PreviewTemplates.xaml` 新增 Denoise DataTemplate（目標欄顯示「覆寫原檔」）；
      縮放 Tab 移除降噪區塊；`MainWindow.xaml.cs` 的比較視窗事件改掛 `DenoiseTool`。
- [x] [DI] `App.xaml.cs` 註冊 `IDenoisePlanner` 與 `IDenoiseExecutor`。
- [x] [文件] 更新 `README.md`：批次縮放功能描述移除降噪，新增批次降噪工具說明。
- [x] [Test] 全部測試綠燈與格式檢查（開發者本機 `dotnet test` 與 `dotnet format` 確認，2026-06-13）。

實作備註（2026-06-12）：

- 進度回報沿用 `ResizeProgressReport`（已有兩個使用情境，僅更新 XML 註解），未另建抽象。
- `DenoiseMode.Off` 保留於 enum（planner / executor 以其作為最後防線並標記錯誤），
  工具選單僅含三種強度，預設 Standard。

發版評估（2026-06-13）：新增使用者可見工具，建議納入下一個版本發布；
發布後依規則回填版本號。

完成判定：

- [x] [正常路徑] 在批次降噪 Tab 選擇模式、產生局部預覽比較與清單預覽後執行，
      原檔被降噪結果覆寫且 log 顯示成功數；批次縮放 Tab 不再有任何降噪選項，縮放結果不套用降噪。
- [x] [邊界] 檔案清單為空時，預覽與執行按鈕停用；全部取消勾選時 Summary 即時更新且執行停用。
- [x] [錯誤狀態] 來源檔案唯讀或被占用時，該檔案列入錯誤明細、其餘檔案繼續處理，log 可定位失敗檔案。

---

## 批次縮放多模式降噪與比較視窗

ID：`resize-denoise-modes-preview`
完成日期：2026-06-01
發布版本：v1.5.0

優先度：中
分支：feat/resize-denoise-modes-preview
前置條件：`resize-denoise` 完成 GUI 驗收並歸檔
被依賴：`resize-denoise-advanced`

影響範圍：`ResizeOptions.DenoiseMode` → `ResizePlanner` 狀態文字 →
`ImageResizeExecutor` 降噪 pipeline → `DenoisePreviewService` →
`DenoisePreviewGenerated` 事件 → `MainWindow.xaml.cs` → `DenoiseCompareWindow`

實作結果：

- 降噪選項從 `bool DenoiseEnabled` 擴充為 `DenoiseMode` enum（Off / Detail / Standard / Strong）。
- 各模式 pipeline：Detail → `WaveletDenoise(10%)`、Standard → `ReduceNoise(3)`、
  Strong → `ReduceNoise(3) + WaveletDenoise(25%)`。
  原計劃 Detail 用 `ReduceNoise(1)`，但對純隨機噪點圖片為 no-op，調整為 `WaveletDenoise(10%)`。
- `DenoisePreviewService`：接受圖片路徑與模式清單，中央裁切 256×256，
  在背景執行緒產生各模式 PNG 位元組，透過 `DenoisePreviewGenerated(DenoisePreviewResult)` 事件回傳。
- `DenoisePreviewService.ApplyDenoise` 為 `internal static`，供 `ImageResizeExecutor` 共用。
- `MainViewModel` 不持有 WPF 型別；`BitmapSource` 轉換在 `MainWindow.xaml.cs` 完成。
- **範圍調整**：原計劃在側欄顯示三欄縮圖；使用者回饋縮圖過小，改為：
  - 側欄只保留單一「放大比較」按鈕（ComboBox 選模式）。
  - 新增 `DenoiseCompareWindow`（940×460，可調整大小，non-modal）：
    三欄並排展示 256×256 的三種模式裁圖，auto-open 於產生完成後。
- 229 個測試全數通過（包含 `DenoisePreviewServiceTests` 與 `MainViewModelCanExecuteTests`）。

已知限制：

- Detail 模式（`WaveletDenoise(10%)`）效果較細微，在高頻隨機噪點圖上與原圖差異不易量化。
- 預覽僅取第一張圖中央 256×256，與實際批次結果可能有差異（裁切位置非自動分析噪點區域）。
- 比較視窗不含縮放或 resampler 效果，僅反映降噪本身的差異。

### 設計決策

- 模式清單：固定四模式 Off / Detail / Standard / Strong，首版不保留擴充位。
- 預設模式：Off（使用者主動啟用降噪）。
- 預覽裁切：中央裁切，最大 256×256，確定性高，不需噪點分析。
- 強力模式在 hint 文字中提示可能偏柔和；不加入輕微銳化（留待 `resize-denoise-advanced`）。

### 多模式降噪研究

- [x] [研究] 比較各 Magick.NET 降噪組合的視覺效果與顆粒指標。
- [x] [研究] 確認各模式名稱與 pipeline 對應。
- [x] [研究] 驗證背景產生不阻塞 UI。
- [x] [決策] 決策記錄於上方「設計決策」節。

### 多模式降噪實作

- [x] [Model] `DenoiseMode` enum；`ResizeOptions.DenoiseMode` 取代 `DenoiseEnabled`。
- [x] [Service] `DenoisePreviewService.ApplyDenoise` 集中各模式 pipeline。
- [x] [Test] 降噪 pipeline 與局部預覽 service 的測試（含邊界與錯誤路徑）。
- [x] [Service] `IDenoisePreviewService` / `DenoisePreviewService`（中央裁切 256×256）。
- [x] [ResizePlanner] 狀態文字顯示降噪模式名稱。
- [x] [MainViewModel] `SelectedDenoiseMode`、`GenerateDenoisePreviewCommand`、事件回傳。
- [x] [Test] MainViewModel 降噪模式切換與 CanExecute 隔離。
- [x] [View] ComboBox 模式選擇 + 單一「放大比較」按鈕 + `DenoiseCompareWindow`。
- [x] [Docs] README 與 TODO 同步更新。

完成判定：

- [v] [正常路徑] 對人像粗顆粒圖片產生多模式預覽時，使用者能看出保留細節、標準與強力的差異。
- [v] [正常路徑] 對夜景高 ISO 圖片選擇標準或強力模式後，輸出結果比舊單一模式更乾淨。
- [v] [邊界] 縮放倍率為 1 且選擇強力模式時，UI hint 文字清楚提示可能偏柔和。
- [v] [錯誤狀態] 預覽來源不存在或損毀時，比較視窗顯示錯誤訊息且不影響批次縮放執行。
- [v] [效能] 對 4K 圖片產生三種模式預覽時，UI 不凍結。

---

## 批次縮放支援像素噪點降低

ID：`resize-denoise`
完成日期：2026-06-01
發布版本：v1.5.0

優先度：低
分支：feat/resize-denoise
前置條件：無
被依賴：`resize-denoise-modes-preview`、`resize-denoise-advanced`

影響範圍：`ResizeOptions` → `ImageResizeExecutor` → `ResizePlanner`（驗證）
→ `MainViewModel` → 批次縮放 UI 面板

邊界案例：縮放比例為 1 時不應劣化原圖；降噪關閉時等同未啟用；
大型圖片（4K+）套用降噪的效能影響需評估。

實作結果：

- 批次縮放新增「降低像素噪點」開關，第一版不提供強度值。
- `ResizeOptions` 新增 `DenoiseEnabled`，由 ViewModel 傳入 planner 與 executor。
- `ResizePlanner` 的狀態文字會在啟用時標示「降噪」。
- `ImageResizeExecutor` 在縮放前套用 Magick.NET `ReduceNoise(3)`。
- 研究曾先評估 `ReduceNoise(2)`，但使用者實圖驗收顯示粗顆粒噪點效果過弱，
  因此提高為 `ReduceNoise(3)`。
- 補上顆粒指標測試，避免只檢查像素有變但實際降噪效果不足。
- 後續更完整的 Magick.NET 多模式降噪與預覽，已拆分為
  `resize-denoise-modes-preview` 繼續追蹤。
- OpenCV / ONNX / 外部商業工具等進階引擎研究，已拆分為
  `resize-denoise-advanced` 繼續追蹤。

### 降噪研究結論

決策日期：2026-06-01

- MVP 採用 `ReduceNoise(3)`，在縮放前套用。
- 第一版只提供「啟用降噪」開關，不提供強度滑桿；避免 UI 複雜化，也降低使用者輸入錯誤。
- Magick.NET Q8 的 `ReduceNoise(1)` 對隨機噪點圖為 no-op。
- `AdaptiveBlur(0.7, 0.4)` 在 20 張 1920×1080 測試中約 13.7–15.8 秒，明顯慢於其他方案，
  且視覺上容易讓線條變糊，不納入 MVP。
- `UnsharpMask` 適合銳化但不是降噪；放大倍率 2.0 時 20 張測試約 23.5 秒，
  且會放大顆粒感，不納入 MVP。
- `ReduceNoise(2)` 在 20 張 1920×1080 測試中：
  倍率 0.5 約 5.4 秒、倍率 1.0 約 6.1 秒、倍率 2.0 約 9.5 秒；
  相較不降噪會增加耗時，但使用者實圖驗收顯示效果過弱，已改用 `ReduceNoise(3)`。
- 倍率 1.0 啟用 `ReduceNoise(3)` 時可更明顯降低顆粒，線條仍保留，視覺差異可接受。

### 降噪研究

- [x] [研究] 使用 Magick.NET 對不同縮放比例的圖片分別試用
      `ReduceNoise`、`AdaptiveBlur`、`UnsharpMask`，
      記錄主觀視覺效果與每張圖的耗時差異。
- [x] [研究] 確認最適合縮小場景（倍率 < 1）與放大場景（倍率 > 1）的算法組合。
- [x] [決策] 根據研究結果決定：算法、套用時機、強度是否可調、UI 控制方式。

### 降噪實作

- [x] [Model] 在 `ResizeOptions` 新增 `DenoiseMode`（啟用 / 停用）
      或等價布林欄位；第一版不提供強度值。
- [x] [Service] 調整 `ImageResizeExecutor`：
      啟用降噪時在縮放前套用 Magick.NET `ReduceNoise(3)`。
- [x] [Test] 補上 `ImageResizeExecutorTests`：
      啟用降噪時仍輸出正確尺寸、顆粒指標需明顯降低，停用時維持原本縮放結果。
- [x] [ViewModel] 在 `MainViewModel` 新增降噪相關屬性與 `PropertyChanged` 觸發。
- [x] [View] 批次縮放 UI 面板加入降噪開關。

完成判定：

- [x] [正常路徑] 啟用降噪、縮放比例 0.5 執行後，輸出圖片的噪點明顯少於未啟用時
  （手動視覺比對）。
- [x] [邊界] 縮放比例為 1 且啟用降噪時，輸出圖片與原圖的視覺差異在可接受範圍內，
  不造成明顯模糊或劣化。
- [x] [邊界] 停用降噪時，縮放行為與原本完全相同，log 不出現降噪相關訊息。
- [x] [錯誤狀態] 啟用降噪後遇到無法讀取或寫入的圖片時，沿用既有縮放錯誤處理，
  log 記錄失敗項目且其他檔案可繼續處理。
- [x] [效能] 對 20 張 1920×1080 圖片啟用降噪縮放的總耗時，
  相對於未啟用時的增幅在可接受範圍內（研究階段定義具體閾值）。

## 抽幀刪除、批次改名、批次縮放均可指定輸出資料夾

ID：`output-folder`
完成日期：2026-05-31
發布版本：v1.4.0

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
發布版本：v1.4.0

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

## 自動檢查 GitHub 發布更新與橫幅通知

ID：`auto-update-check`
完成日期：2026-05-31
發布版本：v1.4.0

優先度：低
分支：feat/auto-update-check
前置條件：無
被依賴：無

影響範圍：`UpdateInfo` → `IUpdateService` → `GitHubUpdateService` →
`IExternalLinkService` → `MainViewModel` → `MainWindow.xaml` →
`App.xaml.cs` → `GitHubUpdateServiceTests` / `MainViewModelUpdateCheckTests`

邊界案例：GitHub API 回傳非 200、JSON 格式不符、
本地版本與遠端版本相同或較新、網路逾時、多開程式時的資源競爭、
應用程式關閉時 HTTP 請求尚未完成

實作結果：

- 新增 `UpdateInfo` record 表示更新結果；`IUpdateService` 定義非同步更新檢查介面。
- `GitHubUpdateService` 呼叫 GitHub Releases API，與 Assembly 版本比對，
  網路異常、非 200、JSON 格式錯誤與取消均靜默回傳 `UpdateInfo.None`。
- `IExternalLinkService` / `ExternalLinkService` 封裝瀏覽器開啟行為，
  非合法絕對 URL 靜默忽略。
- `MainViewModel` 啟動後非同步背景呼叫更新檢查；新增 `IsUpdateAvailable`、
  `LatestVersionText`、`LatestReleaseUrl`、`GoToDownloadPageCommand`、
  `DismissUpdateBannerCommand`。
- `MainWindow.xaml` 頂端加入 Fluent 風格藍色橫幅，點選「下載更新」開啟瀏覽器，
  點選「關閉」隱藏橫幅，本次程式執行期間不再顯示。
- Release workflow 改為以 tag 版本號寫入 AssemblyVersion / FileVersion，
  供執行期比對 GitHub 最新發布版本。
- Code review 後補強：
  - `GitHubUpdateService` 移除反向的 `OperationCanceledException when` 篩選，
    取消（逾時或 app 關閉）一律靜默回傳 `UpdateInfo.None`。
  - `MainViewModel` 實作 `IDisposable`，加入 `_updateCheckCts`；
    app 關閉時由 `ServiceProvider.Dispose()` 觸發取消 HTTP 請求。
  - bare `catch` 改為區分 `OperationCanceledException` 與其他例外，
    非預期例外寫入 log 而非靜默吞掉。
  - `IUpdateService` / `IExternalLinkService` 改為強制注入（非 nullable optional），
    一致化 constructor 注入語意。
  - `HttpClient` 改為 factory lambda，讓 DI 容器正確管理生命週期。

- [x] [Model] 新增 `UpdateInfo` record，包含 `HasUpdate`、`LatestVersion`、`ReleaseUrl`。
- [x] [Service] 新增 `IUpdateService` 介面，定義 `CheckForUpdateAsync(CancellationToken)`。
- [x] [Service] 實作 `GitHubUpdateService`，呼叫 GitHub Releases API 並比對版本號。
- [x] [Service] 新增 `IExternalLinkService` / `ExternalLinkService`，封裝瀏覽器開啟。
- [x] [Test] 補上 `GitHubUpdateServiceTests`：版本比對、網路異常、逾時等情境。
- [x] [Test] 補上 `ExternalLinkServiceTests`：合法 URL 正常通過，非法字串靜默忽略。
- [x] [MainViewModel] 新增更新相關屬性與指令；啟動後非同步呼叫更新檢查。
- [x] [MainViewModel] 實作 `IDisposable`，加入 `_updateCheckCts` 取消機制。
- [x] [Test] 補上 `MainViewModelUpdateCheckTests`：版本比對結果、指令狀態、橫幅隱藏。
- [x] [View] `MainWindow.xaml` 頂端新增更新提示橫幅，繫結下載與關閉命令。
- [x] [DI] `App.xaml.cs` 註冊 `HttpClient`（factory lambda）、`IUpdateService`、`IExternalLinkService`。
- [x] [DI] `App.xaml.cs` 覆寫 `OnExit`，呼叫 `ServiceProvider.Dispose()` 觸發 ViewModel 清理。
- [x] [CI] Release workflow 寫入 AssemblyVersion / FileVersion 供執行期版本比對。
- [x] [Bugfix] `GitHubUpdateService` 移除反向 when 篩選，取消一律靜默回傳 None。
- [x] [Bugfix] `MainViewModel` bare catch 改為 log 型，非預期例外不再靜默吞掉。

完成判定：

- [x] [正常路徑] 當遠端 GitHub 有較新版本發布時，程式啟動後頂端會顯示藍色更新提示橫幅，
  點擊「下載更新」會以瀏覽器開啟 Release 頁面。
- [x] [邊界] 當遠端版本與本機相同或更舊時，橫幅保持隱藏，不干擾使用者。
- [x] [錯誤狀態] 當網路連線異常或 GitHub API 呼叫逾時（設定為 5 秒）時，程式靜默失敗，
  不彈出任何錯誤對話框且橫幅保持隱藏。
- [x] [體驗] 點擊橫幅的「關閉」按鈕後，橫幅立刻隱藏，且在此次程式執行期間不再顯示。

## GetExistingRenameTargetPaths 邏輯重複重構

ID：`rename-planner-project-paths`
完成日期：2026-05-31
發布版本：v1.4.1

優先度：中
分支：refactor/rename-planner-project-paths
前置條件：無
被依賴：無

影響範圍：`IRenamePlanner` → `RenamePlanner` →
`MainViewModel.GetExistingRenameTargetPaths()` → `RenamePlannerTests`

邊界案例：`targetFolderPath` 為空時回傳空集合且不呼叫 planner；
多個子資料夾各自獨立計數行為必須與 `RenamePlanner.Plan()` 完全一致。

實作結果：

- `IRenamePlanner` 新增 `ProjectTargetPaths()`，集中產生複製改名目標路徑。
- `RenamePlanner.Plan()` 與 `MainViewModel.GetExistingRenameTargetPaths()` 共用同一套
  逐資料夾計數與補零命名邏輯，避免兩處規則漂移。
- `GetExistingRenameTargetPaths()` 在目標資料夾為空時直接回傳空集合，不呼叫 planner。
- 補上 `ProjectTargetPaths` 與 `GetExistingRenameTargetPaths` 的對比測試，鎖定路徑一致性。

- [x] [Service] 在 `IRenamePlanner` 新增 `ProjectTargetPaths(files, prefix, startIndex, padding, targetFolderPath)`
      介面方法，回傳 `IEnumerable<string>`。
- [x] [Service] 在 `RenamePlanner` 實作 `ProjectTargetPaths`，
      將 `Plan()` 的逐資料夾計數與目標路徑公式提取至此方法，兩處共用同一邏輯。
- [x] [Test] 補上 `RenamePlannerTests.ProjectTargetPaths_*`：
      單檔無補零、多檔有補零、多個子資料夾各自獨立計數、空清單回傳空序列。
- [x] [ViewModel] `MainViewModel.GetExistingRenameTargetPaths()` 改為
      呼叫 `_renamePlanner.ProjectTargetPaths(...)`，移除重複的計數與命名邏輯。

完成判定：

- [x] [正確性] `GetExistingRenameTargetPaths` 與 `RenamePlanner.Plan()` 對相同輸入產生
      相同的目標路徑集合，`RenamePlannerTests` 有明確對比測試。
- [x] [邊界] `targetFolderPath` 為空時，`GetExistingRenameTargetPaths` 回傳空集合且不呼叫 planner。
- [x] [一致性] 所有現有測試（`dotnet test`）通過，無迴歸。

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
