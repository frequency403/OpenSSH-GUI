using System.Reactive.Linq;
using OpenSSH_GUI.Core.MVVM;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.MVVM;

public class ViewModelBaseTests
{
    [Fact]
    public async Task InitializeAsync_ShouldSetIsInitialized()
    {
        // Arrange
        var vm = new TestViewModel();

        // Act
        await vm.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(vm.IsInitialized);
    }

    [Fact]
    public async Task BooleanSubmit_ShouldCallOnBooleanSubmitAsync()
    {
        // Arrange
        var vm = new TestViewModel();

        // Act
        await vm.BooleanSubmitCommand.Execute(true).FirstAsync();

        // Assert
        Assert.True(vm.OnBooleanSubmitCalled);
        Assert.True(vm.InputParam);
        Assert.False(vm.IsInitialized);
    }

    [Fact]
    public void RequestClose_ShouldInvokeCloseEvent()
    {
        // Arrange
        var vm = new TestViewModel();
        var closeInvoked = false;
        vm.Close += (_, _) => closeInvoked = true;

        // Act
        vm.TriggerClose();

        // Assert
        Assert.True(closeInvoked);
        Assert.False(vm.IsInitialized);
    }

    private class TestViewModel : ViewModelBase
    {
        public bool OnBooleanSubmitCalled { get; private set; }
        public bool InputParam { get; private set; }

        protected override Task BooleanSubmitAsync(bool inputParameter, CancellationToken cancellationToken = default)
        {
            OnBooleanSubmitCalled = true;
            InputParam = inputParameter;
            return Task.CompletedTask;
        }

        public void TriggerClose()
        {
            RequestClose();
        }
    }
}