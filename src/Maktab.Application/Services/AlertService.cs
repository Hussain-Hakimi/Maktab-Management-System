using Maktab.Application.Abstractions;

namespace Maktab.Application.Services;

public sealed class AlertService(
    IBookService bookService,
    IFeeService feeService,
    IAttendanceService attendanceService,
    IAcademicYearService academicYearService,
    IStudentService studentService,
    IAppLogger logger) : IAlertService
{
    private const decimal AbsenceRateThreshold = 20m;
    private const int OverdueDaysWarning = 7;
    private const int OverdueDaysCritical = 14;

    public async Task<IReadOnlyList<AlertItemDto>> GetAlertsAsync(CancellationToken cancellationToken = default)
    {
        var alerts = new List<AlertItemDto>();

        await AddOverdueBookAlertsAsync(alerts, cancellationToken);
        await AddOutstandingFeeAlertsAsync(alerts, cancellationToken);
        await AddHighAbsenceAlertsAsync(alerts, cancellationToken);

        // Order by severity: Critical first, then Warning, then Info
        return alerts
            .OrderByDescending(a => a.Severity)
            .ThenBy(a => a.Type)
            .ToList();
    }

    private async Task AddOverdueBookAlertsAsync(
        List<AlertItemDto> alerts,
        CancellationToken cancellationToken)
    {
        try
        {
            var overdue = await bookService.GetOverdueIssuesAsync(cancellationToken);
            foreach (var issue in overdue)
            {
                var daysOverdue = (DateTime.Today - issue.DueDate).Days;
                var severity = daysOverdue >= OverdueDaysCritical ? AlertSeverity.Critical : AlertSeverity.Warning;
                alerts.Add(new AlertItemDto
                {
                    Type = "OverdueBook",
                    Severity = severity,
                    Message = $"کتاب «{issue.BookTitle}» از طرف {issue.StudentName} (اساس: {issue.RollNumber}) {daysOverdue} روز عقب‌مانده است.",
                    EntityReference = issue.IssueId.ToString()
                });
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to generate overdue-book alerts.", ex);
        }
    }

    private async Task AddOutstandingFeeAlertsAsync(
        List<AlertItemDto> alerts,
        CancellationToken cancellationToken)
    {
        try
        {
            var fees = await feeService.GetFeesAsync(cancellationToken);
            foreach (var fee in fees.Where(f => f.Outstanding > 0m))
            {
                var severity = fee.Outstanding > 500m ? AlertSeverity.Critical : AlertSeverity.Warning;
                alerts.Add(new AlertItemDto
                {
                    Type = "OutstandingFee",
                    Severity = severity,
                    Message = $"فیس «{fee.FeeType}» برای {fee.StudentName} ({fee.RollNumber}) باقی‌مانده: {fee.Outstanding:N0} افغانی",
                    EntityReference = fee.FeeId.ToString()
                });
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to generate outstanding-fee alerts.", ex);
        }
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
