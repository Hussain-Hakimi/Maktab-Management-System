using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Maktab.App.Wpf.Services;
using Maktab.App.Wpf.Views;
using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Enums;

namespace Maktab.App.Wpf;

public partial class MainWindow : Window
{
    private readonly NavigationService _navigationService;
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBackupService _backupService;

    private UserDto? _currentUser;

    // View instances
    private readonly ClassSubjectView _classSubjectView;
    private readonly StudentManagementView _studentManagementView;
    private readonly MarksEntryView _marksEntryView;
    private readonly StudentGradesView _studentGradesView;
    private readonly AttendanceView _attendanceView;
    private readonly AttendanceReportsView _attendanceReportsView;
    private readonly LibraryView _libraryView;
    private readonly TextbookView _textbookView;
    private readonly FeesView _feesView;
    private readonly ReportCardsView _reportCardsView;
    private readonly ReportsView _reportsView;
    private readonly BackupSettingsView _backupSettingsView;
    private readonly DashboardView _dashboardView;
    private readonly AlertsView _alertsView;
    private readonly UserManagementView _userManagementView;
    private readonly PromotionSettingsView _promotionSettingsView;
    private readonly BulkImportView _bulkImportView;
    private readonly AuditLogView _auditLogView;
    private readonly SchoolSettingsView _schoolSettingsView;
    private readonly AcademicYearView _academicYearView;
    private readonly PromotionHistoryView _promotionHistoryView;

    public MainWindow(
        ClassSubjectView classSubjectView,
        StudentManagementView studentManagementView,
        MarksEntryView marksEntryView,
        StudentGradesView studentGradesView,
        AttendanceView attendanceView,
        AttendanceReportsView attendanceReportsView,
        LibraryView libraryView,
        TextbookView textbookView,
        FeesView feesView,
        ReportCardsView reportCardsView,
        ReportsView reportsView,
        BackupSettingsView backupSettingsView,
        DashboardView dashboardView,
        AlertsView alertsView,
        UserManagementView userManagementView,
        PromotionSettingsView promotionSettingsView,
        BulkImportView bulkImportView,
        AuditLogView auditLogView,
        SchoolSettingsView schoolSettingsView,
        AcademicYearView academicYearView,
        PromotionHistoryView promotionHistoryView,
        IUserService userService,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        IBackupService backupService)
    {
        InitializeComponent();

        _classSubjectView = classSubjectView;
        _studentManagementView = studentManagementView;
        _marksEntryView = marksEntryView;
        _studentGradesView = studentGradesView;
        _attendanceView = attendanceView;
        _attendanceReportsView = attendanceReportsView;
        _libraryView = libraryView;
        _textbookView = textbookView;
        _feesView = feesView;
        _reportCardsView = reportCardsView;
        _reportsView = reportsView;
        _backupSettingsView = backupSettingsView;
        _dashboardView = dashboardView;
        _alertsView = alertsView;
        _userManagementView = userManagementView;
        _promotionSettingsView = promotionSettingsView;
        _bulkImportView = bulkImportView;
        _auditLogView = auditLogView;
        _schoolSettingsView = schoolSettingsView;
        _academicYearView = academicYearView;
        _promotionHistoryView = promotionHistoryView;

        _userService = userService;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _backupService = backupService;

        _navigationService = new NavigationService(MainContentArea);

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    public void SetCurrentUser(UserDto user)
    {
        _currentUser = user;
        CurrentUserTextBlock.Text = $"👤 {user.FullName} ({user.Role})";
        ApplyRoleBasedVisibility();
        // Select first available main tab and sub item
        MainTabs.SelectedIndex = 0;
    }

    private void ApplyRoleBasedVisibility()
    {
        if (_currentUser is null) return;

        // Show/hide main tabs based on role
        DashboardTab.Visibility = Visibility.Visible;

        bool isAdmin = _currentUser.Role == UserRole.Admin;
        bool isTeacher = _currentUser.Role == UserRole.Teacher;
        bool isLibrarian = _currentUser.Role == UserRole.Librarian;
        bool isAccountant = _currentUser.Role == UserRole.Accountant;

        AcademicTab.Visibility = (isAdmin || isTeacher) ? Visibility.Visible : Visibility.Collapsed;
        AttendanceTab.Visibility = (isAdmin || isTeacher) ? Visibility.Visible : Visibility.Collapsed;
        OperationsTab.Visibility = (isAdmin || isLibrarian || isAccountant) ? Visibility.Visible : Visibility.Collapsed;
        AdminTab.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

        // Ensure selected tab is visible
        if (MainTabs.SelectedItem is TabItem selectedTab && selectedTab.Visibility != Visibility.Visible)
        {
            MainTabs.SelectedIndex = 0;
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var persianCalendar = new PersianCalendar();
            var now = DateTime.Now;
            var shamsiYear = persianCalendar.GetYear(now);
            var shamsiMonth = persianCalendar.GetMonth(now);
            var shamsiDay = persianCalendar.GetDayOfMonth(now);

            CurrentDateTextBlock.Text = $"📅 {shamsiYear}/{shamsiMonth:D2}/{shamsiDay:D2} — {now:yyyy/MM/dd}";
            SchoolYearTextBlock.Text = $"سال تحصیلی: {AcademicYearProvider.GetCurrentAcademicYear(now)}";
        }
        catch
        {
            CurrentDateTextBlock.Text = $"📅 {DateTime.Now:yyyy/MM/dd}";
        }

        StatusBarText.Text = "✅ سیستم آماده استفاده است.";
    }

    private async void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentUser is null)
            return;

        var changePasswordWindow = new ChangePasswordWindow(_userService, _currentUser);
        var result = changePasswordWindow.ShowDialog();

        if (result == true)
        {
            await _auditService.LogAsync(_currentUser.Username, "تغییر رمز عبور");
            MessageBox.Show("رمز عبور با موفقیت تغییر کرد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SubMenuListBox is null)
            return;

        // Populate sub-menu based on selected main tab
        SubMenuListBox.Items.Clear();

        string? tabName = (MainTabs.SelectedItem as TabItem)?.Name;
        if (tabName == "DashboardTab")
        {
            SubMenuListBox.Items.Add(new ListBoxItem { Content = "🏠 داشبورد", Tag = "Dashboard" });
            SubMenuListBox.SelectedIndex = 0;
        }
        else if (tabName == "AcademicTab")
        {
            if (_currentUser?.Role == UserRole.Admin || _currentUser?.Role == UserRole.Teacher)
            {
                SubMenuListBox.Items.Add(new ListBoxItem { Content = "👨‍🎓 شاگردان", Tag = "Students" });
                SubMenuListBox.Items.Add(new ListBoxItem { Content = "🏫 صنف‌ها و مضامین", Tag = "Classes" });
                SubMenuListBox.Items.Add(new ListBoxItem { Content = "📝 ثبت نمرات", Tag = "MarksEntry" });
                SubMenuListBox.Items.Add(new ListBoxItem { Content = "🎓 نمرات شاگرد", Tag = "StudentGrades" });
                SubMenuListBox.Items.Add(new ListBoxItem { Content = "📄 کارنامه / اطلاع‌نامه", Tag = "ReportCards" });
                SubMenuListBox.Items.Add(new ListBoxItem { Content = "📜 تاریخچه ارتقاء", Tag = "PromotionHistory" });
                SubMenuListBox.SelectedIndex = 0;
            }
        }
        else if (tabName == "AttendanceTab")
        {
            if (_currentUser?.Role == UserRole.Admin || _currentUser?.Role == UserRole.Teacher)
            {
                SubMenuListBox.Items.Add(new ListBoxItem { Content = "🗓️ حاضری", Tag = "Attendance" });
                SubMenuListBox.Items.Add(new ListBoxItem { Content = "🗓️ گزارش حاضری", Tag = "AttendanceReports" });
                SubMenuListBox.SelectedIndex = 0;
            }
        }
        else if (tabName == "OperationsTab")
        {
            if (_currentUser?.Role == UserRole.Admin || _currentUser?.Role == UserRole.Librarian)
            {
                SubMenuListBox.Items.Add(new ListBoxItem { Content = "📚 کتابخانه", Tag = "Library" });
                SubMenuListBox.Items.Add(new ListBoxItem { Content = "📦 کتاب‌های درسی", Tag = "Textbooks" });
            }
            if (_currentUser?.Role == UserRole.Admin || _currentUser?.Role == UserRole.Accountant)
            {
                SubMenuListBox.Items.Add(new ListBoxItem { Content = "💰 فیس‌ها", Tag = "Fees" });
            }
            SubMenuListBox.SelectedIndex = SubMenuListBox.Items.Count > 0 ? 0 : -1;
        }
        else if (tabName == "AdminTab")
        {
            if (_currentUser?.Role == UserRole.Admin)
            {
                SubMenuListBox.Items.Add(new ListBoxItem { Content = "👥 مدیریت کاربران", Tag = "UserManagement" });
                SubMenuListBox.Items.Add(new ListBoxItem { Content = "⚙️ تنظیمات ارتقاء", Tag = "PromotionSettings" });
                SubMenuListBox.Items.Add(new ListBoxItem { Content = "📥 ورود اطلاعات دسته‌جمعی", Tag = "BulkImport" });
                SubMenuListBox.Items.Add(new ListBoxItem { Content = "📋 گزارش وقایع", Tag = "AuditLogs" });
                SubMenuListBox.Items.Add(new ListBoxItem { Content = "🏫 تنظیمات عمومی", Tag = "SchoolSettings" });
                SubMenuListBox.Items.Add(new ListBoxItem { Content = "📅 سال‌های تعلیمی", Tag = "AcademicYears" });
                SubMenuListBox.Items.Add(new ListBoxItem { Content = "📊 گزارش‌ها", Tag = "Reports" });
                SubMenuListBox.Items.Add(new ListBoxItem { Content = "🔔 اعلان‌ها", Tag = "Alerts" });
                SubMenuListBox.Items.Add(new ListBoxItem { Content = "💾 پشتیبان‌گیری و تنظیمات", Tag = "Backup" });
                SubMenuListBox.SelectedIndex = 0;
            }
        }
    }

    private void SubMenuListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SubMenuListBox.SelectedItem is not ListBoxItem item || item.Tag is not string tag)
            return;

        switch (tag)
        {
            case "Dashboard": _navigationService.Navigate(_dashboardView); break;
            case "Students": _navigationService.Navigate(_studentManagementView); break;
            case "Classes": _navigationService.Navigate(_classSubjectView); break;
            case "MarksEntry": _navigationService.Navigate(_marksEntryView); break;
            case "StudentGrades": _navigationService.Navigate(_studentGradesView); break;
            case "ReportCards": _navigationService.Navigate(_reportCardsView); break;
            case "PromotionHistory": _navigationService.Navigate(_promotionHistoryView); break;
            case "Attendance": _navigationService.Navigate(_attendanceView); break;
            case "AttendanceReports": _navigationService.Navigate(_attendanceReportsView); break;
            case "Library": _navigationService.Navigate(_libraryView); break;
            case "Textbooks": _navigationService.Navigate(_textbookView); break;
            case "Fees": _navigationService.Navigate(_feesView); break;
            case "UserManagement": _navigationService.Navigate(_userManagementView); break;
            case "PromotionSettings": _navigationService.Navigate(_promotionSettingsView); break;
            case "BulkImport": _navigationService.Navigate(_bulkImportView); break;
            case "AuditLogs": _navigationService.Navigate(_auditLogView); break;
            case "SchoolSettings": _navigationService.Navigate(_schoolSettingsView); break;
            case "AcademicYears": _navigationService.Navigate(_academicYearView); break;
            case "Reports": _navigationService.Navigate(_reportsView); break;
            case "Alerts": _navigationService.Navigate(_alertsView); break;
            case "Backup": _navigationService.Navigate(_backupSettingsView); break;
        }
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        try
        {
            var lastBackup = _backupService.GetLastBackupDateAsync().GetAwaiter().GetResult();
            if (lastBackup is null || (DateTime.Now - lastBackup.Value).TotalDays > 7)
            {
                var result = MessageBox.Show(
                    "هشدار: آخرین نسخه پشتیبان بیش از ۷ روز پیش تهیه شده است.\nآیا مطمئن هستید که می‌خواهید بدون تهیه نسخه پشتیبان خارج شوید؟",
                    "یادآوری پشتیبان‌گیری",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    e.Cancel = true;
            }
        }
        catch
        {
            // Ignore backup check errors
        }
    }
}
