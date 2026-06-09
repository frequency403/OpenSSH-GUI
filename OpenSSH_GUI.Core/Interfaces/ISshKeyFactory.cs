using OpenSSH_GUI.Core.Lib.Keys;

namespace OpenSSH_GUI.Core.Interfaces;

/// <summary>
///     Factory for creating new <see cref="SshKeyFile" /> instances.
/// </summary>
public interface ISshKeyFactory
{
    /// <summary>
    ///     Creates a new, uninitialized <see cref="SshKeyFile" /> instance.
    /// </summary>
    SshKeyFile Create();
}