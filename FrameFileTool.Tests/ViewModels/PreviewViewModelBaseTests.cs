using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.ViewModels.Previews;

namespace FrameFileTool.Tests.ViewModels;

/// <summary>
/// <see cref="PreviewViewModelBase{TItem}"/> 共用行為的測試：
/// 透過具體子類驗證 Items 訂閱與 PropertyChanged 轉發規則。
/// 各工具特定的摘要與可執行規則由 <see cref="PreviewViewModelTests"/> 覆蓋。
/// </summary>
public sealed class PreviewViewModelBaseTests
{
    [Fact]
    public void 空清單_摘要與狀態應為安全預設值()
    {
        var sut = new FrameDeletePreviewViewModel([]);

        sut.Summary.Should().Be("共 0 個項目，預計刪除 0 個");
        sut.HasErrors.Should().BeFalse();
        sut.HasExecutableItems.Should().BeFalse();
    }

    [Fact]
    public void IsIncluded變更_應轉發三個摘要屬性的通知()
    {
        var item = PreviewItem(OperationActionKind.Delete);
        var sut = new FrameDeletePreviewViewModel([item]);
        var changedProperties = new List<string?>();
        sut.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        item.IsIncluded = false;

        changedProperties.Should().Contain([
            nameof(sut.Summary),
            nameof(sut.HasErrors),
            nameof(sut.HasExecutableItems),
        ]);
    }

    [Fact]
    public void 非IsIncluded的項目屬性變更_不應轉發通知()
    {
        var item = PreviewItem(OperationActionKind.Delete);
        var sut = new FrameDeletePreviewViewModel([item]);
        var changedProperties = new List<string?>();
        sut.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        // HasSelectionConflict 會發出自身與 HasDisplayError 的通知，但不應觸發 ViewModel 轉發。
        item.HasSelectionConflict = true;

        changedProperties.Should().BeEmpty();
    }

    [Fact]
    public void Resize子類_Items應為強型別清單()
    {
        var item = new ResizePreviewItem { ActionKind = OperationActionKind.Resize };
        var sut = new ResizePreviewViewModel([item]);

        sut.Items.Should().AllBeOfType<ResizePreviewItem>();
        sut.HasExecutableItems.Should().BeTrue();
    }

    [Fact]
    public void Rename子類_建構時即計算SelectionConflict()
    {
        // 已勾選改名目標（b.png）撞到未勾選的來源檔 b.png，建構後立即標記衝突。
        var renameItem = PreviewItem(OperationActionKind.Rename, fullPath: @"C:\imgs\a.png", targetName: "b.png");
        var excludedItem = PreviewItem(OperationActionKind.Rename, fullPath: @"C:\imgs\b.png", targetName: "c.png");
        excludedItem.IsIncluded = false;

        _ = new RenamePreviewViewModel([renameItem, excludedItem]);

        renameItem.HasSelectionConflict.Should().BeTrue();
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
}
