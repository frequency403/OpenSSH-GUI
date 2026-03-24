using ReactiveUI;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Interfaces.KnownHosts;

/// Represents a known host key in the OpenSSH GUI.
/// /
public interface IKnownHostKey : IReactiveObject
{
    /// <summary>
    ///     Represents the type of a known host key.
    /// </summary>
    SshKeyType KeyType { get; }

    /// <summary>
    ///     Represents a known host key in the OpenSSH GUI.
    /// </summary>
    string Fingerprint { get; }

    /// <summary>
    ///     Represents a known host key in the OpenSSH GUI.
    /// </summary>
    string EntryWithoutHost { get; }

    /// <summary>
    ///     Gets or sets whether the KnownHostKey is marked for deletion.
    /// </summary>
    bool MarkedForDeletion { get; set; }
}