# 分支命名規範

文件、說明與規則以繁體中文為主，符合 [AGENTS.md](./AGENTS.md) 語言規則。

---

## 格式結構

```text
<type>/<short-scope>
```

| 區段          | 規則                                                          |
| ------------- | ------------------------------------------------------------- |
| `type`        | 小寫英文，與 commit message 的 type 對應                      |
| `short-scope` | 小寫英文與連字號，簡短描述功能範圍，與 commit 的 `scope` 一致 |

---

## Type 清單

| Type       | 說明                           | 範例                           |
| ---------- | ------------------------------ | ------------------------------ |
| `feat`     | 新功能開發                     | `feat/live-preview`            |
| `fix`      | 修補 bug                       | `fix/rename-conflict`          |
| `hotfix`   | 緊急修補，直接從 `master` 開出 | `hotfix/crash-on-empty-folder` |
| `refactor` | 重構，不新增功能也不修補 bug   | `refactor/planner-interface`   |
| `docs`     | 純文件變更                     | `docs/update-readme`           |
| `test`     | 補充或修正測試                 | `test/resize-planner-edge`     |
| `chore`    | 建置、CI、工具設定             | `chore/update-nuget`           |

---

## Scope 命名規則

- 使用小寫英文與連字號（kebab-case）
- 對應該功能在 commit message 中慣用的 scope 名稱
- 不超過 30 個字元
- 避免使用數字編號（用語意名稱，不用 `feat/feature-2`）

---

## 分支生命週期

```text
1. 開始新功能時，從最新的 master 建立分支
   git switch -c feat/<short-scope> master

2. 在分支上開發，遵循 COMMIT_CONVENTION.md 的 commit 規範

3. 功能完成並由開發者完成驗收後，合併回 master
   git switch master
   git merge --no-ff feat/<short-scope>

4. 合併後刪除功能分支
   git branch -d feat/<short-scope>

5. 將功能從 TODO.md 移入 DONE.md
```

---

## TODO 整合規則

- 每個 `TODO.md` 的功能群組對應一個獨立分支。
- 功能標記為 `[~]`（進行中）時，必須同步建立分支並在 `TODO.md` 標頭填入分支名稱。
- 功能仍為 `[ ]`（未開始）時，標頭填入 `feat/<short-scope>（開始時建立）`。

---

## 禁止事項

- ❌ 直接在 `master` 上開發功能
- ❌ 分支名稱使用中文
- ❌ 分支名稱使用大寫或底線（使用連字號）
- ❌ 分支名稱使用功能編號（`feat/feature-2`）
- ❌ 功能完成後不刪除分支，造成遠端分支堆積

---

## 範例

```text
feat/checkbox          ← 可勾選要變動的檔案
feat/output-folder     ← 指定輸出資料夾
feat/scale-factor      ← 批次縮放改用倍率輸入
feat/live-preview      ← 即時自動預覽
feat/clear-remove      ← 清空全部與剔除檔案
fix/drop-import        ← 修正拖放匯入的路徑解析
hotfix/executor-crash  ← 緊急修補 executor 例外
```
