namespace Maktab.Domain.Entities;

public sealed class AuditLog
{
    public int LogId { get; set; }
    public required string UserName { get; set; }
    public required string Action { get; set; }
    public DateTime Timestamp { get; set; }
}
