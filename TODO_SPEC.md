# TODO 任務規劃規範

本文件定義新增 TODO 任務前的標準流程。
每個進入 [TODO.md](./TODO.md) 的功能，必須先完成規劃前置階段，再依序通過以下五個步驟。

## 規劃前置階段

任何新功能或計劃在進入五步驟前，必須先完成獨立的規劃前置階段：

1. **需求釐清（spec）**：釐清目標、使用情境、範圍邊界與明確不做的事。
2. **計劃產出（plan）**：根據釐清結果產出實作計劃草稿。

工具不限：可使用 superpowers 的 spec / plan skill、agent 內建的 plan mode，或手動撰寫。
但無論使用哪種工具，產出都必須轉寫為本文件的五步驟範本格式，才可寫入 `TODO.md`。
未經此階段的任務不得直接進入近期功能計劃。

## 完成後的歸檔規則

功能群組的所有子任務與完成判定均為 `[x]` 後，必須在同一輪 commit 中將該功能從
`TODO.md` 移入 [DONE.md](./DONE.md)。
歸檔 commit 在功能分支上完成，之後才合併回 master
（分支生命週期以 [BRANCH_CONVENTION.md](./BRANCH_CONVENTION.md) 為準）。
歸檔時依序執行以下檢查清單。

### 歸檔檢查清單

前提：所有子任務與完成判定均為 `[x]`，且完成判定由開發者親自確認
（見下方「完成判定的標記權限」）。

1. **改寫最終範圍**：檢視實際程式與文件改動，將 `TODO.md` 的該功能計劃更新為最終完成範圍。
   至少包含：實作結果、範圍調整、追加修正、仍需注意的限制。
2. **補完成日期**：在功能標題下方加入 `完成日期：YYYY-MM-DD`。
3. **補發布版本欄位**：在完成日期下方加入 `發布版本：未發布`；
   若已知納入版本則直接填 `發布版本：vX.Y.Z`。
   欄位格式與回填規則以 [DONE.md](./DONE.md) 的「發布版本標記」為準。
4. **搬移**：將完整功能區塊（含子任務與完成判定）移入 `DONE.md`，
   並自 `TODO.md` 刪除。不得只更新狀態標記而不搬移。
5. **評估發版**：依 `DONE.md` 的發版評估規則，決定是否建立新版本發布，並留下明確狀態。
6. **分支收尾**：依 [BRANCH_CONVENTION.md](./BRANCH_CONVENTION.md) 合併並刪除功能分支。

### 歸檔不變規則

- `TODO.md` 只保留進行中 `[~]` 與未開始 `[ ]` 的功能。
- `DONE.md` 保留完整的子任務與完成判定，供日後查閱實作細節。
- 功能標題不得使用流水號。每個功能必須保留原本的 `ID`，供依賴關係、分支與歸檔引用。

### 完成判定的標記權限

**完成判定的 `[x]` 只能由開發者在親自確認後手動標記，不得由 agent 代為勾選。**

理由：完成判定描述的是使用者可觀察的行為（UI 狀態、互動結果、log 訊息），
這些項目無法僅靠 `dotnet test` 通過來確認，必須由開發者實際執行後驗收。

- 子任務（實作步驟）：agent 完成實作後可標記 `[x]`。
- 完成判定（驗收條件）：開發者親自確認後才可標記 `[x]`，這是搬入 `DONE.md` 的前提。

## Git 分支規則

分支命名格式、type 清單、scope 規則、TODO 整合時機與生命週期，請以 [BRANCH_CONVENTION.md](./BRANCH_CONVENTION.md) 為唯一依據。

---

## 步驟一：分析可執行度

寫進 TODO 前，先確認這個功能「能不能做、難不難做」。

**必須回答的問題：**

1. **架構相容？** 新功能是否能在現有 Service / ViewModel / Model 結構上擴充，還是需要先改底層？
2. **有無現成基礎設施？** 需要的工具、套件、介面是否已存在？
3. **有無未解的設計決策？** 若有「還不確定怎麼做」的地方，先記為前置議題，不直接進計劃。
4. **測試是否可寫？** 核心邏輯能否用 unit test 驗證？若只能靠手動，標明原因。

**結論只有三種：**

- 可直接進計劃
- 需先解決前置條件（列出是什麼）
- 需先拆成更小的探索任務

---

## 步驟二：分析優先度

用三個維度決定這個任務排在哪裡：

| 維度               | 問題                                                             |
| ------------------ | ---------------------------------------------------------------- |
| **依賴性**         | 有沒有其他任務依賴這個？這個本身依賴誰？                         |
| **影響範圍**       | 完成後影響幾個工具或幾個層？改動越廣，風險越高，越需要優先評估。 |
| **使用者感知價值** | 沒有這個功能，工具用起來有多不順？                               |

**任務標頭格式：**

```markdown
ID：`short-kebab-id`
優先度：高 / 中 / 低
前置條件：[無] 或 [`other-id` 已完成]
被依賴：[無] 或 [`dependent-id`]
```

`ID` 必須穩定且不可因排序改變而重命名。建議使用與分支 scope 一致的 kebab-case，
例如 `output-folder`、`live-preview`、`clear-remove`。

---

## 步驟三：拆解任務

每個功能必須拆成可獨立驗收的子任務，按實作順序排列。

**拆解原則：**

- 每條子任務對應單一責任層（Model / Service / ViewModel / View / Test）
- 子任務不寫「實作 XXX 功能」，寫「在哪裡加什麼」
- 測試子任務緊接在對應邏輯子任務之後，不集中放在最後
- 若子任務之間有執行順序，用縮排或文字說明依賴方向

**標準拆解順序（對應本專案架構）：**

```text
1. Model：新增或調整資料結構
2. Service 介面：定義合約（若需新介面）
3. Service 實作（pure planner）
4. Service 測試（XxxPlannerTests）
5. PreviewViewModel
6. PreviewViewModel 測試
7. MainViewModel（CanExecute、觸發邏輯）
8. MainViewModel 測試
9. View / DataTemplate（XAML）
10. DI 註冊（App.xaml.cs）
```

---

## 步驟四：驗證方法

每個功能需要兩種驗證，分開列出。

### 自動驗證

每完成一個功能群組，必須跑：

```powershell
dotnet test -p:UseAppHost=false
dotnet format --verify-no-changes --severity warn
npx markdownlint-cli2 "*.md"
```

若有修改 UI，另需手動確認：

- 空狀態、錯誤狀態、忙碌狀態都能正確顯示。
- 預覽必須先產生，執行按鈕才可使用。
- 長檔名與大量檔案不造成表格排版破裂。
- Log 訊息能定位發生問題的檔案或資料夾。

### 完成判定

完成判定是功能層級的驗收條件，格式固定：

```markdown
- [ ] [觸發條件]，[預期結果]。
```

每個功能的完成判定至少涵蓋三條：

- **正常路徑**：主要使用情境成功執行。
- **邊界輸入**：極端或空白輸入的正確回應。
- **錯誤狀態**：無效操作時 UI 的反應（log 訊息、按鈕停用、錯誤標示）。

---

## 步驟五：注意事項

每個任務進計劃時，必須標記以下幾類已知風險。若某類不適用可省略。

### 跨層影響

若改動 Model 或 Service 介面，列出哪些地方需要同步修改：

```markdown
⚠ 影響範圍：XxxOptions → XxxPlanner → XxxPreviewService → MainViewModel → 所有相關測試
```

### 破壞性邊界案例

加入計劃前，至少想一個「壞掉的輸入」並確認子任務有覆蓋：

```markdown
⚠ 邊界案例：目標路徑含非法字元、清單為空時執行、全部取消勾選
```

### 設計決策待確認

若有尚未決定的細節，標記為議題，不寫進子任務。議題未解決前，任務不可開始執行：

```markdown
⚠ 待確認：目標資料夾不存在時，自動建立還是顯示錯誤並停用執行？
```

---

## 完整任務範本

```markdown
### [功能名稱]

ID：`short-kebab-id`
優先度：高 / 中 / 低
分支：feat/<short-scope>（開始時建立）
前置條件：[無] 或 [`other-id` 已完成]
被依賴：[無] 或 [`dependent-id`]

⚠ 影響範圍：[列出會被改動的層與檔案]
⚠ 邊界案例：[列出至少一個破壞性輸入]
⚠ 待確認：[有疑問的設計決策，若無則省略]

- [ ] [Model] 新增或調整 XXX 資料結構。
- [ ] [Service] 定義 IXxxPlanner 介面。
- [ ] [Service] 實作 XxxPlanner，支援 YYY 邏輯。
- [ ] [Test] 補上 XxxPlannerTests：正常路徑、邊界、錯誤。
- [ ] [PreviewViewModel] 新增 XxxPreviewViewModel，計算 Summary / HasErrors。
- [ ] [Test] 補上 XxxPreviewViewModelTests。
- [ ] [MainViewModel] 新增 PreviewXxx() 與 HasExecutableXxxPreview()。
- [ ] [Test] 補上 CanExecute 邏輯測試。
- [ ] [View] 在 ContentControl.Resources 加入對應型別的 DataTemplate。
- [ ] [DI] 在 App.xaml.cs ConfigureServices 註冊新 service。

完成判定：

- [ ] [正常路徑] 觸發條件，預期結果。
- [ ] [邊界] 觸發條件，預期結果。
- [ ] [錯誤狀態] 觸發條件，預期結果。
```

搬入 `DONE.md` 時，在功能標題下方加入：

```markdown
ID：`short-kebab-id`
完成日期：YYYY-MM-DD
發布版本：未發布
```
