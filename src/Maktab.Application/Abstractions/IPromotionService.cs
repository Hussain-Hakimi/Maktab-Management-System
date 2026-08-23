namespace Maktab.Application.Abstractions;

public interface IPromotionService
{
    Task<PromotionResultDto> RunPromotionForYearAsync(int academicYearId, CancellationToken cancellationToken = default);
}
