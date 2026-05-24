# Commit 規範

> 參考來源：<https://hackmd.io/@howhow/git_commit>

文件、說明與規則以繁體中文為主，符合 [AGENTS.md](./AGENTS.md) 語言規則。

---

## 格式結構

```text
<type>(<scope>): <subject>

<body>

<footer>
```

| 區段       | 必填 | 說明                         |
| ---------- | ---- | ---------------------------- |
| **Header** | ✅   | type + 選填 scope + subject  |
| **Body**   | 選填 | 說明做了什麼、為什麼這樣做   |
| **Footer** | 選填 | Breaking change 或關閉 issue |

---

## 語言規則

| 區段      | 語言         | 原因                                             |
| --------- | ------------ | ------------------------------------------------ |
| `type`    | 英文         | Conventional Commits 國際標準，工具整合需要      |
| `scope`   | 英文         | 對應程式碼識別符（類別、模組名稱）               |
| `subject` | **繁體中文** | 與專案語言規則一致                               |
| `body`    | **繁體中文** | 與專案語言規則一致                               |
| `footer`  | 混合         | `BREAKING CHANGE`、`Closes` 保持英文，說明用中文 |

---

## Type 清單

| Type       | 說明                                    |
| ---------- | --------------------------------------- |
| `feat`     | 新增或修改功能                          |
| `fix`      | 修補 bug                                |
| `docs`     | 文件變更                                |
| `test`     | 新增或修正測試                          |
| `refactor` | 重構，非新功能也非修補 bug              |
| `perf`     | 效能改善                                |
| `style`    | 格式調整，不影響執行結果                |
| `build`    | 影響建置系統或外部依賴（如 NuGet 套件） |
| `chore`    | 建置程序或輔助工具的變動                |
| `ci`       | CI 設定檔與腳本                         |
| `hotfix`   | 不影響主版本的緊急修補                  |
| `revert`   | 撤銷先前 commit                         |

---

## 七大黃金規則

1. 標題與 body 之間**空一行**
2. 標題限制 **50 字元**以內
3. 標題結尾**不加句號**
4. Subject 使用**動詞開頭**（新增、修正、移除、重構⋯）
5. Body 每行不超過 **72 字元**
6. Body 說明 **做了什麼** 與 **為什麼**，不需說明怎麼做
7. 每個 commit 只做**一件事**

---

## Subject 撰寫規則

- 以繁體中文撰寫
- 動詞開頭（新增、修正、移除、重構、升級⋯）
- 不超過 50 字元
- 結尾無句號

---

## Body 撰寫規則

- 與 header 之間空一行
- 每行不超過 72 字元
- 說明「做了什麼」與「為什麼」
- 可使用項目符號

---

## Footer 撰寫規則

- `BREAKING CHANGE`：標記不向下相容的變動，說明以繁體中文補充
- `Closes #123`：關閉對應的 issue 編號

---

## 範例

### 單行（無 body）

```text
feat(RenamePlanner): 新增補零位數支援
```

```text
fix(FileOperationExecutor): 修正改名時檔案不存在的處理
```

```text
docs: 更新 AGENTS.md 目標框架為 net10
```

```text
test(FrameDeletePlanner): 新增子資料夾各自計數的邊界測試
```

```text
refactor(MainViewModel): 改用 CommunityToolkit.Mvvm source generator
```

### 含 body

```text
feat(FileScanner): 支援 .webp 與 .bmp 副檔名

原本預設只啟用 png/jpg/jpeg。
使用者現在可透過勾選框選擇 webp 與 bmp，
不需修改原始碼。
```

### 含 footer

```text
refactor!: 以 CommunityToolkit.Mvvm 取代自訂 ObservableObject

BREAKING CHANGE: ObservableObject 與 RelayCommand 已移除。
所有 ViewModel 必須改為繼承
CommunityToolkit.Mvvm.ComponentModel.ObservableObject。
```

---

## 禁止事項

- ❌ subject 使用過去式動詞（已新增、已修正）
- ❌ 單一 commit 混合多個無關變更
- ❌ 沒有說明原因的大型 commit
- ❌ 提交 build output（`bin/`、`obj/`、`.vs/`）
