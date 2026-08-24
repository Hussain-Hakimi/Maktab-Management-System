using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface IStudentPromotionHistoryRepository
{
    Task<int> AddAsync(StudentPromotionHistory history, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentPromotionHistory>> GetByStudentAsync(int studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PromotionHistoryDto>> GetHistoryAsync(
        int? academicYearId,
        int? studentId,
        CancellationToken cancellationToken = default);
}
