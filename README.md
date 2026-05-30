# 影格整理工具（FrameFileTool）

影格整理工具（FrameFileTool）是一個 Windows WPF 桌面工具，
用於處理序列圖檔的抽幀刪除、批次改名與批次縮放。

## 功能

- 選擇資料夾並掃描序列圖檔
- 可拖曳檔案或資料夾到預覽區新增資料
- 支援副檔名：PNG、JPG、JPEG、WEBP、BMP
- 自然排序檔名，例如 `1.png, 2.png, 10.png`
- 抽幀：每 N 張刪除 1 張
- 批次改名：例如 `A.png, B.png, C.png` 改成 `F_0.png, F_1.png, F_2.png`
- 批次縮放：支援百分比、絕對尺寸、等比縮放與置入指定範圍
- 縮放輸出可選擇覆寫原檔或輸出到子資料夾
- 所有操作都必須先預覽再執行
- 預覽表格可勾選要納入執行的檔案，錯誤列會停用並以顏色標示
- 抽幀刪除預設移到回收桶
- 改名使用暫存檔名中轉，降低撞名風險
- 勾選包含子資料夾時，每個資料夾會各自重新計數

## 開發環境需求

- Windows 10 / 11
- .NET 10 SDK
- Visual Studio 2022 或 JetBrains Rider（或 `dotnet` CLI）

## 開發環境初始設定

若需要讓 Claude、Codex 與 Antigravity CLI 共用同一份專案 skills，請執行：

```powershell
.\tools\setup-agent-skills.ps1
```

此腳本會將 `.claude\skills`、`.codex\skills` 與 `.antigravitycli\skills`
建立為指向 `.agents\skills` 的 Windows directory junction。

## 執行

```powershell
dotnet run --project .\FrameFileTool\FrameFileTool.csproj
```

## 測試

```powershell
dotnet test
```

## 程式碼品質檢查

```powershell
# 自動修正格式（空白、換行、命名）
dotnet format

# 驗證格式是否符合 .editorconfig（有違規則失敗，適合 CI）
dotnet format --verify-no-changes --severity warn

# 檢查所有 Markdown 文件
npx markdownlint-cli2 "*.md"
```

## CI/CD

本專案使用 GitHub Actions 執行 CI/CD。

| Workflow  | 觸發條件                     | 用途                                                    |
| --------- | ---------------------------- | ------------------------------------------------------- |
| `CI`      | push、pull request、手動執行 | 還原套件、build、test、檢查 `.editorconfig` 與 Markdown |
| `Release` | 推送 `v*` tag、手動執行      | 發佈 Windows x64 self-contained 單檔 zip                |

版本號規則、發布前檢查清單與完整發布流程，請見 [RELEASE_CONVENTION.md](RELEASE_CONVENTION.md)。

## 發佈單一 exe

```powershell
dotnet publish .\FrameFileTool\FrameFileTool.csproj `
  -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true
```

## 開發規範

本專案的開發規範、架構說明、套件清單與 commit 規則，請見：

- [AGENTS.md](AGENTS.md) — 開發規範總覽（唯一依據）
- [TODO.md](TODO.md) — 未完成功能與驗收條件
- [TODO_SPEC.md](TODO_SPEC.md) — 新增任務的規劃流程與歸檔規則
- [UI_UX_DESIGN_RULES.md](UI_UX_DESIGN_RULES.md) — UI/UX 設計規則
- [COMMIT_CONVENTION.md](COMMIT_CONVENTION.md) — Commit message 格式與範例
- [BRANCH_CONVENTION.md](BRANCH_CONVENTION.md) — 分支命名格式與生命週期
- [RELEASE_CONVENTION.md](RELEASE_CONVENTION.md) — 版本號規則、發布流程與 release notes 格式
