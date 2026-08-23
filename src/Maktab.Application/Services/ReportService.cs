using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;
using Maktab.Domain.Rules;

namespace Maktab.Application.Services;

public sealed class ReportService(
    IStudentRepository studentRepository,
    IClassSubjectRepository classSubjectRepository,
    IExamMarkRepository examMarkRepository,
    IAttendanceRepository attendanceRepository,
    IFeeRepository feeRepository,
    IAcademicYearRepository academicYearRepository) : IReportService
{
    public async Task<ClassPerformanceReportDto> GetClassPerformanceAsync(int classId, int academicYearId, CancellationToken cancellationToken = default)
    {
        var students = await studentRepository.GetStudentsByClassAsync(classId, cancellationToken);
        var subjects = await classSubjectRepository.GetSubjectsByClassAsync(classId, cancellationToken);
        var className = (await classSubjectRepository.GetClassesAsync(cancellationToken)).FirstOrDefault(c => c.ClassId == classId)?.GradeName ?? $"صنف {classId}";
        var yearName = (await academicYearRepository.GetByIdAsync(academicYearId, cancellationToken))?.YearName ?? "نامشخص";

        var subjectPerformances = new List<SubjectPerformanceDto>();
        decimal totalSum = 0m;
        int totalPass = 0;
        int totalFail = 0;

        foreach (var subject in subjects)
        {
            var marks = await examMarkRepository.GetMarksByClassAndSubjectAsync(classId, subject.SubjectId, cancellationToken);
            var scores = marks.Select(m => GradingPolicy.CalculateTotal(m.MidtermScore, m.FinalScore)).ToList();
            decimal avg = scores.Count > 0 ? scores.Average() : 0m;
            int pass = scores.Count(s => s >= GradingPolicy.PassingMark);
            int fail = scores.Count - pass;

            subjectPerformances.Add(new SubjectPerformanceDto
            {
                SubjectName = subject.SubjectName,
                AverageScore = Math.Round(avg, 2),
                PassCount = pass,
                FailCount = fail
            });

            totalSum += avg * scores.Count;
            totalPass += pass;
            totalFail += fail;
        }

        decimal overall = students.Count > 0 ? Math.Round(totalSum / (students.Count * subjects.Count), 2) : 0m;

        return new ClassPerformanceReportDto
        {
            ClassName = className,
            AcademicYear = yearName,
            TotalStudents = students.Count,
            OverallAverage = overall,
            PassCount = totalPass,
            FailCount = totalFail,
            SubjectPerformances = subjectPerformances
        };
    }

    public async Task<GradeDistributionDto> GetGradeDistributionAsync(int classId, int academicYearId, CancellationToken cancellationToken = default)
    {
        var students = await studentRepository.GetStudentsByClassAsync(classId, cancellationToken);
        var subjects = await classSubjectRepository.GetSubjectsByClassAsync(classId, cancellationToken);
        var className = (await classSubjectRepository.GetClassesAsync(cancellationToken)).FirstOrDefault(c => c.ClassId == classId)?.GradeName ?? $"صنف {classId}";
        var yearName = (await academicYearRepository.GetByIdAsync(academicYearId, cancellationToken))?.YearName ?? "نامشخص";

        var distribution = new GradeDistributionDto { ClassName = className, AcademicYear = yearName };

        foreach (var student in students)
        {
            var marks = await examMarkRepository.GetMarksByStudentAndYearAsync(student.StudentId, academicYearId, cancellationToken);
            var markMap = marks.ToDictionary(m => m.SubjectId);

            decimal totalObtained = 0m;
            foreach (var subject in subjects)
            {
                markMap.TryGetValue(subject.SubjectId, out var mark);
                var total = GradingPolicy.CalculateTotal(mark?.MidtermScore ?? 0m, mark?.FinalScore ?? 0m);
                totalObtained += total;
            }
            var avg = subjects.Count > 0 ? (totalObtained / (subjects.Count * GradingPolicy.TotalMax)) * 100m : 0m;
            var grade = GradingPolicy.ResolveLetterGrade(avg);

            switch (grade)
            {
                case LetterGrade.A: distribution.CountA++; break;
                case LetterGrade.B: distribution.CountB++; break;
                case LetterGrade.C: distribution.CountC++; break;
                case LetterGrade.D: distribution.CountD++; break;
                default: distribution.CountF++; break;
            }
        }

        return distribution;
    }

    public async Task<IReadOnlyList<StudentExportRowDto>> GetStudentExportDataAsync(int classId, CancellationToken cancellationToken = default)
    {
        var students = await studentRepository.GetStudentsByClassAsync(classId, cancellationToken);
        var classDict = (await classSubjectRepository.GetClassesAsync(cancellationToken)).ToDictionary(c => c.ClassId, c => c.GradeName);

        return students.Select(s => new StudentExportRowDto
        {
            StudentId = s.StudentId,
            FirstName = s.FirstName,
            LastName = s.LastName,
            FatherName = s.FatherName,
            RollNumber = s.RollNumber,
            ClassName = classDict.TryGetValue(s.ClassId, out var name) ? name : $"صنف {s.ClassId}",
            RegistrationDate = s.RegistrationDate
        }).ToList();
    }

    public async Task<IReadOnlyList<MarkExportRowDto>> GetMarkExportDataAsync(int classId, int subjectId, int academicYearId, CancellationToken cancellationToken = default)
    {
        var marks = await examMarkRepository.GetMarksByClassAndSubjectAsync(classId, subjectId, cancellationToken);
        var students = await studentRepository.GetStudentsByClassAsync(classId, cancellationToken);
        var subjectName = (await classSubjectRepository.GetSubjectsByClassAsync(classId, cancellationToken)).FirstOrDefault(s => s.SubjectId == subjectId)?.SubjectName ?? "مضمون";
        var studentDict = students.ToDictionary(s => s.StudentId);

        return marks.Select(m =>
        {
            var student = studentDict[m.StudentId];
            var total = GradingPolicy.CalculateTotal(m.MidtermScore, m.FinalScore);
            return new MarkExportRowDto
            {
                StudentName = $"{student.FirstName} {student.LastName}",
                RollNumber = student.RollNumber,
                SubjectName = subjectName,
                MidtermScore = m.MidtermScore,
                FinalScore = m.FinalScore,
                TotalScore = total,
                Status = GradingPolicy.IsPass(total) ? "کامیاب" : "ناکام"
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<AttendanceExportRowDto>> GetAttendanceExportDataAsync(int classId, int academicYearId, CancellationToken cancellationToken = default)
    {
        var students = await studentRepository.GetStudentsByClassAsync(classId, cancellationToken);
        var result = new List<AttendanceExportRowDto>();

        foreach (var student in students)
        {
            var records = await attendanceRepository.GetByStudentAndRangeAsync(student.StudentId, DateTime.MinValue, DateTime.MaxValue, cancellationToken);
            var yearRecords = records.Where(r => r.AcademicYearId == academicYearId).ToList();
            int present = yearRecords.Count(r => r.Status == AttendanceStatus.Present);
            int absent = yearRecords.Count(r => r.Status == AttendanceStatus.Absent);
            int ill = yearRecords.Count(r => r.Status == AttendanceStatus.Ill);
            int permission = yearRecords.Count(r => r.Status == AttendanceStatus.Permission);
            decimal rate = (yearRecords.Count > 0) ? Math.Round((decimal)absent / yearRecords.Count * 100, 2) : 0m;

            result.Add(new AttendanceExportRowDto
            {
                StudentName = $"{student.FirstName} {student.LastName}",
                RollNumber = student.RollNumber,
                PresentDays = present,
                AbsentDays = absent,
                IllDays = ill,
                PermissionDays = permission,
                AbsenceRate = rate
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<FeeExportRowDto>> GetFeeExportDataAsync(int classId, int academicYearId, CancellationToken cancellationToken = default)
    {
        var students = await studentRepository.GetStudentsByClassAsync(classId, cancellationToken);
        var fees = await feeRepository.GetFeesAsync(cancellationToken);
        var result = new List<FeeExportRowDto>();

        foreach (var fee in fees.Where(f => students.Any(s => s.StudentId == f.StudentId)))
        {
            result.Add(new FeeExportRowDto
            {
                StudentName = fee.StudentName,
                RollNumber = fee.RollNumber,
                FeeType = fee.FeeType,
                Amount = fee.Amount,
                TotalPaid = fee.TotalPaid,
                Outstanding = fee.Outstanding,
                Status = fee.Status.ToString()
            });
        }

        return result;
    }
}
