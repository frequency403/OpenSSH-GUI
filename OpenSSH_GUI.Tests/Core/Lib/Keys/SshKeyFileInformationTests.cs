using Avalonia.Headless.XUnit;
using OpenSSH_GUI.Core.Lib.Keys;
using Shouldly;
using SshNet.Keygen;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Lib.Keys;

public class SshKeyFileInformationTests
{
    [AvaloniaFact]
    public void Constructor_ExistingOpenSshFile_ComputesMetadataCorrectly()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(dir.FullName, "id_rsa");
            File.WriteAllText(path, "dummy");

            var info = new SshKeyFileInformation(SshKeyFileSource.FromDisk(path));

            info.Exists.ShouldBeTrue();
            info.FileName.ShouldBe("id_rsa");
            info.FullFileName.ShouldBe(path);
            info.DirectoryName.ShouldBe(dir.FullName);
            info.CurrentFormat.ShouldBe(SshKeyFormat.OpenSSH);
            info.IsOpenSshKey.ShouldBeTrue();
            info.PublicKeyFileName.ShouldBe(path + ".pub");
            info.CanChangeFileName.ShouldBeTrue();
            info.Files.Length.ShouldBe(2);
            info.AvailableFormatsForConversion.ShouldNotContain(SshKeyFormat.OpenSSH);
            info.DefaultConversionFormat.ShouldNotBe(SshKeyFormat.OpenSSH);
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void Constructor_NonExistentFile_ExistsIsFalse()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(dir.FullName, "missing_key");
            var info = new SshKeyFileInformation(SshKeyFileSource.FromDisk(path));

            info.Exists.ShouldBeFalse();
            info.FileName.ShouldBe("missing_key");
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void Constructor_PpkFile_HasNoPublicKeyFileName()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(dir.FullName, "id.ppk");
            File.WriteAllText(path, "dummy");

            var info = new SshKeyFileInformation(SshKeyFileSource.FromDisk(path));

            info.CurrentFormat.ShouldBe(SshKeyFormat.PuTTYv3);
            info.IsOpenSshKey.ShouldBeFalse();
            info.PublicKeyFileName.ShouldBeNull();
            info.Files.Length.ShouldBe(1);
            info.AvailableFormatsForConversion.ShouldContain(SshKeyFormat.OpenSSH);
            info.DefaultConversionFormat.ShouldBe(SshKeyFormat.OpenSSH);
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void Constructor_ProvidedByConfigSource_CanChangeFileNameIsFalse()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(dir.FullName, "id_cfg");
            File.WriteAllText(path, "dummy");

            var info = new SshKeyFileInformation(SshKeyFileSource.FromConfig(path));

            info.CanChangeFileName.ShouldBeFalse();
            info.KeyFileSource.ProvidedByConfig.ShouldBeTrue();
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void Constructor_EmptyPath_FallsBackToAppContextBaseDirectory()
    {
        var info = new SshKeyFileInformation(new SshKeyFileSource());

        info.FullFileName.ShouldBe(new FileInfo(AppContext.BaseDirectory).FullName);
    }

    [AvaloniaFact]
    public void Equals_SameKeyFileSource_AreEqual()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(dir.FullName, "id_x");
            File.WriteAllText(path, "dummy");

            var a = new SshKeyFileInformation(SshKeyFileSource.FromDisk(path));
            var b = new SshKeyFileInformation(SshKeyFileSource.FromDisk(path));

            a.ShouldBe(b);
            a.GetHashCode().ShouldBe(b.GetHashCode());
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void Equals_DifferentKeyFileSource_AreNotEqual()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var pathA = Path.Combine(dir.FullName, "id_a");
            var pathB = Path.Combine(dir.FullName, "id_b");
            File.WriteAllText(pathA, "dummy");
            File.WriteAllText(pathB, "dummy");

            var a = new SshKeyFileInformation(SshKeyFileSource.FromDisk(pathA));
            var b = new SshKeyFileInformation(SshKeyFileSource.FromDisk(pathB));

            a.ShouldNotBe(b);
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void Equals_Null_ReturnsFalse()
    {
        var info = new SshKeyFileInformation(new SshKeyFileSource());

        info.Equals(null).ShouldBeFalse();
    }
}
