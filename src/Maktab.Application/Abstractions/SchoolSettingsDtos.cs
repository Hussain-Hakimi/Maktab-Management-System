namespace Maktab.Application.Abstractions;

public sealed class SchoolSettingsDto
{
    public string SchoolName { get; set; } = string.Empty;
    public string SchoolAddress { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
}
