# Commit 規範

> 參考來源：https://hackmd.io/@howhow/git_commit

文件、說明與規則以繁體中文為主。Commit message 本體（type、subject、body、footer）以英文撰寫，符合 [AGENTS.md](./AGENTS.md) 語言規則。

---

## 格式結構

```
<type>(<scope>): <subject>

<body>

<footer>
```

| 區段 | 必填 | 說明 |
|------|------|------|
| **Header** | ✅ | type + 選填 scope + subject |
| **Body** | 選填 | 說明做了什麼、為什麼這樣做 |
| **Footer** | 選填 | Breaking change 或關閉 issue |

---

## Type 清單

| Type | 說明 |
|------|------|
| `feat` | 新增或修改功能 |
| `fix` | 修補 bug |
| `docs` | 文件變更 |
| `test` | 新增或修正測試 |
| `refactor` | 重構，非新功能也非修補 bug |
| `perf` | 效能改善 |
| `style` | 格式調整，不影響執行結果 |
| `build` | 影響建置系統或外部依賴（如 NuGet 套件）|
| `chore` | 建置程序或輔助工具的變動 |
| `ci` | CI 設定檔與腳本 |
| `hotfix` | 不影響主版本的緊急修補 |
| `revert` | 撤銷先前 commit |

---

## 七大黃金規則

1. 標題與 body 之間**空一行**
2. 標題限制 **50 字元**以內
3. 標題**首字大寫**
4. 標題結尾**不加句號**
5. 標題使用**祈使現在式**（`Add` 而非 `Added`、`Fix` 而非 `Fixed`）
6. Body 每行不超過 **72 字元**
7. Body 說明 **what** 與 **why**，不需說明 how

---

## Subject 撰寫規則

- 以英文撰寫
- 首字大寫
- 不超過 50 字元
- 結尾無句號
- 使用祈使現在式動詞開頭

---

## Body 撰寫規則

- 與 header 之間空一行
- 每行不超過 72 字元
- 說明「做了什麼」與「為什麼」，不需說明怎麼做
- 可使用項目符號

---

## Footer 撰寫規則

- `BREAKING CHANGE`：標記不向下相容的變動，並說明影響範圍
- `Closes #123`：關閉對應的 issue 編號

---

## 範例

### 單行（無 body）

```
feat(RenamePlanner): add zero-padding support
```

```
fix(FileOperationExecutor): handle missing file on rename
```

```
docs: update AGENTS.md target framework to net10
```

```
test(FrameDeletePlanner): add per-folder counter edge cases
```

```
refactor(MainViewModel): migrate to CommunityToolkit.Mvvm source generators
```

### 含 body

```
feat(FileScanner): support .webp and .bmp extensions

Previously only png/jpg/jpeg were enabled by default.
Users can now opt-in to webp and bmp via checkboxes
without modifying source code.
```

### 含 footer

```
refactor!: replace custom ObservableObject with CommunityToolkit.Mvvm

BREAKING CHANGE: ObservableObject and RelayCommand are removed.
All ViewModels must now inherit from
CommunityToolkit.Mvvm.ComponentModel.ObservableObject.
```

---

## 禁止事項

- ❌ 標題使用過去式（`Added`、`Fixed`）
- ❌ 單一 commit 混合多個無關變更
- ❌ 沒有說明原因的大型 commit
- ❌ 提交 build output（`bin/`、`obj/`、`.vs/`）
