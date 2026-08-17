using Maktab.Domain.Enums;

namespace Maktab.Domain.Rules;

public static class PromotionPolicy
{
    public const int MaxAllowedFailedSubjects = 3;
    public const int MaxAllowedAbsenceDays = 30;
    public const decimal PassingAverage = 65m;

    public static PromotionOutcome GetPromotionOutcome(decimal average, int failedSubjects, int absenceDays)
    {
        if (absenceDays > MaxAllowedAbsenceDays)
        {
            return PromotionOutcome.Repeat;
        }

        if (failedSubjects > MaxAllowedFailedSubjects || average < PassingAverage)
        {
            return PromotionOutcome.Repeat;
        }

        if (failedSubjects > 0)
        {
            return PromotionOutcome.Conditional;
        }

        return PromotionOutcome.Promoted;
    }
}