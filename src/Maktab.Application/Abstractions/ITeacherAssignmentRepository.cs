using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface ITeacherAssignmentRepository
{
    // Teacher Subjects
    Task<int> AddTeacherSubjectAsync(TeacherSubject assignment, CancellationToken cancellationToken = default);
    Task RemoveTeacherSubjectAsync(int teacherSubjectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeacherSubjectAssignmentDto>> GetTeacherSubjectsAsync(
        int? teacherUserId = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeacherSubjectAssignmentDto>> GetTeacherSubjectsByTeacherAsync(
        int teacherUserId,
        CancellationToken cancellationToken = default);

    // Class Guardians
    Task<int> AddClassGuardianAsync(ClassGuardian guardian, CancellationToken cancellationToken = default);
    Task RemoveClassGuardianAsync(int classGuardianId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassGuardianDto>> GetClassGuardiansAsync(
        int? teacherUserId = null,
        CancellationToken cancellationToken = default);
    Task<ClassGuardianDto?> GetClassGuardianByTeacherAndClassAsync(
        int teacherUserId,
        int classId,
        CancellationToken cancellationToken = default);
}
