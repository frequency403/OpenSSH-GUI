using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Extensions;

public class SshConfigFilesExtensionTests
{
    [Theory, InlineData(PlatformID.Win32NT, false, "%PROGRAMDATA%\\ssh"), InlineData(PlatformID.Unix, false, "/etc/ssh")]
    public void GetRootSshPath_Tests(PlatformID platform, bool resolve, string expected) { SshConfigFilesExtension.GetRootSshPath(resolve, platform).ShouldBe(expected); }

    [Theory, InlineData(PlatformID.Win32NT, false, "%USERPROFILE%\\.ssh"), InlineData(PlatformID.Unix, false, "%HOME%/.ssh")]
    public void GetBaseSshPath_Tests(PlatformID platform, bool resolve, string expected) { SshConfigFilesExtension.GetBaseSshPath(resolve, platform).ShouldBe(expected); }

    [Theory, InlineData(SshConfigFiles.Config, PlatformID.Win32NT, false, "%USERPROFILE%\\.ssh\\config"),
     InlineData(SshConfigFiles.Config, PlatformID.Unix, false, "%HOME%/.ssh/config"), InlineData(SshConfigFiles.Sshd_Config, PlatformID.Unix, false, "/etc/ssh/sshd_config")]
    public void GetPathOfFile_Tests(SshConfigFiles file, PlatformID platform, bool resolve, string expected) { file.GetPathOfFile(resolve, platform).ShouldBe(expected); }
}