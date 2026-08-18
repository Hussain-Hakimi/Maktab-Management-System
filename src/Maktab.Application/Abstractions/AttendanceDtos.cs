using Maktab.Domain.Enums;

namespace Maktab.Application.Abstractions;

public sealed record SaveAttendanceDto(
    int StudentId,
    DateOnly Date,
    AttendanceStatus Status,
    string? Notes);

public sealed class StudentAttendanceRowDto
{
    public int StudentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FatherName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public string? Notes { get; set; }
    /// <summary>True when the row comes from a saved record; false when it is the default-present placeholder.</summary>
    public bool IsSaved { get; set; }
}

public sealed class StudentAbsenceSummaryDto
{
    public int StudentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FatherName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int IllDays { get; set; }
    public int PermissionDays { get; set; }
    public int TotalMarkedDays => PresentDays + AbsentDays + IllDays + PermissionDays;
    /// <summary>Days counted against the promotion rule (unexcused absences only).</summary>
    public int AbsenceDaysForPromotion => AbsentDays;
    public bool ExceedsAbsenceLimit { get; set; }
}

public sealed class AttendanceImportResultDto
{
    public int ImportedRecords { get; set; }
    public int SkippedCells { get; set; }
    public List<string> Errors { get; } = [];
    public bool HasErrors => Errors.Count > 0;
}
