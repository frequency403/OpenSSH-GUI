using Avalonia.Headless.XUnit;
using OpenSSH_GUI.Core.Extensions;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Extensions;

public class PlatformIdExtensionsTests
{
    [AvaloniaTheory]
    [InlineData(PlatformID.Win32NT, "\r\n")]
    [InlineData(PlatformID.Win32Windows, "\r\n")]
    [InlineData(PlatformID.Win32S, "\r\n")]
    [InlineData(PlatformID.WinCE, "\r\n")]
    [InlineData(PlatformID.Unix, "\n")]
    [InlineData(PlatformID.MacOSX, "\n")]
    [InlineData(PlatformID.Other, "\n")]
    public void GetLineSeparator_Tests(PlatformID platformId, string expected) { platformId.GetLineSeparator().ShouldBe(expected); }
}
