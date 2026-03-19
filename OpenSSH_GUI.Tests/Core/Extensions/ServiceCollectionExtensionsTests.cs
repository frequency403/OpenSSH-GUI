using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.MVVM;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void RegisterViewWithViewModel_ValidNaming_ShouldRegister()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.RegisterViewWithViewModel<MockWindow, MockWindowViewModel>();
        var provider = services.BuildServiceProvider();

        // Assert
        Dispatcher.UIThread.Invoke(() =>
        {
            Assert.NotNull(provider.GetKeyedService<MockWindow>("MockWindow"));
            Assert.NotNull(provider.GetKeyedService<MockWindowViewModel>("MockWindowViewModel"));
        });
    }

    [Fact]
    public void RegisterViewWithViewModel_InvalidNaming_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            services.RegisterViewWithViewModel<MockWindow, InvalidVm>());
    }

    [Fact]
    public void RegisterViewWithViewModel_AsSingleton_ShouldRegisterCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.RegisterViewWithViewModel<MockWindow, MockWindowViewModel>(true);
        var provider = services.BuildServiceProvider();

        // Assert
        Dispatcher.UIThread.Invoke(() =>
        {
            var w1 = provider.GetKeyedService<MockWindow>("MockWindow");
            var w2 = provider.GetKeyedService<MockWindow>("MockWindow");
            Assert.Same(w1, w2);
        });
    }

    private class MockWindow : Window
    {
    }

    private class MockWindowViewModel() : ViewModelBase<MockWindowViewModel>(NullLogger<MockWindowViewModel>.Instance)
    {
    }

    private class InvalidVm() : ViewModelBase<InvalidVm>(NullLogger<InvalidVm>.Instance)
    {
    }
}