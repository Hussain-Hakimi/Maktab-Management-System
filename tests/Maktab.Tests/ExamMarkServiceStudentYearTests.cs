using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;
using Moq;

namespace Maktab.Tests;

public class ExamMarkServiceStudentYearTests
{
    private readonly Mock<IExamMarkRepository> _markRepoMock = new();
    private readonly Mock<IStudentRepository> _studentRepoMock = new();
    private readonly Mock<IClassSubjectRepository> _classRepoMock = new();

    private readonly ExamMarkService _service;

    public ExamMarkServiceStudentYearTests()
    {
        _service = new ExamMarkService(
            _markRepoMock.Object,
            _studentRepoMock.Object,
            _classRepoMock.Object);
    }

    [Fact]
    public async Task GetStudentMarksForYear_ReturnsAllSubjectsWithScores()
    {
        // Arrange
        var student = new Student
        {
            StudentId = 1,
            FirstName = "Ahmad",
            LastName = "Karimi",
            FatherName = "Mohammad",
            ClassId = 1,
            RollNumber = "101",
            RegistrationDate = DateTime.Now
        };
        var subjects = new List<Subject>
        {
            new() { SubjectId = 1, ClassId = 1, SubjectName = "ریاضی" },
            new() { SubjectId = 2, ClassId = 1, SubjectName = "فزیک" }
        };
        var marks = new List<ExamMark>
        {
            new() { StudentId = 1, SubjectId = 1, MidtermScore = 35m, FinalScore = 50m, AcademicYearId = 1 },
            new() { StudentId = 1, SubjectId = 2, MidtermScore = 30m, FinalScore = 40m, AcademicYearId = 1 }
        };

        _studentRepoMock.Setup(r => r.GetStudentByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);
        _classRepoMock.Setup(r => r.GetSubjectsByClassAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subjects);
        _markRepoMock.Setup(r => r.GetMarksByStudentAndYearAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(marks);

        // Act
        var result = await _service.GetStudentMarksForYearAsync(1, 1);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("ریاضی", result[0].SubjectName);
        Assert.Equal(85m, result[0].TotalScore);
        Assert.Equal("فزیک", result[1].SubjectName);
        Assert.Equal(70m, result[1].TotalScore);
    }

    [Fact]
    public async Task GetStudentMarksForYear_WhenStudentNotFound_ReturnsEmptyList()
    {
        _studentRepoMock.Setup(r => r.GetStudentByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);

        var result = await _service.GetStudentMarksForYearAsync(999, 1);

        Assert.Empty(result);
    }
}
