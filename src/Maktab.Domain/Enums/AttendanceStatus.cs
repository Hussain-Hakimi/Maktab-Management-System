namespace Maktab.Domain.Enums;

/// <summary>
/// Daily attendance status for a student.
/// Only <see cref="Absent"/> counts toward the 30-day promotion limit;
/// Ill and Permission are excused absences tracked separately.
/// </summary>
public enum AttendanceStatus
{
    Present,
    Absent,
    Ill,
    Permission
}
