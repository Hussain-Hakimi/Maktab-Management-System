namespace Maktab.Domain.Rules;

public static class PromotionPolicy
{
    public const int MaxAllowedFailedSubjects = 3;
    public const int MaxAllowedAbsenceDays = 30;

    public static bool IsPromoted(int failedSubjects, int absenceDays)
    {
        if (failedSubjects < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(failedSubjects));
        }

        if (absenceDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(absenceDays));
        }

        return failedSubjects <= MaxAllowedFailedSubjects
            && absenceDays <= MaxAllowedAbsenceDays;
    }
}