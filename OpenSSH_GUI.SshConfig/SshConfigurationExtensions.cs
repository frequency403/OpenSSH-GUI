using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace OpenSSH_GUI.SshConfig;

/// <summary>
///     Extension methods for adding <see cref="SshConfigurationSource" />.
/// </summary>
public static class SshConfigurationExtensions
{
    /// <summary>
    ///     Adds the SSH configuration file at <paramref name="path" /> to the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder" /> to add to.</param>
    /// <param name="path">Path relative to the base path stored in <see cref="IConfigurationBuilder.Properties" /> of <paramref name="builder" />.</param>
    /// <returns>The <see cref="IConfigurationBuilder" />.</returns>
    public static IConfigurationBuilder AddSshConfig(this IConfigurationBuilder builder, string path)
    {
        return AddSshConfig(builder, null, path, false, false);
    }

    /// <summary>
    ///     Adds the SSH configuration file at <paramref name="path" /> to the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder" /> to add to.</param>
    /// <param name="path">Path relative to the base path stored in <see cref="IConfigurationBuilder.Properties" /> of <paramref name="builder" />.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <returns>The <see cref="IConfigurationBuilder" />.</returns>
    public static IConfigurationBuilder AddSshConfig(this IConfigurationBuilder builder, string path, bool optional)
    {
        return AddSshConfig(builder, null, path, optional, false);
    }

    /// <summary>
    ///     Adds the SSH configuration file at <paramref name="path" /> to the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder" /> to add to.</param>
    /// <param name="path">Path relative to the base path stored in <see cref="IConfigurationBuilder.Properties" /> of <paramref name="builder" />.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
    /// <returns>The <see cref="IConfigurationBuilder" />.</returns>
    public static IConfigurationBuilder AddSshConfig(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange)
    {
        return AddSshConfig(builder, null, path, optional, reloadOnChange);
    }

    /// <summary>
    ///     Adds the SSH configuration file at <paramref name="path" /> to the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder" /> to add to.</param>
    /// <param name="fileProvider">The <see cref="IFileProvider" /> to use to access the file.</param>
    /// <param name="path">Path relative to the base path stored in <see cref="IConfigurationBuilder.Properties" /> of <paramref name="builder" />.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
    /// <returns>The <see cref="IConfigurationBuilder" />.</returns>
    public static IConfigurationBuilder AddSshConfig(this IConfigurationBuilder builder, IFileProvider? fileProvider, string path, bool optional, bool reloadOnChange)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("File path must be a non-empty string.", nameof(path));
        }

        return builder.AddSshConfig(s =>
        {
            s.FileProvider = fileProvider;
            s.Path = path;
            s.Optional = optional;
            s.ReloadOnChange = reloadOnChange;
            s.ResolveFileProvider();
        });
    }

    /// <summary>
    ///     Adds an SSH configuration source to the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder" /> to add to.</param>
    /// <param name="configureSource">Configures the source.</param>
    /// <returns>The <see cref="IConfigurationBuilder" />.</returns>
    public static IConfigurationBuilder AddSshConfig(this IConfigurationBuilder builder, Action<SshConfigurationSource>? configureSource)
    {
        var source = new SshConfigurationSource();
        configureSource?.Invoke(source);
        return builder.Add(source);
    }
}
