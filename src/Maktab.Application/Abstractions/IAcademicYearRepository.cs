using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface IAcademicYearRepository
{
    Task<AcademicYear?> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<AcademicYear?> GetByIdAsync(int academicYearId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AcademicYear>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<int> CreateAsync(AcademicYear academicYear, CancellationToken cancellationToken = default);
    Task SetActiveAsync(int academicYearId, CancellationToken cancellationToken = default);
}
