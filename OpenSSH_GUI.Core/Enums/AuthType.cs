namespace OpenSSH_GUI.Core.Enums;

/// <summary>
///     Represents the types of authentication supported for SSH connections.
/// </summary>
public enum AuthType
{
    /// <summary>
    ///     Represents connection credentials using password authentication.
    /// </summary>
    Password,

    /// <summary>
    ///     Represents the authentication type of connection credentials using SSH key.
    /// </summary>
    Key,

    /// <summary>
    ///     Represents a multi-key authentication type for SSH connections.
    /// </summary>
    MultiKey
}