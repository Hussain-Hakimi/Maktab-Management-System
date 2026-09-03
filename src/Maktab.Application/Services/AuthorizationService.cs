using Maktab.Application.Abstractions;
using Maktab.Domain.Enums;

namespace Maktab.Application.Services;

public sealed class AuthorizationService(ICurrentUserService currentUserService) : IAuthorizationService
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
}
