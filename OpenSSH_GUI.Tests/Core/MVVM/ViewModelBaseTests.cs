using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenSSH_GUI.Core.MVVM;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.MVVM;

public class ViewModelBaseTests
{
    private class TestViewModel : ViewModelBase<TestViewModel>
    {
        public bool OnBooleanSubmitCalled { get; private set; }
        public bool InputParam { get; private set; }

        protected override ValueTask<TestViewModel?> OnBooleanSubmitAsync(bool inputParameter)
        {
            OnBooleanSubmitCalled = true;
            InputParam = inputParameter;
            return ValueTask.FromResult<TestViewModel?>(this);
        }

        public void TriggerClose() => RequestClose();
    }

    [Fact]
    public async Task InitializeAsync_ShouldSetIsInitialized()
    {
        // Arrange
        var vm = new TestViewModel();

        // Act
        await vm.InitializeAsync();

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
    }
}
