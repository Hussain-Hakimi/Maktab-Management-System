using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface IClassSubjectService
{
    Task<IReadOnlyList<SchoolClass>> GetClassesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Subject>> GetSubjectsByClassAsync(int classId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Subject>> GetAllSubjectsAsync(CancellationToken cancellationToken = default);

    Task<int> CreateClassAsync(string gradeName, int numberOfSubjects, CancellationToken cancellationToken = default);
    Task UpdateClassAsync(int classId, string gradeName, int numberOfSubjects, CancellationToken cancellationToken = default);
    Task DeleteClassAsync(int classId, CancellationToken cancellationToken = default);

    Task<int> CreateSubjectAsync(int classId, string subjectName, CancellationToken cancellationToken = default);
    Task UpdateSubjectAsync(int subjectId, int classId, string subjectName, CancellationToken cancellationToken = default);
    Task DeleteSubjectAsync(int subjectId, CancellationToken cancellationToken = default);
}
