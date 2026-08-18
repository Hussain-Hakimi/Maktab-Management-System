namespace Maktab.Application.Abstractions;

public interface IAttendanceExcelService
{
    /// <summary>
    /// Generates a pre-filled Excel attendance template for a class. Every student row
    /// defaults to "حاضر" (Present) so only exceptions need to be edited offline.
    /// </summary>
    Task<string> GenerateClassTemplateAsync(int classId, DateOnly startDate, int numberOfDays, string outputFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a filled attendance template back into the database.
    /// Unknown students, dates and statuses are skipped and reported in the result.
    /// </summary>
    Task<AttendanceImportResultDto> ImportTemplateAsync(string filePath, CancellationToken cancellationToken = default);
}
