using System.Collections;

namespace FrameFileTool.Services;

/// <summary>
/// 自然排序字串比較器，讓 frame2.png 排在 frame10.png 之前。
/// 演算法：逐字元比較，遇到數字段時改用數值大小比較，而非字典序。
/// </summary>
public sealed class NaturalStringComparer : IComparer<string>, IComparer
{
    /// <summary>
    /// 比較兩個字串的自然排序順序。
    /// null 視為最小值（排在最前）。
    /// </summary>
    public int Compare(string? x, string? y)
    {
        // 相同參考（含兩個 null）直接相等
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        var ix = 0;
        var iy = 0;

        while (ix < x.Length && iy < y.Length)
        {
            var cx = x[ix];
            var cy = y[iy];

            if (char.IsDigit(cx) && char.IsDigit(cy))
            {
                // 兩側同時是數字：提取整段數字塊以數值大小比較
                var result = CompareNumberBlocks(x, ref ix, y, ref iy);
                if (result != 0)
                {
                    return result;
                }

                continue;
            }

            // 非數字字元：大小寫不敏感的字典序比較
            var charResult = char.ToUpperInvariant(cx).CompareTo(char.ToUpperInvariant(cy));
            if (charResult != 0)
            {
                return charResult;
            }

            ix++;
            iy++;
        }

        // 前綴相同時，較短的字串排在前面（例如 "frame" < "frame1"）
        return x.Length.CompareTo(y.Length);
    }

    /// <inheritdoc/>
    public int Compare(object? x, object? y) => Compare(x as string, y as string);

    /// <summary>
    /// 從目前索引位置提取兩側的連續數字塊，並以數值大小比較。
    /// 方法結束後 ix、iy 會移動到數字塊之後的位置。
    /// </summary>
    private static int CompareNumberBlocks(string x, ref int ix, string y, ref int iy)
    {
        var startX = ix;
        var startY = iy;

        // 向右掃描，直到非數字字元
        while (ix < x.Length && char.IsDigit(x[ix]))
        {
            ix++;
        }

        while (iy < y.Length && char.IsDigit(y[iy]))
        {
            iy++;
        }

        // 去除前導零，使 "007" 與 "7" 視為相等
        var numberX = x[startX..ix].TrimStart('0');
        var numberY = y[startY..iy].TrimStart('0');

        if (numberX.Length == 0)
        {
            numberX = "0";
        }

        if (numberY.Length == 0)
        {
            numberY = "0";
        }

        // 先比長度：位數多的數值較大（去除前導零後有效）
        var lengthResult = numberX.Length.CompareTo(numberY.Length);
        if (lengthResult != 0)
        {
            return lengthResult;
        }

        // 長度相同時，字典序等於數值序（因為已去除前導零）
        var valueResult = string.CompareOrdinal(numberX, numberY);
        if (valueResult != 0)
        {
            return valueResult;
        }

        // 數值相同但原始位數不同時（如 "007" vs "7"），位數少的排前面
        return (ix - startX).CompareTo(iy - startY);
    }
}
