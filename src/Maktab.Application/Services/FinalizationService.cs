using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Application.Services;

public sealed class FinalizationService(
    IFinalizationRepository repository,
    ITeacherAssignmentService teacherAssignmentService) : IFinalizationService
{
    public async Task FinalizeClassAsync(
        int classId,
        int academicYearId,
        int teacherUserId,
        CancellationToken cancellationToken = default)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));
        if (academicYearId <= 0) throw new ArgumentOutOfRangeException(nameof(academicYearId));
        if (teacherUserId <= 0) throw new ArgumentOutOfRangeException(nameof(teacherUserId));

        // Only guardian or admin can finalize; here we check if guardian
        bool isGuardian = await teacherAssignmentService.IsClassGuardianAsync(teacherUserId, classId, cancellationToken);
        if (!isGuardian)
            throw new InvalidOperationException("فقط نگران صنف می‌تواند نتایج را نهایی کند.");

        var finalization = new ClassFinalization
        {
            ClassId = classId,
            AcademicYearId = academicYearId,
            IsFinalized = true,
            FinalizedByTeacherUserId = teacherUserId,
            FinalizationDate = DateTime.Now
        };

        await repository.UpsertAsync(finalization, cancellationToken);
    }

    public async Task UnfinalizeClassAsync(
        int classId,
        int academicYearId,
        int teacherUserId,
        CancellationToken cancellationToken = default)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));
        if (academicYearId <= 0) throw new ArgumentOutOfRangeException(nameof(academicYearId));
        if (teacherUserId <= 0) throw new ArgumentOutOfRangeException(nameof(teacherUserId));

        // For now, allow guardian to unfinalize (admin can later also be allowed)
        bool isGuardian = await teacherAssignmentService.IsClassGuardianAsync(teacherUserId, classId, cancellationToken);
        if (!isGuardian)
            throw new InvalidOperationException("فقط نگران صنف می‌تواند وضعیت نهایی را تغییر دهد.");

        var finalization = new ClassFinalization
        {
            ClassId = classId,
            AcademicYearId = academicYearId,
            IsFinalized = false,
            FinalizedByTeacherUserId = teacherUserId,
            FinalizationDate = DateTime.Now
        };

        await repository.UpsertAsync(finalization, cancellationToken);
    }

    public async Task<bool> IsClassFinalizedAsync(
        int classId,
        int academicYearId,
        CancellationToken cancellationToken = default)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));
        if (academicYearId <= 0) throw new ArgumentOutOfRangeException(nameof(academicYearId));

        var finalization = await repository.GetByClassYearAsync(classId, academicYearId, cancellationToken);
        return finalization?.IsFinalized ?? false;
    }
}
