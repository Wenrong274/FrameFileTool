# FrameFileTool

Windows WPF desktop tool for image sequence frame thinning and batch renaming.

## 初版功能

- 選擇資料夾並掃描序列圖檔
- 支援副檔名：PNG、JPG、JPEG、WEBP、BMP
- 自然排序檔名，例如 `1.png, 2.png, 10.png`
- 抽幀：每 N 張刪除 1 張
- 批次改名：例如 `A.png, B.png, C.png` 改成 `F_0.png, F_1.png, F_2.png`
- 所有操作先預覽再執行
- 抽幀刪除預設移到回收桶
- 改名使用暫存檔名中轉，降低撞名風險

## 開發環境

- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 或 `dotnet` CLI

## 執行

```powershell
dotnet run --project .\FrameFileTool\FrameFileTool.csproj
```

## 發佈單一 exe

```powershell
dotnet publish .\FrameFileTool\FrameFileTool.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```
