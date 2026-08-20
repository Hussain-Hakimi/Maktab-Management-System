using System.Windows.Controls;

namespace Maktab.App.Wpf.Services;

public sealed class NavigationService
{
    private readonly ContentControl _contentArea;

    public NavigationService(ContentControl contentArea)
    {
        _contentArea = contentArea;
    }

    public void Navigate(UserControl page)
    {
        _contentArea.Content = page;
    }
}
