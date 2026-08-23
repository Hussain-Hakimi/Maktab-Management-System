namespace Maktab.Application.Abstractions;

public sealed class PromotionResultDto
{
    public int TotalStudents { get; set; }
    public int PromotedCount { get; set; }
    public int ConditionalCount { get; set; }
    public int RepeatCount { get; set; }
    public List<string> Errors { get; set; } = [];
}
