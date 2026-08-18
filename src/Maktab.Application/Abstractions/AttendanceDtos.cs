using Maktab.Domain.Enums;

namespace Maktab.Application.Abstractions;

public sealed record SaveAttendanceDto(
    int StudentId,
    DateOnly Date,
    AttendanceStatus Status,
    string? Notes);

/// <summary>
/// One row of the daily attendance sheet: a student of the class plus the
/// status recorded for the selected date (defaults to Present when nothing
/// has been saved yet — the teacher only marks the exceptions).
/// </summary>
public sealed class DailyAttendanceRowDto
{
    public int StudentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FatherName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public AttendanceStatus Status { get; set; }
    public string? Notes { get; set; }
    public bool IsSaved { get; set; }
}

/// <summary>
/// Per-student attendance statistics used by the statistics view and the
/// promotion rule (AbsentDays feeds the 30-day absence limit).
/// </summary>
public sealed class StudentAttendanceSummaryDto
{
    public int StudentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int IllDays { get; set; }
    public int PermissionDays { get; set; }
    public int TotalRecordedDays => PresentDays + AbsentDays + IllDays + PermissionDays;
    public bool ExceedsAbsenceLimit { get; set; }
}

/// <summary>
/// Result of importing an attendance Excel template: the parsed rows plus
/// human-readable per-cell problems (unknown status words, bad dates, ...).
/// </summary>
public sealed class AttendanceImportResultDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public List<SaveAttendanceDto> Rows { get; } = [];
    public List<string> Errors { get; } = [];
    public bool HasErrors => Errors.Count > 0;
}
