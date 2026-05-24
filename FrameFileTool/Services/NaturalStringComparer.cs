using System.Collections;

namespace FrameFileTool.Services;

public sealed class NaturalStringComparer : IComparer<string>, IComparer
{
    public int Compare(string? x, string? y)
    {
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
                var result = CompareNumberBlocks(x, ref ix, y, ref iy);
                if (result != 0)
                {
                    return result;
                }

                continue;
            }

            var charResult = char.ToUpperInvariant(cx).CompareTo(char.ToUpperInvariant(cy));
            if (charResult != 0)
            {
                return charResult;
            }

            ix++;
            iy++;
        }

        return x.Length.CompareTo(y.Length);
    }

    public int Compare(object? x, object? y)
    {
        return Compare(x as string, y as string);
    }

    private static int CompareNumberBlocks(string x, ref int ix, string y, ref int iy)
    {
        var startX = ix;
        var startY = iy;

        while (ix < x.Length && char.IsDigit(x[ix]))
        {
            ix++;
        }

        while (iy < y.Length && char.IsDigit(y[iy]))
        {
            iy++;
        }

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

        var lengthResult = numberX.Length.CompareTo(numberY.Length);
        if (lengthResult != 0)
        {
            return lengthResult;
        }

        var valueResult = string.CompareOrdinal(numberX, numberY);
        if (valueResult != 0)
        {
            return valueResult;
        }

        return (ix - startX).CompareTo(iy - startY);
    }
}
