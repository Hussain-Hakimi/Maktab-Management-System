namespace Maktab.Infrastructure.Persistence;

public sealed record AppFolders(
    string Root,
    string Data,
    string Logs,
    string Backups,
    string Reports,
    string Logos)
{
    public static AppFolders CreateDefault()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "AppData");

        return new AppFolders(
            Root: root,
            Data: Path.Combine(root, "Data"),
            Logs: Path.Combine(root, "Logs"),
            Backups: Path.Combine(root, "Backups"),
            Reports: Path.Combine(root, "Reports"),
            Logos: Path.Combine(root, "Logos"));
    }
}
