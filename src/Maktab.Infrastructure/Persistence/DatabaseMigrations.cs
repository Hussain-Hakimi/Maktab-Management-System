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
);
")
        };
    }
}
