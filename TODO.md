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

### 抽幀刪除、批次改名、批次縮放均可指定輸出資料夾

ID：`output-folder`
優先度：中
分支：feat/output-folder（開始時建立）
前置條件：`checkbox` 已完成（已於 [DONE.md](./DONE.md) 歸檔）
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

### 批次縮放改用倍率輸入

ID：`scale-factor`
優先度：中
分支：feat/scale-factor（開始時建立）
前置條件：無（獨立改動，不依賴其他功能）
被依賴：`output-folder` 若同期進行需注意 `ResizeOptions` 欄位版本一致性

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

### 清空全部與剔除檔案功能 [~]

ID：`clear-remove`
優先度：中
分支：feat/clear-remove（開始時建立）
前置條件：`live-preview` 已完成（`RemoveFileCommand` 移除後需觸發即時預覽更新）
被依賴：無

⚠ 影響範圍：`MainViewModel`（新增兩個 command）→ `MainWindow.xaml`（清空按鈕）→ 三個工具 DataTemplate（剔除欄位）

⚠ 邊界案例：剔除最後一個檔案後的預覽與按鈕狀態、清空後立即再次載入、`Files` 為空時點擊清空

- [x] [MainViewModel] 新增 `ClearFolderAndFilesCommand`，清空 `SelectedFolder` 與 `Files`，並重設 `CurrentPreview`。
- [x] [Test] 補上 `ClearFolderAndFilesCommand` 執行後 `SelectedFolder`、`Files`、`CurrentPreview` 全部重設，且執行按鈕停用的測試。
- [x] [MainViewModel] 新增 `RemoveFileCommand(FileItem)`，從 `Files` 移除指定項目，移除後觸發即時預覽更新。
- [x] [Test] 補上 `RemoveFileCommand`：移除後預覽更新、移除最後一個檔案後執行按鈕停用的測試。
- [x] [View] `MainWindow.xaml` 加入「清空全部」按鈕，繫結 `ClearFolderAndFilesCommand`。
- [x] [View] 三個工具的 DataTemplate 各自新增「剔除」欄位，繫結 `RemoveFileCommand`，傳入對應的 `FileItem`。

完成判定：

- [v] [正常路徑] 點擊「清空全部」按鈕，來源路徑與檔案清單清空，預覽重設且執行按鈕停用。
- [v] [正常路徑] 點擊預覽表格任意檔案的「剔除」按鈕，該檔案從清單移出，其他項目的預覽立即更新。
- [v] [邊界] 剔除最後一個檔案後，三個工具的執行按鈕全部停用，預覽顯示空狀態。
- [ ] [正常路徑] 點擊「重新掃描」按鈕，原本被剔除的檔案應被重新加載並顯示於預覽清單中。
- [ ] [正常路徑] 執行抽幀、改名或縮放操作完成後，背景自動掃描應維持檔案的剔除狀態（不被加回清單）。

### 自動檢查 GitHub 發布更新與橫幅通知

ID：`auto-update-check`
優先度：低
前置條件：無
被依賴：無

⚠ 影響範圍：`IUpdateService` → `GitHubUpdateService` → `MainViewModel`
→ `MainWindow.xaml` → `UpdateServiceTests`

⚠ 邊界案例：GitHub API 回傳非 200、JSON 格式不符、
本地版本與遠端版本相同或較新、網路逾時、多開程式時的資源競爭

- [ ] [Model] 新增 `UpdateInfo` record，包含 `HasUpdate` (bool), `LatestVersion` (string), `ReleaseUrl` (string)。
- [ ] [Service] 新增 `IUpdateService` 介面，定義 `CheckForUpdateAsync(CancellationToken token)`。
- [ ] [Service] 實作 `GitHubUpdateService`：以 `HttpClient` 背景向
      GitHub Releases API 抓取最新發布，並與 Assembly 版本比對。
- [ ] [Test] 補上 `GitHubUpdateServiceTests`：模擬不同 API 回傳值
      （版本相同、遠端較新、遠端較舊、網路逾時/失敗）的版本號比對邏輯。
- [ ] [MainViewModel] 新增 `IsUpdateAvailable`、`LatestVersionText`、
      `LatestReleaseUrl` 等屬性，並註冊 `GoToDownloadPageCommand`
      與 `DismissUpdateBannerCommand`。
- [ ] [MainViewModel] 於 ViewModel 初始化（程式啟動後）非同步背景呼叫更新檢測服務。
- [ ] [Test] 補上 ViewModel 測試，驗證更新檢測完成後 `IsUpdateAvailable` 與指令狀態正確。
- [ ] [View] `MainWindow.xaml` 頂端新增 Fluent 風格橫幅，繫結 `IsUpdateAvailable`
      屬性，點選下載開啟瀏覽器，點選關閉則隱藏橫幅。
- [ ] [DI] 於 `App.xaml.cs` 的 DI 容器註冊 `HttpClient` 與 `IUpdateService`。

完成判定：

- [ ] [正常路徑] 當遠端 GitHub 有較新版本發布時，程式啟動後頂端會顯示藍色更新提示橫幅，
      點擊「下載更新」會以瀏覽器開啟 Release 頁面。
- [ ] [邊界] 當遠端版本與本機相同或更舊時，橫幅保持隱藏，不干擾使用者。
- [ ] [錯誤狀態] 當網路連線異常或 GitHub API 呼叫逾時（設定為 5 秒）時，程式應靜默失敗，
      不彈出任何錯誤對話框且橫幅保持隱藏。
- [ ] [體驗] 點擊橫幅的「關閉」按鈕後，橫幅必須立刻隱藏，且在此次程式執行期間不再顯示。

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
