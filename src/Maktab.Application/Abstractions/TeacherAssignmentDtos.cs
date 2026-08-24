namespace Maktab.Application.Abstractions;

public sealed class TeacherSubjectAssignmentDto
{
    public int TeacherSubjectId { get; set; }
    public int TeacherUserId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
}

public sealed class ClassGuardianDto
{
    public int ClassGuardianId { get; set; }
    public int TeacherUserId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
}

public sealed record SaveTeacherSubjectAssignmentDto(
    int TeacherUserId,
    int ClassId,
    int SubjectId);

public sealed record SaveClassGuardianDto(
    int TeacherUserId,
    int ClassId);
