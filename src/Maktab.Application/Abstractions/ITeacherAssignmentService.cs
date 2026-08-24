namespace Maktab.Application.Abstractions;

public interface ITeacherAssignmentService
{
    Task<int> AssignTeacherToSubjectAsync(
        int teacherUserId,
        int classId,
        int subjectId,
        CancellationToken cancellationToken = default);

    Task RemoveTeacherSubjectAssignmentAsync(
        int teacherSubjectId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeacherSubjectAssignmentDto>> GetTeacherSubjectsAsync(
        int? teacherUserId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeacherSubjectAssignmentDto>> GetMyTeacherSubjectsAsync(
        int teacherUserId,
        CancellationToken cancellationToken = default);

    Task<int> AssignClassGuardianAsync(
        int teacherUserId,
        int classId,
        CancellationToken cancellationToken = default);

    Task RemoveClassGuardianAsync(
        int classGuardianId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClassGuardianDto>> GetClassGuardiansAsync(
        int? teacherUserId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsClassGuardianAsync(
        int teacherUserId,
        int classId,
        CancellationToken cancellationToken = default);
}
