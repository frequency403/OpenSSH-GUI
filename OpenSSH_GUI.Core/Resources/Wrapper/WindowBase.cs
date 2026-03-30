using Avalonia.Controls;
using Avalonia.Media.Imaging;
using DryIoc;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.MVVM;
using ReactiveUI.Avalonia;
namespace OpenSSH_GUI.Core.Resources.Wrapper;

public abstract class WindowBase<TViewModel, TViewModelInitializer> : WindowBase<TViewModel>
    where TViewModel : ViewModelBase<TViewModel, TViewModelInitializer>
    where TViewModelInitializer : class, IInitializerParameters<TViewModel> 
{
    public async ValueTask InitializeAsync(TViewModelInitializer initializer, WindowStartupLocation startupLocation = WindowStartupLocation.CenterScreen,  CancellationToken cancellationToken = default)
    {
        await WindowInitialize(startupLocation);
        ArgumentNullException.ThrowIfNull(ViewModel);
        await ViewModel.InitializeAsync(initializer,cancellationToken);
    }
}

public abstract class WindowBase<TViewModel> : ReactiveWindow<TViewModel> where TViewModel : ViewModelBase<TViewModel>
{
    public required ILogger<WindowBase<TViewModel>> Logger { get; set; }
    public required IResolver Resolver { get; set; }

    protected async Task WindowInitialize(WindowStartupLocation startupLocation = WindowStartupLocation.CenterScreen)
    {
        EnsureInitialized();
        try
        {
            ViewModel = Resolver.Resolve<TViewModel>(serviceKey: typeof(TViewModel).Name);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to resolve {className}", nameof(TViewModel));
            throw;
        }

        try
        {
            Icon = Resolver.Resolve<WindowIcon>();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to resolve AppIcon");
            throw;
        }
        WindowStartupLocation = startupLocation;
        ViewModel.Close += RequestClose;
    }
    
    public async ValueTask InitializeAsync(WindowStartupLocation startupLocation = WindowStartupLocation.CenterScreen, CancellationToken cancellationToken = default)
    {
        await WindowInitialize(startupLocation);
        ArgumentNullException.ThrowIfNull(ViewModel);
        await ViewModel!.InitializeAsync(cancellationToken);
    }

    private void RequestClose(object? sender, EventArgs e)
    {
        Logger.LogDebug("RequestClose from {sender}", sender);
        Close();
    }
}