using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels.Previews;
using NSubstitute;

namespace FrameFileTool.Tests.ViewModels;

public sealed partial class MainViewModelCanExecuteTests
{
    // ════════════════════════════════════════════════════════
    // ExecuteDenoise CanExecute
    // ════════════════════════════════════════════════════════

    [Fact]
    public void ExecuteDenoise_預覽為null_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = null;

        sut.DenoiseTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteDenoise_預覽為縮放型別_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview();

        sut.DenoiseTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteDenoise_預覽有錯誤_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DenoisePreview(withError: true);

        sut.DenoiseTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
        sut.HasPreviewErrors.Should().BeTrue();
    }

    [Fact]
    public void ExecuteDenoise_預覽正確且無錯誤_CanExecute應為true()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DenoisePreview();

        sut.DenoiseTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ExecuteDenoise_降噪項目取消勾選_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DenoisePreview();
        var preview = (DenoisePreviewViewModel)sut.CurrentPreview;

        preview.Items[0].IsIncluded = false;

        sut.DenoiseTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteDenoise_縮放進行中且預覽正確_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DenoisePreview();
        sut.ResizeTool.IsResizing = true;

        sut.DenoiseTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteResize_降噪進行中且預覽正確_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview();
        sut.DenoiseTool.IsDenoising = true;

        sut.ResizeTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteFrameDelete_降噪進行中且預覽正確_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();
        sut.DenoiseTool.IsDenoising = true;

        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════
    // 降噪局部預覽（放大比較） CanExecute
    // ════════════════════════════════════════════════════════

    [Fact]
    public void GenerateDenoisePreviewCommand_無檔案_CanExecute應為False()
    {
        var sut = CreateSut();

        sut.DenoiseTool.GenerateDenoisePreviewCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void GenerateDenoisePreviewCommand_有檔案_CanExecute應為True()
    {
        var sut = CreateSut();
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));

        sut.DenoiseTool.GenerateDenoisePreviewCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void GenerateDenoisePreview_執行中_不影響ExecuteDenoiseCommand的CanExecute()
    {
        var denoiseService = Substitute.For<IDenoisePreviewService>();
        var tcs = new TaskCompletionSource<DenoisePreviewResult>();
        denoiseService
            .GeneratePreviewAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<DenoiseMode>>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var sut = CreateSut(denoisePreviewService: denoiseService);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.CurrentPreview = DenoisePreview();

        // 啟動降噪局部預覽（不等待完成）
        sut.DenoiseTool.GenerateDenoisePreviewCommand.Execute(null);
        sut.DenoiseTool.IsGeneratingDenoisePreview.Should().BeTrue();

        // 執行降噪的 CanExecute 不受影響
        sut.DenoiseTool.ExecuteCommand.CanExecute(null).Should().BeTrue();

        tcs.SetResult(new DenoisePreviewResult(new Dictionary<DenoiseMode, byte[]>()));
    }

    [Fact]
    public void DenoiseSelectedMode_變更後_應清除既有降噪預覽()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DenoisePreview();

        sut.DenoiseTool.SelectedMode = DenoiseMode.Strong;

        sut.CurrentPreview.Should().BeNull();
        sut.DenoiseTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }
}
