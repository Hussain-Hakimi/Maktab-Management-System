namespace Maktab.Infrastructure.Persistence;

internal sealed record DatabaseMigration(int Version, string Sql);

internal static class DatabaseMigrations
{
    /// <summary>
    /// Baseline schema for version 1. This script is applied only when
    /// the database has no version (user_version = 0) or is new.
    /// It contains all tables required for V1.0.1 + V1.1 features.
    /// </summary>
    public const string BaselineSql = SchemaSql.Script;

    /// <summary>
    /// Ordered list of migrations beyond the baseline.
    /// For now it is empty, but future versions will add entries here.
    /// </summary>
    public static IReadOnlyList<DatabaseMigration> GetMigrations()
    {
        return new List<DatabaseMigration>
        {
            // Future migrations go here, e.g.
            // new(2, "ALTER TABLE tbl_Students ADD COLUMN ...;")
        };
    }
}
