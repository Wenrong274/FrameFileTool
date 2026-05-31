# 發布規範

文件、說明與規則以繁體中文為主，符合 [AGENTS.md](./AGENTS.md) 語言規則。

---

## 版本號規則

採用 [Semantic Versioning](https://semver.org/) 格式：`MAJOR.MINOR.PATCH`

| 版本位  | 升版時機                                     | 範例            |
| ------- | -------------------------------------------- | --------------- |
| `MAJOR` | 不向下相容的重大變動，例如移除功能、架構大改 | `1.x.x → 2.0.0` |
| `MINOR` | 新增使用者可見功能，向下相容                 | `1.0.x → 1.1.0` |
| `PATCH` | 修正 bug、調整 UI 細節，不新增功能           | `1.1.0 → 1.1.1` |

- 版本號不包含前導零（`1.0.0`，不是 `01.00.00`）。
- Git tag 格式為 `v` 加版本號，例如 `v1.1.0`。
- `MAJOR = 0`（`0.x.x`）為開發初期，功能尚未穩定；`1.0.0` 代表對外穩定版本。

---

## 發布前檢查清單

建立 tag 前必須確認以下項目全部通過：

- [ ] 本次發布範圍內的 `TODO.md` 項目已全部標記為 `[x]`
- [ ] 本次發布包含的已完成功能已歸檔至 `DONE.md`，並記錄最終完成範圍與已知限制
- [ ] CI 全部通過（build、unit tests、`dotnet format`、Markdown lint）
- [ ] `README.md` 已反映本版新功能、限制或操作說明的變動
- [ ] `.github/release-notes/vX.Y.Z.md` 已建立，內容符合本規範的格式
- [ ] 確認 tag 尚未在遠端存在（`git tag` 清單中無重複）

---

## 發布流程

### 一般版本（功能版、修正版）

```text
1. 確認 master 已合併本版全部功能分支
2. 完成發布前檢查清單
3. 推送 master
   git push origin master
4. 在 master 建立 tag
   git tag v1.2.0
5. 推送 tag 以觸發 Release workflow
   git push origin v1.2.0
```

Release workflow 會以 tag 去除 `v` 後的版本號寫入 assembly、file 與 informational version，
供應用程式啟動時比對 GitHub 最新發布版本。

### Hotfix 版本（緊急修補）

```text
1. 從最新 master 建立 hotfix 分支
   git switch -c hotfix/<short-scope> master
2. 修補問題，遵循 COMMIT_CONVENTION.md 的 commit 規範
3. 合併回 master
   git switch master
   git merge --no-ff hotfix/<short-scope>
4. 完成發布前檢查清單（使用 PATCH 版本號）
5. 推送 master
   git push origin master
6. 建立 tag 並推送
   git tag v1.1.2
   git push origin v1.1.2
7. 刪除 hotfix 分支
   git branch -d hotfix/<short-scope>
```

### 手動觸發 workflow

GitHub Actions 的 `Release` workflow 支援手動執行，適用於 tag 已推送但 workflow 失敗需要重跑的情況：

1. 前往 GitHub Actions → Release → Run workflow。
2. 輸入版本號，例如 `v1.2.0`。
3. 確認對應的 `.github/release-notes/vX.Y.Z.md` 已存在。

---

## Release Notes 格式

Release notes 存放於 `.github/release-notes/vX.Y.Z.md`，
由 Release workflow 自動讀取作為 GitHub Release 的說明內容。

### 標題與摘要

```markdown
# FrameFileTool vX.Y.Z

一行簡短說明本版主題。
```

### 可用段落

依本版實際變動選用，不需全部出現：

| 段落          | 使用時機                                       |
| ------------- | ---------------------------------------------- |
| `## 主要功能` | 僅用於 MAJOR 版本首次發布，列出全部核心能力    |
| `## 新增`     | 有新增使用者可見功能時                         |
| `## 改善`     | 現有功能有明顯優化但未新增功能時               |
| `## 修正`     | 有 bug 修正時                                  |
| `## 發佈包`   | 每個版本都必須出現，說明壓縮包名稱與解壓縮指示 |
| `## 開發品質` | 測試、CI、架構或文件有值得記錄的改善時         |

### 發佈包段落格式（每版必填）

```markdown
## 發佈包

- `FrameFileTool-vX.Y.Z-win-x64.zip`
- Windows x64 self-contained 單檔發佈包。
- 解壓縮後執行 `FrameFileTool.exe`。
```

### 撰寫規則

- 項目以使用者視角描述，說明「什麼變了」，不說明「怎麼實作的」。
- 不包含 commit hash 或 PR 編號。
- 語言以繁體中文為主，技術詞可保留英文。
- 若無對應內容，段落可省略；不要出現空段落。

---

## 禁止事項

- ❌ CI 未通過時建立 tag
- ❌ 在功能分支上建立 tag（必須從 master）
- ❌ 略過 `.github/release-notes/vX.Y.Z.md`（workflow 會使用預設說明，缺乏本版資訊）
- ❌ 版本號語意不符（例如新增功能卻只升 PATCH）
- ❌ 強制推送已發布的 tag（`git push --force`）
- ❌ 未先推送 master 就推送 tag（tag 會指向遠端尚不存在的 commit）
- ❌ 在 `TODO.md` 仍有未完成項目時發布（除非明確標記為非本版範圍）
