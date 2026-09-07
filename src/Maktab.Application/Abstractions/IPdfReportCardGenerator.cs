using Maktab.Domain.Enums;

namespace Maktab.Application.Abstractions;

public interface IPdfReportCardGenerator
{
    Task GeneratePdfReportAsync(
        StudentReportCardDto reportCard,
        string outputFilePath,
        ReportCardType reportType,
        CancellationToken cancellationToken = default);
}
