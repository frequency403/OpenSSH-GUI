using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Interfaces;

namespace OpenSSH_GUI.Core.Lib.Keys;

/// <summary>
///     Default implementation of <see cref="ISshKeyFactory" />.
///     Creates <see cref="SshKeyFile" /> instances with a shared logger,
///     eliminating the need for a service locator at the call site.
/// </summary>
public sealed class SshKeyFactory(ILogger<SshKeyFactory> logger, ILoggerFactory loggerFactory) : ISshKeyFactory
{
    /// <inheritdoc />
    public SshKeyFile Create()
    {
        logger.LogDebug("Creating new SshKeyFile instance");
        return new SshKeyFile(loggerFactory.CreateLogger<SshKeyFile>());
    }
}