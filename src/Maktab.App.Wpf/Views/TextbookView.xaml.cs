using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.App.Wpf.Views;

public partial class TextbookView : UserControl
{
    private readonly ITextbookService _textbookService;
    private readonly IClassSubjectService _classSubjectService;
    private readonly IStudentService _studentService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    private readonly ObservableCollection<TextbookDto> _textbooks = [];
    private readonly ObservableCollection<TextbookIssueDto> _issues = [];
    private readonly List<SchoolClass> _classes = [];
    private readonly List<Student> _students = [];

    public TextbookView(
        ITextbookService textbookService,
        IClassSubjectService classSubjectService,
        IStudentService studentService,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _textbookService = textbookService;
        _classSubjectService = classSubjectService;
        _studentService = studentService;
        _auditService = auditService;
        _currentUserService = currentUserService;

        InitializeComponent();

        TextbooksDataGrid.ItemsSource = _textbooks;
        IssuesDataGrid.ItemsSource = _issues;
        Loaded += TextbookView_Loaded;
    }

    private async void TextbookView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadClassesAsync();
        await LoadStudentsAsync();
        await LoadTextbooksAsync();
        await LoadIssuesAsync();
    }

    public async Task InitializeDataAsync()
    {
        await LoadClassesAsync();
        await LoadStudentsAsync();
        await LoadTextbooksAsync();
        await LoadIssuesAsync();
    }

    private async Task LoadClassesAsync()
    {
        try
        {
            var classes = await _classSubjectService.GetClassesAsync();
            _classes.Clear();
            _classes.AddRange(classes);
            TextbookClassComboBox.ItemsSource = _classes.ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دریافت صنف‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadStudentsAsync()
    {
        try
        {
            var students = await _studentService.GetAllStudentsAsync();
            _students.Clear();
            _students.AddRange(students);
            IssueStudentComboBox.ItemsSource = _students.Select(s => new StudentComboItem
            {
                StudentId = s.StudentId,
                DisplayName = $"{s.FirstName} {s.LastName} (اساس: {s.RollNumber})"
            }).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دریافت شاگردان:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadTextbooksAsync()
    {
        try
        {
            var textbooks = await _textbookService.GetTextbooksAsync();
            _textbooks.Clear();
            foreach (var textbook in textbooks)
            {
                _textbooks.Add(textbook);
            }

            IssueTextbookComboBox.ItemsSource = _textbooks.ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دریافت کتاب‌های درسی:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadIssuesAsync()
    {
        try
        {
            var issues = await _textbookService.GetIssuesAsync();
            _issues.Clear();
            foreach (var issue in issues.Where(i => i.Status == TextbookIssueStatus.Issued))
            {
                _issues.Add(issue);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دریافت امانت‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private TextbookDto? GetSelectedTextbook() => TextbooksDataGrid.SelectedItem as TextbookDto;

    private void TextbooksDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = GetSelectedTextbook();
        if (selected is null) return;

        TextbookTitleTextBox.Text = selected.Title;
        TextbookSubjectTextBox.Text = selected.Subject ?? string.Empty;
        TextbookClassComboBox.SelectedValue = selected.ClassId;
        TextbookTotalCopiesTextBox.Text = selected.TotalCopies.ToString();
    }

    private void ClearTextbookForm()
    {
        TextbookTitleTextBox.Clear();
        TextbookSubjectTextBox.Clear();
        TextbookClassComboBox.SelectedIndex = -1;
        TextbookTotalCopiesTextBox.Clear();
        TextbooksDataGrid.SelectedItem = null;
    }

    private async void AddTextbookButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateTextbookInput(out var textbookDto)) return;

        try
        {
            await _textbookService.AddTextbookAsync(textbookDto);
            await LogAuditAsync($"افزودن کتاب درسی '{textbookDto.Title}'");
            await LoadTextbooksAsync();
            ClearTextbookForm();
            MessageBox.Show("کتاب درسی با موفقیت اضافه شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void UpdateTextbookButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedTextbook();
        if (selected is null)
        {
            MessageBox.Show("لطفاً یک کتاب درسی را انتخاب کنید.", "انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!ValidateTextbookInput(out var textbookDto)) return;

        try
        {
            await _textbookService.UpdateTextbookAsync(selected.TextbookId, textbookDto);
            await LogAuditAsync($"ویرایش کتاب درسی '{textbookDto.Title}'");
            await LoadTextbooksAsync();
            ClearTextbookForm();
            MessageBox.Show("کتاب درسی ویرایش شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void DeleteTextbookButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedTextbook();
        if (selected is null)
        {
            MessageBox.Show("لطفاً یک کتاب درسی را انتخاب کنید.", "انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show($"آیا از حذف کتاب درسی «{selected.Title}» اطمینان دارید؟", "تأیید حذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _textbookService.DeleteTextbookAsync(selected.TextbookId);
            await LogAuditAsync($"حذف کتاب درسی '{selected.Title}'");
            await LoadTextbooksAsync();
            ClearTextbookForm();
            MessageBox.Show("کتاب درسی حذف شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ClearTextbookFormButton_Click(object sender, RoutedEventArgs e)
    {
        ClearTextbookForm();
    }

    private async void IssueTextbookButton_Click(object sender, RoutedEventArgs e)
    {
        if (IssueTextbookComboBox.SelectedValue is not int textbookId || textbookId <= 0)
        {
            MessageBox.Show("لطفاً کتاب درسی را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (IssueStudentComboBox.SelectedValue is not int studentId || studentId <= 0)
        {
            MessageBox.Show("لطفاً شاگرد را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _textbookService.IssueTextbookAsync(new IssueTextbookDto(textbookId, studentId));
            await LogAuditAsync($"امانت کتاب درسی با آیدی {textbookId} به شاگرد آیدی {studentId}");
            await LoadTextbooksAsync();
            await LoadIssuesAsync();
            MessageBox.Show("کتاب درسی با موفقیت امانت داده شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void ReturnTextbookButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int issueId)
        {
            try
            {
                await _textbookService.ReturnTextbookAsync(new ReturnTextbookDto(issueId));
                await LogAuditAsync($"بازگشت کتاب درسی با شماره امانت {issueId}");
                await LoadTextbooksAsync();
                await LoadIssuesAsync();
                MessageBox.Show("کتاب درسی با موفقیت بازگشت داده شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void RefreshIssuesButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadIssuesAsync();
    }

    private bool ValidateTextbookInput(out SaveTextbookDto textbook)
    {
        textbook = null!;

        if (string.IsNullOrWhiteSpace(TextbookTitleTextBox.Text))
        {
            MessageBox.Show("عنوان کتاب درسی الزامی است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            TextbookTitleTextBox.Focus();
            return false;
        }

        if (!int.TryParse(TextbookTotalCopiesTextBox.Text, out var totalCopies) || totalCopies <= 0)
        {
            MessageBox.Show("تعداد کل نسخه‌ها باید یک عدد مثبت باشد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            TextbookTotalCopiesTextBox.Focus();
            return false;
        }

        int? classId = TextbookClassComboBox.SelectedValue as int?;
        textbook = new SaveTextbookDto(
            Title: TextbookTitleTextBox.Text.Trim(),
            Subject: string.IsNullOrWhiteSpace(TextbookSubjectTextBox.Text) ? null : TextbookSubjectTextBox.Text.Trim(),
            ClassId: classId,
            TotalCopies: totalCopies);

        return true;
    }

    private async Task LogAuditAsync(string action)
    {
        try
        {
            var userName = _currentUserService.CurrentUser?.Username ?? "Unknown";
            await _auditService.LogAsync(userName, action);
        }
        catch
        {
            // Audit logging should not break textbook operations
        }
    }
}

public class TextbookReturnEnabledConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is TextbookIssueStatus status && status == TextbookIssueStatus.Issued;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
