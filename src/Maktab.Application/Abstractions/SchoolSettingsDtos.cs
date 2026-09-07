namespace Maktab.Application.Abstractions;

public sealed class SchoolSettingsDto
{
    public string SchoolName { get; set; } = string.Empty;
    public string SchoolAddress { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public string? LogoPath { get; set; }

    // New fields for official headers
    public string GovernmentTitle { get; set; } = string.Empty;
    public string ProvincialEducationHeader { get; set; } = string.Empty;
    public string DistrictEducationHeader { get; set; } = string.Empty;
}
