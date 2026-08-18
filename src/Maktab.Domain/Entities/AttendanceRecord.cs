using Maktab.Domain.Enums;

namespace Maktab.Domain.Entities;

public sealed class AttendanceRecord
{
    public int StudentId { get; set; }
    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Notes { get; set; }
}
