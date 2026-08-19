using System.Globalization;

namespace Maktab.Application.Services;

public static class ShamsiDateHelper
{
    public static (DateTime Start, DateTime End) GetAcademicYearRange(string academicYear)
    {
        var parts = academicYear.Split('-', StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            throw new ArgumentException("فرمت سال تعلیمی نامعتبر است.");

        int year1 = ParsePersianNumber(parts[0]);
        int year2 = ParsePersianNumber(parts[1]);

        var pc = new PersianCalendar();

        var start = pc.ToDateTime(year1, 1, 1, 0, 0, 0, 0);
        int lastDay = pc.IsLeapYear(year2) ? 30 : 29;
        var end = pc.ToDateTime(year2, 12, lastDay, 23, 59, 59, 0);

        return (start, end);
    }

    public static int ParsePersianNumber(string persianNumber)
    {
        var cleaned = persianNumber.Trim();
        var latinDigits = new string(cleaned.Select(c =>
        {
            if (c >= '۰' && c <= '۹')
                return (char)('0' + (c - '۰'));
            return c;
        }).ToArray());

        return int.Parse(latinDigits);
    }
}
