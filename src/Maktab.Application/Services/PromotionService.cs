using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;
using Maktab.Domain.Rules;

namespace Maktab.Application.Services;

public sealed class PromotionService(
    IStudentRepository studentRepository,
    IClassSubjectRepository classSubjectRepository,
    IExamMarkRepository examMarkRepository,
    IAttendanceRepository attendanceRepository,
    IStudentPromotionHistoryRepository historyRepository) : IPromotionService
{
    public async Task<PromotionResultDto> RunPromotionForYearAsync(int academicYearId, CancellationToken cancellationToken = default)
    {
        if (academicYearId <= 0)
            throw new ArgumentOutOfRangeException(nameof(academicYearId));

        var result = new PromotionResultDto();
        var classes = await classSubjectRepository.GetClassesAsync(cancellationToken);
        var classIds = classes.OrderBy(c => c.ClassId).Select(c => c.ClassId).ToList();

        foreach (var classId in classIds)
        {
            var students = await studentRepository.GetStudentsByClassAsync(classId, cancellationToken);
            foreach (var student in students)
            {
                try
                {
                    var outcome = await DeterminePromotionAsync(student, classId, academicYearId, cancellationToken);

                    int? toClassId = null;
                    string resultText;
                    switch (outcome)
                    {
                        case PromotionOutcome.Promoted:
                            var nextClassId = classIds.FirstOrDefault(id => id > classId);
                            toClassId = nextClassId > 0 ? nextClassId : null;
                            resultText = "Promoted";
                            if (toClassId != null)
                            {
                                await studentRepository.UpdateStudentAsync(new Student
                                {
                                    StudentId = student.StudentId,
                                    FirstName = student.FirstName,
                                    LastName = student.LastName,
                                    FatherName = student.FatherName,
                                    ClassId = toClassId.Value,
                                    RollNumber = student.RollNumber,
                                    RegistrationDate = student.RegistrationDate
                                }, cancellationToken);
                            }
                            result.PromotedCount++;
                            break;
                        case PromotionOutcome.Conditional:
                            resultText = "Conditional";
                            result.ConditionalCount++;
                            break;
                        default:
                            resultText = "Repeat";
                            result.RepeatCount++;
                            break;
                    }

                    await historyRepository.AddAsync(new StudentPromotionHistory
                    {
                        StudentId = student.StudentId,
                        FromClassId = classId,
                        ToClassId = toClassId,
                        AcademicYearId = academicYearId,
                        Result = resultText,
                        PromotionDate = DateTime.Now
                    }, cancellationToken);

                    result.TotalStudents++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"شاگرد آیدی {student.StudentId}: {ex.Message}");
                }
            }
        }

        return result;
    }

    public Task<IReadOnlyList<PromotionHistoryDto>> GetPromotionHistoryAsync(
        int? academicYearId = null,
        int? studentId = null,
        CancellationToken cancellationToken = default)
    {
        return historyRepository.GetHistoryAsync(academicYearId, studentId, cancellationToken);
    }

    private async Task<PromotionOutcome> DeterminePromotionAsync(
        Student student,
        int classId,
        int academicYearId,
        CancellationToken cancellationToken)
    {
        var subjects = await classSubjectRepository.GetSubjectsByClassAsync(classId, cancellationToken);
        var marks = await examMarkRepository.GetMarksByStudentAndYearAsync(student.StudentId, academicYearId, cancellationToken);
        var markMap = marks.ToDictionary(m => m.SubjectId);

        decimal totalObtained = 0m;
        int failedCount = 0;

        foreach (var subject in subjects)
        {
            markMap.TryGetValue(subject.SubjectId, out var mark);
            var midterm = mark?.MidtermScore ?? 0m;
            var final = mark?.FinalScore ?? 0m;
            var total = GradingPolicy.CalculateTotal(midterm, final);
            var isPass = GradingPolicy.IsPass(total);

            totalObtained += total;
            if (!isPass) failedCount++;
        }

        var totalMax = subjects.Count * GradingPolicy.TotalMax;
        var average = totalMax > 0 ? (totalObtained / totalMax) * 100m : 0m;

        var absenceDays = await attendanceRepository.GetAbsenceDaysByStudentAndYearAsync(student.StudentId, academicYearId, cancellationToken);

        return PromotionPolicy.GetPromotionOutcome(average, failedCount, absenceDays);
    }
}
