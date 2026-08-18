namespace Maktab.Application.Abstractions;

public interface IAttendanceExcelService
{
    /// <summary>
    /// Generates a pre-filled Excel attendance template for a class: one row per
    /// student, one column per day, every cell pre-set to "حاضر" (Present) so the
    /// teacher only edits the exceptions.
    /// </summary>
    Task GenerateClassTemplateAsync(int classId, DateOnly startDate, int numberOfDays, string outputFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses a filled attendance template back into attendance rows.
    /// Unknown status words and structural problems are reported in the result's
    /// Errors list instead of throwing.
    /// </summary>
    Task<AttendanceImportResultDto> ImportTemplateAsync(string filePath, CancellationToken cancellationToken = default);
}
