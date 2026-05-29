# AGENTS.md

## 專案

FrameFileTool 是一個 Windows WPF 桌面應用程式，用於處理序列圖檔的抽幀與批次檔名操作。

這個專案應維持為可長期維護的工具平台，而不是一次性的腳本。新增功能時，必須保持 UI、應用程式狀態、規劃邏輯與檔案執行邏輯彼此分離。

## 未完成功能追蹤

目前已確認但尚未完成的功能與驗收條件，請以 [TODO.md](./TODO.md) 為準。

每次開始新增功能、調整 UI 或重構既有流程前，必須先檢查 `TODO.md` 是否已有相關項目。
若本輪工作完成或改變 TODO 內容，必須同步更新 `TODO.md` 的狀態、範圍或完成判定。
執行 `TODO.md` 項目時，必須在同一輪工作中確實更新 `TODO.md`：
開始處理標記為 `[~]`，完成項目標記為 `[x]`，暫停或拆分項目需保留原因與後續方向。
功能搬入 `DONE.md` 前，必須先檢視實際程式與文件改動，將該功能的 TODO 計劃更新為最終完成範圍，
包含實作結果、範圍調整、追加修正與已知限制。

## 開發規則

每次修改這個 repository 時，都必須遵守以下規則：

- 核心行為使用 TDD。
- 遵守 SOLID 原則。
- 規劃與轉換邏輯優先採用 functional programming 的撰寫風格。
- WPF UI 程式碼保持精簡。
- 檔案系統副作用集中隔離。
- 不要把商業邏輯寫在事件處理器或 code-behind。
- 至少有兩個真實使用情境前，不要加入過度抽象。
- 文件與程式註解以繁體中文為主，英文僅作為必要技術詞輔助。

## Skills 參照規則

本專案的通用技術規範由 `.agents/skills` 底下的 `SKILL.md` 補充：

- `.agents/skills/dotnet-wpf-modern.md`：.NET 8+ WPF 整合規範，包含 Host builder、MVVM Toolkit、效能、theming 與現代 C#。
- `.agents/skills/wpf/SKILL.md`：WPF、XAML、binding、commands、threading、styles 與 templates。
- `.agents/skills/mvvm/SKILL.md`：MVVM 分層、CommunityToolkit.Mvvm、ViewModel 測試性與 commands。
- `.agents/skills/modern-csharp/SKILL.md`：符合專案目標框架與語言版本的現代 C# 寫法。
- `.agents/skills/project-setup/SKILL.md`：solution/project 結構、共用 MSBuild 設定、CI 與本機開發基線。
- `.agents/skills/csharp-scripts.md`：明確需要 file-based C# app 時使用，不作為既有專案整合開發的預設方式。

規則優先順序：

- 專案特有的架構、檔案操作安全、Git、文件語言與 UI/UX 規則，以本檔為準。
- 現代 WPF 整合議題優先參照 `dotnet-wpf-modern.md`。
- 細部 XAML / binding / threading 問題參照 `wpf/SKILL.md`。
- ViewModel、commands、dependency injection 與測試性問題參照 `mvvm/SKILL.md`。
- C# 語法與語言版本問題參照 `modern-csharp/SKILL.md`。
- solution / project 結構問題參照 `project-setup/SKILL.md`。
- file-based C# app 僅在使用者明確要求快速 C# 原型或實驗時參照 `csharp-scripts.md`。
- 若 skill 補足本檔未明確定義的行為，應採用 skill 的規範。
- 若本檔與 skill 出現實質衝突，應先依 skill 修正通用技術做法，再同步更新本檔避免重複或矛盾。

重複內容處理：

- `dotnet-wpf-modern.md` 與 `wpf/SKILL.md`、`mvvm/SKILL.md`、`modern-csharp/SKILL.md` 內容有重疊時，
  以 `dotnet-wpf-modern.md` 作為 WPF on modern .NET 的整合方向，
  再用各細分 skill 補足實作細節。
- `csharp-scripts.md` 不應取代本專案的 `.csproj`、測試專案或正式工具程式碼。
  它只適合臨時 C# 語法/API 驗證，且檔案應放在既有 project 目錄外，避免被 SDK-style 專案誤納入編譯。

## 語言規則

文件、使用者說明與程式註解應以繁體中文為主要語言。

專案內所有 Markdown 文件（`.md`）必須通過 markdownlint 檢查。
設定規則定義於根目錄的 `.markdownlint.json`。
MD060（表格欄位對齊）已停用，因為繁體中文全形字元的顯示寬度難以與 ASCII 空白精確對齊。

C# 程式碼風格遵循 Microsoft Code Style Guide，規則定義於根目錄的 `.editorconfig`，
由 IDE 與 `dotnet format` 強制執行，不另開說明文件。

以下情況可以使用英文作為輔助：

- 使用英文更清楚的技術詞，例如 `ViewModel`、`Service`、`pure function`、`executor`、`binding`。
- 類別、方法、屬性、命令與檔案名稱。
- commit message 的 `type` 與 `scope`、套件名稱、framework 名稱與 CLI 指令。

良好範例：

```csharp
// 先用暫存檔名中轉，避免 A->B、B->C 這類互相撞名的 rename chain。
```

```text
抽幀規則使用 pure planner：輸入 files + options，輸出 preview plan，不直接操作檔案。
```

除非內容是指令、API 名稱或外部工具輸出，否則專案文件應避免出現純英文說明。

## 架構

預期架構如下：

```text
Views
  僅放 WPF XAML 與最少量 code-behind。

ViewModels
  管理 UI 狀態、commands、驗證，以及呼叫 services。

  ViewModels/Previews/
    每個工具的預覽結果對應一個獨立的 PreviewViewModel，
    實作 IPreviewViewModel（Summary / HasErrors）。
    MainViewModel 持有 CurrentPreview: IPreviewViewModel?，
    MainWindow 的 ContentControl 依型別自動選對應的 DataTemplate。

Services
  負責檔案掃描、操作規劃與實際執行。

Models
  放置 services 與 view models 共用的 immutable 或簡單資料物件。
  OperationPreviewItem 為所有工具共用的預覽項目基底類別；
  需要額外欄位的工具（如縮放的尺寸資訊）應建立繼承子類別（如 ResizePreviewItem），
  不應在基底類別累積工具專屬欄位。
```

code-behind 可以初始化相依物件並設定 `DataContext`。code-behind 不應包含檔案操作規則、改名規則或抽幀規則。

新增或重整 solution / project 結構時，應依 `.agents/skills/project-setup/SKILL.md`：

- 從應用程式模型與部署目標選擇最小且正確的 SDK 與 target framework。
- 專案與資料夾命名應反映責任邊界，不以暫時實作細節命名。
- 共享 MSBuild 設定、nullable、analyzer 或 package 版本時，必須能降低重複且不隱藏平台差異。
- 避免循環相依與雜物型 utility 專案；優先使用 project references 與組合。
- 本機 build、test、run 流程不直覺時，需同步更新文件或本檔。

### 新增工具的標準流程

每新增一個工具，應依序完成以下項目：

1. **Service 層**：新增 `I*Planner` 介面與 `*Planner` 實作（pure function）。
   若需要工具專屬的預覽欄位，建立繼承自 `OperationPreviewItem` 的子類別。
2. **PreviewViewModel**：新增 `ViewModels/Previews/*PreviewViewModel.cs`，
   實作 `IPreviewViewModel`，持有清單並計算 `Summary` / `HasErrors`。
3. **MainViewModel**：在對應的 `Preview*()` 方法中建立 PreviewViewModel 並指派給 `CurrentPreview`；
   新增 `HasExecutable*Preview()` CanExecute 方法。
4. **MainWindow.xaml**：在 `ContentControl.Resources` 裡加入對應型別的 `DataTemplate`，
   只需定義該工具需要的欄位，不影響其他工具的 DataTemplate。
5. **DI 註冊**：在 `App.xaml.cs` 的 `ConfigureServices` 中加入新 service。
6. **測試**：新增 `*PlannerTests.cs`，涵蓋正常路徑、邊界案例與錯誤路徑。

## TDD 要求

只要實務上可行，核心行為都應先寫測試。

以下功能需要測試：

- 自然排序檔名。
- 依副檔名過濾檔案。
- 抽幀刪除規劃。
- 批次改名規劃。
- 批次縮放規劃（百分比驗證、絕對尺寸驗證、目標尺寸計算、等比縮放、fit-within-box）。
- 衝突偵測。
- 勾選包含子資料夾時，每個資料夾各自計數的行為。
- ViewModel 的 CanExecute 邏輯（HasExecutable\*Preview 依 CurrentPreview 型別判斷）。
- 邊界案例，例如空資料夾、無效間隔、重複目標檔名、目標檔案已存在。

建議工作流程：

```text
1. 撰寫或更新一個會失敗的 unit test。
2. 實作最小範圍的 production code。
3. 執行測試。
4. 在測試維持通過的狀態下重構。
```

不要只依賴手動 GUI 測試驗證商業規則。GUI 測試可以補充 unit tests，但不能作為檔案操作邏輯的唯一驗證方式。

## SOLID 準則

### Single Responsibility

每個 class 應只有一個清楚的變更理由。

範例：

- `FileScanner` 負責掃描與排序檔案。
- `FrameDeletePlanner` 負責判斷哪些檔案應被刪除。
- `RenamePlanner` 負責建立改名計畫並偵測改名衝突。
- `FileOperationExecutor` 負責執行檔案系統副作用。

不要在同一個 method 混合操作規劃與實際執行。

### Open/Closed

新增工具時，應透過新增 planner、view model 或 service 來擴充，而不是修改無關邏輯。

例如未來新增圖片格式轉換功能時，不應修改 `FrameDeletePlanner`。

### Liskov Substitution

如果未來加入 interface，所有實作都必須維持相同的行為契約。除非 interface 明確允許，否則不要建立會靜默略過驗證或副作用的實作。

### Interface Segregation

interface 應保持小而明確。避免建立同時包含掃描、規劃、執行、log 與 UI 關注點的大型 service interface。

### Dependency Inversion

當有多個實作，或測試需要隔離時，ViewModels 應依賴 service abstraction。不要讓 ViewModels 直接建立具體檔案系統 services。

## Functional Programming 撰寫風格

規劃邏輯優先使用 pure functions。

pure planner 應該：

- 接收輸入資料與 options。
- 回傳 plan 或 result object。
- 不讀寫檔案。
- 不修改 shared state。
- 不顯示 dialog，也不接觸 WPF controls。

良好模式：

```text
files + options -> preview plan
```

不良模式：

```text
button click -> scan files -> mutate global state -> delete files immediately
```

表示事實、options 或 planned operations 的資料，實務上可行時應使用 immutable records。只有在 WPF binding 或漸進式 UI 更新需要時，才使用 mutable classes。

## 檔案操作安全規則

任何破壞性或批次操作，都必須先支援預覽再執行。

規則：

- 刪除操作預設應移到回收桶。
- 若未來加入永久刪除，必須要求明確 UI 確認。
- 改名操作必須偵測重複目標檔名。
- 改名操作必須偵測目前計畫外已存在的目標檔案。
- 需要避免撞名時，改名執行應使用暫存檔名中轉。
- 執行程式碼必須回傳包含成功與錯誤細節的 structured result。

## WPF 與 MVVM 規則

WPF/MVVM 的通用實作規範請以 `.agents/skills/dotnet-wpf-modern.md`、
`.agents/skills/wpf/SKILL.md` 與 `.agents/skills/mvvm/SKILL.md` 為準。
本節只保留 FrameFileTool 的專案特有邊界。

Views：

- 只應包含 layout、bindings、styles 與 templates。
- code-behind 僅可做初始化、wiring 與 `DataContext` 設定。
- 不得在 View 或 code-behind 實作檔案掃描、規劃、改名、刪除、縮放或自然排序規則。

ViewModels：

- 使用 CommunityToolkit.Mvvm 的 source generator 與 commands，避免手寫樣板式 `INotifyPropertyChanged`。
- UI 動作應透過 command 與明確的 `CanExecute` 控制。
- 應透過 constructor injection 取得 service abstraction，不直接建立具體 services。
- 長時間操作應使用 `async/await` 與可取消流程，避免同步阻塞 UI thread。
- 可以保存畫面狀態、驗證使用者輸入、轉換 service 結果為 UI collections、更新 log。
- 不應包含自然排序、抽幀選取、改名衝突偵測或縮放規劃演算法。
- `MainViewModel` 使用 `CurrentPreview` 搭配 `HasExecutable*Preview()` 判斷各工具可執行狀態。

Binding 與 UI 執行緒：

- 重要 binding、DataTemplate 與 command flow 必須能在 runtime 驗證。
- 背景工作完成後更新 UI state 時，優先使用 `async/await` 回到 UI context；只有必要時才直接使用 Dispatcher。
- 大量清單應考慮 virtualization、穩定欄寬、文字截斷與 tooltip，避免 UI 卡頓或內容溢出。
- 若未來需要 configuration、logging、hosted services 或更完整的 app lifecycle，
  應依 `dotnet-wpf-modern.md` 評估改用 generic Host builder。
  目前只有 DI 註冊與主視窗啟動時，可維持現有 `ServiceCollection` 啟動方式，避免無必要的大型重構。

## C# 語言版本規則

現代 C# 寫法請以 `.agents/skills/modern-csharp/SKILL.md` 為準。

- 使用新語法前，先確認專案實際 `TargetFramework`、`LangVersion`、SDK 與 `.editorconfig`。
- 本專案目標為 .NET 10；可使用該穩定語言版本支援的語法，但不得因本機 SDK 較新而使用 preview-only feature。
- 只有在能改善正確性、可讀性或維護性時才導入新語法。
- 不為了現代化而大規模重寫既有程式碼。
- 若語法選擇會影響架構、style rule 或 generated-code pattern，需同步檢查 build、test 與 `dotnet format`。

## C# 腳本規則

file-based C# app 請以 `.agents/skills/csharp-scripts.md` 為準，且僅在使用者明確要求快速 C# 實驗、
API 驗證或小型原型時使用。

- 不要用 file-based C# app 取代本專案正式功能、測試或維護腳本。
- 不要把臨時 `.cs` 腳本放在 `FrameFileTool/` 或 `FrameFileTool.Tests/` 目錄內，避免被 SDK-style project 自動納入編譯。
- 若臨時 C# 原型需要保留並整合進產品，應轉為正式 project code、unit tests 或文件化維護腳本。
- 對語言無關的一次性工作，優先使用既有 shell/PowerShell 工具，不強制改用 C# file-based app。

## UI 規則

UI/UX 設計規則、使用者流程、版面、狀態、表格、log 與 WPF 實作規範，
請以 [UI_UX_DESIGN_RULES.md](./UI_UX_DESIGN_RULES.md) 為唯一依據。

## Git 規則

Commit message 格式、type 清單、七大黃金規則與範例，請以 [COMMIT_CONVENTION.md](./COMMIT_CONVENTION.md) 為唯一依據。

分支命名格式、type 清單、生命週期與 TODO 整合規則，請以 [BRANCH_CONVENTION.md](./BRANCH_CONVENTION.md) 為唯一依據。

額外注意事項：

- 使用聚焦的小型 commits，每個 commit 只做一件事。
- 每次建立 git commit 前，都必須重新檢視 [README.md](./README.md) 是否需要同步更新功能、限制、指令或文件連結。
- 不要提交產生的 build output，例如 `bin/`、`obj/`、`.vs/` 或 packaged executables，除非使用者明確要求。

## 套件與工具

### 主專案 NuGet 套件

| 套件                                       | 版本    | 用途                                                                          |
| ------------------------------------------ | ------- | ----------------------------------------------------------------------------- |
| `CommunityToolkit.Mvvm`                    | 8.4.2   | `ObservableObject`、`[ObservableProperty]`、`[RelayCommand]` source generator |
| `Microsoft.Extensions.DependencyInjection` | 10.0.8  | DI 容器，於 `App.xaml.cs` 注冊所有 service 與 ViewModel                       |
| `Magick.NET-Q8-AnyCPU`                     | 14.13.1 | 批次縮放圖片，支援 PNG/JPG/JPEG/WebP/BMP，提供 Lanczos/Mitchell 等演算法      |

### 測試專案 NuGet 套件

| 套件                 | 版本   | 用途                                               |
| -------------------- | ------ | -------------------------------------------------- |
| `xUnit`              | 2.9.3  | 單元測試框架                                       |
| `FluentAssertions`   | 8.10.0 | 可讀性高的斷言語法（`result.Should().Be(...)` 等） |
| `NSubstitute`        | 5.3.0  | Mock 框架，用於隔離 service 依賴                   |
| `coverlet.collector` | 6.0.4  | 測試覆蓋率收集                                     |

### 程式碼品質工具

| 工具                | 設定檔               | 說明                               |
| ------------------- | -------------------- | ---------------------------------- |
| `dotnet format`     | `.editorconfig`      | 自動修正空白、換行、命名等格式違規 |
| `markdownlint-cli2` | `.markdownlint.json` | 檢查所有 `.md` 文件的格式規範      |

常用指令：

```powershell
# 自動修正所有格式問題
dotnet format

# 驗證格式（CI 用，有違規時回傳非零退出碼）
dotnet format --verify-no-changes --severity warn

# 執行所有單元測試
dotnet test

# 檢查 Markdown 文件
npx markdownlint-cli2 "*.md"
```

## CI/CD

GitHub Actions workflow 定義於 `.github/workflows/`。

| Workflow  | 觸發條件                     | 用途                                                    |
| --------- | ---------------------------- | ------------------------------------------------------- |
| `CI`      | push、pull request、手動執行 | 還原套件、build、test、檢查 `.editorconfig` 與 Markdown |
| `Release` | 推送 `v*` tag、手動執行      | 發佈 Windows x64 self-contained 單檔 zip                |

Release workflow 的建議使用方式：

```powershell
git tag v1.0.0
git push origin v1.0.0
```

手動執行 `Release` workflow 時，必須輸入 `version`，例如 `v1.0.0`。

## 目前限制

這個專案目標平台：

```text
.NET 10
WPF
Windows
```

開發機必須安裝 .NET 10 SDK，才能 build、run 與 test 專案。
