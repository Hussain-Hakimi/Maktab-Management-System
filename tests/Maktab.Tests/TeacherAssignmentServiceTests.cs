using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;

namespace Maktab.Tests;

public class TeacherAssignmentServiceTests
{
    private sealed class InMemoryTeacherAssignmentRepository : ITeacherAssignmentRepository
    {
        private readonly List<TeacherSubject> _teacherSubjects = [];
        private readonly List<ClassGuardian> _classGuardians = [];
        private int _nextTeacherSubjectId = 1;
        private int _nextClassGuardianId = 1;

        public Task<int> AddTeacherSubjectAsync(TeacherSubject assignment, CancellationToken cancellationToken = default)
        {
            assignment.TeacherSubjectId = _nextTeacherSubjectId++;
            _teacherSubjects.Add(assignment);
            return Task.FromResult(assignment.TeacherSubjectId);
        }

        public Task RemoveTeacherSubjectAsync(int teacherSubjectId, CancellationToken cancellationToken = default)
        {
            _teacherSubjects.RemoveAll(ts => ts.TeacherSubjectId == teacherSubjectId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<TeacherSubjectAssignmentDto>> GetTeacherSubjectsAsync(
            int? teacherUserId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _teacherSubjects.AsEnumerable();
            if (teacherUserId.HasValue)
                query = query.Where(ts => ts.TeacherUserId == teacherUserId.Value);

            var result = query.Select(ts => new TeacherSubjectAssignmentDto
            {
                TeacherSubjectId = ts.TeacherSubjectId,
                TeacherUserId = ts.TeacherUserId,
                TeacherName = ts.TeacherUserId.ToString(),
                ClassId = ts.ClassId,
                ClassName = ts.ClassId.ToString(),
                SubjectId = ts.SubjectId,
                SubjectName = ts.SubjectId.ToString()
            }).ToList();

            return Task.FromResult<IReadOnlyList<TeacherSubjectAssignmentDto>>(result);
        }

        public Task<IReadOnlyList<TeacherSubjectAssignmentDto>> GetTeacherSubjectsByTeacherAsync(
            int teacherUserId,
            CancellationToken cancellationToken = default)
        {
            return GetTeacherSubjectsAsync(teacherUserId, cancellationToken);
        }

        public Task<int> AddClassGuardianAsync(ClassGuardian guardian, CancellationToken cancellationToken = default)
        {
            guardian.ClassGuardianId = _nextClassGuardianId++;
            _classGuardians.Add(guardian);
            return Task.FromResult(guardian.ClassGuardianId);
        }

        public Task RemoveClassGuardianAsync(int classGuardianId, CancellationToken cancellationToken = default)
        {
            _classGuardians.RemoveAll(g => g.ClassGuardianId == classGuardianId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ClassGuardianDto>> GetClassGuardiansAsync(
            int? teacherUserId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _classGuardians.AsEnumerable();
            if (teacherUserId.HasValue)
                query = query.Where(g => g.TeacherUserId == teacherUserId.Value);

            var result = query.Select(g => new ClassGuardianDto
            {
                ClassGuardianId = g.ClassGuardianId,
                TeacherUserId = g.TeacherUserId,
                TeacherName = g.TeacherUserId.ToString(),
                ClassId = g.ClassId,
                ClassName = g.ClassId.ToString()
            }).ToList();

            return Task.FromResult<IReadOnlyList<ClassGuardianDto>>(result);
        }

        public Task<ClassGuardianDto?> GetClassGuardianByTeacherAndClassAsync(
            int teacherUserId,
            int classId,
            CancellationToken cancellationToken = default)
        {
            var guardian = _classGuardians.FirstOrDefault(g => g.TeacherUserId == teacherUserId && g.ClassId == classId);
            if (guardian is null)
                return Task.FromResult<ClassGuardianDto?>(null);

            return Task.FromResult<ClassGuardianDto?>(new ClassGuardianDto
            {
                ClassGuardianId = guardian.ClassGuardianId,
                TeacherUserId = guardian.TeacherUserId,
                TeacherName = guardian.TeacherUserId.ToString(),
                ClassId = guardian.ClassId,
                ClassName = guardian.ClassId.ToString()
            });
        }
    }

    [Fact]
    public async Task AssignTeacherToSubject_AddsAssignment()
    {
        var repo = new InMemoryTeacherAssignmentRepository();
        var service = new TeacherAssignmentService(repo);

        var id = await service.AssignTeacherToSubjectAsync(teacherUserId: 1, classId: 2, subjectId: 3);

        Assert.True(id > 0);
        var all = await service.GetTeacherSubjectsAsync();
        Assert.Single(all);
        Assert.Equal(1, all[0].TeacherUserId);
        Assert.Equal(2, all[0].ClassId);
        Assert.Equal(3, all[0].SubjectId);
    }

    [Fact]
    public async Task GetMyTeacherSubjects_ReturnsOnlyTeacherAssignments()
    {
        var repo = new InMemoryTeacherAssignmentRepository();
        var service = new TeacherAssignmentService(repo);

        await service.AssignTeacherToSubjectAsync(1, 2, 3);
        await service.AssignTeacherToSubjectAsync(1, 4, 5);
        await service.AssignTeacherToSubjectAsync(6, 7, 8);

        var my = await service.GetMyTeacherSubjectsAsync(teacherUserId: 1);

        Assert.Equal(2, my.Count);
        Assert.All(my, a => Assert.Equal(1, a.TeacherUserId));
    }

    [Fact]
    public async Task AssignClassGuardian_AndCheckIsGuardian()
    {
        var repo = new InMemoryTeacherAssignmentRepository();
        var service = new TeacherAssignmentService(repo);

        var guardianId = await service.AssignClassGuardianAsync(teacherUserId: 1, classId: 2);

        Assert.True(guardianId > 0);

        bool isGuardian = await service.IsClassGuardianAsync(teacherUserId: 1, classId: 2);
        Assert.True(isGuardian);

        bool notGuardian = await service.IsClassGuardianAsync(teacherUserId: 1, classId: 99);
        Assert.False(notGuardian);
    }

    [Fact]
    public async Task RemoveTeacherSubjectAssignment_RemovesCorrectly()
    {
        var repo = new InMemoryTeacherAssignmentRepository();
        var service = new TeacherAssignmentService(repo);

        var id = await service.AssignTeacherToSubjectAsync(1, 2, 3);
        await service.RemoveTeacherSubjectAssignmentAsync(id);

        var all = await service.GetTeacherSubjectsAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task RemoveClassGuardian_RemovesCorrectly()
    {
        var repo = new InMemoryTeacherAssignmentRepository();
        var service = new TeacherAssignmentService(repo);

        var id = await service.AssignClassGuardianAsync(1, 2);
        await service.RemoveClassGuardianAsync(id);

        var all = await service.GetClassGuardiansAsync();
        Assert.Empty(all);
    }
}
