using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;

namespace Maktab.Tests;

public class FinalizationServiceTests
{
    private sealed class InMemoryFinalizationRepository : IFinalizationRepository
    {
        private readonly Dictionary<(int classId, int yearId), ClassFinalization> _data = new();

        public Task<ClassFinalization?> GetByClassYearAsync(
            int classId,
            int academicYearId,
            CancellationToken cancellationToken = default)
        {
            _data.TryGetValue((classId, academicYearId), out var finalization);
            return Task.FromResult(finalization);
        }

        public Task UpsertAsync(ClassFinalization finalization, CancellationToken cancellationToken = default)
        {
            _data[(finalization.ClassId, finalization.AcademicYearId)] = finalization;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryTeacherAssignmentService : ITeacherAssignmentService
    {
        private readonly List<ClassGuardianDto> _guardians = [];
        private readonly List<TeacherSubjectAssignmentDto> _assignments = [];

        public InMemoryTeacherAssignmentService(
            List<ClassGuardianDto> guardians,
            List<TeacherSubjectAssignmentDto> assignments)
        {
            _guardians = guardians;
            _assignments = assignments;
        }

        public Task<int> AssignTeacherToSubjectAsync(int teacherUserId, int classId, int subjectId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task RemoveTeacherSubjectAssignmentAsync(int teacherSubjectId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<IReadOnlyList<TeacherSubjectAssignmentDto>> GetTeacherSubjectsAsync(int? teacherUserId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TeacherSubjectAssignmentDto>>(
                _assignments.Where(a => teacherUserId == null || a.TeacherUserId == teacherUserId).ToList());
        public Task<IReadOnlyList<TeacherSubjectAssignmentDto>> GetMyTeacherSubjectsAsync(int teacherUserId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TeacherSubjectAssignmentDto>>(
                _assignments.Where(a => a.TeacherUserId == teacherUserId).ToList());
        public Task<int> AssignClassGuardianAsync(int teacherUserId, int classId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task RemoveClassGuardianAsync(int classGuardianId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<IReadOnlyList<ClassGuardianDto>> GetClassGuardiansAsync(int? teacherUserId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ClassGuardianDto>>(
                _guardians.Where(g => teacherUserId == null || g.TeacherUserId == teacherUserId).ToList());
        public Task<bool> IsClassGuardianAsync(int teacherUserId, int classId, CancellationToken cancellationToken = default)
            => Task.FromResult(_guardians.Any(g => g.TeacherUserId == teacherUserId && g.ClassId == classId));
    }

    [Fact]
    public async Task FinalizeClass_WhenUserIsGuardian_SetsFinalizedTrue()
    {
        // Arrange
        var repo = new InMemoryFinalizationRepository();
        var guardians = new List<ClassGuardianDto>
        {
            new() { TeacherUserId = 1, ClassId = 2 }
        };
        var service = new FinalizationService(repo, new InMemoryTeacherAssignmentService(guardians, new List<TeacherSubjectAssignmentDto>()));

        // Act
        await service.FinalizeClassAsync(2, 1, 1);

        // Assert
        bool isFinalized = await service.IsClassFinalizedAsync(2, 1);
        Assert.True(isFinalized);
    }

    [Fact]
    public async Task FinalizeClass_WhenUserNotGuardian_ThrowsInvalidOperationException()
    {
        var repo = new InMemoryFinalizationRepository();
        var service = new FinalizationService(repo, new InMemoryTeacherAssignmentService(new List<ClassGuardianDto>(), new List<TeacherSubjectAssignmentDto>()));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await service.FinalizeClassAsync(2, 1, 1);
        });
    }

    [Fact]
    public async Task UnfinalizeClass_RemovesFinalization()
    {
        // Arrange
        var repo = new InMemoryFinalizationRepository();
        var guardians = new List<ClassGuardianDto>
        {
            new() { TeacherUserId = 1, ClassId = 2 }
        };
        var service = new FinalizationService(repo, new InMemoryTeacherAssignmentService(guardians, new List<TeacherSubjectAssignmentDto>()));

        // Finalize then unfinalize
        await service.FinalizeClassAsync(2, 1, 1);
        await service.UnfinalizeClassAsync(2, 1, 1);

        bool isFinalized = await service.IsClassFinalizedAsync(2, 1);
        Assert.False(isFinalized);
    }
}
