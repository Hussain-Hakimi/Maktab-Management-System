using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface IExamMarkRepository
{
    Task<IReadOnlyList<ExamMark>> GetMarksByClassAndSubjectAsync(int classId, int subjectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExamMark>> GetMarksByStudentAsync(int studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExamMark>> GetMarksByClassAsync(int classId, CancellationToken cancellationToken = default);
    Task SaveOrUpdateMarkAsync(ExamMark mark, CancellationToken cancellationToken = default);
    Task SaveOrUpdateMarksBatchAsync(IEnumerable<ExamMark> marks, CancellationToken cancellationToken = default);
}
