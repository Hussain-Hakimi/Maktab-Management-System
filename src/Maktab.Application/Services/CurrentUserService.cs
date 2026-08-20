using Maktab.Application.Abstractions;

namespace Maktab.Application.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    public UserDto? CurrentUser { get; set; }
}
