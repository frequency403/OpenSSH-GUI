using System.Reflection;

namespace OpenSSH_GUI.Core.Lib.Settings;

/// <summary>
///     Represents a settings file for the application.
/// </summary>
public record Settings
{
    /// <summary>
    ///     The current version of the application.
    /// </summary>
    public string Version { get; init; } =
        $"v{(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Version?.ToString(3)}";

    /// <summary>
    ///     Whether to convert ppk files automatically.
    /// </summary>
    public bool ConvertPpkAutomatically { get; set; } = false;
}