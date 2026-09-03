using Maktab.Application.Abstractions;
using Maktab.Domain.Enums;

namespace Maktab.Application.Services;

public sealed class AuthorizationService(
    ICurrentUserService currentUserService,
    ITeacherAssignmentService? teacherAssignmentService = null,
    IFinalizationService? finalizationService = null) : IAuthorizationService
{
    public bool IsAuthenticated => currentUserService.CurrentUser is not null;

    public bool IsInRole(UserRole role) => currentUserService.CurrentUser?.Role == role;

    public void RequireRole(UserRole role)
    {
        if (!IsInRole(role))
            throw new UnauthorizedAccessException($"The current user must have the {role} role to perform this operation.");
    }

    public void RequireAnyRole(params UserRole[] roles)
    {
        ArgumentNullException.ThrowIfNull(roles);

        if (roles.Length == 0 || currentUserService.CurrentUser is null || !roles.Contains(currentUserService.CurrentUser.Role))
            throw new UnauthorizedAccessException("The current user is not authorized to perform this operation.");
    }

    public void RequireSelfOrRole(int userId, UserRole role)
    {
        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId));

        var currentUser = currentUserService.CurrentUser;
        if (currentUser is null || (currentUser.UserId != userId && currentUser.Role != role))
            throw new UnauthorizedAccessException("The current user is not authorized to modify this user.");
    }

    public async Task RequireCanEditMarksAsync(
        int classId,
        int subjectId,
        int academicYearId,
        CancellationToken cancellationToken = default)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));
        if (subjectId <= 0) throw new ArgumentOutOfRangeException(nameof(subjectId));
        if (academicYearId <= 0) throw new ArgumentOutOfRangeException(nameof(academicYearId));

        RequireAnyRole(UserRole.Admin, UserRole.Teacher);

        if (IsInRole(UserRole.Admin))
            return;

        var currentUser = currentUserService.CurrentUser!;
        if (teacherAssignmentService is null || finalizationService is null)
            throw new InvalidOperationException("Marks authorization dependencies are not configured.");

        var assignments = await teacherAssignmentService.GetMyTeacherSubjectsAsync(currentUser.UserId, cancellationToken);
        if (!assignments.Any(a => a.ClassId == classId && a.SubjectId == subjectId))
            throw new UnauthorizedAccessException("The teacher is not assigned to this class and subject.");

        if (await finalizationService.IsClassFinalizedAsync(classId, academicYearId, cancellationToken))
            throw new InvalidOperationException("The results for this class and academic year are finalized and cannot be changed by a teacher.");
    }
}
