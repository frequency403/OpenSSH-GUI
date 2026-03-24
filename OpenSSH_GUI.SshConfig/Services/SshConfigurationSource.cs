using Microsoft.Extensions.Configuration;

namespace OpenSSH_GUI.SshConfig.Services;

/// <summary>
///     Represents an SSH configuration file as an <see cref="IConfigurationSource" />.
/// </summary>
public sealed class SshConfigurationSource : FileConfigurationSource
{
    /// <summary>
    /// Optional callback invoked when an included file cannot be read due to
    /// insufficient permissions or an I/O error. Receives the file path and the
    /// causing exception. When <see langword="null"/>, inaccessible files are
    /// silently skipped.
    /// </summary>
    public Action<string, Exception>? OnSkippedIncludeFile { get; init; }
    
    /// <summary>
    ///     Builds the <see cref="SshConfigurationProvider" /> for this source.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder" />.</param>
    /// <returns>A <see cref="SshConfigurationProvider" />.</returns>
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        EnsureDefaults(builder);
        return new SshConfigurationProvider(this);
    }
}