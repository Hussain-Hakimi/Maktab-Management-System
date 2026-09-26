using Maktab.Application.Abstractions;

namespace Maktab.Application.Services;

public sealed class AlertService(
    IAttendanceService attendanceService,
    IAcademicYearService academicYearService,
    IStudentService studentService,
    IAppLogger logger) : IAlertService
{
    private const decimal AbsenceRateThreshold = 20m;
    public async Task<IReadOnlyList<AlertItemDto>> GetAlertsAsync(CancellationToken cancellationToken = default)
    {
        var alerts = new List<AlertItemDto>();

        await AddHighAbsenceAlertsAsync(alerts, cancellationToken);

        // Order by severity: Critical first, then Warning, then Info
        return alerts
            .OrderByDescending(a => a.Severity)
            .ThenBy(a => a.Type)
            .ToList();
    }

    private async Task AddHighAbsenceAlertsAsync(
        List<AlertItemDto> alerts,
        CancellationToken cancellationToken)
    {
        try
        {
            var activeYear = await academicYearService.GetActiveAcademicYearAsync(cancellationToken);
            if (activeYear is null)
                return;

            var students = await studentService.GetAllStudentsAsync(cancellationToken);
            foreach (var student in students)
            {
                var summary = await attendanceService.GetStudentAttendanceSummaryAsync(
                    student.StudentId,
                    activeYear.AcademicYearId,
                    cancellationToken);

                if (summary is null || summary.TotalDays == 0)
                    continue;

                if (summary.AbsenceRate > AbsenceRateThreshold)
                {
                    alerts.Add(new AlertItemDto
                    {
                        Type = "HighAbsence",
                        Severity = summary.AbsenceRate > 40m ? AlertSeverity.Critical : AlertSeverity.Warning,
                        Message = $"شاگرد {summary.StudentName} ({summary.RollNumber}) نرخ غیبت {summary.AbsenceRate}% دارد.",
                        EntityReference = student.StudentId.ToString()
                    });
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to generate high-absence alerts.", ex);
        }
    }
}
