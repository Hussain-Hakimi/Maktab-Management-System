using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Rules;

namespace Maktab.Application.Services;

public sealed class ExamMarkService(
    IExamMarkRepository markRepository,
    IStudentRepository studentRepository,
    IClassSubjectRepository classSubjectRepository) : IExamMarkService
{
    public async Task<IReadOnlyList<StudentExamMarkDto>> GetClassSubjectMarksAsync(
        int classId,
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId), "Class ID must be greater than zero.");
        if (subjectId <= 0) throw new ArgumentOutOfRangeException(nameof(subjectId), "Subject ID must be greater than zero.");

        var students = await studentRepository.GetStudentsByClassAsync(classId, cancellationToken);
        var subjects = await classSubjectRepository.GetSubjectsByClassAsync(classId, cancellationToken);
        var subject = subjects.FirstOrDefault(s => s.SubjectId == subjectId);
        var subjectName = subject?.SubjectName ?? $"مضمون {subjectId}";

        var existingMarks = await markRepository.GetMarksByClassAndSubjectAsync(classId, subjectId, cancellationToken);
        var markMap = existingMarks.ToDictionary(m => m.StudentId);

        var result = new List<StudentExamMarkDto>();
        foreach (var student in students.OrderBy(s => s.RollNumber))
        {
            markMap.TryGetValue(student.StudentId, out var mark);
            var midterm = mark?.MidtermScore ?? 0m;
            var final = mark?.FinalScore ?? 0m;

            var total = GradingPolicy.CalculateTotal(midterm, final);
            var percentage = GradingPolicy.CalculatePercentage(total);
            var grade = GradingPolicy.ResolveLetterGrade(percentage);
            var isPass = GradingPolicy.IsPass(total);

            result.Add(new StudentExamMarkDto
            {
                StudentId = student.StudentId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                FatherName = student.FatherName,
                RollNumber = student.RollNumber,
                SubjectId = subjectId,
                SubjectName = subjectName,
                MidtermScore = midterm,
                FinalScore = final,
                TotalScore = total,
                Percentage = percentage,
                Grade = grade,
                IsPass = isPass
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<StudentExamMarkDto>> GetStudentMarksAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        if (studentId <= 0) throw new ArgumentOutOfRangeException(nameof(studentId));

        var student = await studentRepository.GetStudentByIdAsync(studentId, cancellationToken);
        if (student is null) return [];

        var subjects = await classSubjectRepository.GetSubjectsByClassAsync(student.ClassId, cancellationToken);
        var existingMarks = await markRepository.GetMarksByStudentAsync(studentId, cancellationToken);
        var markMap = existingMarks.ToDictionary(m => m.SubjectId);

        var result = new List<StudentExamMarkDto>();
        foreach (var subject in subjects.OrderBy(s => s.SubjectName))
        {
            markMap.TryGetValue(subject.SubjectId, out var mark);
            var midterm = mark?.MidtermScore ?? 0m;
            var final = mark?.FinalScore ?? 0m;

            var total = GradingPolicy.CalculateTotal(midterm, final);
            var percentage = GradingPolicy.CalculatePercentage(total);
            var grade = GradingPolicy.ResolveLetterGrade(percentage);
            var isPass = GradingPolicy.IsPass(total);

            result.Add(new StudentExamMarkDto
            {
                StudentId = student.StudentId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                FatherName = student.FatherName,
                RollNumber = student.RollNumber,
                SubjectId = subject.SubjectId,
                SubjectName = subject.SubjectName,
                MidtermScore = midterm,
                FinalScore = final,
                TotalScore = total,
                Percentage = percentage,
                Grade = grade,
                IsPass = isPass
            });
        }

        return result;
    }

    public async Task SaveMarksBatchAsync(
        IEnumerable<SaveExamMarkDto> marks,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(marks);

        var domainMarks = new List<ExamMark>();
        foreach (var m in marks)
        {
            if (m.StudentId <= 0) throw new ArgumentOutOfRangeException(nameof(m.StudentId));
            if (m.SubjectId <= 0) throw new ArgumentOutOfRangeException(nameof(m.SubjectId));

            if (m.MidtermScore < 0m || m.MidtermScore > GradingPolicy.MidtermMax)
            {
                throw new ArgumentOutOfRangeException(nameof(m.MidtermScore), $"نمره صنفی/چهارونیم‌ماهه باید بین ۰ و {GradingPolicy.MidtermMax} باشد.");
            }

            if (m.FinalScore < 0m || m.FinalScore > GradingPolicy.FinalMax)
            {
                throw new ArgumentOutOfRangeException(nameof(m.FinalScore), $"نمره سالانه باید بین ۰ و {GradingPolicy.FinalMax} باشد.");
            }

            domainMarks.Add(new ExamMark
            {
                StudentId = m.StudentId,
                SubjectId = m.SubjectId,
                MidtermScore = m.MidtermScore,
                FinalScore = m.FinalScore
            });
        }

        await markRepository.SaveOrUpdateMarksBatchAsync(domainMarks, cancellationToken);
    }
}
