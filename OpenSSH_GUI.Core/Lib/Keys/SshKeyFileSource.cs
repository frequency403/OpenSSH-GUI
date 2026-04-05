namespace OpenSSH_GUI.Core.Lib.Keys;

public record SshKeyFileSource
{
    public string AbsolutePath { get; init; } = string.Empty;
    public bool ProvidedByConfig { get; init; }

    public static SshKeyFileSource FromDisk(string absolutePath)
    {
        return new SshKeyFileSource { AbsolutePath = absolutePath };
    }

    public static SshKeyFileSource FromConfig(string absolutePath)
    {
        return new SshKeyFileSource { AbsolutePath = absolutePath, ProvidedByConfig = true };
    }

    public override string ToString()
    {
        return $"{AbsolutePath} | Referenced by Config: {ProvidedByConfig}";
    }
}