using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using OpenSSH_GUI.SshConfig.Extensions;
using OpenSSH_GUI.SshConfig.Models;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.SshConfig;

public class SshConfigurationBindingTests
{
    private static IFileProvider GetEmbeddedFileProvider() => new EmbeddedFileProvider(typeof(SshConfigParserTests).Assembly, "OpenSSH_GUI.Tests.Assets.Testfiles");

    [Fact]
    public void AddSshConfig_ShouldBeBindableToObjects()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        builder.AddSshConfig(GetEmbeddedFileProvider(), "ssh_config_personal", false, true);
        var configuration = builder.Build();

        // Act
        var sshConfig = configuration.GetSection("SshConfig").Get<SshConfiguration>();
        sshConfig.ShouldNotBeNull();

        // Assert
        var possibleKeyFiles = new HashSet<string>();
        foreach (var hostSetting in sshConfig.Hosts.Concat(sshConfig.Blocks).Append(sshConfig.Global))
        {
            if (hostSetting.IdentityFiles is not { Length: > 0 } hostIdentityFiles) continue;
            foreach (var hostIdentityFile in hostIdentityFiles.Select(path =>
                     {
                         path = Environment.ExpandEnvironmentVariables(path);

                         if (!path.StartsWith('~')) return Path.GetFullPath(path);
                         var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                         path = path.Length == 1 ? home : Path.Combine(home, path[2..]);
                         return Path.GetFullPath(path);
                     }))
                if (!possibleKeyFiles.Any(e => e.Equals(hostIdentityFile, StringComparison.OrdinalIgnoreCase)))
                    possibleKeyFiles.Add(hostIdentityFile);
        }

        possibleKeyFiles.ShouldNotBeEmpty();
    }
}