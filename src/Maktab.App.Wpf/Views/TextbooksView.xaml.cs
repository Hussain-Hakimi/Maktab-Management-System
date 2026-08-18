using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.App.Wpf.Views;

public sealed class TextbookIssueDisplayItem
{
    public int IssueId { get; set; }
    public string TextbookTitle { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public string IssueDate { get; set; } = string.Empty;
    public string ReturnDateText { get; set; } = string.Empty;
    public bool IsReturned { get; set; }
    public string StatusText => IsReturned ? "✔️ بازگردانده شد" : "🎒 نزد شاگرد";
}

public partial class TextbooksView : UserControl
{
    private readonly ITextbookService _textbookService;
    private readonly IStudentService _studentService;
    private readonly IClassSubjectService _classSubjectService;

    private readonly ObservableCollection<Textbook> _textbooks = [];
    private readonly ObservableCollection<TextbookIssueDisplayItem> _issues = [];
    private List<TextbookIssueDisplayItem> _allIssues = [];
    private readonly List<SchoolClass> _classes = [];

    public TextbooksView(
        ITextbookService textbookService,
        IStudentService studentService,
        IClassSubjectService classSubjectService)
    {
        _textbookService = textbookService;
        _studentService = studentService;
        _classSubjectService = classSubjectService;

        InitializeComponent();

        TextbooksDataGrid.ItemsSource = _textbooks;
        IssuesDataGrid.ItemsSource = _issues;
        Loaded += TextbooksView_Loaded;
    }

    private async void TextbooksView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadClassesAsync();
        await RefreshTextbooksAsync();
        await RefreshIssuesAsync();
    }

    public async Task InitializeDataAsync()
    {
        await LoadClassesAsync();
        await RefreshTextbooksAsync();
        await RefreshIssuesAsync();
    }

    private async Task LoadClassesAsync()
    {
        try
        {
            var classes = await _classSubjectService.GetClassesAsync();
            _classes.Clear();
            _classes.AddRange(classes);
            IssueClassComboBox.ItemsSource = _classes.ToList();
            if (_classes.Count > 0)
            {
                IssueClassComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دریافت لیست صنف‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RefreshTextbooksAsync()
    {
        try
        {
            var textbooks = await _textbookService.GetAllTextbooksAsync();
            _textbooks.Clear();
            foreach (var textbook in textbooks)
            {
                _textbooks.Add(textbook);
            }

            TextbooksCountTextBlock.Text = $" (تعداد: {_textbooks.Count})";
            IssueTextbookComboBox.ItemsSource = _textbooks.Where(t => t.AvailableCopies > 0).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری کتب درسی:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RefreshIssuesAsync()
    {
        try
        {
            var issues = await _textbookService.GetAllIssuesAsync();
            _allIssues = issues.Select(i => new TextbookIssueDisplayItem
            {
                IssueId = i.IssueId,
                TextbookTitle = i.TextbookTitle,
                StudentName = i.StudentName,
                RollNumber = i.RollNumber,
                IssueDate = i.IssueDate.ToString("yyyy-MM-dd"),
                ReturnDateText = i.ReturnDate?.ToString("yyyy-MM-dd") ?? "—",
                IsReturned = i.IsReturned
            }).ToList();

            ApplyIssueFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری توزیع‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyIssueFilters()
    {
        var filtered = _allIssues.AsEnumerable();
        if (ActiveIssuesOnlyCheckBox.IsChecked == true)
        {
            filtered = filtered.Where(i => !i.IsReturned);
        }

        _issues.Clear();
        foreach (var item in filtered)
        {
            _issues.Add(item);
        }
    }

    private Textbook? GetSelectedTextbook() => TextbooksDataGrid.SelectedItem as Textbook;

    private async void AddTextbookButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateTextbookInputs(out var title, out var subject, out var grade, out var copies))
        {
            return;
        }

        try
        {
            await _textbookService.AddTextbookAsync(title, subject, grade, copies);
            TextbookFormStatusTextBlock.Text = $"✅ کتاب درسی «{title}» ثبت شد.";
            ClearTextbookInputs();
            await RefreshTextbooksAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ثبت ناموفق:\n{ex.Message}", "خطا در ثبت", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void UpdateTextbookButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedTextbook();
        if (selected is null)
        {
            MessageBox.Show("لطفاً یک کتاب درسی را از جدول انتخاب کنید.", "انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!ValidateTextbookInputs(out var title, out var subject, out var grade, out var copies))
        {
            return;
        }

        try
        {
            await _textbookService.UpdateTextbookAsync(selected.TextbookId, title, subject, grade, copies);
            TextbookFormStatusTextBlock.Text = $"✅ کتاب درسی «{title}» ویرایش گردید.";
            ClearTextbookInputs();
            await RefreshTextbooksAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ویرایش ناموفق:\n{ex.Message}", "خطا در ویرایش", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void DeleteTextbookButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedTextbook();
        if (selected is null)
        {
            MessageBox.Show("لطفاً یک کتاب درسی را برای حذف انتخاب کنید.", "انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"آیا از حذف کتاب درسی «{selected.Title}» اطمینان دارید؟",
            "تأیید حذف",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _textbookService.RemoveTextbookAsync(selected.TextbookId);
            TextbookFormStatusTextBlock.Text = $"✅ کتاب درسی «{selected.Title}» حذف گردید.";
            ClearTextbookInputs();
            await RefreshTextbooksAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"حذف ناموفق:\n{ex.Message}", "خطا در حذف", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void IssueTextbookButton_Click(object sender, RoutedEventArgs e)
    {
        if (IssueTextbookComboBox.SelectedValue is not int textbookId || textbookId <= 0)
        {
            MessageBox.Show("لطفاً یک کتاب درسی با نسخه موجود انتخاب کنید.", "کتاب انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (IssueStudentComboBox.SelectedValue is not int studentId || studentId <= 0)
        {
            MessageBox.Show("لطفاً یک شاگرد را انتخاب کنید.", "شاگرد انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await _textbookService.IssueTextbookAsync(textbookId, studentId);
            IssueStatusTextBlock.Text = "✅ کتاب درسی به شاگرد توزیع شد.";
            await RefreshTextbooksAsync();
            await RefreshIssuesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"توزیع ناموفق:\n{ex.Message}", "خطا در توزیع", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void ReturnTextbookButton_Click(object sender, RoutedEventArgs e)
    {
        if (IssuesDataGrid.SelectedItem is not TextbookIssueDisplayItem selected)
        {
            MessageBox.Show("لطفاً یک رکورد توزیع را از جدول انتخاب کنید.", "انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (selected.IsReturned)
        {
            MessageBox.Show("این کتاب قبلاً بازگردانده شده است.", "قبلاً بازگشته", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await _textbookService.ReturnTextbookAsync(selected.IssueId);
            IssueStatusTextBlock.Text = $"✅ کتاب «{selected.TextbookTitle}» از «{selected.StudentName}» بازگردانده شد.";
            await RefreshTextbooksAsync();
            await RefreshIssuesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ثبت بازگشت ناموفق:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void IssueClassComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IssueClassComboBox.SelectedValue is not int classId || classId <= 0)
        {
            IssueStudentComboBox.ItemsSource = null;
            return;
        }

        try
        {
            var students = await _studentService.GetStudentsByClassAsync(classId);
            IssueStudentComboBox.ItemsSource = students
                .Select(s => new StudentPickerItem { StudentId = s.StudentId, DisplayName = $"{s.FirstName} {s.LastName} ({s.RollNumber})" })
                .ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دریافت شاگردان:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void IssueFilter_Changed(object sender, RoutedEventArgs e) => ApplyIssueFilters();

    private async void RefreshIssuesButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshTextbooksAsync();
        await RefreshIssuesAsync();
    }

    private void ClearTextbookFormButton_Click(object sender, RoutedEventArgs e)
    {
        ClearTextbookInputs();
        TextbookFormStatusTextBlock.Text = string.Empty;
    }

    private void TextbooksDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = GetSelectedTextbook();
        if (selected is null)
        {
            return;
        }

        TextbookTitleTextBox.Text = selected.Title;
        TextbookSubjectTextBox.Text = selected.SubjectName ?? string.Empty;
        TextbookGradeTextBox.Text = selected.GradeLevel ?? string.Empty;
        TextbookCopiesTextBox.Text = selected.TotalCopies.ToString();
    }

    private bool ValidateTextbookInputs(out string title, out string? subject, out string? grade, out int copies)
    {
        title = TextbookTitleTextBox.Text?.Trim() ?? string.Empty;
        var subjectText = TextbookSubjectTextBox.Text?.Trim() ?? string.Empty;
        subject = string.IsNullOrWhiteSpace(subjectText) ? null : subjectText;
        var gradeText = TextbookGradeTextBox.Text?.Trim() ?? string.Empty;
        grade = string.IsNullOrWhiteSpace(gradeText) ? null : gradeText;
        copies = 0;

        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show("لطفاً عنوان کتاب درسی را وارد کنید.", "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Warning);
            TextbookTitleTextBox.Focus();
            return false;
        }

        if (!int.TryParse(TextbookCopiesTextBox.Text?.Trim(), out copies) || copies < 0)
        {
            MessageBox.Show("تعداد نسخه‌ها باید یک عدد غیرمنفی باشد.", "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Warning);
            TextbookCopiesTextBox.Focus();
            return false;
        }

        return true;
    }

    private void ClearTextbookInputs()
    {
        TextbookTitleTextBox.Clear();
        TextbookSubjectTextBox.Clear();
        TextbookGradeTextBox.Clear();
        TextbookCopiesTextBox.Clear();
        TextbooksDataGrid.SelectedItem = null;
    }
}
