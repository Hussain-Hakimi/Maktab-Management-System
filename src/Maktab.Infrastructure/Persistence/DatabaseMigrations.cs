namespace Maktab.Infrastructure.Persistence;

internal sealed record DatabaseMigration(int Version, string Sql);

internal static class DatabaseMigrations
{
    public const string BaselineSql = SchemaSql.Script;

    public static IReadOnlyList<DatabaseMigration> GetMigrations()
    {
        return new List<DatabaseMigration>
        {
            new(2, @"
CREATE TABLE IF NOT EXISTS tbl_Users (
    UserID INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    FullName TEXT NOT NULL,
    Role TEXT NOT NULL CHECK (Role IN ('Admin', 'Teacher', 'Librarian', 'Accountant')),
    IsActive INTEGER NOT NULL CHECK (IsActive IN (0, 1))
);"),
            new(3, @"
CREATE TABLE IF NOT EXISTS tbl_Settings (
    SettingID INTEGER PRIMARY KEY AUTOINCREMENT,
    Key TEXT NOT NULL UNIQUE,
    Value TEXT NOT NULL
);"),
            new(4, @"
CREATE TABLE IF NOT EXISTS tbl_AcademicYears (
    AcademicYearID INTEGER PRIMARY KEY AUTOINCREMENT,
    YearName TEXT NOT NULL UNIQUE,
    StartDate TEXT NOT NULL,
    EndDate TEXT NOT NULL,
    IsActive INTEGER NOT NULL CHECK (IsActive IN (0, 1))
);
ALTER TABLE tbl_ExamMarks ADD COLUMN AcademicYearId INTEGER NOT NULL DEFAULT 0;
ALTER TABLE tbl_Attendance ADD COLUMN AcademicYearId INTEGER NOT NULL DEFAULT 0;
ALTER TABLE tbl_Fees ADD COLUMN AcademicYearId INTEGER NOT NULL DEFAULT 0;
")
        };
    }
}
