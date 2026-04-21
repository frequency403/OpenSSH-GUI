using Avalonia.Controls;
using Avalonia.Threading;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.MVVM;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Extensions;

public class DependencyInjectionExtensionsTests
{
    [Fact]
    public void RegisterViewWithViewModel_ValidNaming_ShouldRegister()
    {
        // Arrange
        var services = new Container();

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
        var services = new Container();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(services.RegisterViewWithViewModel<MockWindow, InvalidVm>);
    }

    private class MockWindow : Window;

    private class MockWindowViewModel() : ViewModelBase;

    private class InvalidVm() : ViewModelBase;
}