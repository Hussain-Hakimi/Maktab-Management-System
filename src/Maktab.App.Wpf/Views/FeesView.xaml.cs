using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.App.Wpf.Views;

public partial class FeesView : UserControl
{
    private readonly IFeeService _feeService;
    private readonly IStudentService _studentService;
    private readonly IAcademicYearService _academicYearService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    private readonly ObservableCollection<FeeDto> _fees = [];
    private readonly ObservableCollection<FeePaymentDto> _payments = [];
    private readonly List<Student> _students = [];

    public FeesView(
        IFeeService feeService,
        IStudentService studentService,
        IAcademicYearService academicYearService,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _feeService = feeService;
        _studentService = studentService;
        _academicYearService = academicYearService;
        _auditService = auditService;
        _currentUserService = currentUserService;

        InitializeComponent();

        FeesDataGrid.ItemsSource = _fees;
        PaymentsDataGrid.ItemsSource = _payments;
        Loaded += FeesView_Loaded;
    }

    private async void FeesView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadStudentsAsync();
        await LoadFeesAsync();
        await LoadPaymentsAsync();
    }

    public async Task InitializeDataAsync()
    {
        await LoadStudentsAsync();
        await LoadFeesAsync();
        await LoadPaymentsAsync();
    }

    private async Task LoadStudentsAsync()
    {
        try
        {
            var students = await _studentService.GetAllStudentsAsync();
            _students.Clear();
            _students.AddRange(students);

            FeeStudentComboBox.ItemsSource = _students.Select(s => new StudentComboItem
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

    private async Task LoadFeesAsync()
    {
        try
        {
            var fees = await _feeService.GetFeesAsync();
            _fees.Clear();
            foreach (var fee in fees)
            {
                _fees.Add(fee);
            }

            PaymentFeeComboBox.ItemsSource = _fees.Select(f => new FeeComboItem
            {
                FeeId = f.FeeId,
                FeeDisplay = $"{f.FeeType} - {f.StudentName} (باقی: {f.Outstanding:N0})"
            }).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دریافت فیس‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadPaymentsAsync()
    {
        try
        {
            var payments = await _feeService.GetPaymentsAsync();
            _payments.Clear();
            foreach (var payment in payments)
            {
                _payments.Add(payment);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دریافت پرداخت‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void AddFeeButton_Click(object sender, RoutedEventArgs e)
    {
        if (FeeStudentComboBox.SelectedValue is not int studentId || studentId <= 0)
        {
            MessageBox.Show("لطفاً شاگرد را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(FeeTypeTextBox.Text))
        {
            MessageBox.Show("نوع فیس الزامی است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            FeeTypeTextBox.Focus();
            return;
        }

        if (!decimal.TryParse(FeeAmountTextBox.Text, out var amount) || amount <= 0m)
        {
            MessageBox.Show("مبلغ فیس باید یک عدد مثبت باشد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            FeeAmountTextBox.Focus();
            return;
        }

        if (FeeDueDatePicker.SelectedDate is not DateTime dueDate)
        {
            MessageBox.Show("تاریخ سررسید را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            FeeDueDatePicker.Focus();
            return;
        }

        try
        {
            var activeYear = await _academicYearService.GetActiveAcademicYearAsync();
            if (activeYear is null)
            {
                MessageBox.Show("سال تعلیمی فعال تعیین نشده است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await _feeService.AddFeeAsync(new SaveFeeDto(
                StudentId: studentId,
                FeeType: FeeTypeTextBox.Text.Trim(),
                Amount: amount,
                DueDate: dueDate,
                AcademicYearId: activeYear.AcademicYearId));

            await LogAuditAsync($"ثبت فیس برای شاگرد آیدی {studentId}");
            await LoadFeesAsync();
            ClearFeeForm();
            MessageBox.Show("فیس با موفقیت ثبت شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void DeleteFeeButton_Click(object sender, RoutedEventArgs e)
    {
        if (FeesDataGrid.SelectedItem is not FeeDto selected)
        {
            MessageBox.Show("لطفاً یک فیس را انتخاب کنید.", "انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show($"آیا از حذف فیس «{selected.FeeType}» برای {selected.StudentName} اطمینان دارید؟", "تأیید حذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _feeService.DeleteFeeAsync(selected.FeeId);
            await LogAuditAsync($"حذف فیس شماره {selected.FeeId}");
            await LoadFeesAsync();
            await LoadPaymentsAsync();
            MessageBox.Show("فیس حذف شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void RecordPaymentButton_Click(object sender, RoutedEventArgs e)
    {
        if (PaymentFeeComboBox.SelectedValue is not int feeId || feeId <= 0)
        {
            MessageBox.Show("لطفاً فیس را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(PaymentAmountTextBox.Text, out var amount) || amount <= 0m)
        {
            MessageBox.Show("مبلغ پرداخت باید یک عدد مثبت باشد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            PaymentAmountTextBox.Focus();
            return;
        }

        if (PaymentDatePicker.SelectedDate is not DateTime paymentDate)
        {
            MessageBox.Show("تاریخ پرداخت را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            PaymentDatePicker.Focus();
            return;
        }

        try
        {
            await _feeService.RecordPaymentAsync(new RecordPaymentDto(feeId, amount, paymentDate));
            await LogAuditAsync($"ثبت پرداخت برای فیس شماره {feeId}");
            await LoadFeesAsync();
            await LoadPaymentsAsync();
            ClearPaymentForm();
            MessageBox.Show("پرداخت با موفقیت ثبت شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ClearFeeForm()
    {
        FeeStudentComboBox.SelectedIndex = -1;
        FeeTypeTextBox.Clear();
        FeeAmountTextBox.Clear();
        FeeDueDatePicker.SelectedDate = null;
    }

    private void ClearPaymentForm()
    {
        PaymentFeeComboBox.SelectedIndex = -1;
        PaymentAmountTextBox.Clear();
        PaymentDatePicker.SelectedDate = null;
    }

    private async void RefreshFeesButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadFeesAsync();
    }

    private async void RefreshPaymentsButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadPaymentsAsync();
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
            // Audit logging should not break fee operations
        }
    }
}

public sealed class FeeComboItem
{
    public int FeeId { get; set; }
    public string FeeDisplay { get; set; } = string.Empty;
}
