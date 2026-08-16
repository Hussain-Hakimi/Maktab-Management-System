using Microsoft.Extensions.DependencyInjection;
using Maktab.Application.Abstractions;
using Maktab.Infrastructure.Persistence;

namespace Maktab.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureCore(this IServiceCollection services)
    {
        services.AddSingleton(AppFolders.CreateDefault());
        services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();
        services.AddSingleton<IDatabaseInitializer, SqliteDatabaseInitializer>();
        services.AddSingleton<IClassSubjectRepository, SqliteClassSubjectRepository>();
        services.AddSingleton<IStudentRepository, SqliteStudentRepository>();

        return services;
    }
}