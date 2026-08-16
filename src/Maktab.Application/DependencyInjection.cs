using Microsoft.Extensions.DependencyInjection;

namespace Maktab.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationCore(this IServiceCollection services)
    {
        return services;
    }
}