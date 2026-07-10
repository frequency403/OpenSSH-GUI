using Avalonia.Headless.XUnit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OpenSSH_GUI.Core.Lib.Keys;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Lib.Keys;

public class SshKeyFactoryTests
{
    [AvaloniaFact]
    public void Create_ReturnsNewSshKeyFileInstance()
    {
        var logger = Substitute.For<ILogger<SshKeyFactory>>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        var factory = new SshKeyFactory(logger, loggerFactory);

        var result = factory.Create();

        result.ShouldNotBeNull();
        result.ShouldBeOfType<SshKeyFile>();
    }

    [AvaloniaFact]
    public void Create_UsesLoggerFactoryToCreateSshKeyFileLogger()
    {
        var logger = Substitute.For<ILogger<SshKeyFactory>>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        var factory = new SshKeyFactory(logger, loggerFactory);
        factory.Create();

        loggerFactory.Received(1).CreateLogger(typeof(SshKeyFile).FullName!);
    }

    [AvaloniaFact]
    public void Create_CalledMultipleTimes_ReturnsDistinctInstances()
    {
        var logger = Substitute.For<ILogger<SshKeyFactory>>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        var factory = new SshKeyFactory(logger, loggerFactory);

        var first = factory.Create();
        var second = factory.Create();

        first.ShouldNotBeSameAs(second);
    }
}
