using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels;
using FrameFileTool.ViewModels.Previews;
using NSubstitute;

namespace FrameFileTool.Tests.ViewModels;

/// <summary>
/// 測試 MainViewModel 三個執行指令（抽幀刪除、批次改名、批次縮放）的
/// CanExecute 邏輯，驗證 CurrentPreview 型別判斷與 IsResizing 保護機制。
/// </summary>
public sealed class MainViewModelCanExecuteTests
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
        TimeSpan debounceDelay = default)
    {
        var scanner = Substitute.For<IFileScanner>();
        scanner.Scan(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<bool>())
            .Returns(new FileScanResult([], []));

        return new MainViewModel(
        scanner,
        frameDeletePlanner ?? Substitute.For<IFrameDeletePlanner>(),
        renamePlanner ?? Substitute.For<IRenamePlanner>(),
        executor ?? Substitute.For<IFileOperationExecutor>(),
        folderPicker ?? Substitute.For<IFolderPickerService>(),
        resizeExecutor ?? Substitute.For<IImageResizeExecutor>(),
        resizePreviewService ?? Substitute.For<IResizePreviewService>(),
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

    // ════════════════════════════════════════════════════════
    // ExecuteFrameDelete CanExecute
    // ════════════════════════════════════════════════════════

    [Fact]
    public void ExecuteFrameDelete_預覽為null_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = null;

        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteFrameDelete_預覽為改名型別_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();

        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteFrameDelete_預覽有錯誤_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview(withError: true);

        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteFrameDelete_預覽正確且無錯誤_CanExecute應為true()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();

        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ExecuteFrameDelete_刪除項目取消勾選_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();
        var preview = (FrameDeletePreviewViewModel)sut.CurrentPreview;

        preview.Items[0].IsIncluded = false;

        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeFalse();
        sut.PreviewSummary.Should().Be("共 0 個項目，預計刪除 0 個");
    }

    [Fact]
    public void ExecuteFrameDelete_縮放進行中且預覽正確_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();
        sut.IsResizing = true;

        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeFalse();
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
        sut.FrameDeleteOutputMode = FrameDeleteOutputMode.CopyKeptToTargetFolder;
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.CurrentPreview = new FrameDeletePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Copy },
        ]);

        sut.ExecuteFrameDeleteCommand.Execute(null);

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
        sut.FrameDeleteOutputMode = FrameDeleteOutputMode.CopyKeptToTargetFolder;
        sut.CurrentPreview = new FrameDeletePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Copy },
        ]);

        sut.ExecuteFrameDeleteCommand.Execute(null);

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

        sut.ExecuteRenameCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteRename_預覽為抽幀刪除型別_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();

        sut.ExecuteRenameCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteRename_預覽有錯誤_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview(withError: true);

        sut.ExecuteRenameCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteRename_預覽同時有可改名項目與錯誤列_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreviewWithValidItemAndError();

        sut.ExecuteRenameCommand.CanExecute(null).Should().BeFalse();
        sut.HasPreviewErrors.Should().BeTrue();
    }

    [Fact]
    public void ExecuteRename_已勾選改名目標撞到未勾選來源檔_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreviewWithExcludedSourceConflict();

        sut.ExecuteRenameCommand.CanExecute(null).Should().BeFalse();
        sut.HasPreviewErrors.Should().BeTrue();
    }

    [Fact]
    public void ExecuteRename_預覽正確且無錯誤_CanExecute應為true()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();

        sut.ExecuteRenameCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ExecuteRename_改名項目取消勾選_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();
        var preview = (RenamePreviewViewModel)sut.CurrentPreview;

        preview.Items[0].IsIncluded = false;

        sut.ExecuteRenameCommand.CanExecute(null).Should().BeFalse();
        sut.PreviewSummary.Should().Be("共 0 個項目，預計改名 0 個");
    }

    [Fact]
    public void ExecuteRename_縮放進行中且預覽正確_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();
        sut.IsResizing = true;

        sut.ExecuteRenameCommand.CanExecute(null).Should().BeFalse();
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
        sut.RenameOutputMode = RenameOutputMode.CopyToTargetFolder;
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.CurrentPreview = new RenamePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Copy },
        ]);

        sut.ExecuteRenameCommand.Execute(null);

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
        sut.RenameOutputMode = RenameOutputMode.CopyToTargetFolder;
        sut.CurrentPreview = new RenamePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Copy },
        ]);

        sut.ExecuteRenameCommand.Execute(null);

        executor.DidNotReceive().CopyRenamedFilesToTargetFolder(Arg.Any<IEnumerable<OperationPreviewItem>>());
    }

    // ════════════════════════════════════════════════════════
    // ExecuteResize CanExecute
    // ════════════════════════════════════════════════════════

    [Fact]
    public void ExecuteResize_預覽為null_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = null;

        sut.ExecuteResizeCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteResize_預覽為改名型別_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();

        sut.ExecuteResizeCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteResize_預覽有錯誤_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview(withError: true);

        sut.ExecuteResizeCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteResize_預覽正確且無錯誤_CanExecute應為true()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview();

        sut.ExecuteResizeCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ExecuteResize_縮放項目取消勾選_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview();
        var preview = (ResizePreviewViewModel)sut.CurrentPreview;

        preview.Items[0].IsIncluded = false;

        sut.ExecuteResizeCommand.CanExecute(null).Should().BeFalse();
        sut.PreviewSummary.Should().Be("共 0 個項目，預計縮放 0 個");
    }

    [Fact]
    public void ExecuteResize_縮放進行中且預覽正確_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview();
        sut.IsResizing = true;

        sut.ExecuteResizeCommand.CanExecute(null).Should().BeFalse();
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
        sut.ResizeOutputMode = ResizeOutputMode.TargetFolder;
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.CurrentPreview = new ResizePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Resize },
        ]);

        await sut.ExecuteResizeCommand.ExecuteAsync(null);

        folderPicker.Received(1).PickFolder(Arg.Any<string>());
        await resizeExecutor.Received(1).ExecuteAsync(
            Arg.Any<IEnumerable<OperationPreviewItem>>(),
            Arg.Is<ResizeOptions>(options => options.TargetFolderPath == @"D:\out"),
            Arg.Any<IProgress<ResizeProgressReport>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteResize_指定資料夾模式取消選擇資料夾_不應執行縮放()
    {
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns((string?)null);
        var resizeExecutor = Substitute.For<IImageResizeExecutor>();
        var sut = CreateSut(folderPicker: folderPicker, resizeExecutor: resizeExecutor);
        sut.ResizeOutputMode = ResizeOutputMode.TargetFolder;
        sut.CurrentPreview = new ResizePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Resize },
        ]);

        await sut.ExecuteResizeCommand.ExecuteAsync(null);

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

        sut.FrameDeleteInterval = 3;

        sut.CurrentPreview.Should().BeOfType<FrameDeletePreviewViewModel>();
        sut.Logs.Should().Contain(log => log.Contains("抽幀預覽完成"));
        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeTrue();
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

        sut.FrameDeleteOutputMode = FrameDeleteOutputMode.CopyKeptToTargetFolder;

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
        sut.SelectedToolIndex = 1;

        sut.RenamePrefix = "New_";

        sut.CurrentPreview.Should().BeOfType<RenamePreviewViewModel>();
        sut.Logs.Should().Contain(log => log.Contains("改名預覽完成"));
        sut.ExecuteRenameCommand.CanExecute(null).Should().BeTrue();
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
        sut.SelectedToolIndex = 1;

        sut.RenameOutputMode = RenameOutputMode.CopyToTargetFolder;

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
        sut.SelectedToolIndex = 2;

        sut.ScaleFactor = 0.75;
        await sut.LivePreviewTask;

        sut.CurrentPreview.Should().BeOfType<ResizePreviewViewModel>();
        sut.Logs.Should().Contain(log => log.Contains("縮放預覽完成"));
        sut.ExecuteResizeCommand.CanExecute(null).Should().BeTrue();
        await resizePreviewService.Received(1).BuildPreviewAsync(
            Arg.Any<IReadOnlyList<FileItem>>(),
            Arg.Is<ResizeOptions>(options => options.ScaleFactor == 0.75),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DenoiseEnabled_變更且有檔案_應觸發防抖縮放預覽()
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
        sut.SelectedToolIndex = 2;

        sut.DenoiseEnabled = true;
        await sut.LivePreviewTask;

        sut.CurrentPreview.Should().BeOfType<ResizePreviewViewModel>();
        await resizePreviewService.Received(1).BuildPreviewAsync(
            Arg.Any<IReadOnlyList<FileItem>>(),
            Arg.Is<ResizeOptions>(options => options.DenoiseEnabled),
            Arg.Any<CancellationToken>());
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
        sut.SelectedToolIndex = 2;

        sut.ResizeOutputMode = ResizeOutputMode.TargetFolder;
        await sut.LivePreviewTask;

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

        sut.ScaleFactorSliderMinimum.Should().Be(0.1);
        sut.ScaleFactorSliderMaximum.Should().Be(4.0);
        sut.ScaleFactorSliderSmallChange.Should().Be(0.1);
        sut.ScaleFactor.Should().BeInRange(sut.ScaleFactorSliderMinimum, sut.ScaleFactorSliderMaximum);
    }

    [Fact]
    public void SelectedToolIndex_切換到抽幀刪除且有檔案_應即時產生預覽()
    {
        var planner = Substitute.For<IFrameDeletePlanner>();
        planner.Plan(Arg.Any<IReadOnlyList<FileItem>>(), Arg.Any<int>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Delete }]);
        var sut = CreateSut(frameDeletePlanner: planner);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.SelectedToolIndex = 1;

        sut.SelectedToolIndex = 0;

        sut.CurrentPreview.Should().BeOfType<FrameDeletePreviewViewModel>();
        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void SelectedToolIndex_切換到批次改名且有檔案_應即時產生預覽()
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

        sut.SelectedToolIndex = 1;

        sut.CurrentPreview.Should().BeOfType<RenamePreviewViewModel>();
        sut.ExecuteRenameCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task SelectedToolIndex_切換到批次縮放且有檔案_應觸發防抖縮放預覽()
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

        sut.SelectedToolIndex = 2;
        await sut.LivePreviewTask;

        sut.CurrentPreview.Should().BeOfType<ResizePreviewViewModel>();
        sut.ExecuteResizeCommand.CanExecute(null).Should().BeTrue();
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
        sut.SelectedToolIndex = 2;

        // 快速連續變更：每次都取消前一個 debounce，只有最後一個會執行
        sut.ScaleFactor = 0.6;
        sut.ScaleFactor = 0.75;

        await sut.LivePreviewTask;

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

        sut.SelectedToolIndex = 2;

        // 切換到改名 Tab → 取消縮放 debounce（debounce 尚未進入 BuildPreviewAsync），
        // 同步產生改名預覽
        var taskBeforeSwitch = sut.LivePreviewTask;
        sut.SelectedToolIndex = 1;

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

        sut.FrameDeleteInterval = 3;

        sut.CurrentPreview.Should().BeNull();
        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void FrameDeleteInterval_變更時_不應清除改名預覽()
    {
        var sut = CreateSut();
        var preview = RenamePreview();
        sut.CurrentPreview = preview;

        sut.FrameDeleteInterval = 3;

        sut.CurrentPreview.Should().BeSameAs(preview);
        sut.ExecuteRenameCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void RenamePrefix_變更後_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();

        sut.RenamePrefix = "New_";

        sut.CurrentPreview.Should().BeNull();
        sut.ExecuteRenameCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void RenamePrefix_變更時_不應清除抽幀預覽()
    {
        var sut = CreateSut();
        var preview = DeletePreview();
        sut.CurrentPreview = preview;

        sut.RenamePrefix = "New_";

        sut.CurrentPreview.Should().BeSameAs(preview);
        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ScaleFactor_變更後_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview();

        sut.ScaleFactor = 0.75;

        sut.CurrentPreview.Should().BeNull();
        sut.ExecuteResizeCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ScaleFactor_變更時_不應清除改名預覽()
    {
        var sut = CreateSut();
        var preview = RenamePreview();
        sut.CurrentPreview = preview;

        sut.ScaleFactor = 0.75;

        sut.CurrentPreview.Should().BeSameAs(preview);
        sut.ExecuteRenameCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void IncludeSubfolders_變更後_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();

        sut.IncludeSubfolders = true;

        sut.CurrentPreview.Should().BeNull();
        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void SelectedToolIndex_變更到不同工具_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();

        sut.SelectedToolIndex = 1;

        sut.CurrentPreview.Should().BeNull();
        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void SelectedToolIndex_變更到預覽所屬工具_不應清除既有預覽()
    {
        var sut = CreateSut();
        var preview = RenamePreview();
        sut.CurrentPreview = preview;

        sut.SelectedToolIndex = 1;

        sut.CurrentPreview.Should().BeSameAs(preview);
        sut.ExecuteRenameCommand.CanExecute(null).Should().BeTrue();
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
        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeFalse();
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
