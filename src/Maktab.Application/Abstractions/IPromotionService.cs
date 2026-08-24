namespace Maktab.Application.Abstractions;

public interface IPromotionService
{
    Task<PromotionResultDto> RunPromotionForYearAsync(int academicYearId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PromotionHistoryDto>> GetPromotionHistoryAsync(
        int? academicYearId = null,
        int? studentId = null,
        CancellationToken cancellationToken = default);
}
