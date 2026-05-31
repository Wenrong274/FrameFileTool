using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.ViewModels.Previews;

namespace FrameFileTool.Tests.ViewModels;

public sealed class PreviewViewModelTests
{
    [Fact]
    public void OperationPreviewItem_Action_應由ActionKind產生中文顯示文字()
    {
        var item = new OperationPreviewItem
        {
            ActionKind = OperationActionKind.Delete,
        };

        item.Action.Should().Be("刪除");
    }

    [Fact]
    public void OperationPreviewItem_錯誤項目_IsIncluded預設為false()
    {
        var item = new OperationPreviewItem
        {
            ActionKind = OperationActionKind.Error,
            HasError = true,
        };

        item.IsIncluded.Should().BeFalse();
    }

    [Fact]
    public void FrameDeletePreview_取消勾選刪除項目_摘要與可執行狀態應同步更新()
    {
        var deleteItem = PreviewItem(OperationActionKind.Delete);
        var keepItem = PreviewItem(OperationActionKind.Keep);
        var sut = new FrameDeletePreviewViewModel([deleteItem, keepItem]);

        deleteItem.IsIncluded = false;
        keepItem.IsIncluded = false;

        sut.Summary.Should().Be("共 0 個項目，預計刪除 0 個");
        sut.HasExecutableItems.Should().BeFalse();
        sut.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void FrameDeletePreview_複製保留幀_摘要與可執行狀態應以複製項目計算()
    {
        var copyItem = PreviewItem(OperationActionKind.Copy);
        var skippedItem = PreviewItem(OperationActionKind.Keep);
        var sut = new FrameDeletePreviewViewModel([copyItem, skippedItem]);

        sut.Summary.Should().Be("共 2 個項目，預計複製 1 個");
        sut.HasExecutableItems.Should().BeTrue();
    }

    [Fact]
    public void RenamePreview_取消勾選改名項目_摘要與可執行狀態應同步更新()
    {
        var renameItem = PreviewItem(OperationActionKind.Rename);
        var keepItem = PreviewItem(OperationActionKind.Keep);
        var sut = new RenamePreviewViewModel([renameItem, keepItem]);

        renameItem.IsIncluded = false;
        keepItem.IsIncluded = false;

        sut.Summary.Should().Be("共 0 個項目，預計改名 0 個");
        sut.HasExecutableItems.Should().BeFalse();
        sut.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void RenamePreview_複製改名項目_摘要與可執行狀態應以複製項目計算()
    {
        var copyItem = PreviewItem(OperationActionKind.Copy, targetName: @"D:\out\F_1.png");
        var sut = new RenamePreviewViewModel([copyItem]);

        sut.Summary.Should().Be("共 1 個項目，預計複製改名 1 個");
        sut.HasExecutableItems.Should().BeTrue();
        sut.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ResizePreview_取消勾選縮放項目_摘要與可執行狀態應同步更新()
    {
        var resizeItem = ResizePreviewItem(OperationActionKind.Resize);
        var keepItem = ResizePreviewItem(OperationActionKind.Keep);
        var sut = new ResizePreviewViewModel([resizeItem, keepItem]);

        resizeItem.IsIncluded = false;
        keepItem.IsIncluded = false;

        sut.Summary.Should().Be("共 0 個項目，預計縮放 0 個");
        sut.HasExecutableItems.Should().BeFalse();
        sut.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void RenamePreview_包含錯誤列_即使錯誤列未勾選仍應標記有錯誤()
    {
        var renameItem = PreviewItem(OperationActionKind.Rename);
        var errorItem = PreviewItem(OperationActionKind.Error, hasError: true);
        var sut = new RenamePreviewViewModel([renameItem, errorItem]);

        sut.Summary.Should().Be("共 2 個項目，預計改名 1 個，1 個錯誤（執行已停用）");
        sut.HasExecutableItems.Should().BeTrue();
        sut.HasErrors.Should().BeTrue();
    }

    [Fact]
    public void RenamePreview_已勾選改名目標撞到未勾選來源檔_應標記有錯誤()
    {
        var renameItem = PreviewItem(
            OperationActionKind.Rename,
            fullPath: @"C:\imgs\a.png",
            targetName: "b.png");
        var excludedItem = PreviewItem(
            OperationActionKind.Rename,
            fullPath: @"C:\imgs\b.png",
            targetName: "c.png");
        excludedItem.IsIncluded = false;

        var sut = new RenamePreviewViewModel([renameItem, excludedItem]);

        sut.Summary.Should().Be("共 1 個項目，預計改名 0 個，1 個錯誤（執行已停用）");
        sut.HasExecutableItems.Should().BeFalse();
        sut.HasErrors.Should().BeTrue();
        renameItem.HasDisplayError.Should().BeTrue();
        excludedItem.HasDisplayError.Should().BeFalse();
    }

    [Fact]
    public void PreviewViewModel_IsIncluded變更_應通知Summary與HasExecutableItems()
    {
        var item = PreviewItem(OperationActionKind.Rename);
        var sut = new RenamePreviewViewModel([item]);
        var changedProperties = new List<string?>();
        sut.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        item.IsIncluded = false;

        changedProperties.Should().Contain([nameof(sut.Summary), nameof(sut.HasExecutableItems)]);
    }

    private static OperationPreviewItem PreviewItem(
        OperationActionKind actionKind,
        bool hasError = false,
        string fullPath = @"C:\imgs\a.png",
        string targetName = "") =>
        new()
        {
            ActionKind = actionKind,
            HasError = hasError,
            FullPath = fullPath,
            TargetName = targetName,
        };

    private static ResizePreviewItem ResizePreviewItem(OperationActionKind actionKind, bool hasError = false) =>
        new()
        {
            ActionKind = actionKind,
            HasError = hasError,
        };
}
