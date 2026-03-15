namespace OpenSSH_GUI.SshConfig.Models;

/// <summary>
///     Represents the complete SSH configuration as a bindable object.
/// </summary>
public sealed class SshConfiguration
{
    /// <summary>
    ///     Gets or sets the global SSH settings.
    /// </summary>
    public SshHostSettings Global { get; set; } = new();

    /// <summary>
    ///     Gets or sets the list of host-specific SSH settings.
    /// </summary>
    public List<SshHostSettings> Hosts { get; set; } = [];

    /// <summary>
    ///     Gets or sets all blocks (Host and Match) in document order.
    /// </summary>
    public List<SshHostSettings> Blocks { get; set; } = [];
}