namespace Maktab.Application.Abstractions;

public interface IFinalizationService
{
    Task FinalizeClassAsync(
        int classId,
        int academicYearId,
        int teacherUserId,
        CancellationToken cancellationToken = default);

    Task UnfinalizeClassAsync(
        int classId,
        int academicYearId,
        int teacherUserId,
        CancellationToken cancellationToken = default);

    Task<bool> IsClassFinalizedAsync(
        int classId,
        int academicYearId,
        CancellationToken cancellationToken = default);
}
