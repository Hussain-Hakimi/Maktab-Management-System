using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface IStudentService
{
    Task<IReadOnlyList<Student>> GetAllStudentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Student>> GetStudentsByClassAsync(int classId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns students enrolled in a class for a specific academic year.
    /// Implementations that do not provide historical enrollment data fall back to the current class list.
    /// </summary>
    Task<IReadOnlyList<Student>> GetStudentsByClassAndAcademicYearAsync(
        int classId,
        int academicYearId,
        CancellationToken cancellationToken = default)
        => GetStudentsByClassAsync(classId, cancellationToken);

    /// <summary>
    /// Returns a student's academic enrollment history.
    /// Implementations without historical enrollment support explicitly report that the operation is unavailable.
    /// </summary>
    Task<IReadOnlyList<StudentAcademicEnrollment>> GetStudentAcademicHistoryAsync(
        int studentId,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Academic enrollment history is not supported by this student service implementation.");

    Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken cancellationToken = default);
    Task<int> RegisterStudentAsync(string firstName, string lastName, string fatherName, int classId, string rollNumber, CancellationToken cancellationToken = default);
    Task UpdateStudentAsync(int studentId, string firstName, string lastName, string fatherName, int classId, string rollNumber, CancellationToken cancellationToken = default);
    Task RemoveStudentAsync(int studentId, CancellationToken cancellationToken = default);
    Task<int> GetNextRollNumberAsync(int classId, CancellationToken cancellationToken = default);
}
