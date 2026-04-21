using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia.Controls;
using DryIoc;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.MVVM;
using ReactiveUI.Avalonia;

namespace OpenSSH_GUI.Core.Resources.Wrapper;

public abstract class WindowBase<TViewModel, TViewModelInitializer> : WindowBase<TViewModel>
    where TViewModel : ViewModelBase<TViewModelInitializer>
{
    public async ValueTask InitializeAsync(TViewModelInitializer initializer,
        WindowStartupLocation startupLocation = WindowStartupLocation.CenterScreen,
        CancellationToken cancellationToken = default)
    {
        WindowInitialize(startupLocation);
        ArgumentNullException.ThrowIfNull(ViewModel);
        await ViewModel.InitializeAsync(initializer, cancellationToken);
    }
}

public abstract class WindowBase<TViewModel> : ReactiveWindow<TViewModel>, IDisposable
    where TViewModel : ViewModelBase
{
    private CompositeDisposable Disposables { get; } = new();
    public required ILogger<WindowBase<TViewModel>> Logger { get; set; }
    public required IResolver Resolver { get; set; }

    public void Dispose()
    {
        Disposables.Dispose();
    }

    protected void WindowInitialize(WindowStartupLocation startupLocation = WindowStartupLocation.CenterScreen)
    {
        EnsureInitialized();
        Observable.FromEventPattern(
                h => ActualThemeVariantChanged += h,
                h => ActualThemeVariantChanged -= h)
            .ObserveOn(AvaloniaScheduler.Instance)
            .Subscribe(_ => SetIcon())
            .DisposeWith(Disposables);
        try
        {
            ViewModel = Resolver.Resolve<TViewModel>(typeof(TViewModel).Name);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to resolve {className}", nameof(TViewModel));
            throw;
        }

        SetIcon();
        WindowStartupLocation = startupLocation;
        ViewModel.Close += RequestClose;
    }

    private void SetIcon()
    {
        try
        {
            if (Enum.TryParse<ThemeVariant>(ActualThemeVariant.Key.ToString(), true, out var themeVariant))
                Icon = Resolver.Resolve<WindowIcon>(string.Join("_", nameof(WindowIcon), 32, themeVariant).ToLower());
            else
                Logger.LogWarning("Could not resolve theme variant {themeVariant}", ActualThemeVariant);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to resolve AppIcon");
            throw;
        }
    }

    public async ValueTask InitializeAsync(WindowStartupLocation startupLocation = WindowStartupLocation.CenterScreen,
        CancellationToken cancellationToken = default)
    {
        WindowInitialize(startupLocation);
        ArgumentNullException.ThrowIfNull(ViewModel);
        await ViewModel!.InitializeAsync(cancellationToken);
    }

    private void RequestClose(object? sender, EventArgs e)
    {
        Logger.LogDebug("RequestClose from {sender}", sender);
        Close();
    }
}