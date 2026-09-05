using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface IStudentAcademicEnrollmentRepository
{
    Task<StudentAcademicEnrollment?> GetByStudentAndAcademicYearAsync(
        int studentId,
        int academicYearId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all enrollments for a student. Implementations may return an empty set when
    /// historical enrollment storage is not available to the caller.
    /// </summary>
    Task<IReadOnlyList<StudentAcademicEnrollment>> GetByStudentAsync(
        int studentId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<StudentAcademicEnrollment>>(Array.Empty<StudentAcademicEnrollment>());

    Task<IReadOnlyList<StudentAcademicEnrollment>> GetByAcademicYearAsync(
        int academicYearId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentAcademicEnrollment>> GetByClassAndAcademicYearAsync(
        int classId,
        int academicYearId,
        CancellationToken cancellationToken = default);

    Task<int> CreateOrUpdateAsync(
        StudentAcademicEnrollment enrollment,
        CancellationToken cancellationToken = default);
}
