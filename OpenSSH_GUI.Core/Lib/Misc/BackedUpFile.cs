namespace OpenSSH_GUI.Core.Lib.Misc;

public record BackedUpFile
{
    public required FileInfo InitialFile { get; init; }
    public required FileInfo BackupFile { get; init; }
    
    public void Backup() => InitialFile.CopyTo(BackupFile.FullName);
    public void Restore() => BackupFile.MoveTo(InitialFile.FullName, overwrite: true);
    public void Delete() => BackupFile.Delete();
    
    public bool IsBackedUp => InitialFile.Exists && !BackupFile.Exists;
    
    
    public override string ToString() => $"{InitialFile.FullName} -> {BackupFile.FullName}";
}