using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using OpenSSH_GUI.SshConfig.Models;
using OpenSSH_GUI.SshConfig.Services;

namespace OpenSSH_GUI.SshConfig.Extensions;

/// <summary>
///     Extension methods for adding <see cref="SshConfigurationSource" />.
/// </summary>
public static class SshConfigurationExtensions
{
    /// <param name="builder">The <see cref="IConfigurationBuilder" /> to add to.</param>
    extension(IConfigurationBuilder builder)
    {
        /// <summary>
        ///     Adds the SSH configuration file at <paramref name="path" /> to the <paramref name="builder" />.
        /// </summary>
        /// <param name="path">
        ///     Path relative to the base path stored in <see cref="IConfigurationBuilder.Properties" /> of
        ///     <paramref name="builder" />.
        /// </param>
        /// <returns>The <see cref="IConfigurationBuilder" />.</returns>
        public IConfigurationBuilder AddSshConfig(string path)
        {
            return builder.AddSshConfig(null, path, false, false);
        }

        /// <summary>
        ///     Adds the SSH configuration file at <paramref name="path" /> to the <paramref name="builder" />.
        /// </summary>
        /// <param name="path">
        ///     Path relative to the base path stored in <see cref="IConfigurationBuilder.Properties" /> of
        ///     <paramref name="builder" />.
        /// </param>
        /// <param name="optional">Whether the file is optional.</param>
        /// <returns>The <see cref="IConfigurationBuilder" />.</returns>
        public IConfigurationBuilder AddSshConfig(string path, bool optional)
        {
            return builder.AddSshConfig(null, path, optional, false);
        }

        /// <summary>
        ///     Adds the SSH configuration file at <paramref name="path" /> to the <paramref name="builder" />.
        /// </summary>
        /// <param name="path">
        ///     Path relative to the base path stored in <see cref="IConfigurationBuilder.Properties" /> of
        ///     <paramref name="builder" />.
        /// </param>
        /// <param name="optional">Whether the file is optional.</param>
        /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
        /// <returns>The <see cref="IConfigurationBuilder" />.</returns>
        public IConfigurationBuilder AddSshConfig(string path, bool optional,
            bool reloadOnChange)
        {
            return builder.AddSshConfig(null, path, optional, reloadOnChange);
        }

        /// <summary>
        ///     Adds the SSH configuration file at <paramref name="path" /> to the <paramref name="builder" />.
        /// </summary>
        /// <param name="fileProvider">The <see cref="IFileProvider" /> to use to access the file.</param>
        /// <param name="path">
        ///     Path relative to the base path stored in <see cref="IConfigurationBuilder.Properties" /> of
        ///     <paramref name="builder" />.
        /// </param>
        /// <param name="optional">Whether the file is optional.</param>
        /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
        /// <returns>The <see cref="IConfigurationBuilder" />.</returns>
        public IConfigurationBuilder AddSshConfig(IFileProvider? fileProvider,
            string path, bool optional, bool reloadOnChange)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrEmpty(path);

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
        /// <param name="configureSource">Configures the source.</param>
        /// <returns>The <see cref="IConfigurationBuilder" />.</returns>
        public IConfigurationBuilder AddSshConfig(Action<SshConfigurationSource>? configureSource)
        {
            var source = new SshConfigurationSource();
            configureSource?.Invoke(source);
            return builder.Add(source);
        }
    }
}