using Maktab.Domain.Enums;

namespace Maktab.Application.Abstractions;

public interface IAuthorizationService
{
    bool IsAuthenticated { get; }
    bool IsInRole(UserRole role);
    void RequireRole(UserRole role);
    void RequireAnyRole(params UserRole[] roles);
    void RequireSelfOrRole(int userId, UserRole role);
    Task RequireCanEditMarksAsync(int classId, int subjectId, int academicYearId, CancellationToken cancellationToken = default);
}
