using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface IExamRepository
{
    Task<int> CreateAsync(Exam exam, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExamDto>> GetByTeacherAsync(int teacherUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExamDto>> GetByClassSubjectAsync(
        int classId,
        int subjectId,
        int academicYearId,
        CancellationToken cancellationToken = default);
    Task DeleteAsync(int examId, CancellationToken cancellationToken = default);
}
