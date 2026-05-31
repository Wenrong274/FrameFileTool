using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FrameFileTool.Models;

/// <summary>
/// 代表單一檔案的操作預覽項目，供 DataGrid 顯示與 executor 執行使用。
/// 規劃結果欄位建立後不可變更；是否納入執行可由預覽表格勾選切換。
/// </summary>
public class OperationPreviewItem : INotifyPropertyChanged
{
    private bool? _isIncluded;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>在預覽清單中的序號（從 1 開始）。</summary>
    public int Index { get; init; }

    /// <summary>來源檔案的完整路徑。</summary>
    public string FullPath { get; init; } = string.Empty;

    /// <summary>原始檔名（不含目錄）。</summary>
    public string OriginalName { get; init; } = string.Empty;

    /// <summary>操作動作種類，供 executor 與 ViewModel 判斷行為。</summary>
    public OperationActionKind ActionKind { get; init; }

    /// <summary>操作動作的顯示文字，供 UI 繫結。</summary>
    public string Action => OperationAction.ToDisplayText(ActionKind);

    /// <summary>
    /// 目標檔名（改名時）或說明文字（刪除時為「移到回收桶」）。
    /// </summary>
    public string TargetName { get; init; } = string.Empty;

    /// <summary>
    /// executor 使用的完整目標路徑。空字串代表依既有規則由來源路徑與 TargetName 推導。
    /// </summary>
    public string TargetPath { get; init; } = string.Empty;

    /// <summary>操作狀態說明，供使用者確認計畫內容。</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// 是否有錯誤。為 <see langword="true"/> 時，executor 會略過此項目。
    /// </summary>
    public bool HasError
    {
        get;
        init
        {
            field = value;
            if (value)
            {
                _isIncluded = false;
            }
            else
            {
                _isIncluded ??= true;
            }
        }
    }

    /// <summary>
    /// 是否納入後續執行。錯誤項目預設且固定不納入。
    /// </summary>
    public bool IsIncluded
    {
        get => _isIncluded ?? !HasError;
        set
        {
            var nextValue = !HasError && value;
            if (IsIncluded == nextValue)
            {
                return;
            }

            _isIncluded = nextValue;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 勾選狀態造成的暫時性衝突，例如已勾選改名目標撞到未勾選來源檔。
    /// </summary>
    public bool HasSelectionConflict
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasDisplayError));
        }
    }

    /// <summary>是否應在預覽表格以錯誤狀態顯示。</summary>
    public bool HasDisplayError => HasError || HasSelectionConflict;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
