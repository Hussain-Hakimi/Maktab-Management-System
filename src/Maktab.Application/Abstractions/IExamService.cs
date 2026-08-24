namespace Maktab.Application.Abstractions;

public interface IExamService
{
    Task<int> CreateExamAsync(SaveExamDto exam, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExamDto>> GetMyExamsAsync(int teacherUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExamDto>> GetExamsByClassSubjectAsync(
        int classId,
        int subjectId,
        int academicYearId,
        CancellationToken cancellationToken = default);
    Task DeleteExamAsync(int examId, CancellationToken cancellationToken = default);
}
