# AGENTS.md

## 專案

FrameFileTool 是一個 Windows WPF 桌面應用程式，用於處理序列圖檔的抽幀與批次檔名操作。

這個專案應維持為可長期維護的工具平台，而不是一次性的腳本。新增功能時，必須保持 UI、應用程式狀態、規劃邏輯與檔案執行邏輯彼此分離。

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

## 語言規則

文件、使用者說明與程式註解應以繁體中文為主要語言。

專案內所有 Markdown 文件（`.md`）必須通過 markdownlint 檢查。
設定規則定義於根目錄的 `.markdownlint.json`。

C# 程式碼風格遵循 Microsoft Code Style Guide，規則定義於根目錄的 `.editorconfig`，
由 IDE 與 `dotnet format` 強制執行，不另開說明文件。

以下情況可以使用英文作為輔助：

- 使用英文更清楚的技術詞，例如 `ViewModel`、`Service`、`pure function`、`executor`、`binding`。
- 類別、方法、屬性、命令與檔案名稱。
- commit message、套件名稱、framework 名稱與 CLI 指令。

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

Services
  負責檔案掃描、操作規劃與實際執行。

Models
  放置 services 與 view models 共用的 immutable 或簡單資料物件。
```

code-behind 可以初始化相依物件並設定 `DataContext`。code-behind 不應包含檔案操作規則、改名規則或抽幀規則。

## TDD 要求

只要實務上可行，核心行為都應先寫測試。

以下功能需要測試：

- 自然排序檔名。
- 依副檔名過濾檔案。
- 抽幀刪除規劃。
- 批次改名規劃。
- 衝突偵測。
- 勾選包含子資料夾時，每個資料夾各自計數的行為。
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

Views 只應包含 layout 與 bindings。

ViewModels 可以：

- 保存畫面狀態。
- 暴露 commands。
- 驗證使用者輸入。
- 將 service 結果轉換為 UI collections。
- 新增 log 訊息。

ViewModels 不應：

- 直接列舉檔案，除非委派給 service。
- 直接刪除或改名檔案，除非委派給 executor。
- 包含自然排序、抽幀選取或改名衝突演算法。

## UI 規則

這是一個工具型應用程式。UI 應偏向密集、清楚、可預期，而不是行銷式版面。

必要 UX 行為：

- 使用者必須先掃描或選擇資料夾，才能預覽。
- 使用者必須先預覽，才能執行。
- 預覽表格必須顯示原始檔名、動作、目標檔名或刪除狀態，以及驗證狀態。
- 錯誤必須顯示在 UI log。

## Git 規則

Commit message 格式、type 清單、七大黃金規則與範例，請以 [COMMIT_CONVENTION.md](./COMMIT_CONVENTION.md) 為唯一依據。

額外注意事項：

- 使用聚焦的小型 commits，每個 commit 只做一件事。
- 不要提交產生的 build output，例如 `bin/`、`obj/`、`.vs/` 或 packaged executables，除非使用者明確要求。

## 目前限制

這個專案目標平台：

```text
.NET 10
WPF
Windows
```

開發機必須安裝 .NET 10 SDK，才能 build、run 與 test 專案。
