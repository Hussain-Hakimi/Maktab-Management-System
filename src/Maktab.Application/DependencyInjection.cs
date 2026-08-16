using Microsoft.Extensions.DependencyInjection;
using Maktab.Application.Abstractions;
using Maktab.Application.Services;

namespace Maktab.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationCore(this IServiceCollection services)
    {
        services.AddSingleton<IClassSubjectService, ClassSubjectService>();
        return services;
    }
}