using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface IClassSubjectRepository
{
    Task<IReadOnlyList<SchoolClass>> GetClassesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Subject>> GetSubjectsByClassAsync(int classId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Subject>> GetAllSubjectsAsync(CancellationToken cancellationToken = default);

    Task<int> CreateClassAsync(SchoolClass schoolClass, CancellationToken cancellationToken = default);
    Task UpdateClassAsync(SchoolClass schoolClass, CancellationToken cancellationToken = default);
    Task DeleteClassAsync(int classId, CancellationToken cancellationToken = default);

    Task<int> CreateSubjectAsync(Subject subject, CancellationToken cancellationToken = default);
    Task UpdateSubjectAsync(Subject subject, CancellationToken cancellationToken = default);
    Task DeleteSubjectAsync(int subjectId, CancellationToken cancellationToken = default);
}
