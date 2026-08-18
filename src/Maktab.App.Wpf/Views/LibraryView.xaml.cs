using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.App.Wpf.Views;

public sealed class LoanDisplayItem
{
    public int LoanId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public string IssueDate { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public string ReturnDateText { get; set; } = string.Empty;
    public bool IsReturned { get; set; }
    public bool IsOverdue { get; set; }
    public string StatusText => IsReturned ? "✔️ بازگردانده شد" : IsOverdue ? "⚠️ معوق" : "🕒 در امانت";
}

public sealed class StudentPickerItem
{
    public int StudentId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public partial class LibraryView : UserControl
{
    private readonly ILibraryService _libraryService;
    private readonly IStudentService _studentService;
    private readonly IClassSubjectService _classSubjectService;

    private readonly ObservableCollection<LibraryBook> _books = [];
    private readonly ObservableCollection<LoanDisplayItem> _loans = [];
    private List<LoanDisplayItem> _allLoans = [];
    private readonly List<SchoolClass> _classes = [];

    public LibraryView(
        ILibraryService libraryService,
        IStudentService studentService,
        IClassSubjectService classSubjectService)
    {
        _libraryService = libraryService;
        _studentService = studentService;
        _classSubjectService = classSubjectService;

        InitializeComponent();

        BooksDataGrid.ItemsSource = _books;
        LoansDataGrid.ItemsSource = _loans;
        Loaded += LibraryView_Loaded;
    }

    private async void LibraryView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadClassesAsync();
        await RefreshBooksAsync();
        await RefreshLoansAsync();
    }

    public async Task InitializeDataAsync()
    {
        await LoadClassesAsync();
        await RefreshBooksAsync();
        await RefreshLoansAsync();
    }

    private async Task LoadClassesAsync()
    {
        try
        {
            var classes = await _classSubjectService.GetClassesAsync();
            _classes.Clear();
            _classes.AddRange(classes);
            LoanClassComboBox.ItemsSource = _classes.ToList();
            if (_classes.Count > 0)
            {
                LoanClassComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دریافت لیست صنف‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RefreshBooksAsync()
    {
        try
        {
            var books = await _libraryService.GetAllBooksAsync();
            _books.Clear();
            foreach (var book in books)
            {
                _books.Add(book);
            }

            BooksCountTextBlock.Text = $" (تعداد: {_books.Count})";
            LoanBookComboBox.ItemsSource = _books.Where(b => b.AvailableCopies > 0).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری کتب:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RefreshLoansAsync()
    {
        try
        {
            var loans = await _libraryService.GetAllLoansAsync();
            _allLoans = loans.Select(l => new LoanDisplayItem
            {
                LoanId = l.LoanId,
                BookTitle = l.BookTitle,
                StudentName = l.StudentName,
                RollNumber = l.RollNumber,
                IssueDate = l.IssueDate.ToString("yyyy-MM-dd"),
                DueDate = l.DueDate.ToString("yyyy-MM-dd"),
                ReturnDateText = l.ReturnDate?.ToString("yyyy-MM-dd") ?? "—",
                IsReturned = l.IsReturned,
                IsOverdue = l.IsOverdue
            }).ToList();

            ApplyLoanFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری امانات:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyLoanFilters()
    {
        var filtered = _allLoans.AsEnumerable();

        if (OverdueOnlyCheckBox.IsChecked == true)
        {
            filtered = filtered.Where(l => l.IsOverdue);
        }
        else if (ActiveLoansOnlyCheckBox.IsChecked == true)
        {
            filtered = filtered.Where(l => !l.IsReturned);
        }

        _loans.Clear();
        foreach (var item in filtered)
        {
            _loans.Add(item);
        }
    }

    private LibraryBook? GetSelectedBook() => BooksDataGrid.SelectedItem as LibraryBook;

    private async void AddBookButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateBookInputs(out var title, out var author, out var category, out var copies))
        {
            return;
        }

        try
        {
            await _libraryService.AddBookAsync(title, author, category, copies);
            BookFormStatusTextBlock.Text = $"✅ کتاب «{title}» ثبت شد.";
            ClearBookInputs();
            await RefreshBooksAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ثبت ناموفق:\n{ex.Message}", "خطا در ثبت", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void UpdateBookButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedBook();
        if (selected is null)
        {
            MessageBox.Show("لطفاً یک کتاب را از جدول انتخاب کنید.", "انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!ValidateBookInputs(out var title, out var author, out var category, out var copies))
        {
            return;
        }

        try
        {
            await _libraryService.UpdateBookAsync(selected.BookId, title, author, category, copies);
            BookFormStatusTextBlock.Text = $"✅ کتاب «{title}» ویرایش گردید.";
            ClearBookInputs();
            await RefreshBooksAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ویرایش ناموفق:\n{ex.Message}", "خطا در ویرایش", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void DeleteBookButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedBook();
        if (selected is null)
        {
            MessageBox.Show("لطفاً یک کتاب را برای حذف انتخاب کنید.", "انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"آیا از حذف کتاب «{selected.Title}» اطمینان دارید؟",
            "تأیید حذف کتاب",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _libraryService.RemoveBookAsync(selected.BookId);
            BookFormStatusTextBlock.Text = $"✅ کتاب «{selected.Title}» حذف گردید.";
            ClearBookInputs();
            await RefreshBooksAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"حذف ناموفق:\n{ex.Message}", "خطا در حذف", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void IssueBookButton_Click(object sender, RoutedEventArgs e)
    {
        if (LoanBookComboBox.SelectedValue is not int bookId || bookId <= 0)
        {
            MessageBox.Show("لطفاً یک کتاب با نسخه موجود انتخاب کنید.", "کتاب انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (LoanStudentComboBox.SelectedValue is not int studentId || studentId <= 0)
        {
            MessageBox.Show("لطفاً یک شاگرد را انتخاب کنید.", "شاگرد انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!int.TryParse(LoanDaysTextBox.Text?.Trim(), out var loanDays) || loanDays <= 0 || loanDays > 365)
        {
            MessageBox.Show("مدت امانت باید عددی بین ۱ تا ۳۶۵ روز باشد.", "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Warning);
            LoanDaysTextBox.Focus();
            return;
        }

        try
        {
            await _libraryService.IssueBookAsync(bookId, studentId, loanDays);
            LoanStatusTextBlock.Text = $"✅ کتاب به شاگرد امانت داده شد (مدت: {loanDays} روز).";
            await RefreshBooksAsync();
            await RefreshLoansAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"امانت ناموفق:\n{ex.Message}", "خطا در امانت", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void ReturnBookButton_Click(object sender, RoutedEventArgs e)
    {
        if (LoansDataGrid.SelectedItem is not LoanDisplayItem selected)
        {
            MessageBox.Show("لطفاً یک امانت فعال را از جدول انتخاب کنید.", "انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (selected.IsReturned)
        {
            MessageBox.Show("این کتاب قبلاً بازگردانده شده است.", "قبلاً بازگشته", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await _libraryService.ReturnBookAsync(selected.LoanId);
            LoanStatusTextBlock.Text = $"✅ کتاب «{selected.BookTitle}» از «{selected.StudentName}» بازگردانده شد.";
            await RefreshBooksAsync();
            await RefreshLoansAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ثبت بازگشت ناموفق:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void LoanClassComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LoanClassComboBox.SelectedValue is not int classId || classId <= 0)
        {
            LoanStudentComboBox.ItemsSource = null;
            return;
        }

        try
        {
            var students = await _studentService.GetStudentsByClassAsync(classId);
            LoanStudentComboBox.ItemsSource = students
                .Select(s => new StudentPickerItem { StudentId = s.StudentId, DisplayName = $"{s.FirstName} {s.LastName} ({s.RollNumber})" })
                .ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دریافت شاگردان:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoanFilter_Changed(object sender, RoutedEventArgs e) => ApplyLoanFilters();

    private async void RefreshLoansButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshBooksAsync();
        await RefreshLoansAsync();
    }

    private void ClearBookFormButton_Click(object sender, RoutedEventArgs e)
    {
        ClearBookInputs();
        BookFormStatusTextBlock.Text = string.Empty;
    }

    private void BooksDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = GetSelectedBook();
        if (selected is null)
        {
            return;
        }

        BookTitleTextBox.Text = selected.Title;
        BookAuthorTextBox.Text = selected.Author;
        BookCategoryTextBox.Text = selected.Category ?? string.Empty;
        BookCopiesTextBox.Text = selected.TotalCopies.ToString();
    }

    private bool ValidateBookInputs(out string title, out string author, out string? category, out int copies)
    {
        title = BookTitleTextBox.Text?.Trim() ?? string.Empty;
        author = BookAuthorTextBox.Text?.Trim() ?? string.Empty;
        var categoryText = BookCategoryTextBox.Text?.Trim() ?? string.Empty;
        category = string.IsNullOrWhiteSpace(categoryText) ? null : categoryText;
        copies = 0;

        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show("لطفاً عنوان کتاب را وارد کنید.", "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Warning);
            BookTitleTextBox.Focus();
            return false;
        }

        if (!int.TryParse(BookCopiesTextBox.Text?.Trim(), out copies) || copies < 0)
        {
            MessageBox.Show("تعداد نسخه‌ها باید یک عدد غیرمنفی باشد.", "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Warning);
            BookCopiesTextBox.Focus();
            return false;
        }

        return true;
    }

    private void ClearBookInputs()
    {
        BookTitleTextBox.Clear();
        BookAuthorTextBox.Clear();
        BookCategoryTextBox.Clear();
        BookCopiesTextBox.Clear();
        BooksDataGrid.SelectedItem = null;
    }
}
