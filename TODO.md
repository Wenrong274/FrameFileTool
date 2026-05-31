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
