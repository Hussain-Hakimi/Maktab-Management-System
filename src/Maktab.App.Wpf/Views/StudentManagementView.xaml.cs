using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.App.Wpf.Views;

public sealed class StudentDisplayItem
{
    public int StudentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FatherName { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
    public string RegistrationDateFormatted => RegistrationDate.ToString("yyyy/MM/dd");
}

public partial class StudentManagementView : UserControl
{
    private readonly IStudentService _studentService;
    private readonly IClassSubjectService _classSubjectService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    private readonly ObservableCollection<StudentDisplayItem> _displayedStudents = [];
    private List<StudentDisplayItem> _allStudents = [];
    private readonly List<SchoolClass> _classes = [];

    public StudentManagementView(
        IStudentService studentService,
        IClassSubjectService classSubjectService,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _studentService = studentService;
        _classSubjectService = classSubjectService;
        _auditService = auditService;
        _currentUserService = currentUserService;

        InitializeComponent();

        StudentsDataGrid.ItemsSource = _displayedStudents;
        Loaded += StudentManagementView_Loaded;
    }

    private async void StudentManagementView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadClassesAsync();
        await RefreshStudentsListAsync();
    }

    public async Task InitializeDataAsync()
    {
        await LoadClassesAsync();
        await RefreshStudentsListAsync();
    }

    private async Task LoadClassesAsync()
    {
        try
        {
            var classes = await _classSubjectService.GetClassesAsync();
            _classes.Clear();
            _classes.AddRange(classes);

            var filterList = new List<SchoolClass>
            {
                new() { ClassId = 0, GradeName = "همه صنف‌ها (All Classes)", NumberOfSubjects = 0 }
            };
            filterList.AddRange(_classes);

            FilterClassComboBox.ItemsSource = filterList;
            FilterClassComboBox.SelectedIndex = 0;

            FormClassComboBox.ItemsSource = _classes.ToList();
            if (_classes.Count > 0)
            {
                FormClassComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دریافت لیست صنف‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RefreshStudentsListAsync()
    {
        try
        {
            var students = await _studentService.GetAllStudentsAsync();
            var classDict = _classes.ToDictionary(c => c.ClassId, c => c.GradeName);

            _allStudents = students.Select(s => new StudentDisplayItem
            {
                StudentId = s.StudentId,
                FirstName = s.FirstName,
                LastName = s.LastName,
                FatherName = s.FatherName,
                ClassId = s.ClassId,
                ClassName = classDict.TryGetValue(s.ClassId, out var name) ? name : $"صنف {s.ClassId}",
                RollNumber = s.RollNumber,
                RegistrationDate = s.RegistrationDate
            }).ToList();

            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری شاگردان:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allStudents.AsEnumerable();

        if (FilterClassComboBox.SelectedValue is int selectedClassId && selectedClassId > 0)
        {
            filtered = filtered.Where(s => s.ClassId == selectedClassId);
        }

        var search = SearchTextBox.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(s =>
                s.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.FatherName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.RollNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.StudentId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.ClassName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        _displayedStudents.Clear();
        foreach (var item in filtered)
        {
            _displayedStudents.Add(item);
        }

        CountTextBlock.Text = $" (تعداد: {_displayedStudents.Count})";
    }

    private StudentDisplayItem? GetSelectedStudent() => StudentsDataGrid.SelectedItem as StudentDisplayItem;

    private bool IsEditingMode => StudentsDataGrid.SelectedItem is not null;

    private async void AddStudentButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateInputs(out var firstName, out var lastName, out var fatherName, out var classId, out var rollNumber))
        {
            return;
        }

        try
        {
            await _studentService.RegisterStudentAsync(firstName, lastName, fatherName, classId, rollNumber);
            await LogAuditAsync($"ثبت شاگرد '{firstName} {lastName}'");
            FormStatusTextBlock.Text = $"✅ شاگرد «{firstName} {lastName}» با موفقیت ثبت شد.";
            ClearInputs();
            await RefreshStudentsListAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ثبت ناموفق:\n{ex.Message}", "خطا در ثبت", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void UpdateStudentButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedStudent();
        if (selected is null)
        {
            MessageBox.Show("لطفاً یک شاگرد را از جدول انتخاب کنید.", "انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!ValidateInputs(out var firstName, out var lastName, out var fatherName, out var classId, out var rollNumber))
        {
            return;
        }

        try
        {
            await _studentService.UpdateStudentAsync(selected.StudentId, firstName, lastName, fatherName, classId, rollNumber);
            await LogAuditAsync($"ویرایش شاگرد '{firstName} {lastName}'");
            FormStatusTextBlock.Text = $"✅ اطلاعات شاگرد «{firstName} {lastName}» ویرایش گردید.";
            ClearInputs();
            await RefreshStudentsListAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ویرایش ناموفق:\n{ex.Message}", "خطا در ویرایش", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void DeleteStudentButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedStudent();
        if (selected is null)
        {
            MessageBox.Show("لطفاً یک شاگرد را برای حذف انتخاب کنید.", "انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"آیا از حذف شاگرد «{selected.FirstName} {selected.LastName}» (شماره اساس: {selected.RollNumber}) اطمینان دارید؟\nتمام نمرات این شاگرد نیز حذف خواهند شد.",
            "تأیید حذف شاگرد",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _studentService.RemoveStudentAsync(selected.StudentId);
            await LogAuditAsync($"حذف شاگرد '{selected.FirstName} {selected.LastName}'");
            FormStatusTextBlock.Text = $"✅ شاگرد «{selected.FirstName} {selected.LastName}» حذف گردید.";
            ClearInputs();
            await RefreshStudentsListAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"حذف ناموفق:\n{ex.Message}", "خطا در حذف", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ClearFormButton_Click(object sender, RoutedEventArgs e)
    {
        ClearInputs();
        FormStatusTextBlock.Text = string.Empty;
    }

    private void StudentsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = GetSelectedStudent();
        if (selected is null)
        {
            return;
        }

        FirstNameTextBox.Text = selected.FirstName;
        LastNameTextBox.Text = selected.LastName;
        FatherNameTextBox.Text = selected.FatherName;
        FormClassComboBox.SelectedValue = selected.ClassId;
        RollNumberTextBox.Text = selected.RollNumber;
    }

    private void FilterClassComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadClassesAsync();
        await RefreshStudentsListAsync();
    }

    private async void FormClassComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Only auto-fill when NOT editing an existing student
        if (IsEditingMode)
            return;

        if (FormClassComboBox.SelectedValue is not int classId || classId <= 0)
            return;

        try
        {
            var next = await _studentService.GetNextRollNumberAsync(classId);
            RollNumberTextBox.Text = next.ToString();
        }
        catch (Exception ex)
        {
            // Ignore roll number auto-fill errors
        }
    }

    private bool ValidateInputs(out string firstName, out string lastName, out string fatherName, out int classId, out string rollNumber)
    {
        firstName = FirstNameTextBox.Text?.Trim() ?? string.Empty;
        lastName = LastNameTextBox.Text?.Trim() ?? string.Empty;
        fatherName = FatherNameTextBox.Text?.Trim() ?? string.Empty;
        rollNumber = RollNumberTextBox.Text?.Trim() ?? string.Empty;
        classId = FormClassComboBox.SelectedValue is int id ? id : 0;

        if (string.IsNullOrWhiteSpace(firstName))
        {
            MessageBox.Show("لطفاً نام شاگرد را وارد کنید.", "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Warning);
            FirstNameTextBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            MessageBox.Show("لطفاً تخلص شاگرد را وارد کنید.", "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Warning);
            LastNameTextBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(fatherName))
        {
            MessageBox.Show("لطفاً نام پدر شاگرد را وارد کنید.", "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Warning);
            FatherNameTextBox.Focus();
            return false;
        }

        if (classId <= 0)
        {
            MessageBox.Show("لطفاً صنف شاگرد را انتخاب کنید.", "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Warning);
            FormClassComboBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(rollNumber))
        {
            MessageBox.Show("لطفاً شماره اساس یا رول نمبر شاگرد را وارد کنید.", "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Warning);
            RollNumberTextBox.Focus();
            return false;
        }

        return true;
    }

    private void ClearInputs()
    {
        FirstNameTextBox.Clear();
        LastNameTextBox.Clear();
        FatherNameTextBox.Clear();
        RollNumberTextBox.Clear();
        StudentsDataGrid.SelectedItem = null;
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
            // Audit logging should not break the operation
        }
    }
}
