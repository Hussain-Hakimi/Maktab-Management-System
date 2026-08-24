using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface IFinalizationRepository
{
    Task<ClassFinalization?> GetByClassYearAsync(
        int classId,
        int academicYearId,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(ClassFinalization finalization, CancellationToken cancellationToken = default);
}
