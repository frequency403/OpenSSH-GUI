namespace OpenSSH_GUI.Core.Lib.Misc;

public record BackedUpFile
{
    public required FileInfo InitialFile { get; init; }
    public required FileInfo BackupFile { get; init; }

    public void Backup()
    {
        InitialFile.CopyTo(BackupFile.FullName);
    }

    public void Restore()
    {
        BackupFile.MoveTo(InitialFile.FullName, true);
    }

    public void Delete()
    {
        BackupFile.Delete();
    }

    public override string ToString()
    {
        return $"{InitialFile.FullName} -> {BackupFile.FullName}";
    }
}