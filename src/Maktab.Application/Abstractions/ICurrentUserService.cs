namespace Maktab.Application.Abstractions;

public interface ICurrentUserService
{
    UserDto? CurrentUser { get; set; }
}
