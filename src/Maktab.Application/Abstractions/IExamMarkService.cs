namespace Maktab.Application.Abstractions;

public interface IExamMarkService
{
    Task<IReadOnlyList<StudentExamMarkDto>> GetClassSubjectMarksAsync(int classId, int subjectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentExamMarkDto>> GetStudentMarksAsync(int studentId, CancellationToken cancellationToken = default);
    Task SaveMarksBatchAsync(IEnumerable<SaveExamMarkDto> marks, CancellationToken cancellationToken = default);
}
