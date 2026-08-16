using Maktab.Domain.Enums;

namespace Maktab.Domain.Rules;

public static class GradingPolicy
{
    public const decimal MidtermMax = 40m;
    public const decimal FinalMax = 60m;
    public const decimal TotalMax = 100m;
    public const decimal PassingMark = 40m;

    public static decimal CalculateTotal(decimal midtermScore, decimal finalScore)
    {
        ValidateScores(midtermScore, finalScore);
        return midtermScore + finalScore;
    }

    public static decimal CalculatePercentage(decimal total)
    {
        if (total < 0m || total > TotalMax)
        {
            throw new ArgumentOutOfRangeException(nameof(total), "Total must be between 0 and 100.");
        }

        return Math.Round((total / TotalMax) * 100m, 2, MidpointRounding.AwayFromZero);
    }

    public static bool IsPass(decimal total)
    {
        if (total < 0m || total > TotalMax)
        {
            throw new ArgumentOutOfRangeException(nameof(total), "Total must be between 0 and 100.");
        }

        return total >= PassingMark;
    }

    public static LetterGrade ResolveLetterGrade(decimal percentage)
    {
        if (percentage < 0m || percentage > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be between 0 and 100.");
        }

        if (percentage >= 90m) return LetterGrade.A;
        if (percentage >= 80m) return LetterGrade.B;
        if (percentage >= 70m) return LetterGrade.C;
        if (percentage >= 60m) return LetterGrade.D;
        if (percentage >= 50m) return LetterGrade.E;
        return LetterGrade.F;
    }

    private static void ValidateScores(decimal midtermScore, decimal finalScore)
    {
        if (midtermScore < 0m || midtermScore > MidtermMax)
        {
            throw new ArgumentOutOfRangeException(nameof(midtermScore), "Midterm must be between 0 and 40.");
        }

        if (finalScore < 0m || finalScore > FinalMax)
        {
            throw new ArgumentOutOfRangeException(nameof(finalScore), "Final must be between 0 and 60.");
        }
    }
}