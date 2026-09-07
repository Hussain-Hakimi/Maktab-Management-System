using Maktab.Domain.Enums;

namespace Maktab.Application.Abstractions;

public interface IReportCardService
{
    Task<StudentReportCardDto> GetStudentReportCardDataAsync(
        int studentId,
        string academicYear,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentReportCardDto>> GetClassReportCardsDataAsync(
        int classId,
        string academicYear,
        CancellationToken cancellationToken = default);

    Task<string> GenerateStudentReportCardPdfAsync(
        int studentId,
        string academicYear,
        string outputDirectory,
        ReportCardType reportType = ReportCardType.Annual,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GenerateClassReportCardsPdfAsync(
        int classId,
        string academicYear,
        string outputDirectory,
        ReportCardType reportType = ReportCardType.Annual,
        CancellationToken cancellationToken = default);
}
