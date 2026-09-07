using Maktab.Application.Abstractions;
using System.Windows;

namespace Maktab.App.Wpf.Views;

public partial class BulkCreateClassesDialog : Window
{
    private readonly IClassSubjectService _classSubjectService;

    // Suffix table: checkbox name -> suffix text
    private static readonly string[] Suffixes =
    [
        "اول", "دوم", "سوم", "چهارم", "پنجم", "ششم",
        "هفتم", "هشتم", "نهم", "دهم", "یازدهم", "دوازدهم"
    ];

    public BulkCreateClassesDialog(IClassSubjectService classSubjectService)
    {
        _classSubjectService = classSubjectService;
        InitializeComponent();
    }

    private async void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        var prefix = PrefixTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(prefix))
        {
            MessageBox.Show("لطفاً پیشوند نام صنف را وارد کنید.", "ورودی ناقص", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(NumberOfSubjectsTextBox.Text.Trim(), out var numberOfSubjects) || numberOfSubjects < 1 || numberOfSubjects > 30)
        {
            MessageBox.Show("تعداد مضامین باید یک عدد بین ۱ و ۳۰ باشد.", "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Gather checked suffixes
        var checkBoxes = new[]
        {
            CbAval, CbDovom, CbSevom, CbChaharom, CbPanjom, CbSheshom,
            CbHaftom, CbHashtom, CbNahom, CbDahom, CbYazdhom, CbDavazdhom
        };

        var selectedSuffixes = checkBoxes
            .Select((cb, i) => (cb, i))
            .Where(x => x.cb.IsChecked == true)
            .Select(x => Suffixes[x.i])
            .ToList();

        if (selectedSuffixes.Count == 0)
        {
            MessageBox.Show("لطفاً حداقل یک صنف را انتخاب کنید.", "انتخاب ناقص", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        CreateButton.IsEnabled = false;
        StatusTextBlock.Text = "در حال ایجاد صنوف...";
        int success = 0;
        int failed = 0;
        var errors = new System.Text.StringBuilder();

        foreach (var suffix in selectedSuffixes)
        {
            var className = $"{prefix} {suffix}";
            try
            {
                await _classSubjectService.CreateClassAsync(className, numberOfSubjects);
                success++;
            }
            catch (Exception ex)
            {
                failed++;
                errors.AppendLine($"❌ {className}: {ex.Message}");
            }
        }

        var msg = $"✅ {success} صنف با موفقیت ایجاد شد.";
        if (failed > 0) msg += $"\n❌ {failed} صنف ایجاد نشد:\n{errors}";
        StatusTextBlock.Text = msg;
        StatusTextBlock.Foreground = failed > 0
            ? System.Windows.Media.Brushes.OrangeRed
            : System.Windows.Media.Brushes.Green;

        CreateButton.IsEnabled = true;

        if (success > 0)
        {
            // Brief pause so user can read the result, then close
            await System.Threading.Tasks.Task.Delay(1500);
            DialogResult = true;
            Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
