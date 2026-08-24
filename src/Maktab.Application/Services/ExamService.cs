using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Application.Services;

public sealed class ExamService(
    IExamRepository examRepository,
    ITeacherAssignmentService teacherAssignmentService) : IExamService
{
    public async Task<int> CreateExamAsync(
        SaveExamDto exam,
        CancellationToken cancellationToken = default)
    {
        if (exam.SubjectId <= 0) throw new ArgumentOutOfRangeException(nameof(exam.SubjectId));
        if (exam.ClassId <= 0) throw new ArgumentOutOfRangeException(nameof(exam.ClassId));
        if (exam.AcademicYearId <= 0) throw new ArgumentOutOfRangeException(nameof(exam.AcademicYearId));
        if (exam.CreatedByTeacherUserId <= 0) throw new ArgumentOutOfRangeException(nameof(exam.CreatedByTeacherUserId));

        // Verify teacher is assigned to this subject/class
        var myAssignments = await teacherAssignmentService.GetMyTeacherSubjectsAsync(
            exam.CreatedByTeacherUserId,
            cancellationToken);

        bool isAssigned = myAssignments.Any(a =>
            a.ClassId == exam.ClassId && a.SubjectId == exam.SubjectId);

        if (!isAssigned)
        {
            throw new InvalidOperationException("شما به این صنف و مضمون تخصیص داده نشده‌اید.");
        }

        var entity = new Exam
        {
            SubjectId = exam.SubjectId,
            ClassId = exam.ClassId,
            AcademicYearId = exam.AcademicYearId,
            ExamType = exam.ExamType,
            ExamDate = exam.ExamDate.Date,
            CreatedByTeacherUserId = exam.CreatedByTeacherUserId
        };

        return await examRepository.CreateAsync(entity, cancellationToken);
    }

    public Task<IReadOnlyList<ExamDto>> GetMyExamsAsync(
        int teacherUserId,
        CancellationToken cancellationToken = default)
    {
        if (teacherUserId <= 0) throw new ArgumentOutOfRangeException(nameof(teacherUserId));
        return examRepository.GetByTeacherAsync(teacherUserId, cancellationToken);
    }

    public Task<IReadOnlyList<ExamDto>> GetExamsByClassSubjectAsync(
        int classId,
        int subjectId,
        int academicYearId,
        CancellationToken cancellationToken = default)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));
        if (subjectId <= 0) throw new ArgumentOutOfRangeException(nameof(subjectId));
        if (academicYearId <= 0) throw new ArgumentOutOfRangeException(nameof(academicYearId));

        return examRepository.GetByClassSubjectAsync(classId, subjectId, academicYearId, cancellationToken);
    }

    public async Task DeleteExamAsync(
        int examId,
        CancellationToken cancellationToken = default)
    {
        if (examId <= 0) throw new ArgumentOutOfRangeException(nameof(examId));
        await examRepository.DeleteAsync(examId, cancellationToken);
    }
}
