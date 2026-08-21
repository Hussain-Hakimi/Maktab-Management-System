using System.Windows;
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
        var password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ErrorTextBlock.Text = "نام کاربری و رمز عبور الزامی است.";
            return;
        }

        try
        {
            var user = await _userService.AuthenticateAsync(new LoginDto(username, password));
            if (user is null)
            {
                ErrorTextBlock.Text = "نام کاربری یا رمز عبور اشتباه است.";
                PasswordBox.Clear();
                return;
            }

            AuthenticatedUser = user;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ErrorTextBlock.Text = $"خطا: {ex.Message}";
        }
    }
}
