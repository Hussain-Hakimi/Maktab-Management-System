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
