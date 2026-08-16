using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;

namespace Maktab.Tests;

public class StudentServiceTests
{
    private sealed class InMemoryStudentRepository : IStudentRepository
    {
        private readonly List<Student> _students = [];
        private int _nextId = 1;

        public Task<IReadOnlyList<Student>> GetStudentsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Student>>(_students.ToList());
        }

        public Task<IReadOnlyList<Student>> GetStudentsByClassAsync(int classId, CancellationToken cancellationToken = default)
        {
            var filtered = _students.Where(s => s.ClassId == classId).ToList();
            return Task.FromResult<IReadOnlyList<Student>>(filtered);
        }

        public Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken cancellationToken = default)
        {
            var student = _students.FirstOrDefault(s => s.StudentId == studentId);
            return Task.FromResult(student);
        }

        public Task<int> CreateStudentAsync(Student student, CancellationToken cancellationToken = default)
        {
            student.StudentId = _nextId++;
            _students.Add(student);
            return Task.FromResult(student.StudentId);
        }

        public Task UpdateStudentAsync(Student student, CancellationToken cancellationToken = default)
        {
            var idx = _students.FindIndex(s => s.StudentId == student.StudentId);
            if (idx >= 0)
            {
                _students[idx] = student;
            }
            return Task.CompletedTask;
        }

        public Task DeleteStudentAsync(int studentId, CancellationToken cancellationToken = default)
        {
            _students.RemoveAll(s => s.StudentId == studentId);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByRollNumberAsync(int classId, string rollNumber, CancellationToken cancellationToken = default)
        {
            var exists = _students.Any(s => s.ClassId == classId && s.RollNumber.Equals(rollNumber, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(exists);
        }
    }

    [Fact]
    public async Task RegisterStudent_WithValidData_ReturnsNewStudentId()
    {
        var repo = new InMemoryStudentRepository();
        var service = new StudentService(repo);

        var id = await service.RegisterStudentAsync("Ahmad", "Karimi", "Mohammad", 1, "101");

        Assert.True(id > 0);
        var students = await service.GetStudentsByClassAsync(1);
        Assert.Single(students);
        Assert.Equal("Ahmad", students[0].FirstName);
        Assert.Equal("101", students[0].RollNumber);
    }

    [Fact]
    public async Task RegisterStudent_WithDuplicateRollNumberInSameClass_ThrowsInvalidOperationException()
    {
        var repo = new InMemoryStudentRepository();
        var service = new StudentService(repo);

        await service.RegisterStudentAsync("Ahmad", "Karimi", "Mohammad", 1, "101");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await service.RegisterStudentAsync("Mahmood", "Rahimi", "Ali", 1, "101");
        });
    }

    [Fact]
    public async Task RegisterStudent_WithSameRollNumberInDifferentClass_Succeeds()
    {
        var repo = new InMemoryStudentRepository();
        var service = new StudentService(repo);

        var id1 = await service.RegisterStudentAsync("Ahmad", "Karimi", "Mohammad", 1, "101");
        var id2 = await service.RegisterStudentAsync("Mahmood", "Rahimi", "Ali", 2, "101");

        Assert.True(id1 > 0);
        Assert.True(id2 > 0);
        Assert.NotEqual(id1, id2);
    }

    [Theory]
    [InlineData("", "Karimi", "Mohammad", 1, "101")]
    [InlineData("Ahmad", "", "Mohammad", 1, "101")]
    [InlineData("Ahmad", "Karimi", "", 1, "101")]
    [InlineData("Ahmad", "Karimi", "Mohammad", 1, "")]
    [InlineData("Ahmad", "Karimi", "Mohammad", 0, "101")]
    [InlineData("Ahmad", "Karimi", "Mohammad", -1, "101")]
    public async Task RegisterStudent_WithInvalidData_ThrowsArgumentException(string fn, string ln, string father, int classId, string roll)
    {
        var repo = new InMemoryStudentRepository();
        var service = new StudentService(repo);

        await Assert.ThrowsAnyAsync<ArgumentException>(async () =>
        {
            await service.RegisterStudentAsync(fn, ln, father, classId, roll);
        });
    }

    [Fact]
    public async Task UpdateStudent_WhenValid_UpdatesCorrectly()
    {
        var repo = new InMemoryStudentRepository();
        var service = new StudentService(repo);

        var id = await service.RegisterStudentAsync("Ahmad", "Karimi", "Mohammad", 1, "101");
        await service.UpdateStudentAsync(id, "Ahmad Zia", "Karimi", "Mohammad", 1, "101");

        var student = await service.GetStudentByIdAsync(id);
        Assert.NotNull(student);
        Assert.Equal("Ahmad Zia", student.FirstName);
    }

    [Fact]
    public async Task RemoveStudent_RemovesFromRepository()
    {
        var repo = new InMemoryStudentRepository();
        var service = new StudentService(repo);

        var id = await service.RegisterStudentAsync("Ahmad", "Karimi", "Mohammad", 1, "101");
        await service.RemoveStudentAsync(id);

        var students = await service.GetAllStudentsAsync();
        Assert.Empty(students);
    }
}
