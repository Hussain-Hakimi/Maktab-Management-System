using Microsoft.Extensions.DependencyInjection;
using Maktab.Application.Abstractions;
using Maktab.Infrastructure.Logging;
using Maktab.Infrastructure.Persistence;
using Maktab.Infrastructure.Reports;

namespace Maktab.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureCore(this IServiceCollection services)
    {
        services.AddSingleton(AppFolders.CreateDefault());
        services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();
        services.AddSingleton<IDatabaseInitializer, SqliteDatabaseInitializer>();
        services.AddSingleton<IAppLogger, FileAppLogger>();
        services.AddSingleton<IBackupService, SqliteBackupService>();
        services.AddSingleton<IClassSubjectRepository, SqliteClassSubjectRepository>();
        services.AddSingleton<IStudentRepository, SqliteStudentRepository>();
        services.AddSingleton<IExamMarkRepository, SqliteExamMarkRepository>();
        services.AddSingleton<IAttendanceRepository, SqliteAttendanceRepository>();
        services.AddSingleton<IBookRepository, SqliteBookRepository>();
        services.AddSingleton<ITextbookRepository, SqliteTextbookRepository>();
        services.AddSingleton<IFeeRepository, SqliteFeeRepository>();
        services.AddSingleton<IUserRepository, SqliteUserRepository>();
        services.AddSingleton<IAuditLogRepository, SqliteAuditLogRepository>();
        services.AddSingleton<ISettingRepository, SqliteSettingRepository>();
        services.AddSingleton<IAcademicYearRepository, SqliteAcademicYearRepository>();
        services.AddSingleton<IStudentPromotionHistoryRepository, SqliteStudentPromotionHistoryRepository>();
        services.AddSingleton<IPdfReportCardGenerator, QuestPdfReportCardGenerator>();

        return services;
    }
}
