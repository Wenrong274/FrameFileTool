using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels.Previews;
using NSubstitute;

namespace FrameFileTool.Tests.ViewModels;

public sealed partial class MainViewModelCanExecuteTests
{
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
}
