namespace OpenSSH_GUI.SshConfig;

/// <summary>
///     Represents a type-safe data holder for common SSH host configuration settings.
/// </summary>
/// <param name="Patterns">The patterns from the Host line.</param>
/// <param name="HostName">The actual host name to connect to.</param>
/// <param name="User">The user name to login with.</param>
/// <param name="Port">The port number to connect to.</param>
/// <param name="IdentityFiles">One or more identity (private key) files to use for authentication.</param>
/// <param name="ProxyJump">The jump host configuration.</param>
/// <param name="LocalForwards">Local port forwarding configurations.</param>
/// <param name="OtherEntries">Other entries not explicitly mapped to properties.</param>
public sealed record SshHostSettings(
    string[] Patterns,
    string? HostName = null,
    string? User = null,
    int? Port = null,
    string[]? IdentityFiles = null,
    string? ProxyJump = null,
    string[]? LocalForwards = null,
    SshConfigEntry[]? OtherEntries = null
)
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SshHostSettings" /> class.
    ///     Required for the configuration binder.
    /// </summary>
    public SshHostSettings() : this([])
    {
    }

    /// <summary>
    ///     Gets an empty <see cref="SshHostSettings" /> instance.
    /// </summary>
    public static SshHostSettings Empty { get; } = new([]);

    /// <summary>
    ///     Returns a value indicating whether all properties are null or empty.
    /// </summary>
    public bool IsEmpty =>
        Patterns.Length == 0 &&
        HostName == null &&
        User == null &&
        Port == null &&
        (IdentityFiles == null || IdentityFiles.Length == 0) &&
        ProxyJump == null &&
        (LocalForwards == null || LocalForwards.Length == 0) &&
        (OtherEntries == null || OtherEntries.Length == 0);
}