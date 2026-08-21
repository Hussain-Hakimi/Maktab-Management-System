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

    public DashboardView(
        IStudentService studentService,
        IClassSubjectService classSubjectService,
        IAttendanceService attendanceService,
        IFeeService feeService,
        IBookService bookService,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _studentService = studentService;
        _classSubjectService = classSubjectService;
        _attendanceService = attendanceService;
        _feeService = feeService;
        _bookService = bookService;
        _auditService = auditService;
        _currentUserService = currentUserService;

        InitializeComponent();
        Loaded += DashboardView_Loaded;
    }

    private async void DashboardView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadSummaryAsync();
    }

    private async Task LoadSummaryAsync()
    {
        try
        {
            // Total students
            var students = await _studentService.GetAllStudentsAsync();
            TotalStudentsTextBlock.Text = students.Count.ToString();

            // Total classes
            var classes = await _classSubjectService.GetClassesAsync();
            TotalClassesTextBlock.Text = classes.Count.ToString();

            // Today's attendance
            try
            {
                var today = DateTime.Today;
                // We need to count attendance for all classes, but we can simply sum if we iterate classes.
                // For simplicity, we show overall count from all classes.
                int totalStudents = students.Count;
                int presentCount = 0;
                foreach (var cls in classes)
                {
                    var attendance = await _attendanceService.GetClassAttendanceForDateAsync(cls.ClassId, today);
                    presentCount += attendance.Count(a => a.Status == AttendanceStatus.Present);
                }
                TodayAttendanceTextBlock.Text = $"{presentCount}/{totalStudents}";
            }
            catch
            {
                TodayAttendanceTextBlock.Text = "نامشخص";
            }

            // Outstanding fees
            try
            {
                var fees = await _feeService.GetFeesAsync();
                decimal outstanding = fees.Sum(f => f.Outstanding);
                OutstandingFeesTextBlock.Text = outstanding.ToString("N0");
            }
            catch
            {
                OutstandingFeesTextBlock.Text = "نامشخص";
            }

            // Overdue books
            try
            {
                var overdue = await _bookService.GetOverdueIssuesAsync();
                OverdueBooksTextBlock.Text = overdue.Count.ToString();
            }
            catch
            {
                OverdueBooksTextBlock.Text = "نامشخص";
            }

            // Recent audit logs (Admin only)
            try
            {
                if (_currentUserService.CurrentUser?.Role == UserRole.Admin)
                {
                    var logs = await _auditService.GetRecentLogsAsync(3);
                    if (logs.Count > 0)
                    {
                        RecentAuditTextBlock.Text = string.Join("\n", logs.Select(l => $"{l.UserName}: {l.Action}"));
                    }
                    else
                    {
                        RecentAuditTextBlock.Text = "هیچ واقعه‌ای ثبت نشده است.";
                    }
                }
                else
                {
                    RecentAuditTextBlock.Text = "فقط مدیر سیستم";
                }
            }
            catch
            {
                RecentAuditTextBlock.Text = "نامشخص";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری داشبورد:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
