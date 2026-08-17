using Maktab.Application.Services;

namespace Maktab.Tests;

public class AcademicYearProviderTests
{
    [Theory]
    [InlineData(2025, 1, 1, "۱۴۰۳ - ۱۴۰۴")]   // middle of Shamsi year 1403
    [InlineData(2026, 3, 20, "۱۴۰۴ - ۱۴۰۵")]  // last day of Shamsi year 1404
    [InlineData(2026, 3, 21, "۱۴۰۵ - ۱۴۰۶")]  // Nawruz — first day of Shamsi year 1405
    [InlineData(2026, 8, 18, "۱۴۰۵ - ۱۴۰۶")]  // middle of Shamsi year 1405
    public void GetCurrentAcademicYear_ReturnsShamsiYearPairInPersianDigits(int year, int month, int day, string expected)
    {
        var result = AcademicYearProvider.GetCurrentAcademicYear(new DateTime(year, month, day));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetCurrentAcademicYear_UsesOnlyPersianDigits()
    {
        var result = AcademicYearProvider.GetCurrentAcademicYear();

        Assert.Matches(@"^[۰-۹]{4} - [۰-۹]{4}$", result);
    }
}
