using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;
using Maktab.Domain.Enums;

namespace Maktab.App.Wpf.Views;

public partial class UserManagementView : UserControl
{
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ObservableCollection<UserDto> _users = [];

    public UserManagementView(
        IUserService userService,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _userService = userService;
        _auditService = auditService;
        _currentUserService = currentUserService;

        InitializeComponent();

        RoleComboBox.ItemsSource = Enum.GetValues<UserRole>();
        UsersDataGrid.ItemsSource = _users;
        Loaded += UserManagementView_Loaded;
    }

    private async void UserManagementView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            var users = await _userService.GetAllUsersAsync();
            _users.Clear();
            foreach (var user in users)
                _users.Add(user);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دریافت کاربران:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void AddUserButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateInputs(out var userDto, isUpdate: false)) return;

        try
        {
            await _userService.CreateUserAsync(userDto);
            await LogAuditAsync($"افزودن کاربر '{userDto.Username}'");
            await LoadUsersAsync();
            ClearForm();
            MessageBox.Show("کاربر جدید با موفقیت اضافه شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void UpdateUserButton_Click(object sender, RoutedEventArgs e)
    {
        if (UsersDataGrid.SelectedItem is not UserDto selected)
        {
            MessageBox.Show("لطفاً یک کاربر را انتخاب کنید.", "انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!ValidateInputs(out var userDto, isUpdate: true)) return;

        try
        {
            await _userService.UpdateUserAsync(selected.UserId, userDto);
            await LogAuditAsync($"ویرایش کاربر '{userDto.Username}'");
            await LoadUsersAsync();
            ClearForm();
            MessageBox.Show("اطلاعات کاربر ویرایش شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void DeleteUserButton_Click(object sender, RoutedEventArgs e)
    {
        if (UsersDataGrid.SelectedItem is not UserDto selected)
        {
            MessageBox.Show("لطفاً یک کاربر را انتخاب کنید.", "انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show($"آیا از حذف کاربر «{selected.Username}» اطمینان دارید؟", "تأیید حذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _userService.DeleteUserAsync(selected.UserId);
            await LogAuditAsync($"حذف کاربر '{selected.Username}'");
            await LoadUsersAsync();
            ClearForm();
            MessageBox.Show("کاربر حذف شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ClearFormButton_Click(object sender, RoutedEventArgs e)
    {
        ClearForm();
    }

    private void UsersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UsersDataGrid.SelectedItem is UserDto selected)
        {
            UsernameTextBox.Text = selected.Username;
            FullNameTextBox.Text = selected.FullName;
            RoleComboBox.SelectedItem = selected.Role;
            IsActiveCheckBox.IsChecked = selected.IsActive;
            PasswordBox.Clear(); // do not show existing password
        }
    }

    private void ClearForm()
    {
        UsernameTextBox.Clear();
        PasswordBox.Clear();
        FullNameTextBox.Clear();
        RoleComboBox.SelectedIndex = -1;
        IsActiveCheckBox.IsChecked = true;
        UsersDataGrid.SelectedItem = null;
    }

    private bool ValidateInputs(out SaveUserDto userDto, bool isUpdate)
    {
        userDto = null!;

        if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
        {
            MessageBox.Show("نام کاربری الزامی است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            UsernameTextBox.Focus();
            return false;
        }

        if (!isUpdate && string.IsNullOrWhiteSpace(PasswordBox.Password))
        {
            MessageBox.Show("رمز عبور الزامی است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            PasswordBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
        {
            MessageBox.Show("نام کامل الزامی است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            FullNameTextBox.Focus();
            return false;
        }

        if (RoleComboBox.SelectedItem is not UserRole role)
        {
            MessageBox.Show("نقش را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            RoleComboBox.Focus();
            return false;
        }

        userDto = new SaveUserDto(
            Username: UsernameTextBox.Text.Trim(),
            Password: PasswordBox.Password,
            FullName: FullNameTextBox.Text.Trim(),
            Role: role,
            IsActive: IsActiveCheckBox.IsChecked ?? true);

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
            // Audit logging should not break user management
        }
    }
}
