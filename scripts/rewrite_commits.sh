#!/usr/bin/env bash
# 依 GIT_COMMIT SHA 對應輸出新的繁體中文 commit message

case "$GIT_COMMIT" in

  # feat: 初始化 WPF 抽幀工具專案
  3e87990bdb76fac75559523d4ba626fcff9d0f8f)
    printf 'feat: 初始化 WPF 抽幀工具專案'
    ;;

  # feat: 依資料夾各自計算序列編號
  48196dbf12d152a680669e2846aca70f58cb995a)
    printf 'feat: 依資料夾各自計算序列編號'
    ;;

  # docs: 新增開發指引
  f9b284e623f2287b993a00131adfa08a4237c3f6)
    printf 'docs: 新增開發指引'
    ;;

  # docs: 記錄語言規範
  a33089a2cf64e66a6aeaacf2535fba2bf2a74aa0)
    printf 'docs: 記錄語言規範'
    ;;

  # docs: 將專案文件翻譯為繁體中文
  cc44333e1e20768b3ea054a43e346a0c0d659f2d)
    printf 'docs: 將專案文件翻譯為繁體中文'
    ;;

  # docs: 新增 CLAUDE.md 與 .editorconfig
  bd16767883b60c29453ba4165eee48e7d12c6332)
    printf 'docs: 新增 CLAUDE.md 與 .editorconfig\n\nCo-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>'
    ;;

  # refactor: 抽取 Service 介面並將 OperationResult 移至 Models
  76bb0ff02998b5eaf9b8db5eea0257a337d84a1b)
    printf 'refactor: 抽取 Service 介面並將 OperationResult 移至 Models\n\n- 新增 IFileScanner、IFrameDeletePlanner、IRenamePlanner、\n  IFileOperationExecutor、IFolderPickerService\n- 將 OperationResult 從 FileOperationExecutor 移至 Models/\n\nCo-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>'
    ;;

  # refactor: 改用 CommunityToolkit.Mvvm 並建立 DI 容器
  28c3db884c442efc7e73a3ae36651c0be4f4ea5b)
    printf 'refactor: 改用 CommunityToolkit.Mvvm 並建立 DI 容器\n\n- 以 CommunityToolkit.Mvvm 取代自訂 ObservableObject 與 RelayCommand\n- MainViewModel 改用 [ObservableProperty] 與 [RelayCommand]\n- App.xaml.cs 建立 DI 容器，統一注冊所有 service\n- MainWindow 改為建構子注入 MainViewModel\n\nCo-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>'
    ;;

  # test: 新增 xUnit 測試專案，共 25 個測試通過
  81667188c1836b4fd667f2318daad447a59bf42e)
    printf 'test: 新增 xUnit 測試專案，共 25 個測試通過\n\n- 套件：xUnit、FluentAssertions、NSubstitute\n- NaturalStringComparerTests：自然排序與 null 邊界\n- FrameDeletePlannerTests：抽幀邏輯、子資料夾計數、邊界案例\n- RenamePlannerTests：改名計畫、補零、衝突偵測\n\nCo-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>'
    ;;

  # build: 升級目標框架至 net10.0-windows
  10c5ef6a125f40b4dacda0e1496cee0cbe884248)
    printf 'build: 升級目標框架至 net10.0-windows\n\nCo-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>'
    ;;

  # docs: 新增 COMMIT_CONVENTION.md 並更新 AGENTS.md Git 規則
  b2e97187070f4de5d4a92ab5068ab201f489d5b8)
    printf 'docs: 新增 COMMIT_CONVENTION.md 並更新 AGENTS.md Git 規則\n\n- 依據 hackmd.io/@howhow/git_commit 新增 COMMIT_CONVENTION.md\n- 內容包含 type 清單、七大黃金規則、範例\n- 更新 AGENTS.md Git 章節，指向 COMMIT_CONVENTION.md 為唯一依據\n\nCo-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>'
    ;;

  # docs: 將 Git_Commit.md 重新命名為 COMMIT_CONVENTION.md
  b25065a3ebe72b7ccb0af408af58b5e86fccb785)
    printf 'docs: 將 Git_Commit.md 重新命名為 COMMIT_CONVENTION.md\n\n- 重新命名以符合大寫規範檔慣例\n- 依 AGENTS.md 語言規則改寫說明為繁體中文\n- 同步更新 AGENTS.md 連結\n\nCo-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>'
    ;;

  # docs: 強制所有 Markdown 文件符合 markdownlint 規則
  64d0052144a588ce402624066b460324893aea2d)
    printf 'docs: 強制所有 Markdown 文件符合 markdownlint 規則\n\n- 新增 .markdownlint.json（行長 120，code block 200）\n- 在 AGENTS.md 加入 markdownlint 要求\n- 修正 COMMIT_CONVENTION.md：MD034、MD040、MD060\n\nCo-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>'
    ;;

  # style: 擴充 .editorconfig，補齊 Microsoft C# 程式碼風格規則
  5cf867c8a0788d3c603c591997a9bee0ff9c0eb5)
    printf 'style: 擴充 .editorconfig，補齊 Microsoft C# 程式碼風格規則\n\n- 命名：存取修飾詞、介面 I 前綴、私有欄位底線\n- var：型別明確時使用，內建型別明確標示\n- Pattern matching、Null 安全、expression body\n- 現代 C# 語法：using 宣告、primary constructor\n- 更新 AGENTS.md，標記 .editorconfig 為唯一依據\n\nCo-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>'
    ;;

  *)
    cat
    ;;
esac
