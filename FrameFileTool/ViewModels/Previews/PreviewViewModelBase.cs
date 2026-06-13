using System.ComponentModel;
using FrameFileTool.Models;

namespace FrameFileTool.ViewModels.Previews;

/// <summary>
/// 預覽結果 ViewModel 的共用基底：集中 Items 的 PropertyChanged 訂閱，
/// 以及 <see cref="OperationPreviewItem.IsIncluded"/> 變更後對
/// <see cref="Summary"/>、<see cref="HasErrors"/>、<see cref="HasExecutableItems"/> 的通知轉發。
/// 摘要文字與可執行規則屬於各工具的特定行為，由子類實作。
/// 項目清單的生命週期與 ViewModel 相同，因此不需要解除訂閱機制。
/// </summary>
/// <typeparam name="TItem">預覽項目型別，工具專屬欄位以子類（如 ResizePreviewItem）表達。</typeparam>
public abstract class PreviewViewModelBase<TItem> : IPreviewViewModel
    where TItem : OperationPreviewItem
{
    /// <summary>預覽項目清單，繫結到各工具專用的 DataGrid。</summary>
    public IReadOnlyList<TItem> Items { get; }

    /// <inheritdoc/>
    public abstract string Summary { get; }

    /// <inheritdoc/>
    public abstract bool HasErrors { get; }

    /// <inheritdoc/>
    public abstract bool HasExecutableItems { get; }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    protected PreviewViewModelBase(IReadOnlyList<TItem> items)
    {
        Items = items;

        foreach (var item in Items)
        {
            item.PropertyChanged += OnItemPropertyChanged;
        }
    }

    /// <summary>
    /// 任一項目的 IsIncluded 變更後、發出通知前呼叫。
    /// 子類可覆寫以刷新勾選相關的衍生狀態（例如改名的 selection conflict）。
    /// </summary>
    protected virtual void OnIncludedItemsChanged()
    {
    }

    /// <summary>對摘要列依賴的三個屬性發出 PropertyChanged 通知。</summary>
    protected void RaisePreviewStateChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Summary)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasErrors)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasExecutableItems)));
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(OperationPreviewItem.IsIncluded))
        {
            return;
        }

        OnIncludedItemsChanged();
        RaisePreviewStateChanged();
    }
}
