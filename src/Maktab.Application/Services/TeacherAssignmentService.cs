using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Application.Services;

public sealed class TeacherAssignmentService(
    ITeacherAssignmentRepository repository) : ITeacherAssignmentService
{
    public async Task<int> AssignTeacherToSubjectAsync(
        int teacherUserId,
        int classId,
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        if (teacherUserId <= 0) throw new ArgumentOutOfRangeException(nameof(teacherUserId));
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));
        if (subjectId <= 0) throw new ArgumentOutOfRangeException(nameof(subjectId));

        var assignment = new TeacherSubject
        {
            TeacherUserId = teacherUserId,
            ClassId = classId,
            SubjectId = subjectId
        };

        return await repository.AddTeacherSubjectAsync(assignment, cancellationToken);
    }

    public Task RemoveTeacherSubjectAssignmentAsync(
        int teacherSubjectId,
        CancellationToken cancellationToken = default)
    {
        if (teacherSubjectId <= 0) throw new ArgumentOutOfRangeException(nameof(teacherSubjectId));
        return repository.RemoveTeacherSubjectAsync(teacherSubjectId, cancellationToken);
    }

    public Task<IReadOnlyList<TeacherSubjectAssignmentDto>> GetTeacherSubjectsAsync(
        int? teacherUserId = null,
        CancellationToken cancellationToken = default)
        => repository.GetTeacherSubjectsAsync(teacherUserId, cancellationToken);

    public Task<IReadOnlyList<TeacherSubjectAssignmentDto>> GetMyTeacherSubjectsAsync(
        int teacherUserId,
        CancellationToken cancellationToken = default)
    {
        if (teacherUserId <= 0) throw new ArgumentOutOfRangeException(nameof(teacherUserId));
        return repository.GetTeacherSubjectsByTeacherAsync(teacherUserId, cancellationToken);
    }

    public async Task<int> AssignClassGuardianAsync(
        int teacherUserId,
        int classId,
        CancellationToken cancellationToken = default)
    {
        if (teacherUserId <= 0) throw new ArgumentOutOfRangeException(nameof(teacherUserId));
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));

        var guardian = new ClassGuardian
        {
            TeacherUserId = teacherUserId,
            ClassId = classId
        };

        return await repository.AddClassGuardianAsync(guardian, cancellationToken);
    }

    public Task RemoveClassGuardianAsync(
        int classGuardianId,
        CancellationToken cancellationToken = default)
    {
        if (classGuardianId <= 0) throw new ArgumentOutOfRangeException(nameof(classGuardianId));
        return repository.RemoveClassGuardianAsync(classGuardianId, cancellationToken);
    }

    public Task<IReadOnlyList<ClassGuardianDto>> GetClassGuardiansAsync(
        int? teacherUserId = null,
        CancellationToken cancellationToken = default)
        => repository.GetClassGuardiansAsync(teacherUserId, cancellationToken);

    public async Task<bool> IsClassGuardianAsync(
        int teacherUserId,
        int classId,
        CancellationToken cancellationToken = default)
    {
        if (teacherUserId <= 0) throw new ArgumentOutOfRangeException(nameof(teacherUserId));
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));

        var guardian = await repository.GetClassGuardianByTeacherAndClassAsync(teacherUserId, classId, cancellationToken);
        return guardian is not null;
    }
}
