namespace Maktab.Domain.Entities;

public sealed class StudentPromotionHistory
{
    public int PromotionId { get; set; }
    public int StudentId { get; set; }
    public int FromClassId { get; set; }
    public int? ToClassId { get; set; }
    public int AcademicYearId { get; set; }
    public string Result { get; set; } = string.Empty;
    public DateTime PromotionDate { get; set; }
}
