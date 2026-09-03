using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface IStudentRepository
{
    Task<IReadOnlyList<Student>> GetStudentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Student>> GetStudentsByClassAsync(int classId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Student>> GetStudentsByClassAndAcademicYearAsync(int classId, int academicYearId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentAcademicEnrollment>> GetStudentAcademicHistoryAsync(int studentId, CancellationToken cancellationToken = default);
    Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken cancellationToken = default);
    Task<int> CreateStudentAsync(Student student, CancellationToken cancellationToken = default);
    Task UpdateStudentAsync(Student student, CancellationToken cancellationToken = default);
    Task DeleteStudentAsync(int studentId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByRollNumberAsync(int classId, string rollNumber, CancellationToken cancellationToken = default);
}
