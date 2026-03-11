using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSSH_GUI.Core.MVVM;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.MVVM;

public class ViewModelBaseTests
{
    private class TestViewModel() : ViewModelBase<TestViewModel>(NullLogger<TestViewModel>.Instance)
    {
        public bool OnBooleanSubmitCalled { get; private set; }
        public bool InputParam { get; private set; }

        protected override Task OnBooleanSubmitAsync(bool inputParameter, CancellationToken cancellationToken = default)
        {
            OnBooleanSubmitCalled = true;
            InputParam = inputParameter;
            return Task.CompletedTask;
        }

        public void TriggerClose() => RequestClose();
    }

    [Fact]
    public async Task InitializeAsync_ShouldSetIsInitialized()
    {
        // Arrange
        var vm = new TestViewModel();

        // Act
        await vm.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.True(vm.IsInitialized);
    }

    [Fact]
    public async Task BooleanSubmit_ShouldCallOnBooleanSubmitAsync()
    {
        // Arrange
        var vm = new TestViewModel();

        // Act
        await vm.BooleanSubmit.Execute(true).FirstAsync();

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
        bool closeInvoked = false;
        vm.Close += (s, e) => closeInvoked = true;

        // Act
        vm.TriggerClose();

        // Assert
        Assert.True(closeInvoked);
        Assert.False(vm.IsInitialized);
    }
}
