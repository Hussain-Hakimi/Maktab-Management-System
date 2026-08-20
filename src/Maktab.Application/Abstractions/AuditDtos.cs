namespace Maktab.Application.Abstractions;

public sealed class AuditLogDto
{
    public int LogId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string TimestampFormatted => Timestamp.ToString("yyyy/MM/dd HH:mm:ss");
}
