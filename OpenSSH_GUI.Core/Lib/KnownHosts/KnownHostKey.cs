using OpenSSH_GUI.Core.Interfaces.KnownHosts;
using ReactiveUI;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Lib.KnownHosts;

/// <summary>
///     Represents a known host key in the OpenSSH GUI.
/// </summary>
public class KnownHostKey : ReactiveObject, IKnownHostKey
{
    /// <summary>
    ///     Represents a known host key in the OpenSSH GUI.
    /// </summary>
    public KnownHostKey(string entry)
    {
        EntryWithoutHost = entry;
        var splitted = EntryWithoutHost.Split(' ');
        TypeDeclarationInFile = splitted[0];
        KeyType = Enum.Parse<SshKeyType>(
            TypeDeclarationInFile.StartsWith("ssh-")
                ? TypeDeclarationInFile.Replace("ssh-", "")
                : TypeDeclarationInFile.Split('-')[0], true);
        Fingerprint = splitted[1].Replace("\n", "").Replace("\r", "");
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

    /// <summary>
    ///     Represents a known host key without the host entry in the OpenSSH GUI.
    /// </summary>
    public string EntryWithoutHost { get; }

    /// <summary>
    ///     Gets or sets a value indicating whether the known host key is marked for deletion.
    /// </summary>
    public bool MarkedForDeletion
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
}