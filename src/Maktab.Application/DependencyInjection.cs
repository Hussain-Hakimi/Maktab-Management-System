using Microsoft.Extensions.DependencyInjection;
using Maktab.Application.Abstractions;
using Maktab.Application.Services;

namespace Maktab.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationCore(this IServiceCollection services)
    {
        services.AddSingleton<IClassSubjectService, ClassSubjectService>();
        services.AddSingleton<IStudentService, StudentService>();
        services.AddSingleton<IExamMarkService, ExamMarkService>();
        services.AddSingleton<IReportCardService, ReportCardService>();
        services.AddSingleton<IAttendanceService, AttendanceService>();
        services.AddSingleton<IBookService, BookService>();
        services.AddSingleton<ITextbookService, TextbookService>();
        services.AddSingleton<IFeeService, FeeService>();
        services.AddSingleton<IUserService, UserService>();
        services.AddSingleton<IAuditService, AuditService>();
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<ISettingService, SettingService>();
        return services;
    }
}
