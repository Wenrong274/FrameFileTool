using FluentAssertions;
using FrameFileTool.Services;

namespace FrameFileTool.Tests.Services;

public sealed class NaturalStringComparerTests
{
    private readonly NaturalStringComparer _sut = new();

    [Theory]
    [InlineData("frame2.png", "frame10.png", -1)]   // 數字段：2 < 10
    [InlineData("frame10.png", "frame2.png", 1)]    // 數字段：10 > 2
    [InlineData("frame3.png", "frame3.png", 0)]     // 完全相同
    [InlineData("a1b2.png", "a1b10.png", -1)]       // 混合數字
    [InlineData("img_001.jpg", "img_002.jpg", -1)]  // 補零格式
    [InlineData("abc.png", "abd.png", -1)]           // 純文字比較
    public void Compare_排序結果應符合自然排序(string x, string y, int expectedSign)
    {
        var result = _sut.Compare(x, y);

        Math.Sign(result).Should().Be(expectedSign);
    }

    [Fact]
    public void Compare_null_x_應排在最前()
    {
        var result = _sut.Compare(null, "a.png");

        result.Should().BeNegative();
    }

    [Fact]
    public void Compare_null_y_應排在最後()
    {
        var result = _sut.Compare("a.png", null);

        result.Should().BePositive();
    }

    [Fact]
    public void Compare_兩個_null_應相等()
    {
        var result = _sut.Compare(null, null);

        result.Should().Be(0);
    }

    [Fact]
    public void Sort_檔名清單應以自然順序排列()
    {
        var names = new[] { "frame10.png", "frame2.png", "frame1.png", "frame20.png" };

        var sorted = names.OrderBy(n => n, _sut).ToList();

        sorted.Should().ContainInOrder("frame1.png", "frame2.png", "frame10.png", "frame20.png");
    }
}
