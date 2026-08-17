using System.Globalization;

namespace Maktab.Application.Services;

/// <summary>
/// Provides the current Afghan academic year based on the Solar Hijri (Shamsi) calendar.
/// The Afghan academic year starts in Hamal (the first month of the Shamsi year) and is
/// written as a pair of consecutive years, e.g. "۱۴۰۳ - ۱۴۰۴".
/// </summary>
public static class AcademicYearProvider
{
    /// <summary>
    /// Returns the current academic year as a pair of Shamsi years in Persian digits,
    /// e.g. "۱۴۰۳ - ۱۴۰۴". Pass <paramref name="now"/> to make the result deterministic in tests.
    /// </summary>
    public static string GetCurrentAcademicYear(DateTime? now = null)
    {
        var date = now ?? DateTime.Now;
        var persianCalendar = new PersianCalendar();
        var shamsiYear = persianCalendar.GetYear(date);
        return $"{ToPersianDigits(shamsiYear)} - {ToPersianDigits(shamsiYear + 1)}";
    }

    private static string ToPersianDigits(int number)
    {
        var latin = number.ToString();
        var chars = new char[latin.Length];
        for (var i = 0; i < latin.Length; i++)
        {
            chars[i] = (char)('۰' + (latin[i] - '0'));
        }

        return new string(chars);
    }
}
