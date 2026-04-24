namespace OpenSSH_GUI.Core.Lib.Keys;

public record SshKeyFileSource
{
    public string AbsolutePath { get; init; } = string.Empty;
    public bool ProvidedByConfig { get; init; }

    public static SshKeyFileSource FromDisk(string absolutePath) => new()
        { AbsolutePath = absolutePath };

    public static SshKeyFileSource FromConfig(string absolutePath) => new()
        { AbsolutePath = absolutePath, ProvidedByConfig = true };

    public override string ToString() => $"{AbsolutePath} | Referenced by Config: {ProvidedByConfig}";
}