using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Tests;

public class ExamMarkServiceTests
{
    private sealed class InMemoryExamMarkRepository : IExamMarkRepository
    {
        private readonly List<ExamMark> _marks = [];

        public Task<IReadOnlyList<ExamMark>> GetMarksByClassAndSubjectAsync(int classId, int subjectId, CancellationToken cancellationToken = default)
        {
            var result = _marks.Where(m => m.SubjectId == subjectId).ToList();
            return Task.FromResult<IReadOnlyList<ExamMark>>(result);
        }

        public Task<IReadOnlyList<ExamMark>> GetMarksByStudentAsync(int studentId, CancellationToken cancellationToken = default)
        {
            var result = _marks.Where(m => m.StudentId == studentId).ToList();
            return Task.FromResult<IReadOnlyList<ExamMark>>(result);
        }

        public Task<IReadOnlyList<ExamMark>> GetMarksByClassAsync(int classId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ExamMark>>(_marks.ToList());
        }

        public Task SaveOrUpdateMarkAsync(ExamMark mark, CancellationToken cancellationToken = default)
        {
            return SaveOrUpdateMarksBatchAsync([mark], cancellationToken);
        }

        public Task SaveOrUpdateMarksBatchAsync(IEnumerable<ExamMark> marks, CancellationToken cancellationToken = default)
        {
            foreach (var mark in marks)
            {
                var existing = _marks.FirstOrDefault(m => m.StudentId == mark.StudentId && m.SubjectId == mark.SubjectId);
                if (existing != null)
                {
                    existing.MidtermScore = mark.MidtermScore;
                    existing.FinalScore = mark.FinalScore;
                }
                else
                {
                    _marks.Add(new ExamMark
                    {
                        StudentId = mark.StudentId,
                        SubjectId = mark.SubjectId,
                        MidtermScore = mark.MidtermScore,
                        FinalScore = mark.FinalScore
                    });
                }
            }
            return Task.CompletedTask;
        }
    }

    private sealed class MockStudentRepository(List<Student> students) : IStudentRepository
    {
        public Task<IReadOnlyList<Student>> GetStudentsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Student>>(students);

        public Task<IReadOnlyList<Student>> GetStudentsByClassAsync(int classId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Student>>(students.Where(s => s.ClassId == classId).ToList());

        public Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(students.FirstOrDefault(s => s.StudentId == studentId));

        public Task<int> CreateStudentAsync(Student student, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateStudentAsync(Student student, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteStudentAsync(int studentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExistsByRollNumberAsync(int classId, string rollNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class MockClassSubjectRepository(List<Subject> subjects) : IClassSubjectRepository
    {
        public Task<IReadOnlyList<SchoolClass>> GetClassesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Subject>> GetSubjectsByClassAsync(int classId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Subject>>(subjects.Where(s => s.ClassId == classId).ToList());

        public Task<int> CreateClassAsync(SchoolClass schoolClass, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateClassAsync(SchoolClass schoolClass, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteClassAsync(int classId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CreateSubjectAsync(Subject subject, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateSubjectAsync(Subject subject, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteSubjectAsync(int subjectId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    [Fact]
    public async Task GetClassSubjectMarks_CalculatesScoresAndGradesCorrectly()
    {
        var markRepo = new InMemoryExamMarkRepository();
        var students = new List<Student>
        {
            new() { StudentId = 1, FirstName = "Ali", LastName = "Haidari", FatherName = "Reza", ClassId = 1, RollNumber = "10" }
        };
        var subjects = new List<Subject>
        {
            new() { SubjectId = 1, ClassId = 1, SubjectName = "Mathematics" }
        };

        await markRepo.SaveOrUpdateMarkAsync(new ExamMark
        {
            StudentId = 1,
            SubjectId = 1,
            MidtermScore = 38m,
            FinalScore = 55m
        });

        var service = new ExamMarkService(markRepo, new MockStudentRepository(students), new MockClassSubjectRepository(subjects));

        var results = await service.GetClassSubjectMarksAsync(1, 1);

        Assert.Single(results);
        var r = results[0];
        Assert.Equal(93m, r.TotalScore);
        Assert.True(r.IsPass);
    }

    [Fact]
    public async Task SaveMarksBatch_WhenMidtermExceedsMax_ThrowsArgumentOutOfRangeException()
    {
        var markRepo = new InMemoryExamMarkRepository();
        var service = new ExamMarkService(markRepo, new MockStudentRepository([]), new MockClassSubjectRepository([]));

        var marks = new List<SaveExamMarkDto>
        {
            new(StudentId: 1, SubjectId: 1, MidtermScore: 45m, FinalScore: 50m)
        };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await service.SaveMarksBatchAsync(marks);
        });
    }

    [Fact]
    public async Task SaveMarksBatch_WhenFinalExceedsMax_ThrowsArgumentOutOfRangeException()
    {
        var markRepo = new InMemoryExamMarkRepository();
        var service = new ExamMarkService(markRepo, new MockStudentRepository([]), new MockClassSubjectRepository([]));

        var marks = new List<SaveExamMarkDto>
        {
            new(StudentId: 1, SubjectId: 1, MidtermScore: 35m, FinalScore: 65m)
        };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await service.SaveMarksBatchAsync(marks);
        });
    }

    [Fact]
    public async Task SaveMarksBatch_WhenValid_PersistsCorrectly()
    {
        var markRepo = new InMemoryExamMarkRepository();
        var service = new ExamMarkService(markRepo, new MockStudentRepository([]), new MockClassSubjectRepository([]));

        var marks = new List<SaveExamMarkDto>
        {
            new(StudentId: 1, SubjectId: 1, MidtermScore: 30m, FinalScore: 45m),
            new(StudentId: 2, SubjectId: 1, MidtermScore: 20m, FinalScore: 35m)
        };

        await service.SaveMarksBatchAsync(marks);

        var saved = await markRepo.GetMarksByClassAndSubjectAsync(1, 1);
        Assert.Equal(2, saved.Count);
    }
}
