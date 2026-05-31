using System.Globalization;
using System.Windows.Data;
using FluentAssertions;
using FrameFileTool.Converters;

namespace FrameFileTool.Tests.Converters;

public sealed class ScaleFactorTextConverterTests
{
    private readonly ScaleFactorTextConverter _sut = new();

    [Theory]
    [InlineData(0.30000000000000004, "0.3")]
    [InlineData(0.5, "0.5")]
    [InlineData(0.75, "0.75")]
    [InlineData(1.0, "1")]
    public void Convert_倍率值_應限制顯示小數位(double value, string expected)
    {
        var result = _sut.Convert(value, typeof(string), parameter: new object(), CultureInfo.InvariantCulture);

        result.Should().Be(expected);
    }

    [Fact]
    public void ConvertBack_合法倍率文字_應轉回double()
    {
        var result = _sut.ConvertBack("0.75", typeof(double), parameter: new object(), CultureInfo.InvariantCulture);

        result.Should().Be(0.75);
    }

    [Fact]
    public void ConvertBack_非法倍率文字_應保持原值不覆寫()
    {
        var result = _sut.ConvertBack("abc", typeof(double), parameter: new object(), CultureInfo.InvariantCulture);

        result.Should().BeSameAs(Binding.DoNothing);
    }
}
