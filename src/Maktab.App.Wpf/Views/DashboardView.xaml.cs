using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;
using Maktab.Domain.Enums;

namespace Maktab.App.Wpf.Views;

public partial class DashboardView : UserControl
{
    private readonly IStudentService _studentService;
    private readonly IClassSubjectService _classSubjectService;
    private readonly IAttendanceService _attendanceService;
    private readonly IFeeService _feeService;
    private readonly IBookService _bookService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAlertService _alertService;
    private readonly IAppLogger _logger;

    public DashboardView(
        IStudentService studentService,
        IClassSubjectService classSubjectService,
        IAttendanceService attendanceService,
        IFeeService feeService,
        IBookService bookService,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        IAlertService alertService,
        IAppLogger logger)
    {
        _studentService = studentService;
        _classSubjectService = classSubjectService;
        _attendanceService = attendanceService;
        _feeService = feeService;
        _bookService = bookService;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _alertService = alertService;
        _logger = logger;

        InitializeComponent();
        Loaded += DashboardView_Loaded;
    }

    private async void DashboardView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadSummaryAsync();
        await LoadAlertsAsync();
    }

    private async Task LoadSummaryAsync()
    {
        try
        {
            var students = await _studentService.GetAllStudentsAsync();
            TotalStudentsTextBlock.Text = students.Count.ToString();

            var classes = await _classSubjectService.GetClassesAsync();
            TotalClassesTextBlock.Text = classes.Count.ToString();

            try
            {
                var today = DateTime.Today;
                int totalStudents = students.Count;
                int presentCount = 0;
                int absentCount = 0;

                foreach (var cls in classes)
                {
                    var attendance = await _attendanceService.GetClassAttendanceForDateAsync(cls.ClassId, today);
                    presentCount += attendance.Count(a => a.Status == AttendanceStatus.Present);
                    absentCount += attendance.Count(a => a.Status == AttendanceStatus.Absent);
                }

                TodayAttendanceTextBlock.Text = $"{presentCount}/{totalStudents}";
                decimal absenceRate = totalStudents > 0 ? Math.Round((decimal)absentCount / totalStudents * 100, 2) : 0m;
                TodayAbsenceRateTextBlock.Text = $"غیبت: {absenceRate}%";
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to load dashboard attendance summary.", ex);
                TodayAttendanceTextBlock.Text = "نامشخص";
                TodayAbsenceRateTextBlock.Text = "غیبت: نامشخص";
            }

            try
            {
                var fees = await _feeService.GetFeesAsync();
                decimal totalAmount = fees.Sum(f => f.Amount);
                decimal totalPaid = fees.Sum(f => f.TotalPaid);
                decimal outstanding = totalAmount - totalPaid;

                OutstandingFeesTextBlock.Text = outstanding.ToString("N0");

                if (totalAmount > 0)
                {
                    double progress = (double)(totalPaid / totalAmount * 100);
                    FeeProgressBar.Value = progress;
                    FeeCollectionRateTextBlock.Text = $"وصول: {progress:F1}%";
                }
                else
                {
                    FeeProgressBar.Value = 0;
                    FeeCollectionRateTextBlock.Text = "وصول: ۰%";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to load dashboard fee summary.", ex);
                OutstandingFeesTextBlock.Text = "نامشخص";
                FeeProgressBar.Value = 0;
                FeeCollectionRateTextBlock.Text = "وصول: نامشخص";
            }

            try
            {
                var overdue = await _bookService.GetOverdueIssuesAsync();
                OverdueBooksTextBlock.Text = overdue.Count.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to load dashboard overdue books summary.", ex);
                OverdueBooksTextBlock.Text = "نامشخص";
            }

            try
            {
                if (_currentUserService.CurrentUser?.Role == UserRole.Admin)
                {
                    var logs = await _auditService.GetRecentLogsAsync(3);
                    RecentAuditTextBlock.Text = logs.Count > 0
                        ? string.Join("\n", logs.Select(l => $"{l.UserName}: {l.Action}"))
                        : "هیچ واقعه‌ای ثبت نشده است.";
                }
                else
                {
                    RecentAuditTextBlock.Text = "فقط مدیر سیستم";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to load dashboard recent audit summary.", ex);
                RecentAuditTextBlock.Text = "نامشخص";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to load dashboard summary.", ex);
            MessageBox.Show($"خطا در بارگذاری داشبورد:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadAlertsAsync()
    {
        try
        {
            var alerts = await _alertService.GetAlertsAsync();
            var critical = alerts.Count(a => a.Severity == AlertSeverity.Critical);
            var warnings = alerts.Count(a => a.Severity == AlertSeverity.Warning);

            AlertsCountTextBlock.Text = $"{alerts.Count} اعلان — {critical} بحرانی، {warnings} هشدار";

            var topAlerts = alerts.Take(5).ToList();
            AlertsListItemsControl.ItemsSource = topAlerts;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to load dashboard alerts.", ex);
            AlertsCountTextBlock.Text = "امکان بارگذاری اعلان‌ها وجود ندارد.";
        }
    }
}
