using System.Globalization;
using System.Windows;
using Maktab.Application.Services;
using Maktab.App.Wpf.Views;

namespace Maktab.App.Wpf;

public partial class MainWindow : Window
{
public MainWindow(
    ClassSubjectView classSubjectView,
    StudentManagementView studentManagementView,
    MarksEntryView marksEntryView,
    AttendanceView attendanceView,
    LibraryView libraryView,
    TextbookView textbookView,
    ReportCardsView reportCardsView,
    BackupSettingsView backupSettingsView)
{
    InitializeComponent();

    // Embed each view into its corresponding tab's ContentControl
    ClassSubjectContent.Content = classSubjectView;
    StudentManagementContent.Content = studentManagementView;
    MarksEntryContent.Content = marksEntryView;
    AttendanceContent.Content = attendanceView;
    LibraryContent.Content = libraryView;
    TextbookContent.Content = textbookView;
    ReportCardsContent.Content = reportCardsView;
    BackupSettingsContent.Content = backupSettingsView;

    Loaded += MainWindow_Loaded;
}

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Display current Shamsi/Hijri-Solar date and Gregorian date
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
}
