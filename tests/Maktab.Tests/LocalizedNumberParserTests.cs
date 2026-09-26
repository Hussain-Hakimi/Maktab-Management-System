using Maktab.Application.Abstractions;

namespace Maktab.Tests;

public class LocalizedNumberParserTests
{
    [Theory]
    [InlineData("12", 12)]
    [InlineData("۱۲", 12)]
    [InlineData("١٢", 12)]
    [InlineData("۰۷", 7)]
    [InlineData("٠٧", 7)]
    public void TryParseInt_NormalizesSupportedDigits(string input, int expected)
    {
        Assert.True(LocalizedNumberParser.TryParseInt(input, out var actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("۱۲x")]
    [InlineData("-۱")]
    [InlineData("999999999999999999999")]
    public void TryParseInt_RejectsInvalidValues(string input)
    {
        Assert.False(LocalizedNumberParser.TryParseInt(input, out _));
    }

    [Fact]
    public void NormalizeDigits_LeavesEnglishAndPersianTextUntouched()
    {
        Assert.Equal("Class 12 صنف 12", LocalizedNumberParser.NormalizeDigits("Class 12 صنف ۱۲"));
    }
}