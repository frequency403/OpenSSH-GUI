using Microsoft.Extensions.Logging.Abstractions;
using OpenSSH_GUI.Core.Services;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Services;

public sealed class KeyFileWriterServiceTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("keyfilewriter").FullName;
    private readonly KeyFileWriterService _service = new(NullLogger<KeyFileWriterService>.Instance);

    public void Dispose() { Directory.Delete(_directory, true); }

    [Fact]
    public async Task WriteToFile_Overwrite_TruncatesLongerExistingContent()
    {
        var path = Path.Combine(_directory, "id_test");
        await File.WriteAllTextAsync(path, new string('x', 4096), TestContext.Current.CancellationToken);

        await _service.WriteToFile(path, "short", true);

        (await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken)).ShouldBe("short");
    }

    [Fact]
    public async Task WriteToFile_WithoutOverwrite_ThrowsAndKeepsExistingContent()
    {
        var path = Path.Combine(_directory, "id_test");
        await File.WriteAllTextAsync(path, "original", TestContext.Current.CancellationToken);

        await Should.ThrowAsync<IOException>(async () => await _service.WriteToFile(path, "new"));

        (await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken)).ShouldBe("original");
    }

    [Fact]
    public async Task WriteToFile_Overwrite_RestrictsExistingFileToOwner()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix file modes only");
        var path = Path.Combine(_directory, "id_test");
        await File.WriteAllTextAsync(path, "public", TestContext.Current.CancellationToken);
#pragma warning disable CA1416
        File.SetUnixFileMode(
            path,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);

        await _service.WriteToFile(path, "private", true);

        File.GetUnixFileMode(path).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
#pragma warning restore CA1416
    }

    [Fact]
    public async Task WriteToFile_NewFile_IsCreatedOwnerOnly()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix file modes only");
        var path = Path.Combine(_directory, "id_new");

        await _service.WriteToFile(path, "private");

#pragma warning disable CA1416
        File.GetUnixFileMode(path).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
#pragma warning restore CA1416
    }
}
