using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Tests;

public class ExamServiceTests
{
    private sealed class InMemoryExamRepository : IExamRepository
    {
        private readonly List<Exam> _exams = [];
        private int _nextId = 1;

        public Task<int> CreateAsync(Exam exam, CancellationToken cancellationToken = default)
        {
            exam.ExamId = _nextId++;
            _exams.Add(exam);
            return Task.FromResult(exam.ExamId);
        }

        public Task<IReadOnlyList<ExamDto>> GetByTeacherAsync(int teacherUserId, CancellationToken cancellationToken = default)
        {
            var result = _exams
                .Where(e => e.CreatedByTeacherUserId == teacherUserId)
                .Select(e => new ExamDto
                {
                    ExamId = e.ExamId,
                    SubjectId = e.SubjectId,
                    ClassId = e.ClassId,
                    AcademicYearId = e.AcademicYearId,
                    ExamType = e.ExamType,
                    ExamDate = e.ExamDate,
                    CreatedByTeacherUserId = e.CreatedByTeacherUserId
                })
                .ToList();
            return Task.FromResult<IReadOnlyList<ExamDto>>(result);
        }

        public Task<IReadOnlyList<ExamDto>> GetByClassSubjectAsync(
            int classId,
            int subjectId,
            int academicYearId,
            CancellationToken cancellationToken = default)
        {
            var result = _exams
                .Where(e => e.ClassId == classId && e.SubjectId == subjectId && e.AcademicYearId == academicYearId)
                .Select(e => new ExamDto
                {
                    ExamId = e.ExamId,
                    SubjectId = e.SubjectId,
                    ClassId = e.ClassId,
                    AcademicYearId = e.AcademicYearId,
                    ExamType = e.ExamType,
                    ExamDate = e.ExamDate,
                    CreatedByTeacherUserId = e.CreatedByTeacherUserId
                })
                .ToList();
            return Task.FromResult<IReadOnlyList<ExamDto>>(result);
        }

        public Task DeleteAsync(int examId, CancellationToken cancellationToken = default)
        {
            _exams.RemoveAll(e => e.ExamId == examId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryTeacherAssignmentService : ITeacherAssignmentService
    {
        private readonly List<TeacherSubjectAssignmentDto> _assignments = [];

        public InMemoryTeacherAssignmentService(List<TeacherSubjectAssignmentDto> assignments)
        {
            _assignments = assignments;
        }

        public Task<int> AssignTeacherToSubjectAsync(int teacherUserId, int classId, int subjectId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task RemoveTeacherSubjectAssignmentAsync(int teacherSubjectId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<TeacherSubjectAssignmentDto>> GetTeacherSubjectsAsync(int? teacherUserId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TeacherSubjectAssignmentDto>>(
                _assignments.Where(a => teacherUserId is null || a.TeacherUserId == teacherUserId).ToList());

        public Task<IReadOnlyList<TeacherSubjectAssignmentDto>> GetMyTeacherSubjectsAsync(int teacherUserId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TeacherSubjectAssignmentDto>>(
                _assignments.Where(a => a.TeacherUserId == teacherUserId).ToList());

        public Task<int> AssignClassGuardianAsync(int teacherUserId, int classId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task RemoveClassGuardianAsync(int classGuardianId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<ClassGuardianDto>> GetClassGuardiansAsync(int? teacherUserId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ClassGuardianDto>>(new List<ClassGuardianDto>());

        public Task<bool> IsClassGuardianAsync(int teacherUserId, int classId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }

    [Fact]
    public async Task CreateExam_WhenTeacherAssigned_Succeeds()
    {
        var teacherId = 1;
        var assignment = new TeacherSubjectAssignmentDto
        {
            TeacherUserId = teacherId,
            ClassId = 2,
            SubjectId = 3
        };
        var assignmentService = new InMemoryTeacherAssignmentService(new List<TeacherSubjectAssignmentDto> { assignment });
        var repo = new InMemoryExamRepository();
        var service = new ExamService(repo, assignmentService);

        var examDto = new SaveExamDto(
            SubjectId: 3,
            ClassId: 2,
            AcademicYearId: 1,
            ExamType: ExamType.Midterm,
            ExamDate: DateTime.Today,
            CreatedByTeacherUserId: teacherId);

        var id = await service.CreateExamAsync(examDto);

        Assert.True(id > 0);
        var exams = await repo.GetByTeacherAsync(teacherId);
        Assert.Single(exams);
    }

    [Fact]
    public async Task CreateExam_WhenTeacherNotAssigned_ThrowsInvalidOperationException()
    {
        var teacherId = 1;
        var assignmentService = new InMemoryTeacherAssignmentService(new List<TeacherSubjectAssignmentDto>());
        var repo = new InMemoryExamRepository();
        var service = new ExamService(repo, assignmentService);

        var examDto = new SaveExamDto(
            SubjectId: 3,
            ClassId: 2,
            AcademicYearId: 1,
            ExamType: ExamType.Midterm,
            ExamDate: DateTime.Today,
            CreatedByTeacherUserId: teacherId);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await service.CreateExamAsync(examDto);
        });
    }

    [Fact]
    public async Task GetMyExams_ReturnsOnlyTeacherExams()
    {
        var teacher1 = 1;
        var teacher2 = 2;
        var assignment1 = new TeacherSubjectAssignmentDto { TeacherUserId = teacher1, ClassId = 2, SubjectId = 3 };
        var assignment2 = new TeacherSubjectAssignmentDto { TeacherUserId = teacher2, ClassId = 2, SubjectId = 3 };
        var assignmentService = new InMemoryTeacherAssignmentService(new List<TeacherSubjectAssignmentDto> { assignment1, assignment2 });
        var repo = new InMemoryExamRepository();

        await repo.CreateAsync(new Exam
        {
            SubjectId = 3,
            ClassId = 2,
            AcademicYearId = 1,
            ExamType = ExamType.Midterm,
            ExamDate = DateTime.Today,
            CreatedByTeacherUserId = teacher1
        });
        await repo.CreateAsync(new Exam
        {
            SubjectId = 3,
            ClassId = 2,
            AcademicYearId = 1,
            ExamType = ExamType.Final,
            ExamDate = DateTime.Today,
            CreatedByTeacherUserId = teacher2
        });

        var service = new ExamService(repo, assignmentService);
        var myExams = await service.GetMyExamsAsync(teacher1);

        Assert.Single(myExams);
        Assert.All(myExams, e => Assert.Equal(teacher1, e.CreatedByTeacherUserId));
    }
}
