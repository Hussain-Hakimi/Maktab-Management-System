using Maktab.Domain.Enums;

namespace Maktab.Domain.Rules;

public static class PromotionPolicy
{
    // Default values (can be overridden by settings)
    public static decimal PassingAverage { get; private set; } = 65m;
    public static decimal PassingMark { get; private set; } = 40m;
    public static int MaxAllowedFailedSubjects { get; private set; } = 3;
    public static int MaxAllowedAbsenceDays { get; private set; } = 30;

    public static void SetValues(
        decimal passingAverage,
        decimal passingMark,
        int maxAllowedFailedSubjects,
        int maxAllowedAbsenceDays)
    {
        if (passingAverage < 0m || passingAverage > 100m)
            throw new ArgumentOutOfRangeException(nameof(passingAverage));
        if (passingMark < 0m || passingMark > 100m)
            throw new ArgumentOutOfRangeException(nameof(passingMark));
        if (maxAllowedFailedSubjects < 0)
            throw new ArgumentOutOfRangeException(nameof(maxAllowedFailedSubjects));
        if (maxAllowedAbsenceDays < 0)
            throw new ArgumentOutOfRangeException(nameof(maxAllowedAbsenceDays));

        PassingAverage = passingAverage;
        PassingMark = passingMark;
        MaxAllowedFailedSubjects = maxAllowedFailedSubjects;
        MaxAllowedAbsenceDays = maxAllowedAbsenceDays;
    }

    public static PromotionOutcome GetPromotionOutcome(decimal average, int failedSubjects, int absenceDays)
    {
        if (absenceDays > MaxAllowedAbsenceDays)
            return PromotionOutcome.Repeat;

        if (failedSubjects > MaxAllowedFailedSubjects || average < PassingAverage)
            return PromotionOutcome.Repeat;

        if (failedSubjects > 0)
            return PromotionOutcome.Conditional;

        return PromotionOutcome.Promoted;
    }
}
