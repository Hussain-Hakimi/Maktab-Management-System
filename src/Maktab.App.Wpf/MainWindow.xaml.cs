using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Maktab.App.Wpf;

public partial class MainWindow : Window
{
    private readonly IClassSubjectService _classSubjectService;
    private readonly ObservableCollection<SchoolClass> _classes = [];
    private readonly ObservableCollection<Subject> _subjects = [];

    public MainWindow(IClassSubjectService classSubjectService)
    {
        _classSubjectService = classSubjectService;
        InitializeComponent();

        ClassesDataGrid.ItemsSource = _classes;
        SubjectsDataGrid.ItemsSource = _subjects;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
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
            SelectedClassTextBlock.Text = "Select a class from the left list";
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
            throw new InvalidOperationException($"{fieldName} must be a non-negative number.");
        }

        return value;
    }

    private async void AddClassButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var numberOfSubjects = ParseNonNegativeInt(NumberOfSubjectsTextBox.Text, "Number of subjects");
            await _classSubjectService.CreateClassAsync(GradeNameTextBox.Text, numberOfSubjects);

            await RefreshClassesAsync();
            ClearClassInputs();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Add Class Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void UpdateClassButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedClass();
        if (selected is null)
        {
            MessageBox.Show("Please select a class to update.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var numberOfSubjects = ParseNonNegativeInt(NumberOfSubjectsTextBox.Text, "Number of subjects");
            await _classSubjectService.UpdateClassAsync(selected.ClassId, GradeNameTextBox.Text, numberOfSubjects);

            await RefreshClassesAsync();
            ClearClassInputs();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Update Class Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void DeleteClassButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedClass();
        if (selected is null)
        {
            MessageBox.Show("Please select a class to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var decision = MessageBox.Show(
            $"Delete class '{selected.GradeName}' and its subjects?",
            "Confirm Delete",
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
            MessageBox.Show(ex.Message, "Delete Class Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
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
        SelectedClassTextBlock.Text = $"{selected.GradeName} (ID: {selected.ClassId})";

        await RefreshSubjectsAsync(selected.ClassId);
        ClearSubjectInputs();
    }

    private async void AddSubjectButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedClass = GetSelectedClass();
        if (selectedClass is null)
        {
            MessageBox.Show("Please select a class before adding subjects.", "No Class Selected", MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show(ex.Message, "Add Subject Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void UpdateSubjectButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedClass = GetSelectedClass();
        var selectedSubject = GetSelectedSubject();

        if (selectedClass is null || selectedSubject is null)
        {
            MessageBox.Show("Please select a subject to update.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show(ex.Message, "Update Subject Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void DeleteSubjectButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedClass = GetSelectedClass();
        var selectedSubject = GetSelectedSubject();

        if (selectedClass is null || selectedSubject is null)
        {
            MessageBox.Show("Please select a subject to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var decision = MessageBox.Show(
            $"Delete subject '{selectedSubject.SubjectName}'?",
            "Confirm Delete",
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
            MessageBox.Show(ex.Message, "Delete Subject Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
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