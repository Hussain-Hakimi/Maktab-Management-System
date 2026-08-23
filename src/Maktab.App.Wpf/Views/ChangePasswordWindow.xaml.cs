using System.Windows;
using Maktab.Application.Abstractions;

namespace Maktab.App.Wpf.Views;

public partial class ChangePasswordWindow : Window
{
    private readonly IUserService _userService;
    private readonly UserDto _currentUser;

    public ChangePasswordWindow(IUserService userService, UserDto currentUser)
    {
        _userService = userService;
        _currentUser = currentUser;
        InitializeComponent();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var oldPassword = OldPasswordBox.Password;
        var newPassword = NewPasswordBox.Password;
        var confirmPassword = ConfirmPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            ErrorTextBlock.Text = "همه فیلدها الزامی هستند.";
            return;
        }

        if (newPassword != confirmPassword)
        {
            ErrorTextBlock.Text = "رمز عبور جدید و تکرار آن یکسان نیستند.";
            return;
        }

        try
        {
            await _userService.ChangePasswordAsync(_currentUser.UserId, oldPassword, newPassword);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ErrorTextBlock.Text = ex.Message;
        }
    }
}
