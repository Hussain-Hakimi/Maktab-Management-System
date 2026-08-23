using Maktab.Domain.Enums;

namespace Maktab.Application.Abstractions;

public sealed class StudentAttendanceSummaryDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int IllDays { get; set; }
    public int PermissionDays { get; set; }
    public int TotalDays => PresentDays + AbsentDays + IllDays + PermissionDays;
    public decimal AbsenceRate => TotalDays > 0 ? Math.Round((decimal)AbsentDays / TotalDays * 100, 2) : 0m;
}

public sealed class MonthlyAttendanceRowDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public Dictionary<int, AttendanceStatus> DayStatuses { get; set; } = new();
}
