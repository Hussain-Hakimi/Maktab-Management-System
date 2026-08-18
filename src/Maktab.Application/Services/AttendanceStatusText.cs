using Maktab.Domain.Enums;

namespace Maktab.Application.Services;

/// <summary>
/// Maps attendance statuses to their Dari labels and parses template cell values
/// (Dari or English) back into statuses. Shared by the UI and the Excel import.
/// </summary>
public static class AttendanceStatusText
{
    public static string ToDari(AttendanceStatus status) => status switch
    {
        AttendanceStatus.Present => "حاضر",
        AttendanceStatus.Absent => "غیرحاضر",
        AttendanceStatus.Ill => "مریض",
        _ => "اجازه"
    };

    public static bool TryParse(string? value, out AttendanceStatus status)
    {
        status = AttendanceStatus.Present;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim())
        {
            case "حاضر" or "Present" or "present" or "P" or "p":
                status = AttendanceStatus.Present;
                return true;
            case "غیرحاضر" or "Absent" or "absent" or "A" or "a":
                status = AttendanceStatus.Absent;
                return true;
            case "مریض" or "مريض" or "Ill" or "ill" or "Sick" or "sick" or "I" or "i":
                status = AttendanceStatus.Ill;
                return true;
            case "اجازه" or "Permission" or "permission" or "Leave" or "leave" or "E" or "e":
                status = AttendanceStatus.Permission;
                return true;
            default:
                return false;
        }
    }
}
