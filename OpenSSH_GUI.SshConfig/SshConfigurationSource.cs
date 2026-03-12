using Microsoft.Extensions.Configuration;

namespace OpenSSH_GUI.SshConfig;

/// <summary>
///     Represents an SSH configuration file as an <see cref="IConfigurationSource" />.
/// </summary>
public sealed class SshConfigurationSource : FileConfigurationSource
{
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