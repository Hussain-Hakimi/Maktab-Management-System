using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;

namespace Maktab.App.Wpf.Views;

public partial class LoginWindow : Window
{
    private readonly IUserService _userService;
    private readonly IAppLogger _logger;

    public UserDto? AuthenticatedUser { get; private set; }

    public LoginWindow(IUserService userService, IAppLogger logger)
    {
        _userService = userService;
        _logger = logger;
        InitializeComponent();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var username = UsernameTextBox.Text.Trim();
        var password = GetCurrentPassword();

        if (string.IsNullOrWhiteSpace(username))
        {
            StatusTextBlock.Text = "لطفاً نام کاربری را وارد کنید.";
            UsernameTextBox.Focus();
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            StatusTextBlock.Text = "لطفاً رمز عبور را وارد کنید.";
            PasswordBox.Focus();
            return;
        }

        try
        {
            LoginButton.IsEnabled = false;
            StatusTextBlock.Text = "در حال بررسی اطلاعات ورود...";
            var user = await _userService.AuthenticateAsync(new LoginDto(username, password));
            if (user is null || !user.IsActive)
            {
                StatusTextBlock.Text = "نام کاربری یا رمز عبور اشتباه است.";
                return;
            }

            AuthenticatedUser = user;
            MessageBox.Show("ورود با موفقیت انجام شد.", "ورود موفق", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            _logger.LogError("Login failed because of an unexpected error.", ex);
            StatusTextBlock.Text = "در هنگام ورود خطایی رخ داد. لطفاً دوباره تلاش کنید.";
        }
        finally
        {
            LoginButton.IsEnabled = true;
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
