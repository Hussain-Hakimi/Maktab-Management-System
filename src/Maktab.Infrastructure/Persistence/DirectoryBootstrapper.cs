namespace Maktab.Infrastructure.Persistence;

public static class DirectoryBootstrapper
{
    public static void EnsureFoldersExist(AppFolders folders)
    {
        Directory.CreateDirectory(folders.Root);
        Directory.CreateDirectory(folders.Data);
        Directory.CreateDirectory(folders.Logs);
        Directory.CreateDirectory(folders.Backups);
        Directory.CreateDirectory(folders.Reports);
        Directory.CreateDirectory(folders.Logos);
    }
}
