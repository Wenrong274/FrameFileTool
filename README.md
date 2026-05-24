# FrameFileTool

FrameFileTool 是一個 Windows WPF 桌面工具，用於處理序列圖檔的抽幀刪除與批次改名。

## 初版功能

- 選擇資料夾並掃描序列圖檔
- 支援副檔名：PNG、JPG、JPEG、WEBP、BMP
- 自然排序檔名，例如 `1.png, 2.png, 10.png`
- 抽幀：每 N 張刪除 1 張
- 批次改名：例如 `A.png, B.png, C.png` 改成 `F_0.png, F_1.png, F_2.png`
- 所有操作都必須先預覽再執行
- 抽幀刪除預設移到回收桶
- 改名使用暫存檔名中轉，降低撞名風險
- 勾選包含子資料夾時，每個資料夾會各自重新計數

## 開發環境

- Windows 10/11
- .NET 10 SDK
- Visual Studio 2022 或 `dotnet` CLI

## 執行方式

```powershell
dotnet run --project .\FrameFileTool\FrameFileTool.csproj
```

## 發佈單一 exe

```powershell
dotnet publish .\FrameFileTool\FrameFileTool.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

## 開發規範

本專案的開發規範請見 [AGENTS.md](AGENTS.md)。
