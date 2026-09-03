using System.Windows;
using Maktab.Application.Abstractions;
using Maktab.Domain.Enums;

namespace Maktab.App.Wpf.Views;

public partial class FirstRunAdminSetupWindow : Window
{
    private readonly IUserService _userService;

    public UserDto? CreatedAdmin { get; private set; }

    public FirstRunAdminSetupWindow(IUserService userService)
    {
        _userService = userService;
        InitializeComponent();
        FullNameTextBox.Focus();
    }

    private async void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        var fullName = FullNameTextBox.Text.Trim();
        var username = UsernameTextBox.Text.Trim();
        var password = PasswordBox.Password;
        var confirmation = ConfirmPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username))
        {
            StatusTextBlock.Text = "نام کامل و نام کاربری الزامی است.";
            return;
        }

        if (password.Length < 8)
        {
            StatusTextBlock.Text = "رمز عبور باید حداقل ۸ کاراکتر باشد.";
            return;
        }

        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            StatusTextBlock.Text = "رمزهای عبور یکسان نیستند.";
            return;
        }

        try
        {
            CreateButton.IsEnabled = false;
            StatusTextBlock.Text = "در حال ایجاد حساب مدیر...";

            var userId = await _userService.CreateUserAsync(
                new SaveUserDto(username, password, fullName, UserRole.Admin, true));

            CreatedAdmin = new UserDto
            {
                UserId = userId,
                Username = username,
                FullName = fullName,
                Role = UserRole.Admin,
                IsActive = true
            };

            MessageBox.Show(
                "حساب مدیر با موفقیت ایجاد شد. اکنون با همین حساب وارد شوید.",
                "راه‌اندازی موفق",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"ایجاد حساب با خطا مواجه شد: {ex.Message}";
            CreateButton.IsEnabled = true;
        }
    }
}
