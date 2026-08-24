namespace Maktab.Application.Abstractions;

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}

public sealed class AlertItemDto
{
    public string Type { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? EntityReference { get; set; }
}
