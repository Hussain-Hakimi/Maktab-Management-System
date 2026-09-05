using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface IStudentAcademicEnrollmentRepository
{
    Task<StudentAcademicEnrollment?> GetByStudentAndAcademicYearAsync(
        int studentId,
        int academicYearId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentAcademicEnrollment>> GetByStudentAsync(
        int studentId,
        CancellationToken cancellationToken = default);

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
