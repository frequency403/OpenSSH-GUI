using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;

/// <summary>
///     Represents an authorized key.
/// </summary>
public interface IAuthorizedKey
{
    /// <summary>
    ///     Enumeration for SSH key types.
    /// </summary>
    SshKeyType KeyType { get; }

    /// <summary>
    ///     Represents an authorized key entry in an authorized keys file.
    /// </summary>
    string Fingerprint { get; }

    /// <summary>
    ///     Represents an authorized key entry in an authorized keys file.
    /// </summary>
    string Comment { get; }

    /// *Property: MarkedForDeletion**
    bool MarkedForDeletion { get; set; }

    /// <summary>
    ///     Returns the full key entry of an authorized key.
    /// </summary>
    /// <returns>The full key entry string.</returns>
    string GetFullKeyEntry { get; }
}