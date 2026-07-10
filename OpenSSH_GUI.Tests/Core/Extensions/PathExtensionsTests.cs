using Avalonia.Headless.XUnit;
using OpenSSH_GUI.Core.Extensions;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Extensions;

public class PathExtensionsTests
{
    [AvaloniaFact]
    public void WithJsonExtension_AppendsJson() { Path.WithJsonExtension("config").ShouldBe("config.json"); }

    [AvaloniaFact]
    public void WithLogExtension_AppendsLog() { Path.WithLogExtension("app").ShouldBe("app.log"); }

    [AvaloniaFact]
    public void WithOpenSshPublicKeyExtension_AppendsPub() { Path.WithOpenSshPublicKeyExtension("id_rsa").ShouldBe("id_rsa.pub"); }

    [AvaloniaFact]
    public void WithPuTTYKeyExtension_AppendsPpk() { Path.WithPuTTYKeyExtension("id_rsa").ShouldBe("id_rsa.ppk"); }

    [AvaloniaTheory]
    [InlineData("file.json", true)]
    [InlineData("file.JSON", true)]
    [InlineData("file.txt", false)]
    public void IsJson_Tests(string path, bool expected) { Path.IsJson(path).ShouldBe(expected); }

    [AvaloniaTheory]
    [InlineData("file.log", true)]
    [InlineData("file.LOG", true)]
    [InlineData("file.txt", false)]
    public void IsLog_Tests(string path, bool expected) { Path.IsLog(path).ShouldBe(expected); }

    [AvaloniaTheory]
    [InlineData("key.ppk", true)]
    [InlineData("key.PPK", true)]
    [InlineData("key.pub", false)]
    public void IsPuTTYKey_Tests(string path, bool expected) { Path.IsPuTTYKey(path).ShouldBe(expected); }

    [AvaloniaTheory]
    [InlineData("id_rsa.pub", true)]
    [InlineData("id_rsa.PUB", true)]
    [InlineData("id_rsa", false)]
    public void IsOpenSshPublicKey_Tests(string path, bool expected) { Path.IsOpenSshPublicKey(path).ShouldBe(expected); }

    [AvaloniaTheory]
    [InlineData("file.txt", "txt", true)]
    [InlineData("file.TXT", "txt", true)]
    [InlineData("file.txt", "json", false)]
    public void HasExtension_Tests(string path, string extension, bool expected) { Path.HasExtension(path, extension).ShouldBe(expected); }

    [AvaloniaTheory]
    [InlineData("file.TXT", ".txt")]
    [InlineData("file", "")]
    [InlineData("archive.tar.gz", ".gz")]
    public void GetNormalizedExtension_Tests(string path, string expected) { Path.GetNormalizedExtension(path).ShouldBe(expected); }

    [AvaloniaFact]
    public void Directory_CreateIfNotExists_CreatesMissingDirectory()
    {
        var dir = System.IO.Directory.CreateTempSubdirectory();
        try
        {
            var target = System.IO.Path.Combine(dir.FullName, "nested", "dir");

            Directory.CreateIfNotExists(target);

            System.IO.Directory.Exists(target).ShouldBeTrue();
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void Directory_CreateIfNotExists_ExistingDirectory_DoesNotThrow()
    {
        var dir = System.IO.Directory.CreateTempSubdirectory();
        try
        {
            Should.NotThrow(() => Directory.CreateIfNotExists(dir.FullName));
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void Directory_CreateIfNotExists_NullOrWhitespace_DoesNothing()
    {
        Should.NotThrow(() => Directory.CreateIfNotExists(null));
        Should.NotThrow(() => Directory.CreateIfNotExists("   "));
    }

    [AvaloniaFact]
    public void File_CreateIfNotExists_CreatesFileWithContent()
    {
        var dir = System.IO.Directory.CreateTempSubdirectory();
        try
        {
            var target = System.IO.Path.Combine(dir.FullName, "sub", "file.txt");

            File.CreateIfNotExists(target, "hello world");

            System.IO.File.Exists(target).ShouldBeTrue();
            System.IO.File.ReadAllText(target).ShouldBe("hello world");
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void File_CreateIfNotExists_CreatesEmptyFileWithoutContent()
    {
        var dir = System.IO.Directory.CreateTempSubdirectory();
        try
        {
            var target = System.IO.Path.Combine(dir.FullName, "file.txt");

            File.CreateIfNotExists(target);

            System.IO.File.Exists(target).ShouldBeTrue();
            System.IO.File.ReadAllText(target).ShouldBeEmpty();
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void File_CreateIfNotExists_ExistingFile_DoesNotOverwrite()
    {
        var dir = System.IO.Directory.CreateTempSubdirectory();
        try
        {
            var target = System.IO.Path.Combine(dir.FullName, "file.txt");
            System.IO.File.WriteAllText(target, "original");

            File.CreateIfNotExists(target, "new content");

            System.IO.File.ReadAllText(target).ShouldBe("original");
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void File_CreateIfNotExists_NullPath_Throws()
    {
        Should.Throw<ArgumentNullException>(() => File.CreateIfNotExists(null!));
    }

    [AvaloniaFact]
    public void File_RemoveIfExists_DeletesExistingFile()
    {
        var dir = System.IO.Directory.CreateTempSubdirectory();
        try
        {
            var target = System.IO.Path.Combine(dir.FullName, "file.txt");
            System.IO.File.WriteAllText(target, "content");

            File.RemoveIfExists(target);

            System.IO.File.Exists(target).ShouldBeFalse();
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void File_RemoveIfExists_NonExistentFile_DoesNotThrow()
    {
        var dir = System.IO.Directory.CreateTempSubdirectory();
        try
        {
            var target = System.IO.Path.Combine(dir.FullName, "does-not-exist.txt");

            Should.NotThrow(() => File.RemoveIfExists(target));
        }
        finally
        {
            dir.Delete(true);
        }
    }
}
