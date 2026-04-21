using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Lib.KnownHosts;

/// <summary>
///     Represents a known host key in the OpenSSH GUI.
/// </summary>
public partial record KnownHostKey : ReactiveRecord
{
    private readonly string _entryWithoutHost;

    /// <summary>
    ///     Gets or sets a value indicating whether the known host key is marked for deletion.
    /// </summary>
    [Reactive] private bool _markedForDeletion;

    /// <summary>
    ///     Represents a known host key in the OpenSSH GUI.
    /// </summary>
    public KnownHostKey(string[] keyParts)
    {
        _entryWithoutHost = string.Join(" ", keyParts);
        TypeDeclarationInFile = keyParts[0];
        KeyType = Enum.Parse<SshKeyType>(
            TypeDeclarationInFile.StartsWith("ssh-")
                ? TypeDeclarationInFile.Replace("ssh-", "")
                : TypeDeclarationInFile.Split('-')[0], true);
        Fingerprint = keyParts[1];
    }

    /// <summary>
    ///     Represents a known host key.
    /// </summary>
    private string TypeDeclarationInFile { get; }

    /// <summary>
    ///     Represents the type of a known host key.
    /// </summary>
    public SshKeyType KeyType { get; }

    /// <summary>
    ///     Represents a known host key.
    /// </summary>
    public string Fingerprint { get; }

    public override string ToString()
    {
        return _entryWithoutHost;
    }
}