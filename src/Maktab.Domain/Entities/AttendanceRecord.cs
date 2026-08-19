using Maktab.Domain.Enums;

namespace Maktab.Domain.Entities;

public sealed class AttendanceRecord
{
    public int AttendanceId { get; set; }
    public int StudentId { get; set; }
    public DateTime Date { get; set; }
    public AttendanceStatus Status { get; set; }
}
