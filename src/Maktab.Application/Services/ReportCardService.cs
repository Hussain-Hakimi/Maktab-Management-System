using Maktab.Application.Abstractions;
using Maktab.Domain.Rules;

namespace Maktab.Application.Services;

public sealed class ReportCardService(
    IStudentRepository studentRepository,
    IClassSubjectRepository classSubjectRepository,
    IExamMarkRepository markRepository,
    IPdfReportCardGenerator pdfGenerator) : IReportCardService
{
    public async Task<StudentReportCardDto> GetStudentReportCardDataAsync(
        int studentId,
        string academicYear,
        CancellationToken cancellationToken = default)
    {
        if (studentId <= 0) throw new ArgumentOutOfRangeException(nameof(studentId));

        var student = await studentRepository.GetStudentByIdAsync(studentId, cancellationToken);
        if (student is null)
        {
            throw new InvalidOperationException($"شاگرد با آیدی {studentId} یافت نشد.");
        }

        var classes = await classSubjectRepository.GetClassesAsync(cancellationToken);
        var schoolClass = classes.FirstOrDefault(c => c.ClassId == student.ClassId);
        var className = schoolClass?.GradeName ?? $"صنف {student.ClassId}";

        var subjects = await classSubjectRepository.GetSubjectsByClassAsync(student.ClassId, cancellationToken);
        var marks = await markRepository.GetMarksByStudentAsync(studentId, cancellationToken);
        var markMap = marks.ToDictionary(m => m.SubjectId);

        var subjectReports = new List<SubjectMarkReportDto>();
        decimal totalObtained = 0m;
        int failedCount = 0;
        int passedCount = 0;

        foreach (var subject in subjects.OrderBy(s => s.SubjectName))
        {
            markMap.TryGetValue(subject.SubjectId, out var mark);
            var midterm = mark?.MidtermScore ?? 0m;
            var final = mark?.FinalScore ?? 0m;
            var total = GradingPolicy.CalculateTotal(midterm, final);
            var isPass = GradingPolicy.IsPass(total);

            totalObtained += total;
            if (isPass) passedCount++; else failedCount++;

            subjectReports.Add(new SubjectMarkReportDto
            {
                SubjectName = subject.SubjectName,
                MidtermScore = midterm,
                FinalScore = final,
                TotalScore = total,
                IsPass = isPass
            });
        }

        var totalMaxScore = subjects.Count * GradingPolicy.TotalMax;
        var avgPercentage = totalMaxScore > 0 ? Math.Round((totalObtained / totalMaxScore) * 100m, 2) : 0m;
        var overallGrade = GradingPolicy.ResolveLetterGrade(avgPercentage);
        var absenceDays = 0; // Baseline v1.0 attendance

        var outcome = PromotionPolicy.GetPromotionOutcome(avgPercentage, failedCount, absenceDays);
        string promoText;
        string? failureReason = null;

        switch (outcome)
        {
            case PromotionOutcome.Promoted:
                promoText = "ارتقاء نموده است (PROMOTED)";
                break;
            case PromotionOutcome.Conditional:
                promoText = "مشروط (CONDITIONAL)";
                failureReason = "عدم تکمیل معیار قبولی در برخی مضامین";
                break;
            default: // Repeat
                promoText = "تکرار صنف (REPEAT)";
                failureReason = failedCount > PromotionPolicy.MaxAllowedFailedSubjects
                    ? $"بیش از {PromotionPolicy.MaxAllowedFailedSubjects} مضمون ناکام ({failedCount} مضمون)"
                    : (avgPercentage < PromotionPolicy.PassingAverage ? $"اوسط نمرات کمتر از {PromotionPolicy.PassingAverage}" : $"بیش از {PromotionPolicy.MaxAllowedAbsenceDays} روز غیرحاضری");
                break;
        }

        return new StudentReportCardDto
        {
            StudentId = student.StudentId,
            FirstName = student.FirstName,
            LastName = student.LastName,
            FatherName = student.FatherName,
            RollNumber = student.RollNumber,
            ClassId = student.ClassId,
            ClassName = className,
            AcademicYear = string.IsNullOrWhiteSpace(academicYear) ? "۱۴۰۳ - ۱۴۰۴" : academicYear.Trim(),
            IssueDate = DateTime.Now.ToString("yyyy/MM/dd"),
            SubjectMarks = subjectReports,
            TotalObtainedScore = totalObtained,
            TotalMaxScore = totalMaxScore,
            AveragePercentage = avgPercentage,
            OverallGrade = overallGrade,
            PassedSubjectsCount = passedCount,
            FailedSubjectsCount = failedCount,
            AbsenceDays = absenceDays,
            PromotionOutcome = outcome,
            PromotionStatusText = promoText,
            FailureReason = failureReason
        };
    }

    public async Task<IReadOnlyList<StudentReportCardDto>> GetClassReportCardsDataAsync(
        int classId,
        string academicYear,
        CancellationToken cancellationToken = default)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));

        var students = await studentRepository.GetStudentsByClassAsync(classId, cancellationToken);
        var list = new List<StudentReportCardDto>();

        foreach (var student in students)
        {
            var data = await GetStudentReportCardDataAsync(student.StudentId, academicYear, cancellationToken);
            list.Add(data);
        }

        return list;
    }

    public async Task<string> GenerateStudentReportCardPdfAsync(
        int studentId,
        string academicYear,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        var data = await GetStudentReportCardDataAsync(studentId, academicYear, cancellationToken);
        Directory.CreateDirectory(outputDirectory);

        var safeYear = data.AcademicYear.Replace(" ", "").Replace("-", "_").Replace("/", "_");
        var safeName = $"{data.FirstName}_{data.LastName}".Replace(" ", "_");
        var fileName = $"{safeName}_{data.StudentId:D4}_{safeYear}.pdf";
        var filePath = Path.Combine(outputDirectory, fileName);

        await pdfGenerator.GeneratePdfReportAsync(data, filePath, cancellationToken);
        return filePath;
    }

    public async Task<IReadOnlyList<string>> GenerateClassReportCardsPdfAsync(
        int classId,
        string academicYear,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        var reports = await GetClassReportCardsDataAsync(classId, academicYear, cancellationToken);
        Directory.CreateDirectory(outputDirectory);

        var generatedPaths = new List<string>();
        foreach (var data in reports)
        {
            var safeYear = data.AcademicYear.Replace(" ", "").Replace("-", "_").Replace("/", "_");
            var safeName = $"{data.FirstName}_{data.LastName}".Replace(" ", "_");
            var fileName = $"{safeName}_{data.StudentId:D4}_{safeYear}.pdf";
            var filePath = Path.Combine(outputDirectory, fileName);

            await pdfGenerator.GeneratePdfReportAsync(data, filePath, cancellationToken);
            generatedPaths.Add(filePath);
        }

        return generatedPaths;
    }
}
