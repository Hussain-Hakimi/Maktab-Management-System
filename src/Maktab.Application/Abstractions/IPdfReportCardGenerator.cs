namespace Maktab.Application.Abstractions;

public interface IPdfReportCardGenerator
{
    Task GeneratePdfReportAsync(
        StudentReportCardDto reportCard,
        string outputFilePath,
        CancellationToken cancellationToken = default);
}
