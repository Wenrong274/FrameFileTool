# Code Health Audit 2026-06

本文件記錄 2026-06 發布後的程式碼健康檢查結果，目標是找出真正會影響後續功能開發、
發布穩定性或維護成本的項目，避免為了風格而重構。

## 目前狀態

- `master` 已發布 `v1.6.0`，本機與遠端同步。
- 本機完整驗證通過：
  - `dotnet test -p:UseAppHost=false`：309 passed。
  - `dotnet format --verify-no-changes --severity warn`：通過。
  - `npx markdownlint-cli2 "*.md" "docs/**/*.md" ".github/release-notes/*.md"`：0 errors。
- CI `Build, test, and lint` 已通過。
- `dotnet build FrameFileTool.sln --no-restore /warnaserror -v quiet`：0 warnings、0 errors。

## 主要發現

### 1. CI Markdown lint 覆蓋範圍不足

嚴重度：中  
建議優先度：高

本機發布前檢查已包含：

```powershell
npx markdownlint-cli2 "*.md" "docs/**/*.md" ".github/release-notes/*.md"
```

但 CI 目前只檢查：

```powershell
npx markdownlint-cli2@0.22.1 "*.md"
```

這代表 `docs/` 規劃文件與 `.github/release-notes/` 發布說明若格式錯誤，
本機可能抓得到，但 CI 不一定會阻擋。專案規則要求 Markdown 文件都需通過 markdownlint，
因此 CI 應與本機發布前檢查一致。

建議處理：先修 CI lint pattern。這是低風險、高回報的小型品質修正。

### 2. `MainViewModelCanExecuteTests.cs` 過大

嚴重度：中  
建議優先度：中

目前最大 C# 檔案為：

| 檔案 | 行數 |
| ---- | ---- |
| `FrameFileTool.Tests/ViewModels/MainViewModelCanExecuteTests.cs` | 1282 |
| `FrameFileTool/ViewModels/MainViewModel.cs` | 651 |
| `FrameFileTool/ViewModels/Tools/ResizeToolViewModel.cs` | 344 |

`MainViewModelCanExecuteTests.cs` 同時涵蓋四個工具的 CanExecute、執行流程、即時預覽與跨工具 busy gating。
這不是立即 bug，但後續加入 Spritesheet Packer 時，該檔會繼續膨脹，review 成本會升高。

建議處理：下一輪小型重構可將此測試檔拆成工具別 partial test 檔或共用 fixture + 多個測試類別。
拆分時不改 production code，不改測試斷言，只搬移測試與共用 helper。

### 3. Broad exception handling 需要定期複查

嚴重度：低到中  
建議優先度：中

目前 `FileOperationExecutor`、`ImageResizeExecutor`、`DenoiseExecutor` 等檔案有多個 `catch (Exception ex)`。
這些區域多半位於檔案操作或影像處理邊界， broad catch 有合理性：批次處理不應因單一檔案失敗中斷整批。

風險在於錯誤分類與 log 訊息必須足夠清楚，否則使用者只會看到泛用錯誤。

建議處理：暫不重構；等下一輪處理 executor 時，再逐一確認錯誤訊息與測試覆蓋。

### 4. Production 大檔目前不是第一優先

嚴重度：低  
建議優先度：低

`MainViewModel.cs` 已經從先前的大型 ViewModel 拆出工具 ViewModel，目前 651 行仍偏大，
但主要責任已集中在掃描狀態、共用預覽、log、更新檢查與工具協調。

建議處理：不要為行數而拆。只有在新增 Spritesheet Packer 時出現重複 flow 或 constructor 壓力，
再針對當下需求收斂。

## 建議處理順序

1. 立即修正 CI Markdown lint 覆蓋範圍。
2. 下一輪重構拆分 `MainViewModelCanExecuteTests.cs`。
3. Spritesheet Packer 研究前，先確認測試拆分是否足夠降低新增工具成本。
4. Executor 例外處理等到下一次碰檔案操作 pipeline 時再一併檢查。

## 本輪選定處理項目

本輪先處理「CI Markdown lint 覆蓋範圍不足」。

理由：

- 風險明確，且已和本機發布前檢查不一致。
- 改動範圍小，只需調整 workflow。
- 能立刻降低 release notes 與規劃文件漏檢風險。
- 不會打斷後續較大的測試拆分重構。
