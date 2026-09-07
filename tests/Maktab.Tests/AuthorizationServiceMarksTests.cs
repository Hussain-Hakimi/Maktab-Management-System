using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Enums;

namespace Maktab.Tests;

public class AuthorizationServiceMarksTests
{
    private sealed class StubTeacherAssignmentService : ITeacherAssignmentService
    {
        public List<TeacherSubjectAssignmentDto> Assignments { get; } = [];

        public Task<int> AssignTeacherToSubjectAsync(int teacherUserId, int classId, int subjectId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task RemoveTeacherSubjectAssignmentAsync(int teacherSubjectId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<TeacherSubjectAssignmentDto>> GetTeacherSubjectsAsync(int? teacherUserId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<TeacherSubjectAssignmentDto>> GetMyTeacherSubjectsAsync(int teacherUserId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<TeacherSubjectAssignmentDto>>(Assignments);
        public Task<int> AssignClassGuardianAsync(int teacherUserId, int classId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task RemoveClassGuardianAsync(int classGuardianId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<ClassGuardianDto>> GetClassGuardiansAsync(int? teacherUserId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> IsClassGuardianAsync(int teacherUserId, int classId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class StubFinalizationService : IFinalizationService
    {
        public bool IsFinalized { get; set; }

        public Task FinalizeClassAsync(int classId, int academicYearId, int teacherUserId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UnfinalizeClassAsync(int classId, int academicYearId, int teacherUserId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> IsClassFinalizedAsync(int classId, int academicYearId, CancellationToken cancellationToken = default) => Task.FromResult(IsFinalized);
    }

    private static AuthorizationService CreateTeacherAuthorization(
        StubTeacherAssignmentService assignments,
        StubFinalizationService finalization,
        int teacherUserId = 10)
    {
        var currentUser = new CurrentUserService
        {
            CurrentUser = new UserDto
            {
                UserId = teacherUserId,
                Username = "teacher",
                FullName = "Teacher",
                Role = UserRole.Teacher,
                IsActive = true
            }
        };

        return new AuthorizationService(currentUser, assignments, finalization);
    }

    [Fact]
    public async Task TeacherAssignedToClassAndSubject_CanEditMarks()
    {
        var assignments = new StubTeacherAssignmentService();
        assignments.Assignments.Add(new TeacherSubjectAssignmentDto { TeacherUserId = 10, ClassId = 2, SubjectId = 3 });
        var finalization = new StubFinalizationService();
        var authorization = CreateTeacherAuthorization(assignments, finalization);

        await authorization.RequireCanEditMarksAsync(2, 3, 2026);
    }

    [Fact]
    public async Task TeacherNotAssignedToClassAndSubject_CannotEditMarks()
    {
        var assignments = new StubTeacherAssignmentService();
        assignments.Assignments.Add(new TeacherSubjectAssignmentDto { TeacherUserId = 10, ClassId = 2, SubjectId = 4 });
        var finalization = new StubFinalizationService();
        var authorization = CreateTeacherAuthorization(assignments, finalization);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            authorization.RequireCanEditMarksAsync(2, 3, 2026));
    }

    [Fact]
    public async Task TeacherCannotEditFinalizedClass()
    {
        var assignments = new StubTeacherAssignmentService();
        assignments.Assignments.Add(new TeacherSubjectAssignmentDto { TeacherUserId = 10, ClassId = 2, SubjectId = 3 });
        var finalization = new StubFinalizationService { IsFinalized = true };
        var authorization = CreateTeacherAuthorization(assignments, finalization);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            authorization.RequireCanEditMarksAsync(2, 3, 2026));
    }

    [Fact]
    public async Task AdminCanEditMarksWithoutTeacherAssignment()
    {
        var assignments = new StubTeacherAssignmentService();
        var finalization = new StubFinalizationService { IsFinalized = true };
        var currentUser = new CurrentUserService
        {
            CurrentUser = new UserDto { UserId = 1, Username = "admin", FullName = "Admin", Role = UserRole.Admin, IsActive = true }
        };
        var authorization = new AuthorizationService(currentUser, assignments, finalization);

        await authorization.RequireCanEditMarksAsync(2, 3, 2026);
    }
}
