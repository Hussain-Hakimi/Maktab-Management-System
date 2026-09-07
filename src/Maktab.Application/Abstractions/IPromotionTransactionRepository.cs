using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface IPromotionTransactionRepository
{
    Task ApplyAsync(
        Student student,
        StudentPromotionHistory history,
        StudentAcademicEnrollment? targetEnrollment,
        CancellationToken cancellationToken = default);
}
