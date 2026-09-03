using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Maktab.App.Wpf.Views;

public partial class ClassSubjectView : UserControl
{
    private readonly IClassSubjectService _classSubjectService;
    private readonly ObservableCollection<SchoolClass> _classes = [];
    private readonly ObservableCollection<Subject> _subjects = [];

    public ClassSubjectView(IClassSubjectService classSubjectService)
    {
        _classSubjectService = classSubjectService;
        InitializeComponent();

        ClassesDataGrid.ItemsSource = _classes;
        SubjectsDataGrid.ItemsSource = _subjects;
        Loaded += ClassSubjectView_Loaded;
    }

    private async void ClassSubjectView_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshClassesAsync();
    }

    public async Task InitializeDataAsync()
    {
        await RefreshClassesAsync();
    }

    private async Task RefreshClassesAsync()
    {
        var classes = await _classSubjectService.GetClassesAsync();

        _classes.Clear();
        foreach (var schoolClass in classes)
        {
            _classes.Add(schoolClass);
        }

        if (_classes.Count == 0)
        {
            SelectedClassTextBlock.Text = "لطفاً یک صنف را از جدول سمت راست انتخاب کنید";
            _subjects.Clear();
        }
    }

    private async Task RefreshSubjectsAsync(int classId)
    {
        var subjects = await _classSubjectService.GetSubjectsByClassAsync(classId);

        _subjects.Clear();
        foreach (var subject in subjects)
        {
            _subjects.Add(subject);
        }
    }

    private SchoolClass? GetSelectedClass() => ClassesDataGrid.SelectedItem as SchoolClass;

    private Subject? GetSelectedSubject() => SubjectsDataGrid.SelectedItem as Subject;

    private static int ParseNonNegativeInt(string rawValue, string fieldName)
    {
        if (!int.TryParse(rawValue, out var value) || value < 0)
        {
            throw new InvalidOperationException($"{fieldName} باید یک عدد مثبت باشد.");
        }

        return value;
    }

    private async void AddClassButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var numberOfSubjects = ParseNonNegativeInt(NumberOfSubjectsTextBox.Text, "تعداد مضامین");
            await _classSubjectService.CreateClassAsync(GradeNameTextBox.Text, numberOfSubjects);

            await RefreshClassesAsync();
            ClearClassInputs();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا در افزودن صنف", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void BulkCreateClassesButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new BulkCreateClassesDialog(_classSubjectService)
        {
            Owner = Window.GetWindow(this)
        };
        var result = dialog.ShowDialog();
        if (result == true)
        {
            await RefreshClassesAsync();
        }
    }

    private async void UpdateClassButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedClass();
        if (selected is null)
        {
            MessageBox.Show("لطفاً یک صنف را برای ویرایش انتخاب کنید.", "صنفی انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var numberOfSubjects = ParseNonNegativeInt(NumberOfSubjectsTextBox.Text, "تعداد مضامین");
            await _classSubjectService.UpdateClassAsync(selected.ClassId, GradeNameTextBox.Text, numberOfSubjects);

            await RefreshClassesAsync();
            ClearClassInputs();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا در ویرایش صنف", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void DeleteClassButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedClass();
        if (selected is null)
        {
            MessageBox.Show("لطفاً یک صنف را برای حذف انتخاب کنید.", "صنفی انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var decision = MessageBox.Show(
            $"آیا از حذف صنف «{selected.GradeName}» و تمام مضامین آن اطمینان دارید؟",
            "تأیید حذف صنف",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (decision != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _classSubjectService.DeleteClassAsync(selected.ClassId);
            await RefreshClassesAsync();
            ClearClassInputs();
            ClearSubjectInputs();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا در حذف صنف", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ClearClassButton_Click(object sender, RoutedEventArgs e)
    {
        ClearClassInputs();
    }

    private async void ClassesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = GetSelectedClass();
        if (selected is null)
        {
            return;
        }

        GradeNameTextBox.Text = selected.GradeName;
        NumberOfSubjectsTextBox.Text = selected.NumberOfSubjects.ToString();
        SelectedClassTextBlock.Text = $"{selected.GradeName} (شماره: {selected.ClassId})";

        await RefreshSubjectsAsync(selected.ClassId);
        ClearSubjectInputs();
    }

    private async void AddSubjectButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedClass = GetSelectedClass();
        if (selectedClass is null)
        {
            MessageBox.Show("لطفاً ابتدا یک صنف را انتخاب کنید.", "صنفی انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await _classSubjectService.CreateSubjectAsync(selectedClass.ClassId, SubjectNameTextBox.Text);
            await RefreshSubjectsAsync(selectedClass.ClassId);
            ClearSubjectInputs();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا در افزودن مضمون", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void UpdateSubjectButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedClass = GetSelectedClass();
        var selectedSubject = GetSelectedSubject();

        if (selectedClass is null || selectedSubject is null)
        {
            MessageBox.Show("لطفاً یک مضمون را برای ویرایش انتخاب کنید.", "مضمونی انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await _classSubjectService.UpdateSubjectAsync(selectedSubject.SubjectId, selectedClass.ClassId, SubjectNameTextBox.Text);
            await RefreshSubjectsAsync(selectedClass.ClassId);
            ClearSubjectInputs();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا در ویرایش مضمون", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void DeleteSubjectButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedClass = GetSelectedClass();
        var selectedSubject = GetSelectedSubject();

        if (selectedClass is null || selectedSubject is null)
        {
            MessageBox.Show("لطفاً یک مضمون را برای حذف انتخاب کنید.", "مضمونی انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var decision = MessageBox.Show(
            $"آیا از حذف مضمون «{selectedSubject.SubjectName}» اطمینان دارید؟",
            "تأیید حذف مضمون",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (decision != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _classSubjectService.DeleteSubjectAsync(selectedSubject.SubjectId);
            await RefreshSubjectsAsync(selectedClass.ClassId);
            ClearSubjectInputs();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا در حذف مضمون", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ClearSubjectButton_Click(object sender, RoutedEventArgs e)
    {
        ClearSubjectInputs();
    }

    private void SubjectsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = GetSelectedSubject();
        if (selected is null)
        {
            return;
        }

        SubjectNameTextBox.Text = selected.SubjectName;
    }

    private void ClearClassInputs()
    {
        GradeNameTextBox.Clear();
        NumberOfSubjectsTextBox.Clear();
        ClassesDataGrid.SelectedItem = null;
    }

    private void ClearSubjectInputs()
    {
        SubjectNameTextBox.Clear();
        SubjectsDataGrid.SelectedItem = null;
    }
}
