using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
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
        var serviceCollection = new ServiceCollection();

        // Act
        serviceCollection.RegisterViewWithViewModel<MockWindow, MockWindowViewModel>();
        
        var services = serviceCollection.BuildServiceProvider();

        // Assert
        Dispatcher.UIThread.Invoke(() =>
        {
            Assert.NotNull(services.GetKeyedService<MockWindow>(nameof(MockWindow)));
            Assert.NotNull(services.GetKeyedService<MockWindowViewModel>(nameof(MockWindowViewModel)));
        });
    }

    [Fact]
    public void RegisterViewWithViewModel_InvalidNaming_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(services.RegisterViewWithViewModel<MockWindow, InvalidVm>);
    }

    private class MockWindow : Window;

    private class MockWindowViewModel : ViewModelBase;

    private class InvalidVm : ViewModelBase;
}