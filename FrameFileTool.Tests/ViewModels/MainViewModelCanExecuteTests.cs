using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels;
using FrameFileTool.ViewModels.Previews;
using NSubstitute;

namespace FrameFileTool.Tests.ViewModels;

/// <summary>
/// 測試 MainViewModel 四個執行指令（抽幀刪除、批次改名、批次縮放、批次降噪）的
/// CanExecute 邏輯，驗證 CurrentPreview 型別判斷與批次執行中的保護機制。
/// </summary>
public sealed partial class MainViewModelCanExecuteTests
{
    // ── 共用工廠 ──────────────────────────────────────────────

    private static MainViewModel CreateSut(
        IFrameDeletePlanner? frameDeletePlanner = null,
        IRenamePlanner? renamePlanner = null,
        IFileOperationExecutor? executor = null,
        IFolderPickerService? folderPicker = null,
        IImageResizeExecutor? resizeExecutor = null,
        IFileExistenceService? fileExistenceService = null,
        IResizePreviewService? resizePreviewService = null,
        IOutputFolderResolver? outputFolderResolver = null,
        IDenoisePlanner? denoisePlanner = null,
        IDenoiseExecutor? denoiseExecutor = null,
        IDenoisePreviewService? denoisePreviewService = null,
        TimeSpan debounceDelay = default)
    {
        var scanner = Substitute.For<IFileScanner>();
        scanner.Scan(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<bool>())
            .Returns(new FileScanResult([], []));
        var effectiveOutputFolderResolver = outputFolderResolver ?? Substitute.For<IOutputFolderResolver>();
        if (outputFolderResolver is null)
        {
            effectiveOutputFolderResolver
                .ResolveForResize(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<ResizeOptions>())
                .Returns(call => new ResolvedOutputFolder(call.ArgAt<string>(1), WasAutoRedirected: false, LogMessage: null));
        }

        return new MainViewModel(
        scanner,
        frameDeletePlanner ?? Substitute.For<IFrameDeletePlanner>(),
        renamePlanner ?? Substitute.For<IRenamePlanner>(),
        executor ?? Substitute.For<IFileOperationExecutor>(),
        folderPicker ?? Substitute.For<IFolderPickerService>(),
        resizeExecutor ?? Substitute.For<IImageResizeExecutor>(),
        resizePreviewService ?? Substitute.For<IResizePreviewService>(),
        effectiveOutputFolderResolver,
        denoisePlanner ?? Substitute.For<IDenoisePlanner>(),
        denoiseExecutor ?? Substitute.For<IDenoiseExecutor>(),
        denoisePreviewService ?? Substitute.For<IDenoisePreviewService>(),
        fileExistenceService ?? Substitute.For<IFileExistenceService>(),
        Substitute.For<IFileImportService>(),
        Substitute.For<IUpdateService>(),
        Substitute.For<IExternalLinkService>(),
        debounceDelay);
    }

    // ── 預覽 ViewModel 建立輔助 ───────────────────────────────

    private static FrameDeletePreviewViewModel DeletePreview(bool withError = false) =>
        new(new List<OperationPreviewItem>
        {
            new() { ActionKind = withError ? OperationActionKind.Error : OperationActionKind.Delete, HasError = withError },
        });

    private static RenamePreviewViewModel RenamePreview(bool withError = false) =>
        new(new List<OperationPreviewItem>
        {
            new() { ActionKind = withError ? OperationActionKind.Error : OperationActionKind.Rename, HasError = withError },
        });

    private static RenamePreviewViewModel RenamePreviewWithValidItemAndError() =>
        new(new List<OperationPreviewItem>
        {
            new() { ActionKind = OperationActionKind.Rename },
            new() { ActionKind = OperationActionKind.Error, HasError = true },
        });

    private static RenamePreviewViewModel RenamePreviewWithExcludedSourceConflict()
    {
        var excludedItem = new OperationPreviewItem
        {
            FullPath = @"C:\imgs\b.png",
            ActionKind = OperationActionKind.Rename,
            TargetName = "c.png",
        };
        excludedItem.IsIncluded = false;

        return new RenamePreviewViewModel(
        [
            new()
            {
                FullPath = @"C:\imgs\a.png",
                ActionKind = OperationActionKind.Rename,
                TargetName = "b.png",
            },
            excludedItem,
        ]);
    }

    private static ResizePreviewViewModel ResizePreview(bool withError = false) =>
        new(new List<ResizePreviewItem>
        {
            new() { ActionKind = withError ? OperationActionKind.Error : OperationActionKind.Resize, HasError = withError },
        });

    private static DenoisePreviewViewModel DenoisePreview(bool withError = false) =>
        new(new List<OperationPreviewItem>
        {
            new() { ActionKind = withError ? OperationActionKind.Error : OperationActionKind.Denoise, HasError = withError },
        });

    // ════════════════════════════════════════════════════════
    // ExecuteFrameDelete CanExecute
    // ════════════════════════════════════════════════════════

    [Fact]
    public void ExecuteFrameDelete_預覽為null_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = null;

        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteFrameDelete_預覽為改名型別_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();

        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteFrameDelete_預覽有錯誤_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview(withError: true);

        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteFrameDelete_預覽正確且無錯誤_CanExecute應為true()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();

        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ExecuteFrameDelete_刪除項目取消勾選_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();
        var preview = (FrameDeletePreviewViewModel)sut.CurrentPreview;

        preview.Items[0].IsIncluded = false;

        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
        sut.PreviewSummary.Should().Be("共 0 個項目，預計刪除 0 個");
    }

    [Fact]
    public void ExecuteFrameDelete_縮放進行中且預覽正確_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();
        sut.ResizeTool.IsResizing = true;

        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteFrameDelete_複製模式_應先選擇資料夾再呼叫複製執行器()
    {
        var planner = Substitute.For<IFrameDeletePlanner>();
        planner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<int>(),
                FrameDeleteOutputMode.CopyKeptToTargetFolder,
                @"D:\out",
                Arg.Any<IReadOnlySet<string>>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Copy, TargetPath = @"D:\out\a.png" }]);
        var executor = Substitute.For<IFileOperationExecutor>();
        executor.CopyFilesToTargetFolder(Arg.Any<IEnumerable<OperationPreviewItem>>())
            .Returns(new OperationResult { SuccessCount = 1 });
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns(@"D:\out");
        var fileExistenceService = Substitute.For<IFileExistenceService>();
        fileExistenceService.GetExistingPaths(Arg.Any<IEnumerable<string>>())
            .Returns(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        var sut = CreateSut(
            frameDeletePlanner: planner,
            executor: executor,
            folderPicker: folderPicker,
            fileExistenceService: fileExistenceService);
        sut.FrameDeleteTool.OutputMode = FrameDeleteOutputMode.CopyKeptToTargetFolder;
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.CurrentPreview = new FrameDeletePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Copy },
        ]);

        sut.FrameDeleteTool.ExecuteCommand.Execute(null);

        folderPicker.Received(1).PickFolder(Arg.Any<string>());
        planner.Received(1).Plan(
            Arg.Any<IReadOnlyList<FileItem>>(),
            Arg.Any<int>(),
            FrameDeleteOutputMode.CopyKeptToTargetFolder,
            @"D:\out",
            Arg.Any<IReadOnlySet<string>>());
        executor.Received(1).CopyFilesToTargetFolder(Arg.Any<IEnumerable<OperationPreviewItem>>());
        executor.DidNotReceive().DeleteToRecycleBin(Arg.Any<IEnumerable<OperationPreviewItem>>());
    }

    [Fact]
    public void ExecuteFrameDelete_複製模式取消選擇資料夾_不應執行複製()
    {
        var executor = Substitute.For<IFileOperationExecutor>();
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns((string?)null);
        var sut = CreateSut(executor: executor, folderPicker: folderPicker);
        sut.FrameDeleteTool.OutputMode = FrameDeleteOutputMode.CopyKeptToTargetFolder;
        sut.CurrentPreview = new FrameDeletePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Copy },
        ]);

        sut.FrameDeleteTool.ExecuteCommand.Execute(null);

        executor.DidNotReceive().CopyFilesToTargetFolder(Arg.Any<IEnumerable<OperationPreviewItem>>());
    }

    // ════════════════════════════════════════════════════════
    // ExecuteRename CanExecute
    // ════════════════════════════════════════════════════════

    [Fact]
    public void ExecuteRename_預覽為null_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = null;

        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteRename_預覽為抽幀刪除型別_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();

        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteRename_預覽有錯誤_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview(withError: true);

        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteRename_預覽同時有可改名項目與錯誤列_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreviewWithValidItemAndError();

        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
        sut.HasPreviewErrors.Should().BeTrue();
    }

    [Fact]
    public void ExecuteRename_已勾選改名目標撞到未勾選來源檔_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreviewWithExcludedSourceConflict();

        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
        sut.HasPreviewErrors.Should().BeTrue();
    }

    [Fact]
    public void ExecuteRename_預覽正確且無錯誤_CanExecute應為true()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();

        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ExecuteRename_改名項目取消勾選_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();
        var preview = (RenamePreviewViewModel)sut.CurrentPreview;

        preview.Items[0].IsIncluded = false;

        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
        sut.PreviewSummary.Should().Be("共 0 個項目，預計改名 0 個");
    }

    [Fact]
    public void ExecuteRename_縮放進行中且預覽正確_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();
        sut.ResizeTool.IsResizing = true;

        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteRename_複製模式_應先選擇資料夾再呼叫複製改名執行器()
    {
        var planner = Substitute.For<IRenamePlanner>();
        planner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<IReadOnlySet<string>>(),
                RenameOutputMode.CopyToTargetFolder,
                @"D:\out")
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Copy, TargetPath = @"D:\out\F_0.png" }]);
        var executor = Substitute.For<IFileOperationExecutor>();
        executor.CopyRenamedFilesToTargetFolder(Arg.Any<IEnumerable<OperationPreviewItem>>())
            .Returns(new OperationResult { SuccessCount = 1 });
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns(@"D:\out");
        var fileExistenceService = Substitute.For<IFileExistenceService>();
        fileExistenceService.GetExistingPaths(Arg.Any<IEnumerable<string>>())
            .Returns(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        var sut = CreateSut(
            renamePlanner: planner,
            executor: executor,
            folderPicker: folderPicker,
            fileExistenceService: fileExistenceService);
        sut.RenameTool.OutputMode = RenameOutputMode.CopyToTargetFolder;
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.CurrentPreview = new RenamePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Copy },
        ]);

        sut.RenameTool.ExecuteCommand.Execute(null);

        folderPicker.Received(1).PickFolder(Arg.Any<string>());
        planner.Received(1).Plan(
            Arg.Any<IReadOnlyList<FileItem>>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IReadOnlySet<string>>(),
            RenameOutputMode.CopyToTargetFolder,
            @"D:\out");
        executor.Received(1).CopyRenamedFilesToTargetFolder(Arg.Any<IEnumerable<OperationPreviewItem>>());
        executor.DidNotReceive().RenameFiles(Arg.Any<IEnumerable<OperationPreviewItem>>());
    }

    [Fact]
    public void ExecuteRename_複製模式取消選擇資料夾_不應執行複製改名()
    {
        var executor = Substitute.For<IFileOperationExecutor>();
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns((string?)null);
        var sut = CreateSut(executor: executor, folderPicker: folderPicker);
        sut.RenameTool.OutputMode = RenameOutputMode.CopyToTargetFolder;
        sut.CurrentPreview = new RenamePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Copy },
        ]);

        sut.RenameTool.ExecuteCommand.Execute(null);

        executor.DidNotReceive().CopyRenamedFilesToTargetFolder(Arg.Any<IEnumerable<OperationPreviewItem>>());
    }

    [Fact]
    public void ExecuteRename_複製模式目標資料夾有衝突_應停止執行並記錄log()
    {
        var planner = Substitute.For<IRenamePlanner>();
        planner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<IReadOnlySet<string>>(),
                RenameOutputMode.CopyToTargetFolder,
                @"D:\out")
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Error, HasError = true }]);
        var executor = Substitute.For<IFileOperationExecutor>();
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns(@"D:\out");
        var sut = CreateSut(renamePlanner: planner, executor: executor, folderPicker: folderPicker);
        sut.RenameTool.OutputMode = RenameOutputMode.CopyToTargetFolder;
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.CurrentPreview = new RenamePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Copy },
        ]);

        sut.RenameTool.ExecuteCommand.Execute(null);

        executor.DidNotReceive().CopyRenamedFilesToTargetFolder(Arg.Any<IEnumerable<OperationPreviewItem>>());
        sut.Logs.Should().Contain(log => log.Contains("複製改名已停止"));
        sut.HasPreviewErrors.Should().BeTrue();
    }

    [Fact]
    public void ExecuteFrameDelete_複製模式目標資料夾有衝突_應停止執行並記錄log()
    {
        var planner = Substitute.For<IFrameDeletePlanner>();
        planner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<int>(),
                FrameDeleteOutputMode.CopyKeptToTargetFolder,
                @"D:\out",
                Arg.Any<IReadOnlySet<string>>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Error, HasError = true }]);
        var executor = Substitute.For<IFileOperationExecutor>();
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns(@"D:\out");
        var sut = CreateSut(frameDeletePlanner: planner, executor: executor, folderPicker: folderPicker);
        sut.FrameDeleteTool.OutputMode = FrameDeleteOutputMode.CopyKeptToTargetFolder;
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.CurrentPreview = new FrameDeletePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Copy },
        ]);

        sut.FrameDeleteTool.ExecuteCommand.Execute(null);

        executor.DidNotReceive().CopyFilesToTargetFolder(Arg.Any<IEnumerable<OperationPreviewItem>>());
        sut.Logs.Should().Contain(log => log.Contains("抽幀複製已停止"));
        sut.HasPreviewErrors.Should().BeTrue();
    }

    // ════════════════════════════════════════════════════════
    // ExecuteResize CanExecute
    // ════════════════════════════════════════════════════════

    [Fact]
    public void ExecuteResize_預覽為null_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = null;

        sut.ResizeTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteResize_預覽為改名型別_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();

        sut.ResizeTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteResize_預覽有錯誤_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview(withError: true);

        sut.ResizeTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteResize_預覽正確且無錯誤_CanExecute應為true()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview();

        sut.ResizeTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ExecuteResize_縮放項目取消勾選_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview();
        var preview = (ResizePreviewViewModel)sut.CurrentPreview;

        preview.Items[0].IsIncluded = false;

        sut.ResizeTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
        sut.PreviewSummary.Should().Be("共 0 個項目，預計縮放 0 個");
    }

    [Fact]
    public void ExecuteResize_縮放進行中且預覽正確_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview();
        sut.ResizeTool.IsResizing = true;

        sut.ResizeTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteResize_指定資料夾模式_應先選擇資料夾再執行縮放()
    {
        var resizePreviewService = Substitute.For<IResizePreviewService>();
        resizePreviewService
            .BuildPreviewAsync(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Is<ResizeOptions>(options => options.TargetFolderPath == @"D:\out"),
                Arg.Any<CancellationToken>())
            .Returns([new ResizePreviewItem { ActionKind = OperationActionKind.Resize, TargetPath = @"D:\out\a.png" }]);
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns(@"D:\out");
        var resizeExecutor = Substitute.For<IImageResizeExecutor>();
        resizeExecutor.ExecuteAsync(
                Arg.Any<IEnumerable<OperationPreviewItem>>(),
                Arg.Is<ResizeOptions>(options => options.TargetFolderPath == @"D:\out"),
                Arg.Any<IProgress<ResizeProgressReport>>(),
                Arg.Any<CancellationToken>())
            .Returns(new OperationResult { SuccessCount = 1 });
        var sut = CreateSut(
            folderPicker: folderPicker,
            resizePreviewService: resizePreviewService,
            resizeExecutor: resizeExecutor,
            debounceDelay: TimeSpan.Zero);
        sut.ResizeTool.OutputMode = ResizeOutputMode.TargetFolder;
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.CurrentPreview = new ResizePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Resize },
        ]);

        await sut.ResizeTool.ExecuteCommand.ExecuteAsync(null);

        folderPicker.Received(1).PickFolder(Arg.Any<string>());
        await resizeExecutor.Received(1).ExecuteAsync(
            Arg.Any<IEnumerable<OperationPreviewItem>>(),
            Arg.Is<ResizeOptions>(options => options.TargetFolderPath == @"D:\out"),
            Arg.Any<IProgress<ResizeProgressReport>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteResize_指定資料夾選到來源_應使用自動解析後路徑並寫入log()
    {
        var resizePreviewService = Substitute.For<IResizePreviewService>();
        resizePreviewService
            .BuildPreviewAsync(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<ResizeOptions>(),
                Arg.Any<CancellationToken>())
            .Returns([new ResizePreviewItem { ActionKind = OperationActionKind.Resize, TargetPath = @"C:\imgs\cloud\cloud_x0.5\a.png" }]);
        var resizeExecutor = Substitute.For<IImageResizeExecutor>();
        resizeExecutor
            .ExecuteAsync(
                Arg.Any<IEnumerable<OperationPreviewItem>>(),
                Arg.Any<ResizeOptions>(),
                Arg.Any<IProgress<ResizeProgressReport>>(),
                Arg.Any<CancellationToken>())
            .Returns(new OperationResult { SuccessCount = 1 });
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns(@"C:\imgs\cloud");
        var resolver = Substitute.For<IOutputFolderResolver>();
        resolver
            .ResolveForResize(@"C:\imgs\cloud", @"C:\imgs\cloud", Arg.Any<ResizeOptions>())
            .Returns(new ResolvedOutputFolder(
                @"C:\imgs\cloud\cloud_x0.5",
                WasAutoRedirected: true,
                LogMessage: @"輸出資料夾與來源相同，已自動改用：C:\imgs\cloud\cloud_x0.5"));
        var sut = CreateSut(
            folderPicker: folderPicker,
            resizeExecutor: resizeExecutor,
            resizePreviewService: resizePreviewService,
            outputFolderResolver: resolver);
        sut.SelectedFolder = @"C:\imgs\cloud";
        sut.ResizeTool.OutputMode = ResizeOutputMode.TargetFolder;
        sut.Files.Add(new FileItem(@"C:\imgs\cloud\a.png", @"C:\imgs\cloud", "a.png", ".png", 10));
        sut.CurrentPreview = ResizePreview();

        await sut.ResizeTool.ExecuteCommand.ExecuteAsync(null);

        sut.Logs.Should().Contain(log => log.Contains(@"C:\imgs\cloud\cloud_x0.5"));
        await resizeExecutor.Received(1).ExecuteAsync(
            Arg.Any<IEnumerable<OperationPreviewItem>>(),
            Arg.Is<ResizeOptions>(options => options.TargetFolderPath == @"C:\imgs\cloud\cloud_x0.5"),
            Arg.Any<IProgress<ResizeProgressReport>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteResize_自動子資料夾已有衝突_應停止執行並保留錯誤預覽()
    {
        var resizePreviewService = Substitute.For<IResizePreviewService>();
        resizePreviewService
            .BuildPreviewAsync(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<ResizeOptions>(),
                Arg.Any<CancellationToken>())
            .Returns([
                new ResizePreviewItem
                {
                    ActionKind = OperationActionKind.Error,
                    HasError = true,
                    Status = "目標檔案已存在",
                },
            ]);
        var resizeExecutor = Substitute.For<IImageResizeExecutor>();
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns(@"C:\imgs\cloud");
        var resolver = Substitute.For<IOutputFolderResolver>();
        resolver
            .ResolveForResize(@"C:\imgs\cloud", @"C:\imgs\cloud", Arg.Any<ResizeOptions>())
            .Returns(new ResolvedOutputFolder(
                @"C:\imgs\cloud\cloud_x0.5",
                WasAutoRedirected: true,
                LogMessage: @"輸出資料夾與來源相同，已自動改用：C:\imgs\cloud\cloud_x0.5"));
        var sut = CreateSut(
            folderPicker: folderPicker,
            resizeExecutor: resizeExecutor,
            resizePreviewService: resizePreviewService,
            outputFolderResolver: resolver);
        sut.SelectedFolder = @"C:\imgs\cloud";
        sut.ResizeTool.OutputMode = ResizeOutputMode.TargetFolder;
        sut.Files.Add(new FileItem(@"C:\imgs\cloud\a.png", @"C:\imgs\cloud", "a.png", ".png", 10));
        sut.CurrentPreview = ResizePreview();

        await sut.ResizeTool.ExecuteCommand.ExecuteAsync(null);

        await resizeExecutor.DidNotReceive().ExecuteAsync(
            Arg.Any<IEnumerable<OperationPreviewItem>>(),
            Arg.Any<ResizeOptions>(),
            Arg.Any<IProgress<ResizeProgressReport>>(),
            Arg.Any<CancellationToken>());
        sut.HasPreviewErrors.Should().BeTrue();
        sut.Logs.Should().Contain(log => log.Contains("縮放已停止"));
    }

    [Fact]
    public async Task ExecuteResize_指定資料夾模式取消選擇資料夾_不應執行縮放()
    {
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns((string?)null);
        var resizeExecutor = Substitute.For<IImageResizeExecutor>();
        var sut = CreateSut(folderPicker: folderPicker, resizeExecutor: resizeExecutor);
        sut.ResizeTool.OutputMode = ResizeOutputMode.TargetFolder;
        sut.CurrentPreview = new ResizePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Resize },
        ]);

        await sut.ResizeTool.ExecuteCommand.ExecuteAsync(null);

        await resizeExecutor.DidNotReceive().ExecuteAsync(
            Arg.Any<IEnumerable<OperationPreviewItem>>(),
            Arg.Any<ResizeOptions>(),
            Arg.Any<IProgress<ResizeProgressReport>>(),
            Arg.Any<CancellationToken>());
    }

    // ════════════════════════════════════════════════════════
    // 即時預覽觸發（Live Preview）
    // ════════════════════════════════════════════════════════

    [Fact]
    public void PreviewSummary_準備預覽中_應顯示忙碌文字()
    {
        var sut = CreateSut();

        sut.PreviewBusyText = "正在讀取圖片尺寸…";
        sut.IsPreparingPreview = true;

        sut.PreviewSummary.Should().Be("正在讀取圖片尺寸…");
    }

    [Fact]
    public void FrameDeleteInterval_變更且有檔案_應即時更新預覽()
    {
        var planner = Substitute.For<IFrameDeletePlanner>();
        planner.Plan(Arg.Any<IReadOnlyList<FileItem>>(), Arg.Any<int>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Delete }]);
        var sut = CreateSut(frameDeletePlanner: planner);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));

        sut.FrameDeleteTool.Interval = 3;

        sut.CurrentPreview.Should().BeOfType<FrameDeletePreviewViewModel>();
        sut.Logs.Should().Contain(log => log.Contains("抽幀預覽完成"));
        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void FrameDeleteOutputMode_指定資料夾模式變更且有檔案_應即時更新預覽但不選資料夾()
    {
        var planner = Substitute.For<IFrameDeletePlanner>();
        planner.Plan(Arg.Any<IReadOnlyList<FileItem>>(), Arg.Any<int>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Copy }]);
        var folderPicker = Substitute.For<IFolderPickerService>();
        var sut = CreateSut(frameDeletePlanner: planner, folderPicker: folderPicker);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));

        sut.FrameDeleteTool.OutputMode = FrameDeleteOutputMode.CopyKeptToTargetFolder;

        sut.CurrentPreview.Should().BeOfType<FrameDeletePreviewViewModel>();
        folderPicker.DidNotReceive().PickFolder(Arg.Any<string>());
    }

    [Fact]
    public void RenamePrefix_變更且有檔案_應即時更新預覽()
    {
        var planner = Substitute.For<IRenamePlanner>();
        planner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<IReadOnlySet<string>>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Rename }]);
        var sut = CreateSut(renamePlanner: planner);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.SelectedTool = PreviewTool.Rename;

        sut.RenameTool.Prefix = "New_";

        sut.CurrentPreview.Should().BeOfType<RenamePreviewViewModel>();
        sut.Logs.Should().Contain(log => log.Contains("改名預覽完成"));
        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void RenameOutputMode_指定資料夾模式變更且有檔案_應即時更新預覽但不選資料夾()
    {
        var planner = Substitute.For<IRenamePlanner>();
        planner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<IReadOnlySet<string>>(),
                RenameOutputMode.CopyToTargetFolder,
                "")
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Copy }]);
        var folderPicker = Substitute.For<IFolderPickerService>();
        var sut = CreateSut(renamePlanner: planner, folderPicker: folderPicker);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.SelectedTool = PreviewTool.Rename;

        sut.RenameTool.OutputMode = RenameOutputMode.CopyToTargetFolder;

        sut.CurrentPreview.Should().BeOfType<RenamePreviewViewModel>();
        folderPicker.DidNotReceive().PickFolder(Arg.Any<string>());
        planner.Received(1).Plan(
            Arg.Any<IReadOnlyList<FileItem>>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IReadOnlySet<string>>(),
            RenameOutputMode.CopyToTargetFolder,
            "");
    }

    [Fact]
    public async Task ScaleFactor_變更且有檔案_應觸發防抖縮放預覽()
    {
        var resizePreviewService = Substitute.For<IResizePreviewService>();
        resizePreviewService
            .BuildPreviewAsync(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<ResizeOptions>(),
                Arg.Any<CancellationToken>())
            .Returns([new ResizePreviewItem { ActionKind = OperationActionKind.Resize }]);
        var sut = CreateSut(resizePreviewService: resizePreviewService, debounceDelay: TimeSpan.Zero);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.SelectedTool = PreviewTool.Resize;

        sut.ResizeTool.ScaleFactor = 0.75;
        await sut.ResizeTool.LivePreviewTask;

        sut.CurrentPreview.Should().BeOfType<ResizePreviewViewModel>();
        sut.Logs.Should().Contain(log => log.Contains("縮放預覽完成"));
        sut.ResizeTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
        await resizePreviewService.Received(1).BuildPreviewAsync(
            Arg.Any<IReadOnlyList<FileItem>>(),
            Arg.Is<ResizeOptions>(options => options.ScaleFactor == 0.75),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void DenoiseSelectedMode_變更且有檔案_應立即觸發降噪預覽()
    {
        var denoisePlanner = Substitute.For<IDenoisePlanner>();
        denoisePlanner
            .Plan(Arg.Any<IReadOnlyList<FileItem>>(), Arg.Any<DenoiseOptions>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Denoise }]);
        var sut = CreateSut(denoisePlanner: denoisePlanner);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.SelectedTool = PreviewTool.Denoise;

        sut.DenoiseTool.SelectedMode = DenoiseMode.Strong;

        sut.CurrentPreview.Should().BeOfType<DenoisePreviewViewModel>();
        sut.Logs.Should().Contain(log => log.Contains("降噪預覽完成"));
        denoisePlanner.Received().Plan(
            Arg.Any<IReadOnlyList<FileItem>>(),
            Arg.Is<DenoiseOptions>(options => options.Mode == DenoiseMode.Strong));
    }

    [Fact]
    public async Task ResizeOutputMode_指定資料夾模式變更且有檔案_應預覽但不選資料夾()
    {
        var resizePreviewService = Substitute.For<IResizePreviewService>();
        resizePreviewService
            .BuildPreviewAsync(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<ResizeOptions>(),
                Arg.Any<CancellationToken>())
            .Returns([new ResizePreviewItem { ActionKind = OperationActionKind.Resize }]);
        var folderPicker = Substitute.For<IFolderPickerService>();
        var sut = CreateSut(
            folderPicker: folderPicker,
            resizePreviewService: resizePreviewService,
            debounceDelay: TimeSpan.Zero);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.SelectedTool = PreviewTool.Resize;

        sut.ResizeTool.OutputMode = ResizeOutputMode.TargetFolder;
        await sut.ResizeTool.LivePreviewTask;

        await resizePreviewService.Received().BuildPreviewAsync(
            Arg.Any<IReadOnlyList<FileItem>>(),
            Arg.Is<ResizeOptions>(options =>
                options.OutputMode == ResizeOutputMode.TargetFolder &&
                options.TargetFolderPath == ""),
            Arg.Any<CancellationToken>());
        folderPicker.DidNotReceive().PickFolder(Arg.Any<string>());
    }

    [Fact]
    public void ScaleFactorSlider_設定應涵蓋常用倍率且預設值落在範圍內()
    {
        var sut = CreateSut();

        sut.ResizeTool.ScaleFactorSliderMinimum.Should().Be(0.1);
        sut.ResizeTool.ScaleFactorSliderMaximum.Should().Be(4.0);
        sut.ResizeTool.ScaleFactorSliderSmallChange.Should().Be(0.1);
        sut.ResizeTool.ScaleFactor.Should().BeInRange(sut.ResizeTool.ScaleFactorSliderMinimum, sut.ResizeTool.ScaleFactorSliderMaximum);
    }

    [Fact]
    public void SelectedTool_切換到抽幀刪除且有檔案_應即時產生預覽()
    {
        var planner = Substitute.For<IFrameDeletePlanner>();
        planner.Plan(Arg.Any<IReadOnlyList<FileItem>>(), Arg.Any<int>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Delete }]);
        var sut = CreateSut(frameDeletePlanner: planner);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.SelectedTool = PreviewTool.Rename;

        sut.SelectedTool = PreviewTool.FrameDelete;

        sut.CurrentPreview.Should().BeOfType<FrameDeletePreviewViewModel>();
        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void SelectedTool_切換到批次改名且有檔案_應即時產生預覽()
    {
        var planner = Substitute.For<IRenamePlanner>();
        planner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<IReadOnlySet<string>>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Rename }]);
        var sut = CreateSut(renamePlanner: planner);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));

        sut.SelectedTool = PreviewTool.Rename;

        sut.CurrentPreview.Should().BeOfType<RenamePreviewViewModel>();
        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task SelectedTool_切換到批次縮放且有檔案_應觸發防抖縮放預覽()
    {
        var resizePreviewService = Substitute.For<IResizePreviewService>();
        resizePreviewService
            .BuildPreviewAsync(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<ResizeOptions>(),
                Arg.Any<CancellationToken>())
            .Returns([new ResizePreviewItem { ActionKind = OperationActionKind.Resize }]);
        var sut = CreateSut(resizePreviewService: resizePreviewService, debounceDelay: TimeSpan.Zero);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));

        sut.SelectedTool = PreviewTool.Resize;
        await sut.ResizeTool.LivePreviewTask;

        sut.CurrentPreview.Should().BeOfType<ResizePreviewViewModel>();
        sut.ResizeTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task 縮放設定連續變更_最終預覽反映最新設定()
    {
        var resizePreviewService = Substitute.For<IResizePreviewService>();
        resizePreviewService
            .BuildPreviewAsync(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<ResizeOptions>(),
                Arg.Any<CancellationToken>())
            .Returns([new ResizePreviewItem { ActionKind = OperationActionKind.Resize }]);

        var sut = CreateSut(resizePreviewService: resizePreviewService, debounceDelay: TimeSpan.Zero);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.SelectedTool = PreviewTool.Resize;

        // 快速連續變更：每次都取消前一個 debounce，只有最後一個會執行
        sut.ResizeTool.ScaleFactor = 0.6;
        sut.ResizeTool.ScaleFactor = 0.75;

        await sut.ResizeTool.LivePreviewTask;

        sut.CurrentPreview.Should().BeOfType<ResizePreviewViewModel>();
        sut.IsPreparingPreview.Should().BeFalse();
    }

    [Fact]
    public async Task 縮放預覽進行中切換工具_最終顯示新工具預覽()
    {
        var resizePending = new TaskCompletionSource<IReadOnlyList<ResizePreviewItem>>();
        var resizeCallCount = 0;

        var resizePreviewService = Substitute.For<IResizePreviewService>();
        resizePreviewService
            .BuildPreviewAsync(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<ResizeOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                resizeCallCount++;
                return resizePending.Task;
            });

        var renamePlanner = Substitute.For<IRenamePlanner>();
        renamePlanner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<IReadOnlySet<string>>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Rename }]);

        var sut = CreateSut(
            renamePlanner: renamePlanner,
            resizePreviewService: resizePreviewService,
            debounceDelay: TimeSpan.Zero);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));

        sut.SelectedTool = PreviewTool.Resize;

        // 切換到改名 Tab → 取消縮放 debounce（debounce 尚未進入 BuildPreviewAsync），
        // 同步產生改名預覽
        var taskBeforeSwitch = sut.ResizeTool.LivePreviewTask;
        sut.SelectedTool = PreviewTool.Rename;

        // 讓縮放 debounce 排程執行（確認即使它啟動也被取消）
        await taskBeforeSwitch;

        sut.CurrentPreview.Should().BeOfType<RenamePreviewViewModel>();
        sut.IsPreparingPreview.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════
    // Preview invalidation
    // ════════════════════════════════════════════════════════

    [Fact]
    public void FrameDeleteInterval_變更後_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();

        sut.FrameDeleteTool.Interval = 3;

        sut.CurrentPreview.Should().BeNull();
        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void FrameDeleteInterval_變更時_不應清除改名預覽()
    {
        var sut = CreateSut();
        var preview = RenamePreview();
        sut.CurrentPreview = preview;

        sut.FrameDeleteTool.Interval = 3;

        sut.CurrentPreview.Should().BeSameAs(preview);
        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void RenamePrefix_變更後_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();

        sut.RenameTool.Prefix = "New_";

        sut.CurrentPreview.Should().BeNull();
        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void RenamePrefix_變更時_不應清除抽幀預覽()
    {
        var sut = CreateSut();
        var preview = DeletePreview();
        sut.CurrentPreview = preview;

        sut.RenameTool.Prefix = "New_";

        sut.CurrentPreview.Should().BeSameAs(preview);
        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ScaleFactor_變更後_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview();

        sut.ResizeTool.ScaleFactor = 0.75;

        sut.CurrentPreview.Should().BeNull();
        sut.ResizeTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ScaleFactor_變更時_不應清除改名預覽()
    {
        var sut = CreateSut();
        var preview = RenamePreview();
        sut.CurrentPreview = preview;

        sut.ResizeTool.ScaleFactor = 0.75;

        sut.CurrentPreview.Should().BeSameAs(preview);
        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void IncludeSubfolders_變更後_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();

        sut.IncludeSubfolders = true;

        sut.CurrentPreview.Should().BeNull();
        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void SelectedTool_變更到不同工具_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();

        sut.SelectedTool = PreviewTool.Rename;

        sut.CurrentPreview.Should().BeNull();
        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void SelectedTool_變更到預覽所屬工具_不應清除既有預覽()
    {
        var sut = CreateSut();
        var preview = RenamePreview();
        sut.CurrentPreview = preview;

        sut.SelectedTool = PreviewTool.Rename;

        sut.CurrentPreview.Should().BeSameAs(preview);
        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ClearFolderAndFiles_執行後_應清空路徑與檔案且重設預覽()
    {
        var sut = CreateSut();
        sut.SelectedFolder = @"C:\imgs";
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.CurrentPreview = DeletePreview();

        sut.ClearFolderAndFilesCommand.Execute(null);

        sut.SelectedFolder.Should().BeEmpty();
        sut.Files.Should().BeEmpty();
        sut.CurrentPreview.Should().BeNull();
        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void RemoveFile_移除指定項目_應從檔案清單移出並重新計算預覽()
    {
        var planner = Substitute.For<IFrameDeletePlanner>();
        planner.Plan(Arg.Any<IReadOnlyList<FileItem>>(), Arg.Any<int>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Delete }]);

        var sut = CreateSut(frameDeletePlanner: planner);
        var itemA = new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10);
        var itemB = new FileItem(@"C:\imgs\b.png", @"C:\imgs", "b.png", ".png", 10);
        sut.Files.Add(itemA);
        sut.Files.Add(itemB);

        sut.RemoveFileCommand.Execute(itemA);

        sut.Files.Should().ContainSingle().Which.Should().BeSameAs(itemB);
        planner.Received(1).Plan(
            Arg.Is<IReadOnlyList<FileItem>>(list => list.Count == 1 && list[0] == itemB),
            Arg.Any<int>());
    }
}
