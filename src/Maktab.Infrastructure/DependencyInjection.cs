using Microsoft.Extensions.DependencyInjection;
using Maktab.Infrastructure.Persistence;

namespace Maktab.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureCore(this IServiceCollection services)
    {
        services.AddSingleton(AppFolders.CreateDefault());
        services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();
        services.AddSingleton<IDatabaseInitializer, SqliteDatabaseInitializer>();

        return services;
    }
}