using Maktab.Domain.Enums;
using Maktab.Domain.Rules;

namespace Maktab.Tests;

public class UnitTest1
{
    [Fact]
    public void CalculateTotal_ReturnsExpectedValue()
    {
        var total = GradingPolicy.CalculateTotal(35m, 52m);

        Assert.Equal(87m, total);
    }

    [Theory]
    [InlineData(95, LetterGrade.A)]
    [InlineData(86, LetterGrade.B)]
    [InlineData(78, LetterGrade.C)]
    [InlineData(68, LetterGrade.D)]
    [InlineData(51, LetterGrade.F)]
    [InlineData(39, LetterGrade.F)]
    public void ResolveLetterGrade_ReturnsExpectedGrade(decimal average, LetterGrade expected)
    {
        var grade = GradingPolicy.ResolveLetterGrade(average);

        Assert.Equal(expected, grade);
    }

    [Fact]
    public void IsPromoted_WhenRulesSatisfied_ReturnsPromoted()
    {
        var outcome = PromotionPolicy.GetPromotionOutcome(average: 70m, failedSubjects: 2, absenceDays: 10);

        Assert.Equal(PromotionOutcome.Conditional, outcome);
    }

    [Fact]
    public void IsPromoted_WhenAbsenceOverLimit_ReturnsRepeat()
    {
        var outcome = PromotionPolicy.GetPromotionOutcome(average: 80m, failedSubjects: 0, absenceDays: 31);

        Assert.Equal(PromotionOutcome.Repeat, outcome);
    }
}