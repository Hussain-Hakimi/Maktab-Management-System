using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Maktab.Application.Abstractions;
using Maktab.Domain.Enums;

namespace Maktab.App.Wpf.Views;

public partial class LibraryView : UserControl
{
    private readonly IBookService _bookService;
    private readonly IClassSubjectService _classSubjectService;
    private readonly IStudentService _studentService;

    private readonly ObservableCollection<BookDto> _books = [];
    private readonly ObservableCollection<BookIssueDto> _issues = [];
    private readonly List<Student> _students = [];
    private bool _showOverdueOnly;

    public LibraryView(
        IBookService bookService,
        IClassSubjectService classSubjectService,
        IStudentService studentService)
    {
        _bookService = bookService;
        _classSubjectService = classSubjectService;
        _studentService = studentService;

        InitializeComponent();

        BooksDataGrid.ItemsSource = _books;
        IssuesDataGrid.ItemsSource = _issues;
        Loaded += LibraryView_Loaded;
    }

    private async void LibraryView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadBooksAsync();
        await LoadStudentsAsync();
        await LoadIssuesAsync();
    }

    public async Task InitializeDataAsync()
    {
        await LoadBooksAsync();
        await LoadStudentsAsync();
        await LoadIssuesAsync();
    }

    private async Task LoadBooksAsync()
    {
        try
        {
            var books = await _bookService.GetBooksAsync();
            _books.Clear();
            foreach (var book in books)
            {
                _books.Add(book);
            }

            IssueBookComboBox.ItemsSource = _books.ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دریافت کتاب‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
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

    private async Task LoadIssuesAsync()
    {
        try
        {
            IReadOnlyList<BookIssueDto> issues;
            if (_showOverdueOnly)
                issues = await _bookService.GetOverdueIssuesAsync();
            else
                issues = await _bookService.GetIssuesAsync();

            _issues.Clear();
            foreach (var issue in issues.Where(i => i.Status == BookIssueStatus.Issued))
            {
                _issues.Add(issue);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دریافت امانت‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private BookDto? GetSelectedBook() => BooksDataGrid.SelectedItem as BookDto;

    private void BooksDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = GetSelectedBook();
        if (selected is null) return;

        BookTitleTextBox.Text = selected.Title;
        BookAuthorTextBox.Text = selected.Author;
        BookISBNTextBox.Text = selected.ISBN ?? string.Empty;
        BookCategoryTextBox.Text = selected.Category ?? string.Empty;
        BookTotalCopiesTextBox.Text = selected.TotalCopies.ToString();
    }

    private void ClearBookForm()
    {
        BookTitleTextBox.Clear();
        BookAuthorTextBox.Clear();
        BookISBNTextBox.Clear();
        BookCategoryTextBox.Clear();
        BookTotalCopiesTextBox.Clear();
        BooksDataGrid.SelectedItem = null;
    }

    private async void AddBookButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateBookInput(out var bookDto)) return;

        try
        {
            await _bookService.AddBookAsync(bookDto);
            await LoadBooksAsync();
            ClearBookForm();
            MessageBox.Show("کتاب با موفقیت اضافه شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void UpdateBookButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedBook();
        if (selected is null)
        {
            MessageBox.Show("لطفاً یک کتاب را انتخاب کنید.", "انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!ValidateBookInput(out var bookDto)) return;

        try
        {
            await _bookService.UpdateBookAsync(selected.BookId, bookDto);
            await LoadBooksAsync();
            ClearBookForm();
            MessageBox.Show("کتاب ویرایش شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void DeleteBookButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedBook();
        if (selected is null)
        {
            MessageBox.Show("لطفاً یک کتاب را انتخاب کنید.", "انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show($"آیا از حذف کتاب «{selected.Title}» اطمینان دارید؟", "تأیید حذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _bookService.DeleteBookAsync(selected.BookId);
            await LoadBooksAsync();
            ClearBookForm();
            MessageBox.Show("کتاب حذف شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ClearBookFormButton_Click(object sender, RoutedEventArgs e)
    {
        ClearBookForm();
    }

    private async void IssueBookButton_Click(object sender, RoutedEventArgs e)
    {
        if (IssueBookComboBox.SelectedValue is not int bookId || bookId <= 0)
        {
            MessageBox.Show("لطفاً کتاب را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (IssueStudentComboBox.SelectedValue is not int studentId || studentId <= 0)
        {
            MessageBox.Show("لطفاً شاگرد را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (IssueDueDatePicker.SelectedDate is not DateTime dueDate)
        {
            MessageBox.Show("لطفاً تاریخ برگشت را تعیین کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _bookService.IssueBookAsync(new IssueBookDto(bookId, studentId, dueDate));
            await LoadBooksAsync();
            await LoadIssuesAsync();
            MessageBox.Show("کتاب با موفقیت امانت داده شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void ReturnBookButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int issueId)
        {
            try
            {
                await _bookService.ReturnBookAsync(new ReturnBookDto(issueId));
                await LoadBooksAsync();
                await LoadIssuesAsync();
                MessageBox.Show("کتاب با موفقیت بازگشت داده شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void RefreshIssuesButton_Click(object sender, RoutedEventArgs e)
    {
        _showOverdueOnly = false;
        await LoadIssuesAsync();
    }

    private async void ShowOverdueButton_Click(object sender, RoutedEventArgs e)
    {
        _showOverdueOnly = true;
        await LoadIssuesAsync();
    }

    private bool ValidateBookInput(out SaveBookDto book)
    {
        book = null!;

        if (string.IsNullOrWhiteSpace(BookTitleTextBox.Text))
        {
            MessageBox.Show("عنوان کتاب الزامی است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            BookTitleTextBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(BookAuthorTextBox.Text))
        {
            MessageBox.Show("نویسنده کتاب الزامی است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            BookAuthorTextBox.Focus();
            return false;
        }

        if (!int.TryParse(BookTotalCopiesTextBox.Text, out var totalCopies) || totalCopies <= 0)
        {
            MessageBox.Show("تعداد کل نسخه‌ها باید یک عدد مثبت باشد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            BookTotalCopiesTextBox.Focus();
            return false;
        }

        book = new SaveBookDto(
            Title: BookTitleTextBox.Text.Trim(),
            Author: BookAuthorTextBox.Text.Trim(),
            ISBN: string.IsNullOrWhiteSpace(BookISBNTextBox.Text) ? null : BookISBNTextBox.Text.Trim(),
            Category: string.IsNullOrWhiteSpace(BookCategoryTextBox.Text) ? null : BookCategoryTextBox.Text.Trim(),
            TotalCopies: totalCopies);

        return true;
    }
}

public class BookReturnEnabledConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is BookIssueStatus status && status == BookIssueStatus.Issued;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
