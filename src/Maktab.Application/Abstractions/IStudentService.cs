using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface IStudentService
{
    Task<IReadOnlyList<Student>> GetAllStudentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Student>> GetStudentsByClassAsync(int classId, CancellationToken cancellationToken = default);
    Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken cancellationToken = default);
    Task<int> RegisterStudentAsync(string firstName, string lastName, string fatherName, int classId, string rollNumber, CancellationToken cancellationToken = default);
    Task UpdateStudentAsync(int studentId, string firstName, string lastName, string fatherName, int classId, string rollNumber, CancellationToken cancellationToken = default);
    Task RemoveStudentAsync(int studentId, CancellationToken cancellationToken = default);
    Task<int> GetNextRollNumberAsync(int classId, CancellationToken cancellationToken = default);
}
