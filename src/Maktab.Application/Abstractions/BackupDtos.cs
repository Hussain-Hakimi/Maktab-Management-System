namespace Maktab.Application.Abstractions;

public sealed record BackupInfoDto(
    string FileName,
    string FilePath,
    long FileSizeBytes,
    string FileSizeFormatted,
    DateTime CreatedAt,
    string CreatedAtFormatted);
