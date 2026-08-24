using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;

namespace Maktab.App.Wpf.Views;

public partial class LoginWindow : Window
{
    private readonly IUserService _userService;

    public UserDto? AuthenticatedUser { get; private set; }

    public LoginWindow(IUserService userService)
    {
        _userService = userService;
        InitializeComponent();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var username = UsernameTextBox.Text.Trim();
        var password = GetCurrentPassword();

        // FIRST-RUN RULE: empty credentials → auto-login as admin
        if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(password))
        {
            await AutoLoginAsAdminAsync();
            return;
        }

        try
        {
            var user = await _userService.AuthenticateAsync(new LoginDto(username, password));
            if (user is null || !user.IsActive)
            {
                StatusTextBlock.Text = "نام کاربری یا رمز عبور اشتباه است.";
                return;
            }

            AuthenticatedUser = user;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"خطا: {ex.Message}";
        }
    }

    private async Task AutoLoginAsAdminAsync()
    {
        try
        {
            // Find admin user (username "admin")
            var users = await _userService.GetAllUsersAsync();
            var admin = users.FirstOrDefault(u =>
                u.Role == Domain.Enums.UserRole.Admin &&
                u.Username.Equals("admin", StringComparison.OrdinalIgnoreCase));

            if (admin is null)
            {
                StatusTextBlock.Text = "حساب مدیر سیستم یافت نشد.";
                return;
            }

            AuthenticatedUser = admin;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"خطا: {ex.Message}";
        }
    }

    private void TogglePasswordVisibilityButton_Click(object sender, RoutedEventArgs e)
    {
        if (PasswordBox.Visibility == Visibility.Visible)
        {
            // Switch to plain text
            PasswordTextBox.Text = PasswordBox.Password;
            PasswordTextBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
        }
        else
        {
            // Switch to masked
            PasswordBox.Password = PasswordTextBox.Text;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordTextBox.Visibility = Visibility.Collapsed;
        }
    }

    private string GetCurrentPassword()
    {
        return PasswordBox.Visibility == Visibility.Visible
            ? PasswordBox.Password
            : PasswordTextBox.Text;
    }
}
